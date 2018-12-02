namespace Infantry_Launcher
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
            this.WebsiteLink = new System.Windows.Forms.LinkLabel();
            this.UsernameBox = new System.Windows.Forms.TextBox();
            this.PasswordBox = new System.Windows.Forms.TextBox();
            this.RememberPwd = new System.Windows.Forms.CheckBox();
            this.SignUpButton = new System.Windows.Forms.Button();
            this.PlayButton = new System.Windows.Forms.Button();
            this.ProgressBar = new System.Windows.Forms.ProgressBar();
            this.Status = new System.Windows.Forms.Label();
            this.ForgotPswd = new System.Windows.Forms.LinkLabel();
            this.ServerLabel = new System.Windows.Forms.Label();
            this.DiscordLink = new System.Windows.Forms.LinkLabel();
            this.DonateLink = new System.Windows.Forms.LinkLabel();
            this.PswdHint = new System.Windows.Forms.LinkLabel();
            this.SuspendLayout();
            // 
            // WebsiteLink
            // 
            this.WebsiteLink.ActiveLinkColor = System.Drawing.Color.White;
            this.WebsiteLink.AutoSize = true;
            this.WebsiteLink.BackColor = System.Drawing.Color.Transparent;
            this.WebsiteLink.Cursor = System.Windows.Forms.Cursors.WaitCursor;
            this.WebsiteLink.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.WebsiteLink.LinkBehavior = System.Windows.Forms.LinkBehavior.NeverUnderline;
            this.WebsiteLink.LinkColor = System.Drawing.Color.Silver;
            this.WebsiteLink.Location = new System.Drawing.Point(9, 9);
            this.WebsiteLink.Name = "WebsiteLink";
            this.WebsiteLink.Size = new System.Drawing.Size(65, 16);
            this.WebsiteLink.TabIndex = 1;
            this.WebsiteLink.TabStop = true;
            this.WebsiteLink.Text = "Website";
            this.WebsiteLink.UseWaitCursor = true;
            this.WebsiteLink.VisitedLinkColor = System.Drawing.Color.Red;
            this.WebsiteLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.WebsiteLink_LinkClicked);
            // 
            // UsernameBox
            // 
            this.UsernameBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.UsernameBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.UsernameBox.Location = new System.Drawing.Point(115, 147);
            this.UsernameBox.Name = "UsernameBox";
            this.UsernameBox.Size = new System.Drawing.Size(159, 22);
            this.UsernameBox.TabIndex = 4;
            // 
            // PasswordBox
            // 
            this.PasswordBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.PasswordBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PasswordBox.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.PasswordBox.Location = new System.Drawing.Point(115, 197);
            this.PasswordBox.Name = "PasswordBox";
            this.PasswordBox.PasswordChar = '*';
            this.PasswordBox.Size = new System.Drawing.Size(159, 22);
            this.PasswordBox.TabIndex = 5;
            this.PasswordBox.TextChanged += new System.EventHandler(this.PasswordBox_TextChanged);
            // 
            // RememberPwd
            // 
            this.RememberPwd.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.RememberPwd.AutoSize = true;
            this.RememberPwd.BackColor = System.Drawing.Color.Transparent;
            this.RememberPwd.Location = new System.Drawing.Point(115, 232);
            this.RememberPwd.Name = "RememberPwd";
            this.RememberPwd.Size = new System.Drawing.Size(15, 14);
            this.RememberPwd.TabIndex = 6;
            this.RememberPwd.UseVisualStyleBackColor = false;
            // 
            // SignUpButton
            // 
            this.SignUpButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.SignUpButton.BackColor = System.Drawing.Color.Black;
            this.SignUpButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.SignUpButton.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.SignUpButton.FlatAppearance.BorderSize = 0;
            this.SignUpButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.SignUpButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.SignUpButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.SignUpButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SignUpButton.ForeColor = System.Drawing.Color.Silver;
            this.SignUpButton.Image = ((System.Drawing.Image)(resources.GetObject("SignUpButton.Image")));
            this.SignUpButton.Location = new System.Drawing.Point(155, 307);
            this.SignUpButton.Name = "SignUpButton";
            this.SignUpButton.Size = new System.Drawing.Size(63, 45);
            this.SignUpButton.TabIndex = 9;
            this.SignUpButton.Text = "Sign Up";
            this.SignUpButton.UseVisualStyleBackColor = false;
            this.SignUpButton.Click += new System.EventHandler(this.SignUpButton_Click);
            this.SignUpButton.MouseEnter += new System.EventHandler(this.Button_MouseEnter);
            this.SignUpButton.MouseLeave += new System.EventHandler(this.Button_MouseLeave);
            // 
            // PlayButton
            // 
            this.PlayButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.PlayButton.BackColor = System.Drawing.Color.Black;
            this.PlayButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.PlayButton.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.PlayButton.FlatAppearance.BorderSize = 0;
            this.PlayButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.PlayButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.PlayButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.PlayButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PlayButton.ForeColor = System.Drawing.Color.Transparent;
            this.PlayButton.Image = ((System.Drawing.Image)(resources.GetObject("PlayButton.Image")));
            this.PlayButton.Location = new System.Drawing.Point(303, 365);
            this.PlayButton.Name = "PlayButton";
            this.PlayButton.Size = new System.Drawing.Size(63, 45);
            this.PlayButton.TabIndex = 10;
            this.PlayButton.Text = "Play";
            this.PlayButton.UseVisualStyleBackColor = false;
            this.PlayButton.Click += new System.EventHandler(this.PlayButton_Click);
            this.PlayButton.MouseEnter += new System.EventHandler(this.Button_MouseEnter);
            this.PlayButton.MouseLeave += new System.EventHandler(this.Button_MouseLeave);
            // 
            // ProgressBar
            // 
            this.ProgressBar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ProgressBar.BackColor = System.Drawing.SystemColors.Control;
            this.ProgressBar.ForeColor = System.Drawing.Color.DarkRed;
            this.ProgressBar.Location = new System.Drawing.Point(12, 388);
            this.ProgressBar.Name = "ProgressBar";
            this.ProgressBar.Size = new System.Drawing.Size(272, 23);
            this.ProgressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.ProgressBar.TabIndex = 0;
            // 
            // Status
            // 
            this.Status.AutoSize = true;
            this.Status.BackColor = System.Drawing.Color.Transparent;
            this.Status.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Status.ForeColor = System.Drawing.Color.White;
            this.Status.Location = new System.Drawing.Point(9, 366);
            this.Status.Name = "Status";
            this.Status.Size = new System.Drawing.Size(0, 13);
            this.Status.TabIndex = 7;
            // 
            // ForgotPswd
            // 
            this.ForgotPswd.ActiveLinkColor = System.Drawing.Color.Silver;
            this.ForgotPswd.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.ForgotPswd.AutoSize = true;
            this.ForgotPswd.BackColor = System.Drawing.Color.Transparent;
            this.ForgotPswd.Cursor = System.Windows.Forms.Cursors.Hand;
            this.ForgotPswd.DisabledLinkColor = System.Drawing.Color.Silver;
            this.ForgotPswd.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForgotPswd.LinkColor = System.Drawing.Color.Silver;
            this.ForgotPswd.Location = new System.Drawing.Point(129, 280);
            this.ForgotPswd.Name = "ForgotPswd";
            this.ForgotPswd.Size = new System.Drawing.Size(122, 15);
            this.ForgotPswd.TabIndex = 8;
            this.ForgotPswd.TabStop = true;
            this.ForgotPswd.Text = "Forgot Password?";
            this.ForgotPswd.VisitedLinkColor = System.Drawing.Color.Silver;
            this.ForgotPswd.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.ForgotPswd_LinkClicked);
            // 
            // ServerLabel
            // 
            this.ServerLabel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.ServerLabel.AutoSize = true;
            this.ServerLabel.BackColor = System.Drawing.Color.Transparent;
            this.ServerLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ServerLabel.ForeColor = System.Drawing.Color.Red;
            this.ServerLabel.Location = new System.Drawing.Point(129, 9);
            this.ServerLabel.Name = "ServerLabel";
            this.ServerLabel.Size = new System.Drawing.Size(124, 15);
            this.ServerLabel.TabIndex = 0;
            this.ServerLabel.Text = "Server is OFFLINE";
            this.ServerLabel.Visible = false;
            // 
            // DiscordLink
            // 
            this.DiscordLink.ActiveLinkColor = System.Drawing.Color.White;
            this.DiscordLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.DiscordLink.AutoSize = true;
            this.DiscordLink.BackColor = System.Drawing.Color.Transparent;
            this.DiscordLink.Cursor = System.Windows.Forms.Cursors.WaitCursor;
            this.DiscordLink.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.DiscordLink.LinkBehavior = System.Windows.Forms.LinkBehavior.NeverUnderline;
            this.DiscordLink.LinkColor = System.Drawing.Color.Silver;
            this.DiscordLink.Location = new System.Drawing.Point(309, 9);
            this.DiscordLink.Name = "DiscordLink";
            this.DiscordLink.Size = new System.Drawing.Size(62, 16);
            this.DiscordLink.TabIndex = 3;
            this.DiscordLink.TabStop = true;
            this.DiscordLink.Text = "Discord";
            this.DiscordLink.UseWaitCursor = true;
            this.DiscordLink.VisitedLinkColor = System.Drawing.Color.Red;
            this.DiscordLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.DiscordLink_LinkClicked);
            // 
            // DonateLink
            // 
            this.DonateLink.ActiveLinkColor = System.Drawing.Color.White;
            this.DonateLink.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.DonateLink.AutoSize = true;
            this.DonateLink.BackColor = System.Drawing.Color.Transparent;
            this.DonateLink.Cursor = System.Windows.Forms.Cursors.WaitCursor;
            this.DonateLink.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.DonateLink.LinkBehavior = System.Windows.Forms.LinkBehavior.NeverUnderline;
            this.DonateLink.LinkColor = System.Drawing.Color.Silver;
            this.DonateLink.Location = new System.Drawing.Point(160, 38);
            this.DonateLink.Name = "DonateLink";
            this.DonateLink.Size = new System.Drawing.Size(58, 16);
            this.DonateLink.TabIndex = 2;
            this.DonateLink.TabStop = true;
            this.DonateLink.Text = "Donate";
            this.DonateLink.UseWaitCursor = true;
            this.DonateLink.VisitedLinkColor = System.Drawing.Color.Red;
            this.DonateLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.DonateLink_LinkClicked);
            // 
            // PswdHint
            // 
            this.PswdHint.ActiveLinkColor = System.Drawing.Color.Silver;
            this.PswdHint.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.PswdHint.AutoSize = true;
            this.PswdHint.BackColor = System.Drawing.Color.Transparent;
            this.PswdHint.Cursor = System.Windows.Forms.Cursors.Hand;
            this.PswdHint.DisabledLinkColor = System.Drawing.Color.Silver;
            this.PswdHint.Enabled = false;
            this.PswdHint.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PswdHint.LinkColor = System.Drawing.Color.Silver;
            this.PswdHint.Location = new System.Drawing.Point(129, 260);
            this.PswdHint.Name = "PswdHint";
            this.PswdHint.Size = new System.Drawing.Size(41, 15);
            this.PswdHint.TabIndex = 7;
            this.PswdHint.TabStop = true;
            this.PswdHint.Text = "Hint: ";
            this.PswdHint.Visible = false;
            this.PswdHint.VisitedLinkColor = System.Drawing.Color.Silver;
            this.PswdHint.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.PswdHint_LinkClicked);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.GrayText;
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.ClientSize = new System.Drawing.Size(378, 423);
            this.Controls.Add(this.PswdHint);
            this.Controls.Add(this.DonateLink);
            this.Controls.Add(this.DiscordLink);
            this.Controls.Add(this.ServerLabel);
            this.Controls.Add(this.ForgotPswd);
            this.Controls.Add(this.Status);
            this.Controls.Add(this.ProgressBar);
            this.Controls.Add(this.PlayButton);
            this.Controls.Add(this.SignUpButton);
            this.Controls.Add(this.RememberPwd);
            this.Controls.Add(this.PasswordBox);
            this.Controls.Add(this.UsernameBox);
            this.Controls.Add(this.WebsiteLink);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Infantry Online";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.LinkLabel WebsiteLink;
        private System.Windows.Forms.TextBox UsernameBox;
        private System.Windows.Forms.TextBox PasswordBox;
        private System.Windows.Forms.CheckBox RememberPwd;
        private System.Windows.Forms.Button SignUpButton;
        private System.Windows.Forms.Button PlayButton;
        private System.Windows.Forms.ProgressBar ProgressBar;
        private System.Windows.Forms.Label Status;
        private System.Windows.Forms.LinkLabel ForgotPswd;
        private System.Windows.Forms.Label ServerLabel;
        private System.Windows.Forms.LinkLabel DiscordLink;
        private System.Windows.Forms.LinkLabel DonateLink;
        private System.Windows.Forms.LinkLabel PswdHint;
    }
}

