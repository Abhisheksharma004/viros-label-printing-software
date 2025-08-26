using System;

namespace LabelPrinterApp.Models
{
    public class Design
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string PrnContent { get; set; } = "";
        public double WidthInches { get; set; } = 4;
        public double HeightInches { get; set; } = 6;
        public int Dpmm { get; set; } = 8;
        public string PreviewPath { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
