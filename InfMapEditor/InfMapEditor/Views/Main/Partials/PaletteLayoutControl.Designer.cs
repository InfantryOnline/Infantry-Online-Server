namespace InfMapEditor.Views.Main.Partials
{
    partial class PaletteLayoutControl
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PaletteLayoutControl));
            this.PaletteControlGroupBox = new System.Windows.Forms.GroupBox();
            this.PaletteControlSplitContainer = new System.Windows.Forms.SplitContainer();
            this.PaletteControlButton_Undock = new System.Windows.Forms.Button();
            this.PaletteControlButton_V = new System.Windows.Forms.Button();
            this.PaletteControlButton_P = new System.Windows.Forms.Button();
            this.PaletteControlButton_O = new System.Windows.Forms.Button();
            this.PaletteControlButton_F = new System.Windows.Forms.Button();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel3 = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel4 = new System.Windows.Forms.FlowLayoutPanel();
            this.PaletteControlGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PaletteControlSplitContainer)).BeginInit();
            this.PaletteControlSplitContainer.Panel1.SuspendLayout();
            this.PaletteControlSplitContainer.Panel2.SuspendLayout();
            this.PaletteControlSplitContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // PaletteControlGroupBox
            // 
            this.PaletteControlGroupBox.Controls.Add(this.PaletteControlSplitContainer);
            this.PaletteControlGroupBox.Location = new System.Drawing.Point(0, 0);
            this.PaletteControlGroupBox.Name = "PaletteControlGroupBox";
            this.PaletteControlGroupBox.Size = new System.Drawing.Size(266, 291);
            this.PaletteControlGroupBox.TabIndex = 0;
            this.PaletteControlGroupBox.TabStop = false;
            // 
            // PaletteControlSplitContainer
            // 
            this.PaletteControlSplitContainer.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.PaletteControlSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PaletteControlSplitContainer.Location = new System.Drawing.Point(3, 16);
            this.PaletteControlSplitContainer.Name = "PaletteControlSplitContainer";
            this.PaletteControlSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // PaletteControlSplitContainer.Panel1
            // 
            this.PaletteControlSplitContainer.Panel1.Controls.Add(this.PaletteControlButton_Undock);
            this.PaletteControlSplitContainer.Panel1.Controls.Add(this.PaletteControlButton_V);
            this.PaletteControlSplitContainer.Panel1.Controls.Add(this.PaletteControlButton_P);
            this.PaletteControlSplitContainer.Panel1.Controls.Add(this.PaletteControlButton_O);
            this.PaletteControlSplitContainer.Panel1.Controls.Add(this.PaletteControlButton_F);
            // 
            // PaletteControlSplitContainer.Panel2
            // 
            this.PaletteControlSplitContainer.Panel2.Controls.Add(this.flowLayoutPanel1);
            this.PaletteControlSplitContainer.Panel2.Controls.Add(this.flowLayoutPanel2);
            this.PaletteControlSplitContainer.Panel2.Controls.Add(this.flowLayoutPanel3);
            this.PaletteControlSplitContainer.Panel2.Controls.Add(this.flowLayoutPanel4);
            this.PaletteControlSplitContainer.Size = new System.Drawing.Size(260, 272);
            this.PaletteControlSplitContainer.SplitterDistance = 34;
            this.PaletteControlSplitContainer.TabIndex = 0;
            // 
            // PaletteControlButton_Undock
            // 
            this.PaletteControlButton_Undock.Image = ((System.Drawing.Image)(resources.GetObject("PaletteControlButton_Undock.Image")));
            this.PaletteControlButton_Undock.Location = new System.Drawing.Point(233, 3);
            this.PaletteControlButton_Undock.Name = "PaletteControlButton_Undock";
            this.PaletteControlButton_Undock.Size = new System.Drawing.Size(18, 22);
            this.PaletteControlButton_Undock.TabIndex = 4;
            this.PaletteControlButton_Undock.UseVisualStyleBackColor = true;
            this.PaletteControlButton_Undock.Click += new System.EventHandler(this.PaletteControlButton_Undock_Click);
            // 
            // PaletteControlButton_V
            // 
            this.PaletteControlButton_V.Location = new System.Drawing.Point(174, 3);
            this.PaletteControlButton_V.Name = "PaletteControlButton_V";
            this.PaletteControlButton_V.Size = new System.Drawing.Size(51, 22);
            this.PaletteControlButton_V.TabIndex = 3;
            this.PaletteControlButton_V.Text = "Vision";
            this.PaletteControlButton_V.UseVisualStyleBackColor = true;
            this.PaletteControlButton_V.Click += new System.EventHandler(this.PaletteControlButton_V_Click);
            // 
            // PaletteControlButton_P
            // 
            this.PaletteControlButton_P.Location = new System.Drawing.Point(117, 3);
            this.PaletteControlButton_P.Name = "PaletteControlButton_P";
            this.PaletteControlButton_P.Size = new System.Drawing.Size(51, 22);
            this.PaletteControlButton_P.TabIndex = 2;
            this.PaletteControlButton_P.Text = "Physics";
            this.PaletteControlButton_P.UseVisualStyleBackColor = true;
            this.PaletteControlButton_P.Click += new System.EventHandler(this.PaletteControlButton_P_Click);
            // 
            // PaletteControlButton_O
            // 
            this.PaletteControlButton_O.Location = new System.Drawing.Point(60, 3);
            this.PaletteControlButton_O.Name = "PaletteControlButton_O";
            this.PaletteControlButton_O.Size = new System.Drawing.Size(51, 22);
            this.PaletteControlButton_O.TabIndex = 1;
            this.PaletteControlButton_O.Text = "Objects";
            this.PaletteControlButton_O.UseVisualStyleBackColor = true;
            this.PaletteControlButton_O.Click += new System.EventHandler(this.PaletteControlButton_O_Click);
            // 
            // PaletteControlButton_F
            // 
            this.PaletteControlButton_F.Location = new System.Drawing.Point(3, 3);
            this.PaletteControlButton_F.Name = "PaletteControlButton_F";
            this.PaletteControlButton_F.Size = new System.Drawing.Size(51, 22);
            this.PaletteControlButton_F.TabIndex = 0;
            this.PaletteControlButton_F.Text = "Floors";
            this.PaletteControlButton_F.UseVisualStyleBackColor = true;
            this.PaletteControlButton_F.Click += new System.EventHandler(this.PaletteControlButton_F_Click);
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoScroll = true;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(255, 230);
            this.flowLayoutPanel1.TabIndex = 0;
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.AutoScroll = true;
            this.flowLayoutPanel2.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(255, 230);
            this.flowLayoutPanel2.TabIndex = 0;
            // 
            // flowLayoutPanel3
            // 
            this.flowLayoutPanel3.AutoScroll = true;
            this.flowLayoutPanel3.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel3.Name = "flowLayoutPanel3";
            this.flowLayoutPanel3.Size = new System.Drawing.Size(255, 230);
            this.flowLayoutPanel3.TabIndex = 0;
            // 
            // flowLayoutPanel4
            // 
            this.flowLayoutPanel4.AutoScroll = true;
            this.flowLayoutPanel4.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel4.Name = "flowLayoutPanel4";
            this.flowLayoutPanel4.Size = new System.Drawing.Size(255, 230);
            this.flowLayoutPanel4.TabIndex = 0;
            // 
            // PaletteLayoutControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.PaletteControlGroupBox);
            this.Name = "PaletteLayoutControl";
            this.Size = new System.Drawing.Size(266, 291);
            this.PaletteControlGroupBox.ResumeLayout(false);
            this.PaletteControlSplitContainer.Panel1.ResumeLayout(false);
            this.PaletteControlSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.PaletteControlSplitContainer)).EndInit();
            this.PaletteControlSplitContainer.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox PaletteControlGroupBox;
        private System.Windows.Forms.SplitContainer PaletteControlSplitContainer;
        private System.Windows.Forms.Button PaletteControlButton_Undock;
        private System.Windows.Forms.Button PaletteControlButton_V;
        private System.Windows.Forms.Button PaletteControlButton_P;
        private System.Windows.Forms.Button PaletteControlButton_O;
        private System.Windows.Forms.Button PaletteControlButton_F;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel3;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel4;
    }
}
