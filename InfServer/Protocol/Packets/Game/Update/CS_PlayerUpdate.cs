using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

using Assets;


namespace InfServer.Protocol
{	/// <summary>
	/// CS_PlayerUpdate is sent by the client to update the player's
	/// position and status.
	/// </summary>
	public class CS_PlayerUpdate : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public bool bIgnored;				//Was the update ignored?

		public Int16 energy;				//Current energy
		public UInt32 tickCRC;				//The checksum of the client's tickcount
		public UInt16 playerID;				//The player's current id
		public Int16 itemID;				//The ID of the item the player is using, if any
		public Int32 tickCount;				//The tickcount
		public Int16 health;				//Player's health
		public Int16 velocityX;				//Velocity info
		public Int16 velocityY;				//
		public Int16 velocityZ;				//
		public Int16 positionX;				//Positional info
		public Int16 positionY;				//
		public Int16 positionZ;				//
		public byte yaw;					//Our rotation
		public UInt16 direction;			//The direction we're attempting to move on
		public byte unk1;					//Unknown (flags?)

		//Spectator
		public UInt16 playerSpectating;		//The ID of the player we're spectating

		public List<ushort> activeEquip;	//Any additional equipment we may have active

		//Packet routing
		public const ushort TypeID = (ushort)Helpers.PacketIDs.C2S.PlayerUpdate;
		static public Action<CS_PlayerUpdate, Player> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
		public CS_PlayerUpdate(ushort typeID, byte[] buffer, int index, int count)
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
		{	//Should we be serializing?
			Player player = ((Client<Player>)_client)._obj;

			bIgnored = player._bIgnoreUpdates;
			if (player._bIgnoreUpdates)
				return;

			//Read in the bulk of the packet
			energy = _contentReader.ReadInt16();
			tickCRC = _contentReader.ReadUInt32();
			playerID = _contentReader.ReadUInt16();
			itemID = _contentReader.ReadInt16();
			tickCount = _contentReader.ReadInt32();

			//What we read in next depends on our vehicle type
			VehInfo.Types vehType;
			int structRead = 0;

			if (player._occupiedVehicle == null)
				vehType = VehInfo.Types.Car;
			else
				vehType = player._occupiedVehicle._type.Type;

			switch (vehType)
			{
				case VehInfo.Types.Car:
					health = _contentReader.ReadInt16();
					velocityX = _contentReader.ReadInt16();
					velocityY = _contentReader.ReadInt16();
					velocityZ = _contentReader.ReadInt16();
					positionX = _contentReader.ReadInt16();
					positionY = _contentReader.ReadInt16();
					positionZ = _contentReader.ReadInt16();
					yaw = _contentReader.ReadByte();
					direction = _contentReader.ReadUInt16();
					unk1 = _contentReader.ReadByte();

					structRead = 18;
					break;

				case VehInfo.Types.Dependent:
					velocityX = _contentReader.ReadInt16();
					velocityY = _contentReader.ReadInt16();
					velocityZ = _contentReader.ReadInt16();

					positionX = _contentReader.ReadInt16();
					positionY = _contentReader.ReadInt16();
					positionZ = _contentReader.ReadInt16();

					health = _contentReader.ReadInt16();
					yaw = _contentReader.ReadByte();
					yaw = _contentReader.ReadByte();
					yaw = _contentReader.ReadByte();
					unk1 = _contentReader.ReadByte();

					structRead = 18;
					break;

				case VehInfo.Types.Spectator:
					positionX = _contentReader.ReadInt16();
					positionY = _contentReader.ReadInt16();
					playerSpectating = _contentReader.ReadUInt16();

					structRead = 6;
					break;
			}
			
			//Look for any additional equipment
			activeEquip = new List<ushort>();

			int num = (base._size - 14 - structRead) / 2;
			for (int i = 0; i < num; ++i)
				activeEquip.Add(_contentReader.ReadUInt16());
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Player update";
			}
		}
	}
}
