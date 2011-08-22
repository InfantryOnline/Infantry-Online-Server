using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using InfServer.Network;
using InfServer.Protocol;
using InfServer.Game;

namespace InfServer.Logic
{	// Logic_Unknown Class
	/// Deals with unknown packets
	///////////////////////////////////////////////////////
	class Logic_Unknown
	{
		/// <summary>
		/// Triggered when an unknown packet is received
		/// </summary>
		static public void Handle_PacketDummy(PacketDummy pkt, Client client)
		{	//Are we logging unknown packets?
			if (!Client.bLogUnknowns)
				return;

			//Write a dump of it to disk
			Log.write("Packet #{0}", pkt._type);
			Log.write(pkt.DataDump);
		}

		/// <summary>
		/// Registers all handlers
		/// </summary>
		[Logic.RegistryFunc]
		static public void Register()
		{
			PacketDummy.Handlers += Handle_PacketDummy;
		}
	}
}
