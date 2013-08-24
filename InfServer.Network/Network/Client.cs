using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;


namespace InfServer.Protocol
{
	// Client Class
	/// Represents a single client connected to the server
	///////////////////////////////////////////////////////
	public class Client : NetworkClient
	{	// Member variables
		///////////////////////////////////////////////////
		public object _sync;					//Object used for syncing purposes
		private bool _bClientConn;				//Are we part of a client connection?

		public short _timeDiff;					//The current difference in the tickcount between client and server

		#region Connection
		public ConnectionStats _stats;			//Our connection statistics

		public bool _bInitialized;				//Has the connection been initialized?
		public bool _bLoggedIn;					//Has the client successfully logged in?

		public int _connectionID;				//Connection ID assigned by the client at the beginning

		public int _C2S_UDPSize;				//Maximum size of a udp packet C2S
		public int _S2C_UDPSize;				//Maximum size of a udp packet S2C

		public byte _CRCLength;					//Number of bytes to include in the packet
		public CRC32 _CRC_C2S;					//Client to server CRC state
		public CRC32 _CRC_S2C;					//Server to client CRC state

		public int _tickLastDecay;
		public int _bytesWritten;				//Bytes currently written
		public int _rateThreshold;				//The threshold for this connection
		public int _decayRate;					//The rate at which the bytes written decays

		public StreamState[] _streams;			//The states for each data stream

		public Queue<PacketBase> _packetQueue;	//The list of packets waiting to be sent
		#endregion

		#region Statistics
		public int _tickLastBytesSample;		//The time of the last sample

		public int _bytesSent;					//The amount of bytes sent since the last time
		public int _bytesReceived;				//The amount of bytes received since the last time

		public ulong _packetsSent;				//The total packets sent to the client
		public ulong _packetsReceived;			//The total packets received from the client
		#endregion

		//Static settings
		const int RATETHRESHOLD_BASE = 1048576;
		const int DECAYRATE_BASE = 157284;//78642;

		static public int udpMaxSize;
		static public int crcLength;

		static public int connectionTimeout;

		static public bool bLogUnknowns;


		///////////////////////////////////////////////////
		// Member Classes
		///////////////////////////////////////////////////
		#region Member Classes
		/// <summary>
		/// Contains the state for a particular stream
		/// </summary>
		public class StreamState
		{
			public int streamID;

			public ushort C2S_Reliable;										//The expected number for the next reliable message
			public ushort S2C_Reliable;										//
			public ushort S2C_ReliableConfirmed;							//The last reliable packet awaiting confirmation

			public SortedDictionary<ushort, ReliableInfo> reliablePackets;	//The reliable packets we're looking after
			public Queue<ReliableInfo> reliableQueue;						//The reliable packets waiting to be sent

			public SortedDictionary<ushort, PacketBase> oosReliable;		//Reliable packets sent out of sync by the client,
																			//used for synchronization later
			public ushort lastOOSPacket;									//The last reliable id which was received out of sync
			public int tickOOSPacket;										//The tick at which the last packet was received out of sync

			public byte[] dataStreamBuffer;									//The buffer to keep our data stream, before it is put together
			public int dataStreamIndex;										//How far we're into the data stream

			public StreamState(int sID)
			{
				streamID = sID;
				C2S_Reliable = 0;
				S2C_Reliable = 0;

				reliablePackets = new SortedDictionary<ushort, ReliableInfo>();
				oosReliable = new SortedDictionary<ushort, PacketBase>();

				reliableQueue = new Queue<ReliableInfo>();
			}
		}

		/// <summary>
		/// Contains connection statistics for this client
		/// </summary>
		public class ConnectionStats
		{
			public int clientCurrentUpdate;	//Update (ping) timings from the last state sync
			public int clientLastUpdate;	//
			public int clientAverageUpdate;	//
			public int clientShortestUpdate;//
			public int clientLongestUpdate;	//

			public ulong clientPacketsSent;	//Packet count statistics
			public ulong clientPacketsRecv;	//
			public ulong serverPacketsSent;	//
			public ulong serverPacketsRecv;	//

			public short[] clockWander;	//Clock wander samples
			public int wanderIdx;			//

			public short AverageClockWander
			{
				get
				{	//Perform an average!
					short total = 0;

					for (int i = 0; i < clockWander.Length; ++i)
						total += clockWander[i];

					return (short)(total / clockWander.Length);
				}
			}

			public int[] sendSpeeds;		//Send speed samples
			public int sendIdx;				//

			public int SendSpeed
			{
				get
				{	//Perform an average!
					int total = 0;
					
					for (int i = 0; i < sendSpeeds.Length; ++i)
						total += sendSpeeds[i];

					return total / sendSpeeds.Length;
				}
				set
				{
					sendSpeeds[sendIdx++ % sendSpeeds.Length] = value;
				}
			}

			public int[] receiveSpeeds;		//Send speed samples
			public int receiveIdx;			//

			public int ReceiveSpeed
			{
				get
				{	//Perform an average!
					int total = 0;

					for (int i = 0; i < receiveSpeeds.Length; ++i)
						total += receiveSpeeds[i];

					return total / receiveSpeeds.Length;
				}
				set
				{
					receiveSpeeds[receiveIdx++ % receiveSpeeds.Length] = value;
				}
			}

			public float C2SPacketLoss
			{
				get
				{
					double loss = clientPacketsRecv;
					loss /= serverPacketsSent;

					if (loss > 1)		//Don't allow negative packetloss
						return 0.0f;
					return 100.0f - (float)(loss * 100);
				}
			}

			public float S2CPacketLoss
			{
				get
				{
					double loss = serverPacketsRecv;
					loss /= clientPacketsSent;

					if (loss > 1)		//Don't allow negative packetloss
						return 0.0f;
					return 100.0f - (float)(loss * 100);
				}
			}
		}

		/// <summary>
		/// Contains information regarding a reliable packet
		/// </summary>
		public class ReliableInfo
		{
			public int rid;					//The reliable id
			public PacketBase packet;		//The packet sent

			public DataStream dataStream;	//Used to trigger sending of this entire datastream
			public DataStream streamParent;	//The stream this packet was a part of, if any

			public int timeSent;			//The time at which it was sent
			public int attempts;			//The number of attempts we've made to redeliver

			public event Action Completed;		//Event called on packet completion

			public void onCompleted()
			{
				if (Completed != null)
					Completed();
			}

			/// <summary>
			/// Merges all events from a list of reliable infos into one
			/// </summary>
			public void consolidateEvents(IEnumerable<ReliableInfo> reliables)
			{
				foreach (ReliableInfo info in reliables)
					Completed += info.Completed;
			}

			/// <summary>
			/// Merges all events from a reliable infos into the class
			/// </summary>
			public void consolidateEvents(ReliableInfo reliable)
			{
				Completed += reliable.Completed;
			}
		}

		/// <summary>
		/// Contains information regarding a large data transmission
		/// </summary>
		public class DataStream
		{
			public int amountSent;				//The number of bytes already sent
			public byte[] buffer;				//The data waiting to be sent

			public PacketBase lastPacket;		//The last packet we sent

			public event Action Completed;		//Event called on stream completion

			public void onCompleted()
			{
				if (Completed != null)
					Completed();
			}
		}
		#endregion

		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Initializes the new client class
		/// </summary>
		public Client(bool bClientConn)
		{
			_sync = new object();
			_bClientConn = bClientConn;

			_stats = new ConnectionStats();
			_stats.clockWander = new short[10];
			_stats.sendSpeeds = new int[20];
			_stats.receiveSpeeds = new int[20];

			_CRC_C2S = new CRC32();
			_CRC_S2C = new CRC32();

			_streams = new StreamState[4];
			for (int i = 0; i < 4; ++i)
				_streams[i] = new StreamState(i);

			_packetQueue = new Queue<PacketBase>();

			_rateThreshold = RATETHRESHOLD_BASE / 250;
			_decayRate = DECAYRATE_BASE / 250;
		}

		#region State
		/// <summary>
		/// Allows the client to take care of anything it needs to
		/// </summary>
		public override void poll()
		{	//Sync up!
			using (DdMonitor.Lock(_sync))
			{	//Only time out server-side clients
				int now = Environment.TickCount;

				if (connectionTimeout != -1 && !_bClientConn && now - base._lastPacketRecv > connectionTimeout)
				{	//Farewell~
					Log.write(TLog.Warning, "Client timeout: {0}", this);
					destroy();
					return;
				}

				//Should we decay the bytes written?
				if (now - _tickLastDecay > 20)
				{
					_bytesWritten -= _decayRate;
					if (_bytesWritten < 0)
						_bytesWritten = 0;
					_tickLastDecay = now;
				}

				//Update connection stats?
				if (now - _tickLastBytesSample > 1000)
				{
					_stats.SendSpeed = _bytesSent;
					_stats.ReceiveSpeed = _bytesReceived;

					_bytesSent = 0;
					_bytesReceived = 0;

					_tickLastBytesSample = now;
				}

				//Handle each stream
				for (int i = 0; i < 4; ++i)
				{
					Client.StreamState stream = _streams[i];

					//Queue reliable packets as necessary
					enqueueReliables(stream);

					//Make sure they all reach their destination
					ensureReliable(stream);
				}

				//Box packets as necessary and send them out
				sendQueuedPackets();
			}
		}

		/// <summary>
		/// Ceases interaction with this client, removes it from the server
		/// </summary>
		public override void destroy()
		{	//Sync up!
			using (DdMonitor.Lock(_sync))
			{	//Send out a disconnect packet
				Disconnect discon = new Disconnect();

				discon.connectionID = _connectionID;
				discon.reason = Disconnect.DisconnectReason.DisconnectReasonApplication;

				_packetQueue.Enqueue(discon);

				base.destroy();
			}
		}
		#endregion

		#region Connection
		/// <summary>
		/// Adjusts the data rates for the client accordingly
		/// </summary>
		public void adjustRates(int averageDelta)
		{	//If it hasn't initialized yet, stick with default
			if (averageDelta != 0)
			{	//Enforce a highest speed
				if (averageDelta < 20)
					averageDelta = 20;

				_rateThreshold = RATETHRESHOLD_BASE / averageDelta;
				_decayRate = DECAYRATE_BASE / averageDelta;
			}
		}

		/// <summary>
		/// Enqueues necessary packets for a data stream
		/// </summary>
		private bool enqueueDataStream(DataStream ds, StreamState stream)
		{	//Write packets into the stream while we still have bandwidth available
			int bytesLeft = _rateThreshold - _bytesWritten;

			while (bytesLeft > 0 && ds.amountSent < ds.buffer.Length)
			{	//Prepare a data packet
				DataPacket dp = new DataPacket(stream.streamID);

				dp._bFirstPacket = (ds.amountSent == 0);
				dp.data = ds.buffer;
				dp.offset = ds.amountSent;

				dp.rNumber = stream.S2C_Reliable++;

				dp.MakeSerialized(this, _handler);

				ds.lastPacket = dp;		//For completion notification

				ds.amountSent += dp._dataRead;
				bytesLeft -= dp._dataRead;

				//Add it to our list of reliable packets
				ReliableInfo ri = new ReliableInfo();

				ri.streamParent = ds;
				ri.packet = dp;
				ri.rid = dp.rNumber;
				ri.timeSent = 0;

				stream.reliablePackets[dp.rNumber] = ri;
			}

			//Have we completed?
			return !(ds.amountSent >= ds.buffer.Length);
		}

		/// <summary>
		/// Adds any reliable packets waiting to be sent to the packet queue
		/// </summary>
		private void enqueueReliables(StreamState stream)
		{	//Check for data streams
			if (stream.reliableQueue.Count == 0)
				return;

			ReliableInfo ri = stream.reliableQueue.Peek();

			while (ri.dataStream != null)
			{	//If it's still streaming, then don't send any more reliables yet
				if (enqueueDataStream(ri.dataStream, stream))
					return;
				else
					stream.reliableQueue.Dequeue();

				//Check the next
				if (stream.reliableQueue.Count == 0)
					return;

				ri = stream.reliableQueue.Peek();
			}

			//Take care of reliable packet waiting to be streamed
			ICollection<ReliableInfo> reliableBoxed = boxReliablePackets(stream.reliableQueue, stream, stream.streamID);

			//Insert them all into our reliable table
			foreach (ReliableInfo info in reliableBoxed)
			{	//Make sure the rid is correct
				if (info.rid == -1)
				{
					Log.write(TLog.Error, "Reliable info was queued for sending with an unassigned reliable number.");
					continue;
				}

				stream.reliablePackets[(ushort)info.rid] = info;
			}
		}

		/// <summary>
		/// Ensures that the client is receiving all reliable packets sent
		/// </summary>
		/// <remarks>Also is the only function that sends reliable packets.</remarks>
		private void ensureReliable(Client.StreamState stream)
		{	//Compare times
			int currentTick = Environment.TickCount;

			//Do we need to send an out of sync notification?
			if (stream.lastOOSPacket > stream.C2S_Reliable &&
				currentTick - stream.tickOOSPacket > 100)
			{
				OutOfSync oos = new OutOfSync(stream.streamID);

				oos.streamID = stream.streamID;
				oos.rNumber = stream.lastOOSPacket;
				send(oos);

				stream.tickOOSPacket = currentTick + 200;
			}

			//Do we have any bandwidth available?
			int bytesLeft = _rateThreshold - _bytesWritten;
			if (bytesLeft < 0)
				return;

			//We want to start with the first sent packet
			for (ushort n = stream.S2C_ReliableConfirmed; n < stream.S2C_Reliable; ++n)
			{	//Does it exist?
				ReliableInfo ri;

				if (!stream.reliablePackets.TryGetValue(n, out ri))
					continue;

				//Has it been delayed too long?
				if (currentTick - ri.timeSent < 1000)
					continue;

				//Resend it
				_packetQueue.Enqueue(ri.packet);
	
				//Was it a reattempt?
				if (ri.timeSent != 0)
				{
					ri.attempts++;
                    
					//Log.write(TLog.Warning, "Reliable packet #" + ri.rid + " lost. (" + ri.attempts + ")");
				}

				ri.timeSent = Environment.TickCount;

				//Don't go over the bandwidth limit or we'll just complicate things
				bytesLeft -= ri.packet._size;
				if (bytesLeft < 0)
					break;
			}
		}

		/// <summary>
		/// Confirms that a reliable packet has been received by the client.
		/// Note that a higher rID than the lowest expected indicates that all 
		/// previous reliable packets were received.
		/// </summary>
		public void confirmReliable(ushort rID, int streamID)
		{	//Great!
			using (DdMonitor.Lock(_sync))
			{	//Get the relevant stream
				Client.StreamState stream = _streams[streamID];

				//This satisfies all packets inbetween
				for (ushort i = stream.S2C_ReliableConfirmed; i <= rID; ++i)
				{	//Get our associated info
					ReliableInfo ri;

					if (!stream.reliablePackets.TryGetValue(i, out ri))
						continue;

					stream.reliablePackets.Remove(i);

					//Part of a data stream?
					if (ri.streamParent != null)
					{
						if (ri.streamParent.lastPacket == ri.packet)
							ri.streamParent.onCompleted();
					}
					else
						ri.onCompleted();
				}

				stream.S2C_ReliableConfirmed = (ushort)(rID + 1);
			}
		}

		/// <summary>
		/// Takes a note that the packet was received out of sync
		/// </summary>
		public void reportOutOfSync(PacketBase packet, ushort rID, int streamID)
		{	//Get the relevant stream
			Client.StreamState stream = _streams[streamID];

			//Keep the packet around for later
			stream.oosReliable[rID] = packet;

			//Is the last OOS packet still valid?
			if (stream.lastOOSPacket > stream.C2S_Reliable)
				//Ignore the request until this OOS is honored
				return;

			//Note it down as out of sync
			stream.lastOOSPacket = rID;
			stream.tickOOSPacket = Environment.TickCount;
		}

		/// <summary>
		/// Sends a reliable packet to the client
		/// </summary>
		public void sendReliable(PacketBase packet)
		{	//Relay
			sendReliable(packet, null, 0);
		}

		/// <summary>
		/// Sends a reliable packet to the client
		/// </summary>
		public void sendReliable(PacketBase packet, int streamID)
		{
			sendReliable(packet, null, streamID);
		}

		/// <summary>
		/// Sends a reliable packet to the client
		/// </summary>
		public void sendReliable(PacketBase packet, Action completionCallback)
		{
			sendReliable(packet, completionCallback, 0);
		}

		/// <summary>
		/// Sends a reliable packet to the client
		/// </summary>
		public void sendReliable(PacketBase packet, Action completionCallback, int streamID)
		{	//Sync up!
			using (DdMonitor.Lock(_sync))
			{	//Get the relevant stream
				Client.StreamState stream = _streams[streamID];
				
				//Make sure the packet is serialized
				packet.MakeSerialized(this, _handler);
				
				//Is the (packet and reliable header) too large to be sent as one?
				if (4 + packet._size + _CRCLength > _C2S_UDPSize)
				{	//Add the stream packet to the reliable queue so we know
					//when to start streaming it.
					DataStream ds = new DataStream();
					ReliableInfo ri = new ReliableInfo();
					
					ds.amountSent = 0;
					ds.buffer = packet.Data;
					if (completionCallback != null)
						ds.Completed += completionCallback;

					ri.dataStream = ds;

					//Put it in the reliable queue
					stream.reliableQueue.Enqueue(ri);
				}
				else
				{	//Jam it in the reliable queue to be parsed
					ReliableInfo ri = new ReliableInfo();

					ri.packet = packet;
					ri.rid = -1;
					if (completionCallback != null)
						ri.Completed += completionCallback;

					//Put it in the reliable queue
					stream.reliableQueue.Enqueue(ri);
				}
			}
		}

		/// <summary>
		/// Sends a packet using the client socket
		/// </summary>
		internal void internalSend(PacketBase packet)
		{	//First, allow the packet to serialize
			packet.MakeSerialized(this, _handler);

			//Do we need to apply the CRC32?
			if (!_CRC_S2C.bActive)
				_handler.sendPacket(packet, packet.Data, _ipe);
			else
			{	//Perform a CRC check and add it to a new buffer
				byte[] packetData = packet.Data;
				byte[] newPacket = new byte[packetData.Length + _CRCLength];

				Array.Copy(packetData, newPacket, packetData.Length);
				uint checksum = _CRC_S2C.ComputeChecksum(packetData, 0, packetData.Length);

				//Insert a CRC of the appropriate length
				switch (_CRCLength)
				{
					case 1:
						newPacket[packetData.Length] = (byte)(checksum & 0xFF);
						break;

					case 2:
						newPacket[packetData.Length + 1] = (byte)(checksum & 0xFF);
						newPacket[packetData.Length + 0] = (byte)((checksum >> 8) & 0xFF);
						break;

					case 3:
						newPacket[packetData.Length + 2] = (byte)(checksum & 0xFF);
						newPacket[packetData.Length + 1] = (byte)((checksum >> 8) & 0xFF);
						newPacket[packetData.Length + 0] = (byte)((checksum >> 16) & 0xFF);
						break;

					case 4:
						newPacket[packetData.Length + 3] = (byte)(checksum & 0xFF);
						newPacket[packetData.Length + 2] = (byte)((checksum >> 8) & 0xFF);
						newPacket[packetData.Length + 1] = (byte)((checksum >> 16) & 0xFF);
						newPacket[packetData.Length + 0] = (byte)((checksum >> 24) & 0xFF);
						break;
				}

				_handler.sendPacket(packet, newPacket, _ipe);
			}

			//Update our statistics
			_packetsSent++;
			_bytesSent += packet._size;
		}

		/// <summary>
		/// Sends a given packet to the client
		/// </summary>
		public override void send(PacketBase packet)
		{	//Queue the packet up
			using (DdMonitor.Lock(_sync))
				_packetQueue.Enqueue(packet);
		}

		/// <summary>
		/// Allows handling of packets before they're dispatched
		/// </summary>
		public override bool predispatchCheck(PacketBase packet)
		{	//If we're not initialized, it better be an initialize packet
			if (!_bInitialized && !(packet is CS_Initial || packet is SC_Initial))
				return false;

			return true;
		}

		/// <summary>
		/// Checks the integrity of a given packet
		/// </summary>
		public override bool checkPacket(byte[] data, ref int offset, ref int count)
		{	//Update packet stats
			_packetsReceived++;
			_bytesReceived += count;
			
			//Is CRC activated?
			if (!_CRC_C2S.bActive || _CRCLength == 0)
				return true;

			//Clip the CRC from the end of the packet
			count -= _CRCLength;

			//Perform a CRC on the packet
			uint checksum = _CRC_C2S.ComputeChecksum(data, offset, count);
			uint clientChecksum;

			//Check the CRC
			int crcend = count + _CRCLength;
			switch (_CRCLength)
			{
				case 4:
					clientChecksum = (uint)data[crcend - 1] +
										(uint)(data[crcend - 2] << 8) +
										(uint)(data[crcend - 3] << 16) +
										(uint)(data[crcend - 4] << 24);
					return (clientChecksum == checksum);

				case 3:
					clientChecksum = (uint)data[crcend - 1] +
										(uint)(data[crcend - 2] << 8) +
										(uint)(data[crcend - 3] << 16);
					return (clientChecksum == (checksum & 0x00FFFFFF));

				case 2:
					clientChecksum = (uint)data[crcend - 1] +
										(uint)(data[crcend - 2] << 8);
					return (clientChecksum == (checksum & 0x0000FFFF));

				case 1:
					return (data[crcend - 1] == (checksum & 0xFF));

				default:
					return false;
			}
		}

		/// <summary>
		/// Groups packets together into box packets as necessary
		/// </summary>
		public ICollection<ReliableInfo> boxReliablePackets(Queue<ReliableInfo> packetQueue, StreamState stream, int streamID)
		{	//Empty?
			if (packetQueue.Count == 0)
				return null;
			//If it's just one packet, there's no need
			else if (packetQueue.Count == 1)
			{	//We need to put it in a reliable case
				Reliable reli = new Reliable();
				ReliableInfo rinfo = packetQueue.Dequeue();

				reli.streamID = streamID;
				reli.packet = rinfo.packet;
				rinfo.rid = reli.rNumber = stream.S2C_Reliable++;
				rinfo.packet = reli;
				
				List<ReliableInfo> list = new List<ReliableInfo>();
				list.Add(rinfo);
				return list;
			}

			//Go through the list, creating boxed packets as we go
			List<ReliableInfo> reliables = new List<ReliableInfo>();
			ReliableInfo info;
			ReliableBox box = new ReliableBox(streamID);
			int packetStartSize = 4 /*reliable*/ + 2 /*reliable box*/ + _CRCLength;
			int currentSize = packetStartSize;		//Header+footer size of a boxed reliable packet

			//Group our normal packets
			while (packetQueue.Count > 0 && packetQueue.Peek().dataStream == null)
			{
				ReliableInfo pInfo = packetQueue.Dequeue();

				//If the packet exceeds the max limit, send it on it's own
				if (2 + 1 + pInfo.packet.Length > byte.MaxValue)
				{	//We need to send the previous boxing packets first, as they
					//should be in order of reliable id
					if (box.reliables.Count > 0)
					{
						info = new ReliableInfo();
						info.rid = stream.S2C_Reliable++;

						//Don't add a lonely box
						if (box.reliables.Count == 1)
							info.packet = new Reliable(box.reliables[0].packet, info.rid, streamID);
						else
						{	//Make sure the box is serialized
							box.MakeSerialized(this, _handler);

							info.packet = new Reliable(box, info.rid, streamID);
						}

						info.consolidateEvents(box.reliables);

						reliables.Add(info);
					}

					box = new ReliableBox(streamID);
					currentSize = packetStartSize;

					//Add the packet on it's own
					Reliable reli = new Reliable();

					reli.streamID = streamID;
					reli.packet = pInfo.packet;
					pInfo.rid = reli.rNumber = stream.S2C_Reliable++;
					pInfo.packet = reli;

					reliables.Add(pInfo);
					continue;
				}

				//Do we have space to add this packet?
				if (currentSize + pInfo.packet.Length + 1 > udpMaxSize)
				{	//There's not enough room, box up our current packet
					info = new ReliableInfo();
					info.rid = stream.S2C_Reliable++;

					//Don't add a lonely box
					if (box.reliables.Count == 1)
						info.packet = new Reliable(box.reliables[0].packet, info.rid, streamID);
					else
					{	//Make sure the box is serialized
						box.MakeSerialized(this, _handler);

						info.packet = new Reliable(box, info.rid, streamID);
					}

					info.consolidateEvents(box.reliables);

					reliables.Add(info);

					box = new ReliableBox(streamID);
					currentSize = packetStartSize;
				}

				//Add the packet to the box list
				box.reliables.Add(pInfo);
				currentSize += pInfo.packet.Length + 1;
			}

			//If the last box has more than one packet, keep it
			if (box.reliables.Count > 1)
			{
				info = new ReliableInfo();
				info.rid = stream.S2C_Reliable++;

				//Make sure the box is serialized
				box.MakeSerialized(this, _handler);

				info.packet = new Reliable(box, info.rid, streamID);
				info.consolidateEvents(box.reliables);

				reliables.Add(info);
			}
			else if (box.reliables.Count == 1)
			{	//If it's only one packet, we don't need the box
				info = new ReliableInfo();
				info.rid = stream.S2C_Reliable++;
				info.packet = new Reliable(box.reliables[0].packet, info.rid, streamID);
				info.consolidateEvents(box.reliables[0]);

				reliables.Add(info);
			}

			//Boxed them all
			return reliables;
		}

		/// <summary>
		/// Sends queued packets, grouping them where necessary
		/// </summary>
		public void sendQueuedPackets()
		{	//Are we over threshold?
			if (_bytesWritten > _rateThreshold)
				return;

			//If it's just one packet, there's no need
			int queueCount = _packetQueue.Count;
			if (queueCount == 0)
				return;
			else if (queueCount == 1)
			{
				PacketBase packet = _packetQueue.Dequeue();

				internalSend(packet);
				_bytesWritten += packet._size;
				return;
			}

			//Go through the list, creating boxed packets as we go
			List<PacketBase> boxes = new List<PacketBase>();
			BoxPacket box = new BoxPacket();
			int currentSize = 2 + _CRCLength;		//Header+footer size of a box packet

			//Send as many packets as we can!
			while (queueCount > 0 && _bytesWritten < _rateThreshold)
			{	//Get our next packet
				PacketBase packet = _packetQueue.Dequeue();

				_bytesWritten += packet._size;
				queueCount--;

				//Do not group data packets
				if (packet is DataPacket) 
				{
					boxes.Add(packet);
					continue;
				}

				//Make sure the packet is serialized before we go comparing size
				packet.MakeSerialized(this, _handler);

				//If the packet exceeds the max limit, send it on it's own
				if (2 + 1 + packet.Length > byte.MaxValue)
				{	//WARNING: This may disrupt the reliable flow?
					boxes.Add(packet);
					continue;
				}

				//Do we have space to add this packet?
				if (currentSize + packet.Length + 1 > udpMaxSize)
				{	//There's not enough room. Check if our previous packet is on it's
					//own and actually warrants a box packet.
					if (box.packets.Count == 1)
						boxes.Add(box.packets[0]);
					else
						boxes.Add(box);

					//Create our new box
					box = new BoxPacket();
					currentSize = 2 + _CRCLength;
				}

				//Add the packet to the box list
				box.packets.Add(packet);
				currentSize += packet.Length + 1;
			}
	
			//If the last box has more than one packet, keep it
			if (box.packets.Count > 1)
				boxes.Add(box);
			else if (box.packets.Count == 1)
				//If it's only one packet, we don't need the box
				boxes.Add(box.packets[0]);

			//Send all our packets
			foreach (PacketBase packet in boxes)
				internalSend(packet);
		}
		#endregion

		/// <summary>
		/// Creates a new network client class of the same type.
		/// </summary>
		public override NetworkClient newInstance()
		{
			return new Client(false);
		}
	}

	// Client Generic
	/// Allows the client class to properly represent it's purpose
	/// and facilitate routing.
	///////////////////////////////////////////////////////
	public class Client<T> : Client
		where T : IClient
	{	// Member variables
		///////////////////////////////////////////////////
		public T _obj;					//The object represented by this connection


		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Initializes the new client class
		/// </summary>
		public Client(bool bClientConn)
			: base(bClientConn)
		{
		}

		/// <summary>
		/// Allows our client object to be destroyed properly
		/// </summary>
		public override void destroy()
		{	//Redirect
			if (_obj != null)
				_obj.destroy();

			base.destroy();
		}

		/// <summary>
		/// Creates a new network client class of the same type.
		/// </summary>
		public override NetworkClient newInstance()
		{	
			return new Client<T>(false);
		}
	}
}