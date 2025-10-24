//Autodesk
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Lema.Extensions;
using gFrm = Lema.Forms;

//Associate with general commands
namespace Lema.Cmds_General
{
    /// <summary>
    ///     External command entry point
    /// </summary>

    [Transaction(TransactionMode.Manual)]
    public class  Cmd_Test: IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //Collect the document
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            // Implement your command logic here
            gFrm.Custom.Message(message: "This is a message form.");
            var yesNoResult = gFrm.Custom.Message(message: "This is a YesNo form.", yesNo:true); 
            if (yesNoResult.Cancelled) { return gFrm.Custom.Cancelled("No was chosen."); }

            gFrm.Custom.Error("An error did not occur, just showing this form.");


            return gFrm.Custom.Completed("Script Completed");
        }
    }
}