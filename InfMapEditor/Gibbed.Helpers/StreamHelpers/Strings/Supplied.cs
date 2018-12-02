using System.IO;
using System.Text;

namespace Gibbed.Helpers
{
    public static partial class StreamHelpers
    {
        public static string ReadString(this Stream stream, uint size, Encoding encoding)
        {
            return stream.ReadStringInternalStatic(encoding, size, false);
        }

        public static string ReadString(this Stream stream, int size, Encoding encoding)
        {
            return stream.ReadStringInternalStatic(encoding, (uint)size, false);
        }

        public static string ReadString(this Stream stream, uint size, bool trailingNull, Encoding encoding)
        {
            return stream.ReadStringInternalStatic(encoding, size, trailingNull);
        }

        public static string ReadString(this Stream stream, int size, bool trailingNull, Encoding encoding)
        {
            return stream.ReadStringInternalStatic(encoding, (uint)size, trailingNull);
        }

        public static string ReadStringZ(this Stream stream, Encoding encoding)
        {
            return stream.ReadStringInternalDynamic(encoding, '\0');
        }

        public static void WriteString(this Stream stream, string value, Encoding encoding)
        {
            stream.WriteStringInternalStatic(encoding, value);
        }

        public static void WriteStringZ(this Stream stream, string value, Encoding encoding)
        {
            stream.WriteStringInternalDynamic(encoding, value, '\0');
        }
    }
}
