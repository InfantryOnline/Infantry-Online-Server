using System;
using System.Diagnostics;
using System.IO;

namespace Gibbed.Helpers
{
	public static partial class StreamHelpers
	{
		public static Single ReadValueF32(this Stream stream)
		{
            return stream.ReadValueF32(true);
		}

        public static Single ReadValueF32(this Stream stream, bool littleEndian)
        {
            byte[] data = new byte[4];
            int read = stream.Read(data, 0, 4);
            Debug.Assert(read == 4);

            if (ShouldSwap(littleEndian))
            {
                return BitConverter.ToSingle(BitConverter.GetBytes(BitConverter.ToInt32(data, 0).Swap()), 0);
            }
            else
            {
                return BitConverter.ToSingle(data, 0);
            }
        }

        public static void WriteValueF32(this Stream stream, Single value)
        {
            stream.WriteValueF32(value, true);
        }

        public static void WriteValueF32(this Stream stream, Single value, bool littleEndian)
        {
            byte[] data;
            if (ShouldSwap(littleEndian))
            {
                data = BitConverter.GetBytes(BitConverter.ToInt32(BitConverter.GetBytes(value), 0).Swap());
            }
            else
            {
                data = BitConverter.GetBytes(value);
            }
            Debug.Assert(data.Length == 4);
            stream.Write(data, 0, 4);
        }
	}
}
