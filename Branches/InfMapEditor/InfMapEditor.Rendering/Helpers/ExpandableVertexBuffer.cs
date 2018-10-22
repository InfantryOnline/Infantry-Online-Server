using System;
using System.Collections.Generic;
using SlimDX;
using SlimDX.Direct3D9;

namespace InfMapEditor.Rendering.Helpers
{
    internal class ExpandableVertexBuffer<T> : IDisposable where T : struct, IVertex
    {
        public ExpandableVertexBuffer(Device device, PrimitiveType primType, Usage usage, VertexFormat format, Pool pool)
        {
            this.device = device;
            this.primType = primType;
            this.usage = usage;
            this.format = format;
            this.pool = pool;
            this.isDirty = true;
        }

        public void AddVertex(T vertex)
        {
            vertices.Add(vertex);
            isDirty = true;
        }

        public void AddVertices(List<T> verts)
        {
            vertices.AddRange(verts);
            isDirty = true;
        }

        public void ClearVertices()
        {
            vertices = new List<T>();
        }

        public void Render()
        {
            if (vertices.Count == 0)
                return;

            if(isDirty)
            {
                isDirty = false;
                ResetBuffer();
            }

            
            device.SetStreamSource(0, vertexBuffer, 0, vertices[0].SizeInBytes);
            device.VertexDeclaration = declaration;
            device.DrawPrimitives(primType, 0, vertices.Count);
        }

        public void Dispose()
        {
            if (vertexBuffer != null && !vertexBuffer.Disposed)
            {
                vertexBuffer.Dispose();
            }

            if (declaration != null && !declaration.Disposed)
            {
                declaration.Dispose();
            }
        }

        private void ResetBuffer()
        {
            if (vertexBuffer != null && !vertexBuffer.Disposed)
            {
                vertexBuffer.Dispose();
            }

            int sizeOfPrimitive = vertices[0].SizeInBytes;

            vertexBuffer = new VertexBuffer(device, sizeOfPrimitive*vertices.Count, usage, format, pool);
            declaration = new VertexDeclaration(device, vertices[0].VertexElements);

            DataStream stream = vertexBuffer.Lock(0, sizeOfPrimitive*vertices.Count, LockFlags.None);
            stream.WriteRange(vertices.ToArray());
            vertexBuffer.Unlock();
        }

        private List<T> vertices = new List<T>();
        private bool isDirty;
        private readonly Device device;
        private readonly Usage usage;
        private readonly VertexFormat format;
        private readonly Pool pool;
        private readonly PrimitiveType primType;
        private VertexDeclaration declaration;
        private VertexBuffer vertexBuffer;
    }
}
