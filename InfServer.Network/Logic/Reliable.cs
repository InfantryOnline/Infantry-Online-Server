using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Protocol;

namespace InfServer.Logic
{	// Logic_Reliable Class
	/// Deals with handling and keeping track of reliable packets
	///////////////////////////////////////////////////////
	class Logic_Reliable
	{	/// <summary>
		/// Handles all reliable packets received from clients
		/// </summary>
		static public void Handle_Reliable(Reliable pkt, Client client)
		{	//Is the reliable number what we expected?
			if (pkt.rNumber > client._C2S_Reliable)
			{	//Keep the packet around for later
				client._oosReliable[pkt.rNumber] = pkt.packet;
				Log.write(TLog.Warning, "Out of sync reliable. {0} vs {1}", pkt.rNumber, client._C2S_Reliable);
				return;
			}
			//A previously received reliable?
			else if (pkt.rNumber < client._C2S_Reliable)
			{	//Re-send the echo
				ReliableEcho resent = new ReliableEcho();
				resent.rNumber = (ushort)(client._C2S_Reliable - 1);
				client.send(resent);
				return;
			}

			//Expect the next!
			client._C2S_Reliable++;

			//Handle the message we're supposed to receive
			client._handler.handlePacket(pkt.packet, client);

			//If we have other packets in seqence waiting in store, use them too
			PacketBase unhandled;

			while (client._oosReliable.TryGetValue(client._C2S_Reliable, out unhandled))
			{	//Handle it and go to next packet
				client._handler.handlePacket(unhandled, client);
				client._oosReliable.Remove(client._C2S_Reliable++);
			}

			//Prepare an echo for all received packets
			ReliableEcho echo = new ReliableEcho();
			echo.rNumber = (ushort)(client._C2S_Reliable - 1);
			client.send(echo);
		}

		/// <summary>
		/// Handles all reliable confirmations received from clients
		/// </summary>
		static public void Handle_ReliableEcho(ReliableEcho pkt, Client client)
		{	//Confirm that it was received
			client.confirmReliable(pkt.rNumber);
		}

		/// <summary>
		/// Handles all out-of-sync notifications
		/// </summary>
		static public void Handle_OutOfSync(OutOfSync pkt, Client client)
		{
			Log.write(TLog.Error, "** OUTOFSYNC: " + pkt.rNumber);
		}

		/// <summary>
		/// Registers all handlers
		/// </summary>
		[Logic.RegistryFunc]
		static public void Register()
		{
			Reliable.Handlers += Handle_Reliable;
			ReliableEcho.Handlers += Handle_ReliableEcho;
			OutOfSync.Handlers += Handle_OutOfSync;
		}
	}
}
