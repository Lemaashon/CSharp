//System
using System.Reflection;

//Autodesk
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;


//Lema
using gRib = Lema._Utilities.Ribbon_Utils;
using Lema.Extensions;
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
            uiCtrlApp.AddRibbonTab(Global.AddinName);

            //Create Panel
            var panelGeneral = uiCtrlApp.AddRibbonPanel(Global.AddinName, "General");

            //Add button to Panel
            var buttonTest = panelGeneral.AddPushButton("Testing", "Lema.Cmds_General.Cmd_Test");

            // Add pulldownbutton to panel
            var pulldownTest = panelGeneral.AddPulldownButton("PullDown", "Lema.Cmds_PullDown");

            //Add buttons to pulldown
            pulldownTest.AddPushButton("Button 1", "Lema.Cmds_PullDown.Cmd_1Button");
            pulldownTest.AddPushButton("Button 2", "Lema.Cmds_PullDown.Cmd_2Button");
            pulldownTest.AddPushButton("Button 3", "Lema.Cmds_PullDown.Cmd_3Button");

            // Create data objects for the stack
            var stack1Data= gRib.NewPulldownButtonData("Stack 1", "Lema.Cmds.Cmd_Stack1");
            var stack2Data = gRib.NewPulldownButtonData("Stack 2", "Lema.Cmds.Cmd_Stack2");
            var stack3Data = gRib.NewPulldownButtonData("Stack 3", "Lema.Cmds.Cmd_Stack3");

            //Create the stack
            var stack = panelGeneral.AddStackedItems(stack1Data, stack2Data, stack3Data);
            PulldownButton pulldownStack1 = stack[0] as PulldownButton;
            PulldownButton pulldownStack2 = stack[1] as PulldownButton;
            PulldownButton pulldownStack3 = stack[2] as PulldownButton;

            // Add buttons to stacked pulldowns
            pulldownStack1.AddPushButton("Button", "Lema.Cmds_Stack1.Cmd_Button");
            pulldownStack2.AddPushButton("Button", "Lema.Cmds_Stack2.Cmd_Button");
            pulldownStack3.AddPushButton("Button", "Lema.Cmds_Stack3.Cmd_Button");
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