using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_ChangeTeam notifies a client of a forced team change
	/// </summary>
	public class SC_ChangeTeam : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public UInt16 playerID;
		public Int16 teamID;
		public string teamname;

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.ChangeTeam;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_ChangeTeam()
			: base(TypeID)
		{ }

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{	//Type ID
			Write((byte)TypeID);

			//Contents
			Write(playerID);
			Write(teamID);
			Write(teamname, 32);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Team change to '" + teamname + "'";
			}
		}
	}
}
