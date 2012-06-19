using System.IO;

namespace Assets
{
    /// <summary>
    /// Represents the CRC32 checksum state for a specific flow of information.
    /// </summary>
	public class CRC32
	{	//State variables
		public uint[] table;	//Generated CRC Table
		public uint key;		//Current CRC initial state

		public bool bActive;

        public static bool fileIsFree(string path)
        {
            try
            {
                if (!File.Exists(path)) return false;
                using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None)) { };
                return true;
            }
            catch (System.IO.IOException) { return false; }
        }

		public static uint fileChecksum(string filename)
		{
            //Wait until the file has been freed
            while (!fileIsFree(filename)) { }
			byte[] rawData = File.ReadAllBytes(filename);
			CRC32 checkSumCalc = new CRC32();
			return checkSumCalc.ComputeChecksum(rawData, 0, rawData.Length);
		}

		/// <summary>
		/// Initializes the CRC table for use
		/// </summary>
		public CRC32()
		{
			uint poly = 0xedb88320;
			table = new uint[256];
			uint temp = 0;

			for (uint i = 0; i < table.Length; ++i)
			{
				temp = i;
				for (int j = 8; j > 0; --j)
				{
					if ((temp & 1) == 1)
						temp = (uint)((temp >> 1) ^ poly);
					else
						temp >>= 1;
				}

				table[i] = temp;
			}
		}

		/// <summary>
		/// Computes a checksum based on the given bytes
		/// </summary>
		public uint ComputeChecksum(byte[] bytes, int offset, int count)
		{	//Mangle the key a little
			uint crc = 0xFFFFFFFF;
			for (int i = 0; i < count; ++i)
			{
				byte index = (byte)(((crc) & 0xff) ^ bytes[i + offset]);
				crc = (uint)((crc >> 8) ^ table[index]);
			}

			return ~crc;
		}
	}
}
