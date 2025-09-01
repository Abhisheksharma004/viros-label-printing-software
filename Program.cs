// using System;
// using System.Windows.Forms;
// using LabelPrinterApp.Services;
// using LabelPrinterApp.UI;

// namespace LabelPrinterApp
// {
//     internal static class Program
//     {
//         [STAThread]
//         static void Main()
//         {
//             Application.EnableVisualStyles();
//             Application.SetCompatibleTextRenderingDefault(false);
//             Database.Initialize();
//             Application.Run(new LoginForm());
//         }
//     }
// }

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
            // Expiry date check
            DateTime expiryDate = new DateTime(2025, 09, 30);
            if (DateTime.Now.Date > expiryDate)
            {
                MessageBox.Show($"This application has expired as of {expiryDate:yyyy-MM-dd} and will not open.", "License Expired", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Database.Initialize();
            Application.Run(new LoginForm());
        }
    }
}
