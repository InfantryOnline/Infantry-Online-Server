using System;
using System.IO;

namespace Gibbed.Helpers
{
    public static partial class StreamHelpers
    {
        public static Guid ReadValueGuid(this Stream stream, bool littleEndian)
        {
            Int32 a = stream.ReadValueS32(littleEndian);
            Int16 b = stream.ReadValueS16(littleEndian);
            Int16 c = stream.ReadValueS16(littleEndian);
            byte[] d = new byte[8];
            stream.Read(d, 0, d.Length);
            return new Guid(a, b, c, d);
        }

        public static Guid ReadValueGuid(this Stream stream)
        {
            return stream.ReadValueGuid(true);
        }

        public static void WriteValueGuid(this Stream stream, Guid value, bool littleEndian)
        {
            byte[] data = value.ToByteArray();
            stream.WriteValueS32(BitConverter.ToInt32(data, 0), littleEndian);
            stream.WriteValueS16(BitConverter.ToInt16(data, 4), littleEndian);
            stream.WriteValueS16(BitConverter.ToInt16(data, 6), littleEndian);
            stream.Write(data, 8, 8);
        }

        public static void WriteValueGuid(this Stream stream, Guid value)
        {
            stream.WriteValueGuid(value, true);
        }
    }
}
