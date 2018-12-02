using System;
using System.Diagnostics;
using System.IO;

namespace Gibbed.Helpers
{
	public static partial class StreamHelpers
	{
		public static Int32 ReadValueS32(this Stream stream)
		{
            return stream.ReadValueS32(true);
		}

        public static Int32 ReadValueS32(this Stream stream, bool littleEndian)
        {
            byte[] data = new byte[4];
            int read = stream.Read(data, 0, 4);
            Debug.Assert(read == 4);
            Int32 value = BitConverter.ToInt32(data, 0);

            if (ShouldSwap(littleEndian))
            {
                value = value.Swap();
            }

            return value;
        }

		public static void WriteValueS32(this Stream stream, Int32 value)
		{
            stream.WriteValueS32(value, true);
		}

        public static void WriteValueS32(this Stream stream, Int32 value, bool littleEndian)
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
