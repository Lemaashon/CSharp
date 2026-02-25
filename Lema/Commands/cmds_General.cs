using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using models = BSSE.Models;
using taskDialog = Autodesk.Revit.UI.TaskDialog;
using BSSE.Services;

namespace BSSE.Commands
{
    /// <summary>
    /// Main Lema command. Orchestrates the full update pipeline:
    ///
    ///   1. Global Parameters      — writes STR_* / PIM_* global params + revisions
    ///   2. Rebar Update           — spacing, type, hook lengths + stirrup globals
    ///   3. Rebar Visibility       — doc.Regenerate() then SetBarHiddenStatus
    ///   4. Section Boxes          — foundation bounding box → 4 detail view extents
    ///
    /// TRANSACTION STRATEGY
    /// ─────────────────────
    /// Each stage owns exactly one Transaction. A failure inside a stage rolls back
    /// only that stage — work committed by earlier stages is preserved. This matches
    /// the intent of the original Python script, where each EnsureInTransaction block
    /// was a logically separate unit.
    ///
    /// RESULT REPORTING
    /// ─────────────────
    /// Each transaction populates a PhaseResult record. After all four transactions
    /// complete, ShowSummary() renders a structured TaskDialog distinguishing
    /// successes from warnings from failures, with a scrollable expanded section
    /// for the full per-item log.
    ///
    /// RETURN VALUE
    /// ─────────────
    /// Returns Result.Succeeded if at least one transaction committed successfully.
    /// Returns Result.Failed only if every transaction failed (no changes at all).
    /// Returns Result.Cancelled if the user dismisses the file picker.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class cmds_General : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;

            // Guard: no document open
            if (uiDoc == null)
            {
                message = "Addin requires an open Revit document. Please open a project first.";
                return Result.Failed;
            }
            
            Document doc = uiDoc.Document;

            // ── Step 1: File picker ───────────────────────────────────────────────
            string jsonPath = PromptForJsonFile();
            if (string.IsNullOrEmpty(jsonPath))
                return Result.Cancelled;

            // ── Step 2: Load and deserialise JSON ─────────────────────────────────
            models.JsonRoot jsonData;
            try
            {
                jsonData = JsonDataService.Load(jsonPath);
            }
            catch (Exception ex)
            {
                message = $"Failed to load JSON configuration: {ex.Message}";
                return Result.Failed;
            }

            // ── Steps 3–6: Run the four transactions ──────────────────────────────
            var phases = new List<PhaseResult>();

            phases.Add(RunTransaction(
                doc,
                name: "Update Global Parameters",
                label: "Global Parameters",
                work: () =>
                {
                    var gpResults = GlobalParameterService.SetFromProjectConfig(
                        doc, jsonData.ProjectInfo);

                    // Convert the dictionary to a flat list of result strings
                    // so PhaseResult can count OK vs FAIL uniformly.
                    return gpResults
                        .Select(kvp =>
                            $"{(kvp.Value == "OK" ? "OK" : "FAIL")} [{kvp.Key}]: {kvp.Value}")
                        .ToList();
                }));

            phases.Add(RunTransaction(
                doc,
                name: "Update Rebars",
                label: "Rebar Update",
                work: () => RebarService.UpdateFromConfig(
                    doc, jsonData.BarInfo, jsonData.ProjectInfo)));

            // Transaction 3: Regenerate MUST be the first call after tx.Start().
            // doc.Regenerate() is a document modification — forbidden outside a
            // transaction. It must run before RebarVisibilityService reads
            // NumberOfBarPositions, which depends on the post-spacing geometry.
            phases.Add(RunTransaction(
                doc,
                name: "Rebar Visibility",
                label: "Rebar Visibility",
                work: () =>
                {
                    doc.Regenerate();
                    return RebarVisibilityService.ProcessAll(doc);
                }));

            phases.Add(RunTransaction(
                doc,
                name: "BSSE: Section Boxes",
                label: "Section Boxes",
                work: () => SectionBoxService.SetFoundationSectionBox(
                    doc, jsonData.ProjectInfo)));

            // ── Step 7: Show structured summary ───────────────────────────────────
            ShowSummary(phases);

            // ── Step 8: Determine return value ────────────────────────────────────
            // Succeeded  = at least one transaction committed
            // Failed     = every transaction failed (nothing was written to the model)
            bool anySuccess = phases.Any(p => p.Committed);
            if (!anySuccess)
            {
                message = "All four transactions failed. No changes were made. " +
                          "Check the summary dialog for details.";
                return Result.Failed;
            }

            return Result.Succeeded;
        }

        // ── Transaction runner ────────────────────────────────────────────────────

        /// <summary>
        /// Executes <paramref name="work"/> inside a named Transaction and captures
        /// the result into a <see cref="PhaseResult"/>.
        ///
        /// ROLLBACK BEHAVIOUR
        /// ───────────────────
        /// If <paramref name="work"/> throws, the transaction is explicitly rolled
        /// back before the exception is caught. This prevents a dangling transaction
        /// that could corrupt the Revit undo stack.
        ///
        /// If Commit() itself throws (rare, e.g. constraint violation), that exception
        /// is also caught and the phase is marked as failed with the commit error.
        /// </summary>
        private static PhaseResult RunTransaction(
            Document doc,
            string name,
            string label,
            Func<List<string>> work)
        {
            var phase = new PhaseResult(label);

            try
            {
                using (var tx = new Transaction(doc, name))
                {
                    tx.Start();

                    try
                    {
                        phase.Lines = work();
                    }
                    catch (Exception workEx)
                    {
                        // Work threw — roll back explicitly before recording the failure.
                        tx.RollBack();
                        phase.FailureReason = $"Transaction rolled back: {workEx.Message}";
                        return phase;
                    }

                    // Commit can throw (e.g. regeneration failure, constraint violation).
                    try
                    {
                        tx.Commit();
                        phase.Committed = true;
                    }
                    catch (Exception commitEx)
                    {
                        phase.FailureReason = $"Commit failed: {commitEx.Message}";
                    }
                }
            }
            catch (Exception outerEx)
            {
                // Catches Transaction construction or Start() failures.
                phase.FailureReason = $"Transaction could not start: {outerEx.Message}";
            }

            return phase;
        }

        // ── Summary dialog ────────────────────────────────────────────────────────

        /// <summary>
        /// Renders a structured Revit TaskDialog showing:
        ///   - A one-line status per phase (committed / rolled back)
        ///   - An expandable section with the full per-item log
        ///
        /// TaskDialog.MainContent is limited to approximately 1 000 characters before
        /// Revit silently truncates it. The full log is placed in the expandable
        /// ExpandedContent section which has no practical limit.
        ///
        /// FAILURE vs WARNING distinction:
        ///   Lines starting with "ERROR" or "FAIL" → counted as failures
        ///   Lines starting with "WARN"             → counted as warnings
        ///   Lines starting with "OK" or "INFO"     → successes / informational
        /// </summary>
        private static void ShowSummary(List<PhaseResult> phases)
        {
            // ── Build the brief per-phase status table (goes in MainContent) ──────
            var brief = new StringBuilder();
            int totalOk = 0;
            int totalFail = 0;
            int totalWarn = 0;

            foreach (PhaseResult phase in phases)
            {
                string status;
                if (!phase.Committed)
                {
                    status = $"✗ FAILED  — {phase.FailureReason ?? "Unknown error"}";
                }
                else
                {
                    int ok = phase.Lines.Count(l => l.StartsWith("OK", StringComparison.OrdinalIgnoreCase));
                    int fail = phase.Lines.Count(l => l.StartsWith("FAIL", StringComparison.OrdinalIgnoreCase)
                                                   || l.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase));
                    int warn = phase.Lines.Count(l => l.StartsWith("WARN", StringComparison.OrdinalIgnoreCase));

                    totalOk += ok;
                    totalFail += fail;
                    totalWarn += warn;

                    string inner = fail > 0
                        ? $"{fail} error(s), {warn} warning(s), {ok} OK"
                        : warn > 0
                            ? $"{warn} warning(s), {ok} OK"
                            : $"{ok} OK";

                    status = $"✓ committed  — {inner}";
                }

                brief.AppendLine($"[{phase.Label,-22}]  {status}");
            }

            // ── Build the full per-item log (goes in ExpandedContent) ─────────────
            var detail = new StringBuilder();
            foreach (PhaseResult phase in phases)
            {
                detail.AppendLine($"── {phase.Label} ──────────────────────────────");
                if (!phase.Committed)
                {
                    detail.AppendLine($"  FAILED: {phase.FailureReason}");
                }
                else
                {
                    foreach (string line in phase.Lines)
                        detail.AppendLine($"  {line}");
                }
                detail.AppendLine();
            }

            // ── Compose TaskDialog ────────────────────────────────────────────────
            bool anyFailed = phases.Any(p => !p.Committed || p.Lines.Any(l =>
                l.StartsWith("FAIL", StringComparison.OrdinalIgnoreCase) ||
                l.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase)));

            string instruction = anyFailed
                ? $"Finished with issues  —  {totalFail} error(s), {totalWarn} warning(s)"
                : $"Finished successfully  —  {totalOk} item(s) updated";

            var td = new taskDialog("Run Complete")
            {
                MainInstruction = instruction,
                MainContent = brief.ToString(),
                ExpandedContent = detail.ToString(),
                // CollapsedHeight = 300,
                CommonButtons = TaskDialogCommonButtons.Close
            };

            td.Show();
        }

        // ── File picker ───────────────────────────────────────────────────────────

        /// <summary>
        /// Opens a WinForms OpenFileDialog filtered to *.json.
        /// Returns the selected absolute path, or null if the user cancels.
        ///
        /// WinForms is used (not WPF) because it requires no additional assembly
        /// reference beyond System.Windows.Forms, which is already present in all
        /// Revit 2024+ installations. Ensure System.Windows.Forms is referenced
        /// in Lema.csproj (it is not referenced by default in net48 projects):
        ///
        ///   &lt;Reference Include="System.Windows.Forms" /&gt;
        /// </summary>
        private static string PromptForJsonFile()
        {
            using (var dialog = new System.Windows.Forms.OpenFileDialog())
            {
                dialog.Title = "Select JSON Configuration File";
                dialog.Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*";
                dialog.FilterIndex = 1;
                dialog.Multiselect = false;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    return dialog.FileName;
            }

            return null;
        }

        // ── Inner types ───────────────────────────────────────────────────────────

        /// <summary>
        /// Captures the outcome of one pipeline transaction.
        /// </summary>
        private sealed class PhaseResult
        {
            public string Label { get; }
            public bool Committed { get; set; }
            public string FailureReason { get; set; }
            public List<string> Lines { get; set; } = new List<string>();

            public PhaseResult(string label) => Label = label;
        }
    }
}