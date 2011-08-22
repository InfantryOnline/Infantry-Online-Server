using System;
using System.Collections.Generic;
using System.Text;

using InfServer.Protocol;

namespace InfServer.Network
{
	/// <summary>
	/// PacketDummy is used to house undefined packets.
	/// </summary>
	public class PacketDummy : PacketBase
	{
		static public event Action<PacketDummy, Client> Handlers;

		/// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
		public PacketDummy(ushort typeID, byte[] buffer, int index, int count)
			: base(typeID, buffer, index, count)
		{
		}

		/// <summary>
		/// Routes a new packet to various relevant handlers
		/// </summary>
		public override void Route()
		{	//Call all handlers!
			if (Handlers != null)
				Handlers(this, (Client)_client);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{	
				return "Undefined packet type #" + _type;
			}
		}
	}
}
