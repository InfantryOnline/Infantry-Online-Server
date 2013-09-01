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

        //TODO: Get rid of this stupid thing!
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
			{
                //Is there any space?
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

				Log.write(TLog.Normal, "New player: " + newPlayer);

				_players[pk] = newPlayer;
				_nameToPlayer[alias.ToLower()] = newPlayer;

                //Lets setup the players silence list
                if (!_playerSilenced.ContainsKey(alias))
                    _playerSilenced.Add(alias, new Dictionary<int, DateTime>());
                else
                {
                    //Lets check his current silence time
                    int numberKey = newPlayer._server._playerSilenced[newPlayer._alias].Keys.Count;
                    int numberValue = newPlayer._server._playerSilenced[newPlayer._alias].Values.Count;
                    if (numberKey > 0)
                    {
                        foreach (int length in newPlayer._server._playerSilenced[newPlayer._alias].Keys)
                        {
                            //Lets find the last one then set it
                            newPlayer._lengthOfSilence = length;
                            newPlayer._bSilenced = true;
                        }
                    }
                    if (numberValue > 0)
                    {
                        foreach (DateTime stamp in newPlayer._server._playerSilenced[newPlayer._alias].Values)
                            newPlayer._timeOfSilence = stamp;
                    }
                }

				return newPlayer;
			}
		}

		/// <summary>
		/// Handles the loss of a player
		/// </summary>
		public void lostPlayer(Player player)
		{
            if (player == null)
            {
                Log.write(TLog.Error, "lostPlayer(): Called with null player.");
                return;
            }
            // find users throttling logins
            // problem: users cannot leave zone and join the same one for 10 seconds
            // add: message informing user to wait so their client won't hang
            try
            {
                if (_connections != null && player._ipAddress != null)
                {
                    if (_connections.ContainsKey((IPAddress)player._ipAddress))
                    {
                        _connections.Remove(player._ipAddress);
                    }
                }
            }
            catch (Exception e)
            {
                Log.write(TLog.Error, e.ToString());
            }
			using (LogAssume.Assume(_logger))
			{	//Is it present in our list?
				if (!_players.ContainsKey(player._id))
				{	//Notify and discontinue
					Log.write(TLog.Error, "Lost player '{0}' who wasn't present in the ZoneServer list.", player);
					return;
				}
				
				//He's gone!
				_players.Remove(player._id);
				_nameToPlayer.Remove(player._alias);

				Log.write(TLog.Normal, "Lost player: " + player);
	
				//Disconnect him from the server
				removeClient(player._client);

                //Lets update the players silence list
                if ((_playerSilenced.ContainsKey(player._alias)) && _playerSilenced[player._alias].Keys.Count > 0)
                {
                    int min = 0; DateTime time = DateTime.Now;
                    foreach (int length in _playerSilenced[player._alias].Keys)
                        min = length;
                    if (_playerSilenced[player._alias].Values.Count > 0)
                        foreach (DateTime timer in _playerSilenced[player._alias].Values)
                            time = timer;
                    _playerSilenced.Remove(player._alias);
                    _playerSilenced.Add(player._alias, new Dictionary<int, DateTime>());
                    _playerSilenced[player._alias].Add(min, time);
                }

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
