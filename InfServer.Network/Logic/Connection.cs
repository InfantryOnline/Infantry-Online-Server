using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Protocol;

namespace InfServer.Logic
{	// Logic_Connection Class
	/// Deals with connection-related client protocol
	///////////////////////////////////////////////////////
	class Logic_Connection
	{	/// <summary>
		/// Handles the initial packet sent by the client
		/// </summary>
		static public void Handle_CS_Initial(CS_Initial pkt, Client client)
		{	//Has he already been initialized?
			if (client._bInitialized)
			{	//Make a note
				Log.write(TLog.Warning, "Client " + client + " attempted to initialize connection twice.");
				client.destroy();
				return;
			}

			client._C2S_UDPSize = pkt.udpMaxPacket;
			client._connectionID = pkt.connectionID;

			//Set up our CRC keys
			int CRCKey = new Random().Next();
			client._CRC_C2S.key = (uint)CRCKey;
			client._CRC_S2C.key = (uint)CRCKey;

			//Send our initial login packet
			SC_Initial sci = new SC_Initial();

			sci.connectionID = pkt.connectionID;
			sci.CRCSeed = CRCKey;
			client._CRCLength = sci.CRCLen = (byte)Client.crcLength;
			sci.serverUDPMax = Client.udpMaxSize;
			sci.unk1 = 2;

			client.send(sci);

			//Enable CRC!
			client._CRC_C2S.bActive = true;
			client._CRC_S2C.bActive = true;

			//He's now initialized
			client._bInitialized = true;
		}

		/// <summary>
		/// Handles the initial packet sent by the server
		/// </summary>
		static public void Handle_SC_Initial(SC_Initial pkt, Client client)
		{	//Has he already been initialized?
			if (client._bInitialized)
			{	//Make a note
				Log.write(TLog.Warning, "Server attempted to initialize connection twice.");
				client.destroy();
				return;
			}

			client._C2S_UDPSize = Client.udpMaxSize;
			client._S2C_UDPSize = pkt.serverUDPMax;
			client._connectionID = pkt.connectionID;

			//Set up our CRC state
			client._CRCLength = pkt.CRCLen;
			client._CRC_C2S.key = (uint)pkt.CRCSeed;
			client._CRC_S2C.key = (uint)pkt.CRCSeed;

			//Enable CRC!
			client._CRC_C2S.bActive = (pkt.CRCLen > 0);
			client._CRC_S2C.bActive = (pkt.CRCLen > 0);

			//We're now initialized
			client._bInitialized = true;
		}

		/// <summary>
		/// Handles the client's state packet
		/// </summary>
		static public void Handle_CS_State(CS_State pkt, Client client)
		{	//Update what we know of the client's connection state
			client._stats.clientCurrentUpdate = pkt.clientCurrentUpdate;
			client._stats.clientAverageUpdate = pkt.clientAverageUpdate;
			client._stats.clientShortestUpdate = pkt.clientShortestUpdate;
			client._stats.clientLongestUpdate = pkt.clientLongestUpdate;
			client._stats.clientLastUpdate = pkt.clientLastUpdate;

			client._stats.clientPacketsSent = pkt.packetsSent;
			client._stats.clientPacketsRecv = pkt.packetsReceived;
			client._stats.serverPacketsSent = client._packetsSent;
			client._stats.serverPacketsRecv = client._packetsReceived;

			//Prepare our reply
			SC_State sci = new SC_State();

			sci.tickCount = pkt.tickCount;
			sci.serverTickCount = Environment.TickCount;
			sci.clientSentCount = pkt.packetsSent;
			sci.clientRecvCount = pkt.packetsReceived;
			sci.serverRecvCount = client._stats.serverPacketsRecv;
			sci.serverSentCount = client._stats.serverPacketsSent;

			client.send(sci);

			//Set our sync difference
			short wander = (short)client._timeDiff;
			int timeDiff = (sci.serverTickCount - pkt.tickCount) & 0xFFFF;
			client._timeDiff = (short)timeDiff;

			//Calculate the clock wander
			if (wander != 0)
			{
				wander = (short)Math.Abs(client._timeDiff - wander);
				client._stats.clockWander[client._stats.wanderIdx++ % 10] = wander;
			}
			
			//Adjust the client's data rates accordingly
			client.adjustRates(pkt.clientAverageUpdate);
		}

		/// <summary>
		/// Handles the servers's state packet
		/// </summary>
		static public void Handle_SC_State(SC_State pkt, Client client)
		{
			client._stats.serverPacketsSent = pkt.serverSentCount;
			client._stats.serverPacketsRecv = pkt.serverRecvCount;
			client._timeDiff = (short)(Environment.TickCount - pkt.serverTickCount);
		}

		/// <summary>
		/// Handles the client's disconnection notice
		/// </summary>
		static public void Handle_Disconnect(Disconnect pkt, Client client)
		{	//Destroy the client in question
			client.destroy();
		}

		/// <summary>
		/// Registers all handlers
		/// </summary>
		[Logic.RegistryFunc]
		static public void Register()
		{
			CS_Initial.Handlers += Handle_CS_Initial;
			CS_State.Handlers += Handle_CS_State;
			SC_Initial.Handlers += Handle_SC_Initial;
			SC_State.Handlers += Handle_SC_State;
			Disconnect.Handlers += Handle_Disconnect;
		}
	}
}
