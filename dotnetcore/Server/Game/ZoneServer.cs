using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using InfServer.Logic.Events;

using InfServer.Network;
using InfServer.Protocol;
using InfServer.Data;

using Assets;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;

namespace InfServer.Game
{
    // ZoneServer Class
    /// Represents the entire server state
    ///////////////////////////////////////////////////////
    public partial class ZoneServer : Server
    {	// Member variables
        ///////////////////////////////////////////////////
        public bool run = true;
        public ConfigSetting _config;			//Our server config
        public CfgInfo _zoneConfig;				//The zone-specific configuration file

        public AssetManager _assets;
        public Bots.IPathfinder _pathfinder;		//Global pathfinder

        public Database _db;					//Our connection to the database
        public IPEndPoint _dbEP;

        public uint _reliableChecksum = 0;      //Reliable checksum value for this zone

        public new LogClient _logger;           //Our zone server log

        private bool _bStandalone;              //Are we in standalone mode?
        private int _bStandaloneMessage;        //When to send out a message

        private string _name;                   //The zones name
        private string _description;            //The zones description
        private bool _isAdvanced;               //Is the zone normal/advanced?
        private string _bindIP;                 //The IP the zone is binded to
        private int _bindPort;                  //The port the zone is binded to

        private LogClient _dbLogger;
        public int _lastDBAttempt;
        public int _attemptDelay;
        public int _recycleAttempt;
        public bool _recycling;
        public bool _shutdown;

        private ClientPingResponder _pingResponder;
        public Dictionary<IPAddress, DateTime> _connections;

        public Dictionary<IPAddress, Dictionary<int, DateTime>> _playerSilenced; //Self explanitory

        /// <summary>
        /// Compiled game events that have been pulled out of the zone's cfg file.
        /// </summary>
        public Dictionary<string, GameEvent> GameEvents;

        ///////////////////////////////////////////////////
        // Accessors
        ///////////////////////////////////////////////////
        /// <summary>
        /// Gets the name of the zone
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
        }

        /// <summary>
        /// Gets the description of the zone
        /// </summary>
        public string Description
        {
            get
            {
                return _description;
            }
        }

        /// <summary>
        /// Gets whether this zone is advanced
        /// </summary>
        public bool IsAdvanced
        {
            get
            {
                return _isAdvanced;
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
        }

        /// <summary>
        /// Gets the current IP this instance of the zoneserver is running on
        /// </summary>
        public string IP
        {
            get
            {
                return _bindIP;
            }
        }

        /// <summary>
        /// Gets the current port this instance of the zoneserver is binded to
        /// </summary>
        public int Port
        {
            get
            {
                return _bindPort;
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
            _connections = new Dictionary<IPAddress, DateTime>();
            Log.write(TLog.Normal, "Loading Server Configuration");
            _config = new Xmlconfig("server.xml", false).Settings;

            string assetsPath = $"assets{Path.DirectorySeparatorChar}";

            //Load our zone config
            Log.write(TLog.Normal, "Loading Zone Configuration");

            if (!System.IO.Directory.Exists(assetsPath))
            {
                Log.write(TLog.Error, "Unable to find assets directory '" + assetsPath + "'.");
                return false;
            }

            string filePath = AssetFileFactory.findAssetFile(_config["server/zoneConfig"].Value, assetsPath);
            if (filePath == null)
            {
                Log.write(TLog.Error, "Unable to find config file '" + assetsPath + _config["server/zoneConfig"].Value + "'.");
                return false;
            }

            _zoneConfig = CfgInfo.Load(filePath);

            //Load assets from zone config and populate AssMan
            try
            {
                _assets = new AssetManager();

                _assets.bUseBlobs = _config["server/loadBlobs"].boolValue;

                //Grab the latest global news if specified
                if (_config["server/updateGlobalNws"].Value.Length > 0)
                {
                    Log.write(TLog.Normal, String.Format("Grabbing latest global news from {0}...", _config["server/updateGlobalNws"].Value));
                    if (!_assets.grabGlobalNews(_config["server/updateGlobalNws"].Value, $"..{Path.DirectorySeparatorChar}Global{Path.DirectorySeparatorChar}global.nws"))
                    {
                        try
                        {
                            string global;
                            if ((global = Assets.AssetFileFactory.findAssetFile("global.nws", _config["server/copyServerFrom"].Value)) != null)
                            {
                                //We first must delete before copying over
                                if (System.IO.File.Exists($"..{Path.DirectorySeparatorChar}Global{Path.DirectorySeparatorChar}global.nws"))
                                    System.IO.File.Delete($"..{Path.DirectorySeparatorChar}Global{Path.DirectorySeparatorChar}global.nws");

                                System.IO.File.Copy(global, $"..{Path.DirectorySeparatorChar}Global{Path.DirectorySeparatorChar}global.nws");
                            }
                        }
                        catch (Exception e)
                        {
                            Log.write(TLog.Warning, e.ToString());
                        }
                    }
                    else
                    {
                        //Copy over
                        if (System.IO.File.Exists(_config["server/copyServerFrom"].Value + $"{Path.DirectorySeparatorChar}global.nws"))
                            System.IO.File.Delete(_config["server/copyServerFrom"].Value + $"{Path.DirectorySeparatorChar}global.nws");
                        System.IO.File.Copy($"..{Path.DirectorySeparatorChar}Global{Path.DirectorySeparatorChar}global.nws", _config["server/copyServerFrom"].Value + $"{Path.DirectorySeparatorChar}global.nws");
                    }
                }

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

            Client.connectionTimeout = _config["protocol/connectionTimeout"].intValue;
            Client.bLogUnknowns = _config["protocol/logUnknownPackets"].boolValue;

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
                }
            }
            catch (Exception ex)
            {	//Error while compiling
                Log.write(TLog.Exception, "Exception while compiling scripts:\n" + ex.ToString());
            }

            if (_config["server/pathFindingEnabled"].boolValue)
            {
                Log.write("Initializing pathfinder..");
                LogClient log = Log.createClient("Pathfinder");
                _pathfinder = new Bots.Pathfinder(this, log);
                _pathfinder.beginThread();
            }
            else if(_config["server/pathFindingEnabledNew"].boolValue)
            {
                Log.write("Initializing new pathfinder..");
                LogClient log = Log.createClient("Pathfinder2");
                _pathfinder = new Bots.Pathfinder2(this, log);
                _pathfinder.beginThread();
            }
            else
            {
                Log.write("Pathfinder disabled, skipping..");
            }

            // Sets the zone settings
            //////////////////////////////////////////////
            _name = _config["server/zoneName"].Value;
            _description = _config["server/zoneDescription"].Value;
            _isAdvanced = _config["server/zoneIsAdvanced"].boolValue;
            _bindIP = _config["server/bindIP"].Value;
            _bindPort = _config["server/bindPort"].intValue;
            _attemptDelay = _config["database/connectionDelay"].intValue;

            Log.write("");
            Log.write("Server started..");
            // Connect to the database
            ///////////////////////////////////////////////
            //Attempt to connect to our database

            //Are we connecting at all?
            if (_attemptDelay == 0)
            {
                //Skip the database!
                _bStandalone = true;
                Log.write(TLog.Warning, "Skipping database server connection, server is in stand-alone mode..");
            }
            else
            {
                _bStandalone = false;
                _dbLogger = Log.createClient("Database");
                _db = new Database(this, _config["database"], _dbLogger);
                _dbEP = new IPEndPoint(IPAddress.Parse(_config["database/ip"].Value), _config["database/port"].intValue);

                _db.connect(_dbEP, true);

                if (_config.Exists("database2"))
                {
                    //var db2Ep = new IPEndPoint(IPAddress.Parse(_config["database2/ip"].Value), _config["database2/port"].intValue);
                    //dbConnection = new DB2.ClientConnection(db2Ep);
                    //Task.Run(async () => {
                    //    await dbConnection.ProcessAsync();
                    //});

                    //var data = ClientServerPacketFactory.CreateAuthenticateClient(new Random().Next(), _config["database2/password"].Value);
                    //dbConnection.Send(data);
                }
            }

            //Initialize other parts of the zoneserver class
            if (!initPlayers())
                return false;
            if (!initArenas())
                return false;

            // Create the ping/player count responder
            //////////////////////////////////////////////
            _pingResponder = new ClientPingResponder(_players);

            Log.write("Asset Checksum: " + _assets.checkSum());

            //Create a new player silenced list
            _playerSilenced = new Dictionary<IPAddress, Dictionary<int, DateTime>>();

            //InitializeGameEventsDictionary();

            return true;
        }

        /// <summary>
        /// Reloads all of our scripts
        /// </summary>
        public bool reloadScripts()
        {
            bool bSuccess = false;

            try
            {	//Loads!
                bSuccess = Scripting.Scripts.compileScripts();
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

            //If we've gotten this far, we've had success, lets tell each arena to reload its specified script
            foreach (Arena arena in _arenas.Values)
                arena.reloadScript();

            return bSuccess;
        }

        /// <summary>
        /// Begins all server processes, and starts accepting clients.
        /// </summary>
        public void begin()
        {	//Start up the network
            _logger = Log.createClient("ZoneServer");
            base._logger = Log.createClient("Network");

            IPEndPoint listenPoint = new IPEndPoint(
                IPAddress.Parse("0.0.0.0"), _bindPort);
            base.begin(listenPoint);

            _pingResponder.Begin(new IPEndPoint(IPAddress.Parse("0.0.0.0"), _bindPort + 1));

            //Start handling our arenas;
            using (LogAssume.Assume(_logger))
                handleArenas();
        }

        /// <summary>
        /// Handles all base server operations
        /// </summary>
        public void poll()
        {
            int now = Environment.TickCount;
            try
            {
                if (_connections != null)
                {
                    foreach (KeyValuePair<IPAddress, DateTime> pair in _connections.ToList())
                    {
                        if (DateTime.Now > pair.Value)
                        { // Delete this entry
                            _connections.Remove(pair.Key);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.write(TLog.Warning, e.ToString());
            }

            //Are we on an auto-recycle timer?
            if (_recycleAttempt > 0 && now > _recycleAttempt)
            {
                //Time to recycle
                recycle();
            }

            //Are we shutting down or recycling?
            if (!_shutdown || !_recycling)
            {
                //Is it time to make another attempt at connecting to the database?
                if (_lastDBAttempt != 0 && now - _lastDBAttempt > _attemptDelay)
                {
                    _lastDBAttempt = now;

                    //Are we bypassing our connection attempt?
                    if (_attemptDelay == 0)
                        return;

                    //Are we connected to the database currently?
                    if (!_db._bLoginSuccess || (_db._bLoginSuccess && !_db.IsConnected))
                    {
                        _bStandalone = true;
                        _db._bLoginSuccess = false;

                        //Take a stab at connecting
                        if (!_db.connect(_dbEP, true))
                        {
                            Log.write(TLog.Warning, "Failed database connection.");
                            _bStandalone = true;

                            //Send out a message to all of the server's players
                            if ((now - _bStandaloneMessage) > 1800000) //30 mins
                            {
                                _bStandaloneMessage = now;
                                foreach (var arena in _arenas)
                                    if (arena.Value._bActive)
                                        arena.Value.sendArenaMessage("!An attempt to establish a connection to the database failed. Server is in Stand Alone Mode.");
                            }
                        }
                    }
                    else
                    {
                        //Send a message to all of the server's players
                        if (_bStandalone)
                        {
                            foreach (var arena in _arenas)
                            {
                                if (arena.Value._bActive)
                                {
                                    arena.Value.sendArenaMessage("!Connection to the database has been re-established. Server is no longer in Stand Alone Mode.");
                                    arena.Value.sendArenaMessage("!Please relog so your stats will save again.");
                                }
                            }
                        }
                        _bStandalone = false;
                        _db._bLoginSuccess = true;
                    }
                }
            }
        }

        /// <sumary>
        /// Cleanup for shutdown or recycle
        /// </sumary>
        public void cleanup()
        {
            //Loop through each arena
            foreach (Arena arena in _arenas.Values.ToList())
            {
                foreach (Player p in arena.Players.ToList())
                {
                    if (p == null)
                        continue;

                    //Make sure his stats get updated
                    //NOTE: player client destroy will auto send a dc packet
                    p.destroy();
                }
            }

            if (!_bStandalone)
            {
                //If we are shutting down, update the zone
                if (_shutdown)
                {
                    CS_ZoneUpdate<Database> update = new CS_ZoneUpdate<Database>();
                    update.zoneName = _name;
                    update.zoneDescription = _description;
                    update.zoneIP = _bindIP;
                    update.zonePort = _bindPort;
                    update.zoneIsAdvanced = _isAdvanced;
                    update.zoneActive = 0; //Going Inactive
                    _db.send(update);
                }

                //Disconnect from the database gracefully..
                _db.send(new Disconnect<Database>());
            }

            //End all threads
            // FIXME: This throws an error because we aren't using cancellation tokens. need to clean this up.
            // _pingResponder.End();
            // base.end();

            //Add a little delay...
            Thread.Sleep(2000);
        }

        /// <summary>
        /// Recycles our zoneserver
        /// </summary>
        public void recycle()
        {
            Log.write("Recycling...");
            _recycling = true;

            //cleanup
            cleanup();

            //Restart!
            Program.Restart();
        }

        public void shutdown()
        {
            Log.write("Shutting down...");
            _shutdown = true;

            //cleanup
            cleanup();

            //Shutdown!
            Program.Stop();
        }

        private void InitializeGameEventsDictionary()
        {
            var e = _zoneConfig.EventInfo;
            GameEvents = new Dictionary<string, GameEvent>();
            //
            // Compile the event strings into game events/actions.
            //

            GameEvents["jointeam"] = EventsActionsFactory.CreateGameEventFromString(e.joinTeam);
            GameEvents["exitspectatormode"] = EventsActionsFactory.CreateGameEventFromString(e.exitSpectatorMode);
            GameEvents["endgame"] = EventsActionsFactory.CreateGameEventFromString(e.endGame);
            GameEvents["soongame"] = EventsActionsFactory.CreateGameEventFromString(e.soonGame);
            GameEvents["manualjointeam"] = EventsActionsFactory.CreateGameEventFromString(e.manualJoinTeam);
            GameEvents["startgame"] = EventsActionsFactory.CreateGameEventFromString(e.startGame);
            GameEvents["sysopwipe"] = EventsActionsFactory.CreateGameEventFromString(e.sysopWipe);
            GameEvents["selfwipe"] = EventsActionsFactory.CreateGameEventFromString(e.selfWipe);
            GameEvents["killedteam"] = EventsActionsFactory.CreateGameEventFromString(e.killedTeam);
            GameEvents["killedenemy"] = EventsActionsFactory.CreateGameEventFromString(e.killedEnemy);
            GameEvents["killedbyteam"] = EventsActionsFactory.CreateGameEventFromString(e.killedByTeam);
            GameEvents["killedbyenemy"] = EventsActionsFactory.CreateGameEventFromString(e.killedByEnemy);
            GameEvents["firsttimeinvsetup"] = EventsActionsFactory.CreateGameEventFromString(e.firstTimeInvSetup);
            GameEvents["firsttimeskillsetup"] = EventsActionsFactory.CreateGameEventFromString(e.firstTimeSkillSetup);
            GameEvents["hold1"] = EventsActionsFactory.CreateGameEventFromString(e.hold1);
            GameEvents["hold2"] = EventsActionsFactory.CreateGameEventFromString(e.hold2);
            GameEvents["hold3"] = EventsActionsFactory.CreateGameEventFromString(e.hold3);
            GameEvents["hold4"] = EventsActionsFactory.CreateGameEventFromString(e.hold4);
            GameEvents["enterspawnnoscore"] = EventsActionsFactory.CreateGameEventFromString(e.enterSpawnNoScore);
            GameEvents["changedefaultvehicle"] = EventsActionsFactory.CreateGameEventFromString(e.changeDefaultVehicle);
        }

        ///////////////////////////////////////////////////
        // Member Classes
        ///////////////////////////////////////////////////
        /// <summary>
        /// Responds to ping and player count requests made by the client. Runs on the port
        /// above the zone server.
        /// </summary>
        private class ClientPingResponder
        {
            private LogClient _pingLogger;
            private Dictionary<ushort, Player> _players;
            private Thread _listenThread;
            private Socket _socket;
            private Dictionary<EndPoint, Int32> _clients;
            private Boolean _isOperating;
            private ReaderWriterLock _lock;
            private byte[] _buffer;

            /// <summary>
            /// Constructor with socket creation
            /// </summary>
            public ClientPingResponder(Dictionary<ushort, Player> players)
            {
                _pingLogger = Log.createClient("PingResponder");
                _players = players;
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _clients = new Dictionary<EndPoint, Int32>();
                _lock = new ReaderWriterLock();
                _buffer = new byte[4];
            }

            /// <summary>
            /// Begins our threaded responder
            /// </summary>
            public void Begin(IPEndPoint listenPoint)
            {
                Log.assume(_pingLogger);

                _listenThread = new Thread(Listen);
                _listenThread.IsBackground = true;
                _listenThread.Name = "ClientPingResponder";
                _listenThread.Start(listenPoint);
                if (!_listenThread.IsAlive)
                    Log.write(TLog.Warning, "Failed to thread start the client ping responder.");
            }

            /// <summary>
            /// Ends our thread
            /// </summary>
            public void End()
            {
                if (_listenThread.IsAlive)
                {
                    _isOperating = false;
                }
            }

            private void Listen(Object obj)
            {
                var listenPoint = (IPEndPoint)obj;
                EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    //
                    // Prevent WSAECONNRESET. From MSDN:
                    //
                    // WSAECONNRESET
                    // The virtual circuit was reset by the remote side executing a hard or abortive close. The application should
                    // close the socket; it is no longer usable.On a UDP-datagram socket this error indicates a previous send
                    // operation resulted in an ICMP Port Unreachable message.
                    //

                    uint IOC_IN = 0x80000000;
                    uint IOC_VENDOR = 0x18000000;
                    uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
                    _socket.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);
                }

                _socket.Bind(listenPoint);
                _socket.BeginReceiveFrom(_buffer, 0, _buffer.Length, SocketFlags.None, ref remoteEP, OnRequestReceived, null);

                _isOperating = true;

                // Do we have clients to service?
                while (_isOperating)
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

                    if (queue != null && queue.Count != 0)
                    {
                        // May not be synchronized, but that's okay, the client requests often.
                        byte[] playerCount = BitConverter.GetBytes(_players.Count);

                        foreach (var entry in queue)
                        {
                            // TODO: Refactor this into something cultured
                            EndPoint client = entry.Key;
                            byte[] token = BitConverter.GetBytes(entry.Value);

                            byte[] buffer = new[]
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
                if (!result.IsCompleted)
                {
                    // Continue anyways? Let's do it!
                    //return;
                }

                EndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);
                int read = 4;
                try
                {
                    read = _socket.EndReceiveFrom(result, ref remoteEp);
                }
                catch (SocketException)
                {
                    //Packet is too big. Make note of it and the clients IP
                    Log.write("Malformed packet from client: " + remoteEp.ToString() + " (possible attempt to crash the zone)");
                }

                if (read != 4)
                {
                    // Malformed packet, lets continue anyways and log the scums IP
                    Log.write("Malformed packet from client: " + remoteEp.ToString() + " (possible attempt to crash the zone)");
                }

                _lock.AcquireWriterLock(Timeout.Infinite);

                try
                {
                    _clients[remoteEp] = BitConverter.ToInt32(_buffer, 0);
                }
                finally
                {
                    _lock.ReleaseWriterLock();
                }

                remoteEp = new IPEndPoint(IPAddress.Any, 0);
                _socket.BeginReceiveFrom(_buffer, 0, _buffer.Length, SocketFlags.None, ref remoteEp, OnRequestReceived, null);
            }
        }
    }
}