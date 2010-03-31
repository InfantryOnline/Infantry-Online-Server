using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace InfServer.Network
{
	// IPacketHandler Interface
	/// Provides the interface necessary to act as a packet handler class
	///////////////////////////////////////////////////////
	public interface IPacketHandler
	{
		/// <summary>
		/// Sends a packet
		/// </summary>
		void sendPacket(PacketBase packet, byte[] data, EndPoint ep);

		/// <summary>
		/// Handles a packet
		/// </summary>
		void handlePacket(PacketBase packet, NetworkClient client);

		/// <summary>
		/// Gets our packet factory interface
		/// </summary>
		IPacketFactory getFactory();
	}
}
