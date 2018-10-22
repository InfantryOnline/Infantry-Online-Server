using System;
using System.Diagnostics;
using System.IO;

namespace Gibbed.Helpers
{
    public static partial class StreamHelpers
    {
        public static UInt64 ReadValueU64(this Stream stream)
        {
            return stream.ReadValueU64(true);
        }

        public static UInt64 ReadValueU64(this Stream stream, bool littleEndian)
        {
            byte[] data = new byte[8];
            int read = stream.Read(data, 0, 8);
            Debug.Assert(read == 8);
            UInt64 value = BitConverter.ToUInt64(data, 0);

            if (ShouldSwap(littleEndian))
            {
                value = value.Swap();
            }

            return value;
        }

        public static void WriteValueU64(this Stream stream, UInt64 value)
        {
            stream.WriteValueU64(value, true);
        }

        public static void WriteValueU64(this Stream stream, UInt64 value, bool littleEndian)
        {
            if (ShouldSwap(littleEndian))
            {
                value = value.Swap();
            }

            byte[] data = BitConverter.GetBytes(value);
            Debug.Assert(data.Length == 8);
            stream.Write(data, 0, 8);
        }
    }
}
