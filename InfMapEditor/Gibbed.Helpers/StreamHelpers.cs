using System;
using System.IO;

namespace Gibbed.Helpers
{
    public static partial class StreamHelpers
    {
        internal static bool ShouldSwap(bool littleEndian)
        {
            if (littleEndian == true && BitConverter.IsLittleEndian == false)
            {
                return true;
            }
            else if (littleEndian == false && BitConverter.IsLittleEndian == true)
            {
                return true;
            }

            return false;
        }

        public static MemoryStream ReadToMemoryStream(this Stream stream, long size, int buffer)
        {
            MemoryStream memory = new MemoryStream();

            long left = size;
            byte[] data = new byte[buffer];
            while (left > 0)
            {
                int block = (int)(Math.Min(left, data.Length));
                stream.Read(data, 0, block);
                memory.Write(data, 0, block);
                left -= block;
            }

            memory.Seek(0, SeekOrigin.Begin);
            return memory;
        }

        public static MemoryStream ReadToMemoryStream(this Stream stream, long size)
        {
            return stream.ReadToMemoryStream(size, 0x4000);
        }

        public static void WriteFromStream(this Stream stream, Stream input, long size, int buffer)
        {
            long left = size;
            byte[] data = new byte[buffer];
            while (left > 0)
            {
                int block = (int)(Math.Min(left, data.Length));
                input.Read(data, 0, block);
                stream.Write(data, 0, block);
                left -= block;
            }
        }

        public static void WriteFromStream(this Stream stream, Stream input, long size)
        {
            stream.WriteFromStream(input, size, 0x4000);
        }
    }
}
