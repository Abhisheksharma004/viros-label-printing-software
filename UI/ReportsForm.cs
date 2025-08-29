using System;
using System.Data;
using System.Windows.Forms;
using LabelPrinterApp.Services;

namespace LabelPrinterApp.UI
{
    public class ReportsForm : UserControl
    {
        public event Action? ReprintCompleted;
        
        TextBox txtDesignFilter = new TextBox();
        TextBox txtSerialFilter = new TextBox();
        DateTimePicker dtpFromDate = new DateTimePicker();
        DateTimePicker dtpToDate = new DateTimePicker();
        CheckBox chkEnableDateFilter = new CheckBox();
        Button btnToday = new Button();
        Button btnLastWeek = new Button();
        Button btnLastMonth = new Button();
        Button btnSearch = new Button();
        Button btnReprint = new Button();
        Button btnExportExcel = new Button();
        Button btnExportStats = new Button();
        Button btnClearFilters = new Button();
        DataGridView grid = new DataGridView();

        public ReportsForm()
        {
            Dock = DockStyle.Fill;
            // Use TableLayoutPanel for top filter/search area
            var topTable = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 160, // Increased height for quick date filters
                Padding = new Padding(10),
                ColumnCount = 6,
                RowCount = 5, // Increased to 5 rows for quick date filters
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            topTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120)); // label
            topTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180)); // main filter
            topTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100)); // label
            topTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180)); // secondary filter  
            topTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));  // button
            topTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130)); // button
            for (int i = 0; i < 5; i++) topTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lblD = new Label{ Text="Filter by Design:", Anchor=AnchorStyles.Left, AutoSize=true };
            txtDesignFilter.Dock = DockStyle.Fill;
            txtDesignFilter.PlaceholderText = "Enter design name...";
            
            var lblS = new Label{ Text="Filter by Serial:", Anchor=AnchorStyles.Left, AutoSize=true };
            txtSerialFilter.Dock = DockStyle.Fill;
            txtSerialFilter.PlaceholderText = "Enter serial number...";
            
            btnSearch.Text = "Search"; btnSearch.Dock = DockStyle.Fill;
            btnSearch.Click += (s,e)=> RefreshGrid();
            btnReprint.Text = "Reprint Selected"; btnReprint.Dock = DockStyle.Fill;
            btnReprint.Click += (s,e)=> ReprintSelected();

            // Date filter controls
            chkEnableDateFilter = new CheckBox { Text = "Enable Date Filter", Dock = DockStyle.Fill, Checked = false };
            chkEnableDateFilter.CheckedChanged += (s, e) => {
                dtpFromDate.Enabled = chkEnableDateFilter.Checked;
                dtpToDate.Enabled = chkEnableDateFilter.Checked;
                RefreshGrid();
            };

            var lblFromDate = new Label { Text = "From Date:", Anchor = AnchorStyles.Left, AutoSize = true };
            dtpFromDate = new DateTimePicker 
            { 
                Format = DateTimePickerFormat.Short, 
                Dock = DockStyle.Fill,
                Value = DateTime.Now.AddDays(-30), // Default to last 30 days
                Enabled = false
            };
            dtpFromDate.ValueChanged += (s, e) => { if (chkEnableDateFilter.Checked) RefreshGrid(); };

            var lblToDate = new Label { Text = "To Date:", Anchor = AnchorStyles.Left, AutoSize = true };
            dtpToDate = new DateTimePicker 
            { 
                Format = DateTimePickerFormat.Short, 
                Dock = DockStyle.Fill,
                Value = DateTime.Now,
                Enabled = false
            };
            dtpToDate.ValueChanged += (s, e) => { if (chkEnableDateFilter.Checked) RefreshGrid(); };

            btnClearFilters = new Button { Text = "Clear All", Dock = DockStyle.Fill };
            btnClearFilters.Click += (s, e) => ClearAllFilters();

            // Quick date filter buttons
            btnToday = new Button { Text = "Today", Dock = DockStyle.Fill };
            btnToday.Click += (s, e) => SetDateFilter(DateTime.Now.Date, DateTime.Now.Date);

            btnLastWeek = new Button { Text = "Last Week", Dock = DockStyle.Fill };
            btnLastWeek.Click += (s, e) => SetDateFilter(DateTime.Now.AddDays(-7).Date, DateTime.Now.Date);

            btnLastMonth = new Button { Text = "Last Month", Dock = DockStyle.Fill };
            btnLastMonth.Click += (s, e) => SetDateFilter(DateTime.Now.AddDays(-30).Date, DateTime.Now.Date);

            // Add Excel export buttons
            btnExportExcel = new Button { Text = "Export Excel", Dock = DockStyle.Fill };
            btnExportExcel.Click += (s, e) => ExportToExcel();
            
            btnExportStats = new Button { Text = "Export with Stats", Dock = DockStyle.Fill };
            btnExportStats.Click += (s, e) => ExportToExcelWithStats();

            // Row 0: Design filter and Serial filter
            topTable.Controls.Add(lblD, 0, 0);
            topTable.Controls.Add(txtDesignFilter, 1, 0);
            topTable.Controls.Add(lblS, 2, 0);
            topTable.Controls.Add(txtSerialFilter, 3, 0);
            topTable.Controls.Add(btnSearch, 4, 0);
            topTable.Controls.Add(btnReprint, 5, 0);
            
            // Row 1: Date filter controls
            topTable.Controls.Add(chkEnableDateFilter, 0, 1);
            topTable.Controls.Add(lblFromDate, 1, 1);
            topTable.Controls.Add(dtpFromDate, 2, 1);
            topTable.Controls.Add(lblToDate, 3, 1);
            topTable.Controls.Add(dtpToDate, 4, 1);
            topTable.Controls.Add(btnClearFilters, 5, 1);
            
            // Row 2: Quick date filter buttons
            var lblQuickFilters = new Label { Text = "Quick Filters:", Anchor = AnchorStyles.Left, AutoSize = true };
            topTable.Controls.Add(lblQuickFilters, 0, 2);
            topTable.Controls.Add(btnToday, 1, 2);
            topTable.Controls.Add(btnLastWeek, 2, 2);
            topTable.Controls.Add(btnLastMonth, 3, 2);
            
            // Row 3: Export buttons
            topTable.Controls.Add(btnExportExcel, 0, 3);
            topTable.Controls.Add(btnExportStats, 1, 3);
            
            // Row 4: Info label
            var lblInfo = new Label 
            { 
                Text = $"Showing {(grid.DataSource as DataTable)?.Rows.Count ?? 0} records", 
                Anchor = AnchorStyles.Left, 
                AutoSize = true, 
                ForeColor = System.Drawing.Color.Gray 
            };
            topTable.Controls.Add(lblInfo, 2, 3);

            grid.Dock = DockStyle.Fill;
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;

            Controls.Add(grid);
            Controls.Add(topTable);

            RefreshGrid();
        }

        private void RefreshGrid()
        {
            int? serial = null;
            if (int.TryParse(txtSerialFilter.Text, out var s)) serial = s;
            
            DateTime? fromDate = null;
            DateTime? toDate = null;
            
            if (chkEnableDateFilter.Checked)
            {
                fromDate = dtpFromDate.Value.Date;
                toDate = dtpToDate.Value.Date;
                
                // Validate date range
                if (fromDate > toDate)
                {
                    MessageBox.Show("From Date cannot be later than To Date.", "Invalid Date Range", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            
            DataTable dt = Database.GetPrintLogs(
                string.IsNullOrWhiteSpace(txtDesignFilter.Text) ? null : txtDesignFilter.Text, 
                serial, 
                fromDate, 
                toDate
            );
            grid.DataSource = dt;
            
            // Update info label if it exists
            UpdateRecordCountInfo();
        }

        private void UpdateRecordCountInfo()
        {
            // Find and update the info label
            foreach (Control control in Controls)
            {
                if (control is TableLayoutPanel topTable)
                {
                    foreach (Control child in topTable.Controls)
                    {
                        if (child is Label lblInfo && lblInfo.Text.StartsWith("Showing"))
                        {
                            var recordCount = (grid.DataSource as DataTable)?.Rows.Count ?? 0;
                            var filterStatus = "";
                            
                            if (chkEnableDateFilter.Checked)
                            {
                                filterStatus = $" (Filtered: {dtpFromDate.Value:dd/MM/yyyy} - {dtpToDate.Value:dd/MM/yyyy})";
                            }
                            
                            lblInfo.Text = $"Showing {recordCount} records{filterStatus}";
                            break;
                        }
                    }
                    break;
                }
            }
        }

        private void ClearAllFilters()
        {
            txtDesignFilter.Clear();
            txtSerialFilter.Clear();
            chkEnableDateFilter.Checked = false;
            dtpFromDate.Value = DateTime.Now.AddDays(-30);
            dtpToDate.Value = DateTime.Now;
            RefreshGrid();
        }

        private void SetDateFilter(DateTime fromDate, DateTime toDate)
        {
            chkEnableDateFilter.Checked = true;
            dtpFromDate.Enabled = true;
            dtpToDate.Enabled = true;
            dtpFromDate.Value = fromDate;
            dtpToDate.Value = toDate;
            RefreshGrid();
        }

        public void RefreshLogs()
        {
            RefreshGrid();
        }

        private void ReprintSelected()
        {
            if (grid.SelectedRows.Count == 0) 
            {
                MessageBox.Show("Please select a row to reprint.");
                return;
            }
            
            var row = grid.SelectedRows[0];
            var designId = Convert.ToInt32(row.Cells["DesignId"].Value);
            var serial = Convert.ToInt32(row.Cells["Serial"].Value);
            var designName = row.Cells["DesignName"].Value?.ToString() ?? "Unknown";
            
            try
            {
                var prnTemplate = Database.GetPrnByDesignId(designId);
                var prn = ShortcutCodeService.ReplaceShortcutCodes(prnTemplate, serial);
                bool success = PrintingService.PrintRaw("", prn);
                
                if (success)
                {
                    // Log the reprint activity with reprint flag
                    Database.LogPrintedSerial(designId, serial, isReprint: true);
                    
                    MessageBox.Show($"Successfully reprinted serial {serial} for design '{designName}'");
                    
                    // Refresh the grid to show the new reprint log entry
                    RefreshGrid();
                    
                    // Notify that a reprint was completed
                    ReprintCompleted?.Invoke();
                }
                else
                {
                    MessageBox.Show($"Failed to reprint serial {serial}. Please check if printer is available.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during reprint: {ex.Message}");
            }
        }

        private void ExportToExcel()
        {
            var dataTable = grid.DataSource as DataTable;
            if (dataTable == null || dataTable.Rows.Count == 0)
            {
                MessageBox.Show("No data to export. Please run a search first.", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var filePath = ExcelExportService.ExportToExcel(dataTable, "PrintReports", "Print Reports");
                if (!string.IsNullOrEmpty(filePath))
                {
                    var result = MessageBox.Show($"Report exported successfully to:\n{filePath}\n\nWould you like to open the file?", 
                        "Export Successful", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    
                    if (result == DialogResult.Yes)
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = filePath,
                                UseShellExecute = true
                            });
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Could not open file: {ex.Message}", "File Open Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportToExcelWithStats()
        {
            var dataTable = grid.DataSource as DataTable;
            if (dataTable == null || dataTable.Rows.Count == 0)
            {
                MessageBox.Show("No data to export. Please run a search first.", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var designFilter = string.IsNullOrWhiteSpace(txtDesignFilter.Text) ? null : txtDesignFilter.Text;
                int? serialFilter = null;
                if (int.TryParse(txtSerialFilter.Text, out var s)) serialFilter = s;

                // Include date filter information
                DateTime? fromDate = chkEnableDateFilter.Checked ? dtpFromDate.Value.Date : null;
                DateTime? toDate = chkEnableDateFilter.Checked ? dtpToDate.Value.Date : null;

                var filePath = ExcelExportService.ExportPrintReportsWithStats(dataTable, designFilter, serialFilter, fromDate, toDate);
                if (!string.IsNullOrEmpty(filePath))
                {
                    var result = MessageBox.Show($"Report with statistics exported successfully to:\n{filePath}\n\nThis file contains both the data and statistical analysis.\n\nWould you like to open the file?", 
                        "Export Successful", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    
                    if (result == DialogResult.Yes)
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = filePath,
                                UseShellExecute = true
                            });
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Could not open file: {ex.Message}", "File Open Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
