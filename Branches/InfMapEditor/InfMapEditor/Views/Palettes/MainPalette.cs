using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using InfMapEditor.DataStructures;

namespace InfMapEditor.Views.Palettes
{
    /// <summary>
    /// The main palette form is just a window to showcase our Palette Layout Controller.
    /// </summary>
    public partial class MainPalette : Form
    {
        /// <summary>
        /// Gets the palette layout control tied to this form
        /// </summary>
        public Main.Partials.PaletteLayoutControl PaletteLayoutController { get { return PaletteControlLayout; } }

        public MainPalette()
        {
            InitializeComponent();

            //We want to hide our dock button
            PaletteControlLayout.UndockVisible = false;
            paletteSize = new Rectangle(0, 0, this.Size.Width, this.Size.Height);
        }

        private Rectangle paletteSize;
    }
}
