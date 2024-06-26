﻿using System.Collections.Generic;
using System.Linq;

using InfServer.Network;
using InfServer.Protocol;

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

        public InfServer.Database.zone _zone;							//Our zone database entry

        public Dictionary<int, Player> _players;			//The players present in our zone


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
            public string IPAddress;            //The player's ip address
            public bool stealth;
            public long dbid;			        //The player's id in the database
            public string alias;		        //The player's alias
            public Zone zone;                   //The zone he's in.
            public string arena;                //The arena he's in.
            public int permission;              //His permission level.
            public List<string> chats;          //The chats they are in
        }

        ///////////////////////////////////////////////////
        // Member Functions
        ///////////////////////////////////////////////////
        /// <summary>
        /// Generic constructor
        /// </summary>
        public Zone(Client client, DBServer server, InfServer.Database.zone zone)
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
            //Remove the players from the zone and chats.
            foreach (KeyValuePair<int, Player> p in this._players.ToList())
            {
                if (p.Value == null)
                    continue;

                lostPlayer(p.Key);
            }

            //Remove the zone from the list
            _server._zones.Remove(this);
        }

        /// <summary>
        /// Retrieves a player with the given id
        /// </summary>
        public Player getPlayer(int id)
        {	//Attempt to get him..
            if (id == 0)
            {
                Log.write(TLog.Warning, "Zone.getPlayer(): Attempted to find id 0.");
            }

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
            if (string.IsNullOrWhiteSpace(alias))
            {
                Log.write(TLog.Error, "Zone.getPlayer(): Called with no alias.");
                return null;
            }

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
        public bool newPlayer(int id, string alias, InfServer.Database.player dbplayer)
        {
            if (string.IsNullOrWhiteSpace(alias))
            {
                Log.write(TLog.Error, "Zone.newPlayer(): Called with no alias.");
                return false;
            }
            if (id == 0)
            {
                Log.write(TLog.Error, "Zone.newPlayer(): ID was 0 for player '{0}.", alias);
            }
            if (dbplayer == null)
            {
                Log.write(TLog.Error, "Zone.getPlayer(): Called with null dbplayer for '{0}'.", alias);
                return false;
            }

            Player player = new Player();

            player.acctid = dbplayer.alias1.account1.id;
            player.aliasid = dbplayer.alias1.id;
            player.IPAddress = dbplayer.alias1.IPAddress;
            player.dbid = dbplayer.id;
            player.alias = alias;
            player.stealth = dbplayer.alias1.stealth == 1;
            player.permission = dbplayer.permission;
            player.zone = this;
            player.arena = "";
            player.chats = new List<string>();

            // Add them to the Zone players list
            try
            {
                _players.Add(id, player);
            }
            catch
            {
                Log.write(TLog.Error, "Zone.newPlayer(): Key '{0}' already exists for player '{1}'.", id, alias);
                return false;
            }

            // And the global list
            if (!_server.newPlayer(player))
            {
                // Back out of the zone list
                _players.Remove(id);

                return false;
            }

            return true;
        }

        /// <summary>
        /// Indicates that a player has left the zone server
        /// </summary>
        public void lostPlayer(int id)
        {	//Attempt to remove him
            if (id == 0)
            {
                Log.write(TLog.Error, "Zone.lostPlayer(): ID was 0.");
            }

            //Try and get the player so we can pass it to the global list
            Player player = getPlayer(id);
            if (player == null)
            {
                Log.write(TLog.Error, "Zone.lostPlayer(): ID ({0}) not found in list.", id);
                return;
            }

            //Remove him from the zone player list
            if (!_players.Remove(id))
            {
                Log.write(TLog.Error, "Zone.lostPlayer(): Failed removing player ID ({0}).", id);
            }

            //Remove him from the base db list and chats
            _server.lostPlayer(player);
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