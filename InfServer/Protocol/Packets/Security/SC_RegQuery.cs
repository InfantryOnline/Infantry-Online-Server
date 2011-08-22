using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_RegQuery requests an entry in the registry
	/// </summary>
	public class SC_RegQuery : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public int unk;
		public string location;

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.RegQuery;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_RegQuery()
			: base(TypeID)
		{ }

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{	//Just need the id
			Write((byte)TypeID);

			Write(unk);
			Write(location, 0);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Registry request";
			}
		}
	}
}
