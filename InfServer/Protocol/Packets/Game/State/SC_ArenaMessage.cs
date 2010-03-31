using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_ArenaMessage updates an arena message 
	/// </summary>
	public class SC_ArenaMessage : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public byte colour;
		public UInt16 timer;			//Timer seconds * 10
		public Int16 unk2;
		public string tickerMessage;

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.ArenaMessage;

		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_ArenaMessage()
			: base(TypeID)
		{
		}

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{
			Write((byte)TypeID);
			Write(colour);
			Write(timer);
			Write(unk2);
			Write(tickerMessage, 0);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Arena message.";
			}
		}
	}
}
