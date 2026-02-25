using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Assembly = System.Reflection.Assembly;

namespace BSSE.Utilities
{
    public static class RibbonUtils
    {
        public static Assembly Assembly { get; set; }
        public static string AssemblyPath { get; set; }


        //Method to get an icon as an image source
        public static ImageSource GetIcon(string baseName, int resolution = 32)
        {
            Assembly = Assembly.GetExecutingAssembly();
            AssemblyPath = Assembly.Location;

            var resourcePath = $"BSSE.Resources.Icons{resolution}.{baseName}{resolution}.png";

            using (var stream = Assembly.GetManifestResourceStream(resourcePath))
            {
                if (stream is null) { return null; }

                var decoder = new PngBitmapDecoder(
                    stream,
                    BitmapCreateOptions.PreservePixelFormat,
                    BitmapCacheOption.Default);
                if (decoder.Frames.Count > 0)
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
