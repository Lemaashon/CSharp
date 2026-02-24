using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using View = Autodesk.Revit.DB.View;

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
        /// <summary>
        /// Get the ProjectInfo element from the document
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static ProjectInfo Ext_GetProjectInfo(this Document doc)
        {
            // Collect the ProjectInfo element
            var projectInfo = new FilteredElementCollector(doc)
                .OfClass(typeof(ProjectInfo))
                .Cast<ProjectInfo>()
                .FirstOrDefault();

            return projectInfo;
        }
        public static Dictionary<string, object> GetParameters(this ProjectInfo projectInfo, List<string> paramNames)
        {
            var result = new Dictionary<string, object>();

            foreach (var paramName in paramNames)
            {
                Parameter param = projectInfo.LookupParameter(paramName);
                if (param == null)
                {
                    result[paramName] = null; // Parameter not found
                    continue;
                }

                switch (param.StorageType)
                {
                    case StorageType.Integer:
                        result[paramName] = param.AsInteger();
                        break;
                    case StorageType.Double:
                        result[paramName] = param.AsDouble();
                        break;
                    case StorageType.String:
                        result[paramName] = param.AsString();
                        break;
                    case StorageType.ElementId:
                        result[paramName] = param.AsElementId();
                        break;
                    default:
                        result[paramName] = null;
                        break;
                }
            }

            return result;
        }


    }
}
