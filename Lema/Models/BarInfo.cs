using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSSE.Models
{
    /// <summary>
    /// Represents one bar configuration entry from the JSON "BarInfo" object.
    ///
    /// JSON structure example:
    /// <code>
    /// "1 (Dir. X)": {
    ///     "spacing": 150.0,
    ///     "bartype": 12.0,
    ///     "number":  null
    /// }
    /// </code>
    ///
    /// The dictionary key (e.g. "1 (Dir. X)") is parsed separately by
    /// RebarService.ParseBarInfo() into a bar number and an optional direction.
    /// </summary>

    public class BarInfo
    {
        // ── Required fields ───────────────────────────────────────────────────────

        /// <summary>
        /// Centre-to-centre spacing in millimetres.
        /// Converted to feet before being written to the Revit parameter.
        /// </summary>
        [JsonProperty("spacing")]
        public double Spacing { get; set; }

        /// <summary>
        /// Bar diameter in millimetres (e.g. 12.0 for Ø12).
        /// Used both to select the matching Rebar Type by name and
        /// to calculate hook lengths for bars 3 and 4.
        /// </summary>
        [JsonProperty("bartype")]
        public double BarType { get; set; }

        // ── Optional fields ───────────────────────────────────────────────────────

        /// <summary>
        /// Fixed quantity of bars. Only present for bar key "5" (stirrups).
        /// When set, the spacing is derived from the stirrup perimeter rather than
        /// read directly from this value.
        /// Null for all standard spacing-based bars.
        /// </summary>
        [JsonProperty("number")]
        public double? Number { get; set; }

    }
}
