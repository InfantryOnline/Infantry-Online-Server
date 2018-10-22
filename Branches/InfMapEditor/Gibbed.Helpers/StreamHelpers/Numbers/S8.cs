using System.IO;

namespace Gibbed.Helpers
{
	public static partial class StreamHelpers
	{
		public static sbyte ReadValueS8(this Stream stream)
		{
            return (sbyte)stream.ReadByte();
		}

        public static void WriteValueS8(this Stream stream, sbyte value)
		{
			stream.WriteByte((byte)value);
		}
	}
}
