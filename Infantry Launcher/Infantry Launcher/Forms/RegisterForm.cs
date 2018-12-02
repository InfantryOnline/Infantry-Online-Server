using System;
using System.Windows.Forms;

using Infantry_Launcher.Helpers;
using Infantry_Launcher.Controllers;

namespace Infantry_Launcher
{
    public partial class RegisterForm : Form
    {
        /// <summary>
        /// Will auto fill the username and password if the person has already filled it out before clicking register
        /// </summary>
        public void AutoFill(string username, string password)
        {
            UsernameBox.Text = username;
            PasswordBox.Text = password;
            //Lets activate the correct box control
            if (string.IsNullOrEmpty(username))
            { UsernameBox.Select(); }
            else if (string.IsNullOrEmpty(password))
            { PasswordBox.Select(); }
            else
            { EmailBox.Select(); }
        }

        public RegisterForm(IniFile Settings)
        {
            InitializeComponent();
            settings = Settings;

            //Activate our box control
            UsernameBox.Select();
            AcceptButton = RegisterButton;
        }

        private void ReminderBox_TextChanged(object sender, EventArgs e)
        {
            if (!warned)
            { Reminder(); }
        }

        private void RegisterButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UsernameBox.Text))
            {
                MessageBox.Show("Username cannot be blank.", "Register Error", MessageBoxButtons.OK);
                return;
            }

            if (string.IsNullOrWhiteSpace(PasswordBox.Text))
            {
                MessageBox.Show("Password cannot be blank.", "Register Error", MessageBoxButtons.OK);
                return;
            }

            if (string.IsNullOrWhiteSpace(EmailBox.Text))
            {
                MessageBox.Show("Email cannot be blank.", "Register Error", MessageBoxButtons.OK);
                return;
            }

            if (string.IsNullOrWhiteSpace(ReminderBox.Text))
            {
                if (MessageBox.Show("Are you sure you DON'T want to set a password reminder?", "Password Reminder", MessageBoxButtons.YesNo) == DialogResult.No)
                { return; }
            }

            settings["Credentials"]["Reminder"] = string.IsNullOrWhiteSpace(ReminderBox.Text) ? string.Empty : ReminderBox.Text;
            settings.Save();

            Cursor.Current = Cursors.WaitCursor;
            if (!AccountController.RegisterAccount(UsernameBox.Text.Trim(), PasswordBox.Text.Trim(), EmailBox.Text.Trim()))
            {
                Cursor.Current = Cursors.Default;
                return;
            }

            Cursor.Current = Cursors.Default;
            Close();
        }

        private void Reminder()
        {
            ReminderNote.Visible = true;
            warned = true;
        }

        private IniFile settings;
        private bool warned = false;
    }
}
