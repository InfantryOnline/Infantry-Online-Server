using System;
using System.Diagnostics;
using System.IO;

namespace Gibbed.Helpers
{
	public static partial class StreamHelpers
	{
		public static Int64 ReadValueS64(this Stream stream)
		{
			return stream.ReadValueS64(true);
		}

        public static Int64 ReadValueS64(this Stream stream, bool littleEndian)
        {
            byte[] data = new byte[8];
			int read = stream.Read(data, 0, 8);
            Debug.Assert(read == 8);
            Int64 value = BitConverter.ToInt64(data, 0);

            if (ShouldSwap(littleEndian))
            {
                value = value.Swap();
            }

            return value;
        }

		public static void WriteValueS64(this Stream stream, Int64 value)
		{
            stream.WriteValueS64(value, true);
		}

        public static void WriteValueS64(this Stream stream, Int64 value, bool littleEndian)
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
