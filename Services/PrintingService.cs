using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Text;

namespace LabelPrinterApp.Services
{
    public static class PrintingService
    {
        public static bool PrintRaw(string printerName, string data)
        {
            // If no printer specified, try to find a default printer
            if (string.IsNullOrWhiteSpace(printerName))
            {
                printerName = GetDefaultPrinter();
                if (string.IsNullOrWhiteSpace(printerName))
                {
                    throw new InvalidOperationException("No printer specified and no default printer found.");
                }
            }

            // Validate printer exists
            if (!PrinterExists(printerName))
            {
                throw new InvalidOperationException($"Printer '{printerName}' not found. Available printers: {string.Join(", ", GetAvailablePrinters())}");
            }

            try
            {
                // Try direct raw printing first
                return SendRawDataToPrinter(printerName, data);
            }
            catch (Exception ex)
            {
                // If direct printing fails, try alternative method
                try
                {
                    return PrintViaFile(printerName, data);
                }
                catch (Exception ex2)
                {
                    throw new InvalidOperationException($"Failed to print to '{printerName}' using both direct and file methods. Direct error: {ex.Message}. File error: {ex2.Message}");
                }
            }
        }

        private static bool PrintViaFile(string printerName, string data)
        {
            try
            {
                // Create a temporary file
                string tempFile = Path.GetTempFileName();
                
                // Write data to the temp file
                File.WriteAllText(tempFile, data, Encoding.UTF8);
                
                // Use Windows command to copy file to printer
                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = $"/c copy /b \"{tempFile}\" \"\\\\localhost\\{printerName}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                
                process.Start();
                process.WaitForExit(10000); // Wait up to 10 seconds
                
                // Clean up temp file
                try { File.Delete(tempFile); } catch { }
                
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"File-based printing failed: {ex.Message}", ex);
            }
        }

        public static List<string> GetAvailablePrinters()
        {
            var printers = new List<string>();
            foreach (string printerName in PrinterSettings.InstalledPrinters)
            {
                printers.Add(printerName);
            }
            return printers;
        }

        public static string GetDefaultPrinter()
        {
            try
            {
                var printDocument = new PrintDocument();
                return printDocument.PrinterSettings.PrinterName;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static bool PrinterExists(string printerName)
        {
            foreach (string installedPrinter in PrinterSettings.InstalledPrinters)
            {
                if (string.Equals(installedPrinter, printerName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool TestPrinter(string printerName)
        {
            try
            {
                string testData = "^XA^FO50,50^ADN,36,20^FDTest Print^FS^XZ"; // Simple ZPL test
                return PrintRaw(printerName, testData);
            }
            catch
            {
                return false;
            }
        }

        public static string GetPrinterStatus(string printerName)
        {
            try
            {
                var printerSettings = new PrinterSettings();
                printerSettings.PrinterName = printerName;
                
                if (!printerSettings.IsValid)
                    return "Invalid printer";
                
                if (!printerSettings.IsPlotter)
                    return "Ready (Document printer - may not support raw printing)";
                
                return "Ready (Raw printing supported)";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        private static bool SendRawDataToPrinter(string printerName, string data)
        {
            IntPtr hPrinter = IntPtr.Zero;
            DOCINFOA docInfo = new DOCINFOA();
            bool success = false;

            try
            {
                // Open the printer
                if (!OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
                {
                    throw new InvalidOperationException($"Cannot open printer '{printerName}'. Error: {Marshal.GetLastWin32Error()}");
                }

                // Set up the document info
                docInfo.pDocName = "Label Print Job";
                docInfo.pDataType = "RAW";
                docInfo.pOutputFile = null;

                // Start the document
                if (!StartDocPrinter(hPrinter, 1, docInfo))
                {
                    throw new InvalidOperationException($"Cannot start document on printer '{printerName}'. Error: {Marshal.GetLastWin32Error()}");
                }

                // Start the page
                if (!StartPagePrinter(hPrinter))
                {
                    EndDocPrinter(hPrinter);
                    throw new InvalidOperationException($"Cannot start page on printer '{printerName}'. Error: {Marshal.GetLastWin32Error()}");
                }

                // Convert string to bytes
                byte[] bytes = Encoding.UTF8.GetBytes(data);
                IntPtr pBytes = Marshal.AllocHGlobal(bytes.Length);
                Marshal.Copy(bytes, 0, pBytes, bytes.Length);

                // Write the data
                int written;
                if (!WritePrinter(hPrinter, pBytes, bytes.Length, out written))
                {
                    Marshal.FreeHGlobal(pBytes);
                    EndPagePrinter(hPrinter);
                    EndDocPrinter(hPrinter);
                    throw new InvalidOperationException($"Cannot write to printer '{printerName}'. Error: {Marshal.GetLastWin32Error()}");
                }

                Marshal.FreeHGlobal(pBytes);

                // End the page
                if (!EndPagePrinter(hPrinter))
                {
                    EndDocPrinter(hPrinter);
                    throw new InvalidOperationException($"Cannot end page on printer '{printerName}'. Error: {Marshal.GetLastWin32Error()}");
                }

                // End the document
                if (!EndDocPrinter(hPrinter))
                {
                    throw new InvalidOperationException($"Cannot end document on printer '{printerName}'. Error: {Marshal.GetLastWin32Error()}");
                }

                success = true;
            }
            finally
            {
                // Close the printer handle
                if (hPrinter != IntPtr.Zero)
                {
                    ClosePrinter(hPrinter);
                }
            }

            return success;
        }

        // Win32 API declarations for raw printing
        [DllImport("winspool.drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPStr)] string szPrinter, out IntPtr hPrinter, IntPtr pd);

        [DllImport("winspool.drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);

        [DllImport("winspool.drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPStr)] public string pDocName = "";
            [MarshalAs(UnmanagedType.LPStr)] public string? pOutputFile = null;
            [MarshalAs(UnmanagedType.LPStr)] public string pDataType = "";
        }
    }
}
