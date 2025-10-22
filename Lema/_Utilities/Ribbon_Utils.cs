using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Lema._Utilities
{
    public static class Ribbon_Utils
    {
        #region Button Data
        /// <summary>
        /// Create a PushButtonData object.
        /// </summary>
        /// <param name="buttonName"></param>
        /// <param name="className"></param>
        /// <returns>A PushButtonData object.</returns>
        public static PushButtonData? NewPushButtonData( string buttonName, string className)
        {
            //Get our base nae
            var baseName = commandToBaseName(className);

            //Create a data object
            var pushButtonData = new PushButtonData(baseName, buttonName, Global.AssemblyPath, className);

            //Set the values
            pushButtonData.ToolTip = LookupTooltip(baseName);
            pushButtonData.Image = GetIcon(baseName, resolution: 16);
            pushButtonData.LargeImage = GetIcon(baseName, resolution: 32);

            //Return the object
            return pushButtonData;

        }
        /// <summary>
        /// Create a PulldownButtonData object.
        /// </summary>
        /// <param name="buttonName"></param>
        /// <param name="className"></param>
        /// <returns>A PulldownButtonData object.</returns>
        public static PulldownButtonData? NewPulldownButtonData(string buttonName, string className)
        {
            //Get our base nae
            var baseName = commandToBaseName(className);

            //Create a data object
            var pulldownButtonData = new PulldownButtonData(baseName, buttonName);

            //Set the values
            pulldownButtonData.ToolTip = LookupTooltip(baseName);
            pulldownButtonData.Image = GetIcon(baseName, resolution: 16);
            pulldownButtonData.LargeImage = GetIcon(baseName, resolution: 32);

            //Return the object
            return pulldownButtonData;

        }
        #endregion


        //method to get base name
        public static string commandToBaseName(string commandName)
        {
            return commandName.Replace("Lema.Cmds_", "").Replace("Cmd_", "");
        }

        public static string LookupTooltip(string key, string failValue=null)
        {
            failValue ??= "No tooltip found";
            if (Global.Tooltips.TryGetValue(key, out string value))
            {
                return value;
            }
            else
            {
                return failValue;
            }
        }

        //Method to get an icon as an image source
        public static ImageSource GetIcon(string baseName, int resolution =32)
        {
            var resourcePath = $"Lema.Resources.Icons{resolution}.{baseName}{resolution}.png";

            using (var stream = Global.Assembly.GetManifestResourceStream(resourcePath))
            {
                if(stream is null) { return null; }

                var decoder = new PngBitmapDecoder(
                    stream, 
                    BitmapCreateOptions.PreservePixelFormat, 
                    BitmapCacheOption.Default);
                if (decoder.Frames.Count>0)
                {
                    return decoder.Frames.First();
                }
                else
                {
                    return null;
                }
            }
        }



            
    }
}
