//Autodesk
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Lema.Extensions;

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
            //Collect all sheets
            var sheets = doc.Ext_GetSheets();
            var revisions = doc.Ext_GetRevisions();

            TaskDialog.Show(doc.Title, $"We have {sheets.Count} sheets in the model.");
            TaskDialog.Show(doc.Title, $"We have {revisions.Count} revisions in the model.");
            return Result.Succeeded;
        }
    }
}