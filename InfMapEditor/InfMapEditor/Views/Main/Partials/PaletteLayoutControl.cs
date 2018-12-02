using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

using InfMapEditor.DataStructures;

namespace InfMapEditor.Views.Main.Partials
{
    public partial class PaletteLayoutControl : UserControl
    {
        #region Delegates

        public OnFloorSelected FloorSelected;

        public OnObjectSelected ObjectSelected;

        public delegate void OnFloorSelected(BlobImage image);

        public delegate void OnObjectSelected(BlobImage image);

        #endregion

        /// <summary>
        /// Gets the state of the left mouse button being held down over the palette layout screen.
        /// </summary>
        public bool IsLeftMouseDown { get; private set; }

        /// <summary>
        /// Gets a rectangle bound size of our palette control window.
        /// </summary>
        public Rectangle GetWindowSize { get { return windowSize; } }

        /// <summary>
        /// Gets or sets our undocking buttons visible status
        /// </summary>
        public bool UndockVisible
        {
            get { return PaletteControlButton_Undock.Visible; }
            set { PaletteControlButton_Undock.Visible = value; }
        }

        /// <summary>
        /// Generic constructor
        /// </summary>
        public PaletteLayoutControl()
        {
            InitializeComponent();

            paletteType = PaletteType.Floor;

            IsLeftMouseDown = false;
            windowSize = new Rectangle(0, 0, Size.Width, Size.Height);
        }

        /// <summary>
        /// Sets our floor images, creates a clickable radio button for each image
        /// </summary>
        public void SetFloorImages(List<BlobImage> images)
        {
            if (images == null || images.Count == 0)
            { return; }

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(BackgroundWork1);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BackgroundCompleted1);
            worker.RunWorkerAsync(images);

            foreach (BlobImage floor in images)
            {
                RadioButton b = new RadioButton();

                b.Image = floor.Image;
                b.Width = 32;
                b.Height = 32;
                b.Appearance = Appearance.Button;
                b.Tag = floor;
                b.CheckedChanged += OnSelectionChanged;

                flowLayoutPanel1.Controls.Add(b);
            }
        }

        /// <summary>
        /// Sets our object images, creates a clickable radio button for each image
        /// </summary>
        public void SetObjectImages(List<BlobImage> images)
        {
            if (images == null || images.Count == 0)
            { return; }

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(BackgroundWork2);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BackgroundCompleted2);
            worker.RunWorkerAsync(images);

            foreach (BlobImage obj in images)
            {
                RadioButton b = new RadioButton();

                b.Image = obj.Image;
                b.Width = 32;
                b.Height = 32;
                b.Appearance = Appearance.Button;
                b.Tag = obj;
                b.CheckedChanged += OnSelectionChanged;

                flowLayoutPanel2.Controls.Add(b);
            }
        }

        #region Mouse Events

        /// <summary>
        /// Sends the message that a click on the palette control has occured, and begins to track
        /// mouse movements.
        /// </summary>
        private void PaletteLayoutControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                IsLeftMouseDown = true;
                MessageBox.Show("Test");
            }
        }

        /// <summary>
        /// Stops tracking the mouse movement.
        /// </summary>
        private void PaletteLayoutControl_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                IsLeftMouseDown = false;
            }
        }

        #endregion

        #region Button Events

        private void PaletteControlButton_F_Click(object sender, EventArgs e)
        {
            //Show this flow
            flowLayoutPanel1.BringToFront();

            paletteType = PaletteType.Floor;
        }

        private void PaletteControlButton_O_Click(object sender, EventArgs e)
        {
            //Show this flow
            flowLayoutPanel2.BringToFront();

            paletteType = PaletteType.Object;
        }

        private void PaletteControlButton_P_Click(object sender, EventArgs e)
        {
            //Show this flow
            flowLayoutPanel3.BringToFront();

            paletteType = PaletteType.Physic;
        }

        private void PaletteControlButton_V_Click(object sender, EventArgs e)
        {
            //Show this flow
            flowLayoutPanel4.BringToFront();

            paletteType = PaletteType.Vision;
        }

        private void PaletteControlButton_Undock_Click(object sender, EventArgs e)
        {
            //If we are visible, then we were created as a docked control
            if (UndockVisible)
            {
                ////////// UnDock it //////////
                mainForm.UndockPaletteControl();
            }
        }

        #endregion

        private void OnSelectionChanged(object sender, EventArgs e)
        {
            RadioButton selected = (RadioButton)sender;

            switch (paletteType)
            {
                case PaletteType.Floor:
                    if (FloorSelected != null && selected.Checked)
                    {
                        FloorSelected((BlobImage)selected.Tag);
                    }
                    break;

                case PaletteType.Object:
                    if (ObjectSelected != null && selected.Checked)
                    {
                        ObjectSelected((BlobImage)selected.Tag);
                    }
                    break;

                case PaletteType.Physic:
                    break;

                case PaletteType.Vision:
                    break;
            }
        }

        private void BackgroundWork1(object sender, DoWorkEventArgs e)
        {
            var images = (List<BlobImage>)e.Argument;
            var buttons = new List<RadioButton>();
            foreach (BlobImage floor in images)
            {
                RadioButton b = new RadioButton();

                b.Image = floor.Image;
                b.Width = 32;
                b.Height = 32;
                b.Appearance = Appearance.Button;
                b.Tag = floor;
                b.CheckedChanged += OnSelectionChanged;

                buttons.Add(b);
                flowLayoutPanel1.Controls.Add(b);
            }
            e.Result = buttons;
        }

        private void BackgroundWork2(object sender, DoWorkEventArgs e)
        {
            var images = (List<BlobImage>)e.Argument;
            var buttons = new List<RadioButton>();
            foreach (BlobImage obj in images)
            {
                RadioButton b = new RadioButton();

                b.Image = obj.Image;
                b.Width = 32;
                b.Height = 32;
                b.Appearance = Appearance.Button;
                b.Tag = obj;
                b.CheckedChanged += OnSelectionChanged;

                buttons.Add(b);
                flowLayoutPanel2.Controls.Add(b);
            }
        }

        private void BackgroundCompleted1(object sender, RunWorkerCompletedEventArgs e)
        {
            List<RadioButton> buttons = e.Result as List<RadioButton>;
            if (buttons == null || buttons.Count == 0)
            { return; }

            foreach (RadioButton b in buttons)
            {
                flowLayoutPanel1.Controls.Add(b);
            }
        }

        private void BackgroundCompleted2(object sender, RunWorkerCompletedEventArgs e)
        {
            List<RadioButton> buttons = e.Result as List<RadioButton>;
            if (buttons == null || buttons.Count == 0)
            { return; }

            foreach (RadioButton b in buttons)
            {
                flowLayoutPanel2.Controls.Add(b);
            }
        }

        private MainForm mainForm
        {
            get
            {
                if (ParentForm.GetType() != (typeof(MainForm)))
                { return (MainForm)ParentForm.Owner; }

                return (MainForm)ParentForm;
            }
        }

        private void Test(object sender, EventArgs e)
        {
            MessageBox.Show("Test");
        }
        private List<BlobImage> floorImages;
        private List<BlobImage> objectImages;
        private PaletteType paletteType;
        private enum PaletteType
        {
            Floor,
            Object,
            Physic,
            Vision
        }
        private Rectangle windowSize;
    }
}
