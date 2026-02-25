//System
using System.Reflection;

//Autodesk
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using DocumentFormat.OpenXml.Office2010.CustomUI;

using BSSE.Utilities;


//This application belongs to the root namespace
namespace BSSE
{
    /// <summary>
    /// Entry point for the Lema Revit Add-in.
    /// Implements IExternalApplication to register the ribbon tab,
    /// panel, and command button when Revit starts.
    /// </summary>
    // Implementing the interface for the application
    public class App: IExternalApplication
    {
        // ── Constants ────────────────────────────────────────────────────────────

        private const string TabName = "BSSE";
        private const string baseName = "check";
        private const string PanelName = "BIM Foundations";
        private const string ButtonName = "UpdateRebar";
        private const string ButtonText = "UpdateRebar";
        private const string ButtonTip =
            "Load a JSON configuration file and update Revit model parameters, " +
            "rebar spacing/types, rebar visibility, and view section boxes.";

        //This will run on startup
        public Result OnStartup(UIControlledApplication application)
        {

            try
            {
                // A custom tab keeps BSSE isolated from other add-in panels.
                // CreateRibbonTab throws if the tab already exists (e.g. during
                // a hot-reload in debug), so swallow that specific case.
                application.CreateRibbonTab(TabName);
            }
            catch 
            {
            }

            RibbonPanel panel = application.CreateRibbonPanel(TabName, PanelName);

            // Resolve class name and assembly path at runtime so this is
            // refactor-safe: renaming Commands.RunLemaCommand will break the build,
            // not silently produce a bad manifest string.
            string commandClass = typeof(Commands.cmds_General).FullName;
            string assemblyPath = typeof(App).Assembly.Location;

            var buttonData = new PushButtonData(
                name: ButtonName,
                text: ButtonText,
                assemblyName: assemblyPath,
                className: commandClass)
            {
                ToolTip = ButtonTip,

                // Availability: always available so the user can run it from any view.
                // Swap to a custom IExternalCommandAvailability later if needed.
            };
            buttonData.LargeImage = RibbonUtils.GetIcon(baseName, resolution: 32);
            buttonData.Image = RibbonUtils.GetIcon(baseName, resolution: 16);

            panel.AddItem(buttonData);
            //Final return
            return Result.Succeeded;
        }

        #region On Shutdown method
        //This will run on shutdown
        public Result OnShutdown(UIControlledApplication application)
        {
            // Clean up resources if needed
            return Result.Succeeded;
        }
        #endregion
    }

}