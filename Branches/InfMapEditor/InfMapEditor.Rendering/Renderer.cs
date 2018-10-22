using System;
using System.Collections.Generic;
using System.Drawing;
using InfMapEditor.Rendering.Renderers;
using InfMapEditor.Rendering.Spatial;
using SlimDX.Direct3D9;

namespace InfMapEditor.Rendering
{
    /// <summary>
    /// Brings all the rendering functionality under one controller.
    /// </summary>
    /// <remarks>
    /// A layer represents a subrenderer that is dedicated to a specific task. From nearest to farthest:
    /// 
    /// Selection - Renders the selection box if any.
    /// Guide - Renders the guidelines(Grid).
    /// Floor - Renders the terrain.
    /// </remarks>
    public class Renderer
    {
        #region General

        public enum Layers
        {
            Floor,
            Guide,
            Object,
            Physics,
            Vision,
            Selection,
        }

        public Rectangle Viewport
        {
            get { return viewport; }
            set
            {
                viewport = value;
                ResetDevice();
                guides.Viewport = viewport;
            }
        }

        public Size Offset
        {
            set
            {
                viewport.X = value.Width;
                viewport.Y = value.Height;
            }
        }

        #endregion

        #region Selection Rendering
        #endregion

        #region Guide Rendering

        public static Color DefaultGuideColor = Color.White;
        public static int DefaultGuideColumnInterval = 16;
        public static int DefaultGuideRowInterval = 16;
        public static int DefaultGuideTransparency = 0;

        #endregion

        #region Floor Rendering
        #endregion

        public Renderer(IntPtr mapHwnd, Rectangle initialViewport)
        {
            layerStates = new Dictionary<Layers, bool>
                              {
                                  {Layers.Floor, true},
                                  {Layers.Guide, false},
                                  {Layers.Object, true},
                                  {Layers.Physics, true},
                                  {Layers.Vision, true},
                                  {Layers.Selection, true},
                              };

            var presentParams = new PresentParameters
                                    {
                                        BackBufferWidth = initialViewport.Width,
                                        BackBufferHeight = initialViewport.Height,
                                        Windowed = true,
                                        SwapEffect = SwapEffect.Flip,
                                    };

            direct3D = new Direct3D();
            device = new Device(direct3D, 0, DeviceType.Hardware, mapHwnd, CreateFlags.HardwareVertexProcessing,
                                presentParams);

            viewport = initialViewport;
            floors = new FloorRenderer(device, new Size(initialViewport.Width, initialViewport.Height));
            guides = new GuideRenderer(device, initialViewport, DefaultGuideColor, DefaultGuideColumnInterval,
                                       DefaultGuideRowInterval, DefaultGuideTransparency);
            selection = new SelectionRenderer(device);

            grid = new Grid(2048, 2048);
        }

        public void SetGuideAttributes(int colInterval, int rowInterval, int transparency, Color color, Rectangle viewport)
        {
            guides.SetAttributes(colInterval, rowInterval, transparency, color, viewport);
        }

        public void SetLayerEnabled(Layers layer, bool enabled)
        {
            layerStates[layer] = enabled;
        }

        public void SetFloorAtGrid(CellData.FloorData floor, int gridX, int gridY)
        {
            int[] coords = grid.GridCoordinatesToPixels(gridX, gridY);

            SetFloorAt(floor, coords[0], coords[1]);
        }

        public void SetFloorAt(CellData.FloorData floor, int pixelX, int pixelY)
        {
            int[] coords = grid.PixelsToGridCoordinates(pixelX + viewport.X, pixelY + viewport.Y);

            if (selection.IsInsideSelection(new Point(pixelX, pixelY)))
            {
                int[] startCoords = grid.PixelsToGridCoordinates(selection.IPoint.X, selection.IPoint.Y);
                int[] endCoords = grid.PixelsToGridCoordinates(selection.FPoint.X, selection.FPoint.Y);

                Grid.GridRange range = grid.GetRange(startCoords, endCoords);

                foreach(Grid.GridCell cell in range)
                {
                    if (cell.Data == null)
                    {
                        cell.Data = new CellData();
                    }

                    cell.Data.Floor = floor;
                    grid.Insert(cell.Data, cell.X, cell.Y);
                }

                selection.ClearSelection();
            }
            else
            {
                CellData data = grid.Get(coords[0], coords[1]);

                if (data == null)
                {
                    data = new CellData();
                }
                data.Floor = floor;
                grid.Insert(data, coords[0], coords[1]);
            }
        }

        public void Render()
        {
            var coords0 = grid.PixelsToGridCoordinates(viewport.X, viewport.Y);
            var coords1 = grid.PixelsToGridCoordinates(viewport.X + viewport.Width, viewport.Y + viewport.Height);

            Grid.GridRange visibleRange = grid.GetRange(coords0, coords1);

            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
            device.BeginScene();

            if(layerStates[Layers.Selection])
            {
                selection.Render();
            }
            if(layerStates[Layers.Floor])
            {
                floors.Render(visibleRange, viewport);
            }
            if(layerStates[Layers.Guide])
            {
                guides.Render();
            }

            device.EndScene();
            device.Present();
        }

        public void StartSelection(Point p)
        {
            var coords = grid.PixelsToGridCoordinates(p.X, p.Y);
            coords = grid.GridCoordinatesToPixels(coords[0], coords[1]);
            selection.StartSelection(new Point(coords[0], coords[1]));
        }

        public void UpdateSelection(Point p)
        {
            var coords = grid.PixelsToGridCoordinates(p.X, p.Y);
            coords = grid.GridCoordinatesToPixels(coords[0], coords[1]);
            selection.UpdateSelection(new Point(coords[0], coords[1]));
        }

        private void ResetDevice()
        {
            var presentParams = new PresentParameters
            {
                BackBufferWidth = viewport.Width,
                BackBufferHeight = viewport.Height,
                Windowed = true,
            };

            device.Reset(presentParams);
        }

        public void Dispose()
        {
            if (floors != null)
            {
                floors.Dispose();
            }

            if (guides != null)
            {
                guides.Dispose();
            }

            if (selection != null)
            {
                selection.Dispose();
            }

            if (device != null && !device.Disposed)
            {
                device.Dispose();
            }

            if (direct3D != null && !direct3D.Disposed)
            {
                direct3D.Dispose();
            }
        }

        private Dictionary<Layers, bool> layerStates;
        private FloorRenderer floors;
        private GuideRenderer guides;
        private SelectionRenderer selection;
        private Grid grid;
        private Device device;
        private Direct3D direct3D;
        private Rectangle viewport;
    }
}
