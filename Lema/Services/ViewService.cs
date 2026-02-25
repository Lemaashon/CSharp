using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using BSSE.Utilities;

namespace BSSE.Services
{
    /// <summary>
    /// Sets view-level properties such as the Far Clip Offset of section views.
    ///
    /// PYTHON EQUIVALENT: set_section_view_depth(view_name, depth_mm)
    ///
    /// CALL SITE
    /// ─────────
    /// Called internally by RebarService.UpdateFromConfig() (Phase 6) after
    /// computing the stirrup extents. NOT called directly from RunLemaCommand.
    ///
    ///   viewDepth = STR_Stirrup_Extents / 2 + 50   ← already in mm
    ///   ViewService.SetFarClipOffset(doc, "COUPE 1-1 B", viewDepth);
    ///
    /// PYTHON BUGS FIXED
    /// ──────────────────
    /// 1. Line 161 in Compiled.py: "return result.append(...)" uses the undefined
    ///    name 'result' (missing 's') and list.append() returns None, so the
    ///    function silently returns None when the view is not found.
    ///    Fixed: throws InvalidOperationException with a clear message.
    ///
    /// 2. LookupParameter("Far Clip Offset") is locale-dependent — a French or
    ///    German Revit installation uses a translated parameter name.
    ///    Fixed: BuiltInParameter.VIEWER_BOUND_OFFSET_FAR is locale-independent.
    ///    A name-based fallback is kept for any edge case where the BIP is absent.
    /// </summary>
    public static class ViewService
    {
        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Sets the Far Clip Offset of the named section view.
        ///
        /// Must be called inside an open Transaction.
        /// </summary>
        /// <param name="doc">The active Revit document.</param>
        /// <param name="viewName">
        ///     Exact name of the target section view, e.g. "COUPE 1-1 B".
        ///     The match is case-sensitive, consistent with Python behaviour.
        /// </param>
        /// <param name="depthMm">
        ///     Desired far clip depth in millimetres.
        ///     Converted to Revit internal units (feet) before writing.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="doc"/> or <paramref name="viewName"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     No ViewSection with the given name exists in the document, or the
        ///     Far Clip Offset parameter is read-only on that view.
        /// </exception>
        /// 
        public static void SetFarClipOffset(Document doc, string viewName, double depthMm)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (viewName == null) throw new ArgumentNullException(nameof(viewName));

            // ── 1. Locate the ViewSection ─────────────────────────────────────────
            // Python used OfCategory(OST_Views).OfClass(ViewSection).
            // In C#, OfClass(typeof(ViewSection)) alone is sufficient and faster —
            // ViewSection is a concrete Revit API class, so category redundancy
            // is unnecessary and chaining OfCategory + OfClass on the same
            // collector is not recommended (only one quick filter per collector).
            ViewSection section = FindViewSection(doc, viewName);

            if (section == null)
            {
                // Python bug fixed: was "return result.append(...)" — undefined
                // 'result' variable returned None silently. We throw instead.
                throw new InvalidOperationException(
                    $"Section view '{viewName}' not found in the document.");
            }

            // ── 2. Resolve the Far Clip Offset parameter ──────────────────────────
            // Prefer the BuiltInParameter (locale-independent).
            // Fall back to name-based lookup in case the BIP is unavailable on
            // an unusual view type or Revit configuration.
            Parameter farClip = section.get_Parameter(
                BuiltInParameter.VIEWER_BOUND_OFFSET_FAR);

            if (farClip == null || farClip.StorageType == StorageType.None)
                farClip = section.LookupParameter("Far Clip Offset");

            // ── 3. Guard read-only state ──────────────────────────────────────────
            // Python: if farClipOffset and not farClipOffset.IsReadOnly
            if (farClip == null)
                throw new InvalidOperationException(
                    $"Far Clip Offset parameter not found on view '{viewName}'.");

            if (farClip.IsReadOnly)
                throw new InvalidOperationException(
                    $"Far Clip Offset parameter is read-only on view '{viewName}'. " +
                    "Ensure 'Far Clipping' is set to 'Clip with line' in view properties.");

            // ── 4. Convert and write ──────────────────────────────────────────────
            // Python: depth_feet = depth_mm / 304.8  (raw arithmetic)
            // C#: route through UnitConverter — no magic numbers in service code.
            farClip.Set(UnitConverter.MmToFt(depthMm));
        }
        // ── Private Helpers ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns the first <see cref="ViewSection"/> whose Name matches
        /// <paramref name="viewName"/> exactly, or null if not found.
        ///
        /// Uses OfClass(ViewSection) which is equivalent to Python's
        /// OfCategory(OST_Views).OfClass(ViewSection) but without the redundant
        /// category filter.
        /// </summary>
        private static ViewSection FindViewSection(Document doc, string viewName)
        {
            using (var collector = new FilteredElementCollector(doc))
            {
                foreach (Element e in collector.OfClass(typeof(ViewSection)))
                {
                    if (e.Name == viewName)
                        return e as ViewSection;
                }
            }
            return null;
        }

    }
}
