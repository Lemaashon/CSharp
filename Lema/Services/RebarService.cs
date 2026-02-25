using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BSSE.Utilities;
using BSSE.Models;
using view = Autodesk.Revit.DB.View;
using Autodesk.Revit.DB.Structure;

namespace BSSE.Services
{
    /// <summary>
    /// Updates rebar spacing, bar types, and hook lengths from the JSON
    /// BarInfo configuration. Also computes stirrup extents, updates the
    /// matching global parameters, and sets the far clip offset on the
    /// stirrup section view.
    ///
    /// PYTHON EQUIVALENT: update_rebars_from_config(doc, barInfo, projectInfo)
    ///
    /// CALL SITE (RunLemaCommand, Transaction 2)
    /// ──────────────────────────────────────────
    ///   var results = RebarService.UpdateFromConfig(doc, jsonData.BarInfo, jsonData.ProjectInfo);
    ///
    /// PYTHON BUGS / CHANGES
    /// ──────────────────────
    /// 1. NO REFLECTION. Two reflection calls are replaced with direct typed calls:
    ///      GetMethod("GetValidTypes")  →  rebar.GetValidTypes()
    ///      GetMethod("ChangeTypeId")   →  rebar.ChangeTypeId(id)
    ///
    /// 2. NO UnwrapElement(). That is a Dynamo helper. The collector already
    ///    returns native Revit API elements in a C# add-in.
    ///
    /// 3. bar_num AsString() OR AsInteger() fallback is explicit (not Python 'or').
    ///
    /// 4. str(barDiameter) name matching: Python str(12.0) = "12.0" which would
    ///    NOT match a type named "T12". C# uses the integer representation for
    ///    whole-number diameters ("12"), which correctly matches "T12", "H12" etc.
    ///
    /// 5. Hook parameter null guards added — Python accessed .LookupParameter("A")
    ///    without checking the return value.
    /// </summary>
    public static class RebarService
    {
        // ── Constants (mirror Python locals) ──────────────────────────────────────

        private const double BarFourFactor = 40.0;   // hook length multiplier for bar 4
        private const double BarThreeFactor = 50.0;   // hook length multiplier for bar 3
        private const double Factor = 1.3;    // general stirrup geometry factor
        private const double Tolerance = 50.0;   // mm tolerance in stirrup extents formula
        private const string StirrupViewName = "COUPE 1-1 B";

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Main entry point. Must be called inside an open Transaction.
        ///
        /// Execution order (mirrors Python exactly):
        ///   1. Read required bar diameters from BarInfo.
        ///   2. Compute stirrup extents (pure arithmetic, mm).
        ///   3. Set far clip offset on the stirrup section view.
        ///   4. Write the three stirrup global parameters.
        ///   5. Collect all rebar types and rebar instances.
        ///   6. Loop over each BarInfo entry → find matching rebars → apply changes.
        /// </summary>
        /// <param name="doc">The active Revit document.</param>
        /// <param name="barInfo">
        ///     Dictionary keyed by bar label (e.g. "1 (Dir. X)", "5").
        ///     Values contain spacing (mm), bartype (mm diameter), optional number.
        /// </param>
        /// <param name="projectConfig">
        ///     Project-level values needed for stirrup calculations.
        /// </param>
        /// <returns>
        ///     Flat list of result strings — one entry per action, prefixed
        ///     "OK" or "ERROR".
        /// </returns>
        public static List<string> UpdateFromConfig(
            Document doc,
            Dictionary<string, BarInfo> barInfo,
            ProjectConfig projectConfig)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (barInfo == null) throw new ArgumentNullException(nameof(barInfo));
            if (projectConfig == null) throw new ArgumentNullException(nameof(projectConfig));

            var results = new List<string>();

            // ── Step 1: Read the three bar diameters needed for stirrup maths ─────
            // Python accesses these directly by dict key — throws KeyError if absent.
            // C# uses TryGetValue and reports a clear message if a key is missing.
            if (!barInfo.TryGetValue("5", out BarInfo barFiveInfo) ||
                !barInfo.TryGetValue("6", out BarInfo barSixInfo) ||
                !barInfo.TryGetValue("1 (Dir. X)", out BarInfo barOneInfo))
            {
                results.Add("ERROR: BarInfo must contain keys '5', '6', and '1 (Dir. X)' " +
                            "for stirrup extent calculations. Aborting.");
                return results;
            }

            double barTypeFive = barFiveInfo.BarType;
            double barTypeSix = barSixInfo.BarType;
            double barTypeOne = barOneInfo.BarType;
            double diameterOuter = projectConfig.BasePlateDiameterOuter;
            double foundationDepth = projectConfig.FoundationDepth;

            // ── Step 2: Compute stirrup extents (all values in mm) ────────────────
            // Mirrors Python lines 315–319 exactly — arithmetic is identical.
            double stirrupExtents =
                diameterOuter + (2 * Tolerance) + (2 * barTypeFive) +
                (2 * barTypeSix) - (barTypeSix * Factor);

            double stirrupPerimeterLength =
                stirrupExtents - (barTypeFive + barTypeSix) * Factor;

            double stirrupExtentsDepth =
                foundationDepth - (55 + barTypeOne * 1.5 * Factor) -
                (barTypeFive * 0.5 * 1.3);

            double viewDepthMm = stirrupExtents / 2.0 + 50.0;

            results.Add($"OK: Stirrup extents computed — " +
                        $"Extents={stirrupExtents:F1}mm, " +
                        $"Perimeter={stirrupPerimeterLength:F1}mm, " +
                        $"Depth={stirrupExtentsDepth:F1}mm");

            // ── Step 3: Set far clip offset on the stirrup section view ───────────
            // Python: set_section_view_depth(viewName, viewDepth)
            // Must be inside the open transaction (ViewService requires this).
            try
            {
                ViewService.SetFarClipOffset(doc, StirrupViewName, viewDepthMm);
                results.Add($"OK: Far clip offset set on '{StirrupViewName}' " +
                            $"to {viewDepthMm:F1}mm.");
            }
            catch (Exception ex)
            {
                results.Add($"ERROR: Could not set far clip on '{StirrupViewName}': {ex.Message}");
                // Non-fatal — continue with the rest of the rebar updates.
            }

            // ── Step 4: Write stirrup global parameters ────────────────────────────
            // Python: set_global_parameters_from_dict(doc, stirrupExtentsDict)
            // C#: call SetOne directly — these are three known double parameters.
            var stirrupParams = new Dictionary<string, double>
            {
                { "STR_Stirrup_Extents",        stirrupExtents        },
                { "STR_Stirrup_PerimeterLength", stirrupPerimeterLength },
                { "STR_Stirrup_Extents_Depth",  stirrupExtentsDepth   }
            };

            foreach (var kvp in stirrupParams)
            {
                try
                {
                    // GlobalParameterService.SetOne converts mm → ft internally
                    // for DoubleParameterValue global parameters.
                    GlobalParameterService.SetOne(doc, kvp.Key, kvp.Value);
                    results.Add($"OK: Global parameter '{kvp.Key}' = {kvp.Value:F2}mm");
                }
                catch (Exception ex)
                {
                    results.Add($"ERROR: Global parameter '{kvp.Key}': {ex.Message}");
                }
            }

            // ── Step 5: Collect rebar types and instances once ────────────────────
            IList<Element> rebarTypes = ElementFinder.GetAllRebarTypes(doc);
            IList<Element> allRebars = ElementFinder.GetAllRebars(doc);

            // ── Step 6: Process each BarInfo entry ────────────────────────────────
            foreach (var kvp in barInfo)
            {
                string configKey = kvp.Key;
                BarInfo configValues = kvp.Value;

                List<string> entryResults = ProcessBarEntry(
                    doc, configKey, configValues,
                    barInfo, stirrupPerimeterLength,
                    rebarTypes, allRebars);

                results.AddRange(entryResults);
            }

            return results;
        }
        // ── Private: per-entry processing ─────────────────────────────────────────

        /// <summary>
        /// Processes a single BarInfo entry: resolves spacing, finds the matching
        /// rebar type and rebar instances, then applies spacing / type / hook changes.
        /// </summary>
        private static List<string> ProcessBarEntry(
            Document doc,
            string configKey,
            BarInfo configValues,
            Dictionary<string, BarInfo> barInfo,
            double stirrupPerimeterLength,
            IList<Element> rebarTypes,
            IList<Element> allRebars)
        {
            var results = new List<string>();

            // ── Parse key → bar number + optional direction ───────────────────────
            (string barNumber, string barDirection) = ParseBarKey(configKey);

            double barDiameter = configValues.BarType;
            string barTypeName = FormatDiameterForTypeMatch(barDiameter);

            // ── Resolve spacing ───────────────────────────────────────────────────
            // For bar "5": spacing is derived from stirrup perimeter, not from JSON.
            double? fixedNumber = null;
            double? spacingMm = configValues.Spacing;

            if (configKey == "5")
            {
                if (configValues.Number == null)
                {
                    results.Add($"ERROR: Bar '5' requires a 'number' field in BarInfo.");
                    return results;
                }

                fixedNumber = configValues.Number.Value;
                double numberPerFace = fixedNumber.Value / 4.0 + 1.0;
                spacingMm = stirrupPerimeterLength / (numberPerFace - 1.0);
            }

            double? spacingFt = spacingMm.HasValue ? UnitConverter.MmToFt(spacingMm.Value)
                                                   : (double?)null;

            // ── Find the desired rebar type by name ───────────────────────────────
            Element desiredType = FindRebarTypeByName(rebarTypes, barTypeName);

            if (desiredType == null)
                results.Add($"WARN: No rebar type matching '{barTypeName}' found for {configKey}.");

            // ── Find matching rebar instances ─────────────────────────────────────
            List<Rebar> matchingRebars = FindMatchingRebars(allRebars, barNumber, barDirection);

            if (matchingRebars.Count == 0)
            {
                results.Add($"WARN: No matching rebars found for {configKey}.");
                return results;
            }

            // ── Apply changes to each matched rebar ───────────────────────────────
            bool anyChange = false;

            foreach (Rebar rebar in matchingRebars)
            {
                bool changed = ApplyChangesToRebar(
                    doc, rebar, configKey, barDiameter,
                    fixedNumber, spacingFt,
                    configValues.Number.HasValue ? (fixedNumber / 4.0 + 1.0) : (double?)null,
                    desiredType,
                    results);

                if (changed) anyChange = true;
            }

            // ── Log summary for this entry ────────────────────────────────────────
            // Mirrors Python's end-of-loop log block.
            if (anyChange)
                results.Add($"OK: Updated {matchingRebars.Count} rebar(s) for [{configKey}].");
            else
                results.Add($"INFO: {matchingRebars.Count} rebar(s) found for [{configKey}] — no changes applied.");

            return results;
        }
        /// <summary>
        /// Applies spacing, type change, and hook length to a single Rebar element.
        /// Returns true if any change was successfully made.
        /// </summary>
        private static bool ApplyChangesToRebar(
            Document doc,
            Rebar rebar,
            string configKey,
            double barDiameter,
            double? fixedNumber,
            double? spacingFt,
            double? numberPerFace,
            Element desiredType,
            List<string> results)
        {
            bool changesMade = false;

            // ── Spacing / quantity ────────────────────────────────────────────────
            if (fixedNumber != null && spacingFt.HasValue && numberPerFace.HasValue)
            {
                // Bar "5": NumberWithSpacing layout — set both Spacing and Quantity.
                // Python: spacing_param.Set(spacing_ft) + quantity_param.Set(int(numberperFace))
                try
                {
                    Parameter spacingParam = rebar.LookupParameter("Spacing");
                    Parameter quantityParam = rebar.LookupParameter("Quantity");

                    if (spacingParam == null || quantityParam == null)
                        throw new InvalidOperationException(
                            "Rebar is missing 'Spacing' or 'Quantity' parameter.");

                    spacingParam.Set(spacingFt.Value);
                    quantityParam.Set((int)Math.Round(numberPerFace.Value));
                    changesMade = true;
                }
                catch (Exception ex)
                {
                    results.Add($"ERROR: NumberWithSpacing for {configKey} " +
                                $"(Id={rebar.Id}): {ex.Message}");
                }
            }
            else
            {
                // Standard spacing layout.
                // Python dead-code note: "if config_key == '5': continue" in the
                // else branch is unreachable for key "5" since fixedNumber is always
                // set for that key. The guard above already handles bar "5" entirely.
                if (spacingFt.HasValue)
                {
                    Parameter spacingParam = rebar.LookupParameter("Spacing");
                    if (spacingParam != null && !spacingParam.IsReadOnly)
                    {
                        try
                        {
                            spacingParam.Set(spacingFt.Value);
                            changesMade = true;
                        }
                        catch (Exception ex)
                        {
                            results.Add($"ERROR: Setting spacing for {configKey} " +
                                        $"(Id={rebar.Id}): {ex.Message}");
                        }
                    }
                }
            }
            // ── Type change ───────────────────────────────────────────────────────
            // Python used reflection: GetMethod("GetValidTypes") + GetMethod("ChangeTypeId")
            // C#: direct typed API calls on Rebar.
            if (desiredType != null)
            {
                try
                {
                    ICollection<ElementId> validTypes = rebar.GetValidTypes();

                    if (validTypes.Contains(desiredType.Id))
                    {
                        rebar.ChangeTypeId(desiredType.Id);
                        changesMade = true;
                    }
                    else
                    {
                        results.Add($"WARN: Type '{desiredType.Name}' is not valid " +
                                    $"for rebar Id={rebar.Id} [{configKey}]. Skipped.");
                    }
                }
                catch (Exception ex)
                {
                    results.Add($"ERROR: Type change for {configKey} (Id={rebar.Id}): {ex.Message}");
                }
            }

            // ── Hook lengths ──────────────────────────────────────────────────────
            // Bar 4: A = barFourFactor × diameter (in mm) → convert to ft
            // Bar 3: A = barThreeFactor × diameter, C = same value
            // Python had no null guard on LookupParameter("A") — fixed here.
            if (configKey == "4 (Dir. X)" || configKey == "4 (Dir. Y)")
            {
                double hookFt = UnitConverter.MmToFt(BarFourFactor * barDiameter);
                changesMade |= SetHookParameter(rebar, "A", hookFt, configKey, results);
            }

            if (configKey == "3 (Dir. X)" || configKey == "3 (Dir. Y)")
            {
                double hookFt = UnitConverter.MmToFt(BarThreeFactor * barDiameter);
                changesMade |= SetHookParameter(rebar, "A", hookFt, configKey, results);
                changesMade |= SetHookParameter(rebar, "C", hookFt, configKey, results);
            }

            return changesMade;
        }
        // ── Private: helper methods ───────────────────────────────────────────────

        /// <summary>
        /// Parses a BarInfo key into a (barNumber, barDirection) tuple.
        ///
        /// PYTHON EQUIVALENT: parse_bar_info(key)
        ///   "1 (Dir. X)" → ("1", "Dir. X")
        ///   "5"          → ("5", null)
        /// </summary>
        private static (string BarNumber, string BarDirection) ParseBarKey(string key)
        {
            int parenIndex = key.IndexOf(" (", StringComparison.Ordinal);
            if (parenIndex >= 0)
            {
                string number = key.Substring(0, parenIndex);
                string direction = key.Substring(parenIndex + 2)
                                      .TrimEnd(')');
                return (number, direction);
            }
            return (key, null);
        }

        /// <summary>
        /// Converts a bar diameter double to the string used for type-name matching.
        ///
        /// PYTHON BUG FIXED:
        /// Python: str(12.0) = "12.0" — this does NOT match a type named "T12".
        /// C#: produces "12" for whole-number values, "12.5" for fractions.
        /// Rebar type names in Revit are typically "T12", "H12", "Ø12" etc.,
        /// so "12" as a substring match is correct.
        /// </summary>
        private static string FormatDiameterForTypeMatch(double diameter)
        {
            // Use integer representation when the value is a whole number.
            if (Math.Abs(diameter - Math.Round(diameter)) < 1e-9)
                return ((int)Math.Round(diameter)).ToString();

            return diameter.ToString("G");
        }

        /// <summary>
        /// Finds the first rebar type element whose SYMBOL_NAME_PARAM contains
        /// <paramref name="barTypeName"/> as a substring.
        ///
        /// PYTHON EQUIVALENT: loop over rebar_types, type_name contains barType_name.
        /// Bare except: continue — preserved as try/catch with continue.
        /// </summary>
        private static Element FindRebarTypeByName(IList<Element> rebarTypes, string barTypeName)
        {
            foreach (Element t in rebarTypes)
            {
                try
                {
                    Parameter nameParam = t.get_Parameter(BuiltInParameter.SYMBOL_NAME_PARAM);
                    if (nameParam == null) continue;

                    string typeName = nameParam.AsString();
                    if (typeName != null && typeName.IndexOf(barTypeName,
                            StringComparison.OrdinalIgnoreCase) >= 0)
                        return t;
                }
                catch
                {
                    continue;
                }
            }
            return null;
        }
        /// <summary>
        /// Filters all rebar instances to those matching the given bar number
        /// and optional direction shared parameters.
        ///
        /// PYTHON EQUIVALENT: the matching_rebars build loop.
        ///
        /// bar_num fallback:
        ///   Python: bar_num_param.AsString() or str(bar_num_param.AsInteger())
        ///   C#:     explicit null/empty check with integer fallback.
        /// </summary>
        private static List<Rebar> FindMatchingRebars(
            IList<Element> allRebars,
            string barNumber,
            string barDirection)
        {
            var matching = new List<Rebar>();

            foreach (Element e in allRebars)
            {
                if (!(e is Rebar rebar)) continue;

                Parameter barNumParam = rebar.LookupParameter("Bar_Number");
                if (barNumParam == null) continue;

                // Python: bar_num = bar_num_param.AsString() or str(bar_num_param.AsInteger())
                string barNum = barNumParam.AsString();
                if (string.IsNullOrEmpty(barNum))
                    barNum = barNumParam.AsInteger().ToString();

                if (barDirection == null)
                {
                    // No direction filter (bars 5, 6)
                    if (barNum == barNumber)
                        matching.Add(rebar);
                }
                else
                {
                    Parameter barDirParam = rebar.LookupParameter("Bar_Direction");
                    if (barDirParam == null) continue;

                    string barDir = barDirParam.AsString();
                    if (barNum == barNumber && barDir == barDirection)
                        matching.Add(rebar);
                }
            }

            return matching;
        }

        /// <summary>
        /// Sets a named hook parameter on a rebar, with full null-guard.
        /// Returns true if the parameter was set successfully.
        ///
        /// PYTHON BUG FIXED:
        /// native_rebar.LookupParameter("A").Set(value) — no null check.
        /// If the parameter doesn't exist this crashes with NullReferenceException.
        /// </summary>
        private static bool SetHookParameter(
            Rebar rebar,
            string paramName,
            double valueFt,
            string configKey,
            List<string> results)
        {
            Parameter p = rebar.LookupParameter(paramName);

            if (p == null)
            {
                results.Add($"WARN: Hook parameter '{paramName}' not found on " +
                            $"rebar Id={rebar.Id} [{configKey}]. Skipped.");
                return false;
            }

            if (p.IsReadOnly)
            {
                results.Add($"WARN: Hook parameter '{paramName}' is read-only on " +
                            $"rebar Id={rebar.Id} [{configKey}]. Skipped.");
                return false;
            }

            try
            {
                p.Set(valueFt);
                return true;
            }
            catch (Exception ex)
            {
                results.Add($"ERROR: Setting hook '{paramName}' on " +
                            $"rebar Id={rebar.Id} [{configKey}]: {ex.Message}");
                return false;
            }
        }
    }

}
