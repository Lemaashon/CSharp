//System
using System.Reflection;

//Autodesk
using Autodesk.Revit.UI;

//Lema
using gRib = Lema._Utilities.Ribbon_Utils;
//This application belongs to the root namespace
namespace Lema
{
    // Implementing the interface for the application
    public class Application: IExternalApplication
    { 
        //This will run on startup
        public Result OnStartup(UIControlledApplication uiCtrlApp)
        {
            // Collect the controlled application
            var ctrlapp = uiCtrlApp.ControlledApplication;
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyPath = assembly.Location;

            //Variables
            string tabName = "BSSE";

            // Register your command here if needed
            //Add ribbon tab
            gRib.AddRibbonTab(uiCtrlApp, tabName);

            //Create Panel
            var panelGeneral = gRib.AddRibbonPanelToTab(uiCtrlApp, tabName, "General");

            //Add button to Panel
            var buttonTest = gRib.AddPushButtonToPanel(panelGeneral, "Testing", "Lema.Cmds_General.Cmd_Test", "_testing", assemblyPath);


            //Final return
            return Result.Succeeded;
        }

        //This will run on shutdown
        public Result OnShutdown(UIControlledApplication uiCtrlApp)
        {
            // Clean up resources if needed
            return Result.Succeeded;
        }
    }

}