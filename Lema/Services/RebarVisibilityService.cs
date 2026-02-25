using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BSSE.Utilities;
using view = Autodesk.Revit.DB.View;
using Autodesk.Revit.DB.Structure;

namespace BSSE.Services
{
    /// <summary>
    /// Manages per-bar visibility within a rebar set on a named plan view.
    ///
    /// PYTHON EQUIVALENT: process_rebar_visibility(doc, bar_number, bar_direction, should_hide)
    ///
    /// CALL SITE (RunLemaCommand, Transaction 3 — after doc.Regenerate())
    /// ────────────────────────────────────────────────────────────────────
    ///   var results = RebarVisibilityService.ProcessAll(doc);
    ///
    /// WHAT IT DOES
    /// ─────────────
    /// For bars 1 and 2 in both Dir. X and Dir. Y:
    ///   1. Hides every individual bar position in the rebar set.
    ///   2. Unhides exactly ONE representative bar so the plan view is readable:
    ///        Bar "2"       → unhide index 2        (bartohide constant)
    ///        All others    → unhide index (count-2) (last representative)
    ///
    /// PYTHON CHANGES
    /// ───────────────
    /// 1. NO REFLECTION. Python used r.GetType().GetMethod("SetBarHiddenStatus")
    ///    because IronPython cannot resolve the overload directly. In C#,
    ///    Rebar.SetBarHiddenStatus(View, int, bool) is a first-class typed call.
    ///
    /// 2. NULL GUARDS on LookupParameter results. Python line 544 called
    ///    .AsString() directly on the parameter — if either custom parameter
    ///    ("Bar_Number" / "Bar_Direction") is absent the Python crashes with
    ///    AttributeError. C# guards both lookups explicitly.
    ///
    /// 3. ELEMENT CAST. FilteredElementCollector returns Element; must be cast
    ///    to Rebar before SetBarHiddenStatus is available.
    /// </summary>
    public static class RebarVisibilityService
    {
        // ── Constants ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Name of the plan view in which bar visibility is controlled.
        /// Hardcoded in the Python function body (line 534).
        /// </summary>
        private const string VisibilityViewName = "PLAN FERRAILLAGE DU MASSIF 01";

        /// <summary>
        /// The bar-to-hide constant from the Python script (bartohide = 2).
        /// Used both as the unhide index for bar "2" and as the offset from
        /// the end of the bar array for all other bars.
        /// </summary>
        private const int BarToHideOffset = 2;

        /// <summary>
        /// The bar number / direction pairs to process.
        /// Mirrors Python's barProperties dict from the main execution block:
        ///   { "1": ["Dir. X", "Dir. Y"], "2": ["Dir. X", "Dir. Y"] }
        /// </summary>
        private static readonly (string BarNumber, string Direction)[] BarPairs =
        {
            ("1", "Dir. X"),
            ("1", "Dir. Y"),
            ("2", "Dir. X"),
            ("2", "Dir. Y"),
        };
        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Processes all bar/direction pairs defined in <see cref="BarPairs"/>.
        ///
        /// Must be called inside an open Transaction, after doc.Regenerate(),
        /// so that NumberOfBarPositions reflects the current rebar layout.
        ///
        /// Returns a flat list of result strings (one entry per rebar set
        /// processed, plus any errors). Mirrors the Python pattern of collecting
        /// per-rebar result strings.
        /// </summary>
        /// <param name="doc">The active Revit document.</param>
        /// <returns>
        ///     List of result messages. Entries beginning with "ERROR" indicate
        ///     a failure that did not abort the run.
        /// </returns>
        public static List<string> ProcessAll(Document doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            var allResults = new List<string>();

            // ── Resolve the plan view once, shared across all pairs ───────────────
            // Python resolves it inside each process_rebar_visibility call,
            // which means the same FilteredElementCollector query runs 4 times.
            // Resolving it once here is more efficient and fails fast if absent.
            view visibilityView;
            try
            {
                visibilityView = ElementFinder.GetByName(
                    doc,
                    VisibilityViewName,
                    ElementFinder.ElementScope.View) as view;

                if (visibilityView == null)
                    throw new InvalidOperationException(
                        $"Element named '{VisibilityViewName}' is not a View.");
            }
            catch (Exception ex)
            {
                allResults.Add($"ERROR: Could not resolve visibility view " +
                               $"'{VisibilityViewName}': {ex.Message}");
                return allResults;  // Cannot proceed without the target view
            }

            // ── Collect all rebar instances once, reused across all pairs ─────────
            IList<Element> allRebars = ElementFinder.GetAllRebars(doc);

            // ── Process each bar number / direction pair ───────────────────────────
            foreach (var (barNumber, direction) in BarPairs)
            {
                List<string> pairResults = ProcessOne(
                    doc,
                    visibilityView,
                    allRebars,
                    barNumber,
                    direction,
                    shouldHide: true);

                allResults.AddRange(pairResults);
            }

            return allResults;
        }
        // ── Private Implementation ────────────────────────────────────────────────

        /// <summary>
        /// Processes visibility for a single bar number / direction pair.
        ///
        /// Mirrors Python's process_rebar_visibility() with the transaction
        /// management and view resolution removed (handled by callers).
        /// </summary>
        /// <param name="doc">The active Revit document.</param>
        /// <param name="visibilityView">
        ///     The plan view in which hide/show is applied.
        ///     Already resolved by <see cref="ProcessAll"/>.
        /// </param>
        /// <param name="allRebars">
        ///     Pre-collected rebar instances. Passed in to avoid redundant
        ///     collector queries across multiple pairs.
        /// </param>
        /// <param name="barNumber">
        ///     Value of the "Bar_Number" shared parameter to match (e.g. "1").
        /// </param>
        /// <param name="direction">
        ///     Value of the "Bar_Direction" shared parameter to match (e.g. "Dir. X").
        /// </param>
        /// <param name="shouldHide">
        ///     True = hide all bars first then unhide one representative.
        ///     False = show all bars first then hide all but one.
        ///     Always true in the current call site; parameter kept for parity
        ///     with the Python signature.
        /// </param>
        private static List<string> ProcessOne(
            Document doc,
            view visibilityView,
            IList<Element> allRebars,
            string barNumber,
            string direction,
            bool shouldHide)
        {
            var results = new List<string>();

            foreach (Element e in allRebars)
            {
                // ── Cast to Rebar ─────────────────────────────────────────────────
                // ElementFinder returns Element; SetBarHiddenStatus lives on Rebar.
                if (!(e is Rebar rebar))
                    continue;

                // ── Read custom shared parameters ─────────────────────────────────
                // Python: bar_num_param.AsString() with no null check.
                // C#: guard both lookups — if either parameter is absent on this
                // rebar element, skip it rather than throwing.
                Parameter barNumParam = rebar.LookupParameter("Bar_Number");
                Parameter barDirParam = rebar.LookupParameter("Bar_Direction");

                if (barNumParam == null || barDirParam == null)
                    continue;

                string currentBarNum = barNumParam.AsString();
                string currentBarDir = barDirParam.AsString();

                // ── Filter by number and direction ────────────────────────────────
                if (currentBarNum != barNumber || currentBarDir != direction)
                    continue;

                // ── Apply visibility changes ──────────────────────────────────────
                int barCount = rebar.NumberOfBarPositions;

                try
                {
                    // Step 1: set all bar positions to shouldHide
                    // Python: hide_method.Invoke(r, [view, i, should_hide])
                    // C#:     rebar.SetBarHiddenStatus(view, i, shouldHide) — direct call
                    for (int i = 0; i < barCount; i++)
                        rebar.SetBarHiddenStatus(visibilityView, i, shouldHide);

                    // Step 2: unhide the single representative bar
                    // Python:
                    //   if bar_number == "2": unhide_index = bartohide        → 2
                    //   else:                 unhide_index = numberOfBars - bartohide
                    int unhideIndex = (barNumber == "2")
                        ? BarToHideOffset
                        : barCount - BarToHideOffset;

                    // Guard: unhideIndex must be within range after the count
                    // is known. A model with fewer bars than BarToHideOffset
                    // would produce a negative index and crash Revit.
                    if (unhideIndex < 0 || unhideIndex >= barCount)
                    {
                        results.Add(
                            $"ERROR: Computed unhide index {unhideIndex} is out of " +
                            $"range [0, {barCount - 1}] for rebar Id {rebar.Id} " +
                            $"(Bar_Number={barNumber}, Bar_Direction={direction}). Skipped.");
                        continue;
                    }

                    rebar.SetBarHiddenStatus(visibilityView, unhideIndex, false);

                    results.Add(
                        $"OK: Bar_Number={barNumber}, Bar_Direction={direction}, " +
                        $"RebarId={rebar.Id} — {barCount} bars processed, " +
                        $"index {unhideIndex} left visible.");
                }
                catch (Exception ex)
                {
                    // Per-rebar errors are logged, not rethrown, matching Python's
                    // except/continue pattern.
                    results.Add(
                        $"ERROR: RebarId={rebar.Id} " +
                        $"(Bar_Number={barNumber}, Bar_Direction={direction}): {ex.Message}");
                }
            }

            return results;
        }

    }
}
