using System;
using System.Collections.Generic;
using System.Text;

namespace InfServer.Network
{
	/// <summary>
	/// PacketDummy is used to house undefined packets.
	/// </summary>
	public class PacketDummy : PacketBase
	{
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
