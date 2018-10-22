using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Gibbed.Helpers
{
	public static partial class StreamHelpers
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

        public static T ReadStructure<T>(this Stream stream, int size)
        {
            GCHandle handle;
            int structureSize;
            byte[] buffer;

            structureSize = Marshal.SizeOf(typeof(T));

            buffer = new byte[Math.Max(structureSize, size)];

            if (stream.Read(buffer, 0, size) != size)
            {
                throw new InvalidOperationException("could not read all of data for structure");
            }

            handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

            T structure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));

            handle.Free();

            return structure;
        }

		public static void WriteStructure<T>(this Stream stream, T structure)
		{
			GCHandle handle;
			int structureSize;
			byte[] buffer;

			structureSize = Marshal.SizeOf(typeof(T));
			buffer = new byte[structureSize];
			handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

			Marshal.StructureToPtr(structure, handle.AddrOfPinnedObject(), false);

			handle.Free();

			stream.Write(buffer, 0, buffer.Length);
		}

        public static void WriteStructure<T>(this Stream stream, T structure, int size)
        {
            GCHandle handle;
            int structureSize;
            byte[] buffer;

            structureSize = Marshal.SizeOf(typeof(T));
            buffer = new byte[Math.Max(structureSize, size)];
            handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

            Marshal.StructureToPtr(structure, handle.AddrOfPinnedObject(), false);

            handle.Free();

            stream.Write(buffer, 0, buffer.Length);
        }
	}
}
