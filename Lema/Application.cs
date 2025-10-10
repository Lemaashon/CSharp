using Lema.Commands;
using Autodesk.Revit.UI;

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

            // Register your command here if needed

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