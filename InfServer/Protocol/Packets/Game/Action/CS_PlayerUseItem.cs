using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{	/// <summary>
	/// CS_PlayerUseItem is triggered when a player attempts to use a warp/maker item
	/// </summary>
	public class CS_PlayerUseItem : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public UInt32 targetVehicle;
		public Int16 unk1;
		public Int16 unk2;
		public Int16 itemID;
		public Int16 posX;
		public Int16 posY;
		public byte yaw;
		public byte unk3;

		//Packet routing
		public const ushort TypeID = (ushort)Helpers.PacketIDs.C2S.PlayerUseItem;
		static public Action<CS_PlayerUseItem, Player> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
		public CS_PlayerUseItem(ushort typeID, byte[] buffer, int index, int count)
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
			targetVehicle = _contentReader.ReadUInt32();
			unk1 = _contentReader.ReadInt16();
			unk2 = _contentReader.ReadInt16();
			itemID = _contentReader.ReadInt16();
			posX = _contentReader.ReadInt16();
			posY = _contentReader.ReadInt16();
			yaw = _contentReader.ReadByte();
			unk3 = _contentReader.ReadByte();
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Player use item request";
			}
		}
	}
}
