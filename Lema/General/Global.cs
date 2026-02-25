using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BSSE.General
{
    public static class Global
    {
        //Applications
        public static UIControlledApplication UiCtrlApp { get; set; }
        public static ControlledApplication CtrlApp { get; set; }
        public static UIApplication UIApp { get; set; }

        //Assembly
        public static Assembly Assembly { get; set; }
        public static string AssemblyPath { get; set; }
        public static string AddinName { get; set; }
        public static string AddinGuid { get; set; }
        public static void RegisterProperties(UIControlledApplication uiCtrlApp)
        {
            uiCtrlApp = uiCtrlApp;
            CtrlApp = uiCtrlApp.ControlledApplication;
            //UiApp set on idling

            Assembly = Assembly.GetExecutingAssembly();
            AssemblyPath = Assembly.Location;

        }

    }

}
