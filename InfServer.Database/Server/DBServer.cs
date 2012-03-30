using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using InfServer.Network;
using InfServer.Protocol;
using InfServer.Data;

namespace InfServer
{
	// DBServer Class
	/// Represents the database server state
	///////////////////////////////////////////////////////
	public partial class DBServer : Server
	{	// Member variables
		///////////////////////////////////////////////////
		public ConfigSetting _config;			//Our server config
		public new LogClient _logger;			//Our zone server log
        public Dictionary<string, Chat> _chats;
        public Dictionary<string, Zone.Player> _players;      //A list of every connected player

		public List<Zone> _zones;				//The zones currently connected

		private string _connectionString;		//The connectionstring to our database

		static public bool bAllowMulticlienting;//Should we allow players to join multiple times under the same account?


		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Generic constructor
		/// </summary>
		public DBServer()
			: base(new C2SPacketFactory<Zone>(), new Client<Zone>(false))
		{
			_config = ConfigSetting.Blank;
			_zones = new List<Zone>();
            _chats = new Dictionary<string, Chat>();
            _players = new Dictionary<string, Zone.Player>();
		}

        public void newPlayer(Zone.Player player)
        {
            if (_players.ContainsValue(player))
                return;

            _players.Add(player.alias, player);
        }

        public void lostPlayer(Zone.Player player)
        {
            if (!_players.ContainsValue(player))
                return;
            _players.Remove(player.alias);
        }

        public Chat getChat(string name)
        {
            Chat chat;
            if (!_chats.TryGetValue(name, out chat))
                return null;
            return chat;
        }

        public Zone.Player getPlayer(string name)
        {
            Zone.Player player;
            if (!_players.TryGetValue(name, out player))
                return null;
            return player;
        }

        public void sendMessage(Zone zone, string player, string message)
        {
            SC_Chat<Zone> msg = new SC_Chat<Zone>();
            msg.message = message;
            msg.recipient = player;
            zone._client.send(msg);
            Log.write("sent db message");
        }

		/// <summary>
		/// Allows the server to preload all assets.
		/// </summary>
		public bool init()
		{	//Load our server config
			Log.write(TLog.Normal, "Loading Server Configuration");
			_config = new Xmlconfig("server.xml", false).Settings;

			//Load protocol config settings
			Client.udpMaxSize = _config["protocol/udpMaxSize"].intValue;
			Client.crcLength = _config["protocol/crcLength"].intValue;
			if (Client.crcLength > 4)
			{
				Log.write(TLog.Error, "Invalid protocol/crcLength, must be less than 4.");
				return false;
			}
			
			Client.connectionTimeout = _config["protocol/connectionTimeout"].intValue;

			bAllowMulticlienting = _config["allowMulticlienting"].boolValue;

			//Attempt to connect to our database
			_connectionString = _config["database/connectionString"].Value;

			//Does the database exist?
			using (InfantryDataContext db = getContext())
			{
				if (!db.DatabaseExists())
				{	//Create a new one
					Log.write(TLog.Warning, "Database layout doesn't exist, creating..");

					db.CreateDatabase();
				}
			}

			//We're good!
			Log.write("Connected to database.");
			return true;
		}

		/// <summary>
		/// Begins all server processes, and starts accepting clients.
		/// </summary>
		public void begin()
		{	//Start up the network
			_logger = Log.createClient("DBServer");
			base._logger = Log.createClient("Network");

			IPEndPoint listenPoint = new IPEndPoint(
				IPAddress.Parse(_config["bindIP"].Value), _config["bindPort"].intValue);
			base.begin(listenPoint);
			
			while (true)
				Thread.Sleep(10);
		}

		/// <summary>
		/// Creates a new data context to connect to the database
		/// </summary>
		public InfantryDataContext getContext()
		{
			return new InfantryDataContext(_connectionString);
		}
	}
}
