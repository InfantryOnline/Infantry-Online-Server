using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using InfServer.Network;
using InfServer.Protocol;


namespace InfServer.Game
{
	// ZoneServer Class
	/// Represents the entire server state
	///////////////////////////////////////////////////////
	public partial class ZoneServer : Server
	{	// Member variables
		///////////////////////////////////////////////////
		private Dictionary<ushort, Player> _players;		//The players in our zone, indexed by id
		private Dictionary<string, Player> _nameToPlayer;	//Player name lookup
		public ushort _lastPlayerKey;						//The last player key to be allocated

		//Settings
		static public int maxPlayers;



		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Performs player-related initialization
		/// </summary>
		public bool initPlayers()
		{	//Initialize
			_players = new Dictionary<ushort, Player>();
			_nameToPlayer = new Dictionary<string, Player>();
			_lastPlayerKey = 1;			//We don't want to be giving players an id of 0

			//Load some settings
			maxPlayers = _config["server/maxPlayers"].intValue;
			return true;
		}

		/// <summary>
		/// Allocates a new player slot for use for entering players
		/// </summary>
		public Player newPlayer(Client<Player> c, string alias)
		{
			using (LogAssume.Assume(_logger))
			{	//Is there any space?
				if (_players.Count == maxPlayers)
				{
					Log.write(TLog.Warning, "Server full.");
					return null;
				}

				//We want to continue wrapping around the playerid limits
				//looking for empty spots.
				ushort pk;

				for (pk = _lastPlayerKey; pk <= maxPlayers; ++pk)
				{	//If we've reached the maximum, wrap around
					if (pk == maxPlayers)
					{
						pk = 1;
						continue;
					}

					//Does such a player exist?
					if (_players.ContainsKey(pk))
						continue;

					//We have a space!
					break;
				}

				_lastPlayerKey = pk;

				//Create our new player object
				Player newPlayer = new Player();

				c._obj = newPlayer;
				newPlayer._client = c;

				newPlayer._id = pk;
				newPlayer._magic = _rand.Next();

				newPlayer._alias = alias;
				newPlayer._server = this;

				Log.write(TLog.Normal, "New player: " + alias);

				_players[pk] = newPlayer;
				_nameToPlayer[alias.ToLower()] = newPlayer;

				return newPlayer;
			}
		}

		/// <summary>
		/// Handles the loss of a player
		/// </summary>
		public void lostPlayer(Player player)
		{
			using (LogAssume.Assume(_logger))
			{	//Is it present in our list?
				if (!_players.ContainsKey(player._id))
				{	//Notify and discontinue
					Log.write(TLog.Error, "Lost a player which wasn't present in the ZoneServer list.");
					return;
				}
				
				//He's gone!
				_players.Remove(player._id);
				_nameToPlayer.Remove(player._alias);

				Log.write(TLog.Normal, "Lost player: " + player._alias);
	
				//Disconnect him from the server
				removeClient(player._client);

				//Make sure his stats get updated
				if (player._bDBLoaded && !player._server.IsStandalone)
					_db.updatePlayer(player);

				//We've lost him!
                if (!player._server.IsStandalone)
				_db.lostPlayer(player);
			}
		}

		/// <summary>
		/// Obtains the player associated with a given instance 
		/// </summary>
		public Player getPlayer(Data.PlayerInstance inst)
		{	//Dereference the id
			Player player;

			if (!_players.TryGetValue(inst.id, out player))
				//Doesn't exist!
				return null;

			return (player._magic == inst.magic) ? player : null;
		}

		/// <summary>
		/// Obtains the player associated with a given name 
		/// </summary>
		public Player getPlayer(string name)
		{	//Attempt to find him
			Player player;

            if (!_nameToPlayer.ContainsKey(name.ToLower()))
                return null;
            else
                player = _nameToPlayer[name.ToLower()];

			return player;
		}
	}
}
