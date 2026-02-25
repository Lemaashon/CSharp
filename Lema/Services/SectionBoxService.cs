using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BSSE.Utilities; 
using BSSE.Models;

namespace BSSE.Services
{
    /// <summary>
    /// Computes and applies section boxes to the four foundation detail views.
    ///
    /// PYTHON EQUIVALENT: set_foundation_section_box(process, projectinfo)
    ///
    /// CALL SITE (RunLemaCommand, Transaction 4)
    /// ──────────────────────────────────────────
    ///   var results = SectionBoxService.SetFoundationSectionBox(doc, jsonData.ProjectInfo);
    ///
    /// WHAT IT DOES
    /// ─────────────
    /// 1. Locates the "BSSe Foundation Monobloc" structural foundation instance.
    /// 2. Traverses its geometry to collect the bounding-box Min/Max of every Solid.
    /// 3. Uses GeometryHelper.FindIndexByZExtent to identify the solid whose
    ///    Z height matches the STR_Foundation_Depth from the JSON.
    /// 4. Computes section box corners for four named 3D views using the
    ///    Detail A / Detail B offset formulae from the Python script.
    /// 5. Activates the section box on each view and writes the computed extents.
    ///
    /// PYTHON BUGS FIXED
    /// ──────────────────
    /// 1. transform SCOPING BUG (Python line 598–605):
    ///    'transform' is assigned inside "if isinstance(obj, GeometryInstance)"
    ///    but consumed inside "if isinstance(obj, Solid)". If a Solid appears
    ///    before any GeometryInstance in the iteration, Python throws NameError.
    ///    Fixed: transform is initialised to Transform.Identity before the loop.
    ///
    /// 2. SHALLOW GEOMETRY TRAVERSAL:
    ///    Python iterates geom directly and assumes Solids and GeometryInstances
    ///    are siblings at the top level. For FamilyInstance geometry, Solids
    ///    live inside GeometryInstance objects. Fixed: for each GeometryInstance
    ///    encountered, GetInstanceGeometry() is called to retrieve its children.
    ///
    /// 3. View3D CAST MISSING:
    ///    IsSectionBoxActive / GetSectionBox / SetSectionBox only exist on View3D.
    ///    Python never casts; in C# this is a compile-time requirement.
    ///
    /// 4. LOG OUTSIDE LOOP (Python line 635):
    ///    results.append is at the same indent as the 'for view in views' loop —
    ///    it executes once after the loop ends, logging only the last view.
    ///    Fixed: logging occurs inside the loop, once per view.
    ///
    /// 5. index == -1 GUARD:
    ///    GeometryHelper returns -1 when no solid matches the foundation depth.
    ///    Python returned None and would crash on minimums[None].
    ///    Fixed: explicit guard throws with a clear message.
    ///
    /// 6. XYZ OFFSET VALUES ARE IN FEET — NOT MILLIMETRES:
    ///    The literal values (0, 3, 2.4, 10, etc.) in the offset XYZ
    ///    constructors are Revit internal units (decimal feet). They must NOT
    ///    be passed through UnitConverter. This is correct and intentional.
    /// </summary>

    public static class SectionBoxService
    {
        // ── Constants ─────────────────────────────────────────────────────────────

        private const string FoundationFamilyName = "BSSe Foundation Monobloc";

        /// <summary>
        /// The four 3D detail views that receive a section box.
        /// The string value drives the offset formula selection:
        ///   contains "detail a" (case-insensitive) → Detail A formula
        ///   contains "detail b" (case-insensitive) → Detail B formula
        /// </summary>
        private static readonly string[] DetailViewNames =
        {
            "DETAIL A S.02",
            "DETAIL B S.02",
            "DETAIL A S.03",
            "DETAIL B S.03"
        };
        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Applies section boxes to all four foundation detail views.
        /// Must be called inside an open Transaction.
        /// </summary>
        /// <param name="doc">The active Revit document.</param>
        /// <param name="projectConfig">
        ///     Source of STR_Foundation_Depth (mm), used to identify the
        ///     correct solid bounding box via GeometryHelper.
        /// </param>
        /// <returns>
        ///     List of result strings — one per view, prefixed "OK" or "ERROR".
        /// </returns>
        public static List<string> SetFoundationSectionBox(
            Document doc,
            ProjectConfig projectConfig)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (projectConfig == null) throw new ArgumentNullException(nameof(projectConfig));

            var results = new List<string>();

            // ── Step 1: Locate the foundation instance ────────────────────────────
            // Python: UnwrapElement(get_element_by_name(..., element_class="Foundation"))
            // UnwrapElement is Dynamo-only — not needed in a native C# add-in.
            Element foundationElement;
            try
            {
                foundationElement = ElementFinder.GetByName(
                    doc,
                    FoundationFamilyName,
                    ElementFinder.ElementScope.Foundation);
            }
            catch (Exception ex)
            {
                results.Add($"ERROR: Could not locate foundation '{FoundationFamilyName}': {ex.Message}");
                return results;
            }

            // ── Step 2: Traverse geometry to collect solid bounding boxes ─────────
            var minimums = new List<XYZ>();
            var maximums = new List<XYZ>();

            try
            {
                CollectSolidBoundingBoxes(foundationElement, minimums, maximums);
            }
            catch (Exception ex)
            {
                results.Add($"ERROR: Failed to read foundation geometry: {ex.Message}");
                return results;
            }

            if (minimums.Count == 0)
            {
                results.Add("ERROR: No solid geometry found in foundation element. " +
                            "Verify the foundation family contains solids.");
                return results;
            }

            // ── Step 3: Identify which solid matches the foundation depth ─────────
            // Python: index = find_index_by_z_extent(minimums, maximums, foundation_depth)
            int index = GeometryHelper.FindIndexByZExtent(
                minimums,
                maximums,
                foundationDepthMm: projectConfig.FoundationDepth,
                toleranceMm: 10.0);

            if (index < 0)
            {
                results.Add($"ERROR: No solid with Z extent matching " +
                            $"foundation depth {projectConfig.FoundationDepth}mm " +
                            $"(±10mm tolerance) was found among {minimums.Count} solid(s). " +
                            $"Check STR_Foundation_Depth in the JSON.");
                return results;
            }

            XYZ selectedMin = minimums[index];
            XYZ selectedMax = maximums[index];

            // ── Step 4: Apply section box to each detail view ─────────────────────
            foreach (string viewName in DetailViewNames)
            {
                string viewResult = ApplySectionBoxToView(
                    doc, viewName, selectedMin, selectedMax);

                results.Add(viewResult);
            }

            return results;
        }
        // ── Private: geometry traversal ───────────────────────────────────────────

        /// <summary>
        /// Traverses the foundation element's geometry and populates
        /// <paramref name="minimums"/> and <paramref name="maximums"/> with the
        /// transformed bounding-box corners of every Solid found.
        ///
        /// PYTHON BUG FIXED — shallow traversal:
        /// Python iterates the top-level GeometryElement and assumes Solids appear
        /// directly at that level. For a FamilyInstance, Solids live inside
        /// GeometryInstance objects. This method calls GetInstanceGeometry() on
        /// each GeometryInstance to properly descend into the family geometry.
        ///
        /// PYTHON BUG FIXED — transform scoping:
        /// Python assigns 'transform' inside the GeometryInstance branch but uses
        /// it in the Solid branch with no initialisation. If a Solid appears first,
        /// Python throws NameError. C# initialises to Transform.Identity.
        /// </summary>
        private static void CollectSolidBoundingBoxes(
            Element element,
            List<XYZ> minimums,
            List<XYZ> maximums)
        {
            var options = new Options
            {
                ComputeReferences = false,
                DetailLevel = ViewDetailLevel.Fine
            };

            GeometryElement geom = element.get_Geometry(options);
            if (geom == null) return;

            // Initialise transform to Identity in case Solids appear before
            // any GeometryInstance in the iteration (fixes Python NameError bug).
            Transform currentTransform = Transform.Identity;

            foreach (GeometryObject obj in geom)
            {
                if (obj is GeometryInstance gi)
                {
                    // Capture the instance-level transform, then descend into
                    // the instance geometry to find the Solids within.
                    currentTransform = gi.Transform;

                    foreach (GeometryObject innerObj in gi.GetInstanceGeometry())
                    {
                        if (innerObj is Solid innerSolid && innerSolid.Volume > 0)
                            AddTransformedBounds(innerSolid, currentTransform,
                                                 minimums, maximums);
                    }
                }
                else if (obj is Solid topSolid && topSolid.Volume > 0)
                {
                    // Top-level Solid (non-family or in-place family).
                    // Uses currentTransform which is Identity unless a
                    // GeometryInstance was seen first.
                    AddTransformedBounds(topSolid, currentTransform,
                                         minimums, maximums);
                }
            }
        }
        /// <summary>
        /// Applies the instance transform to a solid's axis-aligned bounding box
        /// and adds the resulting Min/Max points to the accumulator lists.
        ///
        /// PYTHON EQUIVALENT (lines 603–605):
        ///   solid_bbox = obj.GetBoundingBox()
        ///   minimums.append(transform.OfPoint(solid_bbox.Min))
        ///   maximums.append(transform.OfPoint(solid_bbox.Max))
        ///
        /// Volume check (> 0) skips degenerate zero-volume Solids that can appear
        /// from void geometry or sketch artefacts — the Python has no such guard.
        /// </summary>
        private static void AddTransformedBounds(
            Solid solid,
            Transform transform,
            List<XYZ> minimums,
            List<XYZ> maximums)
        {
            BoundingBoxXYZ bbox = solid.GetBoundingBox();
            if (bbox == null) return;

            minimums.Add(transform.OfPoint(bbox.Min));
            maximums.Add(transform.OfPoint(bbox.Max));
        }

        // ── Private: section box application ─────────────────────────────────────

        /// <summary>
        /// Computes the section box extents for one view and writes them.
        ///
        /// OFFSET FORMULAE (preserved exactly from Python, values in feet):
        ///
        ///   Detail A:
        ///     offset_min = XYZ(0,  0,  -10)
        ///     offset_max = XYZ(3,  3,  2.4 - selectedMax.Z)
        ///
        ///   Detail B:
        ///     offset_min = XYZ(0,  0,  -selectedMin.Z - 2.4)
        ///     offset_max = XYZ(3,  3,   selectedMax.Z)
        ///
        /// These offsets are added to the foundation corner (selectedMin.X/Y/Z)
        /// to produce the absolute section box min/max coordinates.
        ///
        /// NOTE: All XYZ literal values here are in Revit internal units (feet).
        /// Do NOT route them through UnitConverter.
        ///
        /// PYTHON BUG FIXED — View3D cast:
        ///   Python used revitView.IsSectionBoxActive without casting.
        ///   IsSectionBoxActive / GetSectionBox / SetSectionBox only exist on View3D.
        ///
        /// PYTHON BUG FIXED — log inside loop:
        ///   Python's results.append on line 635 is outside the view loop (wrong
        ///   indentation) — only the last view's values were ever logged.
        /// </summary>
        private static string ApplySectionBoxToView(
            Document doc,
            string viewName,
            XYZ selectedMin,
            XYZ selectedMax)
        {
            // ── Locate the 3D view ────────────────────────────────────────────────
            View3D view3D;
            try
            {
                Element viewElement = ElementFinder.GetByName(
                    doc, viewName, ElementFinder.ElementScope.View);

                view3D = viewElement as View3D;
                if (view3D == null)
                    return $"ERROR: '{viewName}' was found but is not a View3D. " +
                           $"Section boxes require a 3D view.";
            }
            catch (Exception ex)
            {
                return $"ERROR: Could not find view '{viewName}': {ex.Message}";
            }

            // ── Compute offsets (Detail A vs Detail B) ────────────────────────────
            // Python: "Detail A".lower() in view.lower()
            bool isDetailA = viewName.IndexOf("detail a", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isDetailB = viewName.IndexOf("detail b", StringComparison.OrdinalIgnoreCase) >= 0;

            XYZ offsetMin, offsetMax;

            if (isDetailA)
            {
                // Python: offset_min = XYZ(0, 0, -10)
                //         offset_max = XYZ(3, 3, (2.4 - maximums[index].Z))
                offsetMin = new XYZ(0, 0, -10);
                offsetMax = new XYZ(3, 3, 2.4 - selectedMax.Z);
            }
            else if (isDetailB)
            {
                // Python: offset_min = XYZ(0, 0, -minimums[index].Z - 2.4)
                //         offset_max = XYZ(3, 3,  maximums[index].Z)
                offsetMin = new XYZ(0, 0, -selectedMin.Z - 2.4);
                offsetMax = new XYZ(3, 3, selectedMax.Z);
            }
            else
            {
                return $"ERROR: View '{viewName}' does not contain 'Detail A' or " +
                       $"'Detail B' — cannot determine offset formula.";
            }

            // ── Compute absolute section box corners ──────────────────────────────
            // Python: corner = XYZ(minimums[index].X, minimums[index].Y, minimums[index].Z)
            //         new_min = offset_min.Add(corner)
            //         new_max = offset_max.Add(corner)
            XYZ corner = new XYZ(selectedMin.X, selectedMin.Y, selectedMin.Z);
            XYZ newMin = offsetMin.Add(corner);
            XYZ newMax = offsetMax.Add(corner);

            // ── Validate: min must be less than max on every axis ─────────────────
            // If the formula produces an inverted box, Revit will throw.
            // Log a useful error rather than letting Revit crash the transaction.
            if (newMin.X >= newMax.X || newMin.Y >= newMax.Y || newMin.Z >= newMax.Z)
            {
                return $"ERROR: Computed section box for '{viewName}' is inverted or zero: " +
                       $"Min=({newMin.X:F3}, {newMin.Y:F3}, {newMin.Z:F3}) " +
                       $"Max=({newMax.X:F3}, {newMax.Y:F3}, {newMax.Z:F3}). " +
                       $"Check foundation geometry and depth value.";
            }

            // ── Apply section box ─────────────────────────────────────────────────
            try
            {
                // Activate section box if not already on
                // Python: if not revitView.IsSectionBoxActive: revitView.IsSectionBoxActive = True
                if (!view3D.IsSectionBoxActive)
                    view3D.IsSectionBoxActive = true;

                // Get the current section box, overwrite Min/Max, reset transform,
                // then push it back. This mirrors the Python pattern exactly.
                // Python: section = revitView.GetSectionBox()
                //         section.Transform = Transform.Identity
                //         section.Min = new_min
                //         section.Max = new_max
                //         revitView.SetSectionBox(section)
                BoundingBoxXYZ sectionBox = view3D.GetSectionBox();
                sectionBox.Transform = Transform.Identity;
                sectionBox.Min = newMin;
                sectionBox.Max = newMax;
                view3D.SetSectionBox(sectionBox);

                return $"OK: '{viewName}' section box set — " +
                       $"Min=({newMin.X:F3}, {newMin.Y:F3}, {newMin.Z:F3}) ft, " +
                       $"Max=({newMax.X:F3}, {newMax.Y:F3}, {newMax.Z:F3}) ft.";
            }
            catch (Exception ex)
            {
                return $"ERROR: Failed to set section box on '{viewName}': {ex.Message}";
            }
        }

    }
}
