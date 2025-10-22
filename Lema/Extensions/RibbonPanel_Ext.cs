using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using gRib = Lema._Utilities.Ribbon_Utils;

namespace Lema.Extensions
{
    
    public static class RibbonPanel_Ext
    {
        #region Button Creation
        /// <summary>
        /// Method will be described as in the hover menu
        /// </summary>
        /// <param name="panel">This is the ribbonpanel to add to.</param>
        /// <param name="buttonName">The name the user sees</param>
        /// <param name="classname">The full class name the button runs.</param>
        /// <returns>A pushbutton</returns>
        public static PushButton? AddPushButton(this RibbonPanel panel, string buttonName, string className)
        {
            if (panel is null)
            {
                Debug.WriteLine($"ERROR: Could not add {buttonName} to panel");
                return null;
            }



            //Create a data object
            var pushButtonData = gRib.NewPushButtonData(buttonName, className);

            if (panel.AddItem(pushButtonData) is PushButton pushButton)
            {
                //If the button was made, return it
                return pushButton;
            }

            else
            {
                Debug.WriteLine($"ERROR: Could not add {buttonName} to panel");
                return null;

            }
        }

        /// <summary>
        /// Attempts to create a PushButton on a RibbonPanel.
        /// </summary>
        /// <param name="panel">This is the ribbonpanel to add to.</param>
        /// <param name="buttonName">The name the user sees</param>
        /// <param name="classname">The full class name the button runs.</param>
        /// <returns>A pulldownbutton</returns>
        public static PulldownButton? AddPulldownButton(this RibbonPanel panel, string buttonName, string className)
        {
            if (panel is null)
            {
                Debug.WriteLine($"ERROR: Could not add {buttonName} to panel");
                return null;
            }

            //Create a data object
            var pulldownButtonData = gRib.NewPulldownButtonData(buttonName, className);

            if (panel.AddItem(pulldownButtonData) is PulldownButton pulldownButton)
            {
                //If the button was made, return it
                return pulldownButton;
            }

            else
            {
                Debug.WriteLine($"ERROR: Could not add {buttonName} to panel");
                return null;

            }
        }


        #endregion
    }


}
