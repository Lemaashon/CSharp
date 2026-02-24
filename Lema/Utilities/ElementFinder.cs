using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSSE.Utilities
{
    /// <summary>
    /// Locates Revit elements by name using FilteredElementCollector.
    ///
    /// PYTHON EQUIVALENT
    /// ─────────────────
    /// get_element_by_name(doc, name, element_class=None)  →  GetByName()
    /// get_all_rebar_sets()                                →  GetAllRebars()
    ///
    /// DESIGN CHANGES FROM PYTHON
    /// ───────────────────────────
    /// The Python function accepted a string for element_class ("View",
    /// "Foundation"). C# uses the <see cref="LemaElementScope"/> enum instead:
    ///   - Type-safe: the compiler catches typos, not Revit at runtime.
    ///   - Extensible: add a new scope value without touching callers.
    ///
    /// COLLECTOR STRATEGY (matches Python exactly)
    /// ────────────────────────────────────────────
    /// View scope      → OfClass(typeof(View))
    ///                   Catches all View subclasses: ViewPlan, ViewSection,
    ///                   View3D, etc. The Python used OfClass(View) identically.
    ///
    /// Foundation scope → OfCategory(OST_StructuralFoundation)
    ///                    + WhereElementIsNotElementType()
    ///                    The Python used this exact collector chain.
    ///                    Note: OfClass(FamilyInstance) is NOT used here because
    ///                    structural foundations can be non-family system families
    ///                    in some Revit configurations; category filter is safer.
    ///
    /// Rebar scope     → OfCategory(OST_Rebar) + WhereElementIsNotElementType()
    /// </summary>
    public static class ElementFinder
    {
        // ── Scope Enum ────────────────────────────────────────────────────────────

        /// <summary>
        /// Defines the collector strategy used when searching for a named element.
        /// Pass this to <see cref="GetByName"/> to control how the search is scoped.
        /// </summary>
        public enum ElementScope
        {
            /// <summary>
            /// Searches all View subclasses (ViewPlan, ViewSection, View3D …).
            /// Used when looking up section views, plan views, or 3D views by name.
            /// </summary>
            View,

            /// <summary>
            /// Searches structural foundation instances by category
            /// (OST_StructuralFoundation), excluding element types.
            /// Used when looking up the foundation family instance by name.
            /// </summary>
            Foundation
        }
        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the first Revit element whose <c>Name</c> property matches
        /// <paramref name="name"/> exactly (case-sensitive, matches Python behaviour).
        /// </summary>
        /// <param name="doc">The active Revit document.</param>
        /// <param name="name">
        ///     The exact element name to search for, e.g. "DETAIL A S.02"
        ///     or "BSSe Foundation Monobloc".
        /// </param>
        /// <param name="scope">
        ///     Controls which collector strategy is used.
        ///     See <see cref="LemaElementScope"/> for options.
        /// </param>
        /// <returns>The first matching <see cref="Element"/>.</returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="doc"/> or <paramref name="name"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     No element with the given name was found within the given scope.
        ///     Mirrors Python's: raise Exception(f"Could not find element named '{name}'")
        /// </exception>
        public static Element GetByName(Document doc, string name, ElementScope scope)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (name == null) throw new ArgumentNullException(nameof(name));

            IList<Element> candidates = BuildCollector(doc, scope).ToElements();

            foreach (Element e in candidates)
            {
                // Python: if hasattr(e, "Name") and e.Name == name
                // In C# all Element subclasses expose Name, but it can be null
                // for elements that haven't been named, so guard against that.
                if (e.Name != null && e.Name == name)
                    return e;
            }

            // Mirror the Python exception message exactly so existing log
            // parsing tools continue to work.
            throw new InvalidOperationException($"Could not find element named '{name}'");
        }
        /// <summary>
        /// Returns all rebar instances in the document, excluding rebar types.
        ///
        /// PYTHON EQUIVALENT: get_all_rebar_sets()
        /// The Python name is kept in the XML doc so it is easy to cross-reference.
        ///
        /// NOTE: Returns <see cref="IList{T}"/> rather than IEnumerable so that
        /// callers can check .Count before iterating without re-enumerating the
        /// collector.
        /// </summary>
        /// <param name="doc">The active Revit document.</param>
        /// <returns>
        ///     All rebar element instances. Empty list (never null) if none exist.
        /// </returns>
        public static IList<Element> GetAllRebars(Document doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            return new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Rebar)
                .WhereElementIsNotElementType()
                .ToElements();
        }

        /// <summary>
        /// Returns all rebar TYPE elements (used by RebarService to match
        /// the target rebar type by name before calling ChangeTypeId).
        /// </summary>
        /// <param name="doc">The active Revit document.</param>
        /// <returns>All rebar type elements. Empty list if none exist.</returns>

        public static IList<Element> GetAllRebarTypes(Document doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            return new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Rebar)
                .WhereElementIsElementType()
                .ToElements();
        }
        // ── Private Helpers ───────────────────────────────────────────────────────

        /// <summary>
        /// Builds the appropriate <see cref="FilteredElementCollector"/> for the
        /// requested <paramref name="scope"/>. Mirrors Python's if/elif chain.
        /// </summary>
        /// 
        private static FilteredElementCollector BuildCollector(Document doc, ElementScope scope)
        {
            switch (scope)
            {
                case ElementScope.View:
                    // OfClass(View) catches all concrete View subclasses because
                    // Revit's collector treats View as the abstract base and returns
                    // all ViewPlan, ViewSection, View3D etc. instances.
                    return new FilteredElementCollector(doc)
                        .OfClass(typeof(Autodesk.Revit.DB.View));

                case ElementScope.Foundation:
                    // Category filter is used (not OfClass) to match the Python
                    // exactly and to handle both family-based and system foundations.
                    return new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_StructuralFoundation)
                        .WhereElementIsNotElementType();

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(scope), scope, "Unhandled LemaElementScope value.");
            }
        }

    }
}
