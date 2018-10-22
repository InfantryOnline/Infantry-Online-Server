using System.IO;

namespace Gibbed.Helpers
{
	public static partial class StreamHelpers
	{
		public static int ReadAligned(this Stream stream, byte[] buffer, int offset, int size, int align)
		{
			if (size == 0)
			{
				return 0;
			}

			int read = stream.Read(buffer, offset, size);
			int skip = size % align;

			// Skip aligned bytes
			if (skip > 0)
			{
				stream.Seek(align - skip, SeekOrigin.Current);
			}

			return read;
		}

		public static void WriteAligned(this Stream stream, byte[] buffer, int offset, int size, int align)
		{
			if (size == 0)
			{
				return;
			}

			stream.Write(buffer, offset, size);
			int skip = size % align;

			// this is a dumbfuck way to do this but it'll work for now
			if (skip > 0)
			{
				byte[] junk = new byte[align - skip];
				stream.Write(junk, 0, align - skip);
			}
		}
	}
}
