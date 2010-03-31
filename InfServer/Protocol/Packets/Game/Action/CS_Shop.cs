using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{	/// <summary>
	/// CS_Shop enables players to utilize the shop
	/// </summary>
	public class CS_Shop : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public Int16 itemID;			//The item being bought/sold
		public Int16 unk1;
		public Int32 quantity;			//The amount to buy or sell

		//Packet routing
		public const ushort TypeID = (ushort)Helpers.PacketIDs.C2S.Shop;
		static public Action<CS_Shop, Player> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
		public CS_Shop(ushort typeID, byte[] buffer, int index, int count)
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
			itemID = _contentReader.ReadInt16();
			unk1 = _contentReader.ReadInt16();
			quantity = _contentReader.ReadInt32();
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Player shop request";
			}
		}
	}
}
