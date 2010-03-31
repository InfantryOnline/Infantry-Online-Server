using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Protocol;

namespace InfServer.Logic
{	// Logic_Box Class
	/// Deals with the handling of boxed packets
	///////////////////////////////////////////////////////
	class Logic_Box
	{	/// <summary>
		/// Handles all boxed packets received from clients
		/// </summary>
		static public void Handle_Boxed(BoxPacket pkt, Client client)
		{	//Handle each packet seperately
			foreach (PacketBase packet in pkt.packets)
				client._handler.handlePacket(packet, client);
		}

		/// <summary>
		/// Handles all reliable boxed packets received from clients
		/// </summary>
		static public void Handle_ReliableBoxed(ReliableBox pkt, Client client)
		{	//Handle each packet seperately
			foreach (PacketBase packet in pkt.packets)
				client._handler.handlePacket(packet, client);
		}

		/// <summary>
		/// Registers all handlers
		/// </summary>
		[Logic.RegistryFunc]
		static public void Register()
		{
			BoxPacket.Handlers += Handle_Boxed;
			ReliableBox.Handlers += Handle_ReliableBoxed;
		}
	}
}
