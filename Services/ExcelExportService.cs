using System;
using System.Data;
using System.IO;
using System.Windows.Forms;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace LabelPrinterApp.Services
{
    public static class ExcelExportService
    {
        static ExcelExportService()
        {
            // Set EPPlus license context (required for EPPlus 5.0+)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        /// <summary>
        /// Exports DataTable to Excel file with professional formatting
        /// </summary>
        /// <param name="dataTable">The DataTable to export</param>
        /// <param name="fileName">Optional filename (without extension)</param>
        /// <param name="sheetName">Optional worksheet name</param>
        /// <returns>Full path of the created Excel file, or null if cancelled/failed</returns>
        public static string? ExportToExcel(DataTable dataTable, string? fileName = null, string? sheetName = null)
        {
            if (dataTable == null || dataTable.Rows.Count == 0)
            {
                MessageBox.Show("No data to export.", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            try
            {
                // Default filename with timestamp
                fileName ??= $"PrintReports_{DateTime.Now:yyyyMMdd_HHmmss}";
                sheetName ??= "Print Reports";

                // Show save dialog
                using var saveDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                    FileName = fileName,
                    DefaultExt = "xlsx",
                    Title = "Export Reports to Excel"
                };

                if (saveDialog.ShowDialog() != DialogResult.OK)
                    return null;

                var filePath = saveDialog.FileName;

                // Create Excel package
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add(sheetName);

                // Add headers with formatting
                for (int col = 0; col < dataTable.Columns.Count; col++)
                {
                    var headerCell = worksheet.Cells[1, col + 1];
                    headerCell.Value = GetFriendlyColumnName(dataTable.Columns[col].ColumnName);
                    headerCell.Style.Font.Bold = true;
                    headerCell.Style.Fill.SetBackground(System.Drawing.Color.FromArgb(0, 150, 136)); // Viros teal
                    headerCell.Style.Font.Color.SetColor(System.Drawing.Color.White);
                    headerCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    headerCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                }

                // Add data rows
                for (int row = 0; row < dataTable.Rows.Count; row++)
                {
                    for (int col = 0; col < dataTable.Columns.Count; col++)
                    {
                        var cell = worksheet.Cells[row + 2, col + 1];
                        var value = dataTable.Rows[row][col];
                        
                        // Format specific columns
                        var columnName = dataTable.Columns[col].ColumnName;
                        if (columnName == "PrintedAt" && value != null)
                        {
                            if (DateTime.TryParse(value.ToString(), out DateTime dateTime))
                            {
                                cell.Value = dateTime;
                                cell.Style.Numberformat.Format = "dd/mm/yyyy hh:mm:ss";
                            }
                            else
                            {
                                cell.Value = value.ToString();
                            }
                        }
                        else if (columnName == "Serial" || columnName == "LogId" || columnName == "DesignId")
                        {
                            if (int.TryParse(value?.ToString(), out int intValue))
                            {
                                cell.Value = intValue;
                                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            }
                            else
                            {
                                cell.Value = value?.ToString() ?? "";
                            }
                        }
                        else
                        {
                            cell.Value = value?.ToString() ?? "";
                        }

                        // Add alternating row colors for better readability
                        if (row % 2 == 1)
                        {
                            cell.Style.Fill.SetBackground(System.Drawing.Color.FromArgb(248, 249, 250));
                        }
                    }
                }

                // Add summary information
                var summaryRow = dataTable.Rows.Count + 3;
                worksheet.Cells[summaryRow, 1].Value = "Export Summary:";
                worksheet.Cells[summaryRow, 1].Style.Font.Bold = true;
                
                worksheet.Cells[summaryRow + 1, 1].Value = $"Total Records: {dataTable.Rows.Count}";
                worksheet.Cells[summaryRow + 2, 1].Value = $"Export Date: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
                worksheet.Cells[summaryRow + 3, 1].Value = $"Generated by: {AppInfo.ApplicationName}";
                
                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();
                
                // Set minimum column width for better appearance
                for (int col = 1; col <= dataTable.Columns.Count; col++)
                {
                    if (worksheet.Column(col).Width < 12)
                        worksheet.Column(col).Width = 12;
                }

                // Add borders to data range
                var dataRange = worksheet.Cells[1, 1, dataTable.Rows.Count + 1, dataTable.Columns.Count];
                dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                // Save the file
                var fileInfo = new FileInfo(filePath);
                package.SaveAs(fileInfo);

                return filePath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting to Excel: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        /// <summary>
        /// Converts database column names to user-friendly names
        /// </summary>
        /// <param name="columnName">Database column name</param>
        /// <returns>User-friendly column name</returns>
        private static string GetFriendlyColumnName(string columnName)
        {
            return columnName switch
            {
                "LogId" => "Log ID",
                "DesignName" => "Design Name",
                "Serial" => "Serial Number",
                "PrintedAt" => "Printed At",
                "DesignId" => "Design ID",
                "PrintType" => "Print Type",
                _ => columnName
            };
        }

        /// <summary>
        /// Exports current filtered data with additional statistics
        /// </summary>
        /// <param name="dataTable">The DataTable to export</param>
        /// <param name="designFilter">Applied design filter</param>
        /// <param name="serialFilter">Applied serial filter</param>
        /// <param name="fromDate">Applied from date filter</param>
        /// <param name="toDate">Applied to date filter</param>
        /// <returns>Full path of the created Excel file, or null if cancelled/failed</returns>
        public static string? ExportPrintReportsWithStats(DataTable dataTable, string? designFilter = null, int? serialFilter = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (dataTable == null || dataTable.Rows.Count == 0)
            {
                MessageBox.Show("No data to export.", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            try
            {
                // Generate filename with filters
                var fileName = "PrintReports";
                if (!string.IsNullOrWhiteSpace(designFilter))
                    fileName += $"_Design-{designFilter.Replace(" ", "_")}";
                if (serialFilter.HasValue)
                    fileName += $"_Serial-{serialFilter}";
                if (fromDate.HasValue && toDate.HasValue)
                    fileName += $"_From-{fromDate.Value:yyyyMMdd}_To-{toDate.Value:yyyyMMdd}";
                fileName += $"_{DateTime.Now:yyyyMMdd_HHmmss}";

                // Show save dialog
                using var saveDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                    FileName = fileName,
                    DefaultExt = "xlsx",
                    Title = "Export Print Reports to Excel"
                };

                if (saveDialog.ShowDialog() != DialogResult.OK)
                    return null;

                var filePath = saveDialog.FileName;

                // Create Excel package with multiple sheets
                using var package = new ExcelPackage();
                
                // Main data sheet
                var mainSheet = package.Workbook.Worksheets.Add("Print Reports");
                CreateMainDataSheet(mainSheet, dataTable);

                // Statistics sheet
                var statsSheet = package.Workbook.Worksheets.Add("Statistics");
                CreateStatisticsSheet(statsSheet, dataTable, designFilter, serialFilter, fromDate, toDate);

                // Save the file
                var fileInfo = new FileInfo(filePath);
                package.SaveAs(fileInfo);

                return filePath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting to Excel: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        private static void CreateMainDataSheet(ExcelWorksheet worksheet, DataTable dataTable)
        {
            // Add headers
            for (int col = 0; col < dataTable.Columns.Count; col++)
            {
                var headerCell = worksheet.Cells[1, col + 1];
                headerCell.Value = GetFriendlyColumnName(dataTable.Columns[col].ColumnName);
                headerCell.Style.Font.Bold = true;
                headerCell.Style.Fill.SetBackground(System.Drawing.Color.FromArgb(0, 150, 136));
                headerCell.Style.Font.Color.SetColor(System.Drawing.Color.White);
                headerCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            // Add data
            for (int row = 0; row < dataTable.Rows.Count; row++)
            {
                for (int col = 0; col < dataTable.Columns.Count; col++)
                {
                    var cell = worksheet.Cells[row + 2, col + 1];
                    var value = dataTable.Rows[row][col];
                    
                    if (dataTable.Columns[col].ColumnName == "PrintedAt" && value != null)
                    {
                        if (DateTime.TryParse(value.ToString(), out DateTime dateTime))
                        {
                            cell.Value = dateTime;
                            cell.Style.Numberformat.Format = "dd/mm/yyyy hh:mm:ss";
                        }
                        else
                        {
                            cell.Value = value.ToString();
                        }
                    }
                    else
                    {
                        cell.Value = value?.ToString() ?? "";
                    }

                    if (row % 2 == 1)
                    {
                        cell.Style.Fill.SetBackground(System.Drawing.Color.FromArgb(248, 249, 250));
                    }
                }
            }

            worksheet.Cells.AutoFitColumns();
        }

        private static void CreateStatisticsSheet(ExcelWorksheet worksheet, DataTable dataTable, string? designFilter, int? serialFilter, DateTime? fromDate, DateTime? toDate)
        {
            int row = 1;

            // Title
            worksheet.Cells[row, 1].Value = "Print Reports Statistics";
            worksheet.Cells[row, 1].Style.Font.Size = 16;
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            row += 2;

            // Filters applied
            worksheet.Cells[row, 1].Value = "Filters Applied:";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            row++;
            
            worksheet.Cells[row, 1].Value = $"Design Filter: {designFilter ?? "None"}";
            row++;
            worksheet.Cells[row, 1].Value = $"Serial Filter: {(serialFilter?.ToString() ?? "None")}";
            row++;
            
            // Date filter information
            if (fromDate.HasValue && toDate.HasValue)
            {
                worksheet.Cells[row, 1].Value = $"Date Range: {fromDate.Value:dd/MM/yyyy} to {toDate.Value:dd/MM/yyyy}";
            }
            else
            {
                worksheet.Cells[row, 1].Value = "Date Filter: None";
            }
            row += 2;

            // General statistics
            worksheet.Cells[row, 1].Value = "General Statistics:";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            row++;

            worksheet.Cells[row, 1].Value = $"Total Records: {dataTable.Rows.Count}";
            row++;

            // Count by print type
            var originalCount = dataTable.Select("PrintType = 'Original'").Length;
            var reprintCount = dataTable.Select("PrintType = 'Reprint'").Length;
            
            worksheet.Cells[row, 1].Value = $"Original Prints: {originalCount}";
            row++;
            worksheet.Cells[row, 1].Value = $"Reprints: {reprintCount}";
            row += 2;

            // Design statistics
            if (dataTable.Rows.Count > 0)
            {
                worksheet.Cells[row, 1].Value = "Design Statistics:";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                row++;

                var designStats = new System.Collections.Generic.Dictionary<string, int>();
                foreach (DataRow dataRow in dataTable.Rows)
                {
                    var designName = dataRow["DesignName"]?.ToString() ?? "Unknown";
                    designStats[designName] = designStats.GetValueOrDefault(designName, 0) + 1;
                }

                foreach (var kvp in designStats)
                {
                    worksheet.Cells[row, 1].Value = $"{kvp.Key}: {kvp.Value} prints";
                    row++;
                }
            }

            row += 2;
            worksheet.Cells[row, 1].Value = $"Report Generated: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
            worksheet.Cells[row, 1].Style.Font.Italic = true;

            worksheet.Cells.AutoFitColumns();
        }
    }
}
