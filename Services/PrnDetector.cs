namespace LabelPrinterApp.Services
{
    public enum PrnType { Unknown, ZPL, EPL, CPCL, TSPL, DPL }

    public static class PrnDetector
    {
        public static PrnType Detect(string prn)
        {
            var s = (prn ?? string.Empty).Trim().ToUpperInvariant();
            if (s.Contains("^XA") || s.Contains("^FO") || s.Contains("^XZ")) return PrnType.ZPL;
            if (s.StartsWith("N\r") || s.Contains("A\\") || s.Contains("EPL")) return PrnType.EPL;
            if (s.Contains("! 0 200 200") || s.Contains("TONE")) return PrnType.CPCL;
            if (s.Contains("SIZE ") && s.Contains("GAP")) return PrnType.TSPL;
            if (s.Contains("NASC") || (s.Contains("TEXT") && s.Contains("DMATRIX"))) return PrnType.DPL;
            return PrnType.Unknown;
        }
    }
}
