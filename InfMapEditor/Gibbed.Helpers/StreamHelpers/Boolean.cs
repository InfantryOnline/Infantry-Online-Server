using System.IO;

namespace Gibbed.Helpers
{
	public static partial class StreamHelpers
	{
		public static bool ReadValueBoolean(this Stream stream)
		{
            return stream.ReadValueB8();
		}

		public static void WriteValueBoolean(this Stream stream, bool value)
		{
			stream.WriteValueB8(value);
		}

        public static bool ReadValueB8(this Stream stream)
        {
            return stream.ReadValueU8() > 0 ? true : false;
        }

        public static void WriteValueB8(this Stream stream, bool value)
        {
            stream.WriteValueU8((byte)(value == true ? 1 : 0));
        }

        public static bool ReadValueB32(this Stream stream)
        {
            return ((stream.ReadValueU32() & 1) == 1) ? true : false;
        }

        public static void WriteValueB32(this Stream stream, bool value)
        {
            stream.WriteValueU32((byte)(value == true ? 1 : 0));
        }
	}
}
