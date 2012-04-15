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
		public int tickUpdate;								//The time when this update was made

		public Player player;								//The player we're updating, if any
		public Vehicle vehicle;								//The vehicle we're updating

		public Int16 itemID;								//Id of the item we fired
		public List<ushort> activeEquip;					//Any additional equipment we may have active
		public List<ItemInfo.UtilityItem> activeUtilities;	//Alternative list type

		public bool bBot;									//Is this a bot?

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
		}

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{
			Write((byte)TypeID);

			Write((byte)0);			//Really not sure what this is

			if (player != null)
				Write((itemID == 0) ? (short)(-player.Bounty) : itemID);
			else
				Write(itemID);

			Write(vehicle._id);
			Write(tickUpdate);

			if (!bBot)
				Write((player == null ? (ushort)0xFFFF : player._id));
			else
				Write(vehicle._id);

			switch (vehicle._type.Type)
			{
				case VehInfo.Types.Car:
					Write(vehicle._state.health);
					Write(vehicle._state.velocityX);
					Write(vehicle._state.velocityY);
					Write(vehicle._state.velocityZ);
					Write(vehicle._state.positionX);
					Write(vehicle._state.positionY);
					Write(vehicle._state.positionZ);
					Write(vehicle._state.yaw);
					Write((UInt16)vehicle._state.direction);
					Write((byte)vehicle._state.unk1);
					break;

				case VehInfo.Types.Dependent:
					Write(vehicle._state.velocityX);
					Write(vehicle._state.velocityY);
					Write(vehicle._state.velocityZ);
					Write(vehicle._state.positionX);
					Write(vehicle._state.positionY);
					Write(vehicle._state.positionZ);
					Write(vehicle._state.health);
					Write(vehicle._state.pitch);
					Write(vehicle._state.yaw);
					Write(vehicle._state.yaw);
					Write((byte)vehicle._state.unk1);
					break;

				case VehInfo.Types.Computer:
					Write(vehicle._state.health);
					Write((vehicle._team != null) ? vehicle._team._id : (short)-1);
					Write(vehicle._state.positionX);
					Write(vehicle._state.positionY);
					Write(vehicle._state.positionZ);
					Write((short)64);
					Write((short)1);
					Write((short)0);
					Write(vehicle._state.fireAngle);	//Base yaw?
					Write(vehicle._state.fireAngle);	//Turret yaw?
					Write((ushort)206);
					break;
			}

			//Write in all additional equipment
			if (activeEquip != null)
			{
				foreach (ushort equip in activeEquip)
					Write(equip);
			}

			if (activeUtilities != null)
			{
				foreach (ItemInfo.UtilityItem util in activeUtilities)
					Write((ushort)util.id);
			}
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
