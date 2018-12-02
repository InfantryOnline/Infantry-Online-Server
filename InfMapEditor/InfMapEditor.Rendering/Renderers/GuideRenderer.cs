using System.Collections.Generic;
using System.Drawing;
using InfMapEditor.Rendering.Helpers;
using SlimDX;
using SlimDX.Direct3D9;

namespace InfMapEditor.Rendering.Renderers
{
    internal class GuideRenderer
    {
        internal static float RenderingZOffset = 0.6f;

        public int ColumnSpan
        {
            get { return columnSpan; }
            set { columnSpan = value; ResetGrid(); }
        }

        public int RowSpan
        {
            get { return rowSpan; }
            set { rowSpan = value; ResetGrid(); }
        }

        public int Transparency
        {
            get { return transparency; }
            set { transparency = value; ResetGrid(); }
        }

        public Color LineColor
        {
            get { return color; }
            set { color = value; ResetGrid(); }
        }

        public Rectangle Viewport
        {
            get { return viewport; }
            set { viewport = value; ResetGrid(); }
        }

        public GuideRenderer(Device device, Rectangle viewport, Color color, int colInterval, int rowInterval, int transparency)
        {
            this.device = device;
            this.viewport = viewport;
            this.color = color;
            this.columnSpan = colInterval;
            this.rowSpan = rowInterval;
            this.transparency = transparency;

            ResetGrid();
        }

        public void SetAttributes(int colInterval, int rowInterval, int transparency, Color c, Rectangle rect)
        {
            this.viewport = rect;
            this.color = c;
            this.columnSpan = colInterval;
            this.rowSpan = rowInterval;
            this.transparency = transparency;

            ResetGrid();
        }

        public void Render()
        {
            // 1. Set the states -- we don't need textures, but we might need blending for the transparency.
            ////
            device.SetTexture(0, null);

            if(transparency > 0)
            {
                device.SetRenderState(RenderState.AlphaBlendEnable, true);
                device.SetRenderState(RenderState.SourceBlend, Blend.SourceAlpha);
                device.SetRenderState(RenderState.DestinationBlend, Blend.InverseSourceAlpha);
                device.SetRenderState(RenderState.BlendOperation, BlendOperation.Add);
            }

            // 2. Do the work.
            ////
            vertexBuffer.Render();
        }

        private void ResetGrid()
        {
            if(vertexBuffer != null)
            {
                vertexBuffer.Dispose();
            }

            vertexBuffer = new ExpandableVertexBuffer<ColorVertex>(device, PrimitiveType.LineList, Usage.None,
                                                                   VertexFormat.None, Pool.Managed);

            int iX = viewport.X%columnSpan;
            int iY = viewport.Y%rowSpan;

            int fX = viewport.Width;
            int fY = viewport.Height;

            int numOfColumns = viewport.Width/columnSpan;
            int numOfRows = viewport.Height/rowSpan;

            var colVertices = new List<ColorVertex>(numOfColumns * 2);
            var rowVertices = new List<ColorVertex>(numOfRows * 2);

            int opacity = 255 - Transparency;

            for(int i = iX; i < fX; i += columnSpan)
            {
                var v0 = new ColorVertex();
                var v1 = new ColorVertex();

                v0.Position = new Vector4(i, 0, RenderingZOffset, 1.0f);
                v0.Color = Color.FromArgb(opacity, color).ToArgb();

                v1.Position = new Vector4(i, viewport.Height, RenderingZOffset, 1.0f);
                v1.Color = Color.FromArgb(opacity, color).ToArgb();

                colVertices.Add(v0);
                colVertices.Add(v1);
            }

            for(int i = iY; i < fY; i += rowSpan)
            {
                var v0 = new ColorVertex();
                var v1 = new ColorVertex();

                v0.Position = new Vector4(0, i, RenderingZOffset, 1.0f);
                v0.Color = Color.FromArgb(opacity, color).ToArgb();

                v1.Position = new Vector4(viewport.Width, i, RenderingZOffset, 1.0f);
                v1.Color = Color.FromArgb(opacity, color).ToArgb();

                rowVertices.Add(v0);
                rowVertices.Add(v1);
            }

            vertexBuffer.AddVertices(colVertices);
            vertexBuffer.AddVertices(rowVertices);
        }

        public void Dispose()
        {
            if (vertexBuffer != null)
            {
                vertexBuffer.Dispose();
            }
        }

        private int columnSpan;
        private int rowSpan;
        private Color color;
        private int transparency;
        private Rectangle viewport;
        private readonly Device device;
        private ExpandableVertexBuffer<ColorVertex> vertexBuffer;
    }
}
