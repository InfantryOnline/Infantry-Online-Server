using System;
using System.Drawing;
using System.Windows.Forms;
using InfMapEditor.DataStructures;
using InfMapEditor.Rendering;
using InfMapEditor.Rendering.Spatial;
using InfMapEditor.Views.Main.Partials;
using System.Collections.Generic;
using System.Linq;

namespace InfMapEditor.Controllers
{
    public class MapViewController
    {
        #region Delegates

        public event MouseMoved OnMouseMoved;

        public event MouseDownDelegate MouseDown;

        public event MouseUpDelegate MouseUp;

        public event SizeChangedDelegate SizeChanged;

        public delegate void SizeChangedDelegate(Rectangle newSize);

        public delegate void MouseMoved(int x, int y);

        public delegate void MouseDownDelegate(MouseEventArgs mouse);

        public delegate void MouseUpDelegate(MouseEventArgs mouse);

        public delegate BlobImage SelectedTileDelegate();

        public delegate List<BlobImage> GetBlobImagesDelegate();

        public bool MapLoaded;

        public bool MapModified;

        #endregion

        public MapViewController(MapControl map)
        {
            this.map = map;
            InitMapEventHandling();

            Rectangle viewport = new Rectangle(0, 0, map.Width, map.Height);
            renderer = new Renderer(map.Handle, viewport);

            MapLoaded = false;
            MapModified = false;
        }

        public void Refresh()
        {
            renderer.Render();
        }

        public void SetSelectedTileDelegate(SelectedTileDelegate del)
        {
            selectedTile = del;
        }

        public void SetBlobImages(GetBlobImagesDelegate del)
        {
            blobImages = del;
        }

        private void InitMapEventHandling()
        {
            map.SizeChanged += Map_OnSizeChanged;
            map.MouseMove += Map_OnMouseMove;
            map.MouseDown += Map_OnMouseDown;
            map.MouseUp += Map_OnMouseUp;
            map.ScrollChanged += Map_OnScrollChanged;
        }

        #region MapControl Event Handling

        private void Map_OnSizeChanged(object sender, EventArgs e)
        {
            Rectangle newSize = new Rectangle(0, 0, map.Width, map.Height);

            renderer.Viewport = newSize;
            
            if(SizeChanged != null)
            {
                SizeChanged(newSize);
            }
        }

        private void Map_OnMouseMove(object sender, MouseEventArgs e)
        {
            if(selectionStarted)
            {
                renderer.UpdateSelection(new Point(e.X, e.Y));
                map.Cursor = Cursors.Cross;
            }

            if (MapLoaded && OnMouseMoved != null)
            {
                OnMouseMoved(e.X, e.Y);
            }
        }

        private void Map_OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                selectionStarted = true;
                renderer.StartSelection(new Point(e.X, e.Y));
            }
            else
            {
                BlobImage img = selectedTile();

                if (img == null)
                    return;

                CellData.FloorData floor = new CellData.FloorData();
                floor.Image = img;

                renderer.SetFloorAt(floor, e.X, e.Y);
            }

            if(MouseDown != null)
            {
                MouseDown(e);
            }
        }

        private void Map_OnMouseUp(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Left)
            {
                selectionStarted = false;
                map.Cursor = Cursors.Default;
            }

            if(MouseUp != null)
            {
                MouseUp(e);
            }
        }

        private void Map_OnScrollChanged(int x, int y)
        {
            renderer.Offset = new Size(x, y);
            renderer.Render();
        }

        #endregion

        #region Guide Control Settings

        public void AdjustGuideSettings(bool enable, int gridWidth, int gridHeight, int gridTransparency, Color gridColor)
        {
            Rectangle viewport = new Rectangle(0, 0, map.Width, map.Height);
            renderer.SetGuideAttributes(gridHeight, gridWidth, gridTransparency, gridColor, viewport);
            renderer.SetLayerEnabled(Renderer.Layers.Guide, enable);
        }

        #endregion

        #region Layer Control Settings

        public void RenderFloor(bool render)
        {
            renderer.SetLayerEnabled(Renderer.Layers.Floor, render);
        }

        public void RenderObjects(bool render)
        {
            renderer.SetLayerEnabled(Renderer.Layers.Object, render);
        }
        public void RenderPhysics(bool render)
        {
            renderer.SetLayerEnabled(Renderer.Layers.Physics, render);
        }
        public void RenderVision(bool render)
        {
            renderer.SetLayerEnabled(Renderer.Layers.Vision, render);
        }

        #endregion

        public void Dispose()
        {
            if (renderer != null)
            {
                renderer.Dispose();
            }
        }

        private MapControl map;
        private Renderer renderer;
        private SelectedTileDelegate selectedTile;
        private GetBlobImagesDelegate blobImages;
        private bool selectionStarted;

        internal void LoadLevel(FileFormats.Infantry.LevelFile level)
        {
            var images = blobImages();

            for(int x = 0; x < level.Width; x++)
            {
                for(int y = 0; y < level.Height; y++)
                {
                    var cell = level.Tiles[(y * level.Width) + x];
                    var terrain = level.Floors[cell.TerrainLookup];
                    
                    var fd = new CellData.FloorData();
                    fd.Image = (from blob in images
                               where blob.BlobReference.FileName == terrain.FileName &&
                                     blob.BlobReference.Id == terrain.Id
                               select blob).First();

                    renderer.SetFloorAtGrid(fd, x, y);
                }
            }

            MapLoaded = true;
        }
    }
}
