using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSSE.Utilities
{
    /// <summary>
    /// Converts between millimetres and Revit internal units (decimal feet).
    ///
    /// TARGET VERSIONS: Revit 2024, 2025, 2026 (API R24–R26).
    /// UnitTypeId is the only supported API from R22 onward — no compatibility
    /// guards or DisplayUnitType fallbacks are needed.
    ///
    /// Revit stores all length parameters internally as decimal feet.
    /// Every Parameter.AsDouble() returns feet; every Parameter.Set(double)
    /// expects feet. All service classes must convert through this class —
    /// no raw arithmetic or magic numbers (304.8) elsewhere in the codebase.
    /// </summary
    public static class UnitConverter
    {
        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Converts millimetres to Revit internal units (decimal feet).
        /// Use for all values being WRITTEN to Revit parameters:
        /// spacing, hook lengths, far clip offset, section box offsets.
        /// </summary>
        public static double MmToFt(double mm)
            => UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);

        /// <summary>
        /// Converts Revit internal units (decimal feet) to millimetres.
        /// Use when reading a Revit parameter value that needs to be compared
        /// against or logged as a millimetre figure (e.g. bounding box Z extents
        /// in GeometryHelper).
        /// </summary>
        public static double FtToMm(double ft)
            => UnitUtils.ConvertFromInternalUnits(ft, UnitTypeId.Millimeters);

        // ── Internal arithmetic overloads (unit-test use only) ────────────────────
        // UnitUtils requires a live Revit host and cannot be called from a plain
        // xUnit/NUnit test project. These internal methods bypass the API so that
        // conversion logic can be verified without spinning up Revit.

        /// <summary>Pure arithmetic mm → ft. For unit tests only.</summary>
        internal static double MmToFtArithmetic(double mm) => mm / 304.8;

        /// <summary>Pure arithmetic ft → mm. For unit tests only.</summary>
        internal static double FtToMmArithmetic(double ft) => ft * 304.8;

    }
}
