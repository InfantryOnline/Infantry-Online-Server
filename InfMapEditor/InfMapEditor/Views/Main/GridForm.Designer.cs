namespace InfMapEditor.Views.Main
{
    partial class GridForm
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
            this.checkShowGrid = new System.Windows.Forms.CheckBox();
            this.boxGridSize = new System.Windows.Forms.GroupBox();
            this.transparentNumeric = new System.Windows.Forms.NumericUpDown();
            this.lblTransparency = new System.Windows.Forms.Label();
            this.panelColorDisplay = new System.Windows.Forms.Panel();
            this.lblColor = new System.Windows.Forms.Label();
            this.txtboxHeight = new System.Windows.Forms.TextBox();
            this.lblHeight = new System.Windows.Forms.Label();
            this.lblWidth = new System.Windows.Forms.Label();
            this.txtboxWidth = new System.Windows.Forms.TextBox();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.boxGridSize.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.transparentNumeric)).BeginInit();
            this.SuspendLayout();
            // 
            // checkShowGrid
            // 
            this.checkShowGrid.AutoSize = true;
            this.checkShowGrid.Location = new System.Drawing.Point(12, 12);
            this.checkShowGrid.Name = "checkShowGrid";
            this.checkShowGrid.Size = new System.Drawing.Size(75, 17);
            this.checkShowGrid.TabIndex = 0;
            this.checkShowGrid.Text = "Show Grid";
            this.checkShowGrid.UseVisualStyleBackColor = true;
            this.checkShowGrid.CheckedChanged += new System.EventHandler(this.checkShowGrid_CheckedChanged);
            // 
            // boxGridSize
            // 
            this.boxGridSize.Controls.Add(this.transparentNumeric);
            this.boxGridSize.Controls.Add(this.lblTransparency);
            this.boxGridSize.Controls.Add(this.panelColorDisplay);
            this.boxGridSize.Controls.Add(this.lblColor);
            this.boxGridSize.Controls.Add(this.txtboxHeight);
            this.boxGridSize.Controls.Add(this.lblHeight);
            this.boxGridSize.Controls.Add(this.lblWidth);
            this.boxGridSize.Controls.Add(this.txtboxWidth);
            this.boxGridSize.Enabled = false;
            this.boxGridSize.Location = new System.Drawing.Point(10, 45);
            this.boxGridSize.Name = "boxGridSize";
            this.boxGridSize.Size = new System.Drawing.Size(142, 169);
            this.boxGridSize.TabIndex = 1;
            this.boxGridSize.TabStop = false;
            this.boxGridSize.Text = "Grid Settings";
            // 
            // transparentNumeric
            // 
            this.transparentNumeric.Location = new System.Drawing.Point(84, 135);
            this.transparentNumeric.Name = "transparentNumeric";
            this.transparentNumeric.Size = new System.Drawing.Size(48, 20);
            this.transparentNumeric.TabIndex = 6;
            this.transparentNumeric.ValueChanged += new System.EventHandler(this.GridPreview);
            // 
            // lblTransparency
            // 
            this.lblTransparency.AutoSize = true;
            this.lblTransparency.Location = new System.Drawing.Point(6, 137);
            this.lblTransparency.Name = "lblTransparency";
            this.lblTransparency.Size = new System.Drawing.Size(72, 13);
            this.lblTransparency.TabIndex = 5;
            this.lblTransparency.Text = "Transparency";
            // 
            // panelColorDisplay
            // 
            this.panelColorDisplay.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panelColorDisplay.Location = new System.Drawing.Point(50, 101);
            this.panelColorDisplay.Name = "panelColorDisplay";
            this.panelColorDisplay.Size = new System.Drawing.Size(24, 24);
            this.panelColorDisplay.TabIndex = 4;
            this.panelColorDisplay.DoubleClick += new System.EventHandler(this.panelColorDisplay_DoubleClick);
            // 
            // lblColor
            // 
            this.lblColor.AutoSize = true;
            this.lblColor.Location = new System.Drawing.Point(6, 101);
            this.lblColor.Name = "lblColor";
            this.lblColor.Size = new System.Drawing.Size(31, 13);
            this.lblColor.TabIndex = 3;
            this.lblColor.Text = "Color";
            // 
            // txtboxHeight
            // 
            this.txtboxHeight.Location = new System.Drawing.Point(50, 67);
            this.txtboxHeight.Name = "txtboxHeight";
            this.txtboxHeight.Size = new System.Drawing.Size(70, 20);
            this.txtboxHeight.TabIndex = 2;
            this.txtboxHeight.Text = "16";
            this.txtboxHeight.TextChanged += new System.EventHandler(this.textBoxHeight_Changed);
            // 
            // lblHeight
            // 
            this.lblHeight.AutoSize = true;
            this.lblHeight.Location = new System.Drawing.Point(6, 70);
            this.lblHeight.Name = "lblHeight";
            this.lblHeight.Size = new System.Drawing.Size(38, 13);
            this.lblHeight.TabIndex = 2;
            this.lblHeight.Text = "Height";
            // 
            // lblWidth
            // 
            this.lblWidth.AutoSize = true;
            this.lblWidth.Location = new System.Drawing.Point(6, 34);
            this.lblWidth.Name = "lblWidth";
            this.lblWidth.Size = new System.Drawing.Size(35, 13);
            this.lblWidth.TabIndex = 1;
            this.lblWidth.Text = "Width";
            // 
            // txtboxWidth
            // 
            this.txtboxWidth.Location = new System.Drawing.Point(50, 31);
            this.txtboxWidth.Name = "txtboxWidth";
            this.txtboxWidth.Size = new System.Drawing.Size(70, 20);
            this.txtboxWidth.TabIndex = 1;
            this.txtboxWidth.Text = "16";
            this.txtboxWidth.TextChanged += new System.EventHandler(this.textBoxWidth_Changed);
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(10, 229);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(63, 23);
            this.btnOk.TabIndex = 3;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.GridOk_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(89, 229);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(63, 23);
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.GridCancel_Click);
            // 
            // GridForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(164, 255);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.boxGridSize);
            this.Controls.Add(this.checkShowGrid);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "GridForm";
            this.Text = "Grid";
            this.boxGridSize.ResumeLayout(false);
            this.boxGridSize.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.transparentNumeric)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox checkShowGrid;
        private System.Windows.Forms.GroupBox boxGridSize;
        private System.Windows.Forms.TextBox txtboxHeight;
        private System.Windows.Forms.Label lblHeight;
        private System.Windows.Forms.Label lblWidth;
        private System.Windows.Forms.TextBox txtboxWidth;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Panel panelColorDisplay;
        private System.Windows.Forms.Label lblColor;
        private System.Windows.Forms.NumericUpDown transparentNumeric;
        private System.Windows.Forms.Label lblTransparency;
    }
}