using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using InfLauncher.Controllers;
using InfLauncher.Helpers;
using InfLauncher.Models;

namespace InfLauncher.Views
{
    public partial class MainForm : Form
    {
        private MainController _controller;

        public MainForm()
        {
            InitializeComponent();
        }

        public MainForm(MainController controller)
        {
            InitializeComponent();

            _controller = controller;
            loadSettings();
        }

        public void SetNews(News news)
        {
            lblNewsTitle.Text = news.Title;
            lblNewsDescription.Text = news.Description;

            lblNewsLink.Tag = news.URL;
        }

        public void launchGame()
        {
            var infantryProcess = new Process();
            infantryProcess.StartInfo.FileName = Path.Combine(Config.GetConfig().InstallPath, "infantry.exe");
            infantryProcess.StartInfo.Arguments = string.Format("/ticket:{0} /name:{1}", _controller.GetSessionId(), _controller._account);
            infantryProcess.Start();
            Application.Exit();
        }

        public void loadSettings()
        {
            var username = Properties.Settings.Default.username;
            var password = Properties.Settings.Default.password;

            txtboxUsername.Select();
            if (username.Length > 0)
            {
                txtboxUsername.Text = username;
                if (password.Length > 0)
                {
                    txtboxPassword.Text = password;
                    checkPass.Checked = true;
                }
                txtboxPassword.Select();
            }
            this.AcceptButton = btnLogin;
        }

        public void rememberSettings()
        {
            var settings = Properties.Settings.Default;

            settings.username = txtboxUsername.Text;
            settings.password = checkPass.Checked ? txtboxPassword.Text : "";
            settings.Save();            
        }

        #region View Handlers

        private void btnLogin_Click(object sender, System.EventArgs e)
        {
            var username = txtboxUsername.Text;
            var password = txtboxPassword.Text;

            if(!Account.IsValidUsername(username))
            {
                MessageBox.Show(@"Username must be longer than 4 characters. Please register if you haven't yet!");
                return;
            }

            if(!Account.IsValidPassword(password))
            {
                MessageBox.Show(@"Password can't be left empty.");
                return;
            }

            rememberSettings();
            _controller._account = username;
            _controller.LoginAccount(new Account.AccountLoginRequestModel(username, password));
        }

        private void btnNewAccount_Click(object sender, System.EventArgs e)
        {
            _controller.CreateNewAccountForm();
        }

        private void linkWebsite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://www.freeinfantry.org");
        }

        #endregion

        private void lblNewsLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            LinkLabel label = (LinkLabel) sender;
            Process.Start((string) label.Tag);
        }
    }
}
