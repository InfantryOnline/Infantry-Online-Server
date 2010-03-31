using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{	/// <summary>
	/// CS_Explosion is used by the client to indicate where an explosion takes place
	/// </summary>
	public class CS_Explosion : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public Int16 positionX;
		public Int16 positionY;
		public Int16 positionZ;
		public UInt16 explosionID;		//ID of the item which exploded


		//Packet routing
		public const ushort TypeID = (ushort)Helpers.PacketIDs.C2S.Explosion;
		static public Action<CS_Explosion, Player> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
		public CS_Explosion(ushort typeID, byte[] buffer, int index, int count)
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
			positionX = _contentReader.ReadInt16();
			positionY = _contentReader.ReadInt16();
			positionZ = _contentReader.ReadInt16();
			explosionID = _contentReader.ReadUInt16();
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Player explosion notification";
			}
		}
	}
}
