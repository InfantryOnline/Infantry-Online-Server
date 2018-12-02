﻿namespace InfMapEditor.Views.Palettes
{
    partial class MainPalette
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
            this.PaletteControlLayout = new InfMapEditor.Views.Main.Partials.PaletteLayoutControl();
            this.SuspendLayout();
            // 
            // PaletteControlLayout
            // 
            this.PaletteControlLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PaletteControlLayout.Location = new System.Drawing.Point(0, 0);
            this.PaletteControlLayout.Name = "PaletteControlLayout";
            this.PaletteControlLayout.Size = new System.Drawing.Size(264, 288);
            this.PaletteControlLayout.TabIndex = 0;
            this.PaletteControlLayout.UndockVisible = true;
            // 
            // MainPalette
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CausesValidation = false;
            this.ClientSize = new System.Drawing.Size(264, 288);
            this.Controls.Add(this.PaletteControlLayout);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MainPalette";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "MainPalette";
            this.ResumeLayout(false);

        }

        #endregion

        private Main.Partials.PaletteLayoutControl PaletteControlLayout;
    }
}