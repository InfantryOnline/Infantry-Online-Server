using System;
using System.Windows.Forms;

namespace Infantry_Launcher
{
    public partial class MessageBoxForm : Form
    {
        public MessageBoxForm()
        {
            InitializeComponent();
            AcceptButton = MessageBoxButtonOK;
        }

        /// <summary>
        /// Writes an error message on our form
        /// </summary>
        public void Write(string msg)
        {
            MessageBoxErrorMessage.Text = msg;
            MessageBoxErrorMessage.Visible = true;
        }

        /// <summary>
        /// Writes an error message on our form
        /// </summary>
        public void Write(string msg, params object[] param)
        {
            MessageBoxErrorMessage.Text = string.Format(msg, param);
            MessageBoxErrorMessage.Visible = true;
        }

        /// <summary>
        /// Writes a caption and a message to our form
        /// </summary>
        public void Write(string caption, string msg)
        {
            Text = caption;
            MessageBoxErrorMessage.Text = msg;
            MessageBoxErrorMessage.Visible = true;
        }

        /// <summary>
        /// Writes a caption and a message to our form
        /// </summary>
        public void Write(string caption, string msg, params object[] param)
        {
            Text = caption;
            MessageBoxErrorMessage.Text = string.Format(msg, param);
            MessageBoxErrorMessage.Visible = true;
        }

        /// <summary>
        /// Shows our clickable link label with link text
        /// </summary>
        public void ShowLinkLabel(string label)
        {
            MessageBoxLinkLabel.Text = label;
            MessageBoxLinkLabel.Visible = true;
            int width = Size.Width;
            int labelWidth = MessageBoxLinkLabel.Width;
            MessageBoxLinkLabel.Left = (width - labelWidth) / 2 - (label.Length / 3);
        }

        /// <summary>
        /// Closes our form once ok is clicked
        /// </summary>
        private void MessageBoxButtonOK_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
