using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace InfServer.Network
{
	// Server Class
	/// Used to listen for incoming traffic and keep tabs on
	/// who's connected to the server.
	///////////////////////////////////////////////////////
	public class Server : IPacketHandler
	{	// Member variables
		///////////////////////////////////////////////////
		public LogClient _logger;				//The logger we're writing to

		private volatile bool _bOperating;		//Are we still operating?
		protected Socket _sock;					//Our listening socket

		private byte[] _buffer;					//Our data buffer
		private EndPoint _remEP;				//The location the packet came from

		private NetworkClient _clientTemplate;	//The class we're using to track clients
		private IPacketFactory _factory;		//The protocol we're using to construct packets

		public ReaderWriterLock _networkLock;	//The controlling lock on the packet queue and client list
		public volatile bool _packetsWaiting;	//Are there packets waiting to be processed?
		public Queue<PacketBase> _packetQueue;	//The list of packets received and waiting to be handled

		private Thread _listenThread;			//Thread used to dispatch packets and handle network activity
		private IPEndPoint _listenPoint;		//The endpoint to bind the server to

		public Random _rand;					//Our PRNG

		#region Statistics
		public long _totalBytesSent;			//Total bytes sent by the server
		public long _totalBytesReceived;		//Total bytes received by the server
		#endregion

		//The list of clients in communication with the server
		public Dictionary<Int64, NetworkClient> _clients;

		//Settings
		public bool _bLogPackets;


		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Generic constructor
		/// </summary>
		public Server(IPacketFactory factory, NetworkClient clientTemplate)
		{
			_buffer = new byte[4096];
			_logger = null;

			_factory = factory;
			_clientTemplate = clientTemplate;

			_packetQueue = new Queue<PacketBase>();
			_networkLock = new ReaderWriterLock();

			_clients = new Dictionary<long, NetworkClient>();

			_rand = new Random();
		}

		#region State
		/// <summary>
		/// Begins all network server operations
		/// </summary>
		/// <param name="listenPoint">The endpoint to bind on.</param>
		public void begin(IPEndPoint listenPoint)
		{	//Save parameters
			_listenPoint = listenPoint;

			//Create a new listen thread
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

			try
			{	//Create a new UDP socket to use
				_bOperating = true;

				_sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				_remEP = new IPEndPoint(IPAddress.Any, 0);

				_sock.DontFragment = true;

				//Prevent useless connection reset exceptions
				uint IOC_IN = 0x80000000;
				uint IOC_VENDOR = 0x18000000;
				uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
				_sock.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);

				//Bind our socket
				_sock.Bind(_listenPoint);

				//Begin listening for packets
				_sock.BeginReceiveFrom(_buffer, 0, _buffer.Length, SocketFlags.None, ref _remEP, onDataRecieved, null);
			}
			catch (SocketException se)
			{	//Failure!
				Log.write(TLog.Exception, "Encountered an exception while listening:\r\n{0}", se.ToString());
			}

			// Begin handling received packets
			////////////////////////////////
			while (_bOperating)
			{	//Read required data from the server state while using
				//as little lock time as possible.
				Queue<PacketBase> packets = null;
				List<NetworkClient> activeClients;

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

					//Create an image of the client list
					activeClients = _clients.Values.ToList();
				}
				finally
				{	//Release our lock
					_networkLock.ReleaseWriterLock();
				}

				if (packets != null)
				{	//Predispatch, then handle each packet
					foreach (PacketBase packet in packets)
						if (packet._client.predispatchCheck(packet))
							routePacket(packet);
				}

				//Poll each active network client
				foreach (NetworkClient client in activeClients)
					if (!client._bDestroyed)
						client.poll();

                // Sleep a bit
			    Thread.Sleep(5);
			}
		}

		/// <summary>
		/// Delegate for asynchronously receiving UDP packets
		/// </summary>
		private void onDataRecieved(IAsyncResult asyn)
		{	//Use the logger
			using (LogAssume.Assume(_logger))
			{
				PacketBase packet = null;
				int read = 0;

				try
				{	//Sanity checks
					if (!asyn.IsCompleted)
					{	//Abort
						Log.write(TLog.Warning, "Asynchronous socket operation not completed.");
						return;
					}

					//Receive the data
					read = _sock.EndReceiveFrom(asyn, ref _remEP);

					//Read in the typeID
					ushort typeID = NetworkClient.getTypeID(_buffer, 0);

					//Handle the initial packet specially
					bool bNewClient = false;

					//Initial && system packet?
					if (_buffer[0] == 0 && typeID == Protocol.CS_Initial.TypeID)
					{	//If we're receiving the initial packet, then the client will
						//want to begin a new connection state. Destroy the old one!
						//Protocol.CS_Initial init = packet as Protocol.CS_Initial;
						bNewClient = true;
					}

					//Attempt to find the related NetworkClient
					_networkLock.AcquireWriterLock(Timeout.Infinite);
					NetworkClient client = null;

					try
					{	//Form the uid for the client
						IPEndPoint ipe = (IPEndPoint)_remEP;
						Int64 id = ipe.Address.Address | (((Int64)ipe.Port) << 32);

						//Do we have a client?
						if (bNewClient)
						{	//This client doesn't exist yet, let's create a new class
							client = _clientTemplate.newInstance();
							client._lastPacketRecv = Environment.TickCount;
							client._ipe = ipe;
							client._handler = this;

							//Add it to our client list
							_clients[id] = client;
						}
						else
							_clients.TryGetValue(id, out client);

						//Out of sync packet?
						if (client == null)
							Log.write(TLog.Inane, "Out of state packet received from {0}", _remEP);
						//If the client is inactive, ignore
						else if (client._bDestroyed)
							client = null;
					}
					finally
					{	//Release our lock
						_networkLock.ReleaseWriterLock();
					}

					if (client != null)
					{	//Make sure the packet is intact
						int offset = 0;
						if (!client.checkPacket(_buffer, ref offset, ref read))
							//Bad packet!
							Log.write(TLog.Warning, "Bad packet received from {0}", client);
						else
						{	//Transplant the data into a packet class
							packet = _factory.createPacket(client, typeID, _buffer, offset, read);
							packet._client = client; //Error's here
    						packet._handler = this;

							packet.Deserialize();

							//Queue it up
							handlePacket(packet, client);
							client._lastPacketRecv = Environment.TickCount;
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
				}
				catch (Exception ex)
				{	//Store the exception and exit
					Log.write(TLog.Exception, "Exception on recieving data:\r\n{0}", ex.ToString());
					if (packet != null)
						Log.write(TLog.Inane, "Packet data:\r\n{0}", packet.DataDump);
					else
						Log.write(TLog.Inane, "Packet data:\r\n{0}", PacketBase.createDataDump(_buffer, 0, read));
				}

				try
				{	//Wait for more data
					_sock.BeginReceiveFrom(_buffer, 0, _buffer.Length, SocketFlags.None, ref _remEP, onDataRecieved, null);
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
		{	//Add it to our received amount
			_totalBytesReceived += packet._size;

			//Attempt to find the client responsible
			_networkLock.AcquireWriterLock(Timeout.Infinite);

			try
			{	//Queue it up
				packet._client = client;

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
                _sock.SendTo(data, ep);
            }
            catch (Exception)
            {
                //Log.write(TLog.Exception, e.Message);
                Log.write(TLog.Exception, packet.DataDump);
            }

			//Add it to our sent amount
			_totalBytesSent += packet._size;
		}

		/// <summary>
		/// Removes a client from the client list
		/// </summary>
		public void removeClient(NetworkClient client)
		{
			_networkLock.AcquireWriterLock(Timeout.Infinite);

			try
			{	//Look for the culprit and remove it
				_clients.Remove(client._clientID);
				client.destroy();
			}
			finally
			{	//Release our lock
				_networkLock.ReleaseWriterLock();
			}
		}

		/// <summary>
		/// Gets our packet factory interface
		/// </summary>
		public IPacketFactory getFactory()
		{
			return _factory;
		}
		#endregion

		#region Statistics
	
		#endregion
	}
}
