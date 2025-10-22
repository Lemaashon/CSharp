using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lema.Extensions
{
    public static class UIControlledApplication_Ext
    {
        /// <summary>
        /// Attempts to add a tab to the application.
        /// </summary>
        /// <param name="uiCtrlApp">The UIControlledApplication (extended)</param>
        /// <param name="tabName">The name of the tab to create</param>
        /// <returns>A Result.</returns>
        public static Result AddRibbonTab(this UIControlledApplication uiCtrlApp, string tabName)
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
        /// <summary>
        /// Attempts to create a RibbonPanel on a tab by name
        /// </summary>
        /// <param name="uiCtrlApp">The UIControlledApplication (extended)</param>
        /// <param name="tabName">The tab name to add it to.</param>
        /// <param name="panelName">The name to give the panel.</param>
        /// <returns>A RibbonPanel</returns>
        public static RibbonPanel? AddRibbonPanel(this UIControlledApplication uiCtrlApp, string tabName, string panelName)
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

            return uiCtrlApp.GetRibbonPanel(tabName, panelName);
        }
        /// <summary>
        /// Attempts to get a RibbonPanel on a tab by name
        /// </summary>
        /// <param name="uiCtrlApp">The UIControlledApplication (extended)</param>
        /// <param name="tabName">The tab name to search from.</param>
        /// <param name="panelName">The panel name to find</param>
        /// <returns>A RibbonPanel.</returns>
        public static RibbonPanel? GetRibbonPanel(this UIControlledApplication uiCtrlApp, string tabName, string panelName)
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
    }
}
