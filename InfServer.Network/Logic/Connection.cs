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
			client._CRC_C2S.bActive = true;
			client._CRC_S2C.bActive = true;

			//We're now initialized
			client._bInitialized = true;
		}

		/// <summary>
		/// Handles the client's state packet
		/// </summary>
		static public void Handle_CS_State(CS_State pkt, Client client)
		{	//Update what we know of the client's connection state
			client._stats.C2S_packetsSent = pkt.packetsSent;
			client._stats.C2S_packetsRecv = pkt.packetsReceived;

			//Prepare our reply
			SC_State sci = new SC_State();

			sci.tickCount = pkt.tickCount;
			sci.serverTickCount = Environment.TickCount;
			sci.clientSentCount = pkt.packetsSent;
			sci.clientRecvCount = pkt.packetsReceived;
			sci.serverRecvCount = client._stats.S2C_packetsRecv;
			sci.serverSentCount = client._stats.S2C_packetsSent;

			client.send(sci);

			//Set our sync difference
			ushort wander = (ushort)client._timeDiff;
			int timeDiff = (sci.serverTickCount - pkt.tickCount) & 0xFFFF;
			if (timeDiff > 0x7FFF)
				timeDiff = 0xFFFF - timeDiff;
			client._timeDiff = (ushort)timeDiff;

			//Calculate the clock wander
			if (wander != 0)
			{
				wander = (ushort)Math.Abs(client._timeDiff - wander);
				client._stats.clockWander[client._stats.idx++ % 10] = wander;
			}
		}

		/// <summary>
		/// Handles the servers's state packet
		/// </summary>
		static public void Handle_SC_State(SC_State pkt, Client client)
		{
			client._stats.S2C_packetsSent = pkt.serverSentCount;
			client._stats.S2C_packetsRecv = pkt.serverRecvCount;
			client._timeDiff = Environment.TickCount - pkt.serverTickCount;
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
