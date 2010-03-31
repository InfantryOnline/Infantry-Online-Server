using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{	/// <summary>
	/// CS_PlayerDrop is triggered when a player attempts to drop an item
	/// </summary>
	public class CS_PlayerDrop : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public Int32 unk1;
		public Int32 unk2;
		public Int16 unk3;
		public Int16 positionX;
		public Int16 positionY;
		public Int16 unk4;
		public UInt16 itemID;		//The item we're attempting to drop
		public UInt16 quantity;		//The amount we're dropping

		//Packet routing
		public const ushort TypeID = (ushort)Helpers.PacketIDs.C2S.PlayerDrop;
		static public Action<CS_PlayerDrop, Player> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
		public CS_PlayerDrop(ushort typeID, byte[] buffer, int index, int count)
			: base(typeID, buffer, index, count)
		{
		}

		/// <summary>
		/// Routes a new packet to various relevant handlers
		/// </summary>
		public override void Route()
		{	//Call all handlers!
			if (Handlers != null)
				Handlers(this, ((Client<Player>)_client)._obj);
		}

		/// <summary>
		/// Deserializes the data present in the packet contents into data fields in the class.
		/// </summary>
		public override void Deserialize()
		{
			unk1 = _contentReader.ReadInt32();
			unk2 = _contentReader.ReadInt32();
			unk3 = _contentReader.ReadInt16();
			positionX = _contentReader.ReadInt16();
			positionY = _contentReader.ReadInt16();
			unk4 = _contentReader.ReadInt16();
			itemID = _contentReader.ReadUInt16();
			quantity = _contentReader.ReadUInt16();
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Player drop notification";
			}
		}
	}
}
