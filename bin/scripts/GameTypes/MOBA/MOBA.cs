using System;
using System.Linq;
using System.Collections.Generic;

using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;

using Assets;
using InfServer.Logic;

namespace InfServer.Script.GameType_MOBA
{	// Script Class
    /// Provides the interface between the script and arena
    ///////////////////////////////////////////////////////
    public class Script_MOBA : Scripts.IScript
    {	///////////////////////////////////////////////////
        // Member Variables
        ///////////////////////////////////////////////////
        private Arena _arena;					//Pointer to our arena class
        private CfgInfo _config;				//The zone config

        private Team _victoryTeam;				//The team currently winning!

        private int _lastGameCheck;				//The tick at which we last checked for game viability
        private int _tickGameStarting;			//The tick at which the game began starting (0 == not initiated)
        private int _tickGameStart;				//The tick at which the game started (0 == stopped)
        private int _lastTickerCheck;
        private int _tickerCheck;

        private int _lastMinionSpawn;           //The tick at which we last spawned minions
        private int _minionSpawn;               //The amount of time that has to lapse before we spawn a minion
        private int _lastJungleSpawn;
        private int _jungleSpawn;
        private int stage1;                     //Teams 1's stage for minion spawns
        private int stage2;                     //Teams 2's stage for minion spawns
        private int _lastMinionSpacer;
        private int _minionSpacerTick;


        //Leveling stuff
        private int _lastLevelCheck;
        private int _levelCheck;
        private int _levelFactor;
        private int _basexp;

        //Settings
        private int _minPlayers;				//The minimum amount of players

        public int _hqVehId;                   //ID of our HQ
        public int _towerVehId;                //ID of our towers
        public int _inhibVehId;                //ID of our inhibitors
        public int _jungleVehId;               //ID of jungle bot locations
        public int _waypointVehId;

        public string _team1;                  //Names of teams  
        public string _team2;

        private int _team1MinionX;              //Locations for minion spawns
        private int _team1MinionY;
        private int _team2MinionX;
        private int _team2MinionY;

        private int _hqSpawnX1;
        private int _hqSpawny1;
        private int _hqSpawnX2;
        private int _hqSpawny2;

        private bool _team1Upgrade;             //True if team 1 requires stronger bots
        private bool _team2Upgrade;             //True if team 2 requires stronger bots

        //Our Waypoint class
        public class wayPoint
        {
            short x;    //X coordinate of waypoint
            short y;    //Y coordinate of wayypoint
            Team owner; //Owner of waypoint, you want this to be the oppisite team
            int lane;   //Lane of waypoint

            public wayPoint(short xPos, short yPos, Team team, int lan)
            {
                x = xPos;
                y = yPos;
                owner = team;
                lane = lan;
            }
            public short getX()
            { return x; }
            public short getY()
            { return y; }
            public Team getOwner()
            { return owner; }
            public int getLane()
            { return lane; }
            
        }
        public Dictionary<int, wayPoint> _topWaypoints = new Dictionary<int, wayPoint>();
        public Dictionary<int, wayPoint> _bottomWaypoints = new Dictionary<int, wayPoint>();

        //Our tower class
        public class towerObject
        {
            short x;      //X coordinate of tower
            short y;      //Y coordinate of tower
            Team owner;   //The team that owns this object
            bool exists;//Tells us if the tower exists on the map
            int lane;   //Tells us what lane this tower is in
            
            public towerObject(short xPos, short yPos, Team team, int lan)
            {
                exists = false;
                x = xPos;
                y = yPos;
                owner = team;
                lane = lan;
            }
            public int getLane()
            { return lane; }
            public short getX()
            { return x; }
            public short getY()
            { return y; }
            public Team getTeam()
            { return owner; }
            public bool bExists()
            { return exists; }
            public void setExists(bool bExists)
            { exists = bExists; }
        }
        public Dictionary<int, towerObject> _towers = new Dictionary<int, towerObject>();

        public class inhibObject
        {
            short x;      //X coordinate of inhibitor
            short y;      //Y coordinate of inhibitor
            int lane;       //Our lane
            Team owner;   //The team that owns this object
            bool exists;//Tells us if the inhibitor exists on the map

            public inhibObject(short xPos, short yPos, Team team, int lan)
            {
                exists = false;
                x = xPos;
                y = yPos;
                owner = team;
                lane = lan;

            }
            public int getLane()
            { return lane; }
            public short getX()
            { return x; }
            public short getY()
            { return y; }
            public Team getTeam()
            { return owner; }
            public bool bExists()
            { return exists; }
            public void setExists(bool bExists)
            { exists = bExists; }
        }
        public Dictionary<int, inhibObject> _inhibitors;

        public class jungleObject
        {
            short x;      //X coordinate of jungle bot
            short y;      //Y coordinate of jungle bot
            bool exists;//Tells us if the inhibitor exists on the map

            public jungleObject(short xPos, short yPos)
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
        public Dictionary<int, jungleObject> _jungle;

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

            _minPlayers = 1; //We want at least two players to start a game
            _hqVehId = 463;    //Our HQ ID
            _towerVehId = 400; //Our tower ID
            _inhibVehId = 480; //Our inhibitor ID
            _jungleVehId = 401; //Our jungle ID
            _waypointVehId = 999;   //Our waypoint ID

            _team1Upgrade = false; //Default to no upgraded bots
            _team2Upgrade = false;

            _team1MinionX = 1;  //Set where bots will spawn (pixels)
            _team1MinionY = 1;
            _team2MinionX = 1;
            _team2MinionY = 1;

            _minionSpawn = 30000;    //Tick at which we spawn some more minions
            _lastMinionSpawn = 0;
            _jungleSpawn = 60000;   //Tick at which we respawn a jungle bot
            _lastJungleSpawn = 0;
            _tickerCheck = 200;

            _hqSpawnX1 = 224;   //HQ spawn locations, move this later
            _hqSpawny1 = 2192;
            _hqSpawnX2 = 6064;
            _hqSpawny2 = 2192;
          //  _arena._server.
            _lastMinionSpacer = 0;
            _minionSpacerTick = 625;
            stage1 = 0;
            stage2 = 0;

            _lastLevelCheck = 0;
            _levelCheck = 200;
            _levelFactor = 2;
            _basexp = 50;

            _team1 = "Titan Militia";
            _team2 = "Collective";

            _victoryTeam = null;
            
            //Take care of waypoints -- you can switch locations here -- make sure team passed if oppisite of team using waypoint
            _topWaypoints.Add(0, new wayPoint(1032, 1280, _arena.PublicTeams.ElementAt(2), 0)); //Top Lane
            _topWaypoints.Add(1, new wayPoint(1204, 1132, _arena.PublicTeams.ElementAt(2), 0));
            _topWaypoints.Add(2, new wayPoint(1348, 972, _arena.PublicTeams.ElementAt(2), 0));
            _topWaypoints.Add(3, new wayPoint(1524, 808, _arena.PublicTeams.ElementAt(2), 0));
            _topWaypoints.Add(4, new wayPoint(1664, 632, _arena.PublicTeams.ElementAt(2), 0));
            _topWaypoints.Add(5, new wayPoint(1968, 448, _arena.PublicTeams.ElementAt(2), 0));
            _topWaypoints.Add(6, new wayPoint(2292, 480, _arena.PublicTeams.ElementAt(2), 0));
            _topWaypoints.Add(7, new wayPoint(2592, 508, _arena.PublicTeams.ElementAt(2), 0));
            _topWaypoints.Add(8, new wayPoint(3152, 472, _arena.PublicTeams.ElementAt(2), 0));
            _topWaypoints.Add(9, new wayPoint(3628, 476, _arena.PublicTeams.ElementAt(2), 0));
            _topWaypoints.Add(10, new wayPoint(4040, 476, _arena.PublicTeams.ElementAt(2), 0));
            _topWaypoints.Add(11, new wayPoint(4364, 576, _arena.PublicTeams.ElementAt(2), 0));
            _topWaypoints.Add(12, new wayPoint(4544, 752, _arena.PublicTeams.ElementAt(2), 0));
            _topWaypoints.Add(13, new wayPoint(4732, 940, _arena.PublicTeams.ElementAt(2), 0));
            _topWaypoints.Add(14, new wayPoint(5020, 1188, _arena.PublicTeams.ElementAt(2), 0));
            _topWaypoints.Add(15, new wayPoint(5220, 1372, _arena.PublicTeams.ElementAt(2), 0));
            _topWaypoints.Add(16, new wayPoint(5396, 1616, _arena.PublicTeams.ElementAt(2), 0));
            _topWaypoints.Add(17, new wayPoint(5536, 1756, _arena.PublicTeams.ElementAt(2), 0));
            _topWaypoints.Add(18, new wayPoint(5716, 1760, _arena.PublicTeams.ElementAt(2), 0));
            _topWaypoints.Add(19, new wayPoint(5904, 2052, _arena.PublicTeams.ElementAt(2), 0));

            _bottomWaypoints.Add(0, new wayPoint(828, 1956, _arena.PublicTeams.ElementAt(2), 2)); //Bottom Lane
            _bottomWaypoints.Add(1, new wayPoint(1008, 2190, _arena.PublicTeams.ElementAt(2), 2));
            _bottomWaypoints.Add(2, new wayPoint(1132, 2332, _arena.PublicTeams.ElementAt(2), 2));
            _bottomWaypoints.Add(3, new wayPoint(1300, 2498, _arena.PublicTeams.ElementAt(2), 2));
            _bottomWaypoints.Add(4, new wayPoint(1468, 2638, _arena.PublicTeams.ElementAt(2), 2));
            _bottomWaypoints.Add(5, new wayPoint(1564, 2746, _arena.PublicTeams.ElementAt(2), 2));
            _bottomWaypoints.Add(6, new wayPoint(1704, 2882, _arena.PublicTeams.ElementAt(2), 2));
            _bottomWaypoints.Add(7, new wayPoint(1712, 2885, _arena.PublicTeams.ElementAt(2), 2));
            _bottomWaypoints.Add(8, new wayPoint(1904, 3077, _arena.PublicTeams.ElementAt(2), 2));
            _bottomWaypoints.Add(9, new wayPoint(2376, 3024, _arena.PublicTeams.ElementAt(2), 2));
            _bottomWaypoints.Add(10, new wayPoint(2732, 3024, _arena.PublicTeams.ElementAt(2), 2));
            _bottomWaypoints.Add(11, new wayPoint(3420, 3024, _arena.PublicTeams.ElementAt(2), 2));
            _bottomWaypoints.Add(12, new wayPoint(3860, 3024, _arena.PublicTeams.ElementAt(2), 2));
            _bottomWaypoints.Add(13, new wayPoint(4384, 3024, _arena.PublicTeams.ElementAt(2), 2));
            _bottomWaypoints.Add(14, new wayPoint(4568, 2808, _arena.PublicTeams.ElementAt(2), 2));
            _bottomWaypoints.Add(15, new wayPoint(4784, 2612, _arena.PublicTeams.ElementAt(2), 2));
            _bottomWaypoints.Add(16, new wayPoint(4996, 2396, _arena.PublicTeams.ElementAt(2), 2));
            _bottomWaypoints.Add(17, new wayPoint(5128, 2244, _arena.PublicTeams.ElementAt(2), 2));
            _bottomWaypoints.Add(18, new wayPoint(5352, 2032, _arena.PublicTeams.ElementAt(2), 2));
            _bottomWaypoints.Add(19, new wayPoint(5672, 1760, _arena.PublicTeams.ElementAt(2), 2));

            //Take care of all our towers and inhibitors -- you can switch locations here

            _towers.Add(0, new towerObject(1396, 1732, _arena.PublicTeams.ElementAt(0), 1));//Left Base -- Mid
            _towers.Add(1, new towerObject(2368, 1732, _arena.PublicTeams.ElementAt(0), 1));

            _towers.Add(2, new towerObject(1364, 992, _arena.PublicTeams.ElementAt(0), 0));//Top
            _towers.Add(3, new towerObject(2624, 496, _arena.PublicTeams.ElementAt(0), 0));

            _towers.Add(4, new towerObject(2512, 3024, _arena.PublicTeams.ElementAt(0), 2));//Bottom
            _towers.Add(5, new towerObject(1440, 2620, _arena.PublicTeams.ElementAt(0), 2));

            _towers.Add(6, new towerObject(3656, 1732, _arena.PublicTeams.ElementAt(1), 1));//Right Base -- Mid
            _towers.Add(7, new towerObject(4756, 1732, _arena.PublicTeams.ElementAt(1), 1));

            _towers.Add(8, new towerObject(3584, 496, _arena.PublicTeams.ElementAt(1), 0));//Top
            _towers.Add(9, new towerObject(4788, 956, _arena.PublicTeams.ElementAt(1), 0));

            _towers.Add(10, new towerObject(4780, 2600, _arena.PublicTeams.ElementAt(1), 2));//Bottom
            _towers.Add(11, new towerObject(3848, 3024, _arena.PublicTeams.ElementAt(1), 2));


            //Inhibitors
            _inhibitors = new Dictionary<int, inhibObject>();
            _inhibitors.Add(0, new inhibObject(920, 2080, _arena.PublicTeams.ElementAt(0), 1)); //Left Base
            _inhibitors.Add(1, new inhibObject(960, 1344, _arena.PublicTeams.ElementAt(0), 2));//Change lane numbers to correct ones
            _inhibitors.Add(2, new inhibObject(960, 1760, _arena.PublicTeams.ElementAt(0), 0));

            _inhibitors.Add(3, new inhibObject(5220, 1748, _arena.PublicTeams.ElementAt(1), 1)); //Right Base
            _inhibitors.Add(4, new inhibObject(5300, 2060, _arena.PublicTeams.ElementAt(1), 2));
            _inhibitors.Add(5, new inhibObject(5264, 1424, _arena.PublicTeams.ElementAt(1), 0));

            //Jungle bot spawns
            _jungle = new Dictionary<int, jungleObject>();
            _jungle.Add(0, new jungleObject(2776, 1112));

            return true;
        }

        /// <summary>
        /// Allows the script to maintain itself
        /// </summary>
        public bool poll()
        {	//Should we check game state yet?
            int now = Environment.TickCount;

            if (now - _lastGameCheck <= Arena.gameCheckInterval)
                return true;
            _lastGameCheck = now;

            //Do we have enough players ingame?
            int playing = _arena.PlayerCount;

            if ((_tickGameStart == 0 || _tickGameStarting == 0) && playing < _minPlayers)
            {	//Stop the game!
                _arena.setTicker(1, 1, 0, "Not Enough Players");
                _arena.gameReset();
            }

            //Do we have enough players to start a game?
            else if (_tickGameStart == 0 && _tickGameStarting == 0 && playing >= _minPlayers)
            {	//Great! Get going
                _tickGameStarting = now;
                _arena.setTicker(1, 1, _config.flag.startDelay * 100, "Next game: ",
                    delegate()
                    {	//Trigger the game start
                        _arena.gameStart();
                    }
                );
            }

            //Find out if anyone won yet
            if (_victoryTeam != null)
            {//Someone won
                _arena.gameEnd();
            }

            if (_tickGameStart > 0 && now - _lastTickerCheck > _tickerCheck)
            {//Update our tickers
                updateTickers();
                _lastTickerCheck = now;
            }

            //Update levels
            if (_tickGameStart > 0 && now - _lastLevelCheck > _levelCheck)
            {
                foreach (Player p in _arena.PlayersIngame.ToList())
                {
                    if (p.Experience >= ((((p._level + 1) ^ _levelFactor)) * _basexp) + 1 && p._level < 30)
                    {
                        p.Bounty = ++p._level;
                        p.Experience -= (((p._level + 1) ^ _levelFactor) * _basexp);
                        p.syncState();
                        p.sendMessage(3, "You have reached level " + p._level + "!");
                    }
                }
                _lastLevelCheck = now;
            }

            //Check to see if we need to spawn any jungle bots
            if (_tickGameStart > 0 && now - _lastJungleSpawn > _jungleSpawn)
            {
                spawnJungle();
                _lastJungleSpawn = now;
            }
            //Check to see if we need to spawn minions
            if (_tickGameStart > 0 && now - _lastMinionSpawn > _minionSpawn)
            {
                stage1 = 1; //Start off with first set
                stage2 = 1;
                _lastMinionSpawn = now;
            }

            //Check to see if we need to spawn minions
            if (_tickGameStart > 0 && now - _lastMinionSpacer > _minionSpacerTick)
            {//Spawn the minions.
                foreach (Team t in _arena.Teams)
                {
                    if (t._name.Contains(_team1))//Titan
                    {
                        Helpers.ObjectState state = new Helpers.ObjectState();
                        state.positionX = 732;
                        state.positionY = 1664;
                        state.positionZ = 0;
                        state.yaw = 0;
                        //Spawn minion based on stage
                        switch (stage1)
                        {
                            case 0:
                                break;
                            case 1:
                                _arena.newBot(typeof(MeleeMinion), (ushort)324, t, null, state, this, t, 0, 0);
                                _arena.newBot(typeof(MeleeMinion), (ushort)324, t, null, state, this, t, 2, 0);
                                _arena.newBot(typeof(MeleeMinion), (ushort)324, t, null, state, this, t, 1, 0);
                                stage1++;
                                break;
                            case 2:
                                _arena.newBot(typeof(MeleeMinion), (ushort)324, t, null, state, this, t, 0, 0);
                                _arena.newBot(typeof(MeleeMinion), (ushort)324, t, null, state, this, t, 2, 0);
                                _arena.newBot(typeof(MeleeMinion), (ushort)324, t, null, state, this, t, 1, 0);
                                stage1++;
                                break;
                            case 3:
                                _arena.newBot(typeof(MeleeMinion), (ushort)324, t, null, state, this, t, 0, 0);
                                _arena.newBot(typeof(MeleeMinion), (ushort)324, t, null, state, this, t, 2, 0);
                                _arena.newBot(typeof(MeleeMinion), (ushort)324, t, null, state, this, t, 1, 0);
                                stage1++;
                                break;
                            case 4:
                                _arena.newBot(typeof(RangeMinion), (ushort)315, t, null, state, this, t, 0, 0);
                                _arena.newBot(typeof(RangeMinion), (ushort)315, t, null, state, this, t, 2, 0);
                                _arena.newBot(typeof(RangeMinion), (ushort)315, t, null, state, this, t, 1, 0);
                                stage1++;
                                break;
                            case 5:
                                _arena.newBot(typeof(RangeMinion), (ushort)315, t, null, state, this, t, 0, 0);
                                _arena.newBot(typeof(RangeMinion), (ushort)315, t, null, state, this, t, 2, 0);
                                _arena.newBot(typeof(RangeMinion), (ushort)315, t, null, state, this, t, 1, 0);
                                stage1 = 0;
                                break;
                        }
                    }
                    if (t._name.Contains(_team2))//Collective
                    {
                        Helpers.ObjectState state = new Helpers.ObjectState();
                        state.positionX = 5520;
                        state.positionY = 1764;
                        state.positionZ = 0;
                        state.yaw = 0;
                        //Spawn minion based on stage
                        switch (stage2)
                        {
                            case 0:
                                break;
                            case 1:
                                _arena.newBot(typeof(MeleeMinion), (ushort)313, t, null, state, this, t, 0, 1);
                                _arena.newBot(typeof(MeleeMinion), (ushort)313, t, null, state, this, t, 2, 1);
                                _arena.newBot(typeof(MeleeMinion), (ushort)313, t, null, state, this, t, 1, 1);
                                stage2++;
                                break;
                            case 2:
                                _arena.newBot(typeof(MeleeMinion), (ushort)313, t, null, state, this, t, 0, 1);
                                _arena.newBot(typeof(MeleeMinion), (ushort)313, t, null, state, this, t, 2, 1);
                                _arena.newBot(typeof(MeleeMinion), (ushort)313, t, null, state, this, t, 1, 1);
                                stage2++;
                                break;
                            case 3:
                                _arena.newBot(typeof(MeleeMinion), (ushort)313, t, null, state, this, t, 0, 1);
                                _arena.newBot(typeof(MeleeMinion), (ushort)313, t, null, state, this, t, 2, 1);
                                _arena.newBot(typeof(MeleeMinion), (ushort)313, t, null, state, this, t, 1, 1);
                                stage2++;
                                break;                            
                            case 4:
                                _arena.newBot(typeof(RangeMinion), (ushort)314, t, null, state, this, t, 0, 1);
                                _arena.newBot(typeof(RangeMinion), (ushort)314, t, null, state, this, t, 2, 1);
                                _arena.newBot(typeof(RangeMinion), (ushort)314, t, null, state, this, t, 1, 1);
                                stage2++;
                                break;
                            case 5:
                                _arena.newBot(typeof(RangeMinion), (ushort)314, t, null, state, this, t, 0, 1);
                                _arena.newBot(typeof(RangeMinion), (ushort)314, t, null, state, this, t, 2, 1);
                                _arena.newBot(typeof(RangeMinion), (ushort)314, t, null, state, this, t, 1, 1);
                                stage2 = 0;
                                break;
                        }
                    }
                }
                _lastMinionSpacer = now;
            }

            return true;
        }

        //Spawns jungle bots
        public void spawnJungle()
        {
            foreach (KeyValuePair<int, jungleObject> obj in _jungle)
            {
                if (!obj.Value.bExists())
                {//Doesn't exist, find our location to spawn at
                    foreach (Vehicle v in _arena.Vehicles)
                        if (v._type.Id == _jungleVehId && v._state.positionX == obj.Value.getX() && v._state.positionY == obj.Value.getY())
                        {//Spawn the random bot at the correct location
                            //Find out if we can spawn them yet -- TODO
                            _arena.newBot(typeof(JungleBot), (ushort)107, _arena.Teams.ElementAt(2), null, v._state, this, v);
                            obj.Value.setExists(true);
                            break;
                        }

                    break;
                }
            }
        }

        //Spawns a bot
        public void spawnProjectile(ushort id, Helpers.ObjectState state, Team team)
        {
            _arena.newBot(typeof(Projectile), id, team, null, state, this, team);
        }
        //Handles vehicles killed by minions since killer is null
        public void teamReward(int killID, Team team)
        {
            int cashReward = 0;
            int expReward = 0;

            if (killID == _towerVehId)
            {
                cashReward = 5000;
                expReward = 200;
            }
            foreach (Player p in team.ActivePlayers)
            {
                p.Cash += cashReward;
                p.Experience += expReward;
                p.syncState();
                p.sendMessage(0, "Destroyed a Tower! Exp = " + expReward + " Cash = " + cashReward);
            }

        }

        //Updates our tickers
        public void updateTickers()
        {
            _arena.setTicker(1, 2, 0, delegate(Player p)
            {
                return "Current Experience=" + p.Experience + "    Experience for next level=" + ((((p._level + 1) ^ _levelFactor) * _basexp) + 1); ;
            });
            _arena.setTicker(3, 3, 0, delegate(Player p)
            {
                return "Current Level=" + p._level;
            });
        }
        //Destroys all the vehicles we spawned for a game including bots
        public void destroyAllVehs()
        {
            foreach (Vehicle v in _arena.Vehicles.ToList())
            {
                int id = v._type.Id;
                if (id == _inhibVehId || id == _jungleVehId || id == _hqVehId || id == _towerVehId || id == _waypointVehId)
                    v.kill(null);
                if (v._bBotVehicle)
                    v.kill(null);
            }
        }
        //Spawns all the HQs, inhibitors, jungle spawns, towers, and waypoints
        public void spawnAllVehs()
        {
            //Spawn all jungle locations
            foreach (KeyValuePair<int, jungleObject> obj in _jungle)
            {
                VehInfo jungle = _arena._server._assets.getVehicleByID(Convert.ToInt32(_jungleVehId));
                Helpers.ObjectState waypointState = new Protocol.Helpers.ObjectState();
                waypointState.positionX = obj.Value.getX();
                waypointState.positionY = obj.Value.getY();
                waypointState.positionZ = 0;
                waypointState.yaw = 0;
                _arena.newVehicle(
                                jungle,
                                _arena.Teams.ElementAt(2), null,
                               waypointState);
            }

            //Spawn all waypoints
            foreach (KeyValuePair<int, wayPoint> obj in _topWaypoints)
            {
                VehInfo waypoint = _arena._server._assets.getVehicleByID(Convert.ToInt32(_waypointVehId));
                Helpers.ObjectState waypointState = new Protocol.Helpers.ObjectState();
                waypointState.positionX = obj.Value.getX();
                waypointState.positionY = obj.Value.getY();
                waypointState.positionZ = 0;
                waypointState.yaw = 0;
                _arena.newVehicle(
                                waypoint,
                                obj.Value.getOwner(), null,
                               waypointState);
            }
            foreach (KeyValuePair<int, wayPoint> obj in _bottomWaypoints)
            {
                VehInfo waypoint = _arena._server._assets.getVehicleByID(Convert.ToInt32(_waypointVehId));
                Helpers.ObjectState waypointState = new Protocol.Helpers.ObjectState();
                waypointState.positionX = obj.Value.getX();
                waypointState.positionY = obj.Value.getY();
                waypointState.positionZ = 0;
                waypointState.yaw = 0;
                _arena.newVehicle(
                                waypoint,
                                obj.Value.getOwner(), null,
                               waypointState);
            }

            //Spawn all the inhibitors
            foreach (KeyValuePair<int, inhibObject> obj in _inhibitors)
            {
                VehInfo vehicle = _arena._server._assets.getVehicleByID(Convert.ToInt32(_inhibVehId));
                Helpers.ObjectState newState = new Protocol.Helpers.ObjectState();
                newState.positionX = obj.Value.getX();
                newState.positionY = obj.Value.getY();
                newState.positionZ = 0;
                newState.yaw = 0;

                obj.Value.setExists(true);


                _arena.newVehicle(
                            vehicle,
                            obj.Value.getTeam(), null,
                            newState);
            }
            //Spawn all the towers
            foreach (KeyValuePair<int, towerObject> obj in _towers)
            {
                VehInfo vehicle = _arena._server._assets.getVehicleByID(Convert.ToInt32(_towerVehId));
                Helpers.ObjectState newState = new Protocol.Helpers.ObjectState();
                newState.positionX = obj.Value.getX();
                newState.positionY = obj.Value.getY();
                newState.positionZ = 0;
                newState.yaw = 0;

                obj.Value.setExists(true);
                Tower tower = _arena.newVehicle(
                            vehicle,
                            obj.Value.getTeam(), null,
                            newState, null, typeof(Tower)) as Tower;
                tower._moba = this;

            }

            //Reset everyones level(bounty) and experience
            foreach (Player p in _arena.Players.ToList())
            {
                p._level = 1;
                p.Experience = 0;
                p.Bounty = 1;
                p.syncState();
            }

            //Spawn all the HQs
            VehInfo HQ = _arena._server._assets.getVehicleByID(Convert.ToInt32(_hqVehId));
            Helpers.ObjectState hqState = new Protocol.Helpers.ObjectState();
            hqState.positionX = (short)_hqSpawnX1;
            hqState.positionY = (short)_hqSpawny1;
            _arena.newVehicle(
                        HQ,
                        _arena.Teams.ElementAt(0), null,
                        hqState, null);
            hqState.positionX = (short)_hqSpawnX2;
            hqState.positionY = (short)_hqSpawny2;
            _arena.newVehicle(
                        HQ,
                        _arena.Teams.ElementAt(1), null,
                        hqState, null);

        }

        /// <summary>
        /// Called when a player sends a chat command
        /// </summary>
        [Scripts.Event("Player.ChatCommand")]
        public bool playerChatCommand(Player player, Player recipient, string command, string payload)
        {

            if (command.ToLower().Equals("co"))
            {
                player.sendMessage(0, "X: " + player._state.positionX + " Y: " + player._state.positionY);
            }
            
            return true;
        }
        /// <summary>
        /// Triggered when a vehicle dies
        /// </summary>
        [Scripts.Event("Vehicle.Death")]
        public bool vehicleDeath(Vehicle dead, Player killer)
        {
            //Did they kill a tower?
            if (dead._type.Id == _towerVehId)
            {//Yes, let's inform everyone
                if (killer != null)
                {
                    _arena.sendArenaMessage(killer._alias + " has destroyed " + dead._team._name + "'s tower!");
                    teamReward(dead._type.Id, killer._team);
                }
                else
                    _arena.sendArenaMessage("A minion has destroyed " + dead._team._name + "'s tower!");

            }

            //Did they kill an inhibitor?
            if (dead._type.Id == _inhibVehId)
            {//Yes, so lets inform everyone
                _arena.sendArenaMessage(killer._alias + " has destroyed " + dead._team._name + "'s inhibitor!");
                //Now give them better bots on next spawn
                //Check their team to determine who gets the better bot spawn
                if (killer._team._name.Equals(_team1))
                {
                    _team1Upgrade = true;
                    foreach (Player p in killer._team.ActivePlayers)
                        p.sendMessage(1, "Your HQ will now produce upgraded minions");
                }
                else
                {
                    _team2Upgrade = true;
                    foreach (Player p in killer._team.ActivePlayers)
                        p.sendMessage(1, "Your HQ will now produce upgraded minions");
                }
            }

            //Did they kill an HQ?
            if (dead._type.Id == _hqVehId)
            {//Yes, signal that a team has won
                _arena.sendArenaMessage(killer._alias + " has destroyed " + dead._team._name + "'s HQ!");
                _victoryTeam = killer._team;
            }

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
            //Spawn everything            
            spawnAllVehs();
            //Let everyone know
            _arena.sendArenaMessage("Game has started!", _config.flag.resetBong);

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
            //Clear the map
            destroyAllVehs();
            //Reset our variables
            _lastJungleSpawn = 0;
            _lastMinionSpacer = 0;
            _lastMinionSpawn = 0;
            _lastLevelCheck = 0;
            _lastGameCheck = 0;

            _victoryTeam = null;

            _team1Upgrade = false;
            _team2Upgrade = false;

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
            //Clear the map
            destroyAllVehs();
            //Reset our variables
            _victoryTeam = null;

            _lastJungleSpawn = 0;
            _lastMinionSpacer = 0;
            _lastMinionSpawn = 0;
            _lastLevelCheck = 0;
            _lastGameCheck = 0;

            _team1Upgrade = false;
            _team2Upgrade = false;

            return true;
        }

        /// <summary>
        /// Triggered when a player notifies the server of an explosion
        /// </summary>
        [Scripts.Event("Player.Explosion")]
        public bool playerExplosion(Player player, ItemInfo.Projectile weapon, short posX, short posY, short posZ)
        {
            if (weapon.id == 999)
            {//Warp weapon
                player.warp(posX, posY);
            }
            if (weapon.id == 1282)
            {//Tower weapon
                _arena.newBot(typeof(Projectile), (ushort)104, player._team, null, player._state, this, player._team);
            }
            return true;
        }

    }
}