using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Protocol;

namespace InfServer.Logic
{	// Logic_DataStream Class
	/// Deals with receiving and putting together datastream packets
	///////////////////////////////////////////////////////
	class Logic_DataStream
	{	/// <summary>
		/// Handles all reliable packets received from clients
		/// </summary>
		static public void Handle_DataPacketRcv(DataPacketRcv pkt, Client client)
		{	//Get the relevant stream
			Client.StreamState stream = client._streams[pkt.streamID];

			//Is the reliable number what we expected?
			if (pkt.rNumber > stream.C2S_Reliable)
			{	//Report it!
				Log.write(TLog.Inane, "OOS Data Packet Stream[{0}]. {1} vs {2}", pkt.streamID, pkt.rNumber, stream.C2S_Reliable);
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

			//Is there a pre-existing stream?
			if (stream.dataStreamBuffer == null)
			{	//Let's be sensible
                //This commented part causes issues for some players on certain aliases, removing for now
			/*
               if (pkt.dataSize < client._C2S_UDPSize - client._CRCLength)
				{
					Log.write(TLog.Error, "Received data stream packet with invalid size {0}", pkt.dataSize);
					return;
				}
             */
				if (pkt.dataSize > 20 * 1024 * 1024)
				{
					Log.write(TLog.Error, "Received data stream packet with invalid size {0}", pkt.dataSize);
					return;
				}             

				//Create one of the appropriate size
				stream.dataStreamBuffer = new byte[pkt.dataSize];
				Array.Copy(pkt.data, 4, stream.dataStreamBuffer, 0, pkt.data.Length - 4);

				stream.dataStreamIndex = pkt.data.Length - 4;
			}
			else
			{	//Copy in the data
				Array.Copy(pkt.data, 0, stream.dataStreamBuffer, stream.dataStreamIndex, pkt.data.Length);
				stream.dataStreamIndex += pkt.data.Length;

				//Do we have enough?
				if (stream.dataStreamIndex >= stream.dataStreamBuffer.Length)
				{	//Create the packet!
					ushort typeID = NetworkClient.getTypeID(stream.dataStreamBuffer, 0);
					PacketBase packet = null;

					try
					{
						packet = client._handler.getFactory().createPacket(	client, typeID, stream.dataStreamBuffer, 0,
																			stream.dataStreamBuffer.Length);

						packet._client = client;
						packet._handler = client._handler;
						packet.Deserialize();
					}
					catch (Exception ex)
					{	//There was an error while deserializing the packet, create a dummy packet
						packet = null;

						Log.write(TLog.Exception, "Exception while deserializing datastream packet:\r\n{0}", ex);
					}

					if (packet != null)
						client._handler.handlePacket(packet, client);

					//Destroy the original stream
					stream.dataStreamBuffer = null;
					stream.dataStreamIndex = 0;
				}
			}

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
			echo.streamID = pkt.streamID;
			echo.rNumber = (ushort)(stream.C2S_Reliable - 1);
			client.send(echo);
		}

		/// <summary>
		/// Registers all handlers
		/// </summary>
		[Logic.RegistryFunc]
		static public void Register()
		{
			DataPacketRcv.Handlers += Handle_DataPacketRcv;
		}
	}
}
