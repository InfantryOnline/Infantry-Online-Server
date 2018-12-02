using System;
using System.Diagnostics;
using System.IO;

namespace Gibbed.Helpers
{
    public static partial class StreamHelpers
    {
        public static UInt32 ReadValueU32(this Stream stream)
        {
            return stream.ReadValueU32(true);
        }

        public static UInt32 ReadValueU32(this Stream stream, bool littleEndian)
        {
            byte[] data = new byte[4];
            int read = stream.Read(data, 0, 4);
            Debug.Assert(read == 4);
            UInt32 value = BitConverter.ToUInt32(data, 0);

            if (ShouldSwap(littleEndian))
            {
                value = value.Swap();
            }

            return value;
        }

        public static void WriteValueU32(this Stream stream, UInt32 value)
        {
            stream.WriteValueU32(value, true);
        }

        public static void WriteValueU32(this Stream stream, UInt32 value, bool littleEndian)
        {
            if (ShouldSwap(littleEndian))
            {
                value = value.Swap();
            }

            byte[] data = BitConverter.GetBytes(value);
            Debug.Assert(data.Length == 4);
            stream.Write(data, 0, 4);
        }
    }
}
