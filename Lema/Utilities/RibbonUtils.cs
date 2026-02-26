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
    /// <summary>
    /// Helpers for loading embedded PNG resources as WPF ImageSource objects
    /// for use on Revit ribbon buttons.
    ///
    /// RESOURCE NAMING CONVENTION
    /// ───────────────────────────
    /// Embedded resource paths are built from the project default namespace,
    /// the folder path (dots replace slashes), and the filename:
    ///
    ///   File on disk:  Resources/Icons32/check32.png
    ///   Resource path: BSSE.Resources.Icons32.check32.png
    ///
    /// Folder names that contain a dot (e.g. "Icons32") are fine — the compiler
    /// treats the entire folder name as one segment, not multiple namespace parts.
    /// </summary>
    public static class RibbonUtils
    {
        // ── Properties ────────────────────────────────────────────────────────────

        /// <summary>
        /// The assembly from which embedded resources are loaded.
        /// Set once in App.OnStartup and reused by all GetIcon calls.
        ///
        /// IMPORTANT: This property was previously named 'Assembly', which
        /// collided with the 'using Assembly = System.Reflection.Assembly' alias.
        /// Inside a class body, member names take precedence over type aliases,
        /// so 'Assembly.GetExecutingAssembly()' resolved the left-hand 'Assembly'
        /// as the property (null) instead of the type — causing a silent
        /// NullReferenceException and a blank button image.
        /// Renamed to 'PluginAssembly' to eliminate the ambiguity entirely.
        /// </summary
        public static Assembly PluginAssembly { get; set; }

        /// <summary>Absolute path to the add-in DLL. Populated in App.OnStartup.</summary>
        public static string AssemblyPath { get; set; }


        /// <summary>
        /// Loads an embedded PNG icon and returns it as a WPF ImageSource.
        ///
        /// Expected resource path pattern:
        ///   BSSE.Resources.Icons{resolution}.{baseName}{resolution}.png
        ///
        /// Example for baseName="check", resolution=32:
        ///   BSSE.Resources.Icons32.check32.png
        ///
        /// Returns null (no exception) if the resource is not found, so a
        /// missing icon produces a blank button rather than crashing Revit.
        /// </summary>
        /// <param name="baseName">
        ///     Icon name without size suffix or extension, e.g. "check".
        /// </param>
        /// <param name="resolution">
        ///     16 for <see cref="PushButtonData.Image"/>,
        ///     32 for <see cref="PushButtonData.LargeImage"/>.
        /// </param>
        public static ImageSource GetIcon(string baseName, int resolution = 32)
        {
            Assembly assembly = PluginAssembly ?? Assembly.GetExecutingAssembly();

            // DEBUG: This prints every valid resource path to your 'Output' window in Visual Studio
            //string[] resourceNames = Assembly.GetManifestResourceNames();
            //foreach (string name in resourceNames)
            //{
            // System.Diagnostics.Debug.WriteLine("Resource Found: " + name);
            //}

            // Build the fully-qualified embedded resource path.
            var resourcePath = $"BSSE.Resources.Icons{resolution}.{baseName}{resolution}.png";

            using (var stream = assembly.GetManifestResourceStream(resourcePath))
            {
                if (stream is null) 
                {
                    System.Diagnostics.Debug.WriteLine(
                    $"[RibbonUtils] Icon not found: {resourcePath}");
                    return null;
                }

                var decoder = new PngBitmapDecoder(
                    stream,
                    BitmapCreateOptions.PreservePixelFormat,
                    BitmapCacheOption.OnLoad);
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
