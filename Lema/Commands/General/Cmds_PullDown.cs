using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lema.Cmds_PullDown
{
    /// <summary>
    ///     External command entry point
    /// </summary>

    [Transaction(TransactionMode.Manual)]
    public class Cmd_1Button : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //Collect the document
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            // Implement your command logic here
            TaskDialog.Show("Button 1 is working", doc.Title);
            return Result.Succeeded;
        }
    }
    /// <summary>
    ///     External command entry point
    /// </summary>

    [Transaction(TransactionMode.Manual)]
    public class Cmd_2Button : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //Collect the document
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            // Implement your command logic here
            TaskDialog.Show("Button 2 is working", doc.Title);
            return Result.Succeeded;
        }
    }
    /// <summary>
    ///     External command entry point
    /// </summary>

    [Transaction(TransactionMode.Manual)]
    public class Cmd_3Button : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //Collect the document
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            // Implement your command logic here
            TaskDialog.Show("Button 3 is working", doc.Title);
            return Result.Succeeded;
        }
    }
}
