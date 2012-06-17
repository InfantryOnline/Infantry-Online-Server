using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{	/// <summary>
    /// CS_NoSpectator is used when a player requests to decline spectators
	/// </summary>
	public class CS_AllowSpectator : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public bool allow;			//The player to spectator
		public Int32 playerID;          //Possibly the players ID? It's only non-0 when the player enters the arena

		//Packet routing
		public const ushort TypeID = (ushort)Helpers.PacketIDs.C2S.AllowSpectator;
		static public Action<CS_AllowSpectator, Player> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
        public CS_AllowSpectator(ushort typeID, byte[] buffer, int index, int count)
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
		{
            allow = _contentReader.ReadBoolean();
			playerID = _contentReader.ReadInt32();
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Player allow spectate request";
			}
		}
	}
}
