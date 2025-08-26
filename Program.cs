using System;
using System.Windows.Forms;
using LabelPrinterApp.Services;
using LabelPrinterApp.UI;

namespace LabelPrinterApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Database.Initialize();
            Application.Run(new LoginForm());
        }
    }
}
