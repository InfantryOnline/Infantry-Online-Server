namespace Infantry_Launcher
{
    partial class ReminderForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ReminderForm));
            this.ReminderMessage = new System.Windows.Forms.Label();
            this.ReminderTextBox = new System.Windows.Forms.TextBox();
            this.ReminderOkButton = new System.Windows.Forms.Button();
            this.ReminderCancelButton = new System.Windows.Forms.Button();
            this.ReminderCurrent = new System.Windows.Forms.Label();
            this.ReminderNew = new System.Windows.Forms.Label();
            this.SavedReminderLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // ReminderMessage
            // 
            this.ReminderMessage.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.ReminderMessage.AutoSize = true;
            this.ReminderMessage.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ReminderMessage.Location = new System.Drawing.Point(66, 9);
            this.ReminderMessage.Name = "ReminderMessage";
            this.ReminderMessage.Size = new System.Drawing.Size(218, 16);
            this.ReminderMessage.TabIndex = 0;
            this.ReminderMessage.Text = "What would you like to change it to?";
            // 
            // ReminderTextBox
            // 
            this.ReminderTextBox.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.ReminderTextBox.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ReminderTextBox.Location = new System.Drawing.Point(69, 62);
            this.ReminderTextBox.Name = "ReminderTextBox";
            this.ReminderTextBox.Size = new System.Drawing.Size(251, 21);
            this.ReminderTextBox.TabIndex = 1;
            // 
            // ReminderOkButton
            // 
            this.ReminderOkButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ReminderOkButton.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ReminderOkButton.Location = new System.Drawing.Point(85, 101);
            this.ReminderOkButton.Name = "ReminderOkButton";
            this.ReminderOkButton.Size = new System.Drawing.Size(75, 23);
            this.ReminderOkButton.TabIndex = 2;
            this.ReminderOkButton.Text = "OK";
            this.ReminderOkButton.UseVisualStyleBackColor = true;
            this.ReminderOkButton.Click += new System.EventHandler(this.ReminderOkButton_Click);
            // 
            // ReminderCancelButton
            // 
            this.ReminderCancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ReminderCancelButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ReminderCancelButton.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ReminderCancelButton.Location = new System.Drawing.Point(181, 101);
            this.ReminderCancelButton.Name = "ReminderCancelButton";
            this.ReminderCancelButton.Size = new System.Drawing.Size(75, 23);
            this.ReminderCancelButton.TabIndex = 3;
            this.ReminderCancelButton.Text = "Cancel";
            this.ReminderCancelButton.UseVisualStyleBackColor = true;
            this.ReminderCancelButton.Click += new System.EventHandler(this.ReminderCancelButton_Click);
            // 
            // ReminderCurrent
            // 
            this.ReminderCurrent.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.ReminderCurrent.AutoSize = true;
            this.ReminderCurrent.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ReminderCurrent.Location = new System.Drawing.Point(12, 44);
            this.ReminderCurrent.Name = "ReminderCurrent";
            this.ReminderCurrent.Size = new System.Drawing.Size(51, 15);
            this.ReminderCurrent.TabIndex = 0;
            this.ReminderCurrent.Text = "Current:";
            // 
            // ReminderNew
            // 
            this.ReminderNew.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.ReminderNew.AutoSize = true;
            this.ReminderNew.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ReminderNew.Location = new System.Drawing.Point(28, 64);
            this.ReminderNew.Name = "ReminderNew";
            this.ReminderNew.Size = new System.Drawing.Size(35, 15);
            this.ReminderNew.TabIndex = 0;
            this.ReminderNew.Text = "New:";
            // 
            // SavedReminderLabel
            // 
            this.SavedReminderLabel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.SavedReminderLabel.AutoSize = true;
            this.SavedReminderLabel.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SavedReminderLabel.Location = new System.Drawing.Point(69, 44);
            this.SavedReminderLabel.Name = "SavedReminderLabel";
            this.SavedReminderLabel.Size = new System.Drawing.Size(0, 15);
            this.SavedReminderLabel.TabIndex = 0;
            // 
            // ReminderForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(341, 136);
            this.Controls.Add(this.SavedReminderLabel);
            this.Controls.Add(this.ReminderNew);
            this.Controls.Add(this.ReminderCurrent);
            this.Controls.Add(this.ReminderCancelButton);
            this.Controls.Add(this.ReminderOkButton);
            this.Controls.Add(this.ReminderTextBox);
            this.Controls.Add(this.ReminderMessage);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ReminderForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Password Reminder";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label ReminderMessage;
        private System.Windows.Forms.TextBox ReminderTextBox;
        private System.Windows.Forms.Button ReminderOkButton;
        private System.Windows.Forms.Button ReminderCancelButton;
        private System.Windows.Forms.Label ReminderCurrent;
        private System.Windows.Forms.Label ReminderNew;
        private System.Windows.Forms.Label SavedReminderLabel;
    }
}