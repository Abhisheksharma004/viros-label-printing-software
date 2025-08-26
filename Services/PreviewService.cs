using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;

namespace LabelPrinterApp.Services
{
    public static class PreviewService
    {
        public static async Task<Image?> RenderZplToImageAsync(string zpl, double widthInches, double heightInches, int dpmm)
        {
            try
            {
                // Create a fresh HttpClient for each request to avoid connection issues
                using var client = new HttpClient();
                
                // Ensure ZPL is properly formatted
                if (!zpl.StartsWith("^XA")) zpl = "^XA\n" + zpl;
                if (!zpl.EndsWith("^XZ")) zpl += "\n^XZ";
                
                // Use the actual label dimensions for accurate rendering
                // The preview will handle scaling for display
                var url = $"http://api.labelary.com/v1/printers/8dpmm/labels/{widthInches:F1}x{heightInches:F1}/0/";
                
                using var content = new StringContent(zpl, Encoding.UTF8, "application/x-www-form-urlencoded");
                
                // Set proper headers
                client.DefaultRequestHeaders.Add("User-Agent", "LabelPrinterApp/1.0");
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("image/png"));
                
                client.Timeout = TimeSpan.FromSeconds(15);
                
                System.Diagnostics.Debug.WriteLine($"Calling Labelary API: {url}");
                System.Diagnostics.Debug.WriteLine($"Label size: {widthInches}\" x {heightInches}\"");
                
                var resp = await client.PostAsync(url, content);
                
                System.Diagnostics.Debug.WriteLine($"Response Status: {resp.StatusCode}");
                
                if (!resp.IsSuccessStatusCode) 
                {
                    var errorContent = await resp.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Labelary API Error: {resp.StatusCode} - {errorContent}");
                    return null;
                }
                
                var bytes = await resp.Content.ReadAsByteArrayAsync();
                System.Diagnostics.Debug.WriteLine($"Received {bytes.Length} bytes from API");
                
                if (bytes.Length == 0) return null;
                
                using var ms = new MemoryStream(bytes);
                var image = Image.FromStream(ms);
                System.Diagnostics.Debug.WriteLine($"Successfully created image: {image.Width}x{image.Height} pixels for {widthInches}\"x{heightInches}\" label");
                return image;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preview service error: {ex.Message}");
                return null;
            }
        }
    }
}
