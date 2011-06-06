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
		private Arena _arena;								//Pointer to our arena class
		private CfgInfo _config;							//The zone config
		private Random _rand;								//Our PRNG

		private ZombieZoneStats _stats;					//Just a class to look up ZZ stats

		private int _jackpot;								//The game's jackpot so far

		private Team _victoryTeam;							//The team currently winning!
		private Team _zombieHorde;							//The zombie horde teams

		private bool _bGameRunning;							//Is the game currently running?
		private int _lastGameCheck;							//The tick at which we last checked for game viability
		private int _tickGameStarting;						//The tick at which the game began starting (0 == not initiated)
		private int _tickGameStart;							//The tick at which the game started (0 == stopped)
		private int _tickGameLastTickerUpdate;				//The time at which we last updated the ticker display

		//Game state	
		private Dictionary<Team, TeamState> _states;		//The state for each team in the game

		private Player _lastKilledPlayer;					//The last killed player
		private Team _lastKilledTeam;						//The team which the last killed player belonged to

		//Constant Settings
		private const int c_gameStartDelay = 15;			//Delay before a new game is started

		private const short c_startAreaRadius = 160;		//The radius of the start area where a team spawns
		private const int c_startSpacingRadius = 600;		//The distance between teams we should spawn

		private const int c_minPlayers = 1;					//The minimum amount of players

		private const int c_supplyDropSpacing = 30;			//The seconds between using a supply drop and another spawning
		private const int c_supplyDropMinSpawnDist = 3000;	//The closest a supply drop can spawn to it's marine team
		private const int c_supplyDropMaxSpawnDist = 7000;	//The furthest a supply drop can spawn to it's marine team
		private const short c_supplyDropAreaRadius = 96;	//The size of the unblocked area a supply drop should seek

		private const float c_zombieInitialAmountPP = 0.5f;	//The amount of zombies per player initially spawned (minimum of 1)
		private const int c_zombieAddTimerAdjust = 7;		//The amount of seconds to add to the new zombie timer for each person missing from the team
		private const int c_zombieAddTimer = 32;			//The amount of seconds between allowing new zombies
		private const int c_zombieRespawnTime = 300;		//The amount of ms between spawning new zombies
		private const int c_zombieMinRespawnDist = 700;		//The minimum distance zombies can be spawned from the players
		private const int c_zombieMaxRespawnDist = 1500;	//The maximum distance zombies can be spawned from the players

		private const int c_szombieMinRespawnDist = 3000;	//The minimum distance the super zombie can be spawned from the players
		private const int c_szombieMaxRespawnDist = 6000;	//The maximum distance the super zombie can be spawned from the players

		
		///////////////////////////////////////////////////
		// Member Classes
		///////////////////////////////////////////////////
		/// <summary>
		/// Represents the zombies and spawning behavior for a certain team
		/// </summary>
		public class TeamState
		{
			public Team team;						//The team we represent

			public int zombieLimit;					//The amount of zombies after the team at once
			public int tickLastZombieCountIncrease;	//The last time at which we added a zombie

			public List<ZombieBot> zombies;			//The team's pursuing normal zombies
			public ZombieBot superZombie;			//The team's pursuing super zombie

			public bool bWaveForfilled;							//Has the transition wave been produced?
			public ZombieTransitions.ZombieTransition trans;	//The current transition
			public ZombieTransitions transitions;				//Used to determine the flow and zombie composition for the game

			public Computer supplyDrop;				//The team's active supply drop
			public int tickLastSupplyDropFinish;	//The time at which the team's last supply drop was used

			public int tickLastZombieAdd;			//The tick at which the last zombie was added
			public int zombieSpawnRate;				//The time between increases of the zombie limit

			public TeamState()
			{
				zombies = new List<ZombieBot>();
				transitions = new ZombieTransitions();
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
			_stats = new ZombieZoneStats();

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

				//Take care of each active team
				foreach (TeamState team in _states.Values)
				{	//Make sure it's active
					if (team.team.ActivePlayerCount == 0)
						continue;
					
					//Time to spawn a new supply depot?
					if (now - team.tickLastSupplyDropFinish > c_supplyDropSpacing * 1000)
					{
						spawnSupplyDrop(team.team, team);
						team.tickLastSupplyDropFinish = int.MaxValue;
					}
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
			spawnItemInArea(AssetManager.Manager.getItemByName("Chemist Kit"), 1, posX, posY, c_startAreaRadius);
			spawnItemInArea(AssetManager.Manager.getItemByName("Engineer Kit"), 1, posX, posY, c_startAreaRadius);
			spawnItemInArea(AssetManager.Manager.getItemByName("Heavy Marine Kit"), 1, posX, posY, c_startAreaRadius);
			spawnItemInArea(AssetManager.Manager.getItemByName("Squad Leader Kit"), 1, posX, posY, c_startAreaRadius);
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
		/// Called when a player attempts to open a supply drop
		/// </summary>
		public void openSupplyDrop(Computer supply, Player opener, VehInfo.ComputerProduct product)
		{	//Is he on the wrong team?
			if (supply._team != opener._team)
			{	//Is it not a hack action?
				if (product.Title != "Hack Supply Drop" || opener._baseVehicle._type.ClassId != 2)
					return;

				supply._team.sendArenaMessage(opener._alias + " has stolen your supply drop!", -1);
			}

			TeamState state = getTeamState(opener._team);
			Team team = opener._team;

			short posX = supply._state.positionX;
			short posY = supply._state.positionY;

			//Spawn some ammo around the site
			for (int i = 0; i < opener._team.ActivePlayerCount; ++i)
				spawnItemInArea(AssetManager.Manager.getItemByName("Ammo"), (ushort)_rand.Next(150, 225), posX, posY, c_supplyDropAreaRadius);

			spawnItemInArea(AssetManager.Manager.getItemByName("Frag Grenade"), (ushort)_rand.Next(1, 3), posX, posY, c_supplyDropAreaRadius);
			spawnItemInArea(AssetManager.Manager.getItemByName("Molotov Cocktail"), (ushort)_rand.Next(0, 2), posX, posY, c_supplyDropAreaRadius);
			spawnItemInArea(AssetManager.Manager.getItemByName("Repulsion Shield"), (ushort)_rand.Next(0, 2), posX, posY, c_supplyDropAreaRadius);
			spawnItemInArea(AssetManager.Manager.getItemByName("Energizer"), (ushort)_rand.Next(1, 2), posX, posY, c_supplyDropAreaRadius);
			spawnItemInArea(AssetManager.Manager.getItemByName("Energizer"), (ushort)_rand.Next(1, 2), posX, posY, c_supplyDropAreaRadius);

			//Do we have a chemist in the team?
			foreach (Player p in team.ActivePlayers)
			{
				if (p._baseVehicle._type.ClassId == 3 && !p.IsDead)
				{
					spawnItemInArea(AssetManager.Manager.getItemByName("Heal"), (ushort)(team.ActivePlayerCount - 1), posX, posY, c_supplyDropAreaRadius);
					break;
				}
			}

			//Heal every player slightly
			foreach (Player p in team.ActivePlayers)
				if (!p.IsDead)
					p.heal(AssetManager.Manager.getItemByName("SupplyDrop Healing") as ItemInfo.RepairItem, opener);

			//Let's have a weapon spawn!
			int selection = _rand.Next(0, 5);
			ItemInfo weaponPrize = null;

			switch (selection)
			{
				case 0:
					weaponPrize = AssetManager.Manager.getItemByName("Heavy Incinerator");
					break;
				case 1:
					weaponPrize = AssetManager.Manager.getItemByName("Machine Gun");
					break;
				case 3:
					weaponPrize = AssetManager.Manager.getItemByName("Railgun");
					break;
				case 4:
					weaponPrize = AssetManager.Manager.getItemByName("Thermal Lance");
					break;
			}

			if (weaponPrize != null)
				spawnItemInArea(weaponPrize, (ushort)(-weaponPrize.maxAllowed), posX, posY, c_supplyDropAreaRadius);

			//Remove the supply drop!
			state.tickLastSupplyDropFinish = Environment.TickCount;
			state.supplyDrop = null;

			supply.destroy(false);
		}

		/// <summary>
		/// Creates a random supply drop for a team of marines
		/// </summary>
		public void spawnSupplyDrop(Team team, TeamState state)
		{	//Find an open unblocked space in the map
			short posX, posY;

			if (team.ActivePlayerCount == 0)
			{
				Log.write(TLog.Error, "Attempted to spawn supply drop for team with no players.");
				return;
			}

			averageTeamLocation(team, out posX, out posY);

			//Find a clear location
			if (!_arena.getUnblockedTileInRadius(ref posX, ref posY, c_supplyDropMinSpawnDist, c_supplyDropMaxSpawnDist, c_supplyDropAreaRadius))
			{
				Log.write(TLog.Error, "Unable to find a spot to spawn a supply drop.");
				return;
			}

			//Create the vehicle!
			VehInfo supplyVehicle = AssetManager.Manager.getVehicleByID(412);
			Helpers.ObjectState objState = new Helpers.ObjectState();

			objState.positionX = posX;
			objState.positionY = posY;

			state.supplyDrop = _arena.newVehicle(supplyVehicle, team, null, objState) as Computer;
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
		public void spawnNewZombie(Team target, ZombieType ztype)
		{	//Determine a place to spawn
			Helpers.ObjectState state = new Helpers.ObjectState();

			if (!findSpawnLocation(target, ref state, (short)c_zombieMinRespawnDist, (short)c_zombieMaxRespawnDist))
			{
				Log.write(TLog.Error, "Unable to find zombie spawn location.");
				return;
			}

			//Find the team state
			TeamState team = getTeamState(target);

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
		{	//Maintain the zombie population for each team!
			foreach (Team team in _arena.Teams)
			{	//Ignore the zombie horde and teams with no active players
				if (team._id == 0 || team.ActivePlayerCount == 0)
					continue;

				TeamState state = getTeamState(team);

				//Do we need to make a new transition?
				ZombieTransitions.ZombieTransition trans = state.transitions.getNewTransition(now);

				if (trans != null)
				{	//Set it!
					state.trans = trans;
					state.bWaveForfilled = false;
				}

				//Is it a wave?
				if (state.trans.bWave && !state.bWaveForfilled)
				{	//Are we under the correct threshold?
					if (state.trans.zombieWaveThreshold <= state.zombies.Count)
					{	//Spawn zombies!
						state.bWaveForfilled = true;
						state.trans.thresholdReached(state.trans);

						foreach (ZombieType zt in state.trans.types)
							for (int i = 0; i < zt.spawnWeight * team.ActivePlayerCount; ++i)
								spawnNewZombie(team, zt);
					}
				}
				else if (!state.trans.bWave)
				{	//Should we add more?
					if (Math.Floor(state.trans.zombieCountMod * state.zombieLimit) > state.zombies.Count && (now - state.tickLastZombieAdd) > c_zombieRespawnTime)
					{
						state.tickLastZombieAdd = now;
						spawnNewZombie(team, state.trans.getRandomType(_rand));
					}
				}

				//Increase our zombie limit if necessary
				if (!state.trans.bPauseZombieAdd && now > state.tickLastZombieCountIncrease + state.zombieSpawnRate)
				{
					state.zombieLimit++;
					state.tickLastZombieCountIncrease = now;
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

			//Inform them of their nearest supply drop!
			_arena.setTicker(1, 2, 0,
				delegate(Player p)
				{
					TeamState state = getTeamState(p._team);

					if (state.supplyDrop != null)
					{	//Find the player's distance to the drop
						int distance = (int)((p._state.position() - state.supplyDrop._state.position()).Length * 100);

						return String.Format("Supply Drop at {0} (Distance: {1})", state.supplyDrop._state.letterCoord(), distance / 16);
					}
					else
						return "No Supply Drop";
				}
			);

			//Update the zombie count!
			_arena.setTicker(1, 3, 0,
				delegate(Player p)
				{	//Get the zombie count for his team
					return String.Format("Zombie Count: {0}", getTeamState(p._team).zombies.Count);
				}
			);
		}
		#endregion

		#region Utility
		/// <summary>
		/// Obtains the zombie state for a specified team
		/// </summary>
		public TeamState getTeamState(Team target)
		{	//Get the state!
			TeamState team;
			if (!_states.TryGetValue(target, out team))
			{
				team = new TeamState();
				team.team = target;

				_states[target] = team;
			}

			return team;
		}

		/// <summary>
		/// Spawns the given item randomly in the specified area
		/// </summary>
		public void spawnItemInArea(ItemInfo item, ushort quantity, short x, short y, short radius)
		{	//Sanity
			if (quantity <= 0)
				return;

			//Find a position and spawn it!
			Helpers.randomPositionInArea(_arena, radius * 2, ref x, ref y);
			_arena.itemSpawn(item, quantity, x, y);
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
			if (!_arena.getUnblockedTileInRadius(ref posX, ref posY, spawnMinDist, spawnMaxDist))
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
		#endregion

		#region Events
		/// <summary>
		/// Handles a player's produce request
		/// </summary>
		[Scripts.Event("Player.Produce")]
		public bool playerProduce(Player player, Computer computer, VehInfo.Computer.ComputerProduct product)
		{	//Is it a supply drop?
			if (computer._type.Name == "Supply Drop")
			{
				openSupplyDrop(computer, player, product);
				return false;
			}

			return true;
		}

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

			//Calculate the reward
			int expReward = _stats.getZombieExp(dead._type.Id);

			foreach (Player p in killer._team.ActivePlayers)
			{
				p.Experience += expReward;
				p.syncState();
			}

			//Make it known!
			_arena.triggerMessage(2, 500, String.Format("{0} killed a {1}", killer._alias, dead._type.Name), killer._team);
			killer._team.triggerMessage(9, 500, String.Format("{0} killed a {1} (Exp={2})", killer._alias, dead._type.Name, expReward), killer);
			killer.triggerMessage(1, 500, String.Format("{0} killed a {1} (Exp={2})", killer._alias, dead._type.Name, expReward));
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

				_arena.triggerMessage(3, 1000, victim._alias + " has perished");
			}

			//Put him on zombie horde, make sure he's a zombie
			if (victim._team != _zombieHorde)
				_zombieHorde.addPlayer(victim);

			spawnZombiePlayer(victim);
			return false;
		}

		/// <summary>
		/// Triggered when a player requests to pick up an item
		/// </summary>
		[Scripts.Event("Player.ItemPickup")]
		public bool playerItemPickup(Player player, Arena.ItemDrop drop, ushort quantity)
		{	//Are they any kit items?
			if (drop.item.name == "Engineer Kit")
			{
				if (player.findSkill(18) != null)
					player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(18));
				else if (player.findSkill(17) != null)
					player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(17));
				else if (player.findSkill(16) != null)
					player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(16));
				else
					player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(15));
			}
			else if (drop.item.name == "Heavy Marine Kit")
			{
				if (player.findSkill(23) != null)
					player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(23));
				else if (player.findSkill(22) != null)
					player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(22));
				else if (player.findSkill(21) != null)
					player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(21));
				else
					player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(20));
			}
			else if (drop.item.name == "Chemist Kit")
			{	//Give him some heals, depending on the amount of teammates
				player.inventorySet(AssetManager.Manager.getItemByName("Heal"), player._team.ActivePlayerCount);

				if (player.findSkill(28) != null)
					player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(28));
				else if (player.findSkill(27) != null)
					player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(27));
				else if (player.findSkill(26) != null)
					player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(26));
				else
					player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(25));
			}
			else if (drop.item.name == "Squad Leader Kit")
			{
				if (player.findSkill(33) != null)
					player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(33));
				else if (player.findSkill(32) != null)
					player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(32));
				else if (player.findSkill(31) != null)
					player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(31));
				else
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
				if (team._id == 0 || team.ActivePlayerCount == 0)
					continue;

				//Get the team state
				TeamState state = getTeamState(team);

				//Calculate the zombie spawn rate
				state.zombieSpawnRate = c_zombieAddTimer + (c_zombieAddTimerAdjust * (team._info.maxPlayers - team.ActivePlayerCount));
				state.zombieSpawnRate *= 1000;

				state.zombieLimit = (int)Math.Ceiling(c_zombieInitialAmountPP * team.ActivePlayerCount);

				//Find a good location to spawn
				short pX = 0, pY = 0;

				if (!_arena.getUnblockedTileInRadius(ref pX, ref pY, 0, short.MaxValue, c_startAreaRadius))
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
				from.sendMessage(0, "#Individual Survival Breakdown");

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

					from.sendMessage(0, String.Format(format,
						(tickDeath - _arena._tickGameStarted) / (1000 * 60),
						((tickDeath - _arena._tickGameStarted) / (1000)) % 60,
						p._alias));
				}

				int selfDeath = from.getVarInt("tickDeath");
				if (selfDeath != 0)
				{
					from.sendMessage(0, String.Format("You ({0} minutes, {1} seconds)",
						(selfDeath - _arena._tickGameStarted) / (1000 * 60),
						((selfDeath - _arena._tickGameStarted) / (1000)) % 60));
				}
			}

			from.sendMessage(0, "#Individual Kills Breakdown");

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

				from.sendMessage(0, String.Format(format,
					p.getVarInt("zombieKills"),
					p._alias));
			}

			int selfKills = from.getVarInt("zombieKills");
			from.sendMessage(0, String.Format("You (K={0})", selfKills));

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