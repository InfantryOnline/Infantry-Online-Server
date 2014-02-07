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

namespace InfServer.Script.GameType_Fantasy
{	// Script Class
    /// Provides the interface between the script and arena
    ///////////////////////////////////////////////////////
    class Script_Fantasy : Scripts.IScript
    {	///////////////////////////////////////////////////
        // Member Variables
        ///////////////////////////////////////////////////
        private Arena _arena;					//Pointer to our arena class
        private CfgInfo _config;				//The zone config


        private int _jackpot;					//The game's jackpot so far

        private Team _victoryTeam;				//The team currently winning!
        private int _tickVictoryStart;			//The tick at which the victory countdown began
        private int _tickNextVictoryNotice;		//The tick at which we will next indicate imminent victory
        private int _victoryNotice;				//The number of victory notices we've done
        private int _tickLastBotSpawn;
        private int _nestID = 400;
        private int _botSpawnRate = 8000;
        private int _lastGameCheck;				//The tick at which we last checked for game viability
        private int _tickGameStarting;			//The tick at which the game began starting (0 == not initiated)
        private int _tickGameStart;				//The tick at which the game started (0 == stopped)

        //Settings
        private int _minPlayers;				//The minimum amount of players

        private bool _gameWon = false;

        //Headquarters
        public Headquarters _hqs;              //Our headquarter tracker
        private int[] _hqlevels;                //Bounty required to level up HQs
        public int _hqVehId;                   //The vehicle ID of our HQs
        private int _baseXPReward;              //Base XP reward for HQs
        private int _baseCashReward;            //Base Cash reward for HQs
        private int _basePointReward;           //Base Point reward for HQs
        private int _rewardInterval;            //The interval at which we reward for HQs
        private int _lastHQReward;              //The tick at which we last checked for HQ rewards

        private int _tickGameLastTickerUpdate;

        //Koth
        private class PlayerCrownStatus
        {
            public bool crown;                  //Player has crown?
            public int crownKills;              //Crown kills without a crown
            public int crownDeaths;             //Times died with a crown (counted until they lose it)
            public int expireTime;              //When the crown will expire
            public PlayerCrownStatus(bool bCrown)
            {
                crown = bCrown;
            }
            public PlayerCrownStatus()
            {
                crown = true;
            }
        }
        private Dictionary<Player, PlayerCrownStatus> _playerCrownStatus;
        private List<Player> _activeCrowns //List of people with a crown
        {
            get { return _playerCrownStatus.Where(p => p.Value.crown).Select(p => p.Key).ToList(); }
        }
        private List<Player> _noCrowns //List of people with no crowns
        {
            get { return _playerCrownStatus.Where(p => !p.Value.crown).Select(p => p.Key).ToList(); }
        }
        private List<Team> _crownTeams;

        //Bot spawns
        public List<fantasyBot> _spawns = new List<fantasyBot>();
        public class fantasyBot
        {
            //General settings
            public int ID;                  //Our bot ID
            public int vehID;               //Our vehicle ID
            public int spawnID;             //Vehicle ID of where we spawn
            public int count;               //Current amount of this bot
            public int max;                 //Max amount of this bot for assigned spawn
            public int multiple;            //Multiple of bots to spawn at one time
            public int ticksBetweenSpawn;   //How many ticks to wait in between spawns

            //Targetting settings
            public bool atkBots;    //Do we attack bots
            public bool atkVeh;     //Do we attack computer vehicles?
            public bool atkPlayer;  //Do we attack players?
            public bool atkCloak;   //Do we attack cloakers?
            public bool patrol;     //Do we patrol?
            //Spawn point settings
            public bool relyOnSpawn;//Do we cease to exist when our spawn point is missing

            //Distances
            public int defenseRadius;       //Distance from player until we attack them
            public int distanceFromHome;    //Distance from home we have to be to ignore targets and run back [if not fighting]
            public int patrolRadius;        //Radius to patrol around home
            public int lockInTime;          //Amount of time we spend focused on one target
            public int attackRadius;        //Distance from home that we keep engaging units
            //      public bool patrolWaypoints;       //Whether or not to use waypoints for patrol


            //Weapon settings
            public int shortRange;
            public int mediumRange;
            public int longRange;

            //Utility settings

            //Constructor
            public fantasyBot(int id)
            {
                //General settings
                ID = id;
            }

            //Weapon Systems
            public List<BotWeapon> _weapons = new List<BotWeapon>();
            public class BotWeapon
            {
                public int ID;                 //ID of entry
                public int weaponID;           //Weapon ID

                public int preferredRange;  //Preferred range for this weapon [Bot will attempt to stay within this distance of enemy when using this weapon]

                public int shortChance;        //Chances to actually fire this within each range
                public int midChance;          //Use 500 for 50%
                public int longChance;
                public int allChance;

                //Constructor
                public BotWeapon(int id)
                {
                    ID = id;
                }
            }

            //Waypoint settings
            public List<Waypoints> _waypoints = new List<Waypoints>();
            public class Waypoints
            {
                public int ID;          //ID of entry
                public int posX;        //X coordinate
                public int posY;        //Y coordinate

                //Constructor
                public Waypoints(int id)
                {
                    ID = id;
                }
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

            _minPlayers = Int32.MaxValue;

            //Headquarters stuff!
            _hqlevels = new int[] { 500, 1000, 2500, 5000, 10000, 15000, 20000, 25000, 30000, 35000 };
            _hqVehId = 463;
            _baseXPReward = 25;
            _baseCashReward = 150;
            _basePointReward = 10;
            _rewardInterval = 90 * 1000; // 90 seconds
            _hqs = new Headquarters(_hqlevels);
            _hqs.LevelModify += onHQLevelModify;

            //oState
            //_oStates = new Dictionary<Player, Helper.oState>();

            foreach (Arena.FlagState fs in _arena._flags.Values)
            {	//Determine the minimum number of players
                if (fs.flag.FlagData.MinPlayerCount < _minPlayers)
                    _minPlayers = fs.flag.FlagData.MinPlayerCount;

                //Register our flag change events
                fs.TeamChange += onFlagChange;
            }

            if (_minPlayers == Int32.MaxValue)
                //No flags? Run blank games
                _minPlayers = 1;

            _minPlayers = 6;
            if (_minPlayers > 0)
            {
                _playerCrownStatus = new Dictionary<Player, PlayerCrownStatus>();
                _crownTeams = new List<Team>();
            }


            using (StreamReader sr = new StreamReader("./assets/FantasyBots.txt"))
            {

                String bots = sr.ReadToEnd();
                var values = bots.Split(',');

                int i = 0, j = 0;
                while (i < (values.Count() - 1))
                {
                    if (values[i] == "!")
                        i++;

                    _spawns.Add(new fantasyBot(j));
                    _spawns[j].ID = j;
                    //Basics
                    _spawns[j].vehID = Int32.Parse(values[i++]);
                    _spawns[j].spawnID = Int32.Parse(values[i++]);
                    _spawns[j].count = Int32.Parse(values[i++]);
                    _spawns[j].max = Int32.Parse(values[i++]);
                    _spawns[j].multiple = Int32.Parse(values[i++]);
                    _spawns[j].ticksBetweenSpawn = Int32.Parse(values[i++]);
                    //Bools
                    _spawns[j].atkBots = Boolean.Parse(values[i++]);
                    _spawns[j].atkVeh = Boolean.Parse(values[i++]);
                    _spawns[j].atkPlayer = Boolean.Parse(values[i++]);
                    _spawns[j].atkCloak = Boolean.Parse(values[i++]);
                    _spawns[j].patrol = Boolean.Parse(values[i++]);

                    //Distances
                    _spawns[j].defenseRadius = Int32.Parse(values[i++]);
                    _spawns[j].distanceFromHome = Int32.Parse(values[i++]);
                    _spawns[j].patrolRadius = Int32.Parse(values[i++]);
                    _spawns[j].lockInTime = Int32.Parse(values[i++]);
                    _spawns[j].attackRadius = Int32.Parse(values[i++]);
                    int totalWeapons = Int32.Parse(values[i]);
                    i++;
                    int k = 0;
                    Log.write("Adding bot weapons -- total:" + totalWeapons);
                    while (k < totalWeapons)
                    {
                        _spawns[j]._weapons.Add(new fantasyBot.BotWeapon(k));
                        Log.write("Added bot weapon - " + k);
                        _spawns[j]._weapons[k].weaponID = Int32.Parse(values[i++]);
                        Log.write("Added bot weapon - " + _spawns[j]._weapons[k].weaponID);
                        _spawns[j]._weapons[k].allChance = Int32.Parse(values[i++]);
                        _spawns[j]._weapons[k].shortChance = Int32.Parse(values[i++]);
                        _spawns[j]._weapons[k].midChance = Int32.Parse(values[i++]);
                        _spawns[j]._weapons[k].longChance = Int32.Parse(values[i++]);
                        _spawns[j]._weapons[k].preferredRange = Int32.Parse(values[i++]);
                        k++;
                    }
                    _spawns[j].shortRange = Int32.Parse(values[i++]);
                    _spawns[j].mediumRange = Int32.Parse(values[i++]);
                    _spawns[j].longRange = Int32.Parse(values[i++]);

                    //Waypoint Settings
                    int totalWaypoints = Int32.Parse(values[i]);
                    i++;
                    k = 0;
                    while (k < totalWaypoints)
                    {
                        _spawns[j]._waypoints.Add(new fantasyBot.Waypoints(k));
                        _spawns[j]._waypoints[k].ID = k;
                        _spawns[j]._waypoints[k].posX = Int32.Parse(values[i++]);
                        _spawns[j]._waypoints[k].posY = Int32.Parse(values[i]);
                        i++;
                        k++;
                    }
                    i++;
                    j++;
                }
            }


            return true;
        }

        /// <summary>
        /// Allows the script to maintain itself
        /// </summary>
        public bool poll()
        {	//Should we check game state yet?
            //Should we check game state yet?
            int now = Environment.TickCount;

            //Do we have enough people to start a game of KOTH?
            int playing = _arena.PlayerCount;

            if (now - _lastGameCheck <= Arena.gameCheckInterval)
                return true;
            _lastGameCheck = now;

            //Find the nest and spawn a spider at it or whatever
            if (now - _tickLastBotSpawn > _botSpawnRate)
            {
                _tickLastBotSpawn = now;

                foreach (Vehicle v in _arena.Vehicles.ToList())
                    foreach (var p in _spawns)
                        if (p.spawnID == v._type.Id)
                        {
                            //First check to see if we have enough room to spawn this one
                            p.count = 0;
                            foreach (Vehicle existing in _arena.getVehiclesInRange(v._state.positionX, v._state.positionY, 200))
                                if (existing._type.Id == p.vehID)
                                    p.count++;

                            if (p.count < p.max)
                            {
                                int amountToSpawn = 1 * p.multiple;
                                for (int i = 0; i < amountToSpawn; i++)
                                    _arena.newBot(typeof(RangeMinion), (ushort)p.vehID, _arena.Teams.ElementAt(7), null, v._state, this, p.ID);
                            }
                        }
            }


            //Should we reward yet for HQs?
            if (now - _lastHQReward > _rewardInterval)
            {   //Reward time!
                IEnumerable<Vehicle> hqs = _arena.Vehicles.Where(v => v._type.Id == _hqVehId);

                if (hqs != null)
                {
                    foreach (Vehicle hq in hqs)
                    {   //Reward all HQ teams!
                        if (_hqs[hq._team] == null)
                            //We're not tracking this HQ for some reason... hm...
                            continue;

                        if (_hqs[hq._team].Level == 0)
                        {
                            if (hq._team._name.Contains("Bot Team"))
                                continue;

                            hq._team.sendArenaMessage("&Headers - Periodic reward. Your Headquarters is still level 0, minimum level is 1 to obtain rewards. Use ?hq to track your HQ's progress.");
                            continue;
                        }

                        int points = (int)(_basePointReward * 1.5 * _hqs[hq._team].Level);
                        int cash = (int)(_baseCashReward * 1.5 * _hqs[hq._team].Level);
                        int experience = (int)(_baseXPReward * 1.5 * _hqs[hq._team].Level);

                        foreach (Player p in hq._team.ActivePlayers)
                        {
                            p.BonusPoints += points;
                            p.Cash += cash;
                            p.Experience += experience;
                            p.sendMessage(0, "&Headquarters - Periodic reward. Level " + _hqs[hq._team].Level + ": Cash=" + cash + " Experience=" + experience + " Points=" + points);
                        }


                    }
                }

                _lastHQReward = now;
            }
            //Check for expiring crowners
            if (_tickGameStart > 0)
            {
                foreach (var p in _playerCrownStatus)
                {
                    if ((now > p.Value.expireTime || _victoryTeam != null) && p.Value.crown)
                    {
                        p.Value.crown = false;
                        Helpers.Player_Crowns(_arena, true, _activeCrowns);
                        Helpers.Player_Crowns(_arena, false, _noCrowns);
                    }
                }

                //Get a list of teams with crowns and see if there is only one team
                _crownTeams.Clear();

                foreach (Player p in _activeCrowns)
                    if (!_crownTeams.Contains(p._team))
                        _crownTeams.Add(p._team);

                if (_crownTeams.Count == 1 || _activeCrowns.Count == 1)
                {//We have a winning team
                    _victoryTeam = _activeCrowns.First()._team;
                    _arena.sendArenaMessage("Team " + _victoryTeam._name + " is the winner of KOTH!");
                    kothVictory(_victoryTeam);
                    return true;
                }
                else if (_activeCrowns.Count == 0)
                {//There was a tie
                    _arena.sendArenaMessage("There was no winner");
                    resetKOTH();
                    return true;
                }
            }

            //Update our tickers
            if (_tickGameStart > 0 && now - _arena._tickGameStarted > 2000)
            {
                if (now - _tickGameLastTickerUpdate > 1000)
                {
                    updateTickers();
                    _tickGameLastTickerUpdate = now;
                }
            }
            //Do we have enough players to start a game of KOTH?
            if ((_tickGameStart == 0 || _tickGameStarting == 0) && _minPlayers > 0 && playing < _minPlayers)
            {	//Stop the game!
                _arena.setTicker(1, 1, 0, "Not Enough Players");
                resetKOTH();
            }

             //Do we have enough players to start a game of KOTH?
            else if (_tickGameStart == 0 && _tickGameStarting == 0 && playing >= _minPlayers)
            {	//Great! Get going
                _tickGameStarting = now;
                _arena.setTicker(1, 1, 120 * 100, "Next game: ",
                    delegate()
                    {	//Trigger the game start
                        startKOTH();
                    }
                );
            }

            return true;
        }

        #region Events

        /// <summary>
        /// Updates our tickers for KOTH
        /// </summary>
        public void updateTickers()
        {
            if (_arena.ActiveTeams.Count() > 1)
            {//Show players their crown timer using a ticker
                _arena.setTicker(1, 0, 0, delegate(Player p)
                {
                    if (_playerCrownStatus.ContainsKey(p) && _playerCrownStatus[p].crown)
                        return String.Format("Crown Timer: {0}", (_playerCrownStatus[p].expireTime - Environment.TickCount) / 1000);

                    else
                        return "";
                });
            }
        }

        /// <summary>
        /// Gives a crown to the specified player
        /// </summary>
        public void giveCrown(Player p)
        {//Give the player a crown and inform the arena
            var v = _playerCrownStatus[p];
            v.crown = true;
            v.crownDeaths = 0;
            v.crownKills = 0;
            List<Player> crowns = _activeCrowns;
            Helpers.Player_Crowns(_arena, true, crowns);
            updateCrownTime(p);
        }

        /// <summary>
        /// Updates the crown time for the specified player
        /// </summary>
        public void updateCrownTime(Player p)
        {   //Reset the player's counter
            _playerCrownStatus[p].expireTime = Environment.TickCount + (_config.king.expireTime * 1000);
        }
        /// <summary>
        /// Called when KOTH game has ended
        /// </summary>
        public void endKOTH()
        {
            _arena.sendArenaMessage("Game has ended");

            _tickGameStart = 0;
            _tickGameStarting = 0;
            _victoryTeam = null;
            _crownTeams = null;

            //Remove all crowns and clear list of KOTH players
            Helpers.Player_Crowns(_arena, false, _arena.Players.ToList());
            _playerCrownStatus.Clear();
        }

        /// <summary>
        /// Called when KOTH game has been restarted
        /// </summary>
        public void resetKOTH()
        {//Game reset, perhaps start a new one
            _tickGameStart = 0;
            _tickGameStarting = 0;

            _victoryTeam = null;
        }

        /// <summary>
        /// Called when KOTH game has started
        /// </summary>
        public void startKOTH()
        {
            //We've started!
            _tickGameStart = Environment.TickCount;
            _tickGameStarting = 0;
            _playerCrownStatus.Clear();

            //Let everyone know
            _arena.sendArenaMessage("Game has started!", 1);

            _crownTeams = new List<Team>();
            _playerCrownStatus = new Dictionary<Player, PlayerCrownStatus>();
            List<Player> crownPlayers = (_config.king.giveSpecsCrowns ? _arena.Players : _arena.PlayersIngame).ToList();

            foreach (var p in crownPlayers)
            {
                _playerCrownStatus[p] = new PlayerCrownStatus();
                giveCrown(p);
            }
            //Everybody is king!
            Helpers.Player_Crowns(_arena, true, crownPlayers);
        }

        /// <summary>
        /// Called when the specified team have won
        /// </summary>
        public void kothVictory(Team victors)
        {	//Let everyone know          
            //Calculate the jackpot for each player
            foreach (Player p in victors.AllPlayers)
            {	//Spectating? 
                if (p.IsSpectator)
                    continue;

                //Obtain the respective rewards
                int cashReward = _config.king.cashReward * _arena.PlayerCount;
                int experienceReward = _config.king.experienceReward * _arena.PlayerCount;
                int pointReward = _config.king.pointReward * _arena.PlayerCount;

                p.sendMessage(0, String.Format("Your Personal Reward: Points={0} Cash={1} Experience={2}", pointReward, cashReward, experienceReward));

                //Prize winning team
                p.Cash += cashReward;
                p.Experience += experienceReward;
                p.BonusPoints += pointReward;
            }
            _victoryTeam = null;

            endKOTH();
        }

        /// <summary>
        /// Called when a flag changes team
        /// </summary>
        public void onFlagChange(Arena.FlagState flag)
        {	//Does this team now have all the flags?   
            //who cares if they do
        }

        /// <summary>
        /// Triggered when an HQ levels up (or down?)
        /// </summary>
        public void onHQLevelModify(Team team)
        {
            //Let the team know they've leveled up
            if (_hqs[team].Level != _hqlevels.Count())
                team.sendArenaMessage("&Headquarters - Your HQ has reached level " + _hqs[team].Level + "! You need " + _hqlevels[_hqs[team].Level] + " bounty to reach the next level");

            //Lets notify everyone whenever an HQ reaches level 10!
            if (_hqs[team].Level == 10)
                _arena.sendArenaMessage("&Headquarters - " + team._name + " HQ has reached the max level of " + _hqlevels.Count() + "!");
        }

        /// <summary>
        /// Called when the specified team have won
        /// </summary>
        public void gameVictory(Team victors)
        {	//Let everyone know
            if (_config.flag.useJackpot)
                _jackpot = (int)Math.Pow(_arena.PlayerCount, 2);
            _arena.sendArenaMessage(String.Format("Victory={0} Jackpot={1}", victors._name, _jackpot), _config.flag.victoryBong);

            //TODO: Move this calculation to breakdown() in ScriptArena?
            //Calculate the jackpot for each player
            foreach (Player p in _arena.Players)
            {	//Spectating? Psh.
                if (p.IsSpectator)
                    continue;
                //Find the base reward
                int personalJackpot;

                if (p._team == victors)
                    personalJackpot = _jackpot * (_config.flag.winnerJackpotFixedPercent / 1000);
                else
                    personalJackpot = _jackpot * (_config.flag.loserJackpotFixedPercent / 1000);

                //Obtain the respective rewards
                int cashReward = personalJackpot * (_config.flag.cashReward / 1000);
                int experienceReward = personalJackpot * (_config.flag.experienceReward / 1000);
                int pointReward = personalJackpot * (_config.flag.pointReward / 1000);

                p.sendMessage(0, String.Format("Your Personal Reward: Points={0} Cash={1} Experience={2}", pointReward, cashReward, experienceReward));

                p.Cash += cashReward;
                p.Experience += experienceReward;
                p.BonusPoints += pointReward;
            }

            //Stop the game
            _arena.gameEnd();
        }

        /// <summary>
        /// Called when a player sends a chat command
        /// </summary>
        [Scripts.Event("Player.ChatCommand")]
        public bool playerChatCommand(Player player, Player recipient, string command, string payload)
        {
            if (command.ToLower().Equals("hq"))
            {   //Give them some information on their HQ
                if (_hqs[player._team] == null)
                {
                    player.sendMessage(0, "&Headquarters - Your team has no headquarters");
                }
                else
                {
                    player.sendMessage(0, String.Format("&Headquarters - Level={0} Bounty={1}",
                        _hqs[player._team].Level,
                        _hqs[player._team].Bounty));
                }
            }

            if (command.ToLower().Equals("hqlist"))
            {   //Give them some information on all HQs present in the arena
                IEnumerable<Vehicle> hqs = _arena.Vehicles.Where(v => v._type.Id == _hqVehId);
                if (hqs.Count().Equals(0))
                {
                    player.sendMessage(0, "&Headquarters - There are no headquarters present in the arena");
                }
                else
                {
                    player.sendMessage(0, "&Headquarters - Information");
                    foreach (Vehicle hq in hqs)
                    {
                        if (_hqs[hq._team] == null)
                            //We're not tracking this HQ for some reason... hm...
                            continue;
                        player.sendMessage(0, String.Format("*Headquarters - Team={0} Level={1} Bounty={2} Location={3}",
                            hq._team._name,
                            _hqs[hq._team].Level,
                            _hqs[hq._team].Bounty,
                            Helpers.posToLetterCoord(hq._state.positionX, hq._state.positionY)));
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Called when a player leaves the game
        /// </summary>
        [Scripts.Event("Player.Leave")]
        public void playerLeave(Player player)
        {
            //Find out if KOTH is enabled
            if (_minPlayers > 0)
                if (_playerCrownStatus.ContainsKey(player))
                {//Remove their crown and tell everyone
                    _playerCrownStatus[player].crown = false;
                    Helpers.Player_Crowns(_arena, false, _noCrowns);
                }
        }
        /// <summary>
        /// Called when a player enters the arena
        /// </summary>
        [Scripts.Event("Player.EnterArena")]
        public void playerEnter(Player player)
        {   //We always run blank games, try to start a game in whatever arena the player is in
            if (!_arena._bGameRunning)
            {
                _arena.gameStart();
                _arena.flagSpawn();
            }

            //Send them the crowns if KOTH is enabled
            if (_minPlayers > 0)
                if (!_playerCrownStatus.ContainsKey(player))
                {
                    _playerCrownStatus[player] = new PlayerCrownStatus(false);
                    Helpers.Player_Crowns(_arena, true, _activeCrowns, player);
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

            //Scramble the teams!
            // ScriptHelpers.scrambleTeams(_arena, 2, true);

            //Spawn our flags!
            _arena.flagSpawn();

            //Let everyone know
            _arena.sendArenaMessage("Game has started!", _config.flag.resetBong);
            _gameWon = false;

            foreach (var s in _spawns)
                foreach (var w in s._waypoints)
                {
                    VehInfo wp = _arena._server._assets.getVehicleByID(Convert.ToInt32(402));
                    Helpers.ObjectState waypointState = new Protocol.Helpers.ObjectState();
                    waypointState.positionX = (short)w.posX;
                    waypointState.positionY = (short)w.posY;
                    waypointState.positionZ = 0;
                    waypointState.yaw = 0;
                    _arena.newVehicle(
                                    wp,
                                    _arena.Teams.ElementAt(7), null,
                                   waypointState);
                }

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
            _tickVictoryStart = 0;
            _tickNextVictoryNotice = 0;
            _victoryTeam = null;
            _gameWon = false;

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
            _tickVictoryStart = 0;
            _tickNextVictoryNotice = 0;

            _gameWon = false;

            _victoryTeam = null;

            return true;
        }

        /// <summary>
        /// Triggered when a player requests to pick up an item
        /// </summary>
        [Scripts.Event("Player.ItemPickup")]
        public bool playerItemPickup(Player player, Arena.ItemDrop drop, ushort quantity)
        {
            return true;
        }
        /// <summary>
        /// Triggered when a player requests to drop an item
        /// </summary>
        [Scripts.Event("Player.ItemDrop")]
        public bool playerItemDrop(Player player, ItemInfo item, ushort quantity)
        {
            if (_arena.getTerrainID(player._state.positionX, player._state.positionY) == 10)
            {

                if (player.inventoryModify(item, -quantity))
                {
                    //We want to continue wrapping around the vehicleid limits
                    //looking for empty spots.
                    ushort ik;

                    for (ik = _arena._lastItemKey; ik <= Int16.MaxValue; ++ik)
                    {	//If we've reached the maximum, wrap around
                        if (ik == Int16.MaxValue)
                        {
                            ik = (ushort)ZoneServer.maxPlayers;
                            continue;
                        }

                        //Does such an item exist?
                        if (_arena._items.ContainsKey(ik))
                            continue;

                        //We have a space!
                        break;
                    }

                    _arena._lastItemKey = ik;
                    //Create our drop class		
                    Arena.ItemDrop id = new Arena.ItemDrop();

                    id.item = item;
                    id.id = ik;
                    id.quantity = (short)quantity;
                    id.positionX = player._state.positionX;
                    id.positionY = player._state.positionY;
                    id.relativeID = (0 == 0 ? item.relativeID : 0);
                    id.freq = player._team._id;

                    id.owner = player; //For bounty abuse upon pickup

                    int expire = _arena.getTerrain(player._state.positionX, player._state.positionY).prizeExpire;
                    id.tickExpire = (expire > 0 ? (Environment.TickCount + (expire * 1000)) : 0);

                    //Add it to our list
                    _arena._items[ik] = id;

                    //Notify JUST the player
                    Helpers.Object_ItemDrop(player, id);
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// Handles a player's portal request
        /// </summary>
        [Scripts.Event("Player.Portal")]
        public bool playerPortal(Player player, LioInfo.Portal portal)
        {
            List<Arena.FlagState> carried = _arena._flags.Values.Where(flag => flag.carrier == player).ToList();

            foreach (Arena.FlagState carry in carried)
            {   //If the terrain number is 0-15

                int terrainNum = player._arena.getTerrainID(player._state.positionX, player._state.positionY);
                if (terrainNum >= 0 && terrainNum <= 15)
                {   //Check the FlagDroppableTerrains for that specific terrain id

                    if (carry.flag.FlagData.FlagDroppableTerrains[terrainNum] == 0)
                        _arena.flagResetPlayer(player);
                }
            }

            return true;
        }

        /// <summary>
        /// Handles a player's produce request
        /// </summary>
        [Scripts.Event("Player.Produce")]
        public bool playerProduce(Player player, Computer computer, VehInfo.Computer.ComputerProduct product)
        {



            return true;
        }

        /// <summary>
        /// Handles a player's switch request
        /// </summary>
        [Scripts.Event("Player.Switch")]
        public bool playerSwitch(Player player, LioInfo.Switch swi)
        {
            return true;
        }

        /// <summary>
        /// Handles a player's flag request
        /// </summary>
        [Scripts.Event("Player.FlagAction")]
        public bool playerFlagAction(Player player, bool bPickup, bool bInPlace, LioInfo.Flag flag)
        {
            return true;
        }

        /// <summary>
        /// Handles the spawn of a player
        /// </summary>
        [Scripts.Event("Player.Spawn")]
        public bool playerSpawn(Player player, bool bDeath)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a player wants to unspec and join the game
        /// </summary>
        [Scripts.Event("Player.JoinGame")]
        public bool playerJoinGame(Player player)
        {
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
        /// Triggered when a player wants to enter a vehicle
        /// </summary>
        [Scripts.Event("Player.EnterVehicle")]
        public bool playerEnterVehicle(Player player, Vehicle vehicle)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a player wants to leave a vehicle
        /// </summary>
        [Scripts.Event("Player.LeaveVehicle")]
        public bool playerLeaveVehicle(Player player, Vehicle vehicle)
        {
            #region Stone Skin
            if (vehicle._type.Id == 121)
                return false;
            #endregion
            return true;
        }

        /// <summary>
        /// Triggered when a player notifies the server of an explosion
        /// </summary>
        [Scripts.Event("Player.Explosion")]
        public bool playerExplosion(Player player, ItemInfo.Projectile weapon, short posX, short posY, short posZ)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a player has died, by any means
        /// </summary>
        /// <remarks>killer may be null if it wasn't a player kill</remarks>
        [Scripts.Event("Player.Death")]
        public bool playerDeath(Player victim, Player killer, Helpers.KillType killType, CS_VehicleDeath update)
        {
            if (killer == null)
                return true;

            //Was it a player kill?
            if (killType == Helpers.KillType.Player)
            {   //No team killing!
                if (victim._team != killer._team)
                    //Does the killer have an HQ?
                    if (_hqs[killer._team] != null)
                        //Reward his HQ! (Victims bounty + half of own)
                        _hqs[killer._team].Bounty += victim.Bounty + (killer.Bounty / 2);

                //Find out if KOTH is running
                if (_activeCrowns.Count == 0 || killer == null)
                    return true;

                //Handle crowns
                if (_playerCrownStatus[victim].crown)
                {   //Incr crownDeaths
                    _playerCrownStatus[victim].crownDeaths++;

                    if (_playerCrownStatus[victim].crownDeaths >= _config.king.deathCount)
                    {
                        //Take it away now
                        _playerCrownStatus[victim].crown = false;
                        _noCrowns.Remove(victim);
                        Helpers.Player_Crowns(_arena, false, _noCrowns);
                    }
                    if (_playerCrownStatus.ContainsKey(killer))
                    {
                    if (!_playerCrownStatus[killer].crown)
                        _playerCrownStatus[killer].crownKills++;
                    }
                }

                //Reset their timer
                if (_playerCrownStatus[killer].crown)
                    updateCrownTime(killer);
                else if (_config.king.crownRecoverKills != 0)
                {   //Should they get a crown?
                    if (_playerCrownStatus[killer].crownKills >= _config.king.crownRecoverKills)
                    {
                        _playerCrownStatus[killer].crown = true;
                        giveCrown(killer);
                    }
                }
            }

            //Was it a computer kill?
            if (killType == Helpers.KillType.Computer)
            {
                //Let's find the vehicle!
                Computer cvehicle = victim._arena.Vehicles.FirstOrDefault(v => v._id == update.killerPlayerID) as Computer;
                Player vehKiller = cvehicle._creator;
                //Do they exist?
                if (cvehicle != null && vehKiller != null)
                {   //We'll take it from here...
                    update.type = Helpers.KillType.Player;
                    update.killerPlayerID = vehKiller._id;

                    //Don't reward for teamkills
                    if (vehKiller._team == victim._team)
                        Logic_Assets.RunEvent(vehKiller, _arena._server._zoneConfig.EventInfo.killedTeam);
                    else
                        Logic_Assets.RunEvent(vehKiller, _arena._server._zoneConfig.EventInfo.killedEnemy);

                    //Increase stats/HQ bounty and notify arena of the kill!
                    if (_hqs[vehKiller._team] != null)
                        //Reward his HQ! (Victims bounty + half of own)
                        _hqs[vehKiller._team].Bounty += victim.Bounty + (vehKiller.Bounty / 2);

                    vehKiller.Kills++;
                    victim.Deaths++;
                    Logic_Rewards.calculatePlayerKillRewards(victim, vehKiller, update);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Triggered when one player has killed another
        /// </summary>
        [Scripts.Event("Player.PlayerKill")]
        public bool playerPlayerKill(Player victim, Player killer)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a bot has killed a player
        /// </summary>
        [Scripts.Event("Player.BotKill")]
        public bool playerBotKill(Player victim, Bot bot)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a computer vehicle has killed a player
        /// </summary>
        [Scripts.Event("Player.ComputerKill")]
        public bool playerComputerKill(Player victim, Computer computer)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a player attempts to use a warp item
        /// </summary>
        [Scripts.Event("Player.WarpItem")]
        public bool playerWarpItem(Player player, ItemInfo.WarpItem item, ushort targetPlayerID, short posX, short posY)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a player attempts to use a warp item
        /// </summary>
        [Scripts.Event("Player.MakeVehicle")]
        public bool playerMakeVehicle(Player player, ItemInfo.VehicleMaker item, short posX, short posY)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a player attempts to use a warp item
        /// </summary>
        [Scripts.Event("Player.MakeItem")]
        public bool playerMakeItem(Player player, ItemInfo.ItemMaker item, short posX, short posY)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a player is buying an item from the shop
        /// </summary>
        [Scripts.Event("Shop.Buy")]
        public bool shopBuy(Player patron, ItemInfo item, int quantity)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a player is selling an item to the shop
        /// </summary>
        [Scripts.Event("Shop.Sell")]
        public bool shopSell(Player patron, ItemInfo item, int quantity)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a vehicle is created
        /// </summary>
        /// <remarks>Doesn't catch spectator or dependent vehicle creation</remarks>
        [Scripts.Event("Vehicle.Creation")]
        public bool vehicleCreation(Vehicle created, Team team, Player creator)
        {
            //Are they trying to create a headquarters?
            if (created._type.Id == _hqVehId)
            {
                if (_hqs[team] == null)
                {
                    _hqs.Create(team);
                    team.sendArenaMessage("&Headquarters - Your team has created a headquarters at " + Helpers.posToLetterCoord(created._state.positionX, created._state.positionY));
                }
                else
                {
                    if (creator != null)
                        creator.sendMessage(-1, "Your team already has a headquarters");
                    created.destroy(false, true);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Triggered when a vehicle dies
        /// </summary>
        [Scripts.Event("Vehicle.Death")]
        public bool vehicleDeath(Vehicle dead, Player killer)
        {
            //Did they just kill an HQ?!
            if (dead._type.Id == _hqVehId)
            {
                Team killers = killer._team;


                //Check if it was a team kill
                if (dead._team == killer._team)
                {   //Cheaters! Reward the last people to hurt the vehicle if it exists
                    IEnumerable<Player> attackers = dead._attackers;
                    attackers.Reverse();
                    foreach (Player p in attackers)
                        if (p._team != dead._team)
                            killers = p._team;

                    //Did we find a suitable killer?
                    if (killers == dead._team)
                    {   //Nope! Looks like nobody has ever hit their HQ... do nothing I guess.
                        _arena.sendArenaMessage("&Headquarters - " + killers._name + " killed their own HQ worth " + _hqs[dead._team].Bounty + " bounty... scum.");
                        _hqs.Destroy(dead._team);
                        return true;
                    }
                }

                foreach (Player p in killers.ActivePlayers)
                {   //Calculate some rewards
                    int points = (int)(_basePointReward * 1.5 * _hqs[dead._team].Level) * 15;
                    int cash = (int)(_baseCashReward * 1.5 * _hqs[dead._team].Level) * 15;
                    int experience = (int)(_baseXPReward * 1.5 * _hqs[dead._team].Level) * 15;
                    p.BonusPoints += points;
                    p.Cash += cash;
                    p.Experience += experience;
                    p.sendMessage(0, "&Headquarters - Your team has destroyed " + dead._team._name + " HQ (" + _hqs[dead._team].Bounty + " bounty)! Cash=" + cash + " Experience=" + experience + " Points=" + points);
                }

                //Notify the rest of the arena
                foreach (Team t in _arena.Teams.Where(team => team != killers))
                    t.sendArenaMessage("&Headquarters - " + dead._team._name + " HQ worth " + _hqs[dead._team].Bounty + " bounty has been destroyed by " + killers._name + "!");

                //Stop tracking this HQ
                _hqs.Destroy(dead._team);
            }
            return true;
        }
        #endregion
    }
}