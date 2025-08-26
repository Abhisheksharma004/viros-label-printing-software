using System;
using System.Windows.Forms;
using LabelPrinterApp.Services;

namespace LabelPrinterApp.UI
{
    public class LoginForm : Form
    {
        TextBox txtUser = new TextBox();
        TextBox txtPass = new TextBox();
        Button btnLogin = new Button();

        public LoginForm()
        {
            Text = "Login";
            Width = 320; Height = 200;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var lblU = new Label { Left = 20, Top = 25, Width = 70, Text = "Username" };
            var lblP = new Label { Left = 20, Top = 65, Width = 70, Text = "Password" };

            txtUser.Left = 100; txtUser.Top = 22; txtUser.Width = 180;
            txtPass.Left = 100; txtPass.Top = 62; txtPass.Width = 180; txtPass.PasswordChar = '•';

            btnLogin.Left = 100; btnLogin.Top = 110; btnLogin.Width = 80; btnLogin.Height = 30; btnLogin.Text = "Login";
            btnLogin.Click += (s,e) => DoLogin();

            Controls.AddRange(new Control[]{lblU, txtUser, lblP, txtPass, btnLogin});
        }

        private void DoLogin()
        {
            var username = txtUser.Text.Trim();
            var password = txtPass.Text.Trim();
            
            if (Database.ValidateUser(username, password))
            {
                Hide();
                new DashboardForm(username).ShowDialog();
                Close();
            }
            else
            {
                MessageBox.Show("Invalid credentials (admin/admin).");
            }
        }
    }
}
