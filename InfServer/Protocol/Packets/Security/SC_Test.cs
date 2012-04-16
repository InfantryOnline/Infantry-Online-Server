using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_Test used for testing client functionality
	/// </summary>
	public class SC_Test : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
        public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.SendBanner;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_Test()
			: base(TypeID)
		{ }

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{	//Just need the id
			Write((byte)TypeID);

            Write((int)1);

            for (int i = 0; i < 432; i++)
            {
                Write((byte)200);
            }
		}

        /// <summary>
        /// Returns a meaningful of the packet's data
        /// </summary>
        public override string Dump
		{
			get
			{
				return "Test.";
			}
		}
	}
}
