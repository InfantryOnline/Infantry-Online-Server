using InfantryLauncher.Classes;
using InfantryLauncher.Protocol;
using Ini;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace InfantryLauncher.Forms
{
    public class RegisterForm : Form
    {
        public IniFile settings;
        private TextBox txtUsername;
        private Button btnRegister;
        private Label lblUsername;
        private Label lblPassword;
        private TextBox txtPassword;
        private Label lblEmail;
        private TextBox txtEmail;
        private string accountServer;

        public RegisterForm()
        {
            this.InitializeComponent();
            this.settings = new IniFile(Path.Combine(Directory.GetCurrentDirectory(), "settings.ini"));
            if (this.settings.Exists())
                this.settings.Load();
            ((Control) this.txtUsername).Select();
            this.AcceptButton = (IButtonControl) this.btnRegister;
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            this.accountServer = this.settings["Launcher"]["Accounts"];

            //Lets ping first
            AccountServer.PingRequestStatusCode ping = PingServer(this.settings["Launcher"]["Accounts"]);
            if (ping != AccountServer.PingRequestStatusCode.Ok)
            {
                //Try the backup
                if ((ping = PingServer(this.settings["Launcher"]["AccountsBackup"])) != AccountServer.PingRequestStatusCode.Ok)
                {
                    int num3 = (int)MessageBox.Show("Server error, could not connect. Is your firewall enabled?");
                    return;
                }
                this.accountServer = this.settings["Launcher"]["AccountsBackup"];
            }

            switch (AccountServer.RegisterAccount(new AccountServer.RegisterRequestObject()
            {
                Username = this.txtUsername.Text.Trim(),
                PasswordHash = Md5.Hash(this.txtPassword.Text.Trim()),
                Email = this.txtEmail.Text.Trim()
            }, this.accountServer))

            {
                case AccountServer.RegistrationStatusCode.Ok:
                    int num1 = (int) MessageBox.Show("Account created!");
                    break;
                case AccountServer.RegistrationStatusCode.MalformedData:
                    int num2 = (int) MessageBox.Show("Error: malformed username/password");
                    break;
                case AccountServer.RegistrationStatusCode.UsernameTaken:
                    int num3 = (int) MessageBox.Show("Username is taken");
                    break;
                case AccountServer.RegistrationStatusCode.EmailTaken:
                    int num4 = (int)MessageBox.Show("Email is already used");
                    break;
                case AccountServer.RegistrationStatusCode.WeakCredentials:
                    string msg = "(too short/invalid characters/invalid email)";
                    int num5 = (int) MessageBox.Show("Credentials are invalid: " + (AccountServer.Reason != null ? AccountServer.Reason : msg));
                    break;
                case AccountServer.RegistrationStatusCode.ServerError:
                    string defaultMsg = "Server error, could not connect. Is your firewall enabled?";
                    int num6 = (int) MessageBox.Show((AccountServer.Reason != null ? AccountServer.Reason : defaultMsg));
                    break;
            }
        }

        private void RegisterForm_Load(object sender, EventArgs e)
        {
        }

        private AccountServer.PingRequestStatusCode PingServer(string url)
        {
            switch (AccountServer.PingAccount(url))
            {
                default:
                case AccountServer.PingRequestStatusCode.NotFound:
                    return AccountServer.PingRequestStatusCode.NotFound;
                case AccountServer.PingRequestStatusCode.Ok:
                    return AccountServer.PingRequestStatusCode.Ok;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && this.Container != null)
                this.Container.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RegisterForm));
            this.txtUsername = new System.Windows.Forms.TextBox();
            this.btnRegister = new System.Windows.Forms.Button();
            this.lblUsername = new System.Windows.Forms.Label();
            this.lblPassword = new System.Windows.Forms.Label();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.lblEmail = new System.Windows.Forms.Label();
            this.txtEmail = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // txtUsername
            // 
            this.txtUsername.Location = new System.Drawing.Point(12, 25);
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.Size = new System.Drawing.Size(166, 20);
            this.txtUsername.TabIndex = 0;
            // 
            // btnRegister
            // 
            this.btnRegister.Location = new System.Drawing.Point(58, 178);
            this.btnRegister.Name = "btnRegister";
            this.btnRegister.Size = new System.Drawing.Size(75, 23);
            this.btnRegister.TabIndex = 3;
            this.btnRegister.Text = "Register";
            this.btnRegister.UseVisualStyleBackColor = true;
            this.btnRegister.Click += new System.EventHandler(this.btnRegister_Click);
            // 
            // lblUsername
            // 
            this.lblUsername.AutoSize = true;
            this.lblUsername.Location = new System.Drawing.Point(12, 9);
            this.lblUsername.Name = "lblUsername";
            this.lblUsername.Size = new System.Drawing.Size(55, 13);
            this.lblUsername.TabIndex = 4;
            this.lblUsername.Text = "Username";
            // 
            // lblPassword
            // 
            this.lblPassword.AutoSize = true;
            this.lblPassword.Location = new System.Drawing.Point(12, 65);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(53, 13);
            this.lblPassword.TabIndex = 6;
            this.lblPassword.Text = "Password";
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(12, 81);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.PasswordChar = '*';
            this.txtPassword.Size = new System.Drawing.Size(166, 20);
            this.txtPassword.TabIndex = 1;
            // 
            // lblEmail
            // 
            this.lblEmail.AutoSize = true;
            this.lblEmail.Location = new System.Drawing.Point(12, 119);
            this.lblEmail.Name = "lblEmail";
            this.lblEmail.Size = new System.Drawing.Size(32, 13);
            this.lblEmail.TabIndex = 8;
            this.lblEmail.Text = "Email";
            // 
            // txtEmail
            // 
            this.txtEmail.Location = new System.Drawing.Point(12, 135);
            this.txtEmail.Name = "txtEmail";
            this.txtEmail.Size = new System.Drawing.Size(166, 20);
            this.txtEmail.TabIndex = 2;
            // 
            // RegisterForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(190, 214);
            this.Controls.Add(this.lblEmail);
            this.Controls.Add(this.txtEmail);
            this.Controls.Add(this.lblPassword);
            this.Controls.Add(this.txtPassword);
            this.Controls.Add(this.lblUsername);
            this.Controls.Add(this.btnRegister);
            this.Controls.Add(this.txtUsername);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "RegisterForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Create Account";
            this.Load += new System.EventHandler(this.RegisterForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
