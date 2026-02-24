using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Form = System.Windows.Forms.Form;
using System.Drawing;
using ClosedXML.Excel;
using Autodesk.Revit.UI;

namespace Lema._Utilities
{
    public static class File_Utils
    {
        /// <summary>
        /// Sets the icon of the specified form using the provided icon path or a default icon.
        /// </summary>
        /// <remarks>If the <paramref name="iconPath"/> is null, the method uses a default icon embedded
        /// in the assembly. The method attempts to retrieve the icon from the specified path as an embedded
        /// resource.</remarks>
        /// <param name="form">The form whose icon is to be set. Cannot be null.</param>
        /// <param name="iconPath">The path to the icon resource. If null, a default icon is used.</param>
        public static void SetFormIcon(Form form, string iconPath = null)
        {
            iconPath ??= "Lema.Resources.Icons16.IconList16.ico";

            using (var stream = Global.Assembly.GetManifestResourceStream(iconPath))
            {
                if (stream is not null)
                {
                    form.Icon = new Icon(stream);
                }
            }
        }

        /// <summary>
        /// Check if a file can be read.
        /// </summary>
        /// <param name="filePath">The file path to check.</param>
        /// <returns>A boolean</returns>
        public static bool IsFileReadable(string filePath)
        {
            // Catch if file does not exist
            if (!File.Exists(filePath))
            {
                return false;
            }

            // Try to read the file
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <returns></returns>
        public static Result OpenDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                return Result.Failed;
            }

            try
            {
                System.Diagnostics.Process.Start("explorer.exe", directoryPath);
                return Result.Succeeded;
            }
            catch
            {
                return Result.Failed;
            }
        }


    }
}
