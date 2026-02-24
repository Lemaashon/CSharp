using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSSE.Utilities
{
    /// <summary>
    /// Geometry utilities used by SectionBoxService when computing which
    /// solid bounding box in a foundation element matches the known foundation depth.
    ///
    /// WHY THIS EXISTS
    /// ───────────────
    /// The foundation family ("BSSe Foundation Monobloc") is a compound solid:
    /// it contains multiple solids at different Z levels (pedestal, footing slab,
    /// possibly a socket). SectionBoxService collects the Min/Max XYZ of every
    /// solid in the geometry, then calls FindIndexByZExtent to identify which
    /// solid's bounding box height matches the foundation depth from the JSON.
    ///
    /// PYTHON EQUIVALENCE
    /// ──────────────────
    /// Directly mirrors find_index_by_z_extent() from Compiled.py.
    /// The only difference is that Z conversion from feet to mm is now delegated
    /// to UnitConverter.FtToMm() instead of the inline (* 304.8) literal.
    /// </summary>
    public static class GeometryHelper
    {
        /// <summary>
        /// Scans pairs of bounding-box Min/Max points and returns the index of the
        /// pair whose Z extent (height) matches <paramref name="foundationDepthMm"/>
        /// within <paramref name="toleranceMm"/>.
        ///
        /// BEHAVIOUR NOTES
        /// ───────────────
        /// - The lists are walked in order; if multiple solids match, the LAST
        ///   matching index is returned (matches Python's assignment: index=i with
        ///   no early break, so the last match wins).
        /// - XYZ coordinates are in Revit internal units (feet). The method
        ///   converts to mm internally before comparing against the mm inputs.
        /// - Returns -1 (not null as in Python) when no solid matches, so callers
        ///   can check without a nullable dereference.
        ///
        /// CALLED BY
        /// ─────────
        /// SectionBoxService.SetFoundationSectionBox (Phase 8).
        /// </summary>
        /// <param name="minimums">
        ///     Bounding-box Min points in Revit internal units (feet),
        ///     one per solid. Collected from Solid.GetBoundingBox().Min
        ///     after applying the GeometryInstance transform.
        /// </param>
        /// <param name="maximums">
        ///     Bounding-box Max points in Revit internal units (feet),
        ///     one per solid. Must be the same length as <paramref name="minimums"/>.
        /// </param>
        /// <param name="foundationDepthMm">
        ///     The expected height of the target solid in millimetres.
        ///     Sourced from ProjectConfig.FoundationDepth (JSON key "STR_Foundation_Depth").
        /// </param>
        /// <param name="toleranceMm">
        ///     Allowed deviation in millimetres between the measured Z extent
        ///     and <paramref name="foundationDepthMm"/>. Defaults to 10 mm.
        /// </param>
        /// <returns>
        ///     Zero-based index of the matching solid pair, or -1 if none matched.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///     Thrown if <paramref name="minimums"/> and <paramref name="maximums"/>
        ///     have different lengths.
        /// </exception>
        public static int FindIndexByZExtent(
            IList<XYZ> minimums,
            IList<XYZ> maximums,
            double foundationDepthMm,
            double toleranceMm = 10.0)
        {
            if (minimums == null) throw new ArgumentNullException(nameof(minimums));
            if (maximums == null) throw new ArgumentNullException(nameof(maximums));

            if (minimums.Count != maximums.Count)
                throw new ArgumentException(
                    $"minimums ({minimums.Count}) and maximums ({maximums.Count}) " +
                    "must have the same length.");

            int matchIndex = -1;   // -1 = "not found" (Python returned None)

            for (int i = 0; i < minimums.Count; i++)
            {
                // Convert the Z extent from Revit internal feet to millimetres.
                // Python: z_extent = abs(max_z - min_z) * 304.8
                double zExtentMm = UnitConverter.FtToMm(Math.Abs(maximums[i].Z - minimums[i].Z));

                // Python: if abs(z_extent - foundation_depth) < tolerance
                if (Math.Abs(zExtentMm - foundationDepthMm) < toleranceMm)
                    matchIndex = i;   // intentionally no break — last match wins
            }

            return matchIndex;
        }
        }
}
