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
using Axiom.Math;

namespace InfServer.Script.GameType_ZombieZone
{	// Script Class
	/// Provides the interface between the script and arena
	///////////////////////////////////////////////////////
	public class Script_ZombieZone : Scripts.IScript
	{	///////////////////////////////////////////////////
		// Member Variables
		///////////////////////////////////////////////////
		private Arena _arena;									//Pointer to our arena class
		private CfgInfo _config;								//The zone config
		private Random _rand;									//Our PRNG
			
		private ZombieZoneStats _stats;							//Just a class to look up ZZ stats

		private int _jackpot;									//The game's jackpot so far

		private Team _victoryTeam;								//The team currently winning!
		private Team _zombieHorde;								//The zombie horde teams

		private bool _bGameRunning;								//Is the game currently running?
		private int _lastGameCheck;								//The tick at which we last checked for game viability
		private int _tickGameStarting;							//The tick at which the game began starting (0 == not initiated)
		private int _tickGameStart;								//The tick at which the game started (0 == stopped)
		private int _tickGameLastTickerUpdate;					//The time at which we last updated the ticker display

		//Game state	
		private Dictionary<Team, TeamState> _states;			//The state for each team in the game
		private int _marineTeamCount;							//The amount of marine teams participating

		//Constant Settings
		public const int c_gameStartDelay = 30;					//Delay before a new game is started

		public const short c_startAreaRadius = 112;				//The radius of the start area where a team spawns
		public const int c_startSpacingRadius = 2000;			//The distance between teams we should spawn

		public const int c_minPlayers = 1;						//The minimum amount of players

		public const int c_supplyDropSpacing = 30;				//The seconds between using a supply drop and another spawning
		public const int c_supplyDropMinSpawnDist = 2400;		//The closest a supply drop can spawn to it's marine team
		public const int c_supplyDropMaxSpawnDist = 5500;		//The furthest a supply drop can spawn from it's marine team
		public const short c_supplyDropAreaRadius = 112;		//The size of the unblocked area a supply drop should seek

		public const int c_ammoGenerateInterval = 20000;		//The time between ammo generations
		public const int c_ammoGenerateAmount = 80;				//The amount of ammo generated each time
		public const int c_ammoGeneratePerLevel = 20;			//The amount of extra ammo the generator generates per level
		public const int c_ammoGenerateDecay = 10;				//The rate at which the ammo generator health decays

		public const int c_teamCashStart = 48000;				//The amount of cash a team has to start, divided up between everyone

		public const int c_playerSupplyDropCash = 4000;			//The amount of cash gained by finding a supply drop
		public const int c_playerSupplyDropCashGrow = 500;		//The amount of extra cash given for each supply drop found
		public const int c_playerSupplyDropExp = 250;			//The amount of exp gained by finding a supply drop

		public const float c_zombieInitialAmountPP = 0.5f;		//The amount of zombies per player initially spawned (minimum of 1)
		public const int c_zombieAddTimerGrowth = 8;			//The amount of seconds to add to the new zombie timer for each person missing from the team
		public const int c_zombieAddTimer = 36;					//The amount of seconds between allowing new zombies
		public const int c_zombieRespawnTimeGrowth = 400;		//The amount of time to add to the respawn timer for each missing player
		public const int c_zombieRespawnTime = 600;				//The amount of ms between spawning new zombies
		public const int c_zombieMinRespawnDist = 900;			//The minimum distance zombies can be spawned from the players
		public const int c_zombieMaxRespawnDist = 1500;			//The maximum distance zombies can be spawned from the players
		public const int c_zombieMaxPath = 350;					//The maximum path length before a zombie will request a respawn
		public const int c_zombiePathUpdateInterval = 10000;	//The amount of ticks before a zombie will renew it's path
		public const int c_zombieDistanceLeeway = 300;			//The maximum distance leeway a zombie can be from the team before it is respawned
		public const int c_zombieDistanceMaxSep = 600;			//The maximum amount of team seperation before zombies aren't auto-respawned anymore

		public const int c_szombieMinRespawnDist = 3000;		//The minimum distance the super zombie can be spawned from the players
		public const int c_szombieMaxRespawnDist = 6000;		//The maximum distance the super zombie can be spawned from the players

		public const int c_combatBotPathUpdateInterval = 5000;	//The amount of ticks before an engineer's combat bot updates it's path

		public const int c_divertZombiesDuration = 60000;		//How long in ticks the divert zombies skill lasts for

		public const int c_campingCheckInterval = 10000;		//The frequency which we check teams for camping
		public const float c_campingMaxCountMod = 3.0f;			//The maximum amount that camping may influence the zombie count
		public const float c_campingCountModGrowth = 0.027f;	//The rate at which the count mod grows if the team don't satisfy the threshold
		public const float c_campingCountModDecay = 0.15f;		//The rate at which the count mod decays if the team satisfy the threshold
		public const int c_campingDistanceThreshold = 700;		//The distance moved, per check, at which no penalty is incurred
		public const int c_campingSeperationThreshold = 1000;	//The average team seperation before the awareness counter starts increasing
		public const float c_campingSeperationModGrowth = 0.15f;//The rate at which the count mod grows if the team is seperated


		///////////////////////////////////////////////////
		// Member Classes
		///////////////////////////////////////////////////
		#region Member Classes
		/// <summary>
		/// Represents a location that zombies will be attracted to for a period of time
		/// </summary>
		public class ZombieDistraction
		{
			public bool bActive;

			public short x;
			public short y;
			public int tickDistractionEnd;

			public bool bStillHostile;				//Do the zombies still attack from the distraction?
			public int distractLimit;				//The amount of zombies we can distract at once

			public ZombieDistraction(short _x, short _y, int _tickEnd, bool _bStillHostile, int _distractLimit)
			{
				bActive = true;

				x = _x;
				y = _y;
				tickDistractionEnd = _tickEnd;

				bStillHostile = _bStillHostile;
				distractLimit = _distractLimit;
			}
		}

		/// <summary>
		/// Represents the zombies and spawning behavior for a certain team
		/// </summary>
		public class TeamState
		{
			public List<Player> originalPlayers;		//The players present when the team was formed
			public Team team;							//The team we represent

			public List<ZombieDistraction> distractions;//Potentials distractions the zombie may be attracted to

			public int teamSeperation;					//The amount of distance between team members

			public short lastTeamLocationX;				//The last average location of the team
			public short lastTeamLocationY;				//
			public int tickLastLocationCheck;			//The time at which we last checked the average location
			public float campingZombieCountMod;			//The amount which camping is affecting the zombie count

			public bool bCloaked;						//Is the team cloaked? (not to be found)
			public int tickCloakEnd;					//When does the cloak expire?

			public bool bPerished;						//Has the team died?
			public int totalZombieKills;				//The team's total amount of zombie kills
			public int totalSkillEarned;				//The total amount of skill earned by the team

			public int zombieLimit;						//The amount of zombies after the team at once
			public int tickLastZombieCountIncrease;		//The last time at which we added a zombie

			public List<ZombieBot> zombies;				//The team's pursuing normal zombies
			public List<Player> zombiePlayers;			//The zombies attacking this team, who are players
			public ZombieBot superZombie;				//The team's pursuing super zombie

			public ZombieTransitions.ZombieTransition trans;	//The current transition
			public ZombieTransitions transitions;				//Used to determine the flow and zombie composition for the game

			public Computer ammoGenerator;				//The team's current ammo generator
			public int ammoGeneratorLevel;				//The upgrade level the generator is currently at
			public int tickLastAmmoGenerate;			//The time at which ammo was last generated

			public Computer supplyDrop;					//The team's active supply drop
			public int supplyDropsFound;				//The amount of supply drops we've found
			public int tickLastSupplyDropFinish;		//The time at which the team's last supply drop was used

			public int tickLastZombieAdd;				//The tick at which the last zombie was added
			public int zombieSpawnRate;					//The time between increases of the zombie limit

			public TeamState()
			{
				zombies = new List<ZombieBot>();
				zombiePlayers = new List<Player>();

				transitions = new ZombieTransitions();
				distractions = new List<ZombieDistraction>();
			}
		}
		#endregion Member Classes

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

			_zombieHorde = _arena.Teams.SingleOrDefault(t => t._name == "Zombie Horde");

			return true;
		}

		/// <summary>
		/// Allows the script to maintain itself
		/// </summary>
		public bool poll()
		{	//Maintain our arena if the game is running!
			int now = Environment.TickCount;

			if (_bGameRunning && now - _arena._tickGameStarted > 2000)
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

			if (_bGameRunning && now - _arena._tickGameStarted > 2000)
			{	//Should we update the zombie count ticket?
				if (now - _tickGameLastTickerUpdate > 5000)
				{	
					updateGameTickers();
					_tickGameLastTickerUpdate = now;
				}

				//Take care of each active team
				foreach (TeamState team in _states.Values)
				{	//No zombie horde!
					if (team.team._id == 0)
						continue;

					//Make sure it's active
					if (team.team.ActivePlayerCount == 0 || team.bPerished)
					{	//Is it marked as dead?
						if (!team.bPerished)
							teamPerished(team.team);
						continue;
					}

					//Check the team location?
					if (now - team.tickLastLocationCheck > c_campingCheckInterval)
					{	//Get the new average team position
						team.tickLastLocationCheck = now;
						short newX, newY;
						bool bModDecreasing = true;

						averageTeamLocation(team.team, out newX, out newY);

						//Is it over the threshold?
						int distance = (int)Math.Sqrt(Math.Pow(team.lastTeamLocationX - newX, 2) + Math.Pow(team.lastTeamLocationY - newY, 2));
						if (distance <= c_campingDistanceThreshold)
						{
							team.campingZombieCountMod += c_campingCountModGrowth;
							bModDecreasing = false;
						}

						team.lastTeamLocationX = newX;
						team.lastTeamLocationY = newY;

						//Check for team seperation
						if (team.teamSeperation > c_campingSeperationThreshold)
						{
							team.campingZombieCountMod += c_campingSeperationModGrowth;
							bModDecreasing = false;
						}

						if (team.campingZombieCountMod > c_campingMaxCountMod)
							team.campingZombieCountMod = c_campingMaxCountMod;
						else if (team.campingZombieCountMod < 1.0f)
							team.campingZombieCountMod = 1.0f;

						if (bModDecreasing)
							team.campingZombieCountMod -= c_campingCountModDecay;
					}
					
					//Time to spawn a new supply depot?
					if (now - team.tickLastSupplyDropFinish > c_supplyDropSpacing * 1000)
					{
						spawnSupplyDrop(team.team, team);
						team.tickLastSupplyDropFinish = int.MaxValue;
					}

					//Time to spawn more ammo?
					if (team.ammoGenerator != null && team.ammoGenerator._state.health > 0 &&
						now - team.tickLastAmmoGenerate > c_ammoGenerateInterval)
					{	//Generate the ammo!
						ammoGenerate(team, team.ammoGenerator);
						team.tickLastAmmoGenerate = now;

						//It slowly decays!
						team.ammoGenerator._state.health -= c_ammoGenerateDecay;
						if (team.ammoGenerator._state.health <= 0)
							team.ammoGenerator.kill(null);
					}

					//Cloak expired?
					if (team.bCloaked && now > team.tickCloakEnd)
						team.bCloaked = false;

					//Any distractions require ending?
					foreach (ZombieDistraction distract in team.distractions)
						if (now > distract.tickDistractionEnd)
						{
							team.distractions.Remove(distract);
							distract.bActive = false;
							break;
						}

					//Update the seperation
					team.teamSeperation = getTeamSeperation(team.team);
				}
			}

			return true;
		}

		#region Setup
		/// <summary>
		/// Reapplies any weapon upgrades the player may have purchased
		/// </summary>
		public void checkUpgradeSkill(Player p, SkillInfo skill)
		{	//Does he have it?
			if (p.findSkill(skill.SkillId) != null)
			{	//Yes! Prize him the upgrade item
				p.inventoryModify(false, skill.InventoryMutators[0].ItemId, 1);
			}
		}

		/// <summary>
		/// Reapplies any weapon upgrades the player may have purchased
		/// </summary>
		public void checkUpgradeSkills(Player p)
		{	//Check each skill
			checkUpgradeSkill(p, AssetManager.Manager.getSkillByName("Upgrade Assault Rifle"));
			checkUpgradeSkill(p, AssetManager.Manager.getSkillByName("Upgrade Assault Rifle+"));
			checkUpgradeSkill(p, AssetManager.Manager.getSkillByName("Upgrade Battle Rifle"));
			checkUpgradeSkill(p, AssetManager.Manager.getSkillByName("Upgrade Assault Rifle+"));
			checkUpgradeSkill(p, AssetManager.Manager.getSkillByName("Upgrade Machine Pistol"));
			checkUpgradeSkill(p, AssetManager.Manager.getSkillByName("Upgrade Machine Pistol+"));
			checkUpgradeSkill(p, AssetManager.Manager.getSkillByName("Upgrade Gas Projector"));
			checkUpgradeSkill(p, AssetManager.Manager.getSkillByName("Upgrade Gas Projector+"));
			checkUpgradeSkill(p, AssetManager.Manager.getSkillByName("Upgrade Maklov G2 ACW"));
			checkUpgradeSkill(p, AssetManager.Manager.getSkillByName("Upgrade Maklov G2 ACW+"));
			checkUpgradeSkill(p, AssetManager.Manager.getSkillByName("Upgrade RPG"));
			checkUpgradeSkill(p, AssetManager.Manager.getSkillByName("Upgrade RPG+"));
			checkUpgradeSkill(p, AssetManager.Manager.getSkillByName("Upgrade Rifle Grenade"));
		}

		/// <summary>
		/// Spawns equipment around the team's spawn point
		/// </summary>
		public void setupSpawnPoint(Team team, short posX, short posY)
		{	//Let's get some class kits down!
			spawnItemInArea(AssetManager.Manager.getItemByName("Chemist Kit"), 1, posX, posY, c_startAreaRadius);
			spawnItemInArea(AssetManager.Manager.getItemByName("Engineer Kit"), 1, posX, posY, c_startAreaRadius);
			spawnItemInArea(AssetManager.Manager.getItemByName("Heavy Marine Kit"), 1, posX, posY, c_startAreaRadius);
			spawnItemInArea(AssetManager.Manager.getItemByName("Squad Leader Kit"), 1, posX, posY, c_startAreaRadius);
            spawnItemInArea(AssetManager.Manager.getItemByName("Scout Kit"), 1, posX, posY, c_startAreaRadius);
        }

		/// <summary>
		/// Sets up a player for a new game as a marine
		/// </summary>
		public void setupMarinePlayer(Player player)
		{	//Make him a marine!
			if (player.findSkill(13) != null)
				player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(13));
			else if (player.findSkill(12) != null)
				player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(12));
			else if (player.findSkill(11) != null)
				player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(11));
			else
				player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(10));

			//Reset his bounty
			player.Bounty = 0;

			//Set up his inventory
			player.inventoryModify(false, AssetManager.Manager.getItemByName("convertFakeToReal"), 500);
			player.inventoryModify(false, AssetManager.Manager.getItemByName("removeTempItem"), 9999);

			//Give him his cash allowance
			player.Cash = c_teamCashStart / player._team.ActivePlayerCount;

			//Give him some starting ammo!
			ItemInfo ammoItem = AssetManager.Manager.getItemByName("Ammo");
			player.inventorySet(false, ammoItem, -ammoItem.maxAllowed);

			//Give him some consumables!
			player.inventorySet(false, AssetManager.Manager.getItemByName("Energizer"), 2);
			player.inventorySet(false, AssetManager.Manager.getItemByName("Frag Grenade"), 2);

			//Make sure his weapons are suitably upgraded
			checkUpgradeSkills(player);

			//Done, sync!
			player.syncInventory();
		}

		/// <summary>
		/// Called when an ammo generator is due to create new ammo
		/// </summary>
		public void ammoGenerate(TeamState state, Computer ammoGenerator)
		{	//How much ammo do we have in the area?
			ItemInfo ammo = AssetManager.Manager.getItemByName("Ammo");
			int ammoArea = _arena.getItemCountInRange(ammo, ammoGenerator._state.positionX, ammoGenerator._state.positionY, 112);
			int ammoAmount = c_ammoGenerateAmount + (state.ammoGeneratorLevel * c_ammoGeneratePerLevel);

			if (ammoArea < ammoAmount)
				_arena.itemSpawn(ammo, (ushort)ammoAmount, ammoGenerator._state.positionX, ammoGenerator._state.positionY, 112);
		}

		/// <summary>
		/// Called when a player attempts to open a supply drop
		/// </summary>
		public void openSupplyDrop(Computer supply, Player opener, VehInfo.ComputerProduct product)
		{	//Make sure we don't trigger this twice
			if (supply.getVarInt("opened") != 0)
				return;
			supply.setVar("opened", 1);

			//Is he on the wrong team?
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
				spawnItemInArea(AssetManager.Manager.getItemByName("Ammo"), (ushort)_rand.Next(180, 220), posX, posY, c_supplyDropAreaRadius);

			spawnItemInArea(AssetManager.Manager.getItemByName("Frag Grenade"), (ushort)_rand.Next(1, 3), posX, posY, c_supplyDropAreaRadius);
			spawnItemInArea(AssetManager.Manager.getItemByName("Molotov Cocktail"), (ushort)_rand.Next(0, 2), posX, posY, c_supplyDropAreaRadius);
			spawnItemInArea(AssetManager.Manager.getItemByName("Repulsion Shield"), (ushort)_rand.Next(50, 100), posX, posY, c_supplyDropAreaRadius);
			spawnItemInArea(AssetManager.Manager.getItemByName("Energizer"), (ushort)_rand.Next(1, 2), posX, posY, c_supplyDropAreaRadius);
			spawnItemInArea(AssetManager.Manager.getItemByName("Energizer"), (ushort)_rand.Next(1, 2), posX, posY, c_supplyDropAreaRadius);

			//Give each player some cash
			foreach (Player p in team.ActivePlayers)
			{
				int cashGained = c_playerSupplyDropCash + (state.supplyDropsFound * c_playerSupplyDropCashGrow);
				p.Cash += cashGained;
				p.Experience += c_playerSupplyDropExp;

				p.sendMessage(0, String.Format("Found supply drop! (Cash={0} Exp={1})", cashGained, c_playerSupplyDropExp));
				p.syncState();
			}

			state.supplyDropsFound++;

			//Do we have a chemist in the team?
			bool bChemistPresent = false;

			foreach (Player p in team.ActivePlayers)
			{
				if (p._baseVehicle._type.ClassId == 3 && !p.IsDead)
				{
					spawnItemInArea(AssetManager.Manager.getItemByName("Medicine"), (ushort)(team.ActivePlayerCount - 1), posX, posY, c_supplyDropAreaRadius);
					bChemistPresent = true;
					break;
				}
			}

			if (!bChemistPresent)
			{	//Heal every player slightly
				foreach (Player p in team.ActivePlayers)
					if (!p.IsDead)
						p.heal(AssetManager.Manager.getItemByName("SupplyDrop Healing") as ItemInfo.RepairItem, opener);
			}

			//Let's have a weapon spawn!
			int selection = _rand.Next(0, 4);
			ItemInfo weaponPrize = getRandomWeaponPrize();

			if (weaponPrize != null)
				spawnItemInArea(weaponPrize, (ushort)(-weaponPrize.maxAllowed), posX, posY, c_supplyDropAreaRadius);

			//Remove the supply drop!
			state.tickLastSupplyDropFinish = Environment.TickCount;
			state.supplyDrop = null;

			supply.destroy(true);
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
			TeamState tstate = null;
			int attackers = int.MaxValue;

			foreach (TeamState team in _states.Values)
			{
				if (!team.bPerished && team.team.ActivePlayerCount > 0 && team.zombiePlayers.Count < attackers)
				{
					tstate = team;
					attackers = team.zombiePlayers.Count;
				}

				//Make sure he's not present in any teams
				team.zombiePlayers.Remove(player);
			}

			if (tstate == null)
			{
				player.resetWarp();
				return;
			}

			tstate.zombiePlayers.Add(player);

			Team target = tstate.team;
			player.setVar("targetTeam", target);

			//Modify inventory as appropriate
            player.inventoryModify(false, AssetManager.Manager.getItemByName("removeTempItem"), 9999);

			player.inventorySet(AssetManager.Manager.getItemByName("Fury Sprint"), 2);
			
			//Make him a zombie
			player.setDefaultVehicle(ZombieZoneStats.getPlayableZombie(player));

			if (target != null)
			{	//Determine a place to spawn
				Helpers.ObjectState state = new Helpers.ObjectState();

				if (!findSpawnLocation(target, ref state, (short)c_zombieMinRespawnDist, (short)c_zombieMaxRespawnDist))
				{
					Log.write(TLog.Error, "Unable to find zombie spawn location.");
					return;
				}

				player.warp(Helpers.ResetFlags.ResetAll, state, 0, -1, 0);
			}
		}

		/// <summary>
		/// Called when a zombie has despawned due to being too far, etc
		/// </summary>
		public void zombieDespawn(Team attacking)
		{	//Reset the zombie add timers

		}

		/// <summary>
		/// Called when a team has died to clear up the zombie mess
		/// </summary>
		public void teamPerished(Team dead)
		{
			TeamState state = getTeamState(dead);
			state.bPerished = true;

			//Destroy all the bots!
			foreach (ZombieBot bot in state.zombies)
				bot.kill(null);
			if (state.superZombie != null)
				state.superZombie.destroy(true);
			if (state.supplyDrop != null)
				state.supplyDrop.destroy(true);

			//They're gone!
			_arena.sendArenaMessage("!Team " + dead._name + " has fallen.");

			//Is this the last team left?
			bool bLastTeam = true;

			foreach (TeamState t in _states.Values)
				if (!t.bPerished)
					bLastTeam = false;

			if (bLastTeam)
				//They won!
				gameVictory(dead);
			else
			{	//Warp all zombie players to a new team
				List<Player> zombiePlayers = new List<Player>(state.zombiePlayers);
				foreach (Player zombie in zombiePlayers)
					spawnZombiePlayer(zombie);
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
			ZombieBot zombie = _arena.newBot(typeof(KingZombieBot), zombieVeh, _zombieHorde, null, state, this) as ZombieBot;

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
		public void spawnNewZombie(Team target, ZombieType ztype, Helpers.ObjectState state)
		{	//Determine a place to spawn?
			if (state == null)
			{
				state = new Helpers.ObjectState();

				if (!findSpawnLocation(target, ref state, (short)c_zombieMinRespawnDist, (short)c_zombieMaxRespawnDist))
				{
					Log.write(TLog.Error, "Unable to find zombie spawn location.");
					return;
				}
			}

			//Find the team state
			TeamState team = getTeamState(target);

			//Create our new zombie
			ZombieBot zombie = _arena.newBot(ztype.classType, ztype.vehicleType, _zombieHorde, null, state, this) as ZombieBot;

			if (zombie == null)
			{
				Log.write(TLog.Error, "Unable to create zombie bot.");
				return;
			}

			if (ztype.zombieSetup != null)
				ztype.zombieSetup(zombie);
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
				bool bPause;
				ZombieTransitions.ZombieTransition trans = state.transitions.getNewTransition(now, out bPause);

				if (trans != null)
				{	//Set it!
					state.trans = trans;

					//Do we need to reset the zombie count?
					if (trans.resetZombieCountTo != 0)
						state.zombieLimit = trans.resetZombieCountTo;
				}

				//Can we start the transition yet?
				if (!state.trans.bStarted)
				{
					if (state.trans.zombiePopThreshold >= state.zombies.Count)
					{	//We can start!
						state.trans.bStarted = true;
						state.trans.thresholdReached(state.trans);

						if (state.trans.bWave)
						{	//Spawn zombies!
							foreach (ZombieType zt in state.trans.types)
							{
								int waveAmount = (int)(state.campingZombieCountMod * zt.spawnWeight * team.ActivePlayerCount);
								for (int i = 0; i < waveAmount; ++i)
									spawnNewZombie(team, zt, null);
							}
						}

						if (state.trans.started != null)
							state.trans.started(state);
					}
				}
				else
				{
					if (!state.trans.bWave && !bPause)
					{	//Calculate the respawn time
						int respawnTimeBonus = (state.campingZombieCountMod >= 2.0f ? 
							0 : ((state.team._info.maxPlayers - state.originalPlayers.Count) * c_zombieRespawnTimeGrowth));
						int respawnTime = c_zombieRespawnTime + respawnTimeBonus;

						if (Math.Floor(state.campingZombieCountMod * state.trans.zombieCountMod * state.zombieLimit) > state.zombies.Count &&
							(now - state.tickLastZombieAdd) > respawnTime)
						{
							state.tickLastZombieAdd = now;
							spawnNewZombie(team, state.trans.getRandomType(_rand), null);
						}
					}
				}

				//Increase our zombie limit if necessary
				if (!state.trans.bPauseZombieAdd && !bPause && now > state.tickLastZombieCountIncrease + state.zombieSpawnRate)
				{
					state.zombieLimit++;
					state.tickLastZombieCountIncrease = now;
				}

				//Make sure each team has a super zombie (no super zombie for solo teams)
				if (state.originalPlayers.Count > 1 && team.ActivePlayerCount > 0 
					&& state.superZombie != null && state.superZombie.bCondemned)
					spawnSuperZombie(team);
			}
		}

		/// <summary>
		/// Called when a zombie has been killed
		/// </summary>
		public void zombieKilled(Player killer, int zombieVID, int weaponID, Player victim, Bot botVictim)
		{	//Update the jackpot amount
			if (_states.Values.Count(t => t.team.ActivePlayerCount > 0) > 1)
			{
				int zombiesKilled = 0;
				foreach (TeamState st in _states.Values)
					zombiesKilled += st.totalZombieKills;
				_jackpot = (int)((float)(zombiesKilled * 20) * Math.Pow(1.15f, _states.Values.Count));
			}

			//Increment the player's zombie kills
			killer.Kills++;

			killer.setVar("zombieKills", killer.getVarInt("zombieKills") + 1);

			increaseSkillRating(killer, (victim != null ? victim._baseVehicle : botVictim), weaponID);

			//Increment the team's zombie kills
			TeamState state = getTeamState(killer._team);
			if (state != null)
				state.totalZombieKills++;

			//Calculate the reward
			int expReward = _stats.getZombieExp(zombieVID);

			foreach (Player p in killer._team.ActivePlayers)
			{
				p.Experience += expReward;
				p.syncState();
			}

			//Make it known!
			if (victim != null)
			{
				_arena.triggerMessage(5, 500, String.Format("{0} killed {1}", killer._alias, victim._alias), killer._team);
				killer._team.triggerMessage(9, 500, String.Format("{0} killed {1} (Exp={2})", killer._alias, victim._alias, expReward), killer);
				killer.triggerMessage(1, 500, String.Format("{0} killed {1} (Exp={2})", killer._alias, victim._alias, expReward));
			}
			else
			{
				_arena.triggerMessage(5, 500, String.Format("{0} killed a {1}", killer._alias, botVictim._type.Name), killer._team);
				killer._team.triggerMessage(9, 500, String.Format("{0} killed a {1} (Exp={2})", killer._alias, botVictim._type.Name, expReward), killer);
				killer.triggerMessage(1, 500, String.Format("{0} killed a {1} (Exp={2})", killer._alias, botVictim._type.Name, expReward));
			}
		}
		#endregion

		#region Statistics
		/// <summary>
		/// Updates the game's ticker info
		/// </summary>
		public void updateGameTickers()
		{	//Create a list of the most successful players
			if (_arena.PlayerCount == 0)
				return;

			//Calculate some total stats for spec
			int totalZombieKills = _states.Values.Sum(state => state.totalZombieKills);
			int totalZombies = _states.Values.Sum(state => state.zombies.Count);

			List<Player> activePlayers = _arena.PlayersIngame.OrderByDescending(player => player.getVarInt("skillReached")).ToList();

			Player marine = activePlayers[0];
			int skillReached = marine.getVarInt("skillReached");
			
			if (skillReached == 0)
				_arena.setTicker(1, 1, 0, "No zombie kills yet!");
			else
			{
				String best = String.Format("Best Marine: {0} ({1})", marine._alias, skillReached);

				_arena.setTicker(1, 1, 0,
					delegate(Player p)
					{
						if (_jackpot > 0)
							return best + String.Format(" / Jackpot ({0})", _jackpot);
						else
							return best;
					}
				);
			}

			//Inform them of their nearest supply drop!
			_arena.setTicker(1, 2, 0,
				delegate(Player p)
				{	//Is he a zombie?
					if (p._team._id == 0)
					{	//Show his target team's position
						Team target = p.getVar("targetTeam") as Team;
						if (target == null)
							return "No Target Team";

						short posX, posY;
						averageTeamLocation(target, out posX, out posY);

						return String.Format("Marine team at {0}", Helpers.posToLetterCoord(posX, posY));
					}

					TeamState state = getTeamState(p._team);

					if (state != null && state.supplyDrop != null)
					{	//Find the player's distance to the drop
						int distance = (int)((p._state.position() - state.supplyDrop._state.position()).Length * 100);

						return String.Format("Supply Drop at {0} (Distance: {1})", state.supplyDrop._state.letterCoord(), distance / 16);
					}
					else
						return String.Format("Total Zombie Kills: {0}", totalZombieKills);
				}
			);

			//Update the zombie count!
			_arena.setTicker(1, 3, 0,
				delegate(Player p)
				{	//Get the zombie count for his team
					TeamState state = getTeamState(p._team);
					if (state != null)
					{
						int awareness = (int)(((state.campingZombieCountMod - 1) / (c_campingMaxCountMod - 1)) * 100);
						return String.Format("Zombie Count: {0} / Awareness: {1}%", state.zombies.Count, awareness < 0 ? 0 : awareness);
					}
					else
						return String.Format("Total Zombies: {0}", totalZombies);
				}
			);
		}

		/// <summary>
		/// Updates the game's ticker info
		/// </summary>
		public void increaseSkillRating(Player player, Vehicle zombie, int weaponID)
		{	//Determine the skill rating of the kill
			int skillRating = ZombieZoneStats.getKillSkillRating(zombie._type.Id);
			if (weaponID != 0)
				skillRating += ZombieZoneStats.getWeaponSkillRating(weaponID);

			if (skillRating > 0)
			{
				player.Bounty += skillRating;
				player.ZoneStat8 += skillRating;

				if (player.Bounty > player.ZoneStat7)
					player.ZoneStat7 = player.Bounty;

				player.setVar("skillReached", player.getVarInt("skillReached") + skillRating);
				player.setVar("pureSkill", player.getVarInt("pureSkill") + skillRating);

				TeamState state = getTeamState(player._team);
				if (state != null)
					state.totalSkillEarned += skillRating;

				if (state.originalPlayers.Count == 1 && player.Bounty > player.ZoneStat9)
					player.ZoneStat9 = player.Bounty;

				//Is the killer in a turret or vehicle?
				if (player._occupiedVehicle != null)
				{	//We need to reward the creator a little!
					if (player != player._occupiedVehicle._creator && player._occupiedVehicle._creator != null)
					{
						Player creator = player._occupiedVehicle._creator;
						skillRating = ZombieZoneStats.getKillSkillRating(zombie._type.Id);

						//Make sure he's a marine still
						if (creator._baseVehicle._type.Id < 100 && skillRating > 0)
						{
							creator.Bounty += skillRating;
							creator.ZoneStat8 += skillRating;

							if (creator.Bounty > creator.ZoneStat7)
								creator.ZoneStat7 = creator.Bounty;

							creator.setVar("skillReached", creator.getVarInt("skillReached") + skillRating);

							state = getTeamState(creator._team);
							if (state != null)
								state.totalSkillEarned += skillRating;
						}
					}
				}

				//Reward all players for assist damage
				skillRating = ZombieZoneStats.getKillSkillRating(zombie._type.Id);

				foreach (Player assist in zombie._attackers)
				{
					assist.Bounty += skillRating;
					assist.ZoneStat8 += skillRating;

					if (assist.Bounty > assist.ZoneStat7)
						assist.ZoneStat7 = assist.Bounty;

					assist.setVar("skillReached", assist.getVarInt("skillReached") + skillRating);

					state = getTeamState(assist._team);
					if (state != null)
						state.totalSkillEarned += skillRating;
				}
			}
		}
		#endregion

		#region Utility
		/// <summary>
		/// Obtains the zombie state for a specified team
		/// </summary>
		public TeamState getTeamState(Team target)
		{	//Make sure it's a marine team!
			if (target == null || target._id == 0 || target._name == "spec")
				return null;

			//Get the state!
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
		/// Obtains the average seperation from the team location
		/// </summary>
		static public int getTeamSeperation(Team team)
		{	//Find the average location
			short posX, posY;

			averageTeamLocation(team, out posX, out posY);

			int seperation = 0;
			int count = 0;

			foreach (Player p in team.ActivePlayers)
				if (!p.IsDead)
				{
					int xDiff = (p._state.positionX - posX);
					int yDiff = (p._state.positionY - posY);

					seperation += (int)Utility.Sqrt((xDiff * xDiff) + (yDiff * yDiff));
					count++;
				}

			if (count == 0)
				return -1;

			return seperation / count;
		}

		/// <summary>
		/// Chooses a random weapon prize to give
		/// </summary>
		public ItemInfo getRandomWeaponPrize()
		{
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
				case 2:
					weaponPrize = AssetManager.Manager.getItemByName("Railgun");
					break;
				case 3:
					weaponPrize = AssetManager.Manager.getItemByName("Thermal Lance");
					break;
				case 4:
					weaponPrize = AssetManager.Manager.getItemByName("Maser");
					break;
			}

			return weaponPrize;
		}

		/// <summary>
		/// Obtains the average position for an entire team
		/// </summary>
		static public void averageTeamLocation(Team team, out short _posX, out short _posY)
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

			if (count == 0)
			{
				_posX = 0;
				_posY = 0;
				return;
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
			//Laboratory abilities
			else if (computer._type.Name == "Laboratory" && product.Title.StartsWith("Heal "))
			{	//Apply the healing if we have ammo
				if (player.inventoryModify(AssetManager.Manager.getItemByID(product.PlayerItemNeededId), -product.PlayerItemNeededQuantity))
					player.heal(AssetManager.Manager.getItemByName("MedicalCenter Healing") as ItemInfo.RepairItem, player);
				return false;
			}
			else if (computer._type.Name == "Laboratory" && product.Title.StartsWith("Resurrect Player "))
			{	//Find a dead player
				TeamState state = getTeamState(player._team);
				if (state != null)
				{
					bool bResurrected = false;

					foreach (Player p in state.originalPlayers)
					{	//Can we res him?
						if (p.bDestroyed || p.IsDead || p.IsSpectator)
							continue;

						if (p._team == state.team || p._arena != _arena)
							continue;

						//Yes! Do it
						int oldVID = p.getVarInt("oldVehicle");
						if (oldVID == 0)
							continue;

						if (player.inventoryModify(AssetManager.Manager.getItemByID(product.PlayerItemNeededId), -product.PlayerItemNeededQuantity))
						{	//Set him up
							player._team.addPlayer(p);

							//Wipe his previous inventory!
							p.inventoryModify(false, AssetManager.Manager.getItemByName("convertFakeToReal"), 500);
							p.inventoryModify(false, AssetManager.Manager.getItemByName("removeTempItem"), 9999);

							//Give him some starting ammo!
							p.inventorySet(false, AssetManager.Manager.getItemByName("Ammo"), player.getVarInt("oldAmmo"));

							p.setDefaultVehicle(AssetManager.Manager.getVehicleByID(oldVID));
							p.warp(player);

							//Lower his health a little
							p.inventoryModify(AssetManager.Manager.getItemByName("MinusHealth"), 60);

							//Done!
							p.sendMessage(0, "You have been resurrected by " + player._alias);
							bResurrected = true;
							break;
						}
					}

					if (!bResurrected)
						player.sendMessage(-1, "Unable to find a marine for resurrection.");
				}

				return false;
			}
			//Ammo generator abilities
			else if (computer._type.Name == "Ammo Generator" && product.Title == "Upgrade")
			{	
				TeamState state = getTeamState(player._team);
				if (state != null)
				{	//Apply the upgrade if we have enough cash
					int cashCost = 2000 + (state.ammoGeneratorLevel * 750);

					if (player.Cash >= cashCost)
					{
						player.Cash -= cashCost;
						player.syncInventory();

						state.ammoGeneratorLevel++;

						player._team.sendArenaMessage(String.Format("Ammo generator is now level {0}. Next Cost={1}", state.ammoGeneratorLevel, cashCost + 500));
					}
					else
						player.sendMessage(-1, "You need " + cashCost + " cash to upgrade to the next level.");
				}
				
				return false;
			}
			//Command post abilities
			else if (computer._type.Name == "Command Post" && product.Title.StartsWith("Divert Zombies "))
			{	//Got enough cash?
				if (player.Cash >= product.Cost)
				{
					player.Cash -= product.Cost;
					player.syncInventory();

					player._team.sendArenaMessage("Zombies have been diverted by external forces for a while.");

					TeamState state = getTeamState(player._team);
					if (state != null)
					{
						state.transitions.delayTransition(c_divertZombiesDuration);
						state.tickLastZombieAdd += c_divertZombiesDuration;
						state.tickLastZombieCountIncrease += c_divertZombiesDuration;
					}
				}

				return false;
			}
			else if (computer._type.Name == "Command Post" && product.Title.StartsWith("Requisition Weapon "))
			{	//Got enough cash?
				if (player.Cash >= product.Cost)
				{
					player.Cash -= product.Cost;
					player.syncInventory();

					//Find a weapon and give it to him
					ItemInfo weaponPrize = getRandomWeaponPrize();

					player.inventorySet(weaponPrize, (ushort)(-weaponPrize.maxAllowed));
					player.sendMessage(0, weaponPrize.name + " has been requisitioned.");
				}

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
				{	//Use an appropriate zombie vehicle
					VehInfo zombieVeh = AssetManager.Manager.getVehicleByID(211);

					//Create our new zombie
					ZombieBot zombie = _arena.newBot(typeof(ZombieBot), zombieVeh, _arena.PublicTeams.ToList()[0], player, player._state, this) as ZombieBot;
					zombie.targetTeam = _arena.PublicTeams.ToList()[1];
				}
			}
			else if (command.ToLower() == "countmod")
			{
				TeamState state = getTeamState(player._team);
				if (state != null)
					player.sendMessage(0, state.campingZombieCountMod.ToString());
			}
			return true;
		}

		/// <summary>
		/// Triggered when a vehicle dies
		/// </summary>
		[Scripts.Event("Bot.Death")]
		public bool botDeath(Bot dead, Player killer, int weaponID)
		{	//A combat bot?
			if (dead is CombatBot)
			{
				if (dead._creator != null)
					dead._creator.setVar("combatBot", null);
				return true;
			}

			//Suicide bots?
			if (killer == null)
				return true;

			zombieKilled(killer, dead._type.Id, weaponID, null, dead);
			return true;
		}

		/// <summary>
		/// Handles the spawn of a player
		/// </summary>
		[Scripts.Event("Player.Spawn")]
		public bool playerSpawn(Player player, bool bDeath)
		{
			if (!_bGameRunning)
				return false;

			//Put him on zombie horde, make sure he's a zombie
			if (player._team != _zombieHorde)
				_zombieHorde.addPlayer(player);

			spawnZombiePlayer(player);
			return true;
		}

		/// <summary>
		/// Triggered when a player has died, by any means
		/// </summary>
		/// <remarks>killer may be null if it wasn't a player kill</remarks>
		[Scripts.Event("Player.Death")]
		public bool playerDeath(Player victim, Player killer, Helpers.KillType killType, CS_VehicleDeath update)
		{	//Route the death!
			if (killer == null)
				update.type = Helpers.KillType.Computer;
			Helpers.Player_RouteKill(_arena.Players, update, victim, 0, 0, 0, 0);

			if (!_bGameRunning)
				return false;

			//Was he a marine?
			if (victim._baseVehicle._type.Id < 100)
			{	//Make a note of the time of death and last vid
				victim.setVar("tickDeath", Environment.TickCount);
				victim.setVar("oldVehicle", victim._baseVehicle._type.Id);
				victim.setVar("oldAmmo", victim.getInventoryAmount(2000));

				//Update stats
				victim.Deaths++;

				int gameTime = (Environment.TickCount - _tickGameStart) / 1000;
				if (gameTime > victim.ZoneStat1)
					victim.ZoneStat1 = gameTime;

				int zombieKills = victim.getVarInt("zombieKills");
				if (zombieKills > victim.ZoneStat2)
					victim.ZoneStat2 = zombieKills;

				//Was the killer a player?
				if (killer != null)
					killer.ZoneStat5++;
			}
			//No, a zombie! Reward the killer?
			else if (killer != null)
			{
				zombieKilled(killer, victim._baseVehicle._type.Id, 0, victim, null);
				victim.ZoneStat6++;
			}

			return false;
		}

		/// <summary>
		/// Triggered when a player requests to pick up an item
		/// </summary>
		[Scripts.Event("Player.ItemPickup")]
		public bool playerItemPickup(Player player, Arena.ItemDrop drop, ushort quantity)
		{	//Are they any kit items?
			if (drop.item.name == "Engineer Kit")
			{	//Only marines can change class
				if (player._baseVehicle._type.Id < 10 || player._baseVehicle._type.Id > 14)
					return false;

				if (player.findSkill(18) != null)
					player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(18));
				else if (player.findSkill(17) != null)
					player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(17));
				else if (player.findSkill(16) != null)
					player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(16));
				else
					player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(15));

				player.inventorySet(AssetManager.Manager.getItemByName("Engineering Bench"), 1);
			}
			else if (drop.item.name == "Heavy Marine Kit")
			{	//Only marines can change class
				if (player._baseVehicle._type.Id < 10 || player._baseVehicle._type.Id > 14)
					return false;

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
			{	//Only marines can change class
				if (player._baseVehicle._type.Id < 10 || player._baseVehicle._type.Id > 14)
					return false;
				
				//Give him some heals, depending on the amount of teammates
				player.inventorySet(false, AssetManager.Manager.getItemByName("Laboratory"), 1);
				player.inventorySet(AssetManager.Manager.getItemByName("Medicine"), player._team.ActivePlayerCount);

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
			{	//Only marines can change class
				if (player._baseVehicle._type.Id < 10 || player._baseVehicle._type.Id > 14)
					return false;

				player.inventorySet(AssetManager.Manager.getItemByName("Command Post"), 1);

				if (player.findSkill(33) != null)
					player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(33));
				else if (player.findSkill(32) != null)
					player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(32));
				else if (player.findSkill(31) != null)
					player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(31));
				else
					player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(30));
			}
            else if (drop.item.name == "Scout Kit")
            {   //Only marines can change class
                if (player._baseVehicle._type.Id < 10 || player._baseVehicle._type.Id > 14)
                    return false;

                player.inventoryModify(AssetManager.Manager.getItemByName("convertRealToFakeScout"), 16);

                if (player.findSkill(37) != null)
                    player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(37));
                else if (player.findSkill(36) != null)
                    player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(36));
                else if (player.findSkill(35) != null)
                    player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(35));
                else
                    player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(34));
            }

			return true;
		}

		/// <summary>
		/// Called when the specified team have won
		/// </summary>
		public void gameVictory(Team victors)
		{	//Stop the game
			_victoryTeam = victors;
			_arena.gameEnd();
		}

		/// <summary>
		/// Triggered when a player wants to unspec and join the game
		/// </summary>
		[Scripts.Event("Player.JoinGame")]
		public bool playerJoinGame(Player player)
		{	//Is the game in progress?
			if (_bGameRunning)
			{	//Make him a zombie!
				player.unspec(_zombieHorde);
				spawnZombiePlayer(player);
				player.sendMessage(-1, "&The game is already in progress so you have been spawned as a zombie.");
				return false;
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
			_victoryTeam = null;

			_states.Clear();

			//Let everyone know
			_arena.sendArenaMessage("Game has started!", _config.flag.resetBong);

			//Spread players across the public teams
			IEnumerable<Player> players = _arena.PlayersIngame.OrderBy(plyr => _rand.Next(0, 200));
			List<Team> publicTeams = _arena.PublicTeams.ToList();

			int playerCount = _arena.PlayerCount;
			_marineTeamCount = ((playerCount - 1) / 4) + 1;
			int playerPerTeam = (int)Math.Ceiling((float)playerCount / (float)_marineTeamCount);

			foreach (Player p in players)
			{	//Put him on an appropriate team
				publicTeams[_marineTeamCount - ((playerCount + playerPerTeam - 1) / playerPerTeam) + 1].addPlayer(p);
				playerCount--;

				p.ZoneStat3++;
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
				state.originalPlayers = new List<Player>(team.ActivePlayers);

				//Calculate the zombie spawn rate
				state.zombieSpawnRate = c_zombieAddTimer + (c_zombieAddTimerGrowth * (team._info.maxPlayers - team.ActivePlayerCount));
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
					marine.warp(Helpers.ResetFlags.ResetAll, -1,
								(short)(pX - c_startAreaRadius), (short)(pY - c_startAreaRadius),
								(short)(pX + c_startAreaRadius), (short)(pY + c_startAreaRadius));
				}

				setupSpawnPoint(team, pX, pY);
			}

			//After spawning all the teams, spawn a superzombie for each team
			foreach (Team team in _arena.Teams)
				if (team.ActivePlayerCount > 1)
					spawnSuperZombie(team);

			_bGameRunning = true;
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
			_bGameRunning = false;

			//Congratulate the victors
			if (_victoryTeam != null)
			{	//How many zombies have been killed?
				int zombiesKilled = 0;
				foreach (TeamState state in _states.Values)
					zombiesKilled += state.totalZombieKills;

				_arena.sendArenaMessage("!" + zombiesKilled + " zombies were slaughtered!");

				if (_states.Count > 1)
				{
					int winReward = (int)((float)(zombiesKilled * 20) * Math.Pow(1.15f, _states.Values.Count));

					foreach (Player p in getTeamState(_victoryTeam).originalPlayers)
					{
						p.ZoneStat4++;

						p.Experience += winReward;
						p.syncState();
					}

					_arena.sendArenaMessage("Team " + _victoryTeam._name + " were the last to survive! (Reward=" + winReward + ")");
				}

				//Give everyone a skill reward
				foreach (Player p in _arena.Players)
				{
					double zombieKills = p.getVarInt("zombieKills");
					double skillRating = p.getVarInt("pureSkill");

					if (skillRating <= 0)
						continue;

					double reward = skillRating * (skillRating / zombieKills);

					p.KillPoints += (int)reward;
					p.Experience += (int)reward;
					p.syncState();

					p.sendMessage(0, "Your skill reward (Exp=" + (int)reward + ")");
				}
			}

			//Show the breakdown!
			_arena.breakdown(true);

			//Reset all custom vars
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

				from.sendMessage(0, "#Most Skilled Marines");

				rankedPlayers = _arena.PlayersIngame.OrderByDescending(
					p => (int)(((double)p.getVarInt("pureSkill")) / ((double)p.getVarInt("zombieKills"))));
				idx = 3;	//Only display top three players

				foreach (Player p in rankedPlayers)
				{
					if (p.getVarInt("zombieKills") < 5)
						continue;

					if (idx-- == 0)
						break;

					//Set up the format
					string format = "!3rd - {0}";

					switch (idx)
					{
						case 2:
							format = "!1st - {0}";
							break;
						case 1:
							format = "!2nd - {0}";
							break;
					}

					from.sendMessage(0, String.Format(format, p._alias));
				}
			}

			from.sendMessage(0, "#Team Breakdown");

			IEnumerable<TeamState> rankedTeams = _states.Values.OrderByDescending(state => state.totalSkillEarned);
			idx = 3;	//Only display top three teams

			foreach (TeamState state in rankedTeams)
			{
				if (idx-- == 0 || state.totalSkillEarned == 0)
					break;

				string format = "!3rd (Skill={0} Kills={1}): {2}";

				switch (idx)
				{
					case 2:
						format = "!1st (Skill={0} Kills={1}): {2}";
						break;
					case 1:
						format = "!2nd (Skill={0} Kills={1}): {2}";
						break;
				}

				from.sendMessage(0, String.Format(format,
					state.totalSkillEarned, state.totalZombieKills,
					state.team._name));
			}

			from.sendMessage(0, "#Individual Breakdown");

			rankedPlayers = _arena.PlayersIngame.OrderByDescending(player => player.getVarInt("skillReached"));
			idx = 3;	//Only display top three players

			foreach (Player p in rankedPlayers)
			{
				int skillReached = p.getVarInt("skillReached");

				if (idx-- == 0 || skillReached == 0)
					break;

				string format = "!3rd (Skill={0} Kills={1}): {2}";

				switch (idx)
				{
					case 2:
						format = "!1st (Skill={0} Kills={1}): {2}";
						break;
					case 1:
						format = "!2nd (Skill={0} Kills={1}): {2}";
						break;
				}

				from.sendMessage(0, String.Format(format,
					skillReached, p.getVarInt("zombieKills"),
					p._alias));
			}

			from.sendMessage(0, String.Format("You (S={0} K={1})", from.getVarInt("skillReached"), from.getVarInt("zombieKills")));

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

		/// <summary>
		/// Triggered when a player notifies the server of an explosion
		/// </summary>
		[Scripts.Event("Player.Explosion")]
		public bool playerExplosion(Player player, ItemInfo.Projectile weapon, short posX, short posY, short posZ)
		{	//Is it a pheramone grenade?
			if (weapon.id == 1042)
			{	//Add a distraction to the player's team
				TeamState state = getTeamState(player._team);

				if (state != null)
					state.distractions.Add(new ZombieDistraction(posX, posY, Environment.TickCount + 10000, true, 14));
			}
			//Is it a team cloak request?
			else if (weapon.id == 1061)
			{	//Give each team member the temporary cloak
				ItemInfo cloak = AssetManager.Manager.getItemByName("Cloak");

				foreach (Player p in player._team.ActivePlayers)
					p.inventorySet(cloak, 1);

				//Mark the team as cloaked
				TeamState state = getTeamState(player._team);

				if (state != null)
				{
					state.tickCloakEnd = Environment.TickCount + 12000;
					state.bCloaked = true;
				}
			}
			//Zombie phasing?
			else if (weapon.id == 1122 && player._baseVehicle._type.Id == 116)
			{	//Make a note of his health 
				player.setVar("healthDecay", player._baseVehicle._type.Hitpoints - player._state.health);

				//Turn him into a phase zombie
				player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(117));
			}
			else if (weapon.id == 1123 && player._baseVehicle._type.Id == 117)
			{	//Return him to this world!
				player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(116));

				//Modify his health accordingly
				player.inventoryModify(AssetManager.Manager.getItemByName("MinusHealth"), player.getVarInt("healthDecay"));
			}
			//Zombie spawner?
			else if (weapon.id == 1124 && player._baseVehicle._type.Id == 118)
			{	//Spawn a zombie at this position
				Helpers.ObjectState state = new Helpers.ObjectState();
				Team target = player.getVar("targetTeam") as Team;

				if (target != null)
				{
					state.positionX = posX;
					state.positionY = posY;

					spawnNewZombie(target, ZombieTransitions.HiveZombie, state);
				}
			}
			//Teleport
			else if (weapon.id == 1127 || weapon.id == 1130 || weapon.id == 1131 || weapon.id == 1132 || weapon.id == 1137)
			{	//Warp the player to the location
				player.warp(posX, posY);
			}

			return true;
		}

		/// <summary>
		/// Triggered when a player attempts to repair a vehicle or player
		/// </summary>
		[Scripts.Event("Player.Repair")]
		public bool playerRepair(Player player, ItemInfo.RepairItem item, UInt16 targetVehicle, short posX, short posY)
		{	//Repair kit?
			if (item.id == 36)
			{	//Are we targetting our ammo generator?
				TeamState state = getTeamState(player._team);
				if (targetVehicle == state.ammoGenerator._id)
				{
					player.sendMessage(-1, "You are unable to repair Ammo Generators.");
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Triggered when a player attempts to use a warp item
		/// </summary>
		[Scripts.Event("Player.MakeVehicle")]
		public bool playerMakeVehicle(Player player, ItemInfo.VehicleMaker item, short posX, short posY)
		{	//An engineer bot?
			if (item.id == 16)
			{	//Kill the last bot if he had one
				CombatBot bot = player.getVar("combatBot") as CombatBot;
				if (bot != null)
					bot.kill(null);

				//Create the specified vehicle as a combat bot
				bot = _arena.newBot(typeof(CombatBot), (ushort)item.vehicleID, player._team, player, player._state, this, player) as CombatBot;

				player.setVar("combatBot", bot);
				return false;
			}
			//Destroy engineer bot?
			else if (item.id == 17)
			{
				CombatBot bot = player.getVar("combatBot") as CombatBot;
				if (bot != null)
					bot.kill(null);

				player.setVar("combatBot", null);
				return false;
			}
			//Ammo generator?
			else if (item.id == 32)
			{	//Kill the previous one?
				TeamState state = getTeamState(player._team);
				if (state.ammoGenerator != null)
					state.ammoGenerator.kill(null);

				//Create the new generator
				state.ammoGenerator = _arena.newVehicle(AssetManager.Manager.getVehicleByID(item.vehicleID), player._team, player, player._state) as Computer;
				state.ammoGeneratorLevel = 0;
				state.tickLastAmmoGenerate = 0;

				return false;
			}
			//Healing bot?
			else if (item.id == 39)
			{	//Create the bot
				_arena.newVehicle(AssetManager.Manager.getVehicleByID(item.vehicleID), 
					player._team, player, player._state, null, 
					typeof(HealingBotTurret));

				return false;
			}
			//Laser turret?
			else if (item.id == 40)
			{	//Does he already have a laser turret?
				LaserTurret turret = player.getVar("laserTurret") as LaserTurret;
				if (turret != null)
					turret.destroy(true);
				
				//Create the turret
				turret = _arena.newVehicle(AssetManager.Manager.getVehicleByID(item.vehicleID),
					player._team, player, player._state, null,
					typeof(LaserTurret)) as LaserTurret;

				turret.zz = this;
				player.setVar("laserTurret", turret);

				return false;
			}
			//Snare trap?
			else if (item.id == 41)
			{	//Create the turret
				SnareTrap turret = _arena.newVehicle(AssetManager.Manager.getVehicleByID(item.vehicleID),
					player._team, player, player._state, null,
					typeof(SnareTrap)) as SnareTrap;

				turret.zz = this;

				return false;
			}

			return true;
		}
		#endregion
	}
}