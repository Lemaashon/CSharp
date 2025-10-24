using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Lema.Forms
{
    public static class Custom
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        /// <param name="yesNo"></param>
        /// <param name="noCancel"></param>
        /// <param name="icon"></param>
        /// <returns></returns>
        public static FormResult Message(string title = null, string message = null, 
            bool yesNo = false, bool noCancel = false, MessageBoxIcon icon = MessageBoxIcon.None)
        {
            // Base form result
            var formResult = new FormResult(isValid : false);

            // Default values
            title ??= "Message";
            message ??= "No message was provided";

            // Catch the question icon
            if(yesNo && icon == MessageBoxIcon.None)
            {
                icon = MessageBoxIcon.Question;
            }
            
            // Set buttons
            var buttons = MessageBoxButtons.OKCancel;

            if (noCancel)
            {
                buttons = MessageBoxButtons.OK;
            }

            else if (yesNo)
            {
                buttons = MessageBoxButtons.YesNo;
            }

            // Run the form
            var dialogResult = MessageBox.Show(message, title, buttons, icon);

            // Process the result
            if (dialogResult == DialogResult.Yes || dialogResult == DialogResult.OK)
            {
                formResult.Validate();
            }

            // Return the formresult
            return formResult;

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Result Completed(string message)
        {
            Message(title: "Task Completed",
                message: message, 
                noCancel: true, 
                icon: MessageBoxIcon.Information);

            return Result.Succeeded;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Result Cancelled(string message)
        {
            Message(title: "Task Cancelled",
                message: message,
                noCancel: true,
                icon: MessageBoxIcon.Warning);

            return Result.Cancelled;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Result Error(string message)
        {
            Message(title: "Error",
                message: message,
                noCancel: true,
                icon: MessageBoxIcon.Error);
            return Result.Cancelled;
        }
    }
    public class FormResult
    {
        // Form object properties
        public object Object { get; set; }
        public List<object> Objects { get; set; } 
         
        //Form condition properties
        public bool Cancelled { get; set; }
        public bool Valid { get; set; }
        public bool Affirmative { get; set; }

        // Constructor (default)
        public FormResult()
        {
            this.Object = null;
            this.Objects = new List<object>();
            this.Cancelled = true;
            this.Valid = false;
            this.Affirmative = false;
        }

        //Constructor (alternative)
        public FormResult(bool isValid)
        {
            this.Object = null;
            this.Objects = new List<object>();
            this.Cancelled = !isValid;
            this.Valid = isValid;
            this.Affirmative = isValid;
        }

        //Method
        public void Validate()
        {
            this.Cancelled = false;
            this.Valid = true;
            this.Affirmative = true;
        }
        //Method
        public void Validate(object obj)
        {
            this.Validate();
            this.Object = obj;
        }
        //Method
        public void Validate(List<object> objs)
        {
            this.Validate();
            this.Objects = objs;
 
        }
    }
}
