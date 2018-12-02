namespace Infantry_Launcher
{
    partial class MessageBoxForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MessageBoxForm));
            this.MessageBoxLinkLabel = new System.Windows.Forms.LinkLabel();
            this.MessageBoxButtonOK = new System.Windows.Forms.Button();
            this.MessageBoxErrorMessage = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // MessageBoxLinkLabel
            // 
            this.MessageBoxLinkLabel.ActiveLinkColor = System.Drawing.Color.DarkRed;
            this.MessageBoxLinkLabel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.MessageBoxLinkLabel.AutoSize = true;
            this.MessageBoxLinkLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MessageBoxLinkLabel.LinkBehavior = System.Windows.Forms.LinkBehavior.NeverUnderline;
            this.MessageBoxLinkLabel.LinkColor = System.Drawing.Color.Blue;
            this.MessageBoxLinkLabel.Location = new System.Drawing.Point(167, 210);
            this.MessageBoxLinkLabel.Name = "MessageBoxLinkLabel";
            this.MessageBoxLinkLabel.Size = new System.Drawing.Size(0, 16);
            this.MessageBoxLinkLabel.TabIndex = 1;
            this.MessageBoxLinkLabel.UseWaitCursor = true;
            this.MessageBoxLinkLabel.Visible = false;
            this.MessageBoxLinkLabel.VisitedLinkColor = System.Drawing.Color.Red;
            // 
            // MessageBoxButtonOK
            // 
            this.MessageBoxButtonOK.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.MessageBoxButtonOK.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.MessageBoxButtonOK.Location = new System.Drawing.Point(171, 232);
            this.MessageBoxButtonOK.Name = "MessageBoxButtonOK";
            this.MessageBoxButtonOK.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.MessageBoxButtonOK.Size = new System.Drawing.Size(87, 27);
            this.MessageBoxButtonOK.TabIndex = 2;
            this.MessageBoxButtonOK.Text = "OK";
            this.MessageBoxButtonOK.UseVisualStyleBackColor = true;
            this.MessageBoxButtonOK.Click += new System.EventHandler(this.MessageBoxButtonOK_Click);
            // 
            // MessageBoxErrorMessage
            // 
            this.MessageBoxErrorMessage.BackColor = System.Drawing.SystemColors.Control;
            this.MessageBoxErrorMessage.Cursor = System.Windows.Forms.Cursors.Default;
            this.MessageBoxErrorMessage.Dock = System.Windows.Forms.DockStyle.Top;
            this.MessageBoxErrorMessage.Location = new System.Drawing.Point(0, 0);
            this.MessageBoxErrorMessage.Name = "MessageBoxErrorMessage";
            this.MessageBoxErrorMessage.Size = new System.Drawing.Size(436, 202);
            this.MessageBoxErrorMessage.TabIndex = 1;
            this.MessageBoxErrorMessage.Text = "";
            // 
            // MessageBoxForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(436, 266);
            this.Controls.Add(this.MessageBoxErrorMessage);
            this.Controls.Add(this.MessageBoxButtonOK);
            this.Controls.Add(this.MessageBoxLinkLabel);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MessageBoxForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.LinkLabel MessageBoxLinkLabel;
        private System.Windows.Forms.Button MessageBoxButtonOK;
        private System.Windows.Forms.RichTextBox MessageBoxErrorMessage;
    }
}