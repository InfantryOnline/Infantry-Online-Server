using System;
using System.Diagnostics;
using System.IO;

namespace Gibbed.Helpers
{
	public static partial class StreamHelpers
	{
		public static Double ReadValueF64(this Stream stream)
		{
            return stream.ReadValueF64(true);
		}

        public static Double ReadValueF64(this Stream stream, bool littleEndian)
        {
            byte[] data = new byte[8];
            int read = stream.Read(data, 0, 8);
            Debug.Assert(read == 8);

            if (ShouldSwap(littleEndian))
            {
                return BitConverter.Int64BitsToDouble(BitConverter.ToInt64(data, 0).Swap());
            }
            else
            {
                return BitConverter.ToDouble(data, 0);
            }
        }

		public static void WriteValueF64(this Stream stream, Double value)
		{
            stream.WriteValueF64(value, true);
		}

        public static void WriteValueF64(this Stream stream, Double value, bool littleEndian)
        {
            byte[] data;
            if (ShouldSwap(littleEndian))
            {
                data = BitConverter.GetBytes(BitConverter.DoubleToInt64Bits(value).Swap());
            }
            else
            {
                data = BitConverter.GetBytes(value);
            }
            Debug.Assert(data.Length == 8);
            stream.Write(data, 0, 8);
        }
	}
}
