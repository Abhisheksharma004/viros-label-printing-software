   using System.Windows.Forms;

namespace LabelPrinterApp.UI
{
    public class DashboardForm : Form
    {
        TabControl tabs = new TabControl();
        DesignForm designForm;
        PrintForm printForm;
        ReportsForm reportsForm;
        private const string DESIGN_TAB_PASSWORD = "NewViros##9141";
        private bool isDesignTabAuthenticated = false;
        private TabPage tpDesign = null!;
        private TabPage tpPrint = null!;
        private TabPage tpReports = null!;

        public DashboardForm(string username = "")
        {
            Text = string.IsNullOrWhiteSpace(username) 
                ? "Label Printer - Dashboard (Siddharth Grease & Lubes Pvt. Ltd.)" 
                : $"Label Printer - Dashboard(Siddharth Grease & Lubes Pvt. Ltd.) (Logged in as: {username})";
            Width = 1200; Height = 800;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new System.Drawing.Size(1000, 600);

            tabs.Dock = DockStyle.Fill;
            tabs.Padding = new System.Drawing.Point(10, 5);
            
            tpDesign = new TabPage("Design Label");
            tpPrint = new TabPage("Print Label");
            tpReports = new TabPage("Reports");

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

            // Add password protection for Design Label tab (applies to ALL users including admin)
            tabs.Selecting += OnTabSelecting;

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
                // For other users, open Print Label tab by default (since Design Label requires password)
                tabs.SelectedTab = tpPrint;
            }
        }

        private void OnTabSelecting(object? sender, TabControlCancelEventArgs e)
        {
            // Check if user is trying to access Design Label tab
            if (e.TabPage == tpDesign && !isDesignTabAuthenticated)
            {
                // Cancel the tab selection temporarily
                e.Cancel = true;
                
                // Show password dialog
                if (ShowDesignTabPasswordDialog())
                {
                    isDesignTabAuthenticated = true;
                    // Now allow access to the Design tab
                    tabs.SelectedTab = tpDesign;
                }
                // If password is wrong, stay on current tab
            }
        }

        private bool ShowDesignTabPasswordDialog()
        {
            var passwordForm = new Form
            {
                Text = "Design Label Access",
                Width = 400,
                Height = 200,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var lblMessage = new Label
            {
                Text = "Enter password to access Design Label:",
                Left = 20,
                Top = 20,
                Width = 350,
                Height = 20
            };

            var txtPassword = new TextBox
            {
                Left = 20,
                Top = 50,
                Width = 340,
                Height = 25,
                PasswordChar = '•',
                Font = new System.Drawing.Font("Arial", 10)
            };

            var btnOK = new Button
            {
                Text = "OK",
                Left = 200,
                Top = 100,
                Width = 80,
                Height = 30,
                DialogResult = DialogResult.OK
            };

            var btnCancel = new Button
            {
                Text = "Cancel",
                Left = 290,
                Top = 100,
                Width = 80,
                Height = 30,
                DialogResult = DialogResult.Cancel
            };

            passwordForm.Controls.AddRange(new Control[] { lblMessage, txtPassword, btnOK, btnCancel });
            passwordForm.AcceptButton = btnOK;
            passwordForm.CancelButton = btnCancel;

            // Focus on password textbox when form loads
            passwordForm.Load += (s, e) => txtPassword.Focus();

            var result = passwordForm.ShowDialog(this);
            
            if (result == DialogResult.OK)
            {
                if (txtPassword.Text == DESIGN_TAB_PASSWORD)
                {
                    MessageBox.Show("Access granted to Design Label tab.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return true;
                }
                else
                {
                    MessageBox.Show("Incorrect password. Access denied.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }
            
            return false;
        }
    }
}
