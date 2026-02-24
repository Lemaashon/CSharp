using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSSE.Models
{
    /// <summary>
    /// Root object deserialized from the Lema JSON configuration file.
    ///
    /// Expected top-level JSON structure:
    /// <code>
    /// {
    ///   "ProjectInfo": { ... },
    ///   "BarInfo": {
    ///     "1 (Dir. X)": { "spacing": 150.0, "bartype": 12.0 },
    ///     "1 (Dir. Y)": { "spacing": 150.0, "bartype": 12.0 },
    ///     "2 (Dir. X)": { "spacing": 200.0, "bartype": 16.0 },
    ///     "2 (Dir. Y)": { "spacing": 200.0, "bartype": 16.0 },
    ///     "3 (Dir. X)": { "spacing": 200.0, "bartype": 16.0 },
    ///     "3 (Dir. Y)": { "spacing": 200.0, "bartype": 16.0 },
    ///     "4 (Dir. X)": { "spacing": 200.0, "bartype": 16.0 },
    ///     "4 (Dir. Y)": { "spacing": 200.0, "bartype": 16.0 },
    ///     "5":          { "spacing": 0.0,   "bartype": 8.0, "number": 16 },
    ///     "6":          { "spacing": 100.0, "bartype": 6.0 }
    ///   }
    /// }
    /// </code>
    ///
    /// The BarInfo dictionary key format is either:
    ///   - "N (Dir. X)" / "N (Dir. Y)"  → bar number + direction (bars 1–4)
    ///   - "N"                           → bar number only, no direction (bars 5, 6)
    ///
    /// RebarService.ParseBarInfo() handles splitting the key into its two parts.
    /// </summary>
    public class JsonRoot
    {
        /// <summary>
        /// General project and structural parameters.
        /// Feeds GlobalParameterService (Phase 4) and the stirrup calculations
        /// in RebarService (Phase 6).
        /// </summary>
        [JsonProperty("ProjectInfo")]
        public ProjectConfig ProjectInfo { get; set; } = new ProjectConfig();

        /// <summary>
        /// Per-bar spacing and diameter configuration.
        /// The dictionary key is the raw JSON key (e.g. "1 (Dir. X)").
        /// </summary>
        [JsonProperty("BarInfo")]
        public Dictionary<string, BarInfo> BarInfo { get; set; }
            = new Dictionary<string, BarInfo>();

    }
}
