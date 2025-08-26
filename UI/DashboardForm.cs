   using System.Windows.Forms;

namespace LabelPrinterApp.UI
{
    public class DashboardForm : Form
    {
        TabControl tabs = new TabControl();
        DesignForm designForm;
        PrintForm printForm;
        ReportsForm reportsForm;

        public DashboardForm(string username = "")
        {
            Text = string.IsNullOrWhiteSpace(username) 
                ? "Label Printer - Dashboard" 
                : $"Label Printer - Dashboard (Logged in as: {username})";
            Width = 1200; Height = 800;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new System.Drawing.Size(1000, 600);

            tabs.Dock = DockStyle.Fill;
            tabs.Padding = new System.Drawing.Point(10, 5);
            
            var tpDesign = new TabPage("Design Label");
            var tpPrint = new TabPage("Print Label");
            var tpReports = new TabPage("Reports");

            tpDesign.Padding = new Padding(5);
            tpPrint.Padding = new Padding(5);
            tpReports.Padding = new Padding(5);

            // Create form instances and keep references
            designForm = new DesignForm(){ Dock = DockStyle.Fill };
            printForm = new PrintForm(){ Dock = DockStyle.Fill };
            reportsForm = new ReportsForm(){ Dock = DockStyle.Fill };

            // Set up communication between forms
            designForm.DesignSaved += () => printForm.RefreshDesigns();
            printForm.PrintCompleted += () => reportsForm.RefreshLogs();

            tpDesign.Controls.Add(designForm);
            tpPrint.Controls.Add(printForm);
            tpReports.Controls.Add(reportsForm);

            tabs.TabPages.Add(tpDesign);
            tabs.TabPages.Add(tpPrint);
            tabs.TabPages.Add(tpReports);

            Controls.Add(tabs);

            // Set default tab based on user
            if (username.ToLower() == "admin")
            {
                // For admin user, open Print Label tab by default
                tabs.SelectedTab = tpPrint;
                
                // Ensure print form is ready with latest designs
                this.Load += (s, e) => {
                    // Small delay to ensure UI is fully loaded, then refresh designs
                    var timer = new System.Windows.Forms.Timer { Interval = 100 };
                    timer.Tick += (ts, te) => {
                        timer.Stop();
                        printForm.RefreshDesigns(); // Make sure designs are loaded for admin
                    };
                    timer.Start();
                };
            }
            else
            {
                // For other users, open Design Label tab by default
                tabs.SelectedTab = tpDesign;
            }
        }
    }
}
