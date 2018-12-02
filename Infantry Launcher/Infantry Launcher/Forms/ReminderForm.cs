using System;
using System.Windows.Forms;

namespace Infantry_Launcher
{
    public partial class ReminderForm : Form
    {
        /// <summary>
        /// Gets our changed reminder text
        /// </summary>
        public string Reminder
        {
            get;
            private set;
        }

        public ReminderForm(string reminder)
        {
            InitializeComponent();

            AcceptButton = ReminderOkButton;
            CancelButton = ReminderCancelButton;

            SavedReminderLabel.Text = reminder;

            Reminder = string.Empty;
        }

        private void ReminderOkButton_Click(object sender, EventArgs e)
        {
            Reminder = ReminderTextBox.Text;
            Close();
        }

        private void ReminderCancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}