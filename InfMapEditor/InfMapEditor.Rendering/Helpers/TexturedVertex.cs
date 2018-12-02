using SlimDX;
using SlimDX.Direct3D9;

namespace InfMapEditor.Rendering.Helpers
{
    internal struct TexturedVertex : IVertex
    {
        public Vector4 Position;

        public Vector2 TexCoord;

        public TexturedVertex(Vector4 position, Vector2 texCoord)
        {
            Position = position;
            TexCoord = texCoord;
        }

        public int SizeInBytes { get { return 24; } }

        public static int Size { get { return 24; } }

        public VertexElement[] VertexElements { get { return Elements;  } }

        public static VertexElement[] VElements { get { return Elements; } }

        private static readonly VertexElement[] Elements = new[]
                                                               {
                                                                   new VertexElement(0, 0, DeclarationType.Float4,
                                                                                     DeclarationMethod.Default,
                                                                                     DeclarationUsage.
                                                                                         PositionTransformed, 0)
                                                                   ,
                                                                   new VertexElement(0, 16, DeclarationType.Float2,
                                                                                     DeclarationMethod.Default,
                                                                                     DeclarationUsage.
                                                                                         TextureCoordinate, 0),

                                                                   VertexElement.VertexDeclarationEnd
                                                               };
    }
}
