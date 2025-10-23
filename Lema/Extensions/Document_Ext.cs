using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lema.Extensions
{
    public static class Document_Ext
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static FilteredElementCollector Ext_Collector(this Document doc)
        {
            return new FilteredElementCollector(doc);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="view"></param>
        /// <returns></returns>
        public static FilteredElementCollector Ext_Collector(this Document doc,View view)
        {
            return new FilteredElementCollector(doc, view.Id);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="sorted"></param>
        /// <param name="includePlaceholders"></param>
        /// <returns></returns>
        public static List<ViewSheet> Ext_GetSheets(this Document doc, bool sorted = true, bool includePlaceholders=false)
        {
            // Collect our sheets
            var sheets = doc.Ext_Collector()
                .OfClass(typeof(ViewSheet))
                .Cast<ViewSheet>()
                .ToList();

            // Filter out placeholders if desired
            if (!includePlaceholders)
            {
                sheets = sheets
                    .Where(s=>!s.IsPlaceholder)
                    .ToList();
            }

            // Return elements, optional sorting
            if (sorted)
            {
                return sheets
                    .OrderBy(s => s.SheetNumber)
                    .ToList();
            }
            else
            {
                return sheets;  
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="sorted"></param>
        /// <returns></returns>
        public static List<Revision> Ext_GetRevisions(this Document doc, bool sorted = true)
        {
            // Collect our sheets
            var revisions = doc.Ext_Collector()
                .OfClass(typeof(Revision))
                .Cast<Revision>()
                .ToList();

            // Return elements, optional sorting
            if (sorted)
            {
                return revisions
                    .OrderBy(r => r.SequenceNumber)
                    .ToList();
            }
            else
            {
                return revisions;
            }
        }
    }
}
