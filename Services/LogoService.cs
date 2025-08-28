using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace LabelPrinterApp.Services
{
    public static class LogoService
    {
        private static Image? _cachedLogo;
        
        public static Image GetLogo()
        {
            if (_cachedLogo != null) return _cachedLogo;
            
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "LabelPrinterApp.Resources.viros_logo.png";
                
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        _cachedLogo = Image.FromStream(stream);
                        return _cachedLogo;
                    }
                }
            }
            catch (Exception)
            {
                // Fallback: Create a simple logo with company name
                return CreateFallbackLogo();
            }
            
            return CreateFallbackLogo();
        }
        
        public static Image GetResizedLogo(int width, int height)
        {
            var originalLogo = GetLogo();
            var resized = new Bitmap(width, height);
            
            using (var graphics = Graphics.FromImage(resized))
            {
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(originalLogo, 0, 0, width, height);
            }
            
            return resized;
        }
        
        private static Image CreateFallbackLogo()
        {
            var bitmap = new Bitmap(300, 100);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.White);
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Draw Viros Entrepreneurs logo design
                // Draw "VE" stylized letters
                using (var veFont = new Font("Arial", 28, FontStyle.Bold))
                using (var veBrush = new SolidBrush(Color.FromArgb(0, 150, 136))) // Teal color
                {
                    graphics.DrawString("VE", veFont, veBrush, 10, 25);
                }
                
                // Draw company name
                using (var titleFont = new Font("Arial", 14, FontStyle.Bold))
                using (var titleBrush = new SolidBrush(Color.FromArgb(33, 37, 41))) // Dark color
                {
                    graphics.DrawString("Viros", titleFont, titleBrush, 80, 25);
                }
                
                using (var subtitleFont = new Font("Arial", 11, FontStyle.Regular))
                using (var subtitleBrush = new SolidBrush(Color.FromArgb(108, 117, 125))) // Gray color
                {
                    graphics.DrawString("Entrepreneurs", subtitleFont, subtitleBrush, 80, 50);
                }
                
                // Draw some decorative elements
                using (var accentBrush = new SolidBrush(Color.FromArgb(0, 123, 191)))
                {
                    // Small squares/rectangles as design elements
                    graphics.FillRectangle(accentBrush, 240, 30, 8, 8);
                    graphics.FillRectangle(accentBrush, 252, 30, 8, 8);
                    graphics.FillRectangle(accentBrush, 264, 30, 8, 8);
                    
                    graphics.FillRectangle(accentBrush, 246, 42, 8, 8);
                    graphics.FillRectangle(accentBrush, 258, 42, 8, 8);
                }
            }
            
            return bitmap;
        }
        
        public static PictureBox CreateLogoPictureBox(int width, int height, int left, int top)
        {
            var pictureBox = new PictureBox
            {
                Image = GetResizedLogo(width, height),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Width = width,
                Height = height,
                Left = left,
                Top = top,
                BackColor = Color.Transparent
            };
            
            return pictureBox;
        }
    }
}
