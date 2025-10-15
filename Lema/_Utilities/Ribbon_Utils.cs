using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lema._Utilities
{
    public static class Ribbon_Utils
    {
        // Method to add a new ribbon tab
        public static Result AddRibbonTab(UIControlledApplication uiCtrlApp, string tabName)
            {
            try
            {
                uiCtrlApp.CreateRibbonTab(tabName);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // Handle the exception (e.g., log it)
                TaskDialog.Show("Error", $"Failed to create ribbon tab: {ex.Message}");
                return Result.Failed;
            }
        }

        // Method to add a new ribbon panel to an existing tab
        public static RibbonPanel? AddRibbonPanelToTab(UIControlledApplication uiCtrlApp, string tabName, string panelName)
        {
            try
            {
                uiCtrlApp.CreateRibbonPanel(tabName, panelName);
            }
            catch
            {
                Debug.WriteLine($"ERROR: Could not add {panelName} to {tabName}");
                return null;
            }

            return GetRibbonPanelByName(uiCtrlApp, tabName, panelName);
        }

        // Method to retrieve a ribbon panel by its name from a specific tab
        public static RibbonPanel? GetRibbonPanelByName(UIControlledApplication uiCtrlApp, string tabName, string panelName)
        {
            var panels = uiCtrlApp.GetRibbonPanels(tabName);

            foreach (var panel in panels)
            {
                if (panel.Name == panelName)
                {
                    return panel;
                }
            }
            return null;
        }

        //Method to add button to panel
        /// <summary>
        /// Method will be described as in the hover menu
        /// </summary>
        /// <param name="panel">This is the ribbonpanel to add to.</param>
        /// <param name="buttonName">Test</param>
        /// <param name="classname">Test</param>
        /// <param name="InternalName">Test</param>
        /// <param name="assemblyPath">Test</param>
        /// <returns></returns>
        public static PushButton? AddPushButtonToPanel(RibbonPanel panel, string buttonName, string className, 
            string InternalName, string assemblyPath)
        {
            if (panel is  null)
            {
                Debug.WriteLine($"ERROR: Could not add {buttonName} to panel");
                return null;
            }

            var pushButtonData = new PushButtonData(InternalName, buttonName, assemblyPath, className);

            if (panel.AddItem(pushButtonData) is PushButton pushButton)
            {
                pushButton.ToolTip = "Testing tooltip";
                //pushButton.Image = null
                pushButton.LargeImage = null;

                return pushButton;
            }

            else
            {
                Debug.WriteLine($"ERROR: Could not add {buttonName} to panel");
                return null;

            }  



            


        }
    }
}
