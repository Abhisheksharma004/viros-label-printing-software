using System.Drawing;
using System.IO;

namespace LabelPrinterApp.Services
{
    public static class FileHelper
    {
        public static Image? LoadImageSafely(string path)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                return Image.FromStream(fs);
            }
            catch { return null; }
        }
    }
}
