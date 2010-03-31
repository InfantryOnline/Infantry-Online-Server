using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfServer.Protocol
{	
	// CRC32 Class
	/// Represents the checksum state for a specific flow of information
	///////////////////////////////////////////////////////
	public class CRC32
	{	//State variables
		public uint[] table;	//Generated CRC Table
		public uint key;		//Current CRC initial state

		public bool bActive;


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
			uint newKey = key;
            uint crc = table[(~newKey) & 0xFF] ^ 0xFFFFFF;

			uint tmp = (newKey >> 8) ^ crc;
			crc = (crc >> 8) ^ table[tmp & 0xFF];

			tmp = (newKey >> 16) ^ crc;
			crc = (crc >> 8) ^ table[tmp & 0xFF];

			tmp = (newKey >> 24) ^ crc;
			crc = (crc >> 8) ^ table[tmp & 0xFF];

            for (int i = 0; i < count; ++i) 
			{
                byte index = (byte)(((crc) & 0xff) ^ bytes[i + offset]);
                crc = (uint)((crc >> 8) ^ table[index]);
            }

            return ~crc;
        }
	}
}
