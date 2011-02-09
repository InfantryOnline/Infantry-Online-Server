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
			public long dbid;			//The player's id in the database
			public string alias;		//The player's alias
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
		/// Indicates that a player has joined the zone server
		/// </summary>
		public void newPlayer(int id, string alias, Data.DB.player dbplayer)
		{	//Add him to our player list
			Player player = new Player();

			player.dbid = dbplayer.id;
			player.alias = alias;

			_players[id] = player;
		}

		/// <summary>
		/// Indicates that a player has left the zone server
		/// </summary>
		public void lostPlayer(int id)
		{	//Attempt to remove him
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
