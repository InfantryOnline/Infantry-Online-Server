using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Assets
{
    internal static class StreamHelpers
    {
        public static T ReadStructure<T>(this Stream stream)
        {
            GCHandle handle;
            int structureSize;
            byte[] buffer;

            structureSize = Marshal.SizeOf(typeof(T));
            buffer = new byte[structureSize];

            if (stream.Read(buffer, 0, structureSize) != structureSize)
            {
                throw new InvalidOperationException("could not read all of data for structure");
            }

            handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            T structure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();

            return structure;
        }

        public static byte[] DecompressRLE(this Stream stream, int length)
        {
            byte[] data = new byte[length];
            int count = 0;

            while (count < length)
            {
                int repeat = stream.ReadByte();

                if (repeat == 0)
                {
                    repeat = stream.ReadByte();
                    repeat <<= 8;
                    repeat |= stream.ReadByte();
                }

                byte value = (byte)stream.ReadByte();

                for (int i = 0; i < repeat; i++)
                {
                    data[count + i] = value;
                }

                count += repeat;
            }

            return data;
        }

        public static int ReadInt(this Stream stream)
        {
            var b = new byte[4];
            stream.Read(b, 0, 4);
            return BitConverter.ToInt32(b, 0);
        }

        public static short ReadShort(this Stream stream)
        {
            var b = new byte[2];
            stream.Read(b, 0, 2);
            return BitConverter.ToInt16(b, 0);
        }

        public static byte[] ReadByteArray(this Stream stream, int count)
        {
            var b = new byte[count];
            stream.Read(b, 0, count);
            return b;
        }

        public static void Skip(this Stream stream, int count)
        {
            for (var i = 0; i < count; i++) stream.ReadByte();
        }
    }
}
