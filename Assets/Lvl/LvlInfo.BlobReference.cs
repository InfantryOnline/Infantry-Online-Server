using System.Runtime.InteropServices;

namespace Assets
{
    public partial class LvlInfo
    {
        [StructLayout(LayoutKind.Sequential, Size = 64, CharSet = CharSet.Ansi)]
        public struct BlobReference
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string FileName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string Id;
        }
    }
}
