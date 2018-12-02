using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using InfMapEditor.DataStructures;
using InfMapEditor.Rendering.Helpers;
using InfMapEditor.Rendering.Spatial;
using SlimDX;
using SlimDX.Direct3D9;
using Texture = SlimDX.Direct3D9.Texture;

namespace InfMapEditor.Rendering.Rendering
{
    /// <summary>
    /// FloorRenderer is responsible for rendering the terrain (aka "floor") geometry, which is a list of quads.
    /// </summary>
    /// <remarks>
    /// One vertex buffer is used alongside an index buffer. The VB is always maintained at the size of the screen,
    /// so that it only draws the visible tiles. It's cleared every frame and refilled with tiles, sorted by their
    /// texture. The IB is pre-filled because it does not change unless the viewport is resized.
    /// 
    /// Triangle Strip primitives are used, meaning 4 vertices/4 indices per quad for space efficiency's sake.
    /// Textures are loaded when first encountered, and retained thereafter.
    /// 
    /// 
    /// The only curious part is the way the tiling is done. Infantry Online floor tiles are 8x8 pixels, but the
    /// textures themselves are bigger. The tiles only show a portion of the texture, so that if you put enough tiles
    /// down, you will see the entire texture, which afterwards keeps repeating from the start of the texture again. 
    /// So the way we emulate the look is by calculating the (u,v) coordinates and wrapping around the 0...1 border.
    /// 
    /// Bilinear Filtering is used to mitigate the problems due to floating point imprecision when calculating the (u,v)'s,
    /// and it ends up looking just like it does in the game.
    /// </remarks>
    internal class FloorRenderer
    {
        internal static float RenderingZOffset = 0.8f;

        public FloorRenderer(Device device, Size initialSize)
        {
            this.device = device;

            int tileCountX = initialSize.Width/Grid.PixelsPerCell;
            int tileCountY = initialSize.Height/Grid.PixelsPerCell;
            int bufferSize = tileCountX*tileCountY*TexturedVertex.Size*6; // 6 vertices for one tile :(
            vertexBuffer = new VertexBuffer(device, bufferSize, Usage.None, VertexFormat.None, Pool.Managed);
            vertexDecl = new VertexDeclaration(device, TexturedVertex.VElements);
        }

        public void Render(Grid.GridRange range, Rectangle viewport)
        {
            // 1. Remove previous tiles.
            ////
            foreach(var cellList in visibleCells.Values)
            {
                cellList.Clear();
            }

            // 1. Fill
            ////
            foreach(Grid.GridCell cell in range)
            {
                if(cell.Data == null)
                    continue;

                BlobImage image = cell.Data.Floor.Image;

                if(!visibleCells.ContainsKey(image))
                {
                    Bitmap bitmap = image.Image;
                    using(Stream s = new MemoryStream())
                    {
                        bitmap.Save(s, ImageFormat.Png);
                        s.Seek(0, SeekOrigin.Begin);
                        Texture tex = Texture.FromStream(device, s, bitmap.Width, bitmap.Height, 0, Usage.None,
                                                         Format.Unknown,
                                                         Pool.Managed, Filter.None, Filter.None, 0);

                        textures.Add(image, tex);
                        visibleCells.Add(image, new List<TexturedVertex>());
                    }
                }

                List<TexturedVertex> currentList = visibleCells[image];

                int pixelsPerCell = 8;
                int x = (cell.X * pixelsPerCell) - viewport.X;
                int y = (cell.Y * pixelsPerCell) - viewport.Y;

                int w = 8;
                int h = 8;

                // Calculate the (u,v) we need to use based on the tile coordinates.
                float scaleW = 8.0f/image.Image.Width;
                float scaleH = 8.0f/image.Image.Height;

                float uStart = cell.X * scaleW;
                float vStart = cell.Y * scaleH;

                float uEnd = uStart + scaleW;
                float vEnd = vStart + scaleH;

                // Clockwise winding
                // TODO: Index these vertices! argh
                var v0 = new TexturedVertex(new Vector4(x, y, RenderingZOffset, 1.0f), new Vector2(uStart, vStart));
                var v1 = new TexturedVertex(new Vector4(x + w, y, RenderingZOffset, 1.0f), new Vector2(uEnd, vStart));
                var v2 = new TexturedVertex(new Vector4(x + w, y + h, RenderingZOffset, 1.0f),
                                            new Vector2(uEnd, vEnd));

                var v3 = new TexturedVertex(new Vector4(x, y, RenderingZOffset, 1.0f), new Vector2(uStart, vStart));
                var v4 = new TexturedVertex(new Vector4(x + w, y + h, RenderingZOffset, 1.0f),
                                            new Vector2(uEnd, vEnd));
                var v5 = new TexturedVertex(new Vector4(x, y + h, RenderingZOffset, 1.0f), new Vector2(uStart, vEnd));

                currentList.Add(v0);
                currentList.Add(v1);
                currentList.Add(v2);
                currentList.Add(v3);
                currentList.Add(v4);
                currentList.Add(v5);
            }

            // 2. Fill buffer.
            ////
            DataStream stream = vertexBuffer.Lock(0, 0, LockFlags.Discard);

            foreach (var vertexList in visibleCells)
            {
                if (vertexList.Value.Count == 0) continue;

                stream.WriteRange(vertexList.Value.ToArray());
            }

            vertexBuffer.Unlock();

            // 3. Draw.
            ////
            device.SetSamplerState(0, SamplerState.MinFilter, TextureFilter.Linear);
            device.SetStreamSource(0, vertexBuffer, 0, TexturedVertex.Size);
            device.VertexDeclaration = vertexDecl;

            int offset = 0;

            foreach (var vertexList in visibleCells)
            {
                var texture = textures[vertexList.Key];
                device.SetTexture(0, texture);
                int tilesToDraw = vertexList.Value.Count/3;
                device.DrawPrimitives(PrimitiveType.TriangleList, offset, tilesToDraw);
                offset += tilesToDraw*3;
            }
        }

        private Device device;
        private Dictionary<BlobImage, List<TexturedVertex>> visibleCells = new Dictionary<BlobImage, List<TexturedVertex>>();
        private Dictionary<BlobImage, Texture> textures = new Dictionary<BlobImage, Texture>();
        private VertexBuffer vertexBuffer;
        private VertexDeclaration vertexDecl;
    }
}
