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
	public class Script_ZombieZone : Scripts.IScript
	{	///////////////////////////////////////////////////
		// Member Variables
		///////////////////////////////////////////////////
		private Arena _arena;						//Pointer to our arena class
		private CfgInfo _config;					//The zone config

		private int _jackpot;						//The game's jackpot so far

		private Team _victoryTeam;					//The team currently winning!
		private Team _zombieHorde;					//The zombie horde teams

		private bool _bGameRunning;					//Is the game currently running?
		private int _lastGameCheck;					//The tick at which we last checked for game viability
		private int _tickGameStarting;				//The tick at which the game began starting (0 == not initiated)
		private int _tickGameStart;					//The tick at which the game started (0 == stopped)
		private int _tickGameLastTickerUpdate;		//The time at which we last updated the ticker display

		//Game state
		private int _initialZombieCount;			//The amount of zombies the game started with
		private int _tickLastZombieAdd;				//The tick at which we last added a zombie
		private List<ZombieBot> _zombies;			//The zombies currently present in the arena

		private Team _lastKilledTeam;				//The team which the last killed player belonged to

		//Constant Settings
		private int c_gameStartDelay = 15;			//Delay before a new game is started

		private short c_startAreaRadius = 300;		//The radius of the start area where a team spawns
		private int c_startSpacingRadius = 600;		//The distance between teams we should spawn

		private int c_minPlayers = 1;				//The minimum amount of players

		private int c_zombieAddTimer = 15;			//The amount of seconds between allowing new zombies
		private int c_zombieRespawnTime = 500;		//The amount of ms between spawning new zombies
		private int c_zombieMinRespawnDist = 500;	//The minimum distance zombies can be spawned from the players
		private int c_zombieMaxRespawnDist = 1500;	//The maximum distance zombies can be spawned from the players

		private int c_szombieMinRespawnDist = 3000;	//The minimum distance the super zombie can be spawned from the players
		private int c_szombieMaxRespawnDist = 6000;	//The maximum distance the super zombie can be spawned from the players


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

			if (_bGameRunning)
			{	//Should we update the zombie count ticket?
				if (now - _tickGameLastTickerUpdate > 5000)
				{
					updateGameTickers();
					_tickGameLastTickerUpdate = now;
				}

				//Are there any players left alive?
				bool bPlayersLeft = false;

				foreach (Player p in _arena.PlayersIngame)
					if (!p.IsSpectator && !p.IsDead && p._team._id != 0)
						bPlayersLeft = true;

				if (!bPlayersLeft)
					gameVictory(_lastKilledTeam);
			}

			return true;
		}

		#region Setup
		/// <summary>
		/// Sets up a player for a new game as a marine
		/// </summary>
		public void setupMarinePlayer(Player player)
		{	//Make him an infantryman!
			player.setDefaultVehicle(_arena._server._assets.getVehicleByID(10));

			//Give him some starting ammo!
			player.inventorySet(false, _arena._server._assets.getItemByName("Ammo"), 300);

			//Done, sync!
			player.syncInventory();
		}

		/// <summary>
		/// Spawns a player as a new zombie type
		/// </summary>
		public void spawnZombiePlayer(Player player)
		{	//Determine which team to attack
			Team target = null;
			int attackers = int.MaxValue;

			foreach (Team t in _arena.ActiveTeams)
				if (t.getVarInt("attackers") < attackers)
				{
					target = t;
					attackers = t.getVarInt("attackers");
				}

			//Make him a zombie
			player.setDefaultVehicle(_arena._server._assets.getVehicleByID(211));

			//Determine a place to spawn
			Helpers.ObjectState state = new Helpers.ObjectState();

			if (!findSpawnLocation(target, ref state, (short)c_zombieMinRespawnDist, (short)c_zombieMaxRespawnDist))
			{
				Log.write(TLog.Error, "Unable to find zombie spawn location.");
				return;
			}

			player.warp(Helpers.WarpMode.Normal, state, 0, -1, 0);
		}
		#endregion

		#region Bot Handling
		/// <summary>
		/// Spawns a super zombie on the map, to hunt down a certain team
		/// </summary>
		public void spawnSuperZombie(Team target)
		{	//Determine a place to spawn
			Helpers.ObjectState state = new Helpers.ObjectState();

			if (!findSpawnLocation(target, ref state, (short)c_zombieMinRespawnDist, (short)c_zombieMaxRespawnDist))
			{
				Log.write(TLog.Error, "Unable to find zombie spawn location.");
				return;
			}

			//Use an appropriate zombie vehicle
			VehInfo zombieVeh = _arena._server._assets.getVehicleByID(205);

			//Create our new zombie
			ZombieBot zombie = _arena.newBot(typeof(ZombieBot), zombieVeh, state, this) as ZombieBot;

			if (zombie == null)
			{
				Log.write(TLog.Error, "Unable to create zombie bot.");
				return;
			}

			zombie.targetTeam = target;
			target.setVar("superZombie", zombie);
		}

		/// <summary>
		/// Spawns a new zombie on the map
		/// </summary>
		public void spawnNewZombie()
		{	//Determine which team to attack
			Team target = null;
			int attackers = int.MaxValue;

			foreach (Team t in _arena.ActiveTeams)
				if (t.getVarInt("attackers") < attackers)
				{
					target = t;
					attackers = t.getVarInt("attackers");
				}

			//If we can't find a team to attack, nevermind
			if (target == null)
				return;

			//Determine a place to spawn
			Helpers.ObjectState state = new Helpers.ObjectState();

			if (!findSpawnLocation(target, ref state, (short)c_zombieMinRespawnDist, (short)c_zombieMaxRespawnDist))
			{
				Log.write(TLog.Error, "Unable to find zombie spawn location.");
				return;
			}
			
			//Use an appropriate zombie vehicle
			VehInfo zombieVeh = _arena._server._assets.getVehicleByID(211);

			//Create our new zombie
			ZombieBot zombie = _arena.newBot(typeof(ZombieBot), zombieVeh, state, this) as ZombieBot;

			if (zombie == null)
			{
				Log.write(TLog.Error, "Unable to create zombie bot.");
				return;
			}

			zombie.targetTeam = target;

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

			//Make sure each team has a super zombie
			foreach (Team team in _arena.Teams)
			{
				ZombieBot zombiebot = (ZombieBot)team.getVar("superZombie");
				if (team.ActivePlayerCount > 0 && zombiebot != null && zombiebot.bCondemned)
					spawnSuperZombie(team);
			}
		}
		#endregion

		#region Statistics
		/// <summary>
		/// Updates the game's ticker info
		/// </summary>
		public void updateGameTickers()
		{	//Create a list of the most successful players
			List<Player> activePlayers = _arena.PlayersIngame.ToList();
			if (activePlayers.Count == 0)
				return;

			activePlayers.OrderByDescending(player => player.getVarInt("zombieKills"));

			Player marine = activePlayers[0];
			int zombieKills = marine.getVarInt("zombieKills");

			if (zombieKills == 0)
				_arena.setTicker(1, 1, 0, "No zombie kills yet!");
			else
			{
				String best = String.Format("Best Marine: {0} (K={1})", marine._alias, zombieKills);

				_arena.setTicker(1, 1, 0,
					delegate(Player p)
					{
						return best + String.Format(" / (You K={0})", p.getVarInt("zombieKills"));
					}
				);
			}

			//Update the zombie count!
			_arena.setTicker(1, 2, 0, "Zombie Count: " + _zombies.Count);
		}
		#endregion

		#region Utility
		/// <summary>
		/// Obtains an appropriate spawn location for a zombie versus a team
		/// </summary>
		public bool findSpawnLocation(Team target, ref Helpers.ObjectState state, short spawnMinDist, short spawnMaxDist)
		{	//Find the average location of the team
			short posX, posY;

			averageTeamLocation(target, out posX, out posY);

			//Find an unblocked location around the team
			if (!findUnblockedLocation(ref posX, ref posY, spawnMinDist, spawnMaxDist))
				return false;

			state.positionX = posX;
			state.positionY = posY;

			return true;
		}

		/// <summary>
		/// Obtains the average position for an entire team
		/// </summary>
		public void averageTeamLocation(Team team, out short _posX, out short _posY)
		{	//Average the coordinates!
			int count = 0;
			int posX = 0;
			int posY = 0;

			foreach (Player p in team.ActivePlayers)
				if (!p.IsDead)
				{
					posX += p._state.positionX;
					posY += p._state.positionY;
					count++;
				}

			posX /= count;
			posY /= count;

			_posX = (short)posX;
			_posY = (short)posY;
		}

		/// <summary>
		/// Attempts to find an unblocked place on the map
		/// </summary>
		public bool findUnblockedLocation(ref short locX, ref short locY, short innerRadius, short outerRadius)
		{	//Generate a spawn location
			int attempts = 0;
			short mapWidth = (short)((_arena._server._assets.Level.Width - 1) * 16);
			short mapHeight = (short)((_arena._server._assets.Level.Height - 1) * 16);

			short posX, posY;

			while (attempts++ <= 200)
			{	//Generate some random coordinates
				posX = locX;
				posY = locY;

				Helpers.randomPositionInArea(_arena, ref posX, ref posY, outerRadius, outerRadius);

				//Within our inner radius?
				if (Math.Abs(posX - locX) < innerRadius || Math.Abs(posY - locY) < innerRadius)
					continue;

				//Is it blocked?
				if (_arena.getTile(posX, posY).Blocked)
					continue;

				//This will do!
				locX = posX;
				locY = posY;
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
					ZombieBot zombie = _arena.newBot(typeof(ZombieBot), zombieVeh, state, this) as ZombieBot;

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
		{	//Wut?
			if (killer == null)
				return true;

			//Increment the player's zombie kills
			killer.setVar("zombieKills", killer.getVarInt("zombieKills") + 1);

			//Make it known!
			_arena.triggerMessage(1, 500, killer._alias + " killed a " + dead._type.Name);
			return true;
		}

		/// <summary>
		/// Triggered when a player has died, by any means
		/// </summary>
		/// <remarks>killer may be null if it wasn't a player kill</remarks>
		[Scripts.Event("Player.Death")]
		public bool playerDeath(Player victim, Player killer, Helpers.KillType killType)
		{	//Was he a marine?
			if (victim._baseVehicle._type.Id < 100)
			{	//Make a note of his last team
				_lastKilledTeam = victim._team;
			}

			//Put him on zombie horde, make sure he's a zombie
			if (victim._team != _zombieHorde)
				_zombieHorde.addPlayer(victim);

			spawnZombiePlayer(victim);
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
				player.unspec(_zombieHorde);
				
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
			_tickGameLastTickerUpdate = 0;
			_bGameRunning = true;

			//Let everyone know
			_arena.sendArenaMessage("Game has started!", _config.flag.resetBong);

			//Put each player on the zombie horde onto public teams
			_zombieHorde = _arena.getTeamByName("Zombie Horde");
			List<Player> zombies = new List<Player>(_zombieHorde.ActivePlayers);

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
				setupMarinePlayer(marine);

			//Spawn our teams seperately
			foreach (Team team in _arena.Teams)
			{
				if (team.ActivePlayerCount == 0)
					continue;

				//Find a good location to spawn
				short pX = 0, pY = 0;

				if (!findUnblockedLocation(ref pX, ref pY, 0, short.MaxValue))
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

			//After spawning all the teams, spawn a superzombie for each team
			foreach (Team team in _arena.Teams)
				if (team.ActivePlayerCount > 0)
					spawnSuperZombie(team);

			//Calculate our initial zombie count
			_initialZombieCount = _arena.ActiveTeams.ToList().Count;

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

			//Show the breakdown!
			_arena.breakdown(true);

			//Reset all stats
			foreach (Player p in _arena.Players)
				p.resetVars();
			foreach (Team t in _arena.Teams)
				t.resetVars();

			return true;
		}

		/// <summary>
		/// Called when the statistical breakdown is displayed
		/// </summary>
		[Scripts.Event("Player.Breakdown")]
		public bool playerBreakdown(Player from, bool bCurrent)
		{	//Show some statistics!
			from.sendMessage(0, "#Individual Statistics Breakdown");

			IEnumerable<Player> rankedPlayers = _arena.PlayersIngame.OrderByDescending(player => player.getVarInt("zombieKills"));
			int idx = 3;	//Only display top three players

			foreach (Player p in rankedPlayers)
			{
				if (idx-- == 0)
					break;

				string format = "!3rd (K={0}): {1}";

				switch (idx)
				{
					case 2:
						format = "!1st (K={0}): {1}";
						break;
					case 1:
						format = "!2nd (K={0}): {1}";
						break;
				}

				from.sendMessage(0, String.Format(format,
					p.getVarInt("zombieKills"),
					p._alias));
			}

			//Don't show the typical breakdown
			return false;
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