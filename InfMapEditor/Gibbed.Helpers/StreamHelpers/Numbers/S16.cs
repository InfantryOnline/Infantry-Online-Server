using System;
using System.Diagnostics;
using System.IO;

namespace Gibbed.Helpers
{
	public static partial class StreamHelpers
	{
		public static Int16 ReadValueS16(this Stream stream)
		{
            return stream.ReadValueS16(true);
		}

        public static Int16 ReadValueS16(this Stream stream, bool littleEndian)
        {
            byte[] data = new byte[2];
            int read = stream.Read(data, 0, 2);
            Debug.Assert(read == 2);
            Int16 value = BitConverter.ToInt16(data, 0);

            if (ShouldSwap(littleEndian))
            {
                value = value.Swap();
            }

            return value;
        }

		public static void WriteValueS16(this Stream stream, Int16 value)
		{
            stream.WriteValueS16(value, true);
		}

        public static void WriteValueS16(this Stream stream, Int16 value, bool littleEndian)
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
