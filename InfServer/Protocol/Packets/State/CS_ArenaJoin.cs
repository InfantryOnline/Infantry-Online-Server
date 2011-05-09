using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{	/// <summary>
	/// CS_ArenaJoin is used to check the integrity of the client's security
	/// </summary>
	public class CS_ArenaJoin : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public bool Unk1;
		public UInt32 EXEChecksum;		//Checksum of certain critical functions in the code
		public UInt32 AssetChecksum;	//Checksum of all the assets loaded in the client
		public UInt16 Unk2;
		public UInt16 Unk3;
		public string ArenaName;

		//Packet routing
		public const ushort TypeID = (ushort)Helpers.PacketIDs.C2S.Security;
		static public event Action<CS_ArenaJoin, Player> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
		public CS_ArenaJoin(ushort typeID, byte[] buffer, int index, int count)
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
		{	//Get the tickcount
			Unk1 = _contentReader.ReadBoolean();
			EXEChecksum = _contentReader.ReadUInt32();
			AssetChecksum = _contentReader.ReadUInt32();
			Unk2 = _contentReader.ReadUInt16();
			Unk3 = _contentReader.ReadUInt16();
			ArenaName = ReadString(16);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Security Reply";
			}
		}
	}
}
