using System;
using System.Diagnostics;
using System.IO;

namespace Gibbed.Helpers
{
    public static partial class StreamHelpers
    {
        public static UInt16 ReadValueU16(this Stream stream)
        {
            return stream.ReadValueU16(true);
        }

        public static UInt16 ReadValueU16(this Stream stream, bool littleEndian)
        {
            byte[] data = new byte[2];
            int read = stream.Read(data, 0, 2);
            Debug.Assert(read == 2);
            UInt16 value = BitConverter.ToUInt16(data, 0);

            if (ShouldSwap(littleEndian))
            {
                value = value.Swap();
            }

            return value;
        }

        public static void WriteValueU16(this Stream stream, UInt16 value)
        {
            stream.WriteValueU16(value, true);
        }

        public static void WriteValueU16(this Stream stream, UInt16 value, bool littleEndian)
        {
            if (ShouldSwap(littleEndian))
            {
                value = value.Swap();
            }

            byte[] data = BitConverter.GetBytes(value);
            Debug.Assert(data.Length == 2);
            stream.Write(data, 0, 2);
        }
    }
}
