namespace InfLauncher.Views
{
    partial class UpdaterForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UpdaterForm));
            this.lblCurrentFilename = new System.Windows.Forms.Label();
            this.lblFileCount = new System.Windows.Forms.Label();
            this.lblTask = new System.Windows.Forms.Label();
            this.progressBar = new VistaStyleProgressBar.ProgressBar();
            this.SuspendLayout();
            // 
            // lblCurrentFilename
            // 
            this.lblCurrentFilename.AutoSize = true;
            this.lblCurrentFilename.Location = new System.Drawing.Point(12, 35);
            this.lblCurrentFilename.Name = "lblCurrentFilename";
            this.lblCurrentFilename.Size = new System.Drawing.Size(95, 13);
            this.lblCurrentFilename.TabIndex = 3;
            this.lblCurrentFilename.Text = "lblCurrentFileName";
            // 
            // lblFileCount
            // 
            this.lblFileCount.AutoSize = true;
            this.lblFileCount.Location = new System.Drawing.Point(12, 56);
            this.lblFileCount.Name = "lblFileCount";
            this.lblFileCount.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.lblFileCount.Size = new System.Drawing.Size(61, 13);
            this.lblFileCount.TabIndex = 4;
            this.lblFileCount.Text = "lblFileCount";
            // 
            // lblTask
            // 
            this.lblTask.AutoSize = true;
            this.lblTask.Location = new System.Drawing.Point(12, 9);
            this.lblTask.Name = "lblTask";
            this.lblTask.Size = new System.Drawing.Size(41, 13);
            this.lblTask.TabIndex = 5;
            this.lblTask.Text = "lblTask";
            // 
            // progressBar
            // 
            this.progressBar.BackColor = System.Drawing.Color.Transparent;
            this.progressBar.GlowColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.progressBar.Location = new System.Drawing.Point(12, 103);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(427, 19);
            this.progressBar.TabIndex = 6;
            // 
            // UpdaterForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(451, 134);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.lblTask);
            this.Controls.Add(this.lblFileCount);
            this.Controls.Add(this.lblCurrentFilename);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UpdaterForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Infantry Updater";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblCurrentFilename;
        private System.Windows.Forms.Label lblFileCount;
        private System.Windows.Forms.Label lblTask;
        private VistaStyleProgressBar.ProgressBar progressBar;
    }
}