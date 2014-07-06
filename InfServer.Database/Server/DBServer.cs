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
		public ConfigSetting _config;			                        //Our server config
		public new LogClient _logger;			                        //Our zone server log
        public SortedDictionary<string, Chat> _chats;
        public Dictionary<string, Zone.Player> _players;                //A list of every connected player
        public int playerPeak;

		public List<Zone> _zones;				                        //The zones currently connected

        public List<KeyValuePair<int, int>> _squadInvites;              //Our history of squad invites pair<squadid, playerid>

		private string _connectionString;		                        //The connectionstring to our database

		static public bool bAllowMulticlienting;                        //Should we allow players to join multiple times under the same account?


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
            _chats = new SortedDictionary<string, Chat>();
            _players = new Dictionary<string, Zone.Player>();
            _squadInvites = new List<KeyValuePair<int, int>>();
		}

        public bool newPlayer(Zone.Player player)
        {
            if (player == null)
            {
                Log.write(TLog.Error, "DBServer.newPlayer(): Called with null player.");
                return false;
            }
            if (String.IsNullOrWhiteSpace(player.alias))
            {
                Log.write(TLog.Error, "DBServer.newPlayer(): Player has no alias.");
                return false;
            }
            if (_players.ContainsValue(player))
            {
                Log.write(TLog.Error, "DBServer.newPlayer(): Player for '{0}' is already present.", player.alias);
                return false;
            }

            //Give it a go
            try
            {
                _players.Add(player.alias, player);
            }
            catch
            {
                Log.write(TLog.Error, "DBServer.newPlayer(): Key '{0}' already exists.", player.alias);
                return false;
            }

            if (_players.Count() > playerPeak)
                playerPeak = _players.Count();

            return true;
        }

        public void lostPlayer(Zone.Player player)
        {
            if (player == null)
            {
                Log.write(TLog.Error, "DBServer.lostPlayer(): Called with null player.");
                return;
            }

            //Remove him from any chats
            foreach (Chat c in _chats.Values.ToList())
            {
                if (c == null)
                    continue;

                if (c.hasPlayer(player))
                    c.lostPlayer(player);
            }

            //Remove him from the DB server master player list
            if (!_players.Remove(player.alias))
            {
                if (_players.ContainsValue(player))
                {
                    Log.write(TLog.Error, "Failed removing player '{0}' by name.", player.alias);
                }
                else
                {
                    Log.write(TLog.Error, "Lost player not in the list '{0}'.", player.alias);
                }

                return;
            }
        }

        public Chat getChat(string name)
        {
            Chat chat = _chats.Values.SingleOrDefault(c => c._name.ToLower() == name.ToLower());
            if (chat == null)
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
            //297 bytes is the maximum size of a chat message that can be sent without crashing the client
            int maxSize = 297;
            int size = message.Length;

            int idx = 0;

            //Make sure that the message won't crash our player!
            //TODO: FIXME: Less Gheto
            StringBuilder sb = new StringBuilder(maxSize);
            while (size - idx > 0)
            {
                SC_Chat<Zone> msg = new SC_Chat<Zone>();
                sb.Clear();

                while ((size - idx > 0) && (sb.Length < maxSize))
                {
                    sb.Append(message[idx]);
                    idx++;
                }

                msg.recipient = player;
                msg.message = sb.ToString();
                zone._client.send(msg);
            }
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
