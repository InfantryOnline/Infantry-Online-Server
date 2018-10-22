using SlimDX;
using SlimDX.Direct3D9;

namespace InfMapEditor.Rendering.Helpers
{
    internal struct ColorVertex : IVertex
    {
        public Vector4 Position;

        public int Color;

        public ColorVertex(Vector4 position, int color)
        {
            Position = position;
            Color = color;
        }

        int IVertex.SizeInBytes
        {
            get { return 20; }
        }

        public VertexElement[] VertexElements
        {
            get { return Elements; }
        }

        private static readonly VertexElement[] Elements = new[]
                                                               {
                                                                   new VertexElement(0, 0, DeclarationType.Float4,
                                                                                     DeclarationMethod.Default,
                                                                                     DeclarationUsage.
                                                                                         PositionTransformed, 0)
                                                                   ,
                                                                   new VertexElement(0, 16, DeclarationType.Color,
                                                                                     DeclarationMethod.Default,
                                                                                     DeclarationUsage.Color, 0),

                                                                   VertexElement.VertexDeclarationEnd
                                                               };
    }
}
