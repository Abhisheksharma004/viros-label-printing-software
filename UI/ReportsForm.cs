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
        Button btnSearch = new Button();
        Button btnReprint = new Button();
        DataGridView grid = new DataGridView();

        public ReportsForm()
        {
            Dock = DockStyle.Fill;
            // Use TableLayoutPanel for top filter/search area
            var topTable = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(10),
                ColumnCount = 6,
                RowCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            topTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120)); // label
            topTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180)); // design filter
            topTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100)); // label
            topTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));  // serial filter
            topTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));  // search btn
            topTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130)); // reprint btn
            for (int i = 0; i < 2; i++) topTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lblD = new Label{ Text="Filter by Design:", Anchor=AnchorStyles.Left, AutoSize=true };
            txtDesignFilter.Dock = DockStyle.Fill;
            var lblS = new Label{ Text="Filter by Serial:", Anchor=AnchorStyles.Left, AutoSize=true };
            txtSerialFilter.Dock = DockStyle.Fill;
            btnSearch.Text = "Search"; btnSearch.Dock = DockStyle.Fill;
            btnSearch.Click += (s,e)=> RefreshGrid();
            btnReprint.Text = "Reprint Selected"; btnReprint.Dock = DockStyle.Fill;
            btnReprint.Click += (s,e)=> ReprintSelected();

            topTable.Controls.Add(lblD, 0, 0);
            topTable.Controls.Add(txtDesignFilter, 1, 0);
            topTable.Controls.Add(lblS, 2, 0);
            topTable.Controls.Add(txtSerialFilter, 3, 0);
            topTable.Controls.Add(btnSearch, 4, 0);
            topTable.Controls.Add(btnReprint, 5, 0);

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
            DataTable dt = Database.GetPrintLogs(string.IsNullOrWhiteSpace(txtDesignFilter.Text) ? null : txtDesignFilter.Text, serial);
            grid.DataSource = dt;
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
    }
}
