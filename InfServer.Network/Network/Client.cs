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

		public int _timeDiff;					//The current difference in the tickcount between client and server

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

		public ushort _C2S_Reliable;			//The expected number for the next reliable message
		public ushort _S2C_Reliable;			//
		public ushort _S2C_ReliableConfirmed;	//The last reliable packet awaiting confirmation

		public SortedDictionary<ushort, ReliableInfo> _reliablePackets;	//The reliable packets we're looking after
		public SortedDictionary<ushort, PacketBase> _oosReliable;		//Reliable packets sent out of sync by the client,
																		//used for synchronization later
		public Queue<DataStream> _dataQueue;	//A queue of large packets waiting to be sent

		public Queue<PacketBase> _packetQueue;				//The list of packets waiting to be sent
		public Queue<ReliableInfo> _packetReliableQueue;	//The list of reliable packets waiting to be sent
		#endregion

		//Static settings
		static public int udpMaxSize;
		static public int crcLength;

		static public int reliableJuggle;		//The max number of reliable packets to be redelivered at once
		static public int reliableGrace;
		static public int connectionTimeout;


		///////////////////////////////////////////////////
		// Member Classes
		///////////////////////////////////////////////////
		#region Member Classes
		/// <summary>
		/// Contains connection statistics for this client
		/// </summary>
		public class ConnectionStats
		{
			public ulong C2S_packetsSent;	//Packet count statistics
			public ulong C2S_packetsRecv;	//
			public ulong S2C_packetsSent;	//
			public ulong S2C_packetsRecv;	//

			public uint C2S_Loss;			//Packetloss C2S
			public uint S2C_Loss;			//Packetloss S2C

			public ushort[] clockWander;	//Clock wander samples
			public int idx;					//

			public ushort AverageClockWander
			{
				get
				{	//Perform an average!
					ushort total = 0;

					for (int i = 0; i < clockWander.Length; ++i)
						total += clockWander[i];

					return (ushort)(total / clockWander.Length);
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
			_stats.clockWander = new ushort[10];

			_CRC_C2S = new CRC32();
			_CRC_S2C = new CRC32();

			_C2S_Reliable = 0;
			_S2C_Reliable = 0;

			_reliablePackets = new SortedDictionary<ushort, ReliableInfo>();
			_oosReliable = new SortedDictionary<ushort, PacketBase>();
			_dataQueue = new Queue<DataStream>();

			_packetQueue = new Queue<PacketBase>();
			_packetReliableQueue = new Queue<ReliableInfo>();
		}

		#region State
		/// <summary>
		/// Allows the client to take care of anything it needs to
		/// </summary>
		public override void poll()
		{	//Sync up!
			using (DdMonitor.Lock(_sync))
			{	//Only time out server-side clients
				if (!_bClientConn && Environment.TickCount - base._lastPacketRecv > connectionTimeout)
				{	//Farewell~
					Log.write(TLog.Warning, "Client timeout: {0}", this);
					destroy();
					return;
				}

				//Look after our reliable packets
				int reliableDistanceLeft = reliableJuggle;
				ensureReliable(ref reliableDistanceLeft);

				//Handle our data stream
				ensureDataFlow(reliableJuggle);

				//Box packets as necessary and send them out
				IEnumerable<PacketBase> boxed = boxPackets(_packetQueue);
				if (boxed != null)
				{
					foreach (PacketBase packet in boxed)
						internalSend(packet);
					_packetQueue.Clear();
				}
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
				discon.reasonID = 6;

				_packetQueue.Enqueue(discon);

				base.destroy();
			}
		}
		#endregion

		#region Connection
		/// <summary>
		/// Ensures that the client is sending all large data packets
		/// </summary>
		private void ensureDataFlow(int reliableDistanceLeft)
		{	//Are we currently streaming any data?
			if (_dataQueue.Count == 0)
				return;

			//Look after our current stream
			DataStream ds = _dataQueue.Peek();

			//We want to parse the entire data stream into reliable packets,
			//and store them in the queue at once. This is because if any
			//other reliable packet interrupts the sequence of data packets,
			//then infantry.exe dies a painful death.
			while (ds.amountSent < ds.buffer.Length && --reliableDistanceLeft > 0)
			{	//Prepare a data packet
				DataPacket dp = new DataPacket();

				dp._bFirstPacket = (ds.amountSent == 0);
				dp.data = ds.buffer;
				dp.offset = ds.amountSent;

				dp.rNumber = _S2C_Reliable++;

				//Serialize our packet..
				dp._client = this;
				dp._handler = _handler;

				dp.Serialize();
				dp._bSerialized = true;

				ds.lastPacket = dp;		//For completion notification

				//.. in order to the obtain the amount read
				ds.amountSent += dp._dataRead;

				//Add it to our list of reliable packets
				ReliableInfo ri = new ReliableInfo();

				ri.streamParent = ds;
				ri.packet = dp;
				ri.rid = dp.rNumber;
				ri.timeSent = 0;

				_reliablePackets[dp.rNumber] = ri;
			}

			if (ds.amountSent >= ds.buffer.Length)
				//We're completed!
				_dataQueue.Dequeue();
		}

		/// <summary>
		/// Ensures that the client is receiving all reliable packets sent
		/// </summary>
		/// <remarks>Also is the only function that sends reliable packets.</remarks>
		private void ensureReliable(ref int reliableDistanceLeft)
		{	//Take care of reliable packet waiting to be streamed
			if (_packetReliableQueue.Count > 0)
			{	//Box 'em up
				ICollection<ReliableInfo> reliableBoxed = boxReliablePackets(_packetReliableQueue);
				_packetReliableQueue.Clear();

				//Insert them all into our reliable table
				foreach (ReliableInfo info in reliableBoxed)
				{	//Make sure the rid is correct
					if (info.rid == -1)
					{
						Log.write(TLog.Error, "Reliable info was queued for sending with an unassigned reliable number.");
						continue;
					}

					_reliablePackets[(ushort)info.rid] = info;
				}
			}

			//Compare times
			int currentTick = Environment.TickCount;

			//We want to start with the first sent packet
			for (ushort i = _S2C_ReliableConfirmed; i < _S2C_Reliable; ++i)
			{	//We don't want to be resending too many reliable values at once,
				//or we'll have a slew of timeouts and out-of-sync packets.
				if (reliableDistanceLeft-- == 0)
					break;
				
				//Does it exist?
				ReliableInfo ri;

				if (!_reliablePackets.TryGetValue(i, out ri))
					continue;

				//Has it been delayed too long?
				if (currentTick - ri.timeSent < reliableGrace)
					continue;

				//Resend it
				_packetQueue.Enqueue(ri.packet);

				//Was it a reattempt?
				if (ri.timeSent != 0)
				{
					ri.attempts++;
					Log.write(TLog.Warning, "Reliable packet #" + ri.rid + " lost. (" + ri.attempts + ")");
				}

				ri.timeSent = Environment.TickCount;
			}
		}

		/// <summary>
		/// Confirms that a reliable packet has been received by the client.
		/// Note that a higher rID than the lowest expected indicates that all 
		/// previous reliable packets were received.
		/// </summary>
		public void confirmReliable(ushort rID)
		{	//Great!
			using (DdMonitor.Lock(_sync))
			{	//This satisfies all packets inbetween
				for (ushort i = _S2C_ReliableConfirmed; i <= rID; ++i)
				{	//Get our associated info
					ReliableInfo ri;

					if (!_reliablePackets.TryGetValue(i, out ri))
						continue;

					_reliablePackets.Remove(i);

					//Part of a data stream?
					if (ri.streamParent != null)
					{
						if (ri.streamParent.lastPacket == ri.packet)
							ri.streamParent.onCompleted();
					}
					else
						ri.onCompleted();
				}

				_S2C_ReliableConfirmed = (ushort)(rID + 1);
			}
		}

		/// <summary>
		/// Sends a reliable packet to the client
		/// </summary>
		public void sendReliable(PacketBase packet)
		{	//Relay
			sendReliable(packet, null);
		}

		/// <summary>
		/// Sends a reliable packet to the client
		/// </summary>
		public void sendReliable(PacketBase packet, Action completionCallback)
		{	//Sync up!
			using (DdMonitor.Lock(_sync))
			{	//Make sure the packet is serialized
				if (!packet._bSerialized)
				{
					packet._client = this;
					packet._handler = _handler;

					packet.Serialize();
					packet._bSerialized = true;
				}
				
				//Is the packet too large to be sent as one?
				if (packet._size > _C2S_UDPSize)
				{	//We must add it to the data packet queue..
					DataStream ds = new DataStream();

					ds.amountSent = 0;
					ds.buffer = packet.Data;
					ds.Completed += completionCallback;

					_dataQueue.Enqueue(ds);
					return;
				}
				
				//Jam it in the reliable queue to be parsed
				ReliableInfo ri = new ReliableInfo();

				ri.packet = packet;
				ri.rid = -1;
				if (completionCallback != null)
					ri.Completed += completionCallback;

				//Put it in the reliable queue
				_packetReliableQueue.Enqueue(ri);
			}
		}

		/// <summary>
		/// Sends a packet using the client socket
		/// </summary>
		internal void internalSend(PacketBase packet)
		{	//First, allow the packet to serialize
			if (!packet._bSerialized)
			{
				packet._client = this;
				packet._handler = _handler;

				packet.Serialize();
				packet._bSerialized = true;
			}

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
			_stats.S2C_packetsSent++;
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

			//We've got another!
			_stats.S2C_packetsRecv++;
			return true;
		}

		/// <summary>
		/// Checks the integrity of a given packet
		/// </summary>
		public override bool checkPacket(byte[] data, ref int offset, ref int count)
		{	//Is CRC activated?
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
		public ICollection<ReliableInfo> boxReliablePackets(Queue<ReliableInfo> packetQueue)
		{	//Empty?
			if (packetQueue.Count == 0)
				return null;
			//If it's just one packet, there's no need
			else if (packetQueue.Count == 1)
			{	//We need to put it in a reliable case
				Reliable reli = new Reliable();
				ReliableInfo rinfo = packetQueue.Peek();

				reli.packet = rinfo.packet;
				rinfo.rid = reli.rNumber = _S2C_Reliable++;
				rinfo.packet = reli;

				List<ReliableInfo> list = new List<ReliableInfo>();
				list.Add(rinfo);
				return list;
			}

			//Go through the list, creating boxed packets as we go
			List<ReliableInfo> reliables = new List<ReliableInfo>();
			ReliableInfo info;
			ReliableBox box = new ReliableBox();
			int currentSize = 2 + _CRCLength;		//Header+footer size of a box packet

			//Group our normal packets
			foreach (ReliableInfo pInfo in packetQueue)
			{	//If the packet exceeds the max limit, send it on it's own
				if (2 + 1 + pInfo.packet.Length > udpMaxSize)
				{	//We need to send the previous boxing packets first, as they
					//should be in order of reliable id
					if (box.reliables.Count > 0)
					{
						info = new ReliableInfo();
						info.rid = _S2C_Reliable++;
						info.packet = new Reliable(box, info.rid);
						info.consolidateEvents(box.reliables);

						reliables.Add(pInfo);
					}

					box = new ReliableBox();
					currentSize = 2 + _CRCLength;

					//Add the packet on it's own
					Reliable reli = new Reliable();
					reli.packet = pInfo.packet;
					reli.rNumber = _S2C_Reliable++;
					pInfo.packet = reli;

					reliables.Add(pInfo);
					continue;
				}

				//Do we have space to add this packet?
				if (currentSize + pInfo.packet.Length + 1 > udpMaxSize)
				{	//There's not enough room, box up our current packet
					info = new ReliableInfo();
					info.rid = _S2C_Reliable++;
					info.packet = new Reliable(box, info.rid);
					info.consolidateEvents(box.reliables);

					reliables.Add(info);

					box = new ReliableBox();
					currentSize = 2 + _CRCLength;
				}

				//Add the packet to the box list
				box.reliables.Add(pInfo);
				currentSize += pInfo.packet.Length + 1;
			}

			//If the last box has more than one packet, keep it
			if (box.reliables.Count > 1)
			{
				info = new ReliableInfo();
				info.rid = _S2C_Reliable++;
				info.packet = new Reliable(box, info.rid);
				info.consolidateEvents(box.reliables);

				reliables.Add(info);
			}
			else if (box.reliables.Count == 1)
			{	//If it's only one packet, we don't need the box
				info = new ReliableInfo();
				info.rid = _S2C_Reliable++;
				info.packet = new Reliable(box.reliables[0].packet, info.rid);
				info.consolidateEvents(box.reliables[0]);

				reliables.Add(info);
			}

			//Boxed them all
			return reliables;
		}

		/// <summary>
		/// Groups packets together into box packets as necessary
		/// </summary>
		public IEnumerable<PacketBase> boxPackets(Queue<PacketBase> packetQueue)
		{	//If it's just one packet, there's no need
			if (packetQueue.Count <= 1)
				return packetQueue;

			//Go through the list, creating boxed packets as we go
			List<PacketBase> boxes = new List<PacketBase>();
			BoxPacket box = new BoxPacket();
			int currentSize = 2 + _CRCLength;		//Header+footer size of a box packet

			//Group our normal packets
			foreach (PacketBase packet in packetQueue)
			{	//Do not group data packets
				if (packet is DataPacket)
				{
					boxes.Add(packet);
					continue;
				}

				//If the packet exceeds the max limit, send it on it's own
				if (2 + 1 + packet.Length > udpMaxSize)
				{
					boxes.Add(packet);
					continue;
				}

				//Do we have space to add this packet?
				if (currentSize + packet.Length + 1 > udpMaxSize)
				{	//There's not enough room, box up our current packet
					boxes.Add(box);
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

			//We've done it!
			return boxes;
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
