using System.IO;
using System.Text;

namespace Gibbed.Helpers
{
    public static partial class StreamHelpers
    {
        public static Encoding DefaultEncoding = Encoding.ASCII;

        public static string ReadString(this Stream stream, uint size)
        {
            return stream.ReadStringInternalStatic(DefaultEncoding, size, false);
        }

        public static string ReadString(this Stream stream, uint size, bool trailingNull)
        {
            return stream.ReadStringInternalStatic(DefaultEncoding, size, trailingNull);
        }

        public static string ReadStringZ(this Stream stream)
        {
            return stream.ReadStringInternalDynamic(DefaultEncoding, '\0');
        }

        public static void WriteString(this Stream stream, string value)
        {
            stream.WriteStringInternalStatic(DefaultEncoding, value);
        }

        public static void WriteStringZ(this Stream stream, string value)
        {
            stream.WriteStringInternalDynamic(DefaultEncoding, value, '\0');
        }
    }
}
