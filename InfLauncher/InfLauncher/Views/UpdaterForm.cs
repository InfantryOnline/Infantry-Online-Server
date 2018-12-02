using System.Windows.Forms;

namespace InfLauncher.Views
{
    /// <summary>
    /// 
    /// </summary>
    public partial class UpdaterForm : Form
    {
        /// <summary>
        /// 
        /// </summary>
        public UpdaterForm()
        {
            InitializeComponent();

            lblTask.Text = "";
            lblFileCount.Text = "";
            lblCurrentFilename.Text = "";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="task"></param>
        public void SetCurrentTask(string task)
        {
            lblTask.Text = task;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="finished"></param>
        /// <param name="total"></param>
        public void SetFileCounts(int finished, int total)
        {
            lblFileCount.Text = string.Format("{0} / {1}", finished, total);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        public void SetFilename(string filename)
        {
            lblCurrentFilename.Text = filename;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="progress"></param>
        public void SetProgress(int progress)
        {
            progressBar.Value = progress;
        }
    }
}
