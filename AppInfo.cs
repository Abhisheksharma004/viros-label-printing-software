using System;

namespace LabelPrinterApp
{
    public static class AppInfo
    {
        // Core Application Information
        public const string Version = "1.0.0";
        public const string ApplicationName = "Viros Entrepreneurs - Label Printer Application";
        public const string ProductName = "Label Printer Application";
        
        // Company Branding
        public const string CompanyName = "Viros Entrepreneurs";
        public const string DeveloperName = "Viros Entrepreneurs";
        public const string DevelopmentTeam = "Viros IT Innovation Team";
        
        // Branding Colors (RGB values)
        public static class BrandColors
        {
            public static System.Drawing.Color PrimaryTeal => System.Drawing.Color.FromArgb(0, 150, 136);
            public static System.Drawing.Color SecondaryBlue => System.Drawing.Color.FromArgb(0, 123, 191);
            public static System.Drawing.Color DarkText => System.Drawing.Color.FromArgb(33, 37, 41);
            public static System.Drawing.Color LightGray => System.Drawing.Color.FromArgb(108, 117, 125);
            public static System.Drawing.Color BackgroundLight => System.Drawing.Color.FromArgb(248, 249, 250);
        }
        
        // Dynamic Information
        public static string CopyrightYear => DateTime.Now.Year.ToString();
        public static string FullCopyright => $"© {CopyrightYear} {CompanyName}. All Rights Reserved.";
        public static string BuildDate => "August 2025";
        
        // Product Description
        public static string Description => "Professional Label Design and Printing Solution";
        public static string Tagline => "Delivering innovative solutions for business efficiency";
        
        // Feature Highlights
        public static string[] Features => new[]
        {
            "Multi-format PRN support (ZPL, EPL, CPCL, TSPL, DPL)",
            "Live preview with Labelary API integration",
            "Advanced shortcut codes with serial formatting ({SERIAL}, {SERIAL1-5})",
            "Comprehensive print logging and reporting system",
            "Password-protected design access (NewViros##9141)",
            "Professional Windows Forms interface with modern styling",
            "Multi-printer support with status monitoring",
            "Database-driven design storage and management",
            "Reprint functionality with audit trails"
        };
        
        // Company Contact & Branding
        public static string CompanyWebsite => "www.virosentrepreneurs.com";
        public static string SupportEmail => "support@virosentrepreneurs.com";
        public static string TechnicalSupport => "tech@virosentrepreneurs.com";
    }
}
