using System;
using System.Windows.Forms;

namespace InfMapEditor.Views.Main
{
    public partial class GridForm : Form
    {
        public GridForm(MainForm form)
        {
            InitializeComponent();
            mainForm = form;

            DefaultWidth = txtboxWidth.Text;
            DefaultHeight = txtboxHeight.Text;

            checkShowGrid.Checked = form.ShowGrid;
            boxGridSize.Enabled = form.ShowGrid;
            txtboxWidth.Text = string.IsNullOrEmpty(form.gridWidth) ? DefaultWidth : form.gridWidth;
            txtboxHeight.Text = string.IsNullOrEmpty(form.gridHeight) ? DefaultHeight : form.gridHeight;
            panelColorDisplay.BackColor = form.gridColor;
            transparentNumeric.Value = (form.gridTransparency / maxAlphaValue * 100);
        }

        private void checkShowGrid_CheckedChanged(object sender, EventArgs e)
        {
            if(checkShowGrid.Checked)
            {
                boxGridSize.Enabled = true;
            }
            else
            {
                boxGridSize.Enabled = false;
            }
        }

        private void textBoxWidth_Changed(object sender, EventArgs e)
        {
            int Width;
            if (!int.TryParse(txtboxWidth.Text, out Width))
            {
                MessageBox.Show("That is not a number.");
                return;
            }
            width = txtboxWidth.Text;
        }

        private void textBoxHeight_Changed(object sender, EventArgs e)
        {
            int Height;
            if (!int.TryParse(txtboxHeight.Text, out Height))
            {
                MessageBox.Show("That is not a number.");
                return;
            }
            height = txtboxHeight.Text;
        }

        private void panelColorDisplay_DoubleClick(object sender, EventArgs e)
        {
            ColorDialog c = new ColorDialog();
            var result = c.ShowDialog();

            if (result == DialogResult.OK)
            {
                panelColorDisplay.BackColor = c.Color;
                panelColorDisplay.Update();
                GridPreview(null, null);
            }
        }

        private void GridPreview(object sender, EventArgs e)
        {
            mainForm.ShowGrid = checkShowGrid.Checked;
            mainForm.gridWidth = string.IsNullOrEmpty(width) ? DefaultWidth : width;
            mainForm.gridHeight = string.IsNullOrEmpty(height) ? DefaultHeight : height;
            mainForm.gridColor = panelColorDisplay.BackColor;
            mainForm.gridTransparency = (maxAlphaValue * transparentNumeric.Value / 100);

            mainForm.OnGridClick_Ok();
        }

        private void GridOk_Click(object sender, EventArgs e)
        {
            mainForm.ShowGrid = checkShowGrid.Checked;
            mainForm.gridWidth = string.IsNullOrEmpty(width) ? DefaultWidth : width;
            mainForm.gridHeight = string.IsNullOrEmpty(height) ? DefaultHeight : height;
            mainForm.gridColor = panelColorDisplay.BackColor;
            mainForm.gridTransparency = (maxAlphaValue * transparentNumeric.Value / 100);

            mainForm.OnGridClick_Ok();
            Close();
        }

        private void GridCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private readonly MainForm mainForm;
        private string DefaultWidth;
        private string DefaultHeight;
        private string width;
        private string height;
        private readonly int maxAlphaValue = 255;
    }
}
