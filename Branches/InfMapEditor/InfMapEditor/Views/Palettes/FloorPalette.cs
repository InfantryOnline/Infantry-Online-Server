using System;
using System.Collections.Generic;
using System.Windows.Forms;
using InfMapEditor.DataStructures;

namespace InfMapEditor.Views.Palettes
{
    public partial class FloorPalette : Form
    {
        #region Delegates

        public delegate void OnTerrainSelected(BlobImage image);
        public OnTerrainSelected TerrainSelected;

        #endregion

        public FloorPalette(IEnumerable<BlobImage> floorImages)
        {
            InitializeComponent();

            foreach (BlobImage floor in floorImages)
            {
                RadioButton b = new RadioButton();

                b.Image = floor.Image;
                b.Width = 32;
                b.Height = 32;
                b.Appearance = Appearance.Button;
                b.Tag = floor;
                b.CheckedChanged += OnSelectionChanged;

                flowLayout.Controls.Add(b);
            }
        }

        // VS Designer only.
        internal FloorPalette()
        {
            InitializeComponent();
        }

        private void OnSelectionChanged(object sender, EventArgs e)
        {
            RadioButton selected = (RadioButton)sender;

            if (TerrainSelected != null && selected.Checked)
            {
                TerrainSelected((BlobImage)selected.Tag);
            }
        }
    }
}
