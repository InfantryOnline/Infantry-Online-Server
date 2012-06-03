using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Protocol;
using InfServer.Data;

namespace InfServer
{
	// Zone Class
	/// Represents a single connected zone server
	///////////////////////////////////////////////////////
	public class Zone : IClient
	{	// Member variables
		///////////////////////////////////////////////////
		public Client _client;								//Our connection to the zone server
		public DBServer _server;							//The server we work for!

		public Data.DB.zone _zone;							//Our zone database entry

		public Dictionary<int, Player> _players;			//The players present in our server


		///////////////////////////////////////////////////
		// Member Classes
		///////////////////////////////////////////////////
		/// <summary>
		/// Represents a player present on the zone server
		/// </summary>
		public class Player
		{
			public long acctid;			        //The player's account id
			public long aliasid;		        //The player's alias id
			public long dbid;			        //The player's id in the database
			public string alias;		        //The player's alias
            public Zone zone;                   //The zone he's in.
            public string arena;                //The arena he's in.
            public int permission;              //His permission level.
		}

		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Generic constructor
		/// </summary>
		public Zone(Client client, DBServer server, Data.DB.zone zone)
		{
			_client = client;
			_server = server;
			_zone = zone;

			_players = new Dictionary<int, Player>();
		}

		#region State
		/// <summary>
		/// The zone is being destroyed, clean up assets
		/// </summary>
		public void destroy()
		{
            using(InfantryDataContext db = _server.getContext())
                db.ExecuteCommand("UPDATE zone SET active={0} WHERE id={1}", 0, _zone.id);
            

            _server._zones.Remove(this);
            Log.write("{0} disconnected gracefully", _zone.name);
		}

		/// <summary>
		/// Retrieves a player with the given id
		/// </summary>
		public Player getPlayer(int id)
		{	//Attempt to get him..
			Player player;

			if (!_players.TryGetValue(id, out player))
				return null;
			return player;
		}

        /// <summary>
        /// Retrieves a player with the given alias
        /// </summary>
        public Player getPlayer(string alias)
        {

            foreach (KeyValuePair<int, Player> p in _players)
            {
                if (p.Value.alias.ToLower() == alias.ToLower())
                return p.Value;
            }

            return null;
        }

		/// <summary>
		/// Does the zone have a player online under the given account?
		/// </summary>
		public bool hasAccountPlayer(long id)
		{
			return _players.Values.Any(p => p.acctid == id);
		}

		/// <summary>
		/// Does the zone have a given player alias online?
		/// </summary>
		public bool hasAliasPlayer(long id)
		{
			return _players.Values.Any(p => p.aliasid == id);
		}

        /// <summary>
        /// Does the zone have a given player alias online?
        /// </summary>
        public bool hasAliasPlayer(string alias)
        {
            return _players.Values.Any(p => p.alias.ToLower() == alias.ToLower());
        }

		/// <summary>
		/// Indicates that a player has joined the zone server
		/// </summary>
		public void newPlayer(int id, string alias, Data.DB.player dbplayer)
		{	//Add him to our player list
			Player player = new Player();

			player.acctid = dbplayer.alias1.account1.id;
			player.aliasid = dbplayer.alias1.id;
			player.dbid = dbplayer.id;
            player.alias = alias;
            player.permission = dbplayer.permission;
            player.zone = this;
            player.arena = ""; //TODO: ?
           

			_players[id] = player;

            //Alert our boss!
            _server.newPlayer(player);
		}

		/// <summary>
		/// Indicates that a player has left the zone server
		/// </summary>
		public void lostPlayer(int id)
		{	//Attempt to remove him
            _server.lostPlayer(_players[id]);
            _players.Remove(id);
		}
		#endregion

		/// <summary>
		/// Gives a short summary of this player
		/// </summary>
		public override string ToString()
		{	//Return the player credentials
			return _zone.name;
		}
	}
}
