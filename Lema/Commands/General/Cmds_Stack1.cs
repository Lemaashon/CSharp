using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Lema.Extensions;
using Lema._Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lema.Cmds_Stack1
{
    /// <summary>
    ///     External command entry point
    /// </summary>

    [Transaction(TransactionMode.Manual)]
    public class Cmd_Button : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //Collect the document
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;
            ProjectInfo projectinfo = doc.Ext_GetProjectInfo();
            List<string> infoparameters = new List<string> { "Equipment Type", "Equipment Housing Type", "Site Code", "Site Name", "Site Type",
                "Fall Arrest Safety System Type", "Limited Cherry Picker Access", "Maximum Structure Height", 
                "Reinforcement Elements", "Standard Structure Identification Name" };

            var parameters = projectinfo.GetParameters(infoparameters);
            string messageText = string.Join("\n", parameters.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
            TaskDialog.Show("Project Information", messageText);
            // Implement your command logic here
            TaskDialog.Show("It´s working", doc.Title);
            return Result.Succeeded;
        }
    }
}