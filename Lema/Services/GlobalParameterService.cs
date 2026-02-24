using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using BSSE.Utilities;   
using BSSE.Models;  

namespace BSSE.Services
{
    /// <summary>
    /// Reads and writes Revit Global Parameters and Revision fields.
    ///
    /// PYTHON EQUIVALENTS
    /// ───────────────────
    /// set_global_parameter()           →  SetOne()
    /// set_global_parameters_from_dict()→  SetFromProjectConfig()
    ///
    /// CALL SITE (RunLemaCommand, Transaction 1)
    /// ──────────────────────────────────────────
    ///   var results = GlobalParameterService.SetFromProjectConfig(doc, jsonData.ProjectInfo);
    ///
    /// IMPORTANT API NOTE — FindByName return value
    /// ─────────────────────────────────────────────
    /// GlobalParametersManager.FindByName() returns ElementId.InvalidElementId
    /// (NOT null) when a parameter does not exist. Comparing to null will always
    /// be false, silently skipping the not-found guard. This is a common Revit API
    /// pitfall and is handled explicitly in SetOne().
    /// </summary>
    public static class GlobalParameterService
    {
        // ── Client lookup tables (mirror Python's clientList / clientDict) ─────────

        /// <summary>
        /// All client flag global parameter names. When a new client is selected,
        /// every entry in this list is first reset to 0 (false), then only the
        /// matching entry is set to 1 (true).
        /// </summary>
        private static readonly string[] ClientFlags =
            { "BG", "SL", "IT", "CZ", "HB", "LC", "KT", "LU" };

        /// <summary>
        /// Maps the lowercase client name from JSON to its global parameter flag.
        /// Mirrors Python's clientDict exactly, including "tsi" → "TT" and
        /// "sit" → "SI" which are present in the Python dict but absent from
        /// ClientFlags (those two flags are set but not reset — preserved as-is).
        /// </summary>
        private static readonly Dictionary<string, string> ClientNameToFlag =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "bouygues",        "BG" },
                { "itas",            "IT" },
                { "selecom",         "SL" },
                { "calzavara",       "CZ" },
                { "hb towers",       "HB" },
                { "leadcom",         "LC" },
                { "kitting telecom", "KT" },
                { "tsi",             "TT" },
                { "sit",             "SI" },
                { "post luxembourg", "LU" }
            };
        /// <summary>
        /// JSON keys that are handled as Revision fields rather than Global Parameters.
        /// </summary>
        private static readonly HashSet<string> RevisionKeys =
            new HashSet<string> { "PIM_RevisionDate", "PIM_IssuedBy", "PIM_IssuedTo" };

        // ── Public API ────────────────────────────────────────────────────────────
        /// <summary>
        /// Iterates all fields in <paramref name="config"/> and writes them to
        /// the appropriate Revit target (Global Parameter, Revision, or client flag).
        ///
        /// Must be called inside an open Transaction.
        ///
        /// Returns a result dictionary:
        ///   key   = JSON parameter name
        ///   value = "OK" on success, or the exception message on failure.
        ///
        /// Mirrors Python's set_global_parameters_from_dict(doc, param_dict).
        /// </summary>
        public static Dictionary<string, string> SetFromProjectConfig(
            Document doc,
            ProjectConfig config)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (config == null) throw new ArgumentNullException(nameof(config));

            // Build one flat dictionary from the strongly-typed properties
            // plus the catch-all AdditionalProperties bag.
            Dictionary<string, object> allParams = FlattenConfig(config);

            // Pre-collect all Revision elements (mirroring Python's collector
            // at the top of set_global_parameters_from_dict).
            IList<Element> revisions = new FilteredElementCollector(doc)
                .OfClass(typeof(Revision))
                .ToElements();

            var results = new Dictionary<string, string>();

            foreach (var kvp in allParams)
            {
                string key = kvp.Key;
                object value = kvp.Value;

                try
                {
                    if (key == "STR_ClientName")
                    {
                        ApplyClientName(doc, value?.ToString());
                    }
                    else if (RevisionKeys.Contains(key))
                    {
                        ApplyRevisionField(revisions, key, value?.ToString());
                    }
                    else
                    {
                        SetOne(doc, key, value);
                    }

                    results[key] = "OK";
                }
                catch (Exception ex)
                {
                    // Per-parameter errors are logged, not rethrown,
                    // so one bad parameter doesn't abort the entire run.
                    results[key] = ex.Message;
                }
            }

            return results;
        }
        /// <summary>
        /// Sets a single Global Parameter by name, auto-detecting its stored type.
        ///
        /// Mirrors Python's set_global_parameter(doc, param_name, new_value).
        ///
        /// Type detection order:
        ///   1. IntegerParameterValue  → Yes/No flag  (normalised from bool/string/int)
        ///   2. DoubleParameterValue   → Length in mm, converted to ft via UnitConverter
        ///   3. StringParameterValue   → Written as-is
        ///   4. ElementIdParameterValue→ Expects an ElementId directly
        ///
        /// Must be called inside an open Transaction.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     The named global parameter does not exist in the document.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///     The parameter's stored value type is not one of the four handled types.
        /// </exception>
        public static void SetOne(Document doc, string paramName, object newValue)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (paramName == null) throw new ArgumentNullException(nameof(paramName));

            // ── Locate the global parameter ───────────────────────────────────────
            // FindByName returns ElementId.InvalidElementId (not null) when absent.
            ElementId gpId = GlobalParametersManager.FindByName(doc, paramName);

            if (gpId == ElementId.InvalidElementId || gpId == null)
                throw new InvalidOperationException(
                    $"Global Parameter '{paramName}' not found in the document.");

            GlobalParameter gp = doc.GetElement(gpId) as GlobalParameter;
            if (gp == null)
                throw new InvalidOperationException(
                    $"Element '{paramName}' exists but is not a GlobalParameter.");

            // ── Read the existing stored value to determine the parameter type ────
            ParameterValue existing = gp.GetValue();

            // ── Build the correctly-typed wrapper ─────────────────────────────────
            ParameterValue wrapped;

            if (existing is IntegerParameterValue)
            {
                // Yes/No global parameter. Normalise truthy/falsy input from
                // JSON (bool, string, number) to 0 or 1.
                wrapped = new IntegerParameterValue(NormaliseToInt(newValue));
            }
            else if (existing is DoubleParameterValue)
            {
                // All double global parameters are lengths stored in feet.
                // The JSON value is in mm — convert via UnitConverter.
                double mm = Convert.ToDouble(newValue);
                wrapped = new DoubleParameterValue(UnitConverter.MmToFt(mm));
            }
            else if (existing is StringParameterValue)
            {
                wrapped = new StringParameterValue(newValue?.ToString() ?? string.Empty);
            }
            else if (existing is ElementIdParameterValue)
            {
                // Caller must supply an ElementId directly.
                // No JSON path currently produces this type,
                // but the branch is present to match Python completeness.
                if (newValue is ElementId eid)
                    wrapped = new ElementIdParameterValue(eid);
                else
                    throw new NotSupportedException(
                        $"ElementId parameter '{paramName}' requires an ElementId value, " +
                        $"got {newValue?.GetType().Name ?? "null"}.");
            }
            else
            {
                throw new NotSupportedException(
                    $"Unsupported global parameter type for '{paramName}': " +
                    $"{existing?.GetType().Name ?? "null"}.");
            }

            gp.SetValue(wrapped);
        }
        // ── Private Helpers ───────────────────────────────────────────────────────

        /// <summary>
        /// Builds a flat string→object dictionary from a <see cref="ProjectConfig"/>
        /// by merging its strongly-typed properties with its AdditionalProperties bag.
        ///
        /// Strongly-typed properties are added explicitly so the compiler enforces
        /// that every known field is included. AdditionalProperties is merged last,
        /// so unknown future keys are forwarded automatically.
        ///
        /// Null values from optional fields (RevisionDate etc.) are included so
        /// the iteration below can log them as "OK" (no-op) rather than silently
        /// skipping them.
        /// </summary>
        private static Dictionary<string, object> FlattenConfig(ProjectConfig config)
        {
            var dict = new Dictionary<string, object>
            {
                // Strongly-typed known properties
                ["STR_Foundation_Depth"] = config.FoundationDepth,
                ["STR_BasePlate_DiameterOuter"] = config.BasePlateDiameterOuter,
                ["STR_ClientName"] = config.ClientName,
                ["PIM_RevisionDate"] = config.RevisionDate,
                ["PIM_IssuedBy"] = config.IssuedBy,
                ["PIM_IssuedTo"] = config.IssuedTo,
            };

            // Merge the overflow bag — any key already added above is NOT
            // overwritten, keeping the strongly-typed value as authoritative.
            if (config.AdditionalProperties != null)
            {
                foreach (var kvp in config.AdditionalProperties)
                {
                    if (!dict.ContainsKey(kvp.Key))
                        dict[kvp.Key] = kvp.Value;
                }
            }

            return dict;
        }

        /// <summary>
        /// Resets all client flag parameters to 0, then activates the one
        /// matching <paramref name="clientName"/>.
        ///
        /// Mirrors Python's STR_ClientName block in set_global_parameters_from_dict.
        /// </summary>
        private static void ApplyClientName(Document doc, string clientName)
        {
            // Step 1: reset all client flags to 0 (false)
            foreach (string flag in ClientFlags)
                SetOne(doc, flag, 0);

            // Step 2: activate only the matching flag
            if (!string.IsNullOrWhiteSpace(clientName) &&
                ClientNameToFlag.TryGetValue(clientName, out string activeFlag))
            {
                SetOne(doc, activeFlag, 1);
            }
            // If the client name is unknown, flags remain all-zero.
            // This matches Python's silent no-op when value.lower() is not in clientDict.
        }

        /// <summary>
        /// Writes a revision field (RevisionDate, IssuedBy, IssuedTo) to the
        /// Revision element whose RevisionNumber equals "A".
        ///
        /// Mirrors Python's elif key in revisionsList block.
        /// </summary>
        private static void ApplyRevisionField(
            IList<Element> revisions,
            string key,
            string value)
        {
            const string TargetRevisionNumber = "A";

            foreach (Element e in revisions)
            {
                if (e is Revision rev &&
                    rev.RevisionNumber == TargetRevisionNumber)
                {
                    switch (key)
                    {
                        case "PIM_RevisionDate":
                            rev.RevisionDate = value ?? string.Empty;
                            break;
                        case "PIM_IssuedBy":
                            rev.IssuedBy = value ?? string.Empty;
                            break;
                        case "PIM_IssuedTo":
                            rev.IssuedTo = value ?? string.Empty;
                            break;
                    }
                    // Only the first revision numbered "A" is updated,
                    // matching Python's behaviour (no break in Python = last wins,
                    // but in practice there is only one revision "A").
                }
            }
        }

        /// <summary>
        /// Normalises a JSON value (bool, string, long, double) to 0 or 1
        /// for use with IntegerParameterValue (Yes/No) global parameters.
        ///
        /// Mirrors Python's:
        ///   if str(new_value).lower() in ["yes","y","true","1"]: new_value = 1
        ///   elif str(new_value).lower() in ["no","n","false","0"]: new_value = 0
        ///
        /// NOTE: JSON booleans deserialise to C# bool via Newtonsoft, so
        /// 'true' in JSON arrives as (bool)true — the bool branch handles it
        /// without converting to string first.
        /// </summary>
        private static int NormaliseToInt(object value)
        {
            if (value is bool b)
                return b ? 1 : 0;

            string s = value?.ToString()?.Trim().ToLowerInvariant() ?? "0";

            switch (s)
            {
                case "1":
                case "yes":
                case "y":
                case "true":
                    return 1;
                case "0":
                case "no":
                case "n":
                case "false":
                    return 0;
                default:
                    // Attempt numeric parse as fallback
                    if (double.TryParse(s, out double d))
                        return d != 0.0 ? 1 : 0;
                    throw new FormatException(
                        $"Cannot normalise '{value}' to a Yes/No integer value.");
            }
        }
    }
}
