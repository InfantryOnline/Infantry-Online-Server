using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

using InfServer.Logic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;

using Assets;

namespace InfServer.Script.GameType_ZombieZone
{	// Script Class
	/// Provides the interface between the script and arena
	///////////////////////////////////////////////////////
	class Script_ZombieZone : Scripts.IScript
	{	///////////////////////////////////////////////////
		// Member Variables
		///////////////////////////////////////////////////
		private Arena _arena;					//Pointer to our arena class
		private CfgInfo _config;				//The zone config

		private int _jackpot;					//The game's jackpot so far

		private Team _victoryTeam;				//The team currently winning!

		private bool _bGameRunning;				//Is the game currently running?
		private int _lastGameCheck;				//The tick at which we last checked for game viability
		private int _tickGameStarting;			//The tick at which the game began starting (0 == not initiated)
		private int _tickGameStart;				//The tick at which the game started (0 == stopped)

		//Game state
		private int _initialZombieCount;		//The amount of zombies the game started with
		private int _tickLastZombieAdd;			//The tick at which we last added a zombie
		private List<ZombieBot> _zombies;		//The zombies currently present in the arena

		//Constant Settings
		private int c_gameStartDelay = 15;		//Delay before a new game is started

		private short c_startAreaRadius = 300;	//The radius of the start area where a team spawns
		private int c_startSpacingRadius = 600;	//The distance between teams we should spawn

		private int c_minPlayers = 1;			//The minimum amount of players

		private int c_zombieAddTimer = 15;		//The amount of seconds between allowing new zombies
		private int c_zombieRespawnTime = 500;	//The amount of ms between spawning new zombies
		private int c_zombieRespawnDist = 500;	//The distance we need to be from players to spawn
 

		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Performs script initialization
		/// </summary>
		public bool init(IEventObject invoker)
		{	//Populate our variables
			_arena = invoker as Arena;
			_config = _arena._server._zoneConfig;

			_zombies = new List<ZombieBot>();

			return true;
		}

		/// <summary>
		/// Allows the script to maintain itself
		/// </summary>
		public bool poll()
		{	//Maintain our arena if the game is running!
			int now = Environment.TickCount;

			if (_bGameRunning)
			{
				maintainZombies(now);
			}

			//Should we check game state yet?
			if (now - _lastGameCheck <= Arena.gameCheckInterval)
				return true;

			_lastGameCheck = now;

			//Do we have enough players ingame?
			int playing = _arena.PlayerCount;

			if ((_tickGameStart == 0 || (_tickGameStarting == 0 && _tickGameStart != -1)) && playing < c_minPlayers)
			{	//Stop the game!
				_arena.setTicker(1, 1, 0, "Not Enough Players");
				_arena.gameReset();

				_tickGameStart = -1;
			}
			//Do we have enough players to start a game?
			else if (_tickGameStart <= 0 && _tickGameStarting == 0 && playing >= c_minPlayers)
			{	//Great! Get going
				_tickGameStarting = now;
				_arena.setTicker(1, 1, c_gameStartDelay * 100, "Next game: ",
					delegate()
					{	//Trigger the game start
						_arena.gameStart();
					}
				);
			}

			return true;
		}

		#region Setup
		/// <summary>
		/// Sets up a player for a new game
		/// </summary>
		public void setupPlayer(Player player)
		{	//Make him an infantryman!
			player.setDefaultVehicle(_arena._server._assets.getVehicleByID(10));

			//Give him some starting ammo!
			player.inventorySet(false, _arena._server._assets.getItemByName("Ammo"), 300);

			//Done, sync!
			player.syncInventory();
		}

		#endregion

		#region Bot Handling
		/// <summary>
		/// Spawns a new zombie on the map
		/// </summary>
		public void spawnNewZombie()
		{	//Determine a place to spawn
			Helpers.ObjectState state = new Helpers.ObjectState();

			if (!findUnblockedLocation(out state.positionX, out state.positionY, true, c_zombieRespawnDist))
			{
				Log.write(TLog.Error, "Unable to find zombie spawn location.");
				return;
			}
			
			//Use an appropriate zombie vehicle
			VehInfo zombieVeh = _arena._server._assets.getVehicleByID(211);

			//Create our new zombie
			ZombieBot zombie = _arena.newBot(typeof(ZombieBot), zombieVeh, state, null) as ZombieBot;

			if (zombie == null)
			{
				Log.write(TLog.Error, "Unable to create zombie bot.");
				return;
			}

			//Great! Add it to our list
			zombie.Destroyed += delegate(Vehicle bot)
			{
				_zombies.Remove((ZombieBot)bot);
			};

			_zombies.Add(zombie);
		}

		/// <summary>
		/// Takes care of zombies, and spawns more if necessary
		/// </summary>
		public void maintainZombies(int now)
		{	//How many seconds has the game been running?
			int secondsElapsed = (now - _tickGameStart) / 1000;
			int zombiesAllowed = (secondsElapsed / c_zombieAddTimer) + _initialZombieCount;

			//Should we add more?
			if (zombiesAllowed > _zombies.Count && (now - _tickLastZombieAdd) > c_zombieRespawnTime)
			{
				_tickLastZombieAdd = now;
				spawnNewZombie();
			}
		}
		#endregion

		#region Utility
		/// <summary>
		/// Attempts to find an unblocked place on the map
		/// </summary>
		public bool findUnblockedLocation(out short locX, out short locY, bool bCheckPlayers, int radius)
		{	//Generate a spawn location
			int attempts = 0;
			short mapWidth = (short)((_arena._server._assets.Level.Width - 1) * 16);
			short mapHeight = (short)((_arena._server._assets.Level.Height - 1) * 16);
			
			locX = 0;
			locY = 0;

			while (attempts++ <= 200)
			{	//Generate some random coordinates
				Helpers.randomPositionInArea(_arena, ref locX, ref locY, mapWidth, mapHeight);

				//Is it blocked?
				if (_arena.getTile(locX, locY).Blocked)
					continue;

				//Check for players?
				if (bCheckPlayers && _arena.getPlayerCountInArea(locX - radius, locY - radius, locX + radius, locY + radius) > 0)
					continue;

				//This will do!
				return true;
			}

			//Failed?
			return false;
		}
		#endregion

		#region Events
		/// <summary>
		/// Called when a player sends a chat command
		/// </summary>
		[Scripts.Event("Player.ChatCommand")]
		public bool playerChatCommand(Player player, Player recipient, string command, string payload)
		{
			if (command.ToLower() == "spawn")
			{	//Spawn a zombie on him!
				for (int i = 0; i < 1; i++)
				{
					Helpers.ObjectState state = new Helpers.ObjectState();

					state.positionX = (short)(player._state.positionX + 100);
					state.positionY = (short)(player._state.positionY + 100);
					state.positionZ = 0;

					//Use an appropriate zombie vehicle
					VehInfo zombieVeh = _arena._server._assets.getVehicleByID(211);

					//Create our new zombie
					ZombieBot zombie = _arena.newBot(typeof(ZombieBot), zombieVeh, state, null) as ZombieBot;

					if (zombie == null)
					{
						Log.write(TLog.Error, "Unable to create zombie bot.");
						return true;
					}

					//Great! Add it to our list
					zombie.Destroyed += delegate(Vehicle bot)
					{
						_zombies.Remove((ZombieBot)bot);
					};

					_zombies.Add(zombie);
				}
			}

			return true;
		}

		/// <summary>
		/// Triggered when a vehicle dies
		/// </summary>
		[Scripts.Event("Bot.Death")]
		public bool botDeath(Bot dead, Player killer)
		{	//Make it known!
			_arena.triggerMessage(1, 500, killer._alias + " killed a " + dead._type.Name);
			return true;
		}

		/// <summary>
		/// Called when the specified team have won
		/// </summary>
		public void gameVictory(Team victors)
		{	//Stop the game
			_arena.gameEnd();
		}

		/// <summary>
		/// Triggered when a player wants to unspec and join the game
		/// </summary>
		[Scripts.Event("Player.JoinGame")]
		public bool playerJoinGame(Player player)
		{	//Is the game in progress?
			if (_bGameRunning)
			{	//Unspec him onto the zombie team
				player.unspec("Zombie Horde");
				
				player.sendMessage(-1, "&The game is already in progress so you have been spawned as a zombie.");
			}

			return true;
		}

		/// <summary>
		/// Triggered when a player wants to spec and leave the game
		/// </summary>
		[Scripts.Event("Player.LeaveGame")]
		public bool playerLeaveGame(Player player)
		{
			return true;
		}

		/// <summary>
		/// Called when the game begins
		/// </summary>
		[Scripts.Event("Game.Start")]
		public bool gameStart()
		{	//We've started!
			_tickGameStart = Environment.TickCount;
			_tickGameStarting = 0;
			_bGameRunning = true;

			//Let everyone know
			_arena.sendArenaMessage("Game has started!", _config.flag.resetBong);

			//Put each player on the zombie horde onto public teams
			Team zombieHorde = _arena.getTeamByName("Zombie Horde");
			List<Player> zombies = new List<Player>(zombieHorde.ActivePlayers);

			foreach (Player zombie in zombies)
			{	//Assign him a public team
				Team pick = _arena.pickAppropriateTeam(zombie);
				if (pick == null)
					//Wut? Put him in spec
					zombie.spec("spec");
				else
					pick.addPlayer(zombie);
			}

			//Equip everyone in the arena!
			foreach (Player marine in _arena.PlayersIngame)
				setupPlayer(marine);

			//Spawn our teams seperately
			foreach (Team team in _arena.Teams)
			{
				if (team.ActivePlayerCount == 0)
					continue;

				//Find a good location to spawn
				short pX, pY;

				if (!findUnblockedLocation(out pX, out pY, true, c_startSpacingRadius))
				{
					Log.write(TLog.Error, "Unable to find spawn location.");
					continue;
				}

				//Spawn each player around this point
				foreach (Player marine in team.ActivePlayers)
				{
					marine.warp(Helpers.WarpMode.Respawn, -1,
								(short)(pX - c_startAreaRadius), (short)(pY - c_startAreaRadius),
								(short)(pX + c_startAreaRadius), (short)(pY + c_startAreaRadius));
				}
			}

			//Calculate our initial zombie count
			_initialZombieCount = _arena.PlayerCount + 30;

			return true;
		}

		/// <summary>
		/// Called when the game ends
		/// </summary>
		[Scripts.Event("Game.End")]
		public bool gameEnd()
		{	//Game finished, perhaps start a new one
			_tickGameStart = 0;
			_tickGameStarting = 0;
			_victoryTeam = null;
			_bGameRunning = false;

			return true;
		}

		/// <summary>
		/// Called when the statistical breakdown is displayed
		/// </summary>
		[Scripts.Event("Game.Breakdown")]
		public bool breakdown()
		{	//Allows additional "custom" breakdown information


			//Always return true;
			return true;
		}

		/// <summary>
		/// Called to reset the game state
		/// </summary>
		[Scripts.Event("Game.Reset")]
		public bool gameReset()
		{	//Game reset, perhaps start a new one
			_tickGameStart = 0;
			_tickGameStarting = 0;

			_victoryTeam = null;

			return true;
		}
		#endregion
	}
}