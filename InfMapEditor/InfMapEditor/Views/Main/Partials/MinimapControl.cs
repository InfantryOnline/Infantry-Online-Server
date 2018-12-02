using System.Drawing;
using System.Windows.Forms;
using InfMapEditor.Helpers;

namespace InfMapEditor.Views.Main.Partials
{
    /// <summary>
    /// MinimapControl displays and lets the user navigate a 256x256 pixel downsized version of the map.
    /// 
    /// The minimap lets he user to jump to any location on the full map by clicking on the relative spot in the 
    /// minimap, or to virtually scroll through the full map by holding down the mouse button.
    /// 
    /// In addition to the map, the minimap also draws a small rectangle showing the player what portion of the full
    /// map is displayed. Whenever the left mouse button is clicked on the minimap, the rectangle is centered on that
    /// location.
    /// </summary>
    /// 
    /// <remarks>
    /// Minimap control uses a simple System.Drawing.Bitmap object to store and draw the map in the panel.
    /// </remarks>
    public partial class MinimapControl : UserControl
    {
        /// <summary>
        /// Gets or sets the image displayed in the minimap.
        /// </summary>
        public Bitmap Image
        {
            get { return image; }
            set { image = value.Resize(256, 256); }
        }

        private Bitmap image;

        /// <summary>
        /// Gets the state of the left mouse button being held down over the minimap.
        /// </summary>
        public bool IsLeftMouseDown { get; private set; }

        /// <summary>
        /// Gets the last position in the minimap where the mouse has been clicked.
        /// </summary>
        public Point LastClicked { get; private set; }

        /// <summary>
        /// Creates a new Minimap.
        /// </summary>
        public MinimapControl()
        {
            InitializeComponent();
            IsLeftMouseDown = false;
            LastClicked = new Point();
            Image = new Bitmap(256, 256);
        }


        #region MinimapControl Delegate Methods

        /// <summary>
        /// Gives the coordinates of the mouse where the left button was clicked (as an offset from the 
        /// top-left of the minimap.)
        /// </summary>
        /// <param name="p"></param>
        public delegate void OnMinimapLeftMouseClicked(Point p);

        public OnMinimapLeftMouseClicked MinimapLeftMouseClicked;

        #endregion


        #region WinForms Event Handling

        /// <summary>
        /// Grabs the current bitmap image and draws it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MinimapControl_Paint(object sender, PaintEventArgs e)
        {
            // 1. Draw the minimap.
            e.Graphics.DrawImage(Image, 0, 0);

            // 2. Draw the visible rectangle -- adjust the rectangle if too close to the edges of the map.
        }

        /// <summary>
        /// Sends the message that a click on the minimap has occured, and begins to track
        /// mouse movements on the minimap.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MinimapControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                IsLeftMouseDown = true;
                LastClicked = new Point(e.X, e.Y);

                if(MinimapLeftMouseClicked != null)
                    MinimapLeftMouseClicked(LastClicked);
            }
        }

        /// <summary>
        /// Stops tracking the mouse movement on the minimap.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MinimapControl_MouseUp(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Left)
                IsLeftMouseDown = false;
        }

        /// <summary>
        /// If the mouse is currently held down, alerts any listeners of the mice's coordinates.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MinimapControl_MouseMove(object sender, MouseEventArgs e)
        {
            if(IsLeftMouseDown && e.Button == MouseButtons.Left)
            {
                LastClicked = new Point(e.X, e.Y);

                if(MinimapLeftMouseClicked != null)
                    MinimapLeftMouseClicked(LastClicked);
            }
        }

        #endregion
    }
}
