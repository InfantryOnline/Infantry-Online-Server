using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_Environment requests a snapshot of the player's environment
	/// </summary>
	public class CS_SecurityCheck : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
        public UInt16 Unk1;
		public UInt32 EXEChecksum;		//Checksum of certain critical functions in the code
		public UInt32 AssetChecksum;	//Checksum of all the assets loaded in the client
		public UInt32 Unk2;
		public UInt32 Unk3;
        public uint tickCount;

		//Packet routing
		public const ushort TypeID = (ushort)Helpers.PacketIDs.C2S.SecurityResponse;
        static public event Action<CS_SecurityCheck, Player> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
		public CS_SecurityCheck(ushort typeID, byte[] buffer, int index, int count)
			: base(typeID, buffer, index, count)
		{
		}

        public CS_SecurityCheck()
            : base(TypeID)
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
        {            
            EXEChecksum = _contentReader.ReadUInt32(); //??
            AssetChecksum = _contentReader.ReadUInt32(); //??
            Unk2 = _contentReader.ReadUInt32(); //?? Doesn't change on asset change
            Unk3 = _contentReader.ReadUInt32(); //Changes on asset change [memory]
        }

        public override void Serialize()
        {
        }

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "security request";
			}
		}
	}
}
