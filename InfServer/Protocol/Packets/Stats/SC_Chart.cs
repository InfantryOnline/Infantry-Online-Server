using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_Chart is used for displaying charts to a player
	/// </summary>
	public class SC_Chart : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public string title;
		public string columns;
		public List<string> rows;

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.DisplayChart;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_Chart()
			: base(TypeID)
		{
			rows = new List<string>();
		}

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{	//Just need the id
			Write((byte)TypeID);

			Write(title, 0);
			Write(columns, 0);

			foreach (string row in rows)
				Write(row, 0);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "A chart update";
			}
		}
	}
}
