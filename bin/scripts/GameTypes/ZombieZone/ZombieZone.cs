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
		private Arena _arena;							//Pointer to our arena class
		private CfgInfo _config;						//The zone config
		private Random _rand;							//Our PRNG

		private int _jackpot;							//The game's jackpot so far

		private Team _victoryTeam;						//The team currently winning!
		private Team _zombieHorde;						//The zombie horde teams

		private bool _bGameRunning;						//Is the game currently running?
		private int _lastGameCheck;						//The tick at which we last checked for game viability
		private int _tickGameStarting;					//The tick at which the game began starting (0 == not initiated)
		private int _tickGameStart;						//The tick at which the game started (0 == stopped)
		private int _tickGameLastTickerUpdate;			//The time at which we last updated the ticker display

		//Game state
		private Dictionary<Team, TeamState> _states;	//The state for each team in the game

		private List<ZombieType> _zombieTypes;			//The amount of zombies types which should spawn
		private int _zombieTypeMaxWeight;				//The total of weighted values used for picking

		private Player _lastKilledPlayer;				//The last killed player
		private Team _lastKilledTeam;					//The team which the last killed player belonged to

		//Constant Settings
		private int c_gameStartDelay = 15;				//Delay before a new game is started

		private short c_startAreaRadius = 200;			//The radius of the start area where a team spawns
		private int c_startSpacingRadius = 600;			//The distance between teams we should spawn

		private int c_minPlayers = 1;					//The minimum amount of players

		private float c_zombieInitialAmountPP = 0.5f;	//The amount of zombies per player initially spawned (minimum of 1)
		private int c_zombieAddTimerAdjust = 7;			//The amount of seconds to add to the new zombie timer for each person missing from the team
		private int c_zombieAddTimer = 25;				//The amount of seconds between allowing new zombies
		private int c_zombieRespawnTime = 500;			//The amount of ms between spawning new zombies
		private int c_zombieMinRespawnDist = 700;		//The minimum distance zombies can be spawned from the players
		private int c_zombieMaxRespawnDist = 1800;		//The maximum distance zombies can be spawned from the players

		private int c_szombieMinRespawnDist = 3000;		//The minimum distance the super zombie can be spawned from the players
		private int c_szombieMaxRespawnDist = 6000;		//The maximum distance the super zombie can be spawned from the players


		///////////////////////////////////////////////////
		// Member Classes
		///////////////////////////////////////////////////
		/// <summary>
		/// Represents the zombies and spawning behavior for a certain team
		/// </summary>
		public class TeamState
		{
			public List<ZombieBot> zombies;			//The team's pursuing normal zombies
			public ZombieBot superZombie;			//The team's pursuing super zombie

			public int tickLastZombieAdd;			//The tick at which the last zombie was added
			public int initialZombies;				//The amount of zombies initially spawned
			public float zombieSpawnRate;			//The rate at which new zombies get added to the count (each second)

			public TeamState()
			{
				zombies = new List<ZombieBot>();
			}
		}

		/// <summary>
		/// Represents a type of zombie, including the amount it should spawn
		/// </summary>
		public class ZombieType
		{
			public int spawnWeight;
			public VehInfo vehicleType;
			public Type classType;

			public ZombieType(Type _classType, VehInfo _vehicleType, int _spawnWeight)
			{
				spawnWeight = _spawnWeight;
				vehicleType = _vehicleType;
				classType = _classType;
			}
		}

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
			_rand = new Random();

			_states = new Dictionary<Team, TeamState>();

			//Populate our zombie type structure
			_zombieTypes = new List<ZombieType>();

			_zombieTypes.Add(new ZombieType(typeof(ZombieBot), AssetManager.Manager.getVehicleByID(211), 5));
			_zombieTypes.Add(new ZombieType(typeof(SuicideZombieBot), AssetManager.Manager.getVehicleByID(109), 1));
			_zombieTypes.Add(new ZombieType(typeof(RangedZombieBot), AssetManager.Manager.getVehicleByID(108), 2));

			//Calculate the total spawn weight
			_zombieTypeMaxWeight = 0;
			foreach (ZombieType zt in _zombieTypes)
				_zombieTypeMaxWeight += zt.spawnWeight;

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
		/// Spawns equipment around the team's spawn point
		/// </summary>
		public void setupSpawnPoint(Team team, short posX, short posY)
		{	//Let's get some class kits down!
			short x = posX, y = posY;

			ItemInfo kit = AssetManager.Manager.getItemByName("Chemist Kit");
			Helpers.randomPositionInArea(_arena, c_startAreaRadius, ref x, ref y);

			_arena.itemSpawn(kit, 1, x, y);

			x = posX; 
			y = posY;

			kit = AssetManager.Manager.getItemByName("Engineer Kit");
			Helpers.randomPositionInArea(_arena, c_startAreaRadius, ref x, ref y);

			_arena.itemSpawn(kit, 1, x, y);

			x = posX;
			y = posY;

			kit = AssetManager.Manager.getItemByName("Heavy Marine Kit");
			Helpers.randomPositionInArea(_arena, c_startAreaRadius, ref x, ref y);

			_arena.itemSpawn(kit, 1, x, y);

			x = posX;
			y = posY;

			kit = AssetManager.Manager.getItemByName("Squad Leader Kit");
			Helpers.randomPositionInArea(_arena, c_startAreaRadius, ref x, ref y);

			_arena.itemSpawn(kit, 1, x, y);
		}

		/// <summary>
		/// Sets up a player for a new game as a marine
		/// </summary>
		public void setupMarinePlayer(Player player)
		{	//Make him an infantryman!
			player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(10));

			//Conviscate some items
			player.removeAllItemFromInventory(false, AssetManager.Manager.getItemByName("Heal").id);

			//Give him some starting ammo!
			ItemInfo ammoItem = AssetManager.Manager.getItemByName("Ammo");
			player.inventorySet(false, ammoItem, -ammoItem.maxAllowed);

			//Give him some consumables!
			player.inventorySet(false, AssetManager.Manager.getItemByName("Energizer"), 2);
			player.inventorySet(false, AssetManager.Manager.getItemByName("Frag Grenade"), 3);

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
				if (t._id != 0 && t.ActivePlayerCount > 0 && t.getVarInt("attackers") < attackers)
				{
					target = t;
					attackers = t.getVarInt("attackers");
				}

			//Make him a zombie
			player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(211));

			if (target != null)
			{	//Determine a place to spawn
				Helpers.ObjectState state = new Helpers.ObjectState();

				if (!findSpawnLocation(target, ref state, (short)c_zombieMinRespawnDist, (short)c_zombieMaxRespawnDist))
				{
					Log.write(TLog.Error, "Unable to find zombie spawn location.");
					return;
				}

				player.warp(Helpers.WarpMode.Respawn, state, 0, -1, 0);
			}
		}
		#endregion

		#region Bot Handling
		/// <summary>
		/// Spawns a super zombie on the map, to hunt down a certain team
		/// </summary>
		public void spawnSuperZombie(Team target)
		{	//Determine a place to spawn
			Helpers.ObjectState state = new Helpers.ObjectState();

			if (!findSpawnLocation(target, ref state, (short)c_szombieMinRespawnDist, (short)c_szombieMaxRespawnDist))
			{
				Log.write(TLog.Error, "Unable to find zombie spawn location.");
				return;
			}

			//Find the team state
			TeamState team = getTeamState(target);

			//Use an appropriate zombie vehicle
			VehInfo zombieVeh = AssetManager.Manager.getVehicleByID(205);

			//Create our new zombie
			ZombieBot zombie = _arena.newBot(typeof(ZombieBot), zombieVeh, state, this) as ZombieBot;

			if (zombie == null)
			{
				Log.write(TLog.Error, "Unable to create zombie bot.");
				return;
			}

			zombie.targetTeam = target;
			team.superZombie = zombie;
		}

		/// <summary>
		/// Spawns a new zombie on the map
		/// </summary>
		public void spawnNewZombie(Team target)
		{	//Determine a place to spawn
			Helpers.ObjectState state = new Helpers.ObjectState();

			if (!findSpawnLocation(target, ref state, (short)c_zombieMinRespawnDist, (short)c_zombieMaxRespawnDist))
			{
				Log.write(TLog.Error, "Unable to find zombie spawn location.");
				return;
			}

			//Find the team state
			TeamState team = getTeamState(target);

			//Pick a random zombie type
			ZombieType ztype = pickZombieType(team);

			//Create our new zombie
			ZombieBot zombie = _arena.newBot(ztype.classType, ztype.vehicleType, state, this) as ZombieBot;

			if (zombie == null)
			{
				Log.write(TLog.Error, "Unable to create zombie bot.");
				return;
			}

			zombie.targetTeam = target;

			//Great! Add it to our list
			zombie.Destroyed += delegate(Vehicle bot)
			{
				team.zombies.Remove((ZombieBot)bot);
			};

			team.zombies.Add(zombie);
		}

		/// <summary>
		/// Takes care of zombies, and spawns more if necessary
		/// </summary>
		public void maintainZombies(int now)
		{	//How many seconds has the game been running?
			int secondsElapsed = (now - _tickGameStart) / 1000;

			//Maintain the zombie population for each team!
			foreach (Team team in _arena.Teams)
			{	//Ignore the zombie horde and teams with no active players
				if (team._id == 0 || team.ActivePlayerCount == 0)
					continue;

				//How many zombies should we have?
				TeamState state = getTeamState(team);
				int zombiesAllowed = (int)((state.zombieSpawnRate * secondsElapsed) + state.initialZombies);
			
				//Should we add more?
				if (zombiesAllowed > state.zombies.Count && (now - state.tickLastZombieAdd) > c_zombieRespawnTime)
				{
					state.tickLastZombieAdd = now;
					spawnNewZombie(team);
				}

				//Make sure each team has a super zombie
				if (team.ActivePlayerCount > 0 && state.superZombie != null && state.superZombie.bCondemned)
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
			_arena.setTicker(1, 2, 0,
				delegate(Player p)
				{	//Get the zombie count for his team
					return String.Format("Zombie Count: {0}", getTeamState(p._team).zombies.Count);
				}
			);
		}
		#endregion

		#region Utility
		/// <summary>
		/// Picks a zombie type appropriate to the given team
		/// </summary>
		public ZombieType pickZombieType(TeamState target)
		{	//Pick a random zombie type!
			double rand = _rand.NextDouble() * _zombieTypeMaxWeight;

			foreach (ZombieType zt in _zombieTypes)
			{
				rand -= zt.spawnWeight;
				if (rand <= 0)
					return zt;
			}

			return null;
		}

		/// <summary>
		/// Obtains the zombie state for a specified team
		/// </summary>
		public TeamState getTeamState(Team target)
		{	//Get the state!
			TeamState team;
			if (!_states.TryGetValue(target, out team))
			{
				team = new TeamState();
				_states[target] = team;
			}

			return team;
		}

		/// <summary>
		/// Obtains an appropriate spawn location for a zombie versus a team
		/// </summary>
		public bool findSpawnLocation(Team target, ref Helpers.ObjectState state, short spawnMinDist, short spawnMaxDist)
		{	//Find the average location of the team
			short posX, posY;

			if (target.ActivePlayerCount == 0)
			{
				Log.write(TLog.Error, "Attempted to find spawn location for team with no players.");
				return false;
			}

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
			short mapWidth = (short)((AssetManager.Manager.Level.Width - 1) * 16);
			short mapHeight = (short)((AssetManager.Manager.Level.Height - 1) * 16);

			short posX, posY;

			while (attempts++ <= 1000)
			{	//Generate some random coordinates
				posX = locX;
				posY = locY;

				Helpers.randomPositionInArea(_arena, ref posX, ref posY, outerRadius, outerRadius);

				//Within our inner radius?
				if (Math.Abs(posX - locX) < innerRadius && Math.Abs(posY - locY) < innerRadius)
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
					VehInfo zombieVeh = AssetManager.Manager.getVehicleByID(205);

					//Create our new zombie
					ZombieBot zombie = _arena.newBot(typeof(ZombieBot), zombieVeh, state, this) as ZombieBot;
				}
			}

			return true;
		}

		/// <summary>
		/// Triggered when a vehicle dies
		/// </summary>
		[Scripts.Event("Bot.Death")]
		public bool botDeath(Bot dead, Player killer)
		{	//Suicide bots?
			if (killer == null)
				return true;

			//Increment the player's zombie kills
			killer.setVar("zombieKills", killer.getVarInt("zombieKills") + 1);

			//Make it known!
			_arena.triggerMessage(2, 500, killer._alias + " killed a " + dead._type.Name, killer);
			killer.triggerMessage(1, 500, killer._alias + " killed a " + dead._type.Name);
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
				_lastKilledPlayer = victim;
				_lastKilledTeam = victim._team;

				//And the time he died
				victim.setVar("tickDeath", Environment.TickCount);
			}

			//Put him on zombie horde, make sure he's a zombie
			if (victim._team != _zombieHorde)
				_zombieHorde.addPlayer(victim);

			spawnZombiePlayer(victim);
			return true;
		}

		/// <summary>
		/// Triggered when a player requests to pick up an item
		/// </summary>
		[Scripts.Event("Player.ItemPickup")]
		public bool playerItemPickup(Player player, Arena.ItemDrop drop, ushort quantity)
		{	//Are they any kit items?
			if (drop.item.name == "Engineer Kit")
			{
				player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(15));
			}
			else if (drop.item.name == "Heavy Marine Kit")
			{
				player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(20));
			}
			else if (drop.item.name == "Chemist Kit")
			{	//Give him some heals, depending on the amount of teammates
				player.inventorySet(AssetManager.Manager.getItemByName("Heal"), player._team.ActivePlayerCount);

				player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(25));
			}
			else if (drop.item.name == "Squad Leader Kit")
			{
				player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(30));
			}

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

			_states.Clear();

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

				//Get the team state
				TeamState state = getTeamState(team);

				//Calculate the zombie spawn rate
				state.zombieSpawnRate = 1.0f;
				state.zombieSpawnRate /= c_zombieAddTimer + (c_zombieAddTimerAdjust * (team._info.maxPlayers - team.ActivePlayerCount));

				state.initialZombies = (int)Math.Ceiling(c_zombieInitialAmountPP * team.ActivePlayerCount);

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

				setupSpawnPoint(team, pX, pY);
			}

			//After spawning all the teams, spawn a superzombie for each team
			foreach (Team team in _arena.Teams)
				if (team.ActivePlayerCount > 0)
					spawnSuperZombie(team);

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

			//Congratulate the victor
			if (_lastKilledPlayer != null)
				_arena.sendArenaMessage(_lastKilledPlayer._alias + " was the last marine to survive!");

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
			IEnumerable<Player> rankedPlayers;
			int idx;

			if (_arena._tickGameEnded != 0)
			{
				from.sendMessage(-1, "#Individual Survival Breakdown");

				rankedPlayers = _arena.PlayersIngame.OrderByDescending(player => player.getVarInt("tickDeath"));
				idx = 3;	//Only display top three players

				foreach (Player p in rankedPlayers)
				{	//Has he survived at all?
					int tickDeath = p.getVarInt("tickDeath");
					if (tickDeath == 0)
						continue;

					if (idx-- == 0)
						break;

					//Set up the format
					string format = "!3rd ({0} minutes, {1} seconds): {2}";

					switch (idx)
					{
						case 2:
							format = "!1st ({0} minutes, {1} seconds): {2}";
							break;
						case 1:
							format = "!2nd ({0} minutes, {1} seconds): {2}";
							break;
					}

					from.sendMessage(-1, String.Format(format,
						(tickDeath - _arena._tickGameStarted) / (1000 * 60),
						((tickDeath - _arena._tickGameStarted) / (1000)) % 60,
						p._alias));
				}

				int selfDeath = from.getVarInt("tickDeath");
				if (selfDeath != 0)
				{
					from.sendMessage(-1, String.Format("You ({0} minutes, {1} seconds)",
						(selfDeath - _arena._tickGameStarted) / (1000 * 60),
						((selfDeath - _arena._tickGameStarted) / (1000)) % 60));
				}
			}

			from.sendMessage(-1, "#Individual Kills Breakdown");

			rankedPlayers = _arena.PlayersIngame.OrderByDescending(player => player.getVarInt("zombieKills"));
			idx = 3;	//Only display top three players

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

				from.sendMessage(-1, String.Format(format,
					p.getVarInt("zombieKills"),
					p._alias));
			}

			int selfKills = from.getVarInt("zombieKills");
			from.sendMessage(-1, String.Format("You (K={0})", selfKills));

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