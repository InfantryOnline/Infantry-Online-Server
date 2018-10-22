using SlimDX.Direct3D9;

namespace InfMapEditor.Rendering.Helpers
{
    internal interface IVertex
    {
        int SizeInBytes { get; }

        VertexElement[] VertexElements { get; }
    }
}
