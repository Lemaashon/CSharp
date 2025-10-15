//Autodesk
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;

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
            TaskDialog.Show("It´s working", doc.Title);
            return Result.Succeeded;
        }
    }
}