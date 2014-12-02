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
		public Dictionary<string, Arena> _arenas;			//The arenas present in the zone, sorted by name

		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Performs arena-related initialization
		/// </summary>
		public bool initArenas()
		{	//Initialize variables
			_arenas = new Dictionary<string, Arena>(StringComparer.OrdinalIgnoreCase);

			//Gather config settings
			Arena.maxItems = _config["arena/maxArenaItems"].intValue;
			Arena.maxVehicles = _config["arena/maxArenaVehicles"].intValue;
			Arena.gameCheckInterval = _config["arena/gameCheckInterval"].intValue;
			Arena.routeRange = _config["arena/routing/routeRange"].intValue;
			Arena.routeWeaponRange = _config["arena/routing/routeWeaponRange"].intValue;
			Arena.routeRadarRange = _config["arena/routing/routeRadarRange"].intValue;
			Arena.routeRadarRangeFactor = _config["arena/routing/routeRadarRangeFactor"].intValue;
			Arena.routeRadarRangeFar = _config["arena/routing/routeRadarRangeFar"].intValue;
			Arena.routeRadarRangeFarFactor = _config["arena/routing/routeRadarRangeFarFactor"].intValue;

			if (Arena.maxVehicles > UInt16.MaxValue - ZoneServer.maxPlayers)
			{	//Complain
				Log.write(TLog.Error, "Invalid maxVehicles setting; cannot exceed {0}", UInt16.MaxValue - ZoneServer.maxPlayers);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Looks after the gamestate of all arenas
		/// </summary>
		private void handleArenas()
		{	//Get an image of the arena list
			int lastArenaUpdate = Environment.TickCount;
			List<Arena> arenas = new List<Arena>(_arenas.Values);

			while (run)
			{	//Is it time to update our list yet?
				if (Environment.TickCount - lastArenaUpdate > 1000)
				{	//Grab a list of arenas
					using (DdMonitor.Lock(_arenas))
						arenas = new List<Arena>(_arenas.Values);
					lastArenaUpdate = Environment.TickCount;
				}

				//Poll each arena!
				foreach (Arena arena in arenas)
				{
					try
					{
						if (arena._bActive)
							using (LogAssume.Assume(arena._logger))
								arena.poll();
					}
					catch (Exception ex)
					{
						Log.write(TLog.Exception, "Exception whilst polling arena {0}:\r\n{1}", arena._name, ex);
					}
				}

                //Poll our base zoneserver
                try
                {
                    this.poll();
                }
                catch (Exception ex)
                {
                    Log.write(TLog.Exception, "Exception whilst polling baseserver: \r\n{0}", ex);
                }

                // Sleep for a bit
				Thread.Sleep(5);
			}
		}

		/// <summary>
		/// Creates a new, appropriate arena
		/// </summary>
		public Arena newArena(string name)
		{	//Are we going to make a new public arena?
			if (name == "")
			{	//Yes, we need to find the lowest unused public name
				int idx = 1;

				while (_arenas.Keys.Contains("Public" + idx))
					idx++;

				//Got one!
				name = "Public" + idx;
			}

			//Is this a registered arena name?
			string invokerType = _config["server/gameType"].Value;
			IList<ConfigSetting> namedArenas = _config["arena"].GetNamedChildren("namedArena");

			foreach (ConfigSetting named in namedArenas)
			{	//Correct arena?
				if (name.Equals(named.Value, StringComparison.OrdinalIgnoreCase))
				{
					invokerType = named["gameType"].Value;
					break;
				}
			}

			//Instance our gametype
			if (!Scripting.Scripts.invokerTypeExists(invokerType))
			{
				Log.write(TLog.Error, "Unable to find gameType '{0}'", invokerType);
				return null;
			}

			//Populate the class
			Arena arena = new ScriptArena(this, invokerType);

			arena._bActive = true;
			arena._name = name;
            if (arena._name.Length >= 6 && arena._name.Substring(0, 6).Equals("Public"))
                arena._bIsPublic = true;
            else
                arena._bIsPublic = false;

			arena._logger = Log.createClient("a_" + name);

			arena.Close += lostArena;

			arena.init();
			using (DdMonitor.Lock(_arenas))
				_arenas.Add(name, arena);
            
			Log.write(TLog.Normal, "Opened arena: " + name);

			return arena;
		}

		/// <summary>
		/// Handles the loss of an arena
		/// </summary>
		public void lostArena(Arena arena)
		{	//What a shame!
			arena._bActive = false;

			using (DdMonitor.Lock(_arenas))
				_arenas.Remove(arena._name);

			Log.write(TLog.Normal, "Closed arena: " + arena._name);
		}

		/// <summary>
		/// Finds the most suitable arena for a player to join
		/// </summary>
		public Arena allocatePlayer(Player player)
		{
            int maxPlayers = _zoneConfig.arena.maxPlayers;
            int playingDesired = _zoneConfig.arena.playingDesired;

            foreach (KeyValuePair<string, Arena> a in _arenas)
            {
                if (a.Key.StartsWith("Public", StringComparison.OrdinalIgnoreCase))
                {   //Is there space?
                    if (a.Value.PlayerCount < playingDesired && a.Value.TotalPlayerCount < maxPlayers)
                        return a.Value;
                }
            }
            
            //Make a new one
            return newArena("");
		}

		/// <summary>
		/// Determines, given a specific arena request, which arena the player should join
		/// </summary>
        public Arena playerJoinArena(Player player, String arenaName)
        {
            if (player == null)
            {
                Log.write(TLog.Error, "playerJoinArena(): Called with null player.");
                return null;
            }

            if (String.IsNullOrWhiteSpace(arenaName))
            {
                Log.write(TLog.Error, "playerJoinArena(): Called with null / empty arena.");
                return null;
            }

            //Do we have such an arena?
            Arena arena = null;
            if (!_arenas.TryGetValue(arenaName, out arena))
            {	//Let's attempt to make it!
                //Is it a reserved public arena?
                if (arenaName.StartsWith("Public", StringComparison.OrdinalIgnoreCase))
                    //Can't do this I'm afraid
                    return null;

                //Create it!
                return newArena(arenaName);
            }

            //Are we banned from this arena?
            if (arena._blockedList.ContainsKey(player._alias))
            {
                TimeSpan check = DateTime.Now - (arena._blockedList.First(v => v.Key.Equals(player._alias)).Value);
                if (check.Minutes < DateTime.Now.Minute)
                {
                    player.sendMessage(-1, "You are banned from this arena for " + check.Minutes + " minutes.");
                    return null;
                }
                //Lets delete him from the list
                arena._blockedList.Remove(player._alias);
            }

            //Is this arena locked?
            if (arena._aLocked)
            {
                //Are we in the list?
                if (arena._bAllowed.Count == 0 || !arena._bAllowed.Contains(player._alias.ToLower()))
                {   //We a zone admin?
                    if (player.PermissionLevelLocal < Data.PlayerPermission.SMod)
                    {   //We a game mod?
                        if (player.PermissionLevel < Data.PlayerPermission.ArenaMod)
                        {
                            player.sendMessage(-1, "Arena is locked.");
                            return null;
                        }
                    }
                }
            }

            //Is it full?
            if (arena.TotalPlayerCount >= _zoneConfig.arena.maxPlayers)
            {
                player.sendMessage(-1, "Arena is full.");
                return null;
            }

            return arena;
        }
	}
}
