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

using models = BSSE.Models;
using taskDialog = Autodesk.Revit.UI.TaskDialog;
using BSSE.Services;

namespace BSSE.Commands
{
    /// <summary>
    /// Main BSSE command. Orchestrates the full update pipeline:
    ///   1. Global Parameters
    ///   2. Rebar Spacing / Types / Hook Lengths
    ///   3. Rebar Visibility
    ///   4. Section Boxes + Far Clip Offsets
    ///
    /// Each stage runs in its own Transaction so a failure in one stage
    /// does not roll back the work already committed by previous stages.
    ///
    /// Phase 1: Scaffold only — stages 1-4 are stubs that log "TODO".
    ///          Replace each stub call with the real service in Phases 4-8.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class cmds_General : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            // ── Step 1: Ask the user for the JSON config file path ────────────────

            string jsonPath = PromptForJsonFile();

            if (string.IsNullOrEmpty(jsonPath))
            {
                // User cancelled the file dialog — exit cleanly.
                return Result.Cancelled;
            }

            // ── Step 2: Load and validate JSON ───────────────────────────────────
            // Phase 3 will replace this stub with JsonDataService.Load().

            models.JsonRoot jsonData;
            try
            {
                jsonData = JsonDataService.Load(jsonPath);
            }
            catch (Exception ex)
            {
                message = $"Failed to load JSON: {ex.Message}";
                return Result.Failed;
            }

            // ── Collect phase results for the summary dialog ──────────────────────

            var summary = new List<string>();

            // ── Phase 4 stub: Transaction 1 — Global Parameters ──────────────────
            try
            {
                using (var tx = new Transaction(doc, "BSSE: Update Global Parameters"))
                {
                    tx.Start();

                    var gpResults = GlobalParameterService.SetFromProjectConfig(
                        doc, jsonData.ProjectInfo);

                    // Log each per-parameter result into the summary
                    foreach (var kvp in gpResults)
                        summary.Add($"{(kvp.Value == "OK" ? "OK" : "FAIL")} GP [{kvp.Key}]: {kvp.Value}");

                    tx.Commit();
                }
            }
            catch (Exception ex)
            {
                summary.Add($"✗ Global Parameters failed: {ex.Message}");
            }

            // ── Phase 6 stub: Transaction 2 — Rebar Spacing / Types / Hooks ──────
            try
            {
                using (var tx = new Transaction(doc, "BSSE: Update Rebars"))
                {
                    tx.Start();

                    // TODO (Phase 6): Replace with
                    //   RebarService.UpdateFromConfig(doc, jsonData.BarInfo, jsonData.ProjectInfo);
                    summary.Add("✓ [STUB] Rebar update transaction committed.");

                    tx.Commit();
                }
            }
            catch (Exception ex)
            {
                summary.Add($"✗ Rebar update failed: {ex.Message}");
            }

            // ── Regenerate between rebar write and visibility ─────────────────────
            // Mirrors the Python script's doc.Regenerate() call.
            // Must happen outside any transaction.

            // ── Phase 7 stub: Transaction 3 — Rebar Visibility ───────────────────
            try
            {
                using (var tx = new Transaction(doc, "BSSE: Rebar Visibility"))
                {
                    tx.Start();

                    // Regenerate so NumberOfBarPositions is accurate before
                    // we index into it for SetBarHiddenStatus.
                    doc.Regenerate();

                    // TODO (Phase 7): Replace with
                    //   RebarVisibilityService.ProcessAll(doc, barProperties);
                    summary.Add("✓ [STUB] Rebar visibility transaction committed.");

                    tx.Commit();
                }
            }
            catch (Exception ex)
            {
                summary.Add($"✗ Rebar visibility failed: {ex.Message}");
            }

            // ── Phase 8 stub: Transaction 4 — Section Boxes ───────────────────────
            try
            {
                using (var tx = new Transaction(doc, "BSSE: Section Boxes"))
                {
                    tx.Start();

                    // TODO (Phase 8): Replace with
                    //   SectionBoxService.SetFoundationSectionBox(doc, jsonData.ProjectInfo);
                    summary.Add("✓ [STUB] Section box transaction committed.");

                    tx.Commit();
                }
            }
            catch (Exception ex)
            {
                summary.Add($"✗ Section box failed: {ex.Message}");
            }

            // ── Show result summary ───────────────────────────────────────────────
            ShowSummary(summary);

            return Result.Succeeded;
        }

        // ── Private Helpers ───────────────────────────────────────────────────────

        /// <summary>
        /// Opens a Windows file picker filtered to *.json files.
        /// Returns the selected path, or null if the user cancels.
        /// </summary>
        private static string PromptForJsonFile()
        {
            // Revit runs on Windows, so WinForms OpenFileDialog is reliable here.
            // No WPF dependency needed.
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

        /// <summary>
        /// Displays the per-phase results in a Revit TaskDialog.
        /// </summary>
        private static void ShowSummary(List<string> lines)
        {
            var td = new taskDialog("BSSE — Run Complete")
            {
                MainInstruction = "Pipeline finished. Review results below:",
                MainContent = string.Join(Environment.NewLine, lines),
                CommonButtons = TaskDialogCommonButtons.Close
            };

            td.Show();
        }
    }
}
