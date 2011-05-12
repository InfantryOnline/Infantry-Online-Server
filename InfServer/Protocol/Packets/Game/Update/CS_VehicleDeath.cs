using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{	/// <summary>
	/// CS_VehicleDeath is triggered when a player dies
	/// </summary>
	public class CS_VehicleDeath : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public Helpers.KillType type;		//The way in which the player was killed
		public UInt32 killerPlayerID;		//The id of the player who killed me
		public UInt16 unk1;
		public UInt16 killedID;				//ID of the vehicle/player which was killed		
		public Int16 positionX;
		public Int16 positionY;
		public UInt16 unk2;

		//Packet routing
		public const ushort TypeID = (ushort)Helpers.PacketIDs.C2S.PlayerDeath;
		static public Action<CS_VehicleDeath, Player> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
		public CS_VehicleDeath(ushort typeID, byte[] buffer, int index, int count)
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
			type = (Helpers.KillType)_contentReader.ReadByte();
			killerPlayerID = _contentReader.ReadUInt32();
			unk1 = _contentReader.ReadUInt16();
			killedID = _contentReader.ReadUInt16();
			positionX = _contentReader.ReadInt16();
			positionY = _contentReader.ReadInt16();
			unk2 = _contentReader.ReadUInt16();
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Vehicle death update";
			}
		}
	}
}
