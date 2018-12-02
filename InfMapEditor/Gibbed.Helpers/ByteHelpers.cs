using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Gibbed.Helpers
{
	public static partial class ByteHelpers
	{
        /// <summary>
        /// Set the contents of a byte array to the specified value.
        /// </summary>
        public static void Reset(this byte[] data, byte value)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = value;
            }
        }

        public static T ToStructure<T>(this byte[] data, int index)
		{
			int size = Marshal.SizeOf(typeof(T));

			if (index + size > data.Length)
			{
				throw new Exception("not enough data to fit the structure");
			}

			byte[] buffer = new byte[size];
			Array.Copy(data, index, buffer, 0, size);

			GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
			T structure = (T)(Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T)));
			handle.Free();
			return structure;
		}

		public static T ToStructure<T>(this byte[] data)
		{
			return data.ToStructure<T>(0);
		}
	}
}
