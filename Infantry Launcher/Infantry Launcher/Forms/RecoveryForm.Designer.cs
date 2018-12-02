namespace Infantry_Launcher
{
    partial class RecoveryForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RecoveryForm));
            this.RecoveryMessage = new System.Windows.Forms.Label();
            this.UsernameButton = new System.Windows.Forms.Button();
            this.PasswordButton = new System.Windows.Forms.Button();
            this.OKButton = new System.Windows.Forms.Button();
            this.CANCELButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // RecoveryMessage
            // 
            this.RecoveryMessage.AutoSize = true;
            this.RecoveryMessage.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RecoveryMessage.Location = new System.Drawing.Point(31, 9);
            this.RecoveryMessage.Name = "RecoveryMessage";
            this.RecoveryMessage.Size = new System.Drawing.Size(222, 16);
            this.RecoveryMessage.TabIndex = 0;
            this.RecoveryMessage.Text = "What are you trying to recover/reset?";
            // 
            // UsernameButton
            // 
            this.UsernameButton.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.UsernameButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.UsernameButton.Location = new System.Drawing.Point(106, 37);
            this.UsernameButton.Name = "UsernameButton";
            this.UsernameButton.Size = new System.Drawing.Size(75, 23);
            this.UsernameButton.TabIndex = 1;
            this.UsernameButton.Text = "Username";
            this.UsernameButton.UseVisualStyleBackColor = true;
            this.UsernameButton.Click += new System.EventHandler(this.UsernameButton_Click);
            // 
            // PasswordButton
            // 
            this.PasswordButton.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.PasswordButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PasswordButton.Location = new System.Drawing.Point(106, 81);
            this.PasswordButton.Name = "PasswordButton";
            this.PasswordButton.Size = new System.Drawing.Size(75, 23);
            this.PasswordButton.TabIndex = 2;
            this.PasswordButton.Text = "Password";
            this.PasswordButton.UseVisualStyleBackColor = true;
            this.PasswordButton.Click += new System.EventHandler(this.PasswordButton_Click);
            // 
            // OKButton
            // 
            this.OKButton.Cursor = System.Windows.Forms.Cursors.Default;
            this.OKButton.Location = new System.Drawing.Point(34, 139);
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size(75, 23);
            this.OKButton.TabIndex = 3;
            this.OKButton.Text = "Ok";
            this.OKButton.UseVisualStyleBackColor = true;
            this.OKButton.Click += new System.EventHandler(this.OKButton_Click);
            // 
            // CANCELButton
            // 
            this.CANCELButton.Location = new System.Drawing.Point(178, 139);
            this.CANCELButton.Name = "CANCELButton";
            this.CANCELButton.Size = new System.Drawing.Size(75, 23);
            this.CANCELButton.TabIndex = 4;
            this.CANCELButton.Text = "Cancel";
            this.CANCELButton.UseVisualStyleBackColor = true;
            this.CANCELButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // RecoveryForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(295, 174);
            this.Controls.Add(this.CANCELButton);
            this.Controls.Add(this.OKButton);
            this.Controls.Add(this.PasswordButton);
            this.Controls.Add(this.UsernameButton);
            this.Controls.Add(this.RecoveryMessage);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "RecoveryForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Forgot Password/Username";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label RecoveryMessage;
        private System.Windows.Forms.Button UsernameButton;
        private System.Windows.Forms.Button PasswordButton;
        private System.Windows.Forms.Button OKButton;
        private System.Windows.Forms.Button CANCELButton;
    }
}