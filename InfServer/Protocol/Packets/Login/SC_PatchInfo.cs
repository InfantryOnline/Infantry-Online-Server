using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_PatchInfo contains info used to patch using a HTTP webserver
	/// </summary>
	public class SC_PatchInfo : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public bool Unk1;
		public string patchServer;		//Hostname of the patch server
		public ushort patchPort;		//Port for the patch server

		public string patchXml;			//Location of the patch XML file on the server

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.PatchInfo;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_PatchInfo()
			: base(TypeID)
		{ }

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{	//Type ID
			Write((byte)TypeID);

			//Contents
			Write(Unk1);
			Write(patchServer, 64);
			Write(patchPort);
			Write(patchXml, 128);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Patch server info: " + patchServer;
			}
		}
	}
}
