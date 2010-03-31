using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

using Assets;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_PlayerUpdate is used to inform clients of their enemies' movements 
	/// </summary>
	public class SC_PlayerUpdate : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public Helpers.Update_Type updateType;
		public Player player;				//The player we're updating, if any
		public Vehicle vehicle;				//The vehicle we're updating

		public Int16 itemID;				//The ID of the item the player is using, if any
		public UInt16 vehicleID;			//The player's current vehicle's id, if any
		public Int16 tickCount;				//The tickcount
		public UInt16 playerID;				//The player's current id
		public UInt16 unk2;
		public Int16 health;				//Player's health
		public Int16 velocityX;				//Velocity info
		public Int16 velocityY;				//
		public Int16 velocityZ;				//
		public Int16 positionX;				//Positional info
		public Int16 positionY;				//
		public Int16 positionZ;				//
		public byte yaw;					//Our rotation
		public UInt16 direction;			//The direction we're attempting to move on
		public byte unk3;					//Unknown (flags?)

		public List<ushort> activeEquip;	//Any additional equipment we may have active

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.PlayerUpdate;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_PlayerUpdate()
			: base(TypeID)
		{
			activeEquip = new List<ushort>();
		}

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{
			Write((byte)TypeID);

			Write((byte)updateType);
			Write(itemID);
			Write(vehicleID);
			Write(tickCount);
			Write(unk2);
			Write(playerID);
			Write(health);
			Write(velocityX);
			Write(velocityY);
			Write(velocityZ);
			Write(positionX);
			Write(positionY);
			Write(positionZ);
			Write(yaw);
			Write(direction);
			Write(unk3);

			//Write in all additional equipment
			foreach (ushort equip in activeEquip)
				Write(equip);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Player update.";
			}
		}
	}
}
