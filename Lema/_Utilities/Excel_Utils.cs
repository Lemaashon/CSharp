using Autodesk.Revit.UI;
using ClosedXML.Excel;
using System.IO;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using gFrm = Lema.Forms;
using gFil = Lema._Utilities.File_Utils;

namespace Lema._Utilities
{
    public static class Excel_Utils
    {
        /// <summary>
        /// Attempt to get a workbook at a filepath.
        /// </summary>
        /// <param name="filePath">The filepath to access.</param>
        /// <returns>An XLWorkbook</returns>
        public static XLWorkbook GetWorkbook(string filePath)
        {
            // Try to read the workbook
            try
            {
                return new XLWorkbook(filePath);
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// Attempt to get a worksheet, with the option to default to the first one.
        /// </summary>
        /// <param name="workbook">The workbook to check.</param>
        /// <param name="worksheetName">The name of the worksheet to get.</param>
        /// <param name="getFirstIfNotFound">Get the first worksheet if we fail to find.</param>
        /// <returns>An IXLWorksheet</returns>
        public static IXLWorksheet GetWorkSheet(XLWorkbook workbook, string worksheetName = "Sheet1", bool getFirstIfNotFound = false)
        {
            // Null check
            if (workbook is null)
            {
                return null;
            }

            // Check each worksheet
            foreach (var worksheet in workbook.Worksheets)
            {
                if (worksheet.Name == worksheetName)
                {
                    return worksheet;
                }
            }

            // Get the first if we failed to find
            if (getFirstIfNotFound)
            {
                return workbook.Worksheets.First();
            }

            // Return null if we got no worksheet
            return null;
        }

        /// <summary>
        /// Verifies if an Excel file can be read.
        /// </summary>
        /// <param name="filePath">The filepath to check.</param>
        /// <param name="worksheetName">An optional worksheet to find.</param>
        /// <returns>A Result.</returns>
        public static Result VerifyExcelFile(string filePath, string worksheetName = null)
        {
            // Catch if file does not exist
            if (!File.Exists(filePath))
            {
                return gFrm.Custom.Cancelled("The file could not be found.");
            }

            // Catch if file cannot be read
            if (!gFil.IsFileReadable(filePath))
            {
                return gFrm.Custom.Cancelled("The file could not be read.");
            }

            // If we want to check for a worksheet by name...
            if (worksheetName is not null)
            {
                // Get the workbook
                var workbook = GetWorkbook(filePath);

                // If we did not found the worksheet, cancel
                if (GetWorkSheet(workbook, worksheetName, false) is null)
                {
                    return gFrm.Custom.Cancelled($"{worksheetName} was not found in the Excel file.");
                }
            }

            // Otherwise, we can proceed
            return Result.Succeeded;
        }
    }
}
