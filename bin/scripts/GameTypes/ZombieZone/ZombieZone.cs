using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Security.Cryptography;
using System.Reflection;

using InfServer.Logic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;

using Assets;
using Axiom.Math;

namespace InfServer.Script.GameType_ZombieZone
{	// Script Class

    public static class ArrayExtensions
    {
        //makes lower <= i < upper
        public static int makeIntBetween(int i, int lower, int upper)
        {
            if (i < lower)
                return lower;
            else if (i >= upper)
                return upper - 1;
            else
                return i;
        }

        //gets the ith element of an array, or closest if out of bounds
        public static T safeGet<T>(this T[] a, int i)
        {
            return a[makeIntBetween(i, 0, a.Length)];
        }

        //gets the (i,j)th element of an array, or closest if out of bounds
        public static T safeGet<T>(this T[,] a, int i, int j)
        {
            i = makeIntBetween(i, 0, a.GetLength(0));
            j = makeIntBetween(j, 0, a.GetLength(1));

            return a[i, j];
        }


    }
    public static class BotExtensions
    {
        public static bool isKing(this Bot bot)
        {
            return bot._type.Name.Contains("King");
        }
    }

    public static class ArenaExtensions
    {
        /// <summary>
        /// Spawns the given item randomly in the specified area
        /// </summary>
        public static void spawnItemInArea(this Arena arena, ItemInfo item, ushort quantity, short x, short y, short radius)
        {	//Sanity
            if (quantity <= 0)
                return;

            //Find a position and spawn it!
            Helpers.randomPositionInArea(arena, radius * 2, ref x, ref y);
            arena.itemSpawn(item, quantity, x, y);
        }

        /*
                public static Dictionary<string, Team> getTeams(this Arena arena)
                {
                    FieldInfo fi = typeof(Arena).GetField("_teams", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (Dictionary<string,Team>)fi.GetValue(arena);
                }
        */


    }
    public static class TeamExtensions
    {
        /// <summary>
        /// Obtains the average position for an entire team
        /// </summary>
        public static void averagePosition(this Team team, out short _posX, out short _posY)
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
        /// <summary>
        /// Obtains the standard deviation of the team's location'
        /// </summary>
        public static int getSeparation(this Team team)
        {	//Find the average location
            short posX, posY;

            team.averagePosition(out posX, out posY);

            int seperation = 0;
            int count = 0;

            foreach (Player p in team.ActivePlayers)
                if (!p.IsDead)
                {
                    int xDiff = (p._state.positionX - posX);
                    int yDiff = (p._state.positionY - posY);

                    seperation += (xDiff * xDiff) + (yDiff * yDiff);
                    count++;
                }

            if (count == 0)
                return -1;

            return (int)Utility.Sqrt(seperation / count);
        }
    }
    public static class PlayerExtensions
    {
        public static int maxHumanClassUpgrades = 3;
        //class upgrades
        public static int[] levelUps = { 50, 200, 500 };            //the amount of skill before you're auto-levelled up

        public static int c_PickupDelayTicks = 3000;   //how much time (ticks) before we allow him to pickup an item after dropping - used for autopickup


        public static Dictionary<int, string[]> defaultSetups = new Dictionary<int, string[]>
        {
            {1,new string[] { "Rpg", "Recoiless Rifle", "Micromissile Launcher", "Incinerator" }},
            {2,new string[] { "Machine Pistol", "Incinerator", "Battle Rifle" }},
            {3,new string[] { "Machine Pistol", "CL5", "Gas Projector" }},
            {4,new string[] { "Assault Rifle", "Maklov G2 ACW", "Incinerator", "Auto Cannon", "Rifle Grenade" }},
            {5,new string[] { "Battle Rifle", "Incinerator" }},
            {9,new string[] { "Hand Maser", "Phantom MK47", "Energy Pistol", "Machine Pistol" }}
        };

        public static void unlearn(this Player player, int skillID)
        {
            List<Player.SkillItem> removes = player._skills.Values.Where(sk => sk.skill.SkillId == skillID).ToList();

            foreach (Player.SkillItem sk in removes)
            {
                player._skills.Remove(sk.skill.SkillId);
                player.Experience += sk.skill.Price;

                player.sendMessage(0, String.Format("Unlearned {0}!  (Exp refund = {1})", sk.skill.Name, sk.skill.Price));
            }

            player.syncState();
        }

        public static bool isZombie(this Player player)
        {
            return !player.IsSpectator && (player._baseVehicle._type.ClassId == 0 || player._baseVehicle._type.ClassId == 99);
        }


        public static void checkUpgradeSkill(this Player p, SkillInfo skill)
        {
            if (p.findSkill(skill.SkillId) != null) //he's got the skill
            {
                int prizeid = skill.InventoryMutators[0].ItemId;
                int upgradedid = (AssetManager.Manager.getItemByID(prizeid) as ItemInfo.UpgradeItem).upgrades[0].outputID;

                //upgrade his crappy gun to new gun, assuming he doesn't have one already
                if (p.getInventoryAmount(upgradedid) == 0)
                    p.inventoryModify(false, prizeid, 1);
            }
        }

        /// <summary>
        /// Reapplies any weapon upgrades the player may have purchased
        /// </summary>
        public static void checkUpgradeSkill(this Player p, string skillName)
        {
            p.checkUpgradeSkill(AssetManager.Manager.getSkillByName(skillName));
        }

        //checks whether to level a human up based on bounty
        public static void checkSkillLevel(this Player player)
        {
            //sanity check
            if (player.isZombie())
            {
                Log.write(TLog.Error, "Tried to check skill level of a zombie.");
                return;
            }

            int skillLevel = player.Bounty;

            int upgradeLevel;
            //determines upgrade level, where 0 means stay at baseLevel
            for (upgradeLevel = 0; upgradeLevel < levelUps.Length; upgradeLevel++)
                if (skillLevel < levelUps[upgradeLevel])
                    break;

            if (upgradeLevel <= 0)  //still level 1, nothing to do here
                return;

            //the actual level that we're willing to upgrade him to
            int totalLevel = upgradeLevel + player.getVarInt("baseClassVehicle");

            //if this is better than what the player has now, upgrade            
            if (totalLevel > player._baseVehicle._type.Id)
            {
                //Note: currently no plans to not-reset health
                player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(totalLevel));
                player.sendMessage(29, "&Level Up!  Now Level " + (upgradeLevel + 1));
            }
        }

        /*gives player class basevehicle, upgrades it based on purchased skills,
        or up to baseVehicle + level, whichever is greater

        baseVehicle is the "level 1", e.g. 10 for marine
        
        setup means we treat it as a default setup, true by default
        */
        public static void upgradeVehicle(this Player player, int baseVehicle, int level = 0, bool setup = true)
        {
            int topVehicle = baseVehicle + maxHumanClassUpgrades; //the max vehicle in this class ladder
            int playerVehicle = player._baseVehicle._type.Id;       //the vehicle currently used by the player

            /*The minimum (inclusive) that we're willing to upgrade him to.
            i.e. if the player already belongs to this class ladder, we're willing to upgrade him only to his current level + 1,
            otherwise, we can do down to baseVehicle
            */
            int minVehicle = baseVehicle <= playerVehicle && playerVehicle <= topVehicle ? playerVehicle + 1 : baseVehicle;

            //start from max possible, and prize only when he meets our criteria
            for (int i = topVehicle; i >= minVehicle; i--)
            {
                //base vehicles don't have skills, so just make it so if we're willing to upgrade him all the way there
                if (i == baseVehicle || i == baseVehicle + level || player.findSkill(i) != null)
                {
                    player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(i));
                    player.setVar("baseClassVehicle", baseVehicle);

                    if (setup)
                    {
                        player.giveDefaultSetup();
                        player.checkUpgradeSkills();
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// Reapplies any weapon upgrades the player may have purchased
        /// </summary>
        public static void checkUpgradeSkills(this Player p)
        {	//Check each skill
            p.checkUpgradeSkill("Upgrade Assault Rifle");
            p.checkUpgradeSkill("Upgrade Assault Rifle+");
            p.checkUpgradeSkill("Upgrade AutoCannon");
            p.checkUpgradeSkill("Upgrade AutoCannon+");
            p.checkUpgradeSkill("Upgrade Battle Rifle");
            p.checkUpgradeSkill("Upgrade Battle Rifle+");
            p.checkUpgradeSkill("Upgrade Incinerator");
            p.checkUpgradeSkill("Upgrade Machine Pistol");
            p.checkUpgradeSkill("Upgrade Machine Pistol+");
            p.checkUpgradeSkill("Upgrade Gas Projector");
            p.checkUpgradeSkill("Upgrade Gas Projector+");
            p.checkUpgradeSkill("Upgrade Maklov G2 ACW");
            p.checkUpgradeSkill("Upgrade Maklov G2 ACW+");
            p.checkUpgradeSkill("Upgrade Phantom MK47");
            p.checkUpgradeSkill("Upgrade Phantom MK47+");
            p.checkUpgradeSkill("Upgrade RPG");
            p.checkUpgradeSkill("Upgrade RPG+");
            p.checkUpgradeSkill("Upgrade RR");
            p.checkUpgradeSkill("Upgrade RR+");
            p.checkUpgradeSkill("Upgrade Rifle Grenade");
        }

        public static void giveDefaultSetup(this Player player)
        {
            int vehid = player._baseVehicle._type.ClassId;

            //this item indicates the player already got the default setup for this classid
            string alreadyGivenName = String.Format("[ClassID {0} Setup]", vehid);
            ItemInfo alreadyGivenItem = AssetManager.Manager.getItemByName(alreadyGivenName);

            if (defaultSetups.ContainsKey(vehid) && (alreadyGivenItem == null || player.getInventoryAmount(alreadyGivenItem.id) == 0))
            {
                string[] defaultSetup = defaultSetups[vehid];

                foreach (string itemName in defaultSetup)
                    player.inventoryModify(false, AssetManager.Manager.getItemByName(itemName), 1);

                if (alreadyGivenItem != null)
                    player.inventoryModify(false, alreadyGivenItem, 1);
                else
                    Log.write(TLog.Error, "[giveDefaultSetup] Attempted to check for nonexistent item: " + alreadyGivenName);
            }
        }

        public static void dropped(this Player player, ItemInfo item, int now)
        {
            List<int> lastDrops = player.getVar("lastDropIDs") as List<int>;

            if (lastDrops == null || now - player.getVarInt("lastDropTick") >= c_PickupDelayTicks)
                lastDrops = new List<int>();

            lastDrops.Add(item.id);

            player.setVar("lastDropIDs", lastDrops);
            player.setVar("lastDropTick", now);
        }

        public static bool pickupTimeOK(this Player player, ItemInfo item, int now)
        {
            if (now - player.getVarInt("lastDropTick") >= c_PickupDelayTicks)   //the last drop was too long ago
                return true;
            else    //check whether this item id was one of the recent drops
            {
                List<int> lastDrops = player.getVar("lastDropIDs") as List<int>;
                return !lastDrops.Contains(item.id);
            }
        }

    }


    /// Provides the interface between the script and arena
    ///////////////////////////////////////////////////////
    public class Script_ZombieZone : Scripts.IScript
    {	///////////////////////////////////////////////////
        // Member Variables
        ///////////////////////////////////////////////////

        public const string ZZ_Version = "April 22 2012 v44 teamswitch no lasers";

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
        public const int _tickGameUpdateRate = 800;     //the rate (ms) at which we update tickers
        public static int numTickers = 4;                 //the number of tickers we have

        //Game state	
        private Dictionary<Team, TeamState> _states;			//The state for each team in the game
        private int _marineTeamCount;							//The amount of marine teams participating

        private int _lastHumanZombieCheck;      //the tick at which we last checked for zombies on human teams

        //Constant Settings

        public static float[] kingWeaponRanges = new float[] { 1.0f, 5.0f, 8.5f, 13.0f, 17.0f, 21.0f };    //ranges of king zombie's weapons'

        public double[] playerZombieVsTeamSizeWeights = { 1, 1.65, 2.5, 3, 3.8 };  //affects chances of player zombies spawning against team of that size
        public static int[,] zombieMultipliers = new int[,] { { 0, 0, 0, 0, 0, 0 }, { 0, 1, 2, 3, 4, 5 }, { 0, 2, 4, 5, 7, 8 }, { 0, 3, 5, 7, 9, 11 }, { 0, 4, 6, 9, 12, 14 }, { 0, 5, 7, 11, 15, 18 }, { 0, 6, 8, 13, 17, 22 } };  //see zombieAmount for more info
        public const int c_playersPerTeam = 5;             //number of players per marine team
        public const int c_humanZombieCheckInverval = 10;   //Time (in seconds) before checking for zombies bugged onto human teams

        //afk checker
        public const int c_afkDistance = 60;           //number of pixels to move before not being considered afk
        public const int c_afkNumGamesValidated = 1;    //you get a challenge request every this number of games

        public const int c_minResAmmo = 100;              //You get at least this much ammo when being ressed (or res cost, whichever is less)

        public const int c_gameStartDelay = 30;					//Delay before a new game is started

        public const short c_startAreaRadius = 112;				//The radius of the start area where a team spawns
        public const int c_startSpacingRadius = 2000;			//The distance between teams we should spawn

        public const int c_minPlayers = 1;						//The minimum amount of players
        public const int c_supplyDropSpacing = 30;				//The seconds between using a supply drop and another spawning
        public const int c_supplyDropMinSpawnDist = 2400;		//The closest a supply drop can spawn to it's marine team
        public const int c_supplyDropMaxSpawnDist = 5500;		//The furthest a supply drop can spawn from it's marine team
        public const short c_supplyDropAreaRadius = 112;		//The size of the unblocked area a supply drop should seek

        public const int c_ammoGenerateDirectDist = 500;        //the distance at which the gen will drop directly into inventory
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
        public const int c_zombieDistanceLeeway = 500;			//The maximum distance leeway a zombie can be from the team before it is respawned
        //public const int c_zombieDistanceMaxSep = 600;			//The maximum amount of team seperation before zombies aren't auto-respawned anymore

        public const int c_szombieMinRespawnDist = 3000;		//The minimum distance the king zombie can be spawned from the players
        public const int c_szombieMaxRespawnDist = 6000;		//The maximum distance the king zombie can be spawned from the players

        public const int c_combatBotPathUpdateInterval = 5000;	//The amount of ticks before an engineer's combat bot updates it's path

        public const int c_campingCheckInterval = 10000;		//The frequency which we check teams for camping
        public const int c_campGrowth = 100;	//The rate at which the count mod grows if the team don't satisfy the threshold
        public const int c_campDecay = 400;		//The rate at which the count mod decays if the team satisfy the threshold
        public const int c_campingDistanceThreshold = 700;		//The distance moved, per check, at which no penalty is incurred
        public const int c_separationCheckInterval = 5000;  //The frequency with which we check teams' separation

        public const int c_vehicleCheckInterval = 10000;		//The frequency which we check if they're in a vehicle

        public const int c_humanKillRewardDistance = 800;   //The max distance a player zombie is rewarded when a human is killed
        public const int c_humanKillXPReward = 826;         //the default reward for player zombies when a human is killed

        public const int c_ammoEaterDefaultPrize = 40;      //Default amount of ammo given to ammo eaters

        public const int c_separationDecreaseInterval = 175;    //the amount that separation decreases per check (can be really high)

        public const float c_kingLevelUpPerPlayer = 0.3f;     //the king level increase per player, on death

        public const int c_SupplyDropHealReduction_PerChemLevel = 5;    //how much less the supply drop heals per chem level on your team



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

            Random _rand;

            public List<ZombieDistraction> distractions;//Potentials distractions the zombie may be attracted to

            public short lastLocationX;				//The last average location of the team
            public short lastLocationY;
            public int tickLastLocationCheck;			//The time at which we last checked the average location
            public int tickLastSeparationCheck;   //The time at which we last checked their separation
            public int camp = 0;                      //measure for how much they're camping
            public int separation = 0;                  //the "average separation" between team players

            public int tickLastVehicleCheck;      //the last time we checked for vehicles
            public int vehicleClass;            //(currently) the increase in multiplier due to being in this vehicle

            public static int maxCamp = 5000;        //hard limit, used for computation of awareness
            public static int maxSeparation = 5000; //hard limit, used for computation of awareness

            public ZombieParameters zombieParams = null;   //the parameters to determine zombie info in transitions; updateZombieParams determines format
            public ZombieParameters maxZombieParams = null;
            public ZombieParameters minZombieParams = null;

            public static int initialPause = 45000;		//How long in ticks the divert zombies skill initially lasts
            public static int pauseDecay = 5000;		  //How much shorter divert gets whenever it is used

            public bool bCloaked;						//Is the team cloaked? (not to be found)
            public int tickCloakEnd;					//When does the cloak expire?

            public bool bPerished;						//Has the team died?
            public int totalZombieKills;				//The team's total amount of zombie kills
            public int totalSkillEarned;				//The total amount of skill earned by the team

            public int tickLastZombieCountIncrease;		//The last time at which we added a zombie

            public Dictionary<Player, List<LaserTurret>> lasers;
            public List<ZombieBot> zombies;		
		//The team's pursuing normal zombies
            public List<Player> zombiePlayers;			//The zombies attacking this team, who are players
            public ZombieBot kingZombie;				//The team's pursuing king zombie

            public ZombieTransitions transitions;				//Used to determine the flow and zombie composition for the game

            public Computer ammoGenerator;				//The team's current ammo generator
            public int ammoGeneratorLevel;				//The upgrade level the generator is currently at
            public int tickLastAmmoGenerate;			//The time at which ammo was last generated

            public Computer supplyDrop;					//The team's active supply drop
            public int supplyDropsFound;				//The amount of supply drops we've found
            public int tickLastSupplyDropFinish;		//The time at which the team's last supply drop was used

            public int tickLastZombieAdd;				//The tick at which the last zombie was added

            public const int c_separationWeightThreshold = 750;     //(distance from average team location)/this : is the increase in spawning weight 

            public static int[] supplyDropHealAmounts = { 25, 35, 50, 70 };     //how much the supply drop heals for each transition level
            public static int[] supplyDropChargeAmounts = { 200, 300, 500, 1000 };     //how much the supply drop charges for each transition level

            //dictionary of possible weapon prizes, and their min and max possible amounts
            public static Dictionary<string, Tuple<int, int>> weaponPrizes = new Dictionary<string, Tuple<int, int>>
            {
                {"Heavy Incinerator",Tuple.Create(-10,0)},
                {"Machine Gun",Tuple.Create(-15,0)},
                {"Railgun",Tuple.Create(10,20)},
                {"Thermal Lance",Tuple.Create(-20,0)},
                {"Maser",Tuple.Create(20,30)}
            };

            public ZombieTransitions.ZombieTransition currentTransition
            {
                get
                {
                    return transitions.currentTransition();
                }
            }

            public VehInfo kingVehicle
            {
                get
                {
                    return AssetManager.Manager.getVehicleByID(kingChooser(zombieParams));
                }
            }


            public int transitionLevel
            {
                get
                {
                    return currentTransition.level;
                }
            }

            public int supplyDropChargeAmount
            {
                get
                {
                    return supplyDropChargeAmounts.safeGet(transitionLevel) + supplyDropsFound * 5;
                }
            }

            public int supplyDropHealAmount
            {
                get
                {
                    return supplyDropHealAmounts.safeGet(transitionLevel) + supplyDropsFound / 2;
                }
            }


            public delegate int KingChooser(ZombieParameters parameters);
            private KingChooser kingLevelChooser;

            public const int minKingVehID = 200;  //safety values
            public const int maxKingVehID = 209;  //safety values

            private KingChooser kingChooser
            {
                get
                {
                    return delegate(ZombieParameters parameters)  //gets actual king vehid (also range checking)
                    {
                        int kingLevel = kingLevelChooser(parameters);

                        if (kingLevel < 0)
                            return minKingVehID;
                        else if (minKingVehID + kingLevel < maxKingVehID)
                            return minKingVehID + kingLevel;
                        else
                            return maxKingVehID;
                    };

                }
            }

            public TeamState(Team t, Random random, ZombieTransitions.Spawner s, KingChooser levelChooser)
            {
                team = t;
                zombies = new List<ZombieBot>();
                zombiePlayers = new List<Player>();

                transitions = new ZombieTransitions(s);
                setTransitions(transitions, random);

                distractions = new List<ZombieDistraction>();

                lasers = new Dictionary<Player, List<LaserTurret>>();

                kingLevelChooser = levelChooser;
                updateZombieParams();

                foreach (Player player in t.ActivePlayers.ToList())
                {
                    player.setVar("lastLocationX", 0);
                    player.setVar("lastLocationY", 0);
                }

                _rand = random;
            }

            public ZombieTransitions.TickerMessage[] humanTickers
            {
                get
                {
                    if (currentTransition == null)
                        return null;

                    return currentTransition.humanTickers;
                }
            }

            public ZombieTransitions.TickerMessage[] zombieTickers
            {
                get
                {
                    if (currentTransition == null)
                        return null;

                    return currentTransition.zombieTickers;
                }
            }

            public int laserLimit(Player player)
            {
                if (player.getVarInt("baseClassVehicle") == 15) //he's an engy
                    return 3;

                return 2;
            }

            public void updateZombieParams()
            {
                if (zombieParams == null)
                {
                    zombieParams = new ZombieParameters();
                    maxZombieParams = new ZombieParameters();
                    minZombieParams = new ZombieParameters();

                    maxZombieParams.camp = maxCamp;
                    maxZombieParams.separation = maxSeparation;

                    minZombieParams.camp = 0;
                    minZombieParams.separation = 0;

                }
                zombieParams.playing = maxZombieParams.playing = minZombieParams.playing = team.ActivePlayerCount;
                zombieParams.camp = Math.Min(camp, maxCamp);
                zombieParams.separation = Math.Min(separation, maxSeparation);
                zombieParams.vehicleClass = vehicleClass;
            }

            public void cloak(int now)
            {
                string cloakName = String.Format("Cloak{0}", transitionLevel);
                ItemInfo cloak = AssetManager.Manager.getItemByName(cloakName);

                if (cloak == null)
                {
                    Log.write(TLog.Error, String.Format("[TeamState.cloak]: item {0} does not exist.", cloakName));
                    return;
                }

                foreach (Player p in team.ActivePlayers.ToList())
                    p.inventorySet(cloak, 1);

                tickCloakEnd = now + cloak.expireTimer * 10;
                bCloaked = true;
            }

            public void zombieMessage(string m, int bong = 0)
            {
                foreach (Player zombie in zombiePlayers.ToList())
                {
                    if (zombie != null && !zombie.IsDead && !zombie.IsSpectator)
                        zombie.sendMessage(bong, m);
                }
            }

            public void pheremone(short x, short y, int now)
            {
                distractions.Add(new ZombieDistraction(x, y, now + 10000 + 1500 * transitionLevel, true, 14 + 4 * transitionLevel));
            }

            public void pauseTransition(int now)
            {
                currentTransition.pauseFor(initialPause - pauseDecay * zombieParams.pauses, now);
                zombieParams.pauses++;
            }

            public void popTransition()
            {
                transitions.pop();
            }

            public void wipeOut() //wipes out all computer zombies currently attacking
            {
                for (int i = zombies.Count - 1; i >= 0; i--)
                    zombies[i].destroy(true, true);

                zombies = new List<ZombieBot>();
            }

            /// <summary>
            /// Obtains an appropriate spawn location for a zombie versus a team
            /// </summary>
            public bool findSpawnLocation(ref Helpers.ObjectState state, short spawnMinDist, short spawnMaxDist)
            {
                int numPlayers = zombieParams.playing;

                if (numPlayers == 0)
                {
                    Log.write(TLog.Error, "Attempted to find spawn location for team with no players.");
                    return false;
                }

                //randomly chooses target player from team, weighted by their distance from the average
                Dictionary<Player, int> players = new Dictionary<Player, int>();

                int totalWeight = 0;

                foreach (Player player in team.ActivePlayers.ToList())
                {
                    //using the max metric for distance, add how many times farther than the separation distance threshold he is, to his weight
                    int weight = 1 + Math.Max(Math.Abs(player._state.positionX - lastLocationX), Math.Abs(player._state.positionY - lastLocationY)) / c_separationWeightThreshold;

                    totalWeight += weight;

                    players.Add(player, weight);
                }

                int selection = _rand.Next(0, totalWeight);

                int sum = 0;
                Player targetPlayer = null;

                foreach (KeyValuePair<Player, int> pair in players)
                {
                    sum += pair.Value;
                    if (selection < sum)
                    {
                        targetPlayer = pair.Key;
                        break;
                    }
                }

                short posX = (short)targetPlayer._state.positionX;
                short posY = (short)targetPlayer._state.positionY;

                //Find an unblocked location around the player
                if (!team._arena.getUnblockedTileInRadius(ref posX, ref posY, spawnMinDist, spawnMaxDist))
                    return false;

                //sets the state spawning location to the unblocked tile
                state.positionX = posX;
                state.positionY = posY;

                return true;
            }

            public ItemInfo weaponPrize()
            {
                ushort throwaway;
                return weaponPrize(out throwaway);
            }

            /// <summary>
            /// Chooses a random weapon prize to give
            /// </summary>
            public ItemInfo weaponPrize(out ushort amount)
            {
                string selection = weaponPrizes.Keys.ToList()[_rand.Next(0, weaponPrizes.Count)];
                ItemInfo prize = AssetManager.Manager.getItemByName(selection);

                //if negative, then we assume entry means (maxItem - entry) 
                int min = weaponPrizes[selection].Item1;
                min = min > 0 ? min : Math.Abs(prize.maxAllowed) + min;

                //if not positive, then we assume entry means (maxItem - entry) 
                int max = weaponPrizes[selection].Item2;
                max = max > 0 ? max + 1 : Math.Abs(prize.maxAllowed) + 1 + max;

                amount = (ushort)_rand.Next(min, max);
                return prize;

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

        public void checkForHumanZombies()
        {
            List<Player> players = _arena.PlayersIngame.ToList();

            foreach (Player player in players)
                if (player.isZombie() && player._team != _zombieHorde)
                {
                    //_zombieHorde.addPlayer(player);
                    player.upgradeVehicle(player.getVarInt("baseClassVehicle"));
                    player.checkSkillLevel();

                    //adds them to this team's original players (since this is likely because mod put them on team)
                    TeamState state = getTeamState(player._team);
                    if (state != null && !state.originalPlayers.Contains(player))
                        state.originalPlayers.Add(player);
                        
                    //Make sure he's not in any team's zombie lists
                    foreach (TeamState s in _states.Values)
                        s.zombiePlayers.Remove(player);
                }
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

                if (now - _lastHumanZombieCheck > c_humanZombieCheckInverval * 1000)
                {
                    _lastHumanZombieCheck = now;
                    checkForHumanZombies();
                }
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

                        //clears afkers
                        List<Player> playersCopy = _arena.PlayersIngame.ToList();

                        //specs player if he failed the afk test
                        foreach (Player player in playersCopy)
                        {
                            if (player.getVarInt("afk") > 0)
                            {
                                //if they're too close to where they were 30s ago, spec them.  if they're dead they can't move so assume pass
                                if (!player.IsDead && Helpers.isInRange(c_afkDistance, player._state.positionX, player._state.positionY, player.getVarInt("afkposx"), player.getVarInt("afkposy")))
                                {
                                    player.spec("spec");
                                    player.sendMessage(0, "You have been sent to spectator mode due to high afkness.");
                                }
                                else  //afk challenge passed
                                    player.setVar("afk", -c_afkNumGamesValidated + 1);
                            }
                        }

                        if (_arena.PlayerCount >= c_minPlayers)
                            _arena.gameStart();

                        else //Stop the game!
                        {
                            _arena.gameReset();
                            _tickGameStart = -1;
                        }

                    }
              );
            }

            if (_bGameRunning && now - _arena._tickGameStarted > 2000)
            {	//Should we update the zombie count ticket?
                if (now - _tickGameLastTickerUpdate > _tickGameUpdateRate)
                {
                    updateGameTickers(now);
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

                    //Check the team's locations for camping
                    if (now - team.tickLastLocationCheck > c_campingCheckInterval)
                    {
                        team.tickLastLocationCheck = now;

                        bool grow = false;

                        foreach (Player player in team.team.ActivePlayers.ToList())
                        {
                            int lastX = player.getVarInt("lastLocationX");
                            int lastY = player.getVarInt("lastLocationY");

                            int distance = (int)Math.Sqrt(Math.Pow(player._state.positionX - lastX, 2) + Math.Pow(player._state.positionY - lastY, 2));

                            if (distance <= c_campingDistanceThreshold)
                                grow = true;

                            player.setVar("lastLocationX", player._state.positionX);
                            player.setVar("lastLocationY", player._state.positionY);
                        }

                        if (grow)
                            team.camp += c_campGrowth;
                        else if (team.camp > c_campDecay)
                            team.camp -= c_campDecay;
                        else
                            team.camp = 0;
                    }

                    //Update the team's separation
                    if (now - team.tickLastSeparationCheck > c_separationCheckInterval)
                    {
                        team.tickLastSeparationCheck = now;

                        int newSeparation = team.team.getSeparation();

                        //separation increases right away but decreases slowly
                        team.separation = Math.Max(newSeparation, team.separation - c_separationDecreaseInterval);

                        //updates average team location
                        team.team.averagePosition(out team.lastLocationX, out team.lastLocationY);
                    }

                    if (now - team.tickLastVehicleCheck > c_vehicleCheckInterval)
                    {
                        team.tickLastVehicleCheck = now;

                        foreach (Player player in team.team.ActivePlayers.ToList())
                        {
                            if (player._occupiedVehicle != null && player._occupiedVehicle._type.Id == 199) //apc
                                team.vehicleClass = 1;
                        }
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
                        {
                            team.ammoGenerator.kill(null);
                            team.ammoGenerator = null;
                        }
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
                }
            }

            return true;
        }
        #region Setup

        /// <summary>
        /// Spawns equipment around the team's spawn point
        /// </summary>
        public void setupSpawnPoint(Team team, short posX, short posY)
        {	//Let's get some class kits down!
            _arena.spawnItemInArea(AssetManager.Manager.getItemByName("Chemist Kit"), 1, posX, posY, c_startAreaRadius);
            _arena.spawnItemInArea(AssetManager.Manager.getItemByName("Engineer Kit"), (ushort)team.ActivePlayerCount, posX, posY, c_startAreaRadius);
            _arena.spawnItemInArea(AssetManager.Manager.getItemByName("Heavy Marine Kit"), 2, posX, posY, c_startAreaRadius);
            _arena.spawnItemInArea(AssetManager.Manager.getItemByName("Squad Leader Kit"), 1, posX, posY, c_startAreaRadius);
            _arena.spawnItemInArea(AssetManager.Manager.getItemByName("Scout Kit"), (ushort)team.ActivePlayerCount, posX, posY, c_startAreaRadius);
        }


        /// <summary>
        /// Sets up a player for a new game as a marine
        /// </summary>
        public void setupMarinePlayer(Player player)
        {	//Make him a marine!
            player.upgradeVehicle(10);

            //Reset his bounty
            player.Bounty = 0;

            player.giveDefaultSetup();

            //Set up his inventory
            player.inventoryModify(false, AssetManager.Manager.getItemByName("removeTempItem"), 9999);
            player.inventoryModify(false, AssetManager.Manager.getItemByName("convertFakeToReal"), 9999);

            //Give him his cash allowance
            player.Cash = c_teamCashStart / player._team.ActivePlayerCount;

            //Give him some starting ammo!
            ItemInfo ammoItem = AssetManager.Manager.getItemByName("Ammo");
            player.inventorySet(false, ammoItem, -ammoItem.maxAllowed);

            //Give him some consumables!
            player.inventorySet(false, AssetManager.Manager.getItemByName("Energizer"), 2);
            player.inventorySet(false, AssetManager.Manager.getItemByName("Frag Grenade"), 2);

            //Make sure his weapons are suitably upgraded
            player.checkUpgradeSkills();

            //Done, sync!
            player.syncInventory();
        }

        /// <summary>
        /// Called when an ammo generator is due to create new ammo
        ///
        /// Prizes ammo to people in 
        /// </summary>
        public void ammoGenerate(TeamState state, Computer ammoGenerator)
        {
            ItemInfo ammo = AssetManager.Manager.getItemByName("Ammo");
            int range = 112;

            //How much ammo do we have in the area?
            int ammoArea = _arena.getItemCountInRange(ammo, ammoGenerator._state.positionX, ammoGenerator._state.positionY, range);

            //total ammo being spawned
            int ammoAmount = c_ammoGenerateAmount + (state.ammoGeneratorLevel * c_ammoGeneratePerLevel);
            int amountRemaining = ammoAmount; //total ammo left to spawn

            List<Player> players = new List<Player>();

            //gets players: the list of teammates in range (using the max metric)
            foreach (Player player in state.team.ActivePlayers)
            {
                if (!player.IsDead && Helpers.isInRange(c_ammoGenerateDirectDist, ammoGenerator._state, player._state))
                {
                    players.Add(player);
                }
            }

            //prizes ammo for each player in range, up to an equal distribution
            int maxPerPlayer = ammoAmount / state.team.ActivePlayerCount;
            foreach (Player player in players)
            {
                int ammoNeeded = -ammo.maxAllowed - player.getInventoryAmount(ammo.id);
                int amountPrized = ammoNeeded <= maxPerPlayer ? ammoNeeded : maxPerPlayer;

                player.inventoryModify(ammo, amountPrized);
                amountRemaining -= amountPrized;
            }


            //drops the rest
            if (amountRemaining > 0 && ammoArea < ammoAmount)
                _arena.itemSpawn(ammo, (ushort)amountRemaining, ammoGenerator._state.positionX, ammoGenerator._state.positionY, (short)range);
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
                _arena.spawnItemInArea(AssetManager.Manager.getItemByName("Ammo"), (ushort)_rand.Next(180, 220), posX, posY, c_supplyDropAreaRadius);

            _arena.spawnItemInArea(AssetManager.Manager.getItemByName("Frag Grenade"), (ushort)_rand.Next(1, 3), posX, posY, c_supplyDropAreaRadius);
            _arena.spawnItemInArea(AssetManager.Manager.getItemByName("Molotov Cocktail"), (ushort)_rand.Next(0, 2), posX, posY, c_supplyDropAreaRadius);
            _arena.spawnItemInArea(AssetManager.Manager.getItemByName("Repulsion Shield"), (ushort)_rand.Next(50, 100), posX, posY, c_supplyDropAreaRadius);
            _arena.spawnItemInArea(AssetManager.Manager.getItemByName("Energizer"), (ushort)_rand.Next(1, 2), posX, posY, c_supplyDropAreaRadius);
            _arena.spawnItemInArea(AssetManager.Manager.getItemByName("Energizer"), (ushort)_rand.Next(1, 2), posX, posY, c_supplyDropAreaRadius);

            if (_rand.Next(4) == 0)  //1/4 chance of spawning fuel cell
                _arena.spawnItemInArea(AssetManager.Manager.getItemByName("Fuel Cell"), 1, posX, posY, c_supplyDropAreaRadius);

            //Give each player some cash
            foreach (Player p in team.ActivePlayers)
            {
                int cashGained = c_playerSupplyDropCash + (state.supplyDropsFound * c_playerSupplyDropCashGrow);
                p.Cash += cashGained;
                p.Experience += c_playerSupplyDropExp;

                p.sendMessage(0, String.Format("Found {0} supply drop! (Cash={1} Exp={2})", supply._state.letterCoord(), cashGained, c_playerSupplyDropExp));
                p.syncState();
            }

            state.supplyDropsFound++;

            int chemistLevel = 0;

            //checks for a chemist on the team
            foreach (Player p in team.ActivePlayers)
            {
                if (p._baseVehicle._type.ClassId == 3 && !p.IsDead)
                {
                    _arena.spawnItemInArea(AssetManager.Manager.getItemByName("Medicine"), (ushort)3, posX, posY, c_supplyDropAreaRadius);
                    chemistLevel = p._baseVehicle._type.Id - p.getVarInt("baseClassVehicle") + 1;
                    break;
                }
            }

            //Heal/charges every player slightly
            foreach (Player p in team.ActivePlayers)
                if (!p.IsDead)
                {
                    ItemInfo.RepairItem healing = AssetManager.Manager.getItemByName("SupplyDrop Healing") as ItemInfo.RepairItem;

                    int amountHealed = Math.Min(healing.repairAmount, p._baseVehicle._type.Hitpoints - p._state.health);
                    int maxHealDesired = Math.Max(0, state.supplyDropHealAmount - chemistLevel * c_SupplyDropHealReduction_PerChemLevel);
                    int amountNegativeHealthPrize = amountHealed - maxHealDesired;

                    p.heal(healing, opener);

                    //subtracts heal amount for each chem level
                    p.inventoryModify(AssetManager.Manager.getItemByName("MinusHealth"), amountNegativeHealthPrize);

                    //charges their energy!
                    p.inventoryModify(AssetManager.Manager.getItemByName("Energy"), state.supplyDropChargeAmount);
                }


            //Let's have a weapon spawn!
            int selection = _rand.Next(0, 4);
            ushort amount;
            ItemInfo weaponPrize = state.weaponPrize(out amount);

            _arena.spawnItemInArea(weaponPrize, amount, posX, posY, c_supplyDropAreaRadius);

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

            team.averagePosition(out posX, out posY);

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

            team.sendArenaMessage(String.Format("&Supplies have been dropped at {0}.", state.supplyDrop._state.letterCoord()), 4);

            if (state.supplyDropsFound == 0)
                team.sendArenaMessage("*See the top-right ticker for more current information.");
        }
        /// <summary>
        /// mastar Function number 1
        /// </summary>
        public void Shuffle(List<TeamState> list)
        {
            RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
            int n = list.Count;

            while (n > 1)
            {
                byte[] box = new byte[1];
                do provider.GetBytes(box);
                while (!(box[0] < n * (Byte.MaxValue / n)));
                int k = (box[0] % n);
                n--;
                TeamState value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        /// <summary>
        /// Spawns a player as a new zombie type
        /// </summary>
        public void spawnZombiePlayer(Player player)
        {	//Determine which team to attack
            if (player == null || player.IsSpectator)
                return;

            TeamState tstate = null;

            List<TeamState> teamsRemaining = new List<TeamState>();

            //gets list of active teams
            foreach (TeamState team in _states.Values)
            {
                if (!team.bPerished && team.team.ActivePlayerCount > 0)
                    teamsRemaining.Add(team);

                //we're rechoosing his team; wipe him out of previous team lists
                team.zombiePlayers.Remove(player);
            }

            Shuffle(teamsRemaining);


            //if only one team left, go for them
            if (teamsRemaining.Count == 1)
            {
                tstate = teamsRemaining.First();
            }
            else
            {
                /*
                    //otherwise, send to team with fewest player-zombies attacking, excluding own team
    
                    int attackers = int.MaxValue;
                    for (int i = 0; i < teamsRemaining.Count; i++)
                    {
                        TeamState team = teamsRemaining[i];

                        if (!team.originalPlayers.Contains(player) && team.zombiePlayers.Count < attackers)
                        {
                            tstate = team;
                            attackers = team.zombiePlayers.Count;
                        }
                    }
                */

                //otherwise, spawn randomly against teams (weighted by playercount), excluding own team
                double totalWeight = 0;

                foreach (TeamState state in teamsRemaining)
                    if (!state.originalPlayers.Contains(player))
                        totalWeight += playerZombieVsTeamSizeWeights[state.team.ActivePlayerCount - 1];

                double randomChoice = _rand.NextDouble() * totalWeight;
                totalWeight = 0;

                foreach (TeamState state in teamsRemaining)
                    if (!state.originalPlayers.Contains(player))
                    {
                        totalWeight += playerZombieVsTeamSizeWeights[state.team.ActivePlayerCount - 1];

                        if (randomChoice <= totalWeight)
                            tstate = state;
                    }
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
            if (target != null && (target.ActivePlayerCount == 4 || target.ActivePlayerCount == 5) && tstate.zombiePlayers.Count == 1)
                try
                {
                    player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(150)); //nightmare zombie, temporary
                }
                catch (System.NullReferenceException)
                {
                    Log.write(TLog.Error, "Null error happening when trying to spawn nightmare zombie");
                    Log.write(TLog.Error, "Player in question was " + player._alias + ".  His vehicle is " + player._baseVehicle._type.Id);
                }
            else
            {
                VehInfo newVeh = null;
                try
                {
                    newVeh = ZombieZoneStats.getPlayableZombie(player);
                }
                catch (System.NullReferenceException)
                {
                    Log.write(TLog.Error, "Null error happening when getting zombie value");
                    Log.write(TLog.Error, "Player in question was " + player._alias + ".  His vehicle is " + player._baseVehicle._type.Id);
                    Log.write(TLog.Error, "The zombie value gotten was " + newVeh.Id);
                }
                try
                {
                    player.setDefaultVehicle(newVeh);
                }
                catch (System.NullReferenceException)
                {
                    Log.write(TLog.Error, "Null error happening while setting player's vehicle to " + newVeh.Id);
                    Log.write(TLog.Error, "Player in question was " + player._alias + ".  His vehicle is " + player._baseVehicle._type.Id);
                }

                try
                {
                    if (AssetManager.Manager.getVehicleByID(610) == newVeh) //if ammo-eater, give him some ammo
                        player.inventorySet(AssetManager.Manager.getItemByName("Ammo"), c_ammoEaterDefaultPrize);
                }
                catch (System.NullReferenceException)
                {
                    Log.write(TLog.Error, "Null error happened while prizing for ammo eater.");
                    Log.write(TLog.Error, "Player in question was " + player._alias + ".  His vehicle is " + player._baseVehicle._type.Id);
                }
            }

            if (target != null)
            {	//Determine a place to spawn
                Helpers.ObjectState state = new Helpers.ObjectState();

                if (!tstate.findSpawnLocation(ref state, (short)c_zombieMinRespawnDist, (short)c_zombieMaxRespawnDist))
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
            if (state.kingZombie != null)
                state.kingZombie.destroy(true);
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
                foreach (Player zombie in state.zombiePlayers.ToList())
                    spawnZombiePlayer(zombie);
            }
        }
        #endregion

        #region Bot Handling
        /// <summary>
        /// Spawns a king zombie on the map, to hunt down a certain team
        /// </summary>
        public void spawnKingZombie(Team target)
        {	//Determine a place to spawn
            Helpers.ObjectState state = new Helpers.ObjectState();

            //Find the team state
            TeamState teamState = getTeamState(target);

            //Create our new zombie
            ZombieType KingZombie = new ZombieType(typeof(KingZombieBot), teamState.kingVehicle,
                delegate(ZombieBot zomb)
                {
                    KingZombieBot z = zomb as KingZombieBot;
                    z.ranges = kingWeaponRanges;
                }
            );

            ZombieBot zombie = spawnNewZombie(target, KingZombie, null, c_szombieMinRespawnDist, c_szombieMaxRespawnDist, false);

            if (zombie == null)
            {
                Log.write(TLog.Error, "Unable to create zombie bot.");
                return;
            }

            zombie.targetTeam = target;
            teamState.kingZombie = zombie;

            target.sendArenaMessage(String.Format("#{0} has spawned at {1}!", zombie._type.Name, zombie._state.letterCoord()));
        }

        /// <summary>
        /// Spawns a new zombie on the map
        /// </summary>
        public ZombieBot spawnNewZombie(Team target, ZombieType ztype, Helpers.ObjectState state = null, int minDist = 0, int maxDist = 0, bool addToList = true)
        {	//Determine a place to spawn?
            if (target == null)
            {
                Log.write(TLog.Error, "Tried to spawn zombie for no team!");
                return null;
            }

            TeamState team = getTeamState(target);

            if (state == null)
            {
                state = new Helpers.ObjectState();

                if (!team.findSpawnLocation(ref state, (short)minDist, (short)maxDist))
                {
                    Log.write(TLog.Error, "Unable to find zombie spawn location.");
                    return null;
                }
            }

            //Create our new zombie
            ZombieBot zombie = _arena.newBot(ztype.classType, ztype.vehicleType, _zombieHorde, null, state, this) as ZombieBot;

            if (zombie == null)
            {
                Log.write(TLog.Error, "Unable to create zombie bot.");
                return null;
            }

            if (ztype.zombieSetup != null)
                ztype.zombieSetup(zombie);

            zombie.targetTeam = target;

            if (addToList)
            {
                //Great! Add it to our list
                zombie.Destroyed += delegate(Vehicle bot)
                {
                    team.zombies.Remove((ZombieBot)bot);
                };

                team.zombies.Add(zombie);
            }

            return zombie;
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
                state.updateZombieParams();             //updates the parameters that determine zombie info

                if (state.currentTransition.finaleStarted(now))
                    state.currentTransition.tryFinalAction(state);

                if (state.currentTransition.isDone(now))
                {
                    if (state.currentTransition.ended != null)
                        state.currentTransition.ended(state);

                    state.popTransition();
                }

                ZombieTransitions.ZombieTransition trans = state.currentTransition;

                if (trans.notStartedYet())
                {
                    trans.start(now, state.zombieParams);

                    if (trans.started != null)
                        trans.started(state);
                }
                else if (now - state.tickLastZombieAdd >= trans.spawnRate(state.zombieParams) && state.zombies.Count < trans.zombieCount(now, state.zombieParams))
                {
                    state.tickLastZombieAdd = now;
                    trans.spawnRandomType(_rand, state.zombieParams);
                }

                //Make sure each team has a king zombie
                if (team.ActivePlayerCount > 0 && (state.kingZombie == null || state.kingZombie.bCondemned))
                    spawnKingZombie(team);
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
            killer.checkSkillLevel();

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

                if (botVictim.isKing())
                {
                    state.zombieParams.kingKills++;
                    state.zombieParams.kingLevel += c_kingLevelUpPerPlayer * state.zombieParams.playing;
                }
            }


        }
        #endregion
        public delegate string TickerChooser(Player p, int ticker, ZombieTransitions.TickerMessage defaultHumanTicker = null, ZombieTransitions.TickerMessage defaultZombieTicker = null, ZombieTransitions.TickerMessage defaultTicker = null);
        #region Statistics
        /// <summary>
        /// Updates the game's ticker info
        /// </summary>
        public void updateGameTickers(int now)
        {	//Create a list of the most successful players
            if (_arena.PlayerCount == 0)
                return;

            //Calculate some total stats for spec
            int totalZombieKills = _states.Values.Sum(state => state.totalZombieKills);
            int totalZombies = _states.Values.Sum(state => state.zombies.Count);

            //being extra-cautious with the copying - not sure if necessary
            List<Player> activePlayers = _arena.PlayersIngame.ToList().OrderByDescending(player => player.getVarInt("skillReached")).ToList();

            if (activePlayers.Count == 0)
                return;

            Player marine = activePlayers[0];
            int skillReached = marine.getVarInt("skillReached");

            
            TickerChooser chooseTicker = delegate(Player p, int tickerNum, ZombieTransitions.TickerMessage defaultHumanTicker, ZombieTransitions.TickerMessage defaultZombieTicker, ZombieTransitions.TickerMessage defaultTicker )
            {
                if(defaultTicker == null)
                    defaultTicker = info => "";

                TeamState state = null;
                ZombieTransitions.TickerMessage ticker = null;
                
                if (p.isZombie())
                {
                    Team target = p.getVar("targetTeam") as Team;
                    state = getTeamState(target);
                    
                    if(state != null && state.zombieTickers[tickerNum] != null)
                        ticker = state.zombieTickers[tickerNum];
                    else
                        ticker = defaultZombieTicker;
                }
                else if (!p.IsSpectator)    //he belongs to some human team!
                {
                    state = getTeamState(p._team);
                    
                    if(state != null)
                    {                        
                        if(tickerNum == 3)
                        {
                            //if zombies are being diverted, use this third ticker instead
                            int resumingIn = state.currentTransition.secondsUntilResume(now);
                            if (resumingIn > 0)
                                return String.Format("Zombies Diverted: {0}:{1}", resumingIn / 60, (resumingIn < 10 ? "0" : "") + (resumingIn % 60));

                        }
                        
                        if(state.humanTickers[tickerNum] != null)
                            ticker = state.humanTickers[tickerNum];
                        else
                            ticker = defaultHumanTicker;
                    }
                }
                
                TickerInfo tinfo = new TickerInfo(p,state, state == null ? -1 : state.currentTransition.secondsUntilEnd(now) );
                
                if (ticker != null) //tries plugging into the resulting ticker, if applicable
                {
                    string result = ticker(tinfo);
                    
                    if(result != null)
                        return result;
                }
                
                return defaultTicker(tinfo);   //default if all else fails
            };     
            
            _arena.setTicker(1, 1, 0,
                player => chooseTicker(player,1,
                    defaultTicker: delegate(TickerInfo info)
                    {
                        if (skillReached == 0)
                            return "No zombie kills yet!";
                                
                        String best = String.Format("Best Marine: {0} ({1})", marine._alias, skillReached);
                        if (_jackpot > 0)
                            return best + String.Format(" / Jackpot ({0})", _jackpot);
                        else
                            return best;
                    }
                )
            );
            

            //Inform them of their nearest supply drop!
            _arena.setTicker(1, 2, 0,
                player => chooseTicker(player,2, defaultTicker: info => String.Format("Total Zombie Kills: {0}", totalZombieKills),
                    defaultHumanTicker: delegate(TickerInfo info)
                    {
                        if (info.state == null || info.state.supplyDrop == null)
                            return null;
                                
                        //Find the player's distance to the drop
                        int distance = (int)((info.player._state.position() - info.state.supplyDrop._state.position()).Length * 100);
                        return String.Format("Supply Drop at {0} (Distance: {1})", info.state.supplyDrop._state.letterCoord(), distance / 16);

                    },
                        
                    defaultZombieTicker: delegate(TickerInfo info)
                    {
                        if(info.targetState == null)
                            return "No Target Team";
                                
                        short posX, posY;
                        info.targetState.team.averagePosition(out posX, out posY);

                        return String.Format("Meatlings at {0}", Helpers.posToLetterCoord(posX, posY));
                    }
                )
                    
            );

            //Update the zombie count!
            _arena.setTicker(1, 3, 0,
                player => chooseTicker (player,3, defaultTicker: info => String.Format("Total Zombies: {0}", totalZombies) ,
                    defaultHumanTicker: delegate(TickerInfo info)
                    {
                        if (info.state == null)
                            return null;
                                
                        ZombieTransitions.ZombieTransition trans = info.state.currentTransition;

                        int awareness = 0;
                        
                        awareness += 50*info.state.zombieParams.camp / info.state.maxZombieParams.camp;
                        awareness += 50*info.state.zombieParams.separation / info.state.maxZombieParams.separation;
                        
                        return String.Format("Zombie Count: {0} / Awareness: {1}%", info.state.zombies.Count, awareness < 0 ? 0 : awareness);

                    },
                        
                    defaultZombieTicker: delegate(TickerInfo info)
                    {
                        if (info.targetState == null || info.targetState.supplyDrop == null)
                           return "Wandering aimlessly.";
                        
                        return String.Format("Blue Box {0}", info.targetState.supplyDrop._state.letterCoord());
                    }
               )
            );
        }

        /// <summary>
        /// Updates the game's ticker info
        /// </summary>
        public void increaseSkillRating(Player player, Vehicle zombie, int weaponID)
        {	//Determine the skill rating of the kill
            int skillRating = _stats.getKillSkillRating(zombie._type.Id);
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
                {
                    state.totalSkillEarned += skillRating;

                    if (state.originalPlayers.Count == 1 && player.Bounty > player.ZoneStat9)

                        player.ZoneStat9 = player.Bounty;
                }

                //Is the killer in a turret or vehicle?
                if (player._occupiedVehicle != null)
                {	//We need to reward the creator a little!
                    if (player._occupiedVehicle._creator != null && player != player._occupiedVehicle._creator)
                    {
                        Player creator = player._occupiedVehicle._creator;
                        skillRating = _stats.getKillSkillRating(zombie._type.Id);

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
                skillRating = _stats.getKillSkillRating(zombie._type.Id);

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
                team = new TeamState(target, _rand, (ztype, state, minDist, maxDist) => spawnNewZombie(target, ztype, state, minDist, maxDist),
                    delegate(ZombieParameters p)
                    {
                        return (int)Math.Round(p.kingLevel + 1.0f * p.camp / TeamState.maxCamp + 1.0f * p.separation / TeamState.maxSeparation, 0);
                    }
                );

                _states[target] = team;
            }

            return team;
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
                            p.inventoryModify(false, AssetManager.Manager.getItemByName("removeTempItem"), 9999);

                            p.setDefaultVehicle(AssetManager.Manager.getVehicleByID(oldVID));
                            p.warp(player);

                            //Lower his health a little
                            p.inventoryModify(AssetManager.Manager.getItemByName("MinusHealth"), 60);

                            foreach (TeamState team in _states.Values)
                            {
                                //Make sure he's not present in any team zombie lists
                                team.zombiePlayers.Remove(p);
                            }

                            /*Give him old ammo amount!  The least ammo you might get is Math.Min(c_minResAmmo,product.Cost)
                            [sanity check to prevent free ammo] */
                            p.inventorySet(false, AssetManager.Manager.getItemByName("Ammo"), Math.Max(p.getVarInt("oldAmmo"), Math.Min(c_minResAmmo, product.PlayerItemNeededQuantity)));

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

                    player.sendMessage(0, "#You'll do it now, Sergeant.  I order you to send the cannon fodder NOW.");
                    player._team.sendArenaMessage("&Zombies have been diverted by external forces for a while.", 6);

                    TeamState state = getTeamState(player._team);
                    if (state != null)
                        state.pauseTransition(Environment.TickCount);
                }

                return false;
            }
            else if (computer._type.Name == "Command Post" && product.Title.StartsWith("Requisition Weapon "))
            {	//Got enough cash?
                if (player.Cash >= product.Cost)
                {
                    TeamState state = getTeamState(player._team);

                    if (state != null)
                    {
                        player.Cash -= product.Cost;
                        player.syncInventory();

                        //Find a weapon and give it to him
                        ItemInfo weaponPrize = state.weaponPrize();

                        player.inventorySet(weaponPrize, (ushort)Math.Abs(weaponPrize.maxAllowed));
                        player.sendMessage(0, weaponPrize.name + " has been requisitioned.");
                    }
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
            if (command.ToLower() == "spawn" && player.PermissionLevel >= InfServer.Data.PlayerPermission.ArenaMod)
            {	//Spawn a zombie on him!
                if (player.isZombie() || player.IsDead || player.IsSpectator)
                    player.sendMessage(0, "Sorry, there might be a conflict of interest here.");
                else
                {
                    for (int i = 0; i < 1; i++)
                    {   	//Use an appropriate zombie vehicle

                        VehInfo zombieVeh = AssetManager.Manager.getVehicleByID(211);

                        //Create our new zombie
                        ZombieBot zombie = _arena.newBot(typeof(ZombieBot), zombieVeh, _arena.Teams.ToList()[0], player, player._state, this) as ZombieBot;
                        //zombie.targetTeam = _arena.PublicTeams.ToList()[1];

                        zombie.targetTeam = player._team;
                    }
                }
            }
            else if (command.ToLower() == "aware" || command.ToLower() == "awareness")
            {
                Player target = recipient == null || recipient.IsSpectator || recipient._team == _zombieHorde && player._team != _zombieHorde ? player : recipient;

                if (target._team == _zombieHorde)
                    player.sendMessage(0, "The zombie horde has no awareness.  The zombies are aware of ALL.");
                else if (target.IsSpectator)
                    return true;
                else
                {
                    TeamState state = getTeamState(target._team);
                    if (state != null)
                    {
                        player.sendMessage(0, String.Format("Team {0} - Zombie Spawning Statistics:", target._team._name));

                        if (state.zombieParams != null)
                        {
                            player.sendMessage(0, "Playing: " + state.zombieParams.playing);
                            player.sendMessage(0, "Camp stat: " + state.zombieParams.camp);
                            player.sendMessage(0, "Separation: " + state.zombieParams.separation);
                            player.sendMessage(0, "Vehicle Class: " + state.zombieParams.vehicleClass);
                        }

                        if (state.currentTransition != null && state.zombieParams != null)
                        {
                            player.sendMessage(0, String.Format("Zombie Count: {0}/{1}", state.zombies.Count, state.currentTransition.zombieCount(Environment.TickCount, state.zombieParams)));
                            player.sendMessage(0, String.Format("Zombie Spawn Rate: every {0} ticks.", state.currentTransition.spawnRate(state.zombieParams)));
                        }
                        else
                            player.sendMessage(0, String.Format("Zombie Count: {0}", state.zombies.Count));
                    }
                }

            }
            else if (command.ToLower() == "version")
            {
                player.sendMessage(0, "Zombie Zone - script version: " + ZZ_Version);
            }
            else if (command.ToLower() == "afk")
            {
                player.sendMessage(0, "&Thanks for confirming that you're still here.");
                player.setVar("afk", -c_afkNumGamesValidated + 1);
            }
            else if (command.ToLower() == "unlearn")
            {
                int skillID;
                bool parsed = Int32.TryParse(payload, out skillID);

                if (!parsed)
                {
                    //if (player.isZombie())
                    player.unlearn(player._baseVehicle._type.Id);
                    //else
                    //    player.sendMessage(0, "The skill number you've entered does not match anything.");
                }
                else
                {
                    SkillInfo s = _arena._server._assets.getSkillByID(skillID);
                    if (s == null)
                        player.sendMessage(0, "The skill number you've entered does not match anything.");
                    else
                    {
                        player.unlearn(skillID);
                    }
                }
            }
            else if (command.ToLower() == "geninfo")
            {
                TeamState state = getTeamState(player._team);
                string teamGenMessage = state.ammoGenerator != null ? "Your team has an ammo generator at " + state.ammoGenerator._state.letterCoord() : "Only one is allowed per team";

                player.sendMessage(0, String.Format("Ammo generators will deposit ammo directly into your inventory if you are within {0} distance.  Otherwise, or if you're already full, it will drop your share at its location.  They naturally decay, and have a limited number of drops. {1}.", c_ammoGenerateDirectDist, teamGenMessage));
            }
            else if (command.ToLower() == "replacegen")
            {
                int ammogenid = 32;

                if (player.isZombie())
                    player.sendMessage(0, "Sorry, Zombies can't use ammo gens.  The devs are working on your opposable thumbs, though.. we'll see.");
                else if (player.getInventoryAmount(ammogenid) <= 0)
                {
                    int cashCost = AssetManager.Manager.getItemByID(ammogenid).buyPrice;

                    if (player.Cash < cashCost)
                        player.sendMessage(0, "You do not have enough cash to make this auto-purchase.");
                    else if (createAmmoGenerator(player, replace: true))
                    {
                        player.Cash -= cashCost;
                        player.syncInventory();
                    }
                }
                else if (createAmmoGenerator(player, replace: true))
                    player.inventoryModify(false, ammogenid, -1);

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
                victim.setVar("oldAmmo", victim.getInventoryAmount(AssetManager.Manager.getItemByName("Ammo").id));

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

                //prizes all nearby human zombies the xp reward
                foreach (Player zombie in _zombieHorde.ActivePlayers.ToList())
                {
                    if (Helpers.isInRange(c_humanKillRewardDistance, zombie._state, victim._state))
                    {
                        zombie.sendMessage(0, String.Format("&{0} killed!  Reward: {1} experience.", victim._alias, c_humanKillXPReward));
                        zombie.Experience += c_humanKillXPReward;
                        zombie.syncState();
                    }
                }
            }
            //No, a zombie! Reward the killer?
            else if (killer != null)
            {
                zombieKilled(killer, victim._baseVehicle._type.Id, 0, victim, null);
                victim.ZoneStat6++;
            }

            return false;
        }

        [Scripts.Event("Player.ItemDrop")]
        public bool playerItemDrop(Player player, ItemInfo item, ushort quantity)
        {
            player.dropped(item, Environment.TickCount);
            return true;
        }
        /// <summary>
        /// Triggered when a player requests to pick up an item
        /// </summary>
        [Scripts.Event("Player.ItemPickup")]
        public bool playerItemPickup(Player player, Arena.ItemDrop drop, ushort quantity)
        {	//Are they any kit items?

            //check if he dropped this item too soon ago (auto-pickup check)
            if (!player.pickupTimeOK(drop.item, Environment.TickCount))
                return false;

            if (drop.item.name == "Engineer Kit")
            {	//Only marines can change class
                if (player._baseVehicle._type.Id < 10 || player._baseVehicle._type.Id > 14)
                    return false;

                player.inventorySet(AssetManager.Manager.getItemByName("Engineering Bench"), 1);
                player.inventoryModify(AssetManager.Manager.getItemByName("convertRealToFakeEng"), 99);

                player.upgradeVehicle(15);
            }
            else if (drop.item.name == "Heavy Marine Kit")
            {	//Only marines can change class
                if (player._baseVehicle._type.Id < 10 || player._baseVehicle._type.Id > 14)
                    return false;

                player.inventoryModify(AssetManager.Manager.getItemByName("convertRealToFakeHeavy"), 99);

                player.upgradeVehicle(20);
            }
            else if (drop.item.name == "Chemist Kit")
            {	//Only marines can change class
                if (player._baseVehicle._type.Id < 10 || player._baseVehicle._type.Id > 14)
                    return false;

                //Give him some heals, depending on the amount of teammates
                player.inventorySet(false, AssetManager.Manager.getItemByName("Laboratory"), 1);
                player.inventorySet(AssetManager.Manager.getItemByName("Medicine"), player._team.ActivePlayerCount);
                player.inventoryModify(AssetManager.Manager.getItemByName("convertRealToFakeChem"), 99);

                player.upgradeVehicle(25);
            }
            else if (drop.item.name == "Squad Leader Kit")
            {	//Only marines can change class
                if (player._baseVehicle._type.Id < 10 || player._baseVehicle._type.Id > 14)
                    return false;

                player.inventorySet(AssetManager.Manager.getItemByName("Command Post"), 1);
                player.inventoryModify(AssetManager.Manager.getItemByName("convertRealToFakeSarge"), 99);

                player.upgradeVehicle(30);
            }
            else if (drop.item.name == "Scout Kit")
            {	//Only marines can change class
                if (player._baseVehicle._type.Id < 10 || player._baseVehicle._type.Id > 14)
                    return false;

                player.inventoryModify(AssetManager.Manager.getItemByName("convertRealToFakeScout"), 99);

                player.upgradeVehicle(34);
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
        {
            player.setVar("afk", -c_afkNumGamesValidated + 1); //set him as having "won" afk challenge (he just unspecced)
            player.setVar("baseClassVehicle", 10); //set him as a marine (this is to avoid possible errors)


            //Is the game in progress?
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
            compensateForDeparted(player);
            return true;
        }

        //splits cash between remaining teammates
        public void compensateForDeparted(Player player)
        {
            int cash = player.Cash;
            Team team = player._team;

            if (team != null && team != _zombieHorde)
            {
                List<Player> remaining = team.ActivePlayers.Where(p => p != player).ToList();
                int numPlayers = remaining.Count;

                if (numPlayers <= 0)
                    return;

                int compensation = 1 + cash / numPlayers;

                foreach (Player p in remaining)
                {
                    p.sendMessage(0, String.Format("&{0} left the game!  Compensation: {1}", player._alias, compensation));
                    p.Cash += compensation;
                    p.syncState();
                }
            }
        }

        //semi-balanced scramble: team sizes differ by no more than 1
        public void scramble(IEnumerable<Player> unorderedPlayers, List<Team> teams, int maxPerTeam)
        {
            List<Player> players = unorderedPlayers.OrderBy(plyr => _rand.Next(0, 500)).ToList();

            //gets the minimum number of teams we need to fit our players
            int numTeams = players.Count / maxPerTeam + (players.Count % maxPerTeam == 0 ? 0 : 1);

            //adds our players to these teams in team-order
            for (int i = 0; i < players.Count; i++)
            {
                Player p = players[i];
                teams[i % numTeams].addPlayer(p);
                p.ZoneStat3++;
            }
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

            //List<Team> publicTeams = _arena.getTeams().Values.Where(team => team.IsPublic && team._name != "Zombie Horde").ToList();
            //scramble(_arena.PlayersIngame, _arena.PublicTeams.Where(team => team._name != "Zombie Horde" && team._name != "spec").ToList(), c_playersPerTeam);
            scramble(_arena.PlayersIngame, _arena.Teams.Where(team => team.IsPublic && team._name != "Zombie Horde" && team._name != "spec").ToList(), c_playersPerTeam);

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

            //After spawning all the teams, spawn a kingzombie for each team
            /*foreach (Team team in _arena.Teams)
                if (team.ActivePlayerCount > 0)  //let the normal spawner take care of this
                    spawnKingZombie(team);*/

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

                    TeamState victorState = getTeamState(_victoryTeam);

                    if (victorState != null)
                        foreach (Player p in victorState.originalPlayers)
                        {
                            p.ZoneStat4++;

                            p.Experience += winReward;
                            p.syncState();
                            p.sendMessage(0, String.Format("&You've received {0} experience for being on the winning team.", winReward));
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
            //_arena.breakdown(true);

            //Reset all custom vars
            foreach (Player p in _arena.Players)
            {
                if (p.IsSpectator)
                    p.resetVars();
                else
                {
                    int prevAFK = p.getVarInt("afk");
                    int newAFK = prevAFK + 1;

                    if (newAFK > 0)
                    {
                        p.sendMessage(0, "=");
                        p.sendMessage(0, "==");
                        p.sendMessage(7, String.Format("&Warning: staying in this location for {0} more seconds will put you into spectator mode.", c_gameStartDelay));
                        p.sendMessage(0, "(a.k.a.. MOVE IT!)");
                    }

                    p.resetVars();
                    p.setVar("afk", newAFK);
                    //p.sendMessage(0,"Setting afk to " + newAFK);

                    p.setVar("afkposx", p._state.positionX);
                    p.setVar("afkposy", p._state.positionY);
                }
            }
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

        //gets zombie amount for this multiplier, accounts for vehicle class as well
        public static int zombieAmount(ZombieParameters parameters, int multiplier, int multiplyByTeamSize = 0)
        {
            return zombieMultipliers.safeGet(multiplier + parameters.vehicleClass, parameters.playing) + multiplyByTeamSize * parameters.playing;
        }

        public static void suddenDeathWave(ZombieTransitions transitions, List<ZombieType> composition, int time = 15)
        {
            ZombieTransitions.ZombieTransition trans = transitions.newTransition();
            trans.finalZombieCount = parameters => zombieAmount(parameters, 4, (parameters.camp / 800) + (parameters.separation / 600));
            trans.spawnComposition = ZombieTransitions.constComposition(composition);
            trans.finalTime = time;
            trans.spawnRate = parameters => Math.Max(50, 1000 - 50 * parameters.playing - 50 * (parameters.camp / 500) - 50 * (parameters.separation / 500));

            trans.minSpawnDistance = parameters => Math.Max(450, 1000 - 50 * parameters.playing - 50 * (parameters.camp / 500) - 50 * (parameters.separation / 500));
            trans.maxSpawnDistance = parameters => Math.Max(650, 1200 - 50 * parameters.playing - 50 * (parameters.camp / 500) - 50 * (parameters.separation / 500));
            trans.started = delegate(Script_ZombieZone.TeamState state)
            {
                state.team.sendArenaMessage("You think you hear something.", 3); //thunderstorm 11
                state.zombieMessage("You think you remember something.", 3);
            };
            trans.ended = delegate(Script_ZombieZone.TeamState state)
            {
                state.wipeOut();
            };
            trans.zombieTickers[1] = info => info.time > 2 ? "Wipe the throwbacks out!" : "Until all that remains is Silence.";
            trans.zombieTickers[2] = delegate(TickerInfo info)
            {
                short posX, posY;
                info.targetState.team.averagePosition(out posX, out posY);

                return String.Format("Humans at {0}", Helpers.posToLetterCoord(posX, posY));
            };
            trans.humanTickers[3] = info => String.Format("Sudden Death Wave: {0}", info.timeString);

            transitions.gracePeriod(20);
        }

        public static void setTransitions(ZombieTransitions transitions, Random random)
        {
            ZombieTransitions.ZombieTransition trans = transitions.gracePeriod(10);
            trans.humanTickers[3] = info => String.Format("Initial Grace Period: {0}", info.timeString);

            //////////DUEL BOT TEST
            /* ZombieTransitions.ZombieTransition transy = transitions.newTransition();
            transy.spawnComposition = parameters => (new List<ZombieType>()).addType(ZombieTransitions.HiveZombie, 10).addType(ZombieTransitions.RepulsorZombie, parameters.camp / 1000);  //if they're camping too hard, add some predators into the mix           
            transy.initialTime = 20;
            transy.finalTime = 30;
            transy.initialZombieCount = ZombieTransitions.constInt(1);
            transy.finalZombieCount = parameters => zombieAmount(parameters, 3, (parameters.camp / 1000) + (parameters.separation / 800));
            transy.spawnRate = parameters => Math.Max(50, 1000 - 50 * parameters.playing - 60 * (parameters.camp / 1000) - 120 * (parameters.separation / 600));

            transitions.gracePeriod(25);*/

            ///////////////////////////////////////


            //alien cannon fodder, start from 1, increase to team size
            trans = transitions.newTransition();
            trans.initialZombieCount = ZombieTransitions.constInt(1);
            trans.initialTime = 30;
            trans.spawnComposition = ZombieTransitions.constComposition((new List<ZombieType>()).addType(ZombieTransitions.AlienZombie, 1).addType(ZombieTransitions.HumanZombie, 1));
            trans.finalZombieCount = p => zombieAmount(p, 1);
            trans.finalTime = 10;

            trans.spawnRate = ZombieTransitions.constInt(600);
            trans.minSpawnDistance = ZombieTransitions.constInt(1000);
            trans.maxSpawnDistance = ZombieTransitions.constInt(1200);

            trans.started = delegate(Script_ZombieZone.TeamState state)
            {
                state.team.sendArenaMessage("The zombies are coming!  Let's get a move on!");
            };

            transitions.gracePeriod(15);


            //adds some suicides, still relaxed
            trans = transitions.newTransition();
            trans.initialWave = ZombieTransitions.emptyComposition();
            trans.initialZombieCount = p => zombieAmount(p, 1);
            trans.initialTime = 30;

            trans.spawnComposition = ZombieTransitions.constComposition((new List<ZombieType>()).addType(ZombieTransitions.AlienZombie, 2).addType(ZombieTransitions.HumanZombie, 2).addType(ZombieTransitions.SuicideZombie, 1));
            trans.finalZombieCount = p => zombieAmount(p, 1);
            trans.finalTime = 20;


            transitions.gracePeriod(20);

            //melee wave, we start to care if they're separated and/or/basing
            trans = transitions.newTransition();
            trans.initialWave = parameters => (new List<ZombieType>()).addType(ZombieTransitions.AlienZombie, zombieAmount(parameters, 1, (parameters.camp / 2000) + (parameters.separation / 2000)));

            transitions.gracePeriod(20);

            //a few ranged
            trans = transitions.newTransition();
            trans.spawnComposition = ZombieTransitions.constComposition((new List<ZombieType>()).addType(ZombieTransitions.AlienZombie, 2).addType(ZombieTransitions.HumanZombie, 2).addType(ZombieTransitions.SuicideZombie, 1).addType(ZombieTransitions.RangedZombie, 2));
            trans.finalZombieCount = parameters => zombieAmount(parameters, 2, (parameters.camp / 2000) + (parameters.separation / 2000));
            trans.initialZombieCount = parameters => zombieAmount(parameters, 2, (parameters.camp / 2000) + (parameters.separation / 2000)) / 2;
            trans.initialTime = 40;
            trans.finalTime = 20;

            transitions.gracePeriod(20);

            //suicide rush
            trans = transitions.newTransition();
            trans.initialWave = parameters => (new List<ZombieType>()).addType(ZombieTransitions.SuicideZombie, zombieAmount(parameters, 2, (parameters.camp / 1700) + (parameters.separation / 1700)));

            transitions.gracePeriod(25);

            //predators and suicides
            trans = transitions.newTransition();
            trans.spawnComposition = parameters => (new List<ZombieType>()).addType(ZombieTransitions.AlienZombie, 2).addType(ZombieTransitions.HumanZombie, 1).addType(ZombieTransitions.PredatorZombie, 1).addType(ZombieTransitions.SuicideZombie, 1).addType(ZombieTransitions.InfectedZombie, (parameters.camp / 2000) + (parameters.separation / 2000));
            trans.initialTime = 20;
            trans.finalTime = 50;
            trans.initialZombieCount = parameters => zombieAmount(parameters, 1, (parameters.camp / 1000) + (parameters.separation / 1000));
            trans.finalZombieCount = parameters => (int)(1.5 * zombieAmount(parameters, 1, (parameters.camp / 1000) + (parameters.separation / 1000)));
            trans.spawnRate = parameters => Math.Max(150, 1000 - 50 * parameters.playing - 100 * (parameters.camp / 1000) - 50 * (parameters.separation / 500) - 100 * parameters.vehicleClass);

            transitions.gracePeriod(20);

            int deathWave = random.Next(0, 2); //randomly chooses when between the next 3 waves to have a deathwave

            List<ZombieType> deathWave1Composition = (new List<ZombieType>()).addType(ZombieTransitions.RepulsorZombie, 4).addType(ZombieTransitions.SuicideZombie, 2).addType(ZombieTransitions.RangedZombie, 2).addType(ZombieTransitions.InfectedZombie, 1);

            if (deathWave == 0)
                suddenDeathWave(transitions, deathWave1Composition);

            //instant melee invasion
            trans = transitions.newTransition();
            trans.initialWave = parameters => (new List<ZombieType>()).addType(ZombieTransitions.AlienZombie, zombieAmount(parameters, 2)).addType(ZombieTransitions.HumanZombie, zombieAmount(parameters, 1)).addType(ZombieTransitions.InfectedZombie, zombieAmount(parameters, 0, (parameters.camp / 500) + (parameters.separation / 500)));
            trans.started = delegate(Script_ZombieZone.TeamState state)
            {
                state.team.sendArenaMessage("@The zombies are AMASSING!");
            };

            transitions.gracePeriod(20);

            if (deathWave == 1)
                suddenDeathWave(transitions, deathWave1Composition);

            //hives!
            trans = transitions.newTransition();
            //if they're camping too hard, add some repulsion into the mix
            //if they're spreading out too far, throw in some infected
            trans.spawnComposition = parameters => (new List<ZombieType>()).addType(ZombieTransitions.HiveZombie, 10).addType(ZombieTransitions.RepulsorZombie, parameters.camp / 1000).addType(ZombieTransitions.InfectedZombie, parameters.separation / 1000);
            trans.initialTime = 20;
            trans.finalTime = 30;
            trans.initialZombieCount = ZombieTransitions.constInt(1);
            trans.finalZombieCount = parameters => zombieAmount(parameters, 3, (parameters.camp / 1000) + (parameters.separation / 800));
            trans.spawnRate = parameters => Math.Max(50, 1000 - 50 * parameters.playing - 60 * (parameters.camp / 1000) - 120 * (parameters.separation / 600) - 100 * parameters.vehicleClass);

            transitions.gracePeriod(25);

            if (deathWave == 2)
                suddenDeathWave(transitions, deathWave1Composition);

            transitions.currentLevel = 1;

            //slow, ranged and suicide combo
            trans = transitions.newTransition();
            trans.spawnComposition = parameters => (new List<ZombieType>()).addType(ZombieTransitions.RepulsorZombie, 1 + (parameters.camp / 600)).addType(ZombieTransitions.PredatorZombie, 1).addType(ZombieTransitions.DerangedZombie, 1 + (parameters.separation / 800)).addType(ZombieTransitions.SuicideZombie, 2).addType(ZombieTransitions.InfectedZombie, 1 + (parameters.separation / 900));
            trans.finalTime = 50;
            trans.finalZombieCount = parameters => zombieAmount(parameters, 2, (parameters.camp / 1200) + (parameters.separation / 1000));
            trans.spawnRate = parameters => Math.Max(100, 1000 - 50 * parameters.playing - 60 * (parameters.camp / 800) - 140 * (parameters.separation / 500) - 150 * parameters.vehicleClass);
            trans.ended = delegate(Script_ZombieZone.TeamState state)
            {

                state.team.sendArenaMessage("The zombies are regrouping; prepare yourself!");
            };

            transitions.gracePeriod(20);

            //PHASE 2
            trans = transitions.newTransition();
            trans.spawnComposition = parameters => (new List<ZombieType>()).addType(ZombieTransitions.KamikazeZombie, 5).addType(ZombieTransitions.DerangedZombie, 5).addType(ZombieTransitions.AcidZombie, parameters.camp / 700).addType(ZombieTransitions.RepulsorZombie, parameters.camp / 1700);
            trans.finalTime = 40;
            trans.initialTime = 10;
            trans.initialZombieCount = ZombieTransitions.constInt(1);
            trans.finalZombieCount = parameters => zombieAmount(parameters, 2, (parameters.camp / 1000) + (parameters.separation / 800));
            trans.spawnRate = parameters => Math.Max(100, 1000 - 50 * parameters.playing - 50 * (parameters.camp / 700) - 150 * (parameters.separation / 500) - 200 * parameters.vehicleClass);

            transitions.gracePeriod(20);

            trans = transitions.newTransition();
            trans.spawnComposition = parameters => (new List<ZombieType>()).addType(ZombieTransitions.KamikazeZombie, 5).addType(ZombieTransitions.DerangedZombie, 5 + (parameters.separation / 2000)).addType(ZombieTransitions.AcidZombie, 1 + (parameters.camp / 600)).addType(ZombieTransitions.AsgardianZombie, 2).addType(ZombieTransitions.RepulsorZombie, parameters.camp / 500).addType(ZombieTransitions.InfestedZombie, 1 + (parameters.separation / 900) * 2);
            trans.initialTime = 10;
            trans.finalTime = 40;
            trans.initialZombieCount = ZombieTransitions.constInt(1);
            trans.finalZombieCount = parameters => zombieAmount(parameters, 3, (parameters.camp / 1000) + (parameters.separation / 1000));
            trans.spawnRate = parameters => Math.Max(100, 1000 - 50 * parameters.playing - 50 * (parameters.camp / 1000) - 150 * (parameters.separation / 500) - 200 * parameters.vehicleClass);

            transitions.gracePeriod(20);

            //hive/acid swarm
            trans = transitions.newTransition();
            trans.spawnComposition = parameters => (new List<ZombieType>()).addType(ZombieTransitions.LairZombie, 7).addType(ZombieTransitions.AcidZombie, parameters.camp < 3500 ? 1 : 0).addType(ZombieTransitions.DoomZombie, parameters.camp < 3500 ? 0 : 1).addType(ZombieTransitions.RepulsorZombie, (parameters.camp / 1500));
            trans.initialTime = 10;
            trans.initialZombieCount = ZombieTransitions.constInt(1);
            trans.finalTime = 50;
            trans.finalZombieCount = parameters => zombieAmount(parameters, 4, (parameters.camp / 800) + (parameters.separation / 700));
            trans.spawnRate = parameters => Math.Max(60, 1000 - 50 * parameters.playing - 50 * (parameters.camp / 1000) - 150 * (parameters.separation / 500) - 200 * parameters.vehicleClass);

            transitions.gracePeriod(20);

            //plus a few ranged
            trans = transitions.newTransition();
            trans.spawnComposition = parameters => (new List<ZombieType>()).addType(ZombieTransitions.LairZombie, 7).addType(ZombieTransitions.AcidZombie, 1 - (parameters.camp > 1500 ? 1 : 0)).addType(ZombieTransitions.DoomZombie, (parameters.camp > 1500 ? 1 : 0)).addType(ZombieTransitions.AsgardianZombie, 1 - (parameters.camp > 1500 ? 1 : 0)).addType(ZombieTransitions.KryptonianZombie, (parameters.camp > 1500 ? 1 : 0)).addType(ZombieTransitions.DerangedZombie, 2 - (parameters.camp > 2000 ? 1 : 0)).addType(ZombieTransitions.RageZombie, (parameters.camp > 2000 ? 1 : 0)).addType(ZombieTransitions.RepulsorZombie, (parameters.camp / 2000));
            trans.initialTime = 10;
            trans.initialZombieCount = ZombieTransitions.constInt(1);
            trans.finalTime = 50;
            trans.finalZombieCount = parameters => zombieAmount(parameters, 3, (parameters.camp / 900) + (parameters.separation / 800));
            trans.spawnRate = parameters => Math.Max(60, 1000 - 50 * parameters.playing - 50 * (parameters.camp / 1000) - 150 * (parameters.separation / 500) - 200 * parameters.vehicleClass);

            transitions.gracePeriod(20);

            deathWave = random.Next(0, 3); //randomly chooses when between the next 4 waves to have a deathwave

            List<ZombieType> deathWave2Composition = (new List<ZombieType>()).addType(ZombieTransitions.RepulsorZombie, 2).addType(ZombieTransitions.DestroyerZombie, 3).addType(ZombieTransitions.KryptonianZombie, 2).addType(ZombieTransitions.RageZombie, 3).addType(ZombieTransitions.DoomZombie, 4).addType(ZombieTransitions.InfatuatedZombie, 1);

            transitions.currentLevel = 2;

            if (deathWave == 0)
                suddenDeathWave(transitions, deathWave2Composition, 20);

            //make repulsors more prominent, hives less
            trans = transitions.newTransition();
            trans.spawnComposition = parameters => (new List<ZombieType>()).addType(ZombieTransitions.LairZombie, parameters.separation / 1500).addType(ZombieTransitions.InfestedZombie, parameters.separation / 1500).addType(ZombieTransitions.AcidZombie, 1).addType(ZombieTransitions.AsgardianZombie, 1).addType(ZombieTransitions.DerangedZombie, 1).addType(ZombieTransitions.RepulsorZombie, 1 + (parameters.camp / 1000));
            trans.initialTime = 10;
            trans.finalTime = 50;
            trans.finalZombieCount = parameters => zombieAmount(parameters, 2, (parameters.camp / 1000) + (parameters.separation / 1000));
            trans.initialZombieCount = parameters => zombieAmount(parameters, 2, (parameters.camp / 1000) + (parameters.separation / 1000)) / 2;
            trans.spawnRate = parameters => Math.Max(100, 1000 - 50 * parameters.playing - 50 * (parameters.camp / 1000) - 150 * (parameters.separation / 500) - 250 * parameters.vehicleClass);
            trans.started = delegate(Script_ZombieZone.TeamState state)
            {

                if (state.team.ActivePlayerCount <= 2)
                    state.team.sendArenaMessage("&The loneliness is starting to make you slip; you swear you can hear another voice inside your head.");

            };

            transitions.gracePeriod(20);

            if (deathWave == 1)
                suddenDeathWave(transitions, deathWave2Composition, 20);

            //infected attack
            trans = transitions.newTransition();
            trans.spawnComposition = parameters => (new List<ZombieType>()).addType(ZombieTransitions.InfatuatedZombie, 7 - parameters.camp / 714).addType(ZombieTransitions.InfatuatedZombie, parameters.camp / 714 + parameters.separation / 1000).addType(ZombieTransitions.DoomZombie, parameters.camp / 2500).addType(ZombieTransitions.RepulsorZombie, parameters.camp / 1400);
            trans.initialTime = 5;
            trans.initialZombieCount = ZombieTransitions.constInt(1);
            trans.finalTime = 45;
            trans.finalZombieCount = parameters => zombieAmount(parameters, 3, (parameters.camp / 800) + (parameters.separation / 700));
            trans.spawnRate = parameters => Math.Max(60, 1000 - 50 * parameters.playing - 50 * (parameters.camp / 900) - 150 * (parameters.separation / 500) - 300 * parameters.vehicleClass);

            transitions.gracePeriod(20);

            if (deathWave == 2)
                suddenDeathWave(transitions, deathWave2Composition, 20);

            //balanced, large army
            trans = transitions.newTransition();
            trans.spawnComposition = p => (new List<ZombieType>()).addType(ZombieTransitions.AcidZombie, 1 - (p.camp / 3600)).addType(ZombieTransitions.DoomZombie, p.camp / 3600).addType(ZombieTransitions.AsgardianZombie, 1 - (p.camp / 2700)).addType(ZombieTransitions.KryptonianZombie, p.camp / 2700).addType(ZombieTransitions.DisruptorZombie, 1).addType(ZombieTransitions.RepulsorZombie, 1 + (p.camp / 2000)).addType(ZombieTransitions.DerangedZombie, 2 - (p.camp / 2000) - (p.separation / 1500)).addType(ZombieTransitions.RageZombie, (p.camp / 2000) + (p.separation / 1500)).addType(ZombieTransitions.KamikazeZombie, 1 - (p.camp / 2000)).addType(ZombieTransitions.DestroyerZombie, p.camp / 2000);
            trans.initialTime = 10;
            trans.initialZombieCount = parameters => zombieAmount(parameters, 3, (parameters.camp / 2000) + (parameters.separation / 2000)) / 2;
            trans.finalTime = 50;
            trans.finalZombieCount = parameters => zombieAmount(parameters, 3, (parameters.camp / 2000) + (parameters.separation / 2000));
            trans.spawnRate = parameters => Math.Max(150, 1000 - 100 * parameters.playing - 70 * (parameters.camp / 1000) - 100 * (parameters.separation / 300) - 300 * parameters.vehicleClass);
            trans.ended = delegate(Script_ZombieZone.TeamState state)
            {
                state.team.sendArenaMessage("We're getting close to the end, I think.");
            };

            transitions.gracePeriod(15);

            if (deathWave == 3)
                suddenDeathWave(transitions, deathWave2Composition, 20);

            //massive acid attack
            trans = transitions.newTransition();
            trans.initialWave = (p) => (new List<ZombieType>()).addType(ZombieTransitions.AcidZombie, zombieAmount(p, (5000 - p.camp) / 1500)).addType(ZombieTransitions.DoomZombie, zombieAmount(p, p.camp / 1500));
            trans.spawnComposition = (p) => (new List<ZombieType>()).addType(ZombieTransitions.AcidZombie, Math.Max(3, (5000 - p.camp) / 1000)).addType(ZombieTransitions.DoomZombie, p.camp / 1000).addType(ZombieTransitions.RepulsorZombie, p.camp / 2000);
            trans.initialTime = 10;
            trans.initialZombieCount = parameters => zombieAmount(parameters, 3, (parameters.camp / 2000) + (parameters.separation / 2000)) / 2;
            trans.finalTime = 50;
            trans.finalZombieCount = parameters => zombieAmount(parameters, 3, (parameters.camp / 2000) + (parameters.separation / 2000));
            trans.spawnRate = parameters => Math.Max(150, 1000 - 100 * parameters.playing - 90 * (parameters.camp / 1000) - 80 * (parameters.separation / 300) - 300 * parameters.vehicleClass);
            trans.ended = delegate(Script_ZombieZone.TeamState state)
            {
                state.team.sendArenaMessage("Night has fallen.", 1);
                state.zombieMessage("The Day is now ours.", 1);
            };

            transitions.gracePeriod(15);

            transitions.currentLevel = 3;

            //PHASE 3
            trans = transitions.newTransition();
            trans.spawnComposition = (p) => (new List<ZombieType>()).addType(ZombieTransitions.DoomZombie, 3).addType(ZombieTransitions.KryptonianZombie, 2).addType(ZombieTransitions.LairZombie, 2).addType(ZombieTransitions.RageZombie, 2).addType(ZombieTransitions.RepulsorZombie, p.camp / 1000).addType(ZombieTransitions.InfatuatedZombie, p.separation / 800);
            trans.initialTime = 15;
            trans.initialZombieCount = parameters => zombieAmount(parameters, 1, (parameters.camp / 1250) + (parameters.separation / 1250)) / 2;
            trans.finalTime = 30;
            trans.finalZombieCount = parameters => zombieAmount(parameters, 1, (parameters.camp / 1250) + (parameters.separation / 1250));
            trans.spawnRate = parameters => Math.Max(150, 1000 - 100 * parameters.playing - 90 * (parameters.camp / 1000) - 80 * (parameters.separation / 300) - 300 * parameters.vehicleClass);

            transitions.gracePeriod(20);
            transitions.gracePeriod(10).started = delegate(Script_ZombieZone.TeamState state)
            {
                state.team.sendArenaMessage("&These suicide zombies are almost invincible.  We have to try to outrun them.", 8);
                state.zombieMessage("&The Death Zombies are awakening!  Prepare to swarm the puny humans.", 8);
            };

            trans = transitions.newTransition();
            trans.spawnComposition = (p) => (new List<ZombieType>()).addType(ZombieTransitions.DeathZombie, 12).addType(ZombieTransitions.DoomZombie, 1).addType(ZombieTransitions.DerangedZombie, 2).addType(ZombieTransitions.InfatuatedZombie, 1 + p.separation / 1200);
            trans.initialTime = 15;
            trans.initialZombieCount = parameters => trans.finalZombieCount(parameters) / 3;
            trans.finalTime = 40;
            trans.finalZombieCount = parameters => zombieAmount(parameters, 2, (parameters.camp / 1250) + (parameters.separation / 1250));
            trans.spawnRate = parameters => Math.Max(150, 1000 - 100 * parameters.playing - 90 * (parameters.camp / 1000) - 80 * (parameters.separation / 300) - 300 * parameters.vehicleClass);
            trans.ended = delegate(Script_ZombieZone.TeamState state)
            {
                state.wipeOut();
                state.team.sendArenaMessage("What the hell just happened?  Where did they go?!");
                state.zombieMessage("The Death Zombies have burrowed. It won't be long until the opportune moment to re-emerge.");
            };
            trans.zombieTickers[0] = info => String.Format("Something feels familiar about this.. {0}.  {1}", info.player._alias, info.timeString);
            trans.humanTickers[2] = info => String.Format("Inexorable Death Wave: {0}", info.timeString);

            transitions.gracePeriod(25);

            trans = transitions.newTransition();
            trans.spawnComposition = (p) => (new List<ZombieType>()).addType(ZombieTransitions.KryptonianZombie, 2).addType(ZombieTransitions.DoomZombie, 2).addType(ZombieTransitions.RageZombie, 2).addType(ZombieTransitions.DestroyerZombie, 2).addType(ZombieTransitions.RepulsorZombie, p.camp / 1250).addType(ZombieTransitions.InfatuatedZombie, p.separation / 700);
            trans.initialTime = 15;
            trans.initialZombieCount = parameters => zombieAmount(parameters, 2, (parameters.camp / 1250) + (parameters.separation / 1250)) / 2;
            trans.finalTime = 50;
            trans.finalZombieCount = parameters => zombieAmount(parameters, 2, (parameters.camp / 1250) + (parameters.separation / 1250));
            trans.spawnRate = parameters => Math.Max(150, 1000 - 100 * parameters.playing - 90 * (parameters.camp / 1000) - 80 * (parameters.separation / 300) - 300 * parameters.vehicleClass);

            transitions.gracePeriod(20);

            //aaerox wave
            trans = transitions.newTransition();
            trans.initialWave = p => (new List<ZombieType>()).addType(ZombieTransitions.DoomZombie, 2 * p.playing).addType(ZombieTransitions.RepulsorZombie, 2 * p.playing).addType(ZombieTransitions.DestroyerZombie, 3 * p.playing).addType(ZombieTransitions.RageZombie, 2 * p.playing);
            trans.spawnComposition = ZombieTransitions.constComposition((new List<ZombieType>()).addType(ZombieTransitions.DoomZombie, 3).addType(ZombieTransitions.RepulsorZombie, 6).addType(ZombieTransitions.DestroyerZombie, 4).addType(ZombieTransitions.RageZombie, 4));
            trans.initialTime = 30;
            trans.initialZombieCount = ZombieTransitions.constInt(12);
            trans.finalTime = 40;
            trans.finalZombieCount = ZombieTransitions.constInt(25);
            trans.spawnRate = ZombieTransitions.constInt(50);
            trans.started = delegate(Script_ZombieZone.TeamState state)
            {
                state.team.sendArenaMessage("aaerox never coded this far.  But do you see the silence all around you?");
                //state.zombieMessage("The ZOMBIES were EVOLVED for this end.  Now, UNLEASH their true nature on those pathetic survivors.");
            };

            transitions.gracePeriod(10);


        }


        /// <summary>
        /// Triggered when a player notifies the server of an explosion
        /// </summary>
        [Scripts.Event("Player.Explosion")]
        public bool playerExplosion(Player player, ItemInfo.Projectile weapon, short posX, short posY, short posZ)
        {
            if (weapon.id == 1042)    //pheremone grenade
            {
                TeamState state = getTeamState(player._team);

                if (state != null)
                    state.pheremone(posX, posY, Environment.TickCount);
            }
            else if (weapon.id == 1061)   //team cloak
            {
                TeamState state = getTeamState(player._team);

                if (state != null)
                    state.cloak(Environment.TickCount);
            }
            //Zombie phasing?
            else if (weapon.id == 1122 && player._baseVehicle._type.Id == 640)
            {	//Make a note of his health 
                player.setVar("healthDecay", player._baseVehicle._type.Hitpoints - player._state.health);

                //Turn him into a phase zombie
                player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(117));
            }
            else if (weapon.id == 1123 && player._baseVehicle._type.Id == 117)
            {	//Return him to this world!
                player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(640));

                //Modify his health accordingly
                player.inventoryModify(AssetManager.Manager.getItemByName("MinusHealth"), player.getVarInt("healthDecay"));
            }
            //Zombie shell?
            else if (weapon.id == 1179 && player._baseVehicle._type.Id == 600)
            {	//Make a note of his health 
                player.setVar("healthDecay", player._baseVehicle._type.Hitpoints - player._state.health);

                //Turn him into a shelled zombie, modify health
                player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(129));
                player.inventoryModify(AssetManager.Manager.getItemByName("MinusHealth"), player.getVarInt("healthDecay"));
            }
            else if (weapon.id == 1181 && player._baseVehicle._type.Id == 129)
            {	//Return him to this world!

                //after making a note of his health
                player.setVar("healthDecay", player._baseVehicle._type.Hitpoints - player._state.health);

                //turn back to default, modify health
                player.setDefaultVehicle(AssetManager.Manager.getVehicleByID(600));
                player.inventoryModify(AssetManager.Manager.getItemByName("MinusHealth"), player.getVarInt("healthDecay"));
            }
            //Zombie spawner?
            else if (weapon.id == 1124 && player._baseVehicle._type.Id == 630)
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
            else if (weapon.id == 1127 || weapon.id == 1130 || weapon.id == 1131 || weapon.id == 1137)
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
            if (item.name.StartsWith("Unlearn ") && item.name.Length > 8)
            {
                string skillName = item.name.Substring(8);
                SkillInfo skill = AssetManager.Manager.getSkillByName(skillName);
                //player.sendMessage(0,"Wiping " + skillName);
                //player.sendMessage(0," The skill is " + (skill == null ? "null" : "NOT null"));

                if (skill != null)
                    player.unlearn(skill.SkillId);

            }
            if (item.id == 36)
            {	//Are we targetting our ammo generator?
                TeamState state = getTeamState(player._team);
                if (state != null && state.ammoGenerator != null && targetVehicle == state.ammoGenerator._id)
                {
                    player.sendMessage(-1, "You are unable to repair Ammo Generators.");
                    return false;
                }
            }

            return true;
        }

        //returns whether or not creation was successful
        public bool createAmmoGenerator(Player player, bool replace = false)
        {
            TeamState state = getTeamState(player._team);

            //if old ammo gen already exists, we kill it
            if (state.ammoGenerator != null)
            {
                //if the new ammo gen is too close, however, treat it as an error
                if (!replace && Helpers.isInRange(c_ammoGenerateDirectDist, state.ammoGenerator._state, player._state))
                {
                    player.sendMessage(-1, String.Format("Request denied: Ammo generator already exists at {0}.", state.ammoGenerator._state.letterCoord()));
                    player.sendMessage(-1, "&Type ?replacegen if you need to replace it.");

                    return false;
                }
                else
                    state.ammoGenerator.kill(null);
            }

            //Create the new generator
            state.ammoGenerator = _arena.newVehicle(AssetManager.Manager.getVehicleByID((AssetManager.Manager.getItemByID(32) as ItemInfo.VehicleMaker).vehicleID), player._team, player, player._state) as Computer;
            state.ammoGeneratorLevel = 0;
            state.tickLastAmmoGenerate = 0;

            player._team.sendArenaMessage(String.Format("New ammo generator created at {0}. (Type ?geninfo for more details.)", state.ammoGenerator._state.letterCoord()));

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
            {
                if (!createAmmoGenerator(player))
                    player.inventoryModify(false, item.id, 1);  //gives them the generator back

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
            //Energizerbot?
            else if (item.id == 95)
            {	//Create the bot
                _arena.newVehicle(AssetManager.Manager.getVehicleByID(item.vehicleID),
                    player._team, player, player._state, null,
                    typeof(EnergizerBotTurret));

                return false;
            }
            //Laser turret?
            else if (item.id == 40)
            {	//Does he already have a laser turret?
                TeamState state = getTeamState(player._team);

                List<LaserTurret> lasers = null;

                //their laser list already exists
                if (state.lasers.ContainsKey(player) && state.lasers[player] != null)
                {
                    lasers = state.lasers[player];  //get it

                    //filter out dead or null lasers
                    int length = lasers.Count;
                    for (int i = 0; i < length; i++)
                        if (lasers[i] == null || lasers[i].IsDead)
                        {
                            lasers.RemoveAt(i);
                            i--;
                            length--;
                        }
                }
                else
                    lasers = state.lasers[player] = new List<LaserTurret>();

                int laserLimit = state.laserLimit(player);

                //if they're over their laser limit, remove the earliest one
                if (lasers.Count >= laserLimit && lasers.Count > 0)
                {
                    lasers[0].destroy(true);
                    lasers.RemoveAt(0);
                }

                //Create the turret
                LaserTurret turret = _arena.newVehicle(AssetManager.Manager.getVehicleByID(item.vehicleID),
                    player._team, player, player._state, null,
                    typeof(LaserTurret)) as LaserTurret;

                turret.zz = this;

                lasers.Add(turret);

                int lasersRemaining = laserLimit - lasers.Count;
                player.sendMessage(0, String.Format("&Laser turret created at {0}.  {1}", turret._state.letterCoord(), lasersRemaining <= 0 ? "Your next laser will automatically replace a previous one." : String.Format("You can have up to {0} more, before earlier lasers are removed.", lasersRemaining)));

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


        [Scripts.Event("Player.WarpItem")]
        public bool playerWarpItem(Player player, ItemInfo.WarpItem item, ushort targetVehicle, short posX, short posY)
        {

            if (item.id == 46) //teleport beacon needs to cost ammo!
            {
                ItemInfo ammo = AssetManager.Manager.getItemByName("Ammo");
                int amountAmmo = player.getInventoryAmount(ammo.id);

                if (amountAmmo < item.ammoUsedPerShot)
                    return false;
                else
                {
                    player.inventoryModify(AssetManager.Manager.getItemByName("NegativeAmmo"), item.ammoUsedPerShot);
                    return true;
                }
            }

            return true;
        }

        [Scripts.Event("Shop.Buy")]
        public bool playerShop(Player player, ItemInfo item, int quantity)
        {
            /*
            if(quantity < 0 || quantity > 1000) //avoids cash overflow bug
            {
                player.sendMessage(7, "You are not allowed to buy that many.");
                return false;
            }

            player.sendMessage(0, "just bought " + quantity);*/

            return true;
        }
        #endregion
    }


}
