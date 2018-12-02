using System.IO;

namespace Gibbed.Helpers
{
	public static partial class StreamHelpers
	{
		public static byte ReadValueU8(this Stream stream)
		{
			return (byte)stream.ReadByte();
		}

		public static void WriteValueU8(this Stream stream, byte value)
		{
			stream.WriteByte(value);
		}
	}
}
