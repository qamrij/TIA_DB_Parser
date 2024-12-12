using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using TiaDBReader.Models;

namespace TiaDBReader
{
    public class ExcelExporter
    {
        // Static properties for row configuration
        public static int HeaderRow { get; } = 11;  // Row where headers start
        public static int StartRow => HeaderRow + 1; // Row where data starts
        public void ExportComments(List<CommentInfo> comments, string outputPath)
        {
            try
            {
                Console.WriteLine("\nExporting comments to Excel...");
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Alarms");

                    // Aggiungi intestazioni
                    ConfigureHeaders(worksheet);

                    // Aggiungi dati
                    FillData(worksheet, comments);

                    // Formatta il foglio
                    //FormatWorksheet(worksheet, comments.Count);

                    // Salva il file
                    string excelPath = Path.Combine(outputPath, "Alarms.xlsx");
                    workbook.SaveAs(excelPath);
                    Console.WriteLine($"Excel file saved successfully at: {excelPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting to Excel: {ex.Message}");
                if (ex.Message.Contains("being used by another process"))
                {
                    Console.WriteLine("Please ensure the Excel file is not open in another application.");
                }
                throw;
            }
        }

        private void ConfigureHeaders(IXLWorksheet worksheet)
        {
            var headers = new[]
                                {
                                    "Name", "Folder", "Tag", "ActivationType", "Value", "AlarmType", "Message", "Priority", "PageType",
                                    "Page", "IsLogged", "IsPrinted", "HasAcknowledgeTag", "AcknowledgeType", "AcknowledgeTag",
                                    "AcknowledgeValue", "RangeMin", "RangeMax", "SingleInstance", "NegativeLogic", "CustomKey", "Tag OPC",
                                    "OPC-Folder", "Identifier", "Node Id", "Num. Path"
                                };
            var cell = worksheet.Cell(2, 1);
            cell.Value = "1.0.9";
            for (int i = 0; i < headers.Length; i++)
            {
                cell = worksheet.Cell(HeaderRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = false;
                cell.Style.Fill.BackgroundColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
        }

        private void FillData(IXLWorksheet worksheet, List<CommentInfo> comments)
        {
            for (int i = 0; i < comments.Count; i++)
            {
                var row = StartRow + i; // Use the Static startrow
                var comment = comments[i];

                // Populate row data
                worksheet.Cell(row, 1).Value = comment.Name; // Name
                worksheet.Cell(row, 2).Value = CommentInfo.Folder; // Folder
                worksheet.Cell(row, 3).Value = comment.Tag; // Tag
                worksheet.Cell(row, 4).Value = CommentInfo.ActivationType; // ActivationType
                worksheet.Cell(row, 5).Value = comment.Value; // Value
                worksheet.Cell(row, 6).Value = CommentInfo.AlarmType; // AlarmType
                worksheet.Cell(row, 7).Value = comment.Message; // Message
                worksheet.Cell(row, 8).Value = comment.Priority; // Priority
                worksheet.Cell(row, 9).Value = CommentInfo.PageType; // PageType
                worksheet.Cell(row, 10).Value = CommentInfo.Page; // Page
                worksheet.Cell(row, 11).Value = CommentInfo.IsLogged; // IsLogged
                worksheet.Cell(row, 12).Value = CommentInfo.IsPrinted; // IsPrinted
                worksheet.Cell(row, 13).Value = CommentInfo.HasAcknowledgeTag; // HasAcknowledgeTag
                worksheet.Cell(row, 14).Value = CommentInfo.AcknowledgeType; // AcknowledgeType
                worksheet.Cell(row, 15).Value = CommentInfo.AcknowledgeTag; // AcknowledgeTag
                worksheet.Cell(row, 16).Value = CommentInfo.AcknowledgeValue; // AcknowledgeValue
                worksheet.Cell(row, 17).Value = CommentInfo.RangeMin; // RangeMin
                worksheet.Cell(row, 18).Value = CommentInfo.RangeMax; // RangeMax
                worksheet.Cell(row, 19).Value = CommentInfo.SingleInstance; // SingleInstance
                worksheet.Cell(row, 20).Value = CommentInfo.NegativeLogic; // NegativeLogic
                worksheet.Cell(row, 21).Value = comment.CustomKey; // CustomKey
                worksheet.Cell(row, 22).Value = CommentInfo.TagOPC; // Tag OPC
                worksheet.Cell(row, 23).Value = CommentInfo.OPCFolder; // OPC-Folder
                worksheet.Cell(row, 24).Value = CommentInfo.Identifier; // Identifier
                worksheet.Cell(row, 25).Value = CommentInfo.NodeId; // Node Id
                worksheet.Cell(row, 26).Value = CommentInfo.NumPath; // Num. Path

                // Optional: Align all cells for better readability
                for (int col = 1; col <= 26; col++)
                {
                    worksheet.Cell(row, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                }
            }
        }

        private void FormatWorksheet(IXLWorksheet worksheet, int rowCount)
        {
            // Autofit colonne
            worksheet.Columns().AdjustToContents();

            // Imposta una larghezza minima e massima per le colonne
            foreach (var column in worksheet.Columns())
            {
                if (column.Width < 10) column.Width = 10;
                if (column.Width > 100) column.Width = 100;
            }

            /*
            // Aggiungi bordi alla tabella
            var tableRange = worksheet.Range(1, 1, rowCount + 1, 5);
            tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Aggiungi filtri alle colonne
            //worksheet.Range(1, 1, 1, 5).SetAutoFilter();

            // Blocca la riga dell'intestazione
            //worksheet.SheetView.FreezeRows(1);
            */
         }
    }
}