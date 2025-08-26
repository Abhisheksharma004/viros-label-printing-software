using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using LabelPrinterApp.Services;

namespace LabelPrinterApp.UI
{
    public class DesignForm : UserControl
    {
        public event Action? DesignSaved;
        
        TextBox txtName = new TextBox();
        TextBox txtPrnEditor = new TextBox();
        Button btnBrowse = new Button();
        Button btnPreview = new Button();
        Button btnSave = new Button();
        Button btnUploadPreview = new Button();
        Button btnShortcutHelp = new Button();

        PictureBox previewBox = new PictureBox();

        System.Windows.Forms.Timer debounce = new System.Windows.Forms.Timer();

        public DesignForm()
        {
            Dock = DockStyle.Fill;
            var left = new Panel { Dock = DockStyle.Left, Width = 560, Padding = new Padding(10) };
            var right = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            var lblName = new Label{ Text="Design Name", Left=10, Top=10, Width=120 };
            txtName.SetBounds(10, 35, 520, 24);

            var lblPrn = new Label{ Text="PRN Code", Left=10, Top=70, Width=100 };
            txtPrnEditor.Multiline = true; txtPrnEditor.ScrollBars = ScrollBars.Both; txtPrnEditor.AcceptsReturn = true;
            txtPrnEditor.SetBounds(10, 95, 520, 360);
            txtPrnEditor.TextChanged += (s,e)=> { debounce.Stop(); debounce.Start(); };

            btnBrowse.Text = "Browse PRN"; btnBrowse.SetBounds(10, 470, 100, 30);
            btnBrowse.Click += async (s,e) => await BrowsePrnAsync();

            btnPreview.Text = "Refresh Preview"; btnPreview.SetBounds(120, 470, 110, 30);
            btnPreview.Click += async (s,e) => await RenderPreviewAsync(txtPrnEditor.Text);

            btnUploadPreview.Text = "Upload Preview"; btnUploadPreview.SetBounds(240, 470, 120, 30);
            btnUploadPreview.Click += (s,e)=> UploadPreviewImage();

            btnShortcutHelp.Text = "Shortcut Codes"; btnShortcutHelp.SetBounds(10, 510, 120, 30);
            btnShortcutHelp.Click += (s,e)=> ShowShortcutHelp();

            btnSave.Text = "Save Design"; btnSave.SetBounds(370, 470, 100, 30);
            btnSave.Click += (s,e)=> SaveDesign();

            left.Controls.AddRange(new Control[]{lblName, txtName, lblPrn, txtPrnEditor, btnBrowse, btnPreview, btnUploadPreview, btnShortcutHelp, btnSave});

            // Create a full-screen preview panel that uses all available space
            var previewPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.LightGray, Padding = new Padding(15) };
            previewBox = new PictureBox 
            { 
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            
            // Add resize handler to ensure preview always fits properly
            previewPanel.Resize += (s, e) => {
                if (previewBox.Image != null)
                {
                    // Force refresh of zoom calculation
                    previewBox.Invalidate();
                }
            };
            
            previewPanel.Controls.Add(previewBox);
            right.Controls.Add(previewPanel);

            Controls.Add(right);
            Controls.Add(left);

            debounce.Interval = 700;
            debounce.Tick += async (s,e)=> { debounce.Stop(); await RenderPreviewAsync(txtPrnEditor.Text); };
        }

        private async Task BrowsePrnAsync()
        {
            using var ofd = new OpenFileDialog();
            ofd.Filter = "PRN files (*.prn)|*.prn|All files (*.*)|*.*";
            ofd.Title = "Select PRN File";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                var content = File.ReadAllText(ofd.FileName);
                txtPrnEditor.Text = content;
                await RenderPreviewAsync(content);
            }
        }

        private async Task RenderPreviewAsync(string prn)
        {
            if (string.IsNullOrWhiteSpace(prn))
            {
                previewBox.Image?.Dispose();
                previewBox.Image = CreateEmptyPreview();
                return;
            }

            // Replace shortcut codes for preview (use sample values)
            var previewPrn = ShortcutCodeService.ReplaceShortcutCodesForPreview(prn, 1);

            // Extract label dimensions from PRN code or use defaults
            var (width, height) = ExtractLabelDimensions(previewPrn);
            
            // Use default values if not found in PRN
            double w = width > 0 ? width : 4.0; // Default width in inches
            double h = height > 0 ? height : 6.0; // Default height in inches
            int d = 8; // Default DPMM

            var type = PrnDetector.Detect(previewPrn);
            if (type == PrnType.ZPL)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"Attempting ZPL preview render for {w}\" x {h}\" label...");
                    System.Diagnostics.Debug.WriteLine($"ZPL Content: {previewPrn.Substring(0, Math.Min(200, previewPrn.Length))}...");
                    
                    var img = await PreviewService.RenderZplToImageAsync(previewPrn, w, h, d);
                    if (img != null)
                    {
                        if (previewBox.Image != null) previewBox.Image.Dispose();
                        previewBox.Image = img;
                        System.Diagnostics.Debug.WriteLine($"ZPL preview rendered successfully! Image size: {img.Width}x{img.Height}");
                        return;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("ZPL preview returned null - API failed, will show fallback");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ZPL preview failed with exception: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Exception details: {ex}");
                }
            }
            
            // Fallback: create formatted preview
            previewBox.Image?.Dispose();
            previewBox.Image = CreateFormattedPreview(previewPrn, type);
        }

        private (double width, double height) ExtractLabelDimensions(string prn)
        {
            try
            {
                // Look for common ZPL dimension commands
                double width = 4.0, height = 6.0; // defaults
                
                var lines = prn.Split('\n');
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    
                    // ^PWnnn - Print Width in dots
                    if (trimmed.StartsWith("^PW"))
                    {
                        var pwValue = trimmed.Substring(3);
                        if (int.TryParse(pwValue, out var dots))
                        {
                            width = dots / 203.0; // Convert dots to inches (203 DPI)
                        }
                    }
                    
                    // ^LLnnn - Label Length in dots
                    if (trimmed.StartsWith("^LL"))
                    {
                        var llValue = trimmed.Substring(3);
                        if (int.TryParse(llValue, out var dots))
                        {
                            height = dots / 203.0; // Convert dots to inches (203 DPI)
                        }
                    }
                    
                    // Look for maximum field coordinates to estimate label size
                    if (trimmed.StartsWith("^FO"))
                    {
                        var coords = trimmed.Substring(3).Split(',');
                        if (coords.Length >= 2)
                        {
                            if (int.TryParse(coords[0], out var x) && int.TryParse(coords[1], out var y))
                            {
                                // Add some margin to the maximum coordinates
                                var estimatedWidth = (x + 100) / 203.0; // Add 100 dots margin
                                var estimatedHeight = (y + 100) / 203.0; // Add 100 dots margin
                                
                                if (estimatedWidth > width) width = estimatedWidth;
                                if (estimatedHeight > height) height = estimatedHeight;
                            }
                        }
                    }
                }
                
                // Round to reasonable values
                width = Math.Max(1.0, Math.Round(width * 2) / 2); // Round to nearest 0.5 inch, min 1 inch
                height = Math.Max(1.0, Math.Round(height * 2) / 2); // Round to nearest 0.5 inch, min 1 inch
                
                System.Diagnostics.Debug.WriteLine($"Extracted label dimensions: {width}\" x {height}\"");
                return (width, height);
            }
            catch
            {
                // Return defaults if parsing fails
                return (4.0, 6.0);
            }
        }

        private Image CreateEmptyPreview()
        {
            // Create a standard preview size that's easier to display
            int width = 600;
            int height = 400;
            var bmp = new Bitmap(width, height);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.White);
            
            using var font = new Font("Arial", 24, FontStyle.Bold);
            string message = "Enter PRN Code to Preview";
            var size = g.MeasureString(message, font);
            g.DrawString(message, font, Brushes.Gray, 
                (bmp.Width - size.Width) / 2, (bmp.Height - size.Height) / 2);
                
            return bmp;
        }

        private Image CreateFormattedPreview(string prn, PrnType type)
        {
            // Create a reasonable preview size
            int width = 600;
            int height = 400;
            var bmp = new Bitmap(width, height);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.White);
            
            // Draw header
            using var headerFont = new Font("Arial", 16, FontStyle.Bold);
            using var contentFont = new Font("Consolas", 9);
            
            string header = type == PrnType.ZPL 
                ? "ZPL Preview (API unavailable - showing code preview)" 
                : $"Design Preview - {type} Format";
            var headerColor = type == PrnType.ZPL ? Brushes.Orange : Brushes.DarkBlue;
            g.DrawString(header, headerFont, headerColor, 20, 20);
            
            // Draw content preview
            string content = FormatContentForPreview(prn, type);
            var contentRect = new RectangleF(20, 50, bmp.Width - 40, bmp.Height - 70);
            g.DrawString(content, contentFont, Brushes.Black, contentRect);
            
            return bmp;
        }

        private string FormatContentForPreview(string prn, PrnType type)
        {
            if (string.IsNullOrWhiteSpace(prn))
                return "No PRN code entered";
                
            switch (type)
            {
                case PrnType.ZPL:
                    return "ZPL Format Detected\n\nAPI preview failed.\nShowing code preview:\n\n" + 
                           FormatZplCode(prn);
                case PrnType.EPL:
                    return "EPL Format Detected\n\nPreview not available.\nConsider uploading a preview image.";
                case PrnType.CPCL:
                    return "CPCL Format Detected\n\nPreview not available.\nConsider uploading a preview image.";
                case PrnType.TSPL:
                    return "TSPL Format Detected\n\nPreview not available.\nConsider uploading a preview image.";
                case PrnType.DPL:
                    return "DPL Format Detected\n\nPreview not available.\nConsider uploading a preview image.";
                default:
                    return $"Unknown Format\n\nFirst 200 characters:\n{prn.Substring(0, Math.Min(prn.Length, 200))}...";
            }
        }

        private string FormatZplCode(string zpl)
        {
            var lines = zpl.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var formatted = new System.Text.StringBuilder();
            
            foreach (var line in lines.Take(8)) // Show first 8 lines
            {
                var trimmedLine = line.Trim();
                if (!string.IsNullOrWhiteSpace(trimmedLine))
                {
                    formatted.AppendLine(trimmedLine);
                }
            }
            
            if (lines.Length > 8)
            {
                formatted.AppendLine("...");
                formatted.AppendLine($"({lines.Length - 8} more lines)");
            }
            
            return formatted.ToString();
        }

        private void UploadPreviewImage()
        {
            using var ofd = new OpenFileDialog();
            ofd.Filter = "Image files (*.png;*.jpg)|*.png;*.jpg|All files (*.*)|*.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                var img = FileHelper.LoadImageSafely(ofd.FileName);
                if (img != null) previewBox.Image = img;
            }
        }

        private void SaveDesign()
        {
            var name = string.IsNullOrWhiteSpace(txtName.Text) ? "Untitled" : txtName.Text.Trim();
            var prn = txtPrnEditor.Text;
            
            // Extract label dimensions from PRN code or use defaults
            var (width, height) = ExtractLabelDimensions(prn);
            double w = width > 0 ? width : 4.0; // Default width in inches
            double h = height > 0 ? height : 6.0; // Default height in inches
            int d = 8; // Default DPMM
            
            var id = Database.SaveDesign(name, prn, w, h, d, 0, null);
            MessageBox.Show($"Saved design #{id} with dimensions {w:F1}\" x {h:F1}\"");
            
            // Notify that a design was saved
            DesignSaved?.Invoke();
        }

        private void ShowShortcutHelp()
        {
            var helpText = ShortcutCodeService.GetShortcutCodesHelpText();
            var helpForm = new Form
            {
                Text = "Shortcut Codes Help",
                Width = 600,
                Height = 500,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var helpTextBox = new TextBox
            {
                Text = helpText,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10),
                BackColor = Color.White
            };

            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            panel.Controls.Add(helpTextBox);

            var buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(10) };
            var insertButton = new Button 
            { 
                Text = "Insert Sample", 
                Width = 100, 
                Height = 30, 
                Anchor = AnchorStyles.Right 
            };
            insertButton.Left = buttonPanel.Width - insertButton.Width - 20;
            insertButton.Top = 10;
            insertButton.Click += (s, e) => {
                InsertSampleShortcutCodes();
                helpForm.Close();
            };

            var closeButton = new Button 
            { 
                Text = "Close", 
                Width = 80, 
                Height = 30, 
                Anchor = AnchorStyles.Right 
            };
            closeButton.Left = insertButton.Left - closeButton.Width - 10;
            closeButton.Top = 10;
            closeButton.Click += (s, e) => helpForm.Close();

            buttonPanel.Controls.Add(insertButton);
            buttonPanel.Controls.Add(closeButton);

            helpForm.Controls.Add(panel);
            helpForm.Controls.Add(buttonPanel);
            helpForm.ShowDialog();
        }

        private void InsertSampleShortcutCodes()
        {
            var sampleCode = @"^XA
^PW600
^FO50,50^A0N,30,30^FDSerial: {SERIAL}^FS
^FO50,100^A0N,25,25^FD2-digit: {SERIAL1}^FS
^FO50,150^A0N,25,25^FD3-digit: {SERIAL2}^FS
^FO50,200^A0N,25,25^FD4-digit: {SERIAL3}^FS
^FO50,250^A0N,20,20^FDDate: {DATE} Time: {TIME}^FS
^FO50,300^A0N,20,20^FDMonth: {CHAR_MM} Year: {YYYY}^FS
^XZ";

            txtPrnEditor.Text = sampleCode;
        }
    }
}
