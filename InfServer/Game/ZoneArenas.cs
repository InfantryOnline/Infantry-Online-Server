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
			_arenas = new Dictionary<string, Arena>();

			//Gather config settings
			Arena.maxItems = _config["arena/maxArenaItems"].intValue;
			Arena.maxVehicles = _config["arena/maxArenaVehicles"].intValue;
			Arena.gameCheckInterval = _config["arena/gameCheckInterval"].intValue;

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

			while (true)
			{	//Is it time to update our list yet?
				if (Environment.TickCount - lastArenaUpdate > 1000)
				{	//Grab a list of arenas
					using (DdMonitor.Lock(_arenas))
						arenas = new List<Arena>(_arenas.Values);
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

			//Instance our default gametype
			string invokerType = _config["server/gameType"].Value;
			if (!Scripting.Scripts.invokerTypeExists(invokerType))
			{
				Log.write(TLog.Error, "Unable to find gameType '{0}'", invokerType);
				return null;
			}

			//Populate the class
			Arena arena = new ScriptArena(this, invokerType);

			arena._bActive = true;
			arena._name = name;
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
		{	//Let's just stick him in public1 for now
			Arena arena = null;
			if (!_arenas.TryGetValue("Public1", out arena))
				return newArena("");
			return arena;
		}
	}
}
