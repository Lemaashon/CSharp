//System
using System.Reflection;

//Autodesk
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;


//Lema
using gRib = Lema._Utilities.Ribbon_Utils;
//This application belongs to the root namespace
namespace Lema
{
    // Implementing the interface for the application
    public class Application: IExternalApplication
    {
        //Make a private uiCtrlApp
        private static UIControlledApplication _uiCtrlApp;
        //This will run on startup
        public Result OnStartup(UIControlledApplication uiCtrlApp)
        {
            #region Global registration
            //Store _uiCtrlApp, register on idling
            _uiCtrlApp = uiCtrlApp;

            try
            {
                _uiCtrlApp.Idling += RegisterUiApp;
            }
            catch 
            {
                Global.UIApp = null;
                Global.UsernameRevit = null;
            }

            //Registering globals
            Global.RegisterProperties(uiCtrlApp);
            Global.RegisterTooltips("Lema.Resources.Files.Tooltips");
            #endregion

            #region Ribbon Setup
            // Register your command here if needed
            //Add ribbon tab
            gRib.AddRibbonTab(uiCtrlApp, Global.AddinName);

            //Create Panel
            var panelGeneral = gRib.AddRibbonPanelToTab(uiCtrlApp, Global.AddinName, "General");

            //Add button to Panel
            var buttonTest = gRib.AddPushButtonToPanel(panelGeneral, "Testing", "Lema.Cmds_General.Cmd_Test");
            #endregion

            //Final return
            return Result.Succeeded;
        }

        #region On Shutdown method
        //This will run on shutdown
        public Result OnShutdown(UIControlledApplication uiCtrlApp)
        {
            // Clean up resources if needed
            return Result.Succeeded;
        }
        #endregion

        #region Use idling to register
        private static void RegisterUiApp(object sender, IdlingEventArgs e)
        {
            _uiCtrlApp.Idling -= RegisterUiApp;

            if(sender is UIApplication uiApp)
            {
                Global.UIApp = uiApp;
                Global.UsernameRevit = uiApp.Application.Username;
            }
        }
        #endregion
    }

}