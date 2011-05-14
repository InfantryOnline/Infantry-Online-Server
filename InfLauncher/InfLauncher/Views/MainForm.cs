using System.Diagnostics;
using System.Windows.Forms;
using InfLauncher.Controllers;

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

            btnPlay.Enabled = false;
            _controller = controller;
        }

        public void SetPlayButtonState(bool enabled)
        {
            btnPlay.Enabled = enabled;
        }

        #region View Handlers

        private void btnLogin_Click(object sender, System.EventArgs e)
        {
            var username = txtboxUsername.Text;
            var password = txtboxPassword.Text;

            _controller.LoginAccount(username, password);
        }

        private void btnNewAccount_Click(object sender, System.EventArgs e)
        {
            _controller.CreateNewAccountForm();
        }

        private void btnPlay_Click(object sender, System.EventArgs e)
        {
            var infantryProcess = new Process();
            infantryProcess.StartInfo.FileName = "infantry.exe";
            infantryProcess.StartInfo.Arguments = string.Format("/ticket:{0} /name:{1}", _controller.GetSessionId(), "Jovan");

            infantryProcess.Start();
        }

        private void linkWebsite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://www.aaerox.com");
        }

        #endregion
    }
}
