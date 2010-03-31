using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using InfServer.Protocol;

namespace InfServer.Network
{
	// ClientConn Class
	/// Uses the Client class to connect to a server
	///////////////////////////////////////////////////////
	public class ClientConn<T> : IPacketHandler
		where T: IClient
	{	// Member variables
		///////////////////////////////////////////////////
		public Client<T> _client;					//Our client, including the connection state
		public LogClient _logger;				//The logger we're writing to

		private volatile bool _bOperating;		//Are we still operating?
		protected UdpClient _udp;				//Our udp connection

		private IPEndPoint _remEP;				//The location the packet came from

		private IPacketFactory _factory;		//The protocol we're using to construct packets
		private T _obj;							//The object we represent as a connection

		public ReaderWriterLock _networkLock;	//The controlling lock on the packet queue and client list
		public volatile bool _packetsWaiting;	//Are there packets waiting to be processed?
		public Queue<PacketBase> _packetQueue;	//The list of packets received and waiting to be handled

		private Thread _listenThread;			//Thread used to dispatch packets and handle network activity
		private IPEndPoint _targetPoint;		//The endpoint to bind the server to

		public Random _rand;					//Our PRNG

		private int _tickLastPing;				//The last time we sent a ping packet

		//Settings
		public bool _bLogPackets;
		static public int clientPingFreq;


		///////////////////////////////////////////////////
		// Properties
		///////////////////////////////////////////////////
		/// <summary>
		/// Generic constructor
		/// </summary>
		public bool IsConnected
		{
			get
			{
				return _client != null;
			}
		}


		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Generic constructor
		/// </summary>
		public ClientConn(IPacketFactory factory, T obj)
		{
			_logger = null;

			_factory = factory;
			_obj = obj;

			_packetQueue = new Queue<PacketBase>();
			_networkLock = new ReaderWriterLock();

			_rand = new Random();
		}

		/// <summary>
		/// Begins all network server operations
		/// </summary>
		/// <param name="targetPoint">The endpoint to connect to.</param>
		public void begin(IPEndPoint targetPoint)
		{	//Save parameters
			_targetPoint = targetPoint;

			//Close our current listener thread
			if (_listenThread != null)
				_listenThread.Abort();

			//Create a new client object
			if (_client != null)
				_client.destroy();
			_client = new Client<T>(true);

			_client._obj = _obj;
			_client._handler = this;
			_client._ipe = targetPoint;
			_client._lastPacketRecv = Environment.TickCount;

			_client.Destruct += onClientDestroy;

			//Ready our udp client
			if (_udp != null)
				_udp.Close();
			_udp = new UdpClient();
			_udp.DontFragment = true;

			_udp.Connect(targetPoint);

			//Start listening for packets
			_udp.BeginReceive(onDataRecieved, null);
			_bOperating = true;

			//Restart the listen thread
			_listenThread = new Thread(new ThreadStart(listen));
			_listenThread.IsBackground = true;
			_listenThread.Name = "NetworkDispatch";
			_listenThread.Start();
		}

		/// <summary>
		/// Handles all network server operations in a seperate thread
		/// </summary>
		/// <param name="listenPoint">The endpoint to bind on.</param>
		private void listen()
		{
			Log.assume(_logger);

			_bOperating = true;
			_remEP = new IPEndPoint(IPAddress.Any, 0);

			// Begin handling received packets
			////////////////////////////////
			while (_bOperating)
			{	//Read required data from the server state while using
				//as little lock time as possible.
				Queue<PacketBase> packets = null;

				_networkLock.AcquireWriterLock(Timeout.Infinite);

				try
				{
					if (_packetsWaiting)
					{	//Take the entire queue, and substitute the current
						//queue for a new one
						packets = _packetQueue;

						_packetQueue = new Queue<PacketBase>();
						_packetsWaiting = false;
					}
				}

				finally
				{	//Release our lock
					_networkLock.ReleaseWriterLock();
				}

				if (packets != null)
				{	//Predispatch, then handle each packet
					foreach (PacketBase packet in packets)
						if (_client.predispatchCheck(packet))
							routePacket(packet);
				}

				//Poll our active network client
				_client.poll();

			}

			_listenThread = null;
		}

		/// <summary>
		/// Allows the client to take care of anything it needs to
		/// </summary>
		public void poll()
		{	//Are we overdue on sending a ping?
			if (Environment.TickCount - _tickLastPing > clientPingFreq)
			{	//Send one!
				_tickLastPing = Environment.TickCount;
				_client.send(new PingPacket());
			}
		}

		/// <summary>
		/// Called when client is destroyed due to the connection being terminated
		/// </summary>
		private void onClientDestroy(NetworkClient client)
		{	//We're no longer operating
			_client = null;
			_bOperating = false;

			_udp.Close();
			_udp = null;
		}

		/// <summary>
		/// Delegate for asynchronously receiving UDP packets
		/// </summary>
		private void onDataRecieved(IAsyncResult asyn)
		{	//If we're not operating, abort.
			if (!_bOperating)
				return;
			
			//Use the logger
			using (LogAssume.Assume(_logger))
			{
				PacketBase packet = null;
				byte[] data = null;

				try
				{	//Sanity checks
					if (!asyn.IsCompleted)
					{	//Abort
						Log.write(TLog.Warning, "Asynchronous socket operation not completed.");
						return;
					}

					//Receive the data
					data = _udp.EndReceive(asyn, ref _remEP);
					int dataLen = data.Length;
					int offset = 0;

					//Read in the typeID
					ushort typeID = NetworkClient.getTypeID(data, 0);

					//If our client is inactive, ignore this packet
					if (!_client._bDestroyed)
					{	//Make sure the packet is intact
						if (!_client.checkPacket(data, ref offset, ref dataLen))
							//Bad packet!
							Log.write(TLog.Warning, "Bad packet received from server.");
						else
						{	//Transplant the data into a packet class
							packet = _factory.createPacket(_client, typeID, data, offset, dataLen);
							packet._client = _client;
							packet._handler = this;

							packet.Deserialize();

							//Queue it up
							handlePacket(packet, _client);
							_client._lastPacketRecv = Environment.TickCount;
						}
					}
				}
				catch (ObjectDisposedException)
				{	//Socket was closed
					Log.write(TLog.Inane, "Socket closed.");
					_bOperating = false;
					return;
				}
				catch (SocketException se)
				{	//Store the exception and exit
					Log.write(TLog.Exception, "Socket exception[{0}]:\r\n{1}", se.ErrorCode, se.ToString());
					_bOperating = false;
					return;
				}
				catch (Exception ex)
				{	//Store the exception and exit
					Log.write(TLog.Exception, "Exception on recieving data:\r\n{0}", ex.ToString());
					if (packet != null)
						Log.write(TLog.Inane, "Packet data:\r\n{0}", packet.DataDump);
					else if (data != null)
						Log.write(TLog.Inane, "Packet data:\r\n{0}", PacketBase.createDataDump(data, 0, data.Length));
				}

				try
				{	//Wait for more data
					if (_bOperating)
						_udp.BeginReceive(onDataRecieved, null);
				}
				catch (SocketException se)
				{	//Store the exception and exit
					Log.write(TLog.Exception, "Socket exception[{0}]:\r\n{1}", se.ErrorCode, se.ToString());
				}
			}
		}

		/// <summary>
		/// Routes a packet to the relevant handling functions
		/// </summary>
		private void routePacket(PacketBase packet)
		{	//Log packets?
			if (_bLogPackets)
				Log.write(TLog.Normal, "<-- Packet: {0}\r\n{1}", _logger, packet.Dump, packet.DataDump);

			try
			{	//Allow the packet type to call relevant handlers
				packet.Route();
			}
			catch (Exception ex)
			{
				Log.write(TLog.Exception, "Exception while routing packet:\r\n{0}\r\n{1}", packet, ex.ToString());
			}
		}

		/// <summary>
		/// Adds the given packet to the packet handling queue
		/// </summary>
		public void handlePacket(PacketBase packet, NetworkClient client)
		{	//Attempt to find the client responsible
			_networkLock.AcquireWriterLock(Timeout.Infinite);

			try
			{	//Queue it up
				packet._client = _client;

				_packetQueue.Enqueue(packet);
				_packetsWaiting = true;
			}
			finally
			{	//Release our lock
				_networkLock.ReleaseWriterLock();
			}
		}

		/// <summary>
		/// Sends a packet
		/// </summary>
		public void sendPacket(PacketBase packet, byte[] data, EndPoint ep)
		{	//Log packets?
			if (_bLogPackets)
				Log.write(TLog.Normal, "--> Packet: {0}\r\n{1}", _logger, packet.Dump, packet.DataDump);

			try
			{
				_udp.Send(data, data.Length);
			}
			catch (Exception ex)
			{
				Log.write(TLog.Exception, "Exception while sending packet:\r\n{0}\r\n{1}", packet, ex.ToString());
			}
		}

		/// <summary>
		/// Gets our packet factory interface
		/// </summary>
		public IPacketFactory getFactory()
		{
			return _factory;
		}
	}
}
