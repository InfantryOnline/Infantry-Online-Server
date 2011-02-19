using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using InfServer.Network;
using InfServer.Protocol;
using InfServer.Data;

using Assets;


namespace InfServer.Game
{
	// ZoneServer Class
	/// Represents the entire server state
	///////////////////////////////////////////////////////
	public partial class ZoneServer : Server
	{	// Member variables
		///////////////////////////////////////////////////
		public ConfigSetting _config;			//Our server config
		public CfgInfo _zoneConfig;				//The zone-specific configuration file

		public AssetManager _assets;

		public Database _db;					//Our connection to the database
 
		public new LogClient _logger;			//Our zone server log

		private bool _bStandalone;				//Are we in standalone mode?

	    private ClientPingResponder _pingResponder;

        /// <summary>
        /// Responds to ping and player count requests made by the client. Runs on the port
        /// above the zone server.
        /// </summary>
        private class ClientPingResponder
        {
            private Dictionary<ushort, Player> _players;
            private Thread _listenThread;
            private Socket _socket;
            private Dictionary<EndPoint, Int32> _clients;
            private Boolean _isOperating;
            private ReaderWriterLock _lock;
            private byte[] _buffer;

            public ClientPingResponder(Dictionary<ushort, Player> players)
            {
                _players = players;
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _clients = new Dictionary<EndPoint, Int32>();
                _lock = new ReaderWriterLock();
                _buffer = new byte[4];
            }

            public void Begin(IPEndPoint listenPoint)
            {
                _listenThread = new Thread(Listen);
                _listenThread.IsBackground = true;
                _listenThread.Name = "ClientPingResponder";
                _listenThread.Start(listenPoint);
            }

            private void Listen(Object obj)
            {
                var listenPoint = (IPEndPoint)obj;
                EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

                //Prevent useless connection reset exceptions
                uint IOC_IN = 0x80000000;
                uint IOC_VENDOR = 0x18000000;
                uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
                _socket.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);

                _socket.Bind(listenPoint);
                _socket.BeginReceiveFrom(_buffer, 0, _buffer.Length, SocketFlags.None, ref remoteEP, OnRequestReceived, null);

                _isOperating = true;

                // Do we have clients to service?
                while(_isOperating)
                {
                    Dictionary<EndPoint, Int32> queue = null;
                    _lock.AcquireWriterLock(Timeout.Infinite);

                    // Swap the queue
                    try
                    {
                        queue = _clients;
                        _clients = new Dictionary<EndPoint, Int32>();
                    }
                    finally
                    {
                        _lock.ReleaseWriterLock();
                    }

                    if(queue != null && queue.Count != 0)
                    {
                        // May not be synchronized, but that's okay, the client requests often.
                        byte[] playerCount = BitConverter.GetBytes(_players.Count);

                        foreach(var entry in queue)
                        {
                            // TODO: Refactor this into something cultured
                            EndPoint client = entry.Key;
                            byte[] token = BitConverter.GetBytes(entry.Value);

                            byte[] buffer = new []
                                                {
                                                    playerCount[0], playerCount[1], playerCount[2], playerCount[3],
                                                    token[0], token[1], token[2], token[3]
                                                };

                            _socket.SendTo(buffer, client);
                        }
                    }

                    Thread.Sleep(10);
                }
            }

            private void OnRequestReceived(IAsyncResult result)
            {
                if(!result.IsCompleted)
                {
                    // Log this
                    return;
                }

                EndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);
                int read = _socket.EndReceiveFrom(result, ref remoteEp);

                if(read != 4)
                {
                    // Log and disregard
                    return;
                }

                _lock.AcquireWriterLock(Timeout.Infinite);

                try
                {
                    Int32 token = BitConverter.ToInt32(_buffer, 0);
                    _clients[remoteEp] = token;
                }
                finally
                {
                    _lock.ReleaseWriterLock();
                }

                remoteEp = new IPEndPoint(IPAddress.Any, 0);
                _socket.BeginReceiveFrom(_buffer, 0, _buffer.Length, SocketFlags.None, ref remoteEp, OnRequestReceived, null);
            }
        }

		/// <summary>
		/// Indicates whether the server is in standalone (no database) mode
		/// </summary>
		public bool IsStandalone
		{
			get
			{
				return _bStandalone;
			}

			set
			{
				//TODO: Kick all players from the server, etc
			}
		}

		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Generic constructor
		/// </summary>
		public ZoneServer()
			: base(new PacketFactory(), new Client<Player>(false))
		{
			_config = ConfigSetting.Blank;
		}

		/// <summary>
		/// Allows the server to preload all assets.
		/// </summary>
		public bool init()
		{	// Load configuration
			///////////////////////////////////////////////
			//Load our server config
            Log.write(TLog.Normal, "Loading Server Configuration");
			_config = new Xmlconfig("server.xml", false).Settings;

			//Load our zone config
            Log.write(TLog.Normal, "Loading Zone Configuration");

			string filePath = AssetFileFactory.findAssetFile(_config["server/zoneConfig"].Value, "assets\\");
			if (filePath == null)
			{
				Log.write(TLog.Error, "Unable to find config file.");
				return false;
			}

			_zoneConfig = CfgInfo.Load(filePath);

			//Load assets from zone config and populate AssMan
			try
			{
				_assets = new AssetManager();
				if (!_assets.load(_zoneConfig, _config["server/zoneConfig"].Value))
				{	//We're unable to continue
					Log.write(TLog.Error, "Files missing, unable to continue.");
					return false;
				}
			}
			catch (System.IO.FileNotFoundException ex)
			{	//Report and abort
				Log.write(TLog.Error, "Unable to find file '{0}'", ex.FileName);
				return false;
			}

			//Make sure our protocol helpers are aware
			Helpers._server = this;

			//Load protocol config settings
			base._bLogPackets = _config["server/logPackets"].boolValue;
			Client.udpMaxSize = _config["protocol/udpMaxSize"].intValue;
			Client.crcLength = _config["protocol/crcLength"].intValue;
			if (Client.crcLength > 4)
			{
				Log.write(TLog.Error, "Invalid protocol/crcLength, must be less than 4.");
				return false;
			}

			Client.reliableJuggle = _config["protocol/reliableJuggle"].intValue;
			Client.reliableGrace = _config["protocol/reliableGrace"].intValue;
			Client.connectionTimeout = _config["protocol/connectionTimeout"].intValue;

			ClientConn<Database>.clientPingFreq = _config["protocol/clientPingFreq"].intValue;


			// Load scripts
			///////////////////////////////////////////////
			Log.write("Loading scripts..");

			//Obtain the bot and operation types
			ConfigSetting scriptConfig = new Xmlconfig("scripts.xml", false).Settings;
			IList<ConfigSetting> scripts = scriptConfig["scripts"].GetNamedChildren("type");

			//Load the bot types
			List<Scripting.InvokerType> scriptingBotTypes = new List<Scripting.InvokerType>();

			foreach (ConfigSetting cs in scripts)
			{	//Convert the config entry to a bottype structure
				scriptingBotTypes.Add(
					new Scripting.InvokerType(
							cs.Value,
							cs["inheritDefaultScripts"].boolValue,
							cs["scriptDir"].Value)
				);
			}

			//Load them into the scripting engine
			Scripting.Scripts.loadBotTypes(scriptingBotTypes); 

			try
			{	//Loads!
				bool bSuccess = Scripting.Scripts.compileScripts();
				if (!bSuccess)
				{	//Failed. Exit
					Log.write(TLog.Error, "Unable to load scripts.");
					return false;
				}
			}
			catch (Exception ex)
			{	//Error while compiling
				Log.write(TLog.Exception, "Exception while compiling scripts:\n" + ex.ToString());
				return false;
			}


			// Connect to the database
			///////////////////////////////////////////////
			//Attempt to connect to our database
			IPEndPoint dbLoc = new IPEndPoint(IPAddress.Parse(_config["database/ip"].Value), _config["database/port"].intValue);
			_db = new Database(this, _config["database"]);

			_bStandalone = !_db.connect(dbLoc, true);

			//Initialize other parts of the zoneserver class
			if (!initPlayers())
				return false;
			if (!initArenas())
				return false;

            // Create the ping/player count responder
            //////////////////////////////////////////////
		    _pingResponder = new ClientPingResponder(_players);

			return true;
		}

		/// <summary>
		/// Begins all server processes, and starts accepting clients.
		/// </summary>
		public void begin()
		{	//Start up the network
			_logger = Log.createClient("Zone");
			base._logger = Log.createClient("Network");

			IPEndPoint listenPoint = new IPEndPoint(
				IPAddress.Parse(_config["bindIP"].Value), _config["bindPort"].intValue);
			base.begin(listenPoint);

		    _pingResponder.Begin(new IPEndPoint(IPAddress.Parse(_config["bindIP"].Value), _config["bindPort"].intValue + 1));

			//Start handling our arenas);
			using (LogAssume.Assume(_logger))
				handleArenas();
		}
	}
}
