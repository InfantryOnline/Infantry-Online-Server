using System;
using System.Diagnostics;
using System.IO;

namespace Gibbed.Helpers
{
    public static partial class StreamHelpers
    {
        public static UInt32 ReadValueU24(this Stream stream)
        {
            return stream.ReadValueU24(true);
        }

        public static UInt32 ReadValueU24(this Stream stream, bool littleEndian)
        {
            byte[] data = new byte[4];
            int read = stream.Read(data, 0, 3);
            Debug.Assert(read == 3);
            UInt32 value = BitConverter.ToUInt32(data, 0);

            if (ShouldSwap(littleEndian))
            {
                value = value.Swap24();
            }

            return value & 0xFFFFFF;
        }

        public static void WriteValueU24(this Stream stream, UInt32 value)
        {
            stream.WriteValueU24(value, true);
        }

        public static void WriteValueU24(this Stream stream, UInt32 value, bool littleEndian)
        {
            if (ShouldSwap(littleEndian))
            {
                value = value.Swap24();
            }

            value &= 0xFFFFFF;

            byte[] data = BitConverter.GetBytes(value);
            Debug.Assert(data.Length == 4);
            stream.Write(data, 0, 3);
        }
    }
}
