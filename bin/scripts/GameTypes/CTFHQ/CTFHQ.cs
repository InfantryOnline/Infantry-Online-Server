using System;
using System.Linq;
using System.Collections.Generic;

using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;

using Assets;
using InfServer.Logic;

namespace InfServer.Script.GameType_CTFHQ
{	// Script Class
    /// Provides the interface between the script and arena
    ///////////////////////////////////////////////////////
    class Script_CTFHQ : Scripts.IScript
    {	///////////////////////////////////////////////////
        // Member Variables
        ///////////////////////////////////////////////////
        private Arena _arena;					//Pointer to our arena class
        private CfgInfo _config;				//The zone config

        //Headquarters
        public Headquarters _hqs;              //Our headquarter tracker
        private int[] _hqlevels;                //Bounty required to level up HQs
        public int _hqVehId;                   //The vehicle ID of our HQs
        private int _baseXPReward;              //Base XP reward for HQs
        private int _baseCashReward;            //Base Cash reward for HQs
        private int _basePointReward;           //Base Point reward for HQs
        private int _rewardInterval;            //The interval at which we reward for HQs

        private int _lastGameCheck;				//The tick at which we last checked for game viability
        private int _lastHQReward;              //The tick at which we last checked for HQ rewards

        //KOTH
        private Team _victoryTeam;				//The team currently winning!
        private int _tickGameLastTickerUpdate;  //The tick at which the ticker was last updated
        private int _tickGameStarting;			//The tick at which the game began starting (0 == not initiated)
        private int _tickGameStart;				//The tick at which the game started (0 == stopped)
        private int _minPlayers;				//The minimum amount of players needed for a KOTH game

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

        //Bots
        //Perimeter defense Bots
        public const float c_defenseInitialAmountPP = 0.5f;		//The amount of defense bots per player initially spawned (minimum of 1)
        public const int c_defenseAddTimerGrowth = 8;			//The amount of seconds to add to the new bot timer for each person missing from the team
        public const int c_defenseAddTimer = 36;			    //The amount of seconds between allowing new defense bots
        public const int c_defenseRespawnTimeGrowth = 400;		//The amount of time to add to the respawn timer for each missing player
        public const int c_defenseRespawnTime = 600;		    //The amount of ms between spawning new zombies
        public const int c_defenseMinRespawnDist = 900;			//The minimum distance bot can be spawned from the players
        public const int c_defenseMaxRespawnDist = 1500;		//The maximum distance bot can be spawned from the players
        public const int c_defenseMaxPath = 350;				//The maximum path length before a bot will request a respawn
        public const int c_defensePathUpdateInterval = 1000;	//The amount of ticks before a bot will renew it's path
        public const int c_defenseDistanceLeeway = 500;			//The maximum distance leeway a bot can be from the team before it is respawned
        public const int _checkCaptain = 50000;                 //The tick at which we check for a captain
        public const int _checkEngineer = 70000;                //The tick at which we check for an engineer
        protected int _tickLastEngineer = 0;                    //Last time we checked for an engineer
        protected int _tickLastCaptain = 0;                     //Last time we checked for a captain
        protected int _lastPylonCheck = 0;                      //Last time we check for pylons to build

        public const int c_CaptainPathUpdateInterval = 5000;	//The amount of ticks before an engineer's combat bot updates it's path

        public Dictionary<Team, int> botCount;
        public Dictionary<Team, int> captainBots;
        public List<Team> engineerBots;

        private class pylonObject
        {
            short x;      //X coordinate of pylon
            short y;      //Y coordinate of pylon
            bool exists;//Tells us if the pylon exists on the map

            public pylonObject(short xPos, short yPos)
            {
                exists = false;
                x = xPos;
                y = yPos;
            }
            public short getX()
            { return x; }
            public short getY()
            { return y; }
            public bool bExists()
            { return exists; }
            public void setExists(bool bExists)
            { exists = bExists; }
        }
        private Dictionary<int, pylonObject> _pylons;

        public const int _maxEngineers = 3;                      //Maximum amount of engineer bots that will spawn in game
        public int _currentEngineers = 0;                        //Current amount of engineer bots playing in the game
        public int[] _lastPylon;                                //Array of all pylons that are being used
        public const int _pylonVehID = 480;                      //The vehicle ID of our pylon


        //Bot teams
        Team botTeam1;
        Team botTeam2;
        Team botTeam3;

        Random _rand;

        public string botTeamName1 = "Bot Team - Jazz Nuggets";
        public string botTeamName2 = "Bot Team - Denny's Gobble Melt";
        public string botTeamName3 = "Bot Team - The Vehicles";

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

            //Headquarters stuff!
            _hqlevels = new int[] { 500, 1000, 2500, 5000, 10000, 15000, 20000, 25000, 30000, 35000 };
            _hqVehId = 463;
            _baseXPReward = 25;
            _baseCashReward = 150;
            _basePointReward = 10;
            _rewardInterval = 90 * 1000; // 90 seconds
            _hqs = new Headquarters(_hqlevels);
            _hqs.LevelModify += onHQLevelModify;

            //Handle bots
            captainBots = new Dictionary<Team, int>(); //Keeps track of captain bots
            botCount = new Dictionary<Team, int>(); //Counts of all defense bots and their teams
            engineerBots = new List<Team>();
            _currentEngineers = 0;  //The number of engineers currently alive
            //Handle the bot team using the engineer
            botTeam1 = new Team(_arena, _arena._server);
            botTeam1._name = botTeamName1;
            botTeam1._id = (short)_arena.Teams.Count();
            botTeam1._password = "jojotheClown";
            botTeam1._owner = null;
            botTeam1._isPrivate = true;

            botTeam2 = new Team(_arena, _arena._server);
            botTeam2._name = botTeamName2;
            botTeam2._id = (short)_arena.Teams.Count();
            botTeam2._password = "jojotheClown";
            botTeam2._owner = null;
            botTeam2._isPrivate = true;

            botTeam3 = new Team(_arena, _arena._server);
            botTeam3._name = botTeamName3;
            botTeam3._id = (short)_arena.Teams.Count();
            botTeam3._password = "jojotheClown";
            botTeam3._owner = null;
            botTeam3._isPrivate = true;

            _rand = new Random(System.Environment.TickCount);
            _lastPylon = null;
            _arena.createTeam(botTeam1);
            _arena.createTeam(botTeam2);
            _arena.createTeam(botTeam3);

            //Handle pylons
            _pylons = new Dictionary<int, pylonObject>();
            _pylons.Add(0, new pylonObject(338, 8500));
            _pylons.Add(1, new pylonObject(5488, 6476));
            _pylons.Add(2, new pylonObject(8375, 6628));
            _pylons.Add(3, new pylonObject(779, 460));
            _pylons.Add(4, new pylonObject(8976, 1225));
            //_pylons.Add(5, new pylonObject(1855, 267)); // bad one
            _pylons.Add(6, new pylonObject(4222, 4049));
            _pylons.Add(7, new pylonObject(8944, 5040));
            _pylons.Add(8, new pylonObject(831, 7066));
            // _pylons.Add(9, new pylonObject(8940, 7542)); // bad one

            //Find out if we will be running KOTH games and if we have enough players
            _minPlayers = _config.king.minimumPlayers;
            if (_minPlayers > 0)
            {
                _playerCrownStatus = new Dictionary<Player, PlayerCrownStatus>();
                _crownTeams = new List<Team>();
            }

            return true;
        }

        /// <summary>
        /// Allows the script to maintain itself
        /// </summary>
        public bool poll()
        {	//Should we check game state yet?
            int now = Environment.TickCount;

            //Do we have enough people to start a game of KOTH?
            int playing = _arena.PlayerCount;

            if (now - _lastGameCheck <= Arena.gameCheckInterval)
                return true;
            _lastGameCheck = now;

            //Should we spawn some pylons? Only check once an hour
            if (now - _lastPylonCheck > 36000000 && playing > 0)
            {
                foreach (KeyValuePair<int, pylonObject> obj in _pylons)
                {
                    if (obj.Value.bExists())
                        continue;

                    VehInfo vehicle = _arena._server._assets.getVehicleByID(Convert.ToInt32(_pylonVehID));
                    Helpers.ObjectState newState = new Protocol.Helpers.ObjectState();
                    newState.positionX = obj.Value.getX();
                    newState.positionY = obj.Value.getY();
                    newState.positionZ = 0;
                    newState.yaw = 0;

                    obj.Value.setExists(true);

                    //Put them all on one bot team since it doesn't matter who owns the pylon
                    _arena.newVehicle(
                                vehicle,
                                botTeam1, null,
                                newState);
                }

                _lastPylonCheck = now;
            }

            //Should we reward yet for HQs?
            if (now - _lastHQReward > _rewardInterval)
            {   //Reward time!
                IEnumerable<Vehicle> hqs = _arena.Vehicles.Where(v => v._type.Id == _hqVehId);

                Player owner = null;
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

                        //Is this an all-bot team?
                        if (hq._team._name.Contains("Bot Team"))
                            owner = null;
                        //Set an 'owner' for the team that the bots will consider their owner
                        else
                            if (hq._team.ActivePlayerCount > 0)
                                owner = hq._team.ActivePlayers.Last();

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

            if (now - _tickLastCaptain > _checkCaptain)
            {
                IEnumerable<Vehicle> hqs = _arena.Vehicles.Where(v => v._type.Id == _hqVehId);

                Player owner = null;
                if (hqs != null)
                {
                    foreach (Vehicle hq in hqs)
                    {//Handle the captains
                        Captain captain = null;

                        if (captain == null)
                        {//They don't have a captain   

                            //Pick a random faction out of two                   
                            Random rand = new Random(System.Environment.TickCount);
                            int r = rand.Next(1, 5);
                            int id = 0;
                            //See if they have a captain for their HQ, if not spawn one
                            if (owner != null && captainBots != null && !captainBots.ContainsKey(owner._team))
                            {   //Pick the appropriate faction for humans
                                if (owner._team._name.Contains("Collective"))
                                {
                                    id = 300;
                                    r = 1;
                                }
                                else if (owner._team._name.Contains("Europan"))
                                {
                                    id = 306;
                                    r = 2;
                                }
                                else if (owner._team._name.Contains("Faraday"))
                                {
                                    id = 312;
                                    r = 3;
                                }
                                else if (owner._team._name.Contains("Starfire"))
                                {
                                    id = 318;
                                    r = 4;
                                }
                                else if (owner._team._name.Contains("Titan"))
                                {
                                    id = 324;
                                    r = 5;
                                }
                                else
                                {//Pick a random one 
                                    switch (r)
                                    {
                                        case 1: id = 300; break; //Collective
                                        case 2: id = 306; break; //Europan
                                        case 3: id = 312; break; //Faraday
                                        case 4: id = 318; break; //Starfire
                                        case 5: id = 324; break; //Titan
                                    }
                                }

                                //Keep track of the bots they spawn
                                if (botCount.ContainsKey(owner._team))
                                    botCount[owner._team] = 0;
                                else
                                    botCount.Add(owner._team, 0);

                                //Spawn them in the HQ
                                captain = _arena.newBot(typeof(Captain), (ushort)id, owner._team, owner, hq._state, this, owner) as Captain;
                                captainBots.Add(owner._team, r);
                            }
                            else if (owner == null && captainBots != null && !captainBots.ContainsKey(botTeam1) && hq._team == botTeam1)
                            {//It's a bot team
                                r = rand.Next(1, 5);
                                switch (r)
                                {
                                    case 1: id = 300; break; //Collective
                                    case 2: id = 306; break; //Europan
                                    case 3: id = 312; break; //Faraday
                                    case 4: id = 318; break; //Starfire
                                    case 5: id = 324; break; //Titan
                                }
                                captain = _arena.newBot(typeof(Captain), (ushort)id, botTeam1, null, hq._state, this, null) as Captain;
                                captainBots.Add(botTeam1, r);
                                if (botCount.ContainsKey(botTeam1))
                                    botCount[botTeam1] = 0;
                                else
                                    botCount.Add(botTeam1, 0);
                            }
                            else if (owner == null && captainBots != null && !captainBots.ContainsKey(botTeam2) && hq._team == botTeam2)
                            {//It's a bot team
                                r = rand.Next(1, 5);
                                switch (r)
                                {
                                    case 1: id = 300; break; //Collective
                                    case 2: id = 306; break; //Europan
                                    case 3: id = 312; break; //Faraday
                                    case 4: id = 318; break; //Starfire
                                    case 5: id = 324; break; //Titan
                                }
                                captain = _arena.newBot(typeof(Captain), (ushort)id, botTeam2, null, hq._state, this, null) as Captain;
                                captainBots.Add(botTeam2, r);
                                if (botCount.ContainsKey(botTeam2))
                                    botCount[botTeam2] = 0;
                                else
                                    botCount.Add(botTeam2, 0);
                            }
                            else if (owner == null && captainBots != null && !captainBots.ContainsKey(botTeam3) && hq._team == botTeam3)
                            {//It's a bot team
                                r = rand.Next(1, 5);
                                switch (r)
                                {
                                    case 1: id = 300; break; //Collective
                                    case 2: id = 306; break; //Europan
                                    case 3: id = 312; break; //Faraday
                                    case 4: id = 318; break; //Starfire
                                    case 5: id = 324; break; //Titan
                                }
                                captain = _arena.newBot(typeof(Captain), (ushort)id, botTeam3, null, hq._state, this, null) as Captain;
                                captainBots.Add(botTeam3, r);
                                if (botCount.ContainsKey(botTeam3))
                                    botCount[botTeam3] = 0;
                                else
                                    botCount.Add(botTeam3, 0);
                            }
                        }
                    }
                }

                _tickLastCaptain = now;
            }


            if (now - _tickLastEngineer > _checkEngineer)
            {
                //Should we spawn a bot engineer to go base somewhere?
                if (_currentEngineers < _maxEngineers)
                {//Yes
                    IEnumerable<Vehicle> hqs = _arena.Vehicles.Where(v => v._type.Id == _hqVehId);
                    Vehicle home = null;

                    //First find out if we need to respawn to our previous team
                    foreach (Vehicle hq in hqs)
                    {
                        //Check to see if that HQ has an engineer
                        if (engineerBots.Contains(hq._team))
                            continue;

                        if (hq._team == botTeam1 || hq._team == botTeam2 || hq._team == botTeam3)
                        {
                            home = hq;
                        }
                    }
                    if (home == null)
                    {
                        //Find a random pylon to make our new home
                        IEnumerable<Vehicle> pylons = _arena.Vehicles.Where(v => v._type.Id == _pylonVehID);
                        if (pylons.Count() != 0)
                        {
                            _rand = new Random(System.Environment.TickCount);
                            int rand = _rand.Next(0, pylons.Count());

                            home = pylons.ElementAt(rand);
                        }
                    }
                    //Just in case there are no pylons
                    if (home != null)
                    {
                        if (home._type.Id == _pylonVehID)
                        {
                            Team team = null;
                            _arena.sendArenaMessage("An engineer has been deployed to from an orbiting drop ship.");
                            if (_hqs[botTeam1] == null)
                                team = botTeam1;
                            else if (_hqs[botTeam2] == null)
                                team = botTeam2;
                            else if (_hqs[botTeam3] == null)
                                team = botTeam3;

                            Engineer George = _arena.newBot(typeof(Engineer), (ushort)300, team, null, home._state, this) as Engineer;

                            //Find the pylon we are about to destroy and mark it as nonexistent
                            foreach (KeyValuePair<int, pylonObject> obj in _pylons)
                                if (home._state.positionX == obj.Value.getX() && home._state.positionY == obj.Value.getY())
                                    obj.Value.setExists(false);

                            //Destroy our pylon because we will use our hq to respawn and we dont want any other engineers grabbing this one
                            home.destroy(false);

                            //Keep track of the engineers
                            _currentEngineers++;
                            engineerBots.Add(team);
                        }

                        if (home._type.Id == _hqVehId)
                        {
                            Team team = null;
                            if (home._team == botTeam1)
                                team = botTeam1;
                            else if (home._team == botTeam2)
                                team = botTeam2;
                            else if (home._team == botTeam3)
                                team = botTeam3;

                            Engineer Filbert = _arena.newBot(typeof(Engineer), (ushort)300, team, null, home._state, this) as Engineer;

                            _currentEngineers++;
                            engineerBots.Add(team);
                        }

                    }
                }
                _tickLastEngineer = now;

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
                _arena.setTicker(1, 1, _config.king.startDelay * 100, "Next game: ",
                    delegate()
                    {	//Trigger the game start
                        startKOTH();
                    }
                );
            }

            return true;
        }


        //Uhm
        public void addBot(Player owner, Helpers.ObjectState state, Team team)
        {
            int id = 0;
            Random rand = new Random(System.Environment.TickCount);

            if (owner == null)
            {//This is a bot team
                int r = rand.Next(0, 4);
                //Find out what bot team this is
                switch (captainBots[team])
                {
                    case 1: id = 301 + r; break; //Collective
                    case 2: id = 307 + r; break; //Europan
                    case 3: id = 313 + r; break; //Faraday
                    case 4: id = 319 + r; break; //Starfire
                    case 5: id = 325 + r; break; //Titan
                }
                //Spawn a random bot in their faction
                if (botCount.ContainsKey(team))
                    botCount[team]++;
                else
                    botCount.Add(team, 0);
                BasicDefense dBot = _arena.newBot(typeof(BasicDefense), (ushort)id, team, null, state, this, null) as BasicDefense;


            }
            else
            {
                if (captainBots.ContainsKey(owner._team))
                {
                    int r = rand.Next(0, 4);
                    switch (captainBots[owner._team])
                    {//Randomize the bots
                        case 1: id = 301 + r; break; //Collective
                        case 2: id = 307 + r; break; //Europan
                        case 3: id = 313 + r; break; //Faraday
                        case 4: id = 319 + r; break; //Starfire
                        case 5: id = 325 + r; break; //Titan
                    }
                    if (botCount.ContainsKey(owner._team))
                        botCount[owner._team]++;
                    else
                        botCount.Add(owner._team, 0);
                    BasicDefense dBot = _arena.newBot(typeof(BasicDefense), (ushort)id, owner._team, owner, state, this, owner) as BasicDefense;

                }
            }
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
        /// Called when a player sends a chat command
        /// </summary>
        [Scripts.Event("Player.ChatCommand")]
        public bool playerChatCommand(Player player, Player recipient, string command, string payload)
        {
            if (command.ToLower().Equals("crown"))
            {   //Give them their crown time if KOTH is enabled
                if (_minPlayers <= 0)
                    player.sendMessage(0, "&KOTH is not enabled in this zone");

                else
                    if (_playerCrownStatus.ContainsKey(player))
                        player.sendMessage(0, "&Crown kills: " + _playerCrownStatus[player].crownKills);
            }

            if (command.ToLower().Equals("co"))
            {
                player.sendMessage(0, "X: " + player._state.positionX + " Y: " + player._state.positionY);
            }

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
        /// Called when a player leaves the game
        /// </summary>
        [Scripts.Event("Player.Leave")]
        public void playerLeave(Player player)
        {//Find out if KOTH is enabled
            if (_minPlayers > 0)
                if (_playerCrownStatus.ContainsKey(player))
                {//Remove their crown and tell everyone
                    _playerCrownStatus[player].crown = false;
                    Helpers.Player_Crowns(_arena, false, _noCrowns);
                }

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
        /// Triggered when a vehicle is created
        /// </summary>
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
            //Did they kill a pylon?
            if (dead._type.Id == _pylonVehID)
                foreach (KeyValuePair<int, pylonObject> obj in _pylons)
                    if (dead._state.positionX == obj.Value.getX() && dead._state.positionY == obj.Value.getY())
                    {//Found which pylon they killed, signal its destruction
                        obj.Value.setExists(false);
                        break;
                    }

            //Did they just kill an HQ?!
            if (dead._type.Id == _hqVehId)
            {
                Team killers = killer._team;

                if (captainBots.ContainsKey(dead._team))
                    captainBots.Remove(dead._team);

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

                    if (!_playerCrownStatus[killer].crown)
                        _playerCrownStatus[killer].crownKills++;
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
    }
}