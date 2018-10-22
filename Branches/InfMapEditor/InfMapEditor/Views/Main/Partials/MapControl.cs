using System.Windows.Forms;

namespace InfMapEditor.Views.Main.Partials
{
    public partial class MapControl : UserControl
    {
        public event ScrollChangedDelegate ScrollChanged;
        public delegate void ScrollChangedDelegate(int x, int y);

        public MapControl()
        {
            InitializeComponent();

            horizontalScrollbar = new HScrollBar();
            horizontalScrollbar.Dock = DockStyle.Bottom;
            horizontalScrollbar.Scroll += OnScroll;
            horizontalScrollbar.Minimum = 0;
            horizontalScrollbar.Maximum = 16000;
            horizontalScrollbar.LargeChange = 1600;
            horizontalScrollbar.SmallChange = 160;

            verticalScrollBar = new VScrollBar();
            verticalScrollBar.Dock = DockStyle.Right;
            verticalScrollBar.Scroll += OnScroll;
            verticalScrollBar.Minimum = 0;
            verticalScrollBar.Maximum = 16000;
            verticalScrollBar.LargeChange = 1600;
            verticalScrollBar.SmallChange = 160;

            Controls.Add(horizontalScrollbar);
            Controls.Add(verticalScrollBar);
        }

        private void OnScroll(object o, ScrollEventArgs e)
        {
            if (ScrollChanged != null)
                ScrollChanged(horizontalScrollbar.Value, verticalScrollBar.Value);
        }

        private HScrollBar horizontalScrollbar;
        private VScrollBar verticalScrollBar;
    }
}
