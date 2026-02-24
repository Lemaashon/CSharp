using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSSE.Models
{
    /// <summary>
    /// Represents the "ProjectInfo" block of the Lema JSON file.
    ///
    /// Two categories of fields exist here:
    ///
    ///   A) Strongly-typed properties — fields whose names and types are known
    ///      and are used directly in the C# services (e.g. for stirrup calculations).
    ///
    ///   B) AdditionalProperties — every other key/value pair is captured here
    ///      and forwarded to GlobalParameterService as-is, so no code change is
    ///      needed when new global parameters are added to the JSON.
    ///
    /// JSON structure example:
    /// <code>
    /// "ProjectInfo": {
    ///     "STR_Foundation_Depth":       800.0,
    ///     "STR_BasePlate_DiameterOuter": 320.0,
    ///     "STR_ClientName":             "bouygues",
    ///     "PIM_RevisionDate":           "2024-01-15",
    ///     "PIM_IssuedBy":               "JD",
    ///     "PIM_IssuedTo":               "Client",
    ///     "MY_OTHER_PARAM":             42.0
    /// }
    /// </code>
    /// </summary>
    public class ProjectConfig
    {
        // ── Strongly-typed properties used by C# services ─────────────────────────

        /// <summary>
        /// Foundation depth in millimetres.
        /// Used by SectionBoxService (GeometryHelper.FindIndexByZExtent)
        /// and by RebarService (stirrup extents calculation).
        /// Maps to global parameter: STR_Foundation_Depth
        /// </summary>
        [JsonProperty("STR_Foundation_Depth")]
        public double FoundationDepth { get; set; }

        /// <summary>
        /// Outer diameter of the base plate in millimetres.
        /// Used by RebarService to compute STR_Stirrup_Extents.
        /// Maps to global parameter: STR_BasePlate_DiameterOuter
        /// </summary>
        [JsonProperty("STR_BasePlate_DiameterOuter")]
        public double BasePlateDiameterOuter { get; set; }

        /// <summary>
        /// Client name string (e.g. "bouygues", "selecom").
        /// GlobalParameterService uses the clientDict lookup to activate
        /// the matching Yes/No global parameter flag.
        /// Maps to global parameter key: STR_ClientName
        /// </summary>
        [JsonProperty("STR_ClientName")]
        public string ClientName { get; set; }

        // ── Revision fields ───────────────────────────────────────────────────────

        /// <summary>Maps to Revision.RevisionDate on revision "A".</summary>
        [JsonProperty("PIM_RevisionDate")]
        public string RevisionDate { get; set; }

        /// <summary>Maps to Revision.IssuedBy on revision "A".</summary>
        [JsonProperty("PIM_IssuedBy")]
        public string IssuedBy { get; set; }

        /// <summary>Maps to Revision.IssuedTo on revision "A".</summary>
        [JsonProperty("PIM_IssuedTo")]
        public string IssuedTo { get; set; }

        // ── Overflow bucket for all other global parameters ───────────────────────

        /// <summary>
        /// Catches every JSON key that does not match a declared property above.
        /// GlobalParameterService iterates this dictionary to set the remaining
        /// global parameters without needing a code change per new parameter.
        ///
        /// Values are stored as <c>object</c> because Newtonsoft.Json will
        /// deserialize numbers as <c>long</c> or <c>double</c> and strings as
        /// <c>string</c> — GlobalParameterService handles the type switch.
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object> AdditionalProperties { get; set; }
            = new Dictionary<string, object>();
    }
}
