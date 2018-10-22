namespace InfMapEditor.Views.Main.Partials
{
    partial class MinimapControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // MinimapControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.Name = "MinimapControl";
            this.Size = new System.Drawing.Size(252, 252);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.MinimapControl_Paint);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.MinimapControl_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.MinimapControl_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.MinimapControl_MouseUp);
            this.ResumeLayout(false);

        }

        #endregion
    }
}
