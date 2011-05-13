using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_ItemUsed indicates that an item has been used at the specified location
	/// </summary>
	public class SC_ItemUsed : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public byte unk1;
		public UInt32 targetVehicle;
		public Int16 unk2;
		public Int16 unk3;
		public Int16 unk4;
		public Int16 unk5;
		public Int16 itemID;
		public Int16 posX;
		public Int16 posY;
		public byte yaw;
		public byte unk6;

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.ItemUsed;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_ItemUsed()
			: base(TypeID)
		{ }

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{
			Write((byte)TypeID);

			Write(unk1);
			Write(targetVehicle);
			Write(unk2);
			Write(unk3);
			Write(unk4);
			Write(unk5);
			Write(itemID);
			Write(posX);
			Write(posY);
			Write(yaw);
			Write(unk6);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Item used notification";
			}
		}
	}
}
