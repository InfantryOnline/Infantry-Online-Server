using System.Runtime.InteropServices;

namespace Assets
{
    public partial class LvlInfo
    {
        [StructLayout(LayoutKind.Explicit, Size = 5152)]
        private struct Header
        {
            [FieldOffset(0x0000)]
            public int Version;

            [FieldOffset(0x0004)]
            public int Width;

            [FieldOffset(0x0008)]
            public int Height;

            [FieldOffset(0x000C)]
            public int EntityCount;

            [FieldOffset(0x0010)]
            public int FloorCount;

            [FieldOffset(0x0014)]
            public int ObjectCount;

            [FieldOffset(0x0820)]
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
            public int[] TerrainLookup;

            [FieldOffset(0x0A20)]
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public short[] PhysicsLow;

            [FieldOffset(0x0A60)]
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public short[] PhysicsHigh;

            [FieldOffset(0x0AA0)]
            public uint Unknown0AA0;

            [FieldOffset(0x0AA4)]
            public uint Unknown0AA4;

            [FieldOffset(0x0AA8)]
            public uint Unknown0AA8;

            [FieldOffset(0x0AAC)]
            public uint Unknown0AAC;
        }
    }
}
