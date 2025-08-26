using System;
using System.Collections.Generic;

namespace LabelPrinterApp.Services
{
    public static class ShortcutCodeService
    {
        /// <summary>
        /// Gets all available shortcut codes and their descriptions
        /// </summary>
        public static Dictionary<string, string> GetAvailableShortcutCodes()
        {
            return new Dictionary<string, string>
            {
                { "{SERIAL}", "Serial number (e.g., 1, 2, 3...)" },
                { "{SERIAL1}", "Serial number 2-digit format (e.g., 01, 02, 03...)" },
                { "{SERIAL2}", "Serial number 3-digit format (e.g., 001, 002, 003...)" },
                { "{SERIAL3}", "Serial number 4-digit format (e.g., 0001, 0002, 0003...)" },
                { "{SERIAL4}", "Serial number 5-digit format (e.g., 00001, 00002, 00003...)" },
                { "{SERIAL5}", "Serial number 6-digit format (e.g., 000001, 000002, 000003...)" },
                { "{DATE}", "System Date (e.g., 26/08/2025)" },
                { "{DD}", "System Day (e.g., 26)" },
                { "{MM}", "System Month (e.g., 08)" },
                { "{YYYY}", "System Year (e.g., 2025)" },
                { "{YY}", "System Year Last 2 Digits (e.g., 25)" },
                { "{CHAR_MM}", "Month Character (01:A, 02:B, 03:C, 04:D, 05:E, 06:F, 07:G, 08:H, 09:I, 10:J, 11:K, 12:L)" },
                { "{TIME}", "Current Time (e.g., 11:01:25)" }
            };
        }

        /// <summary>
        /// Replaces all shortcut codes in the PRN content with actual values
        /// </summary>
        /// <param name="prnContent">The PRN content containing shortcut codes</param>
        /// <param name="serialNumber">The serial number to use for {SERIAL} replacement</param>
        /// <param name="customDateTime">Optional custom date/time. If null, uses current system time</param>
        /// <returns>PRN content with all shortcut codes replaced</returns>
        public static string ReplaceShortcutCodes(string prnContent, int serialNumber, DateTime? customDateTime = null)
        {
            if (string.IsNullOrEmpty(prnContent))
                return prnContent;

            var dateTime = customDateTime ?? DateTime.Now;
            var result = prnContent;

            // Replace all shortcut codes
            result = result.Replace("{SERIAL}", serialNumber.ToString());
            result = result.Replace("{SERIAL1}", serialNumber.ToString("D2"));    // 2-digit: 01, 02, 03...
            result = result.Replace("{SERIAL2}", serialNumber.ToString("D3"));    // 3-digit: 001, 002, 003...
            result = result.Replace("{SERIAL3}", serialNumber.ToString("D4"));    // 4-digit: 0001, 0002, 0003...
            result = result.Replace("{SERIAL4}", serialNumber.ToString("D5"));    // 5-digit: 00001, 00002, 00003...
            result = result.Replace("{SERIAL5}", serialNumber.ToString("D6"));    // 6-digit: 000001, 000002, 000003...
            result = result.Replace("{DATE}", dateTime.ToString("dd/MM/yyyy"));
            result = result.Replace("{DD}", dateTime.ToString("dd"));
            result = result.Replace("{MM}", dateTime.ToString("MM"));
            result = result.Replace("{YYYY}", dateTime.ToString("yyyy"));
            result = result.Replace("{YY}", dateTime.ToString("yy"));
            result = result.Replace("{CHAR_MM}", GetMonthCharacter(dateTime.Month));
            result = result.Replace("{TIME}", dateTime.ToString("HH:mm:ss"));

            return result;
        }

        /// <summary>
        /// Replaces only date/time shortcut codes (excludes {SERIAL})
        /// Useful for preview where serial number is not relevant
        /// </summary>
        /// <param name="prnContent">The PRN content containing shortcut codes</param>
        /// <param name="customDateTime">Optional custom date/time. If null, uses current system time</param>
        /// <returns>PRN content with date/time shortcut codes replaced</returns>
        public static string ReplaceDateTimeShortcutCodes(string prnContent, DateTime? customDateTime = null)
        {
            if (string.IsNullOrEmpty(prnContent))
                return prnContent;

            var dateTime = customDateTime ?? DateTime.Now;
            var result = prnContent;

            // Replace only date/time codes, keep {SERIAL} for preview
            result = result.Replace("{DATE}", dateTime.ToString("dd/MM/yyyy"));
            result = result.Replace("{DD}", dateTime.ToString("dd"));
            result = result.Replace("{MM}", dateTime.ToString("MM"));
            result = result.Replace("{YYYY}", dateTime.ToString("yyyy"));
            result = result.Replace("{YY}", dateTime.ToString("yy"));
            result = result.Replace("{CHAR_MM}", GetMonthCharacter(dateTime.Month));
            result = result.Replace("{TIME}", dateTime.ToString("HH:mm:ss"));

            return result;
        }

        /// <summary>
        /// Replaces shortcut codes for preview purposes
        /// Shows sample values including a sample serial number
        /// </summary>
        /// <param name="prnContent">The PRN content containing shortcut codes</param>
        /// <param name="sampleSerialNumber">Sample serial number for preview (default: 1)</param>
        /// <param name="customDateTime">Optional custom date/time. If null, uses current system time</param>
        /// <returns>PRN content with all shortcut codes replaced with sample values</returns>
        public static string ReplaceShortcutCodesForPreview(string prnContent, int sampleSerialNumber = 1, DateTime? customDateTime = null)
        {
            return ReplaceShortcutCodes(prnContent, sampleSerialNumber, customDateTime);
        }

        /// <summary>
        /// Converts month number to character (01:A, 02:B, etc.)
        /// </summary>
        /// <param name="month">Month number (1-12)</param>
        /// <returns>Character representation of the month</returns>
        private static string GetMonthCharacter(int month)
        {
            return month switch
            {
                1 => "A",   // January
                2 => "B",   // February
                3 => "C",   // March
                4 => "D",   // April
                5 => "E",   // May
                6 => "F",   // June
                7 => "G",   // July
                8 => "H",   // August
                9 => "I",   // September
                10 => "J",  // October
                11 => "K",  // November
                12 => "L",  // December
                _ => "A"    // Default to A for invalid months
            };
        }

        /// <summary>
        /// Checks if the PRN content contains any shortcut codes
        /// </summary>
        /// <param name="prnContent">The PRN content to check</param>
        /// <returns>True if shortcut codes are found, false otherwise</returns>
        public static bool ContainsShortcutCodes(string prnContent)
        {
            if (string.IsNullOrEmpty(prnContent))
                return false;

            var shortcutCodes = GetAvailableShortcutCodes().Keys;
            foreach (var code in shortcutCodes)
            {
                if (prnContent.Contains(code))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets a formatted string showing all available shortcut codes with examples
        /// </summary>
        /// <returns>Formatted help text</returns>
        public static string GetShortcutCodesHelpText()
        {
            var dateTime = DateTime.Now;
            var helpText = "Available Shortcut Codes:\n\n";
            
            helpText += "SERIAL NUMBER FORMATS:\n";
            helpText += $"{"{SERIAL}",-12} → Simple serial number (e.g., 1, 2, 3, 123)\n";
            helpText += $"{"{SERIAL1}",-12} → 2-digit format (e.g., 01, 02, 03, 123)\n";
            helpText += $"{"{SERIAL2}",-12} → 3-digit format (e.g., 001, 002, 003, 123)\n";
            helpText += $"{"{SERIAL3}",-12} → 4-digit format (e.g., 0001, 0002, 0003, 0123)\n";
            helpText += $"{"{SERIAL4}",-12} → 5-digit format (e.g., 00001, 00002, 00003, 00123)\n";
            helpText += $"{"{SERIAL5}",-12} → 6-digit format (e.g., 000001, 000002, 000003, 000123)\n\n";
            
            helpText += "DATE & TIME FORMATS:\n";
            helpText += $"{"{DATE}",-12} → System Date (e.g., {dateTime:dd/MM/yyyy})\n";
            helpText += $"{"{DD}",-12} → System Day (e.g., {dateTime:dd})\n";
            helpText += $"{"{MM}",-12} → System Month (e.g., {dateTime:MM})\n";
            helpText += $"{"{YYYY}",-12} → System Year (e.g., {dateTime:yyyy})\n";
            helpText += $"{"{YY}",-12} → Year Last 2 Digits (e.g., {dateTime:yy})\n";
            helpText += $"{"{CHAR_MM}",-12} → Month Character (e.g., {GetMonthCharacter(dateTime.Month)})\n";
            helpText += $"{"{TIME}",-12} → Current Time (e.g., {dateTime:HH:mm:ss})\n\n";
            helpText += "Month Characters: 01:A, 02:B, 03:C, 04:D, 05:E, 06:F, 07:G, 08:H, 09:I, 10:J, 11:K, 12:L";
            
            return helpText;
        }
    }
}
