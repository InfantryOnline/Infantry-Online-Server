using System;
using System.Drawing;
using System.Windows.Forms;

using Infantry_Launcher.Controllers;

namespace Infantry_Launcher
{
    public partial class RecoveryForm : Form
    {
        public RecoveryForm()
        {
            InitializeComponent();
            MaximizeBox = false;

            AcceptButton = OKButton;
            CancelButton = CANCELButton;
        }

        #region Button Events
        private void UsernameButton_Click(object sender, EventArgs e)
        {
            RecoveryMessage.Text = "Enter the email associated with the account.";
            RecoveryMessage.Location = new Point(RecoveryMessage.Text.Length / 3, RecoveryMessage.Location.Y);
            UsernameSetup();
        }

        private void PasswordButton_Click(object sender, EventArgs e)
        {
            RecoveryMessage.Text = "Enter the username associated with the account.";
            RecoveryMessage.Location = new Point(5, RecoveryMessage.Location.Y);
            PasswordSetup();
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            string email = string.Empty;
            string username = string.Empty;
            if (textBox == null)
            {
                MessageBox.Show("You must pick a recovery method first.\r\nHit cancel to return to the launcher.", "Recovery Error");
                return;
            }

            if (!Password && string.IsNullOrEmpty(textBox.Text))
            {
                MessageBox.Show("Email cannot be blank.", "Recovery Error", MessageBoxButtons.OK);
                return;
            }
            else
            {
                if (string.IsNullOrEmpty(textBox.Text))
                {
                    MessageBox.Show("Username cannot be blank.", "Recovery Error", MessageBoxButtons.OK);
                    return;
                }
            }

            if (!Password)
                email = textBox.Text.Trim();
            else
                username = textBox.Text.Trim();

            Cursor.Current = Cursors.WaitCursor;
            if (!AccountController.RecoveryRequest(username, email, Password))
            {
                Cursor.Current = Cursors.Default;
                return;
            }

            Cursor.Current = Cursors.Default;
            Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }
        #endregion

        private void UsernameSetup()
        {
            int Ux = UsernameButton.Location.X, Uy = UsernameButton.Location.Y;
            int Px = PasswordButton.Location.X, Py = PasswordButton.Location.Y;

            Label emailLabel = new Label();
            emailLabel.Name = "EmailLabel";
            emailLabel.Text = "Email";
            emailLabel.Location = new Point(Ux + emailLabel.Text.Length * 3, Uy + emailLabel.Text.Length * 3);
            emailLabel.Size = new Size(159, 22);
            emailLabel.Anchor = AnchorStyles.Left;
            emailLabel.AutoSize = true;
            emailLabel.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            Controls.Add(emailLabel);

            textBox = new TextBox();
            textBox.Name = "EmailTextBox";
            textBox.Location = new Point(Px / 5 - emailLabel.Text.Length, Py);
            textBox.Text = string.Empty;
            textBox.Size = new Size(259, 22);
            textBox.Anchor = AnchorStyles.Left;
            textBox.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            Controls.Add(textBox);

            UsernameButton.Dispose();
            PasswordButton.Dispose();

            emailLabel.Show();
            textBox.Show();

            Password = false;
        }

        private void PasswordSetup()
        {
            int Ux = UsernameButton.Location.X, Uy = UsernameButton.Location.Y;
            int Px = PasswordButton.Location.X, Py = PasswordButton.Location.Y;

            Label usernameLabel = new Label();
            usernameLabel.Name = "UsernameLabel";
            usernameLabel.Text = "Username";
            usernameLabel.Location = new Point(Ux + 3, Uy + usernameLabel.Text.Length * 3);
            usernameLabel.Size = new Size(159, 22);
            usernameLabel.Anchor = AnchorStyles.Left;
            usernameLabel.AutoSize = true;
            usernameLabel.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            Controls.Add(usernameLabel);

            textBox = new TextBox();
            textBox.Name = "UsernameTextBox";
            textBox.Location = new Point(Px - PasswordButton.Size.Width / 2, Py);
            textBox.Text = string.Empty;
            textBox.Size = new Size(159, 22);
            textBox.Anchor = AnchorStyles.Left;
            textBox.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            Controls.Add(textBox);

            UsernameButton.Dispose();
            PasswordButton.Dispose();

            usernameLabel.Show();
            textBox.Show();

            Password = true;
        }

        private TextBox textBox;
        private bool Password;
    }
}
