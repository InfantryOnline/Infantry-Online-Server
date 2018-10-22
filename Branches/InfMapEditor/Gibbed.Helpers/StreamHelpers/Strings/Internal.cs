using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Gibbed.Helpers
{
    public static partial class StreamHelpers
    {
        internal static string ReadStringInternalStatic(this Stream stream, Encoding encoding, uint size, bool trailingNull)
        {
            byte[] data = new byte[size];
            stream.Read(data, 0, data.Length);

            string value = encoding.GetString(data, 0, data.Length);

            if (trailingNull == true)
            {
                var position = value.IndexOf('\0');
                if (position >= 0)
                {
                    value = value.Substring(0, position);
                }
            }

            return value;
        }

        internal static void WriteStringInternalStatic(this Stream stream, Encoding encoding, string value)
        {
            byte[] data = encoding.GetBytes(value);
            stream.Write(data, 0, data.Length);
        }

        internal static string ReadStringInternalDynamic(this Stream stream, Encoding encoding, char end)
        {
            int characterSize = encoding.GetByteCount("e");
            Debug.Assert(characterSize == 1 || characterSize == 2 || characterSize == 4);
            string characterEnd = end.ToString();

            int i = 0;
            byte[] data = new byte[128 * characterSize];

            while (true)
            {
                if (i + characterSize > data.Length)
                {
                    Array.Resize(ref data, data.Length + (128 * characterSize));
                }

                int read = stream.Read(data, i, characterSize);
                Debug.Assert(read == characterSize);

                if (encoding.GetString(data, i, characterSize) == characterEnd)
                {
                    break;
                }

                i += characterSize;
            }

            if (i == 0)
            {
                return "";
            }

            return encoding.GetString(data, 0, i);
        }

        internal static void WriteStringInternalDynamic(this Stream stream, Encoding encoding, string value, char end)
        {
            byte[] data;
            
            data = encoding.GetBytes(value);
            stream.Write(data, 0, data.Length);

            data = encoding.GetBytes(end.ToString());
            stream.Write(data, 0, data.Length);
        }
    }
}
