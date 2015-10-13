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
		{	//Get the relevant stream
			Client.StreamState stream = client._streams[pkt.streamID];
			
			//Is the reliable number what we expected?
			if (pkt.rNumber > stream.C2S_Reliable)
			{	//Report it!
				Log.write(TLog.Inane, "OOS Reliable Packet Stream[{0}]. {1} vs {2}", pkt.streamID, pkt.rNumber, stream.C2S_Reliable);
				client.reportOutOfSync(pkt, pkt.rNumber, pkt.streamID);
				return;
			}
			//A previously received reliable?
			else if (pkt.rNumber < stream.C2S_Reliable)
			{	//Re-send the echo
				ReliableEcho resent = new ReliableEcho(pkt.streamID);
				resent.rNumber = (ushort)(stream.C2S_Reliable - 1);
				client.send(resent);
				return;
			}

			//Expect the next!
			stream.C2S_Reliable++;

			//Handle the message we're supposed to receive
			client._handler.handlePacket(pkt.packet, client);

			//If we have other packets in seqence waiting in store, use them too
			PacketBase unhandled;
			ushort reliableNext = stream.C2S_Reliable;

			while (stream.oosReliable.TryGetValue(reliableNext, out unhandled))
			{	//Handle it and go to next packet
				client._handler.handlePacket(unhandled, client);
				stream.oosReliable.Remove(reliableNext++);
			}

			//Prepare an echo for all received packets
			ReliableEcho echo = new ReliableEcho(pkt.streamID);
			echo.rNumber = (ushort)(stream.C2S_Reliable - 1);
			client.send(echo);
		}

		/// <summary>
		/// Handles all reliable confirmations received from clients
		/// </summary>
		static public void Handle_ReliableEcho(ReliableEcho pkt, Client client)
		{	//Confirm that it was received
			client.confirmReliable(pkt.rNumber, pkt.streamID);
		}

		/// <summary>
		/// Handles all out-of-sync notifications
		/// </summary>
		static public void Handle_OutOfSync(OutOfSync pkt, Client client)
		{
			//TODO: Resend packet?
			//Log.write(TLog.Error, "** OUTOFSYNC: " + pkt.rNumber);
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
