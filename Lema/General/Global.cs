using Autodesk.Revit.UI;
using Autodesk.Revit.ApplicationServices;
using Assembly = System.Reflection.Assembly;
using System.Resources;
using Lema.Resources.Files;
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Collections.Generic;



namespace Lema
{
    public static class Global
    {
        #region Properties
        //Applications
        public static UIControlledApplication UiCtrlApp { get; set; }
        public static ControlledApplication CtrlApp { get; set; }
        public static UIApplication UIApp { get; set; }

        //Assembly
        public static Assembly Assembly { get; set; }
        public static string AssemblyPath { get; set; }

        //Revit Versions
        public static string RevitVersion { get; set; }
        public static int RevitVersionInt { get; set; }

        //Usernames
        public static string UsernameRevit { get; set; }
        public static string UsernameWindows { get; set; }

        //Guids and versioning
        public static string AddinVersionNumber { get; set; }
        public static string AddinVersionName { get; set; }
        public static string AddinName { get; set; }
        public static string AddinGuid { get; set; }

        // Dictionaries for resources
        public static Dictionary<string, string> Tooltips { get; set; } = new Dictionary<string, string>();    
        #endregion

        #region Methods
        public static void RegisterProperties(UIControlledApplication uiCtrlApp)
        {
            uiCtrlApp = uiCtrlApp;
            CtrlApp = uiCtrlApp.ControlledApplication;
            //UiApp set on idling

            Assembly = Assembly.GetExecutingAssembly();
            AssemblyPath = Assembly.Location;

            RevitVersion = CtrlApp.VersionNumber;
            RevitVersionInt = Int32.Parse(RevitVersion);

            //Revit username set on idling
            UsernameWindows = Environment.UserName;

            AddinVersionNumber = "25.03.01";
            AddinVersionName = "WIP";
            AddinName = "Lema"; 
            AddinGuid   = "251380C0-FE9D-45CE-977B-1497CFC3B037";

        }
        #endregion

        //Register our tooltips

        public static void RegisterTooltips(string resourcePath)
        {
            var resourceManager = new ResourceManager(resourcePath, typeof(Application).Assembly);
            var resourceSet = resourceManager.GetResourceSet(System.Globalization.CultureInfo.CurrentCulture, true, true);

            foreach (DictionaryEntry entry in resourceSet)
            {
                var key = entry.Key.ToString();
                var value = entry.Value.ToString();
                Tooltips[key] = value;

            }
        }
    }
}
