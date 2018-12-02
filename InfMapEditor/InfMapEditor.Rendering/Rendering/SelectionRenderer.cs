using System;
using System.Drawing;
using SlimDX;
using SlimDX.Direct3D9;
using InfMapEditor.Rendering.Helpers;

namespace InfMapEditor.Rendering.Rendering
{
    /// <summary>
    /// SelectionRenderer is responsible for drawing the selection box.
    /// </summary>
    /// <remarks>
    /// Since the Index Buffer is hardcoded, here is a pictoral explanation of the layout.
    /// 
    ///   v0, i0        v2, i2        v - vertex
    ///       +--------+              i - index
    ///       |        |
    ///       |        |
    ///       |        |
    ///       |        |
    ///       +--------+
    ///   v1, i1        v3, i3
    /// 
    /// So to connect the four vertices, the index buffer joins (i0,i1), (i0, i2), (i2, i3) and (i3, i1).
    /// </remarks>
    internal class SelectionRenderer
    {
        internal static float RenderingZOffset = 0.2f;

        public SelectionRenderer(Device device)
        {
            this.device = device;

            vertexBuffer = new VertexBuffer(device, NumOfVertices*20, Usage.None, VertexFormat.None, Pool.Managed);
            indexBuffer = new IndexBuffer(device, NumOfIndices * sizeof(Int32), Usage.None, Pool.Managed, true);
            vertexDecl = new VertexDeclaration(device, new ColorVertex().VertexElements);

            // Index buffer will never change, so prefill it.
            DataStream istream = indexBuffer.Lock(0, 0, LockFlags.None);
            istream.WriteRange(new short[] {0, 1, 0, 2, 2, 3, 3, 1});
            indexBuffer.Unlock();
        }

        public void StartSelection(Point p)
        {
            IPoint = p;
            FPoint = p;
        }

        public void UpdateSelection(Point p)
        {
            FPoint = p;
        }

        public bool IsInsideSelection(Point p)
        {
            int minX = Math.Min(IPoint.X, FPoint.X);
            int maxX = Math.Max(IPoint.X, FPoint.X);

            int minY = Math.Min(IPoint.Y, FPoint.Y);
            int maxY = Math.Max(IPoint.Y, FPoint.Y);

            if (p.X >= minX && p.X <= maxX && p.Y >= minY && p.Y <= maxY)
                return true;

            return false;
        }

        public bool HasSelection()
        {
            return IPoint != FPoint;
        }

        public void ClearSelection()
        {
            IPoint = new Point();
            FPoint = new Point();
        }

        public void ResetResources()
        {
            vertexBuffer.Dispose();
            indexBuffer.Dispose();
        }

        public void Render()
        {
            if (IPoint == FPoint)
                return;

            // 1. Construct the four vertices of the selection box.
            ////

            int x = IPoint.X;
            int y = IPoint.Y;
            int width = FPoint.X - IPoint.X;
            int height = FPoint.Y - IPoint.Y;

            var tLeft = new ColorVertex(new Vector4(x, y, RenderingZOffset, 1.0f), color);
            var bLeft = new ColorVertex(new Vector4(x, y + height, RenderingZOffset, 1.0f), color);
            var tRight = new ColorVertex(new Vector4(x + width, y, RenderingZOffset, 1.0f), color);
            var bRight = new ColorVertex(new Vector4(FPoint.X, FPoint.Y, RenderingZOffset, 1.0f), color);

            var points = new[] {tLeft, bLeft, tRight, bRight};

            // 2. Fill buffer.
            ////

            DataStream vstream = vertexBuffer.Lock(0, 0, LockFlags.Discard);
            vstream.WriteRange(points);
            vertexBuffer.Unlock();

            // 3. Set states and draw!
            ////

            device.SetTexture(0, null);

            device.VertexDeclaration = vertexDecl;
            device.Indices = indexBuffer;
            device.SetStreamSource(0, vertexBuffer, 0, 20);

            device.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, NumOfVertices, 0, 4);
        }

        public Point IPoint { get; private set; }
        public Point FPoint { get; private set; }
        private int color = Color.Green.ToArgb();

        private VertexDeclaration vertexDecl;
        private VertexBuffer vertexBuffer;
        private IndexBuffer indexBuffer;
        private Device device;

        private const int NumOfVertices = 4;
        private const int NumOfIndices = 8;
    }
}
