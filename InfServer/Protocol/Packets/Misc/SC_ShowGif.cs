using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_ShowGif is used for displaying animated gifs on the client
	/// </summary>
	public class SC_ShowGif : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public short displayTime;
		public string website;

		public byte[] gifData;

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.ShowGIF;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_ShowGif()
			: base(TypeID)
		{ }

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{	//Just need the id
			Write((byte)TypeID);

			Write(displayTime);
			Write(website, 0);

			Write(gifData);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "A show gif command";
			}
		}
	}
}
