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

    public static class PulldownButton_Ext
    {
        #region Button Creation
        /// <summary>
        /// Attempts to create a pushbutton on  a pulldownButton.
        /// </summary>
        /// <param name="pulldownButton">This is the button to add the button to.</param>
        /// <param name="buttonName">The name the user sees</param>
        /// <param name="classname">The full class name the button runs.</param>
        /// <returns>A pushbutton</returns>
        public static PushButton? AddPushButton(this PulldownButton pulldownButton, string buttonName, string className)
        {
            if (pulldownButton is null)
            {
                Debug.WriteLine($"ERROR: Could not add {buttonName} to pulldown");
                return null;
            }



            //Create a data object
            var pushButtonData = gRib.NewPushButtonData(buttonName, className);

            if (pulldownButton.AddPushButton(pushButtonData) is PushButton pushButton)
            {
                //If the button was made, return it
                return pushButton;
            }

            else
            {
                Debug.WriteLine($"ERROR: Could not add {buttonName} to pulldown");
                return null;

            }
        }



        #endregion
    }


}
