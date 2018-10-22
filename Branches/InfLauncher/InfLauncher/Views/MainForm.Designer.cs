namespace InfLauncher.Views
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.txtboxUsername = new System.Windows.Forms.TextBox();
            this.txtboxPassword = new System.Windows.Forms.TextBox();
            this.lblUsername = new System.Windows.Forms.Label();
            this.lblPassword = new System.Windows.Forms.Label();
            this.btnLogin = new System.Windows.Forms.Button();
            this.btnNewAccount = new System.Windows.Forms.Button();
            this.linkWebsite = new System.Windows.Forms.LinkLabel();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.lblNewsLink = new System.Windows.Forms.LinkLabel();
            this.lblNewsDescription = new System.Windows.Forms.Label();
            this.lblNewsTitle = new System.Windows.Forms.Label();
            this.checkPass = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtboxUsername
            // 
            this.txtboxUsername.Location = new System.Drawing.Point(18, 21);
            this.txtboxUsername.Name = "txtboxUsername";
            this.txtboxUsername.Size = new System.Drawing.Size(139, 20);
            this.txtboxUsername.TabIndex = 0;
            // 
            // txtboxPassword
            // 
            this.txtboxPassword.Location = new System.Drawing.Point(18, 60);
            this.txtboxPassword.Name = "txtboxPassword";
            this.txtboxPassword.PasswordChar = '*';
            this.txtboxPassword.Size = new System.Drawing.Size(139, 20);
            this.txtboxPassword.TabIndex = 1;
            // 
            // lblUsername
            // 
            this.lblUsername.AutoSize = true;
            this.lblUsername.Location = new System.Drawing.Point(15, 5);
            this.lblUsername.Name = "lblUsername";
            this.lblUsername.Size = new System.Drawing.Size(55, 13);
            this.lblUsername.TabIndex = 2;
            this.lblUsername.Text = "Username";
            // 
            // lblPassword
            // 
            this.lblPassword.AutoSize = true;
            this.lblPassword.Location = new System.Drawing.Point(15, 44);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(53, 13);
            this.lblPassword.TabIndex = 3;
            this.lblPassword.Text = "Password";
            // 
            // btnLogin
            // 
            this.btnLogin.Location = new System.Drawing.Point(51, 140);
            this.btnLogin.Name = "btnLogin";
            this.btnLogin.Size = new System.Drawing.Size(75, 23);
            this.btnLogin.TabIndex = 3;
            this.btnLogin.Text = "Login";
            this.btnLogin.UseVisualStyleBackColor = true;
            this.btnLogin.Click += new System.EventHandler(this.btnLogin_Click);
            // 
            // btnNewAccount
            // 
            this.btnNewAccount.Location = new System.Drawing.Point(44, 169);
            this.btnNewAccount.Name = "btnNewAccount";
            this.btnNewAccount.Size = new System.Drawing.Size(88, 23);
            this.btnNewAccount.TabIndex = 4;
            this.btnNewAccount.Text = "New Account";
            this.btnNewAccount.UseVisualStyleBackColor = true;
            this.btnNewAccount.Click += new System.EventHandler(this.btnNewAccount_Click);
            // 
            // linkWebsite
            // 
            this.linkWebsite.AutoSize = true;
            this.linkWebsite.Location = new System.Drawing.Point(117, 201);
            this.linkWebsite.Name = "linkWebsite";
            this.linkWebsite.Size = new System.Drawing.Size(46, 13);
            this.linkWebsite.TabIndex = 5;
            this.linkWebsite.TabStop = true;
            this.linkWebsite.Text = "Website";
            this.linkWebsite.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkWebsite_LinkClicked);
            // 
            // splitContainer
            // 
            this.splitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer.IsSplitterFixed = true;
            this.splitContainer.Location = new System.Drawing.Point(12, 12);
            this.splitContainer.Name = "splitContainer";
            // 
            // splitContainer.Panel1
            // 
            this.splitContainer.Panel1.Controls.Add(this.lblNewsLink);
            this.splitContainer.Panel1.Controls.Add(this.lblNewsDescription);
            this.splitContainer.Panel1.Controls.Add(this.lblNewsTitle);
            // 
            // splitContainer.Panel2
            // 
            this.splitContainer.Panel2.Controls.Add(this.checkPass);
            this.splitContainer.Panel2.Controls.Add(this.btnNewAccount);
            this.splitContainer.Panel2.Controls.Add(this.lblUsername);
            this.splitContainer.Panel2.Controls.Add(this.linkWebsite);
            this.splitContainer.Panel2.Controls.Add(this.txtboxUsername);
            this.splitContainer.Panel2.Controls.Add(this.btnLogin);
            this.splitContainer.Panel2.Controls.Add(this.txtboxPassword);
            this.splitContainer.Panel2.Controls.Add(this.lblPassword);
            this.splitContainer.Size = new System.Drawing.Size(457, 219);
            this.splitContainer.SplitterDistance = 292;
            this.splitContainer.SplitterWidth = 1;
            this.splitContainer.TabIndex = 8;
            // 
            // lblNewsLink
            // 
            this.lblNewsLink.AutoSize = true;
            this.lblNewsLink.Location = new System.Drawing.Point(3, 201);
            this.lblNewsLink.Name = "lblNewsLink";
            this.lblNewsLink.Size = new System.Drawing.Size(60, 13);
            this.lblNewsLink.TabIndex = 6;
            this.lblNewsLink.TabStop = true;
            this.lblNewsLink.Text = "Read More";
            this.lblNewsLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lblNewsLink_LinkClicked);
            // 
            // lblNewsDescription
            // 
            this.lblNewsDescription.AutoEllipsis = true;
            this.lblNewsDescription.Location = new System.Drawing.Point(3, 28);
            this.lblNewsDescription.Name = "lblNewsDescription";
            this.lblNewsDescription.Size = new System.Drawing.Size(246, 164);
            this.lblNewsDescription.TabIndex = 1;
            this.lblNewsDescription.Text = "label1";
            // 
            // lblNewsTitle
            // 
            this.lblNewsTitle.AutoSize = true;
            this.lblNewsTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblNewsTitle.Location = new System.Drawing.Point(3, 5);
            this.lblNewsTitle.Name = "lblNewsTitle";
            this.lblNewsTitle.Size = new System.Drawing.Size(51, 16);
            this.lblNewsTitle.TabIndex = 0;
            this.lblNewsTitle.Text = "label1";
            // 
            // checkPass
            // 
            this.checkPass.AutoSize = true;
            this.checkPass.Location = new System.Drawing.Point(18, 86);
            this.checkPass.Name = "checkPass";
            this.checkPass.Size = new System.Drawing.Size(125, 17);
            this.checkPass.TabIndex = 2;
            this.checkPass.Text = "Remember password";
            this.checkPass.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(474, 240);
            this.Controls.Add(this.splitContainer);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Infantry Online Launcher";
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel1.PerformLayout();
            this.splitContainer.Panel2.ResumeLayout(false);
            this.splitContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox txtboxUsername;
        private System.Windows.Forms.TextBox txtboxPassword;
        private System.Windows.Forms.Label lblUsername;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.Button btnLogin;
        private System.Windows.Forms.Button btnNewAccount;
        private System.Windows.Forms.LinkLabel linkWebsite;
        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.LinkLabel lblNewsLink;
        private System.Windows.Forms.Label lblNewsDescription;
        private System.Windows.Forms.Label lblNewsTitle;
        private System.Windows.Forms.CheckBox checkPass;
    }
}

