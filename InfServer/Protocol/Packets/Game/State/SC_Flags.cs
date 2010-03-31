using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

using Assets;


namespace InfServer.Protocol
{	/// <summary>
	/// SC_Flags contains updates regarding flags in the arena 
	/// </summary>
	public class SC_Flags : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public bool bDisable;
		public Arena.FlagState singleUpdate;
		public IEnumerable<Arena.FlagState> flags;

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.Flags;

		public class FlagInfo
		{	//Object credentials
			public UInt16 id;
			public UInt16 carrierID;
			public UInt16 posX;
			public UInt16 posY;
			public Int16 teamID;
		}


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_Flags()
			: base(TypeID)
		{
		}

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{	//Disable all?
			if (bDisable)
			{
				Write((byte)TypeID);
				Write((short)-1);
				Write((short)-1);
				Write((short)-1);
				Write((short)-1);
				Write((short)-1);
				return;
			}

			//A single update?
			if (singleUpdate != null)
			{
				Write((byte)TypeID);

				if (singleUpdate.bActive)
				{
					Write((ushort)singleUpdate.flag.GeneralData.Id);
					if (singleUpdate.carrier == null)
					{
						Write((short)-1);
						Write(singleUpdate.posX);
						Write(singleUpdate.posY);
					}
					else
					{
						Write(singleUpdate.carrier._id);
						Write((short)-1);
						Write((short)-1);
					}
					Write((short)((singleUpdate.team == null) ? (short) -1 : singleUpdate.team._id));
				}
				return;
			}
			

			//Write out each asset
			bool bEmpty = true;

			foreach (Arena.FlagState flag in flags)
			{	//Only if it's active..
				if (!flag.bActive)
					continue;

				//Not sure why it does this for each entry
				Write((byte)TypeID);

				Write((ushort)flag.flag.GeneralData.Id);
				if (flag.carrier == null)
				{
					Write((short)-1);
					Write(flag.posX);
					Write(flag.posY);
				}
				else
				{
					Write(flag.carrier._id);
					Write((short)-1);
					Write((short)-1);
				}
				Write((short)((flag.team == null) ? (short) -1 : flag.team._id));

				bEmpty = false;
			}

			if (bEmpty)
				Write((byte)TypeID);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Flag info.";
			}
		}
	}
}
