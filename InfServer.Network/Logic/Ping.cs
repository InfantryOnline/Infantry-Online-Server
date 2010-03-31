using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Protocol;

namespace InfServer.Logic
{	// Logic_Ping Class
	/// Deals with client pings
	///////////////////////////////////////////////////////
	class Logic_Ping
	{	/// <summary>
		/// Handles all ping packets received from clients
		/// </summary>
		static public void Handle_Ping(PingPacket pkt, Client client)
		{	
		}

		/// <summary>
		/// Registers all handlers
		/// </summary>
		[Logic.RegistryFunc]
		static public void Register()
		{
			//PingPacket.Handlers += Handle_Ping;
		}
	}
}
