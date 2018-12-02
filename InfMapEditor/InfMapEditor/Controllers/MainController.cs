using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using InfMapEditor.DataStructures;
using InfMapEditor.Helpers;
using InfMapEditor.Views.Main;
using InfMapEditor.FileFormats.Infantry;

namespace InfMapEditor.Controllers
{
    public class MainController : ApplicationContext
    {
        /// <summary>
        /// Gets or sets the current working directory
        /// </summary>
        public string CurrentWorkingDirectory { get; set; }

        /// <summary>
        /// Our list of floor images
        /// </summary>
        public List<BlobImage> FloorBlobs;

        /// <summary>
        /// Our list of object images
        /// </summary>
        public List<BlobImage> ObjectBlobs;

        /// <summary>
        /// Generic constructor
        /// </summary>
        public MainController()
        {
            CurrentWorkingDirectory = @"C:\Program Files (x86)\Infantry Online";

            mainForm = new MainForm(this);
            mainForm.FloorSelected += OnFloorSelected;
            mainForm.ObjectSelected += OnObjectSelected;
            mainForm.OnFloorRender += OnFloorRender;
            mainForm.OnObjectsRender += OnObjectsRender;
            mainForm.OnPhysicsRender += OnPhysicsRender;
            mainForm.OnVisionRender += OnVisionRender;

            mapViewController = new MapViewController(mainForm.MapControl);
            mapViewController.OnMouseMoved += OnMouseMoved;

            LoadBlobs();

            mainForm.InitPaletteControl();

            mapViewController.SetSelectedTileDelegate(() => selectedBlobImage);
            //mapViewController.SetBlobImages(() => blobImages);

            // Show form
            MainForm = mainForm;
        }

        public void PostWindowsLoop()
        {
            mapViewController.Refresh();
            mainForm.CheckPaletteCollision();
        }

        public void GridChanged(bool enable, int gridWidth, int gridHeight, int gridTransparency, Color gridColor)
        {
            mapViewController.AdjustGuideSettings(enable, gridWidth, gridHeight, gridTransparency, gridColor);
        }

        public void LoadWorkingDirectory(string dir)
        {
            LoadWorkingDir(dir);
        }

        public void LoadLevel(LevelFile level)
        {
            this.level = level;
            mapViewController.LoadLevel(level);
        }

        public bool MapModified
        {
            get
            { 
                return mapViewController.MapModified; 
            }
            set
            { 
                mapViewController.MapModified = value; 
            }
        }

        public void Disposed()
        {
            mapViewController.Dispose();
        }

        private void LoadBlobs()
        {
            try
            {
                DirectoryInfo directory;
                if (!string.IsNullOrEmpty(CurrentWorkingDirectory))
                {
                    directory = new DirectoryInfo(CurrentWorkingDirectory);
                }
                else
                {
                    directory = new DirectoryInfo("assets");
                }
                if (directory == null || !directory.Exists)
                {
                    //Try within same program folder
                    directory = new DirectoryInfo(Directory.GetCurrentDirectory());
                }
                FileInfo[] floorFiles = directory.GetFiles("f_*.blo");
                FloorBlobs = BlobLoader.GetBlobsFrom(floorFiles);

                FileInfo[] objectFiles = directory.GetFiles("o_*.blo");
                ObjectBlobs = BlobLoader.GetBlobsFrom(objectFiles);
            }
            catch (System.Exception e)
            {
                MessageBox.Show(e.Message, "MainController", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadWorkingDir(string dir)
        {
            var directory = new DirectoryInfo(dir);
            FileInfo[] floorFiles = directory.GetFiles("f_*.blo");
            FloorBlobs = BlobLoader.GetBlobsFrom(floorFiles);

            FileInfo[] objectFiles = directory.GetFiles("o_*.blo");
            ObjectBlobs = BlobLoader.GetBlobsFrom(objectFiles);

            mainForm.ReloadBlobImages();

            mapViewController.SetSelectedTileDelegate(() => selectedBlobImage);
            //mapViewController.SetBlobImages(() => blobImages);
        }

        private void OnMouseMoved(int x, int y)
        {
            mainForm.UpdatePositionLabel(x, y);
        }

        private void OnFloorSelected(BlobImage blob)
        {
            selectedBlobImage = blob;
        }

        private void OnObjectSelected(BlobImage blob)
        {
            selectedBlobImage = blob;
        }

        private void OnFloorRender(bool render)
        {
            mapViewController.RenderFloor(render);
        }

        private void OnObjectsRender(bool render)
        {
            mapViewController.RenderObjects(render);
        }

        private void OnPhysicsRender(bool render)
        {
            mapViewController.RenderPhysics(render);
        }

        private void OnVisionRender(bool render)
        {
            mapViewController.RenderVision(render);
        }

        private List<BlobImage> floorBlobImages;
        private List<BlobImage> objectBlobImages;
        private readonly MapViewController mapViewController;
        private readonly MainForm mainForm;
        private BlobImage selectedBlobImage;
        private LevelFile level;
    }
}