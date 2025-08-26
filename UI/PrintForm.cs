using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using LabelPrinterApp.Services;

namespace LabelPrinterApp.UI
{
    public class PrintForm : UserControl
    {
        public event Action? PrintCompleted;
        
        private ComboBox cboDesign;
        private ComboBox cboPrinter;
        private TextBox txtStart;
        private NumericUpDown numQty;
        private Button btnAuto;
        private Button btnPreview;
        private Button btnPrint;
        private Button btnRefreshPrinters;
        private Button btnTestPrint;
        private Label lblPrinterStatus;
        private PictureBox pbPreview;

        public PrintForm()
        {
            Dock = DockStyle.Fill;
            // TableLayoutPanel for left panel
            var leftTable = new TableLayoutPanel
            {
                Dock = DockStyle.Left,
                Width = 450,
                Padding = new Padding(8),
                ColumnCount = 4,
                RowCount = 7,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            leftTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110)); // label col
            leftTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));   // main input col
            leftTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));  // button col
            leftTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));  // button col
            for (int i = 0; i < 7; i++) leftTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lblDesign = new Label { Text = "Select Design:", Anchor = AnchorStyles.Left, AutoSize = true };
            cboDesign = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
            cboDesign.SelectedIndexChanged += async (s,e)=> await UpdatePreviewAsync();
            leftTable.Controls.Add(lblDesign, 0, 0);
            leftTable.SetColumnSpan(cboDesign, 3);
            leftTable.Controls.Add(cboDesign, 1, 0);

            var lblPrinter = new Label { Text = "Select Printer:", Anchor = AnchorStyles.Left, AutoSize = true };
            cboPrinter = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
            btnRefreshPrinters = new Button { Text = "Refresh", Dock = DockStyle.Fill };
            btnRefreshPrinters.Click += (s,e) => LoadPrinters();
            leftTable.Controls.Add(lblPrinter, 0, 1);
            leftTable.Controls.Add(cboPrinter, 1, 1);
            leftTable.Controls.Add(btnRefreshPrinters, 2, 1);

            lblPrinterStatus = new Label { Text = "Loading printers...", ForeColor = Color.Blue, AutoSize = true, Anchor = AnchorStyles.Left };
            leftTable.SetColumnSpan(lblPrinterStatus, 4);
            leftTable.Controls.Add(lblPrinterStatus, 0, 2);

            var lblSerial = new Label { Text = "Start Serial:", Anchor = AnchorStyles.Left, AutoSize = true };
            txtStart = new TextBox { PlaceholderText = "Start Serial", Dock = DockStyle.Fill };
            var lblQty = new Label { Text = "Quantity:", Anchor = AnchorStyles.Left, AutoSize = true };
            numQty = new NumericUpDown { Minimum = 1, Maximum = 100000, Value = 10, Dock = DockStyle.Fill };
            leftTable.Controls.Add(lblSerial, 0, 3);
            leftTable.Controls.Add(txtStart, 1, 3);
            leftTable.Controls.Add(lblQty, 2, 3);
            leftTable.Controls.Add(numQty, 3, 3);

            btnAuto = new Button { Text = "Auto Resume", Dock = DockStyle.Fill };
            btnAuto.Click += (s,e)=> { if (SelectedDesignId!=0) txtStart.Text=(Database.GetLastPrintedSerial(SelectedDesignId)+1).ToString(); };
            btnPreview = new Button { Text = "Preview", Dock = DockStyle.Fill };
            btnPreview.Click += async (s,e)=> await UpdatePreviewAsync();
            btnPrint = new Button { Text = "Print", Dock = DockStyle.Fill };
            btnPrint.Click += BtnPrint_Click;
            btnTestPrint = new Button { Text = "Test Printer", Dock = DockStyle.Fill };
            btnTestPrint.Click += BtnTestPrint_Click;
            leftTable.Controls.Add(btnAuto, 0, 4);
            leftTable.Controls.Add(btnPreview, 1, 4);
            leftTable.Controls.Add(btnPrint, 2, 4);
            leftTable.Controls.Add(btnTestPrint, 3, 4);

            // Add a filler row for spacing
            leftTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Create a fixed-size preview panel
            var right = new Panel{ Dock = DockStyle.Fill, Padding = new Padding(8) };
            var previewPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            pbPreview = new PictureBox 
            { 
                Width = 400, 
                Height = 300, 
                BorderStyle = BorderStyle.FixedSingle, 
                SizeMode = PictureBoxSizeMode.StretchImage,
                Anchor = AnchorStyles.None,
                BackColor = Color.White
            };
            // Center the preview in the panel when panel resizes
            previewPanel.Resize += (s, e) => {
                pbPreview.Left = Math.Max(10, (previewPanel.Width - pbPreview.Width) / 2);
                pbPreview.Top = Math.Max(10, (previewPanel.Height - pbPreview.Height) / 2);
            };
            previewPanel.Controls.Add(pbPreview);
            right.Controls.Add(previewPanel);

            Controls.Add(right);
            Controls.Add(leftTable);

            LoadDesigns();
            LoadPrinters();
        }

        private void LoadPrinters()
        {
            cboPrinter.Items.Clear();
            lblPrinterStatus.Text = "Loading printers...";
            lblPrinterStatus.ForeColor = Color.Blue;
            
            try
            {
                var printers = PrintingService.GetAvailablePrinters();
                if (printers.Count == 0)
                {
                    cboPrinter.Items.Add("No printers found");
                    cboPrinter.SelectedIndex = 0;
                    lblPrinterStatus.Text = "No printers detected. Install a printer driver.";
                    lblPrinterStatus.ForeColor = Color.Red;
                    return;
                }

                foreach (var printer in printers)
                {
                    cboPrinter.Items.Add(printer);
                }

                // Select default printer if available
                string defaultPrinter = PrintingService.GetDefaultPrinter();
                if (!string.IsNullOrEmpty(defaultPrinter))
                {
                    int index = cboPrinter.Items.IndexOf(defaultPrinter);
                    if (index >= 0)
                    {
                        cboPrinter.SelectedIndex = index;
                        string status = PrintingService.GetPrinterStatus(defaultPrinter);
                        lblPrinterStatus.Text = $"Default: {defaultPrinter} - {status}";
                        lblPrinterStatus.ForeColor = status.Contains("Error") ? Color.Red : Color.Green;
                        return;
                    }
                }

                // Select first printer if no default found
                if (cboPrinter.Items.Count > 0)
                {
                    cboPrinter.SelectedIndex = 0;
                    string firstPrinter = printers[0];
                    string status = PrintingService.GetPrinterStatus(firstPrinter);
                    lblPrinterStatus.Text = $"{printers.Count} printer(s) found - {status}";
                    lblPrinterStatus.ForeColor = status.Contains("Error") ? Color.Red : Color.Green;
                }
            }
            catch (Exception ex)
            {
                cboPrinter.Items.Clear();
                cboPrinter.Items.Add($"Error loading printers");
                cboPrinter.SelectedIndex = 0;
                lblPrinterStatus.Text = $"Error: {ex.Message}";
                lblPrinterStatus.ForeColor = Color.Red;
            }
        }

        private void LoadDesigns()
        {
            cboDesign.Items.Clear();
            foreach (var d in Database.GetDesigns())
            {
                cboDesign.Items.Add(new ComboItem { Id = d.Id, Text = $"{d.Name} (#{d.Id})" });
            }
            if (cboDesign.Items.Count > 0) 
            {
                cboDesign.SelectedIndex = 0;
                // Trigger preview update after selection
                _ = UpdatePreviewAsync();
            }
            else
            {
                // Show no design message if no designs available
                pbPreview.Image?.Dispose();
                pbPreview.Image = CreateNoPreviewImage();
            }
        }

        public void RefreshDesigns()
        {
            LoadDesigns();
        }

        private class ComboItem { public int Id; public string Text=""; public override string ToString()=>Text; }
        private int SelectedDesignId => (cboDesign.SelectedItem as ComboItem)?.Id ?? 0;

        private async Task UpdatePreviewAsync()
        {
            if (SelectedDesignId == 0) 
            {
                pbPreview.Image?.Dispose();
                pbPreview.Image = CreateNoPreviewImage();
                return;
            }
            
            if (!int.TryParse(txtStart.Text, out int start)) start = Database.GetLastPrintedSerial(SelectedDesignId) + 1;
            var prnTemplate = Database.GetPrnByDesignId(SelectedDesignId);
            var prn = ShortcutCodeService.ReplaceShortcutCodes(prnTemplate, start);
            var size = Database.GetDesignSize(SelectedDesignId);
            System.Drawing.Image? img = null;

            // First, try to use uploaded preview image
            var previewPath = Database.GetDesignPreviewPath(SelectedDesignId);
            if (!string.IsNullOrWhiteSpace(previewPath) && System.IO.File.Exists(previewPath))
            {
                pbPreview.Image?.Dispose();
                pbPreview.Image = FileHelper.LoadImageSafely(previewPath);
                return;
            }

            // Second, try ZPL rendering if it's ZPL format
            var type = PrnDetector.Detect(prn);
            if (type == PrnType.ZPL)
            {
                try
                {
                    img = await PreviewService.RenderZplToImageAsync(prn, size.w, size.h, size.d);
                    if (img != null)
                    {
                        pbPreview.Image?.Dispose();
                        pbPreview.Image = img;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    // Don't show error message, just fall back to text preview
                    System.Diagnostics.Debug.WriteLine($"ZPL preview failed: {ex.Message}");
                }
            }

            // Fallback: Create a better-formatted text preview
            pbPreview.Image?.Dispose();
            pbPreview.Image = CreateFormattedPreview(prn, type);
        }

        private Image CreateNoPreviewImage()
        {
            var bmp = new Bitmap(400, 300);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.White);
            
            // Draw border
            using var borderPen = new Pen(Color.LightGray, 2);
            g.DrawRectangle(borderPen, 1, 1, bmp.Width - 3, bmp.Height - 3);
            
            using var titleFont = new Font("Arial", 16, FontStyle.Bold);
            using var textFont = new Font("Arial", 12);
            
            string message = "No Design Selected";
            string subMessage = "Please select a design to preview";
            
            var titleSize = g.MeasureString(message, titleFont);
            var textSize = g.MeasureString(subMessage, textFont);
            
            g.DrawString(message, titleFont, Brushes.DarkGray, 
                (bmp.Width - titleSize.Width) / 2, (bmp.Height - titleSize.Height) / 2 - 20);
            g.DrawString(subMessage, textFont, Brushes.Gray, 
                (bmp.Width - textSize.Width) / 2, (bmp.Height - textSize.Height) / 2 + 20);
                
            return bmp;
        }

        private Image CreateFormattedPreview(string prn, PrnType type)
        {
            var bmp = new Bitmap(400, 300);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.White);
            
            // Draw border
            using var borderPen = new Pen(Color.Black, 2);
            g.DrawRectangle(borderPen, 1, 1, bmp.Width - 3, bmp.Height - 3);
            
            // Draw header
            using var headerFont = new Font("Arial", 12, FontStyle.Bold);
            using var contentFont = new Font("Consolas", 8);
            
            string header = $"Label Preview - {type} Format";
            g.DrawString(header, headerFont, Brushes.DarkBlue, 10, 10);
            
            // Draw formatted content
            string content = FormatPrnForDisplay(prn, type);
            var contentRect = new RectangleF(10, 35, bmp.Width - 20, bmp.Height - 45);
            g.DrawString(content, contentFont, Brushes.Black, contentRect);
            
            return bmp;
        }

        private string FormatPrnForDisplay(string prn, PrnType type)
        {
            if (string.IsNullOrWhiteSpace(prn))
                return "No PRN code available";
                
            switch (type)
            {
                case PrnType.ZPL:
                    return FormatZplForDisplay(prn);
                case PrnType.EPL:
                    return "EPL Format Detected\n\nPreview not available for EPL.\nPlease upload a preview image in Design tab.";
                case PrnType.CPCL:
                    return "CPCL Format Detected\n\nPreview not available for CPCL.\nPlease upload a preview image in Design tab.";
                case PrnType.TSPL:
                    return "TSPL Format Detected\n\nPreview not available for TSPL.\nPlease upload a preview image in Design tab.";
                case PrnType.DPL:
                    return "DPL Format Detected\n\nPreview not available for DPL.\nPlease upload a preview image in Design tab.";
                default:
                    return $"Unknown Format\n\nRaw PRN Code:\n{prn.Substring(0, Math.Min(prn.Length, 200))}...";
            }
        }

        private string FormatZplForDisplay(string zpl)
        {
            var lines = zpl.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var formatted = new System.Text.StringBuilder();
            
            formatted.AppendLine("ZPL Format Detected");
            formatted.AppendLine("API preview failed - showing formatted code:");
            formatted.AppendLine();
            
            foreach (var line in lines.Take(10)) // Show first 10 lines
            {
                var trimmedLine = line.Trim();
                if (!string.IsNullOrWhiteSpace(trimmedLine))
                {
                    formatted.AppendLine(trimmedLine);
                }
            }
            
            if (lines.Length > 10)
            {
                formatted.AppendLine("...");
                formatted.AppendLine($"({lines.Length - 10} more lines)");
            }
            
            return formatted.ToString();
        }

        private void BtnPrint_Click(object? sender, EventArgs e)
        {
            if (SelectedDesignId == 0) { MessageBox.Show("Select a design"); return; }
            if (!int.TryParse(txtStart.Text, out int start)) { MessageBox.Show("Invalid start serial"); return; }
            
            string selectedPrinter = cboPrinter.SelectedItem?.ToString() ?? "";
            if (string.IsNullOrEmpty(selectedPrinter) || selectedPrinter.Contains("No printers found") || selectedPrinter.Contains("Error loading printers"))
            {
                MessageBox.Show("Please select a valid printer. Click 'Refresh' to reload printers.");
                return;
            }

            int qty = (int)numQty.Value;
            var prnTemplate = Database.GetPrnByDesignId(SelectedDesignId);
            
            try
            {
                for (int i = 0; i < qty; i++)
                {
                    int serial = start + i;
                    var prn = ShortcutCodeService.ReplaceShortcutCodes(prnTemplate, serial);
                    var ok = PrintingService.PrintRaw(selectedPrinter, prn);
                    if (!ok)
                    {
                        MessageBox.Show($"Failed to print label {serial}. Stopping print job.");
                        return;
                    }
                    Database.LogPrintedSerial(SelectedDesignId, serial);
                }
                MessageBox.Show($"Successfully printed {qty} labels starting from {start} to printer '{selectedPrinter}'");
                
                // Notify that printing is completed to refresh reports
                PrintCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Printing failed: {ex.Message}\n\nTip: Make sure the printer is online and properly configured for raw/direct printing.");
            }
        }

        private void BtnTestPrint_Click(object? sender, EventArgs e)
        {
            string selectedPrinter = cboPrinter.SelectedItem?.ToString() ?? "";
            if (string.IsNullOrEmpty(selectedPrinter) || selectedPrinter.Contains("No printers found") || selectedPrinter.Contains("Error loading printers"))
            {
                MessageBox.Show("Please select a valid printer first.");
                return;
            }

            try
            {
                lblPrinterStatus.Text = "Testing printer...";
                lblPrinterStatus.ForeColor = Color.Blue;
                Application.DoEvents(); // Update UI

                bool success = PrintingService.TestPrinter(selectedPrinter);
                
                if (success)
                {
                    lblPrinterStatus.Text = $"Test successful - {selectedPrinter} is ready";
                    lblPrinterStatus.ForeColor = Color.Green;
                    MessageBox.Show($"Test print sent successfully to '{selectedPrinter}'!\n\nIf you have a ZPL-compatible label printer, you should see a test label with 'Test Print' text.", "Test Print Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    lblPrinterStatus.Text = $"Test failed - {selectedPrinter} may not support raw printing";
                    lblPrinterStatus.ForeColor = Color.Orange;
                    MessageBox.Show($"Test print failed on '{selectedPrinter}'.\n\nThis printer may not support raw/direct printing or may be offline.", "Test Print Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                lblPrinterStatus.Text = $"Test error - {ex.Message}";
                lblPrinterStatus.ForeColor = Color.Red;
                MessageBox.Show($"Test print error: {ex.Message}", "Test Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
