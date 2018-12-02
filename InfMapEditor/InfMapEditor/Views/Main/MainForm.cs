using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

using InfMapEditor.DataStructures;
using InfMapEditor.Views.Main.Partials;
using InfMapEditor.Views.Palettes;
using InfMapEditor.Controllers;
using InfMapEditor.FileFormats.Infantry;

namespace InfMapEditor.Views.Main
{
    public partial class MainForm : Form
    {
        #region Delegates

        public event FloorSelectedDelegate FloorSelected;

        public event ObjectSelectedDelegate ObjectSelected;

        public event OnFloorRenderDelegate OnFloorRender;

        public event OnObjectsRenderDelegate OnObjectsRender;

        public event OnPhysicsRenderDelegate OnPhysicsRender;

        public event OnVisionRenderDelegate OnVisionRender;


        public delegate void FloorSelectedDelegate(BlobImage floorImage);

        public delegate void ObjectSelectedDelegate(BlobImage objectImage);

        public delegate void OnFloorRenderDelegate(bool render);

        public delegate void OnObjectsRenderDelegate(bool render);

        public delegate void OnPhysicsRenderDelegate(bool render);

        public delegate void OnVisionRenderDelegate(bool render);

        #endregion

        public MapControl MapControl { get { return mapControl; } }

        public MinimapControl MinimapControl { get { return miniMap; } }

        public bool ShowGrid;

        public string gridWidth;

        public string gridHeight;

        public Color gridColor;

        public decimal gridTransparency;

        public Rectangle GetPanelSize { get { return panelSize; } }

        public MainForm(MainController controller)
        {
            mainController = controller;
            InitializeComponent();
            panelSize = new Rectangle(0, 0, mainFormPanel.Size.Width, mainFormPanel.Size.Height);
        }

        /// <summary>
        /// Resets any active palette layout control with the current working directory images
        /// </summary>
        public void ReloadBlobImages()
        {
            if (mainPalette != null)
            {
            }

            if (palette != null)
            {
            }
        }

        public void InitPaletteControl()
        {
            //This is called only once on application start
            OnMainPaletteClosing(this, null);
        }

        public void UpdatePositionLabel(int x, int y)
        {
            statusLabelMousePosition.Text = String.Format("Map Pos ({0}, {1})", x, y);
        }

        #region File Menu

        private void MenuItemFileNew_Click(object sender, EventArgs e)
        {

        }

        private void MenuItemFileOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            open.Title = "Open File";
            open.Filter = "Map File (*.map)|*.map|Level file (*.lvl)|*.lvl";
            if (!String.IsNullOrEmpty(mainController.CurrentWorkingDirectory))
            {
                open.InitialDirectory = mainController.CurrentWorkingDirectory;
            }
            if (open.ShowDialog() == DialogResult.OK)
            {
                if (open.InitialDirectory != mainController.CurrentWorkingDirectory)
                {
                    mainController.CurrentWorkingDirectory = Directory.GetCurrentDirectory();
                }

                var level = new LevelFile();
                Stream input;
                if ((input = open.OpenFile()) != null)
                {
                    level.Deserialize(input);
                    FileName = open.SafeFileName;

                    input.Close();

                    //Strip duplicated blobs
                    for (int i = 0; i < level.Floors.Count; i++)
                    {
                        var floor = level.Floors[i];
                        if (floor.FileName != null &&
                            floor.FileName.EndsWith(".lvb.blo") == true)
                        {
                            floor.FileName = null;
                            level.Floors[i] = floor;
                        }
                    }

                    for (int i = 0; i < level.Objects.Count; i++)
                    {
                        var obj = level.Objects[i];
                        if (obj.FileName != null &&
                            obj.FileName.EndsWith(".lvb.blo") == true)
                        {
                            obj.FileName = null;
                            level.Objects[i] = obj;
                        }
                    }
                }

                mainController.LoadLevel(level);
            }
            open.Dispose();
        }

        private void MenuItemFileSave_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog save = new SaveFileDialog();
                save.Title = "Save File";
                save.Filter = "Map File (*.map)|*.map|Level file (*.lvl)|*.lvl";
                if (!String.IsNullOrEmpty(FileName))
                {
                    save.FileName = FileName;
                }

                if (save.ShowDialog() == DialogResult.OK)
                {
                    Stream s;
                    if ((s = File.Open(FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite)) != null)
                    {
                        s.Close();
                        //Update our map controller and tell it we have saved now
                        mainController.MapModified = false;
                    }
                }
                save.Dispose();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "MainForm", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MenuItemFileSaveAs_Click(object sender, EventArgs e)
        {

        }

        private void MenuItemFileImportLevel_Click(object sender, EventArgs e)
        {

        }

        private void MenuItemFileImportMap_Click(object sender, EventArgs e)
        {

        }

        private void MenuItemFileExit_Click(object sender, EventArgs e)
        {
            if (mainController.MapModified)
            {
                switch ((DialogResult)MessageBox.Show(String.Format("Do you want to save changes to {0}?", FileName), "Infantry Map Editor", MessageBoxButtons.YesNoCancel, MessageBoxIcon.None))
                {
                    case DialogResult.Yes:
                        {
                            SaveFileDialog save = new SaveFileDialog();
                            save.Title = "Save as";
                            save.FileName = FileName;
                            if (save.ShowDialog() == DialogResult.OK)
                            {
                                //Update our map controller and tell it we have saved now
                                mainController.MapModified = false;
                            }
                            save.Dispose();
                        }
                        break;
                    case DialogResult.Cancel:
                        return;
                }
            }
            mainController.Disposed();
            Application.Exit();
        }

        #endregion

        #region Edit Menu

        private void MenuItemEditShowGrid_Click(object sender, EventArgs e)
        {
            GridForm grid = new GridForm(this);

            grid.ShowDialog();
        }

        /// <summary>
        /// This is called from only gridform.cs. Itll enable our guide(grid) on the map if selected.
        /// </summary>
        public void OnGridClick_Ok()
        {
            int Width;
            if (!int.TryParse(gridWidth, out Width))
            {
                MessageBox.Show("GridWidth value cannot be empty.", "MainForm", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            int Height;
            if (!int.TryParse(gridHeight, out Height))
            {
                MessageBox.Show("GridHeight value cannot be empty.", "MainForm", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            mainController.GridChanged(ShowGrid, Width, Height, (int)gridTransparency, gridColor);
        }

        private void MenuItemEditWorkingDir_Click(object sender, EventArgs e)
        {
            try
            {
                FolderBrowserDialog open = new FolderBrowserDialog();
                open.Description = "What directory would you like to change to?";
                if (open.ShowDialog() == DialogResult.OK)
                {
                    mainController.LoadWorkingDirectory(open.SelectedPath);
                    mainController.CurrentWorkingDirectory = open.SelectedPath;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "MainForm", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Layers Menu

        private void MenuItemObjFloors_Click(object sender, EventArgs e)
        {
            if (OnFloorRender != null)
            { OnFloorRender(this.MenuItemLayerFloors.Checked); }
        }

        private void MenuItemObjObjects_Click(object sender, EventArgs e)
        {
            if (OnObjectsRender != null)
            { OnObjectsRender(this.MenuItemLayerObjects.Checked); }
        }

        private void MenuItemObjPhysics_Click(object sender, EventArgs e)
        {
            if (OnPhysicsRender != null)
            { OnPhysicsRender(this.MenuItemLayerPhysics.Checked); }
        }

        private void MenuItemObjVision_Click(object sender, EventArgs e)
        {
            if (OnVisionRender != null)
            { OnVisionRender(this.MenuItemLayerVision.Checked); }
        }

        #endregion

        #region Objects Menu
        #endregion

        #region Window Menu

        private void MenuItemWindowMainPalette_Click(object sender, EventArgs e)
        {
            //Are we docked?
            if (palette != null)
            { palette.Dispose(); }

            mainPalette = new MainPalette();
            mainPalette.Owner = this;
            mainPalette.FormClosing += OnMainPaletteClosing;

            //Set the blobs
            mainPalette.PaletteLayoutController.SetFloorImages(mainController.FloorBlobs);
            mainPalette.PaletteLayoutController.SetObjectImages(mainController.ObjectBlobs);

            //Set our selections
            palette.FloorSelected +=
                delegate(BlobImage image)
                {
                    if (FloorSelected != null)
                        FloorSelected(image);
                };

            palette.ObjectSelected +=
                delegate(BlobImage image)
                {
                    if (ObjectSelected != null)
                        ObjectSelected(image);
                };

            mainPalette.ShowDialog();
        }

        private void OnMainPaletteClosing(object sender, FormClosingEventArgs e)
        {
            ////////// Dock it //////////
            palette = new PaletteLayoutControl();
            palette.Location = new Point(0, 0);
            palette.Name = "MainFormPaletteLayoutControl";
            palette.Size = new Size(266, 291);
            palette.TabIndex = 2;
            palette.Parent = mainFormPanel;
            palette.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            palette.Dock = DockStyle.Fill;

            //Make the dock button visible
            palette.UndockVisible = true;

            //Set our blobs
            palette.SetFloorImages(mainController.FloorBlobs);
            palette.SetObjectImages(mainController.ObjectBlobs);

            //Set our selections
            palette.FloorSelected +=
                delegate(BlobImage image)
                {
                    if (FloorSelected != null)
                        FloorSelected(image);
                };

            palette.ObjectSelected +=
                delegate(BlobImage image)
                {
                    if (ObjectSelected != null)
                        ObjectSelected(image);
                };

            palette.Show();
        }

        #endregion

        #region Palette Control

        public void CheckPaletteCollision()
        {
            CheckRectCollision();
        }

        private void CheckRectCollision()
        {

        }

        public void UndockPaletteControl()
        {
            //If we are already loaded, close it
            if (palette != null)
            {
                palette.Dispose();
            }
            MenuItemWindowMainPalette_Click(this, null);
        }

        private void PaletteControl_OnScrollChanged(int y)
        {

        }

        #endregion

        private MainController mainController;
        private string FileName;
        private Rectangle panelSize;
        private MainPalette mainPalette;
        private PaletteLayoutControl palette;
    }
}
