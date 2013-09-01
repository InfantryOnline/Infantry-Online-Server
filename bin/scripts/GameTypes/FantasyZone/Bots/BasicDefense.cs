using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;

using InfServer.Game;
using InfServer.Protocol;
using InfServer.Scripting;
using InfServer.Bots;

using Assets;
using Axiom.Math;
using Bnoerj.AI.Steering;

namespace InfServer.Script.GameType_Fantasy
{
    //Minion bot, will spawn at team's HQ and run down its path searching for the closest enemy tower or enemy minions or enemy
    class RangeMinion : Bot
    {
        ///////////////////////////////////////////////////
        // Member variables
        ///////////////////////////////////////////////////

        protected SteeringController steering;	//System for controlling the bot's steering
        protected List<Vector3> _path;			//The path to our destination
        protected int _pathTarget;				//The next target node of the path
        protected int _tickLastPath;			//The time at which we last made a path to follow
        protected int _pathUpdateInterval = 5000;      //The tick at which we update our path

        //Stuff for 
        private int _homeID;                    //Home ID of this bot
        private Vehicle _home;                  //Our home
        private int _vehID;                      //Vehicle ID of this bot
        //Targeting
        private Player _targetPlayer;         //Our target player 
        private Vehicle _targetVehicle;       //Our target bot or vehicle
        private int _defenseRadius;           //We won't attack people outside of this radius

        //These determine whether or not to target specific things
        private bool targetBot;
        private bool targetPlayer;
        private bool targetVehicles;

        private int _distanceFromHome; //Distance from home until we decide to ignore everything else and run back home [if not attacking] 
        private int _radiusToPatrol;
        private int _lockInTime;        //How long do we stay locked on one target
        private int _attackRadius;      //Distance from home that we activly chase people until going back to the _distanceFromHome radius
        private int _lastTargetSearch;      //Tick at which we last looked for a target
        private int _lastWeaponSwitch;      //Tick at which we last chose our weapon
        private int _weaponSwitchTime;      //How often we switch weapons

        private int defaultWeapon;

        protected bool _attacking;              //Are we attacking something?
        private bool _ceaseFire;
        private Script_Fantasy _script;
        //Ranges for weapons
        private int _closeRange = 1;
        private int _midRange = 101;
        private int _longRange = 301;
        private Random rnd = new Random(Environment.TickCount);
        protected int _maxWaypoints = 19;
        private int _tickRandomPath = 0;

        //Weapons systems
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

        private float _seperation;

        ///////////////////////////////////////////////////
        // Member Functions
        ///////////////////////////////////////////////////

        /// <summary>
        /// Generic constructor
        /// Type = Vehicle bot uses
        /// State = direction and location of bot for spawn
        /// Arena = the arena the bot is in
        /// </summary>
        public RangeMinion(VehInfo.Car type, Helpers.ObjectState state, Arena arena, Script_Fantasy script, int ID)
            //: base(
            : base(type, state, arena,
                    new SteeringController(type, state, arena))
        {
            Random rnd = new Random(Environment.TickCount);
            _attacking = false;

            _seperation = (float)rnd.NextDouble();
            steering = _movement as SteeringController;


            //Assign all our stats
            foreach (var p in script._spawns)
                if (p.ID == ID)
                {//Found our entry, now set all the settings  
                    //Targetting settings
                    targetBot = p.atkBots;
                    targetVehicles = p.atkVeh;
                    targetPlayer = p.atkPlayer;

                    _homeID = p.spawnID;
                    _vehID = p.vehID;
                    //Spawn point settings
                    //p.relyOnSpawn;

                    //Distances
                    _defenseRadius = p.defenseRadius;
                    _distanceFromHome = p.distanceFromHome;
                    _radiusToPatrol = p.patrolRadius;
                    _lockInTime = p.lockInTime;
                    _attackRadius = p.attackRadius;
                    foreach (var w in p._weapons)
                    {
                        _weapons.Add(new BotWeapon(w.ID));
                        _weapons[w.ID].weaponID = w.weaponID;
                        _weapons[w.ID].allChance = w.allChance;
                        _weapons[w.ID].shortChance = w.shortChance;
                        _weapons[w.ID].midChance = w.midChance;
                        _weapons[w.ID].longChance = w.longChance;
                        _weapons[w.ID].preferredRange = w.preferredRange;
                    }
                    _midRange = p.shortRange + 1;
                    _longRange = p.longRange + 1;

                }
            //By default set out current weapon to close range
            if (type.InventoryItems[0] != 0)
                _weapon.equip(AssetManager.Manager.getItemByID(type.InventoryItems[0]));

        }

        /// <summary>
        /// Looks after the bot's functionality
        /// </summary>
        public override bool poll()
        {
            int now = Environment.TickCount;
            //Find our home
            _home = getHome();
            //Check if we are dead or if our home is dead
            if (IsDead || _home == null)
            {
                steering.steerDelegate = null; //Stop movements                
                bCondemned = true; //Make sure the bot gets removed in Bot class poll

                return base.poll();
            }

            //Find the nearest player or computer vehicle if there is one
            if (_targetPlayer == null || _targetVehicle == null || now - _lastTargetSearch > _lockInTime)
            {
                _targetPlayer = getTargetPlayer();
                //_targetVehicle = getTargetVehicle();
            }

            //Check if we have any player targets
            if (_targetPlayer != null)
            {
                Vehicle waypoint = getWaypoint(402);
                if (waypoint != null && Helpers.distanceTo(_state, waypoint._state) < Helpers.distanceTo(_state, _targetPlayer._state))
                {
                    goToWaypoint();
                    Console.WriteLine("Going to waypoint");
                }
                else
                {
                    attackPlayer();
                    //   Console.WriteLine("Going to player");
                }
                return base.poll();
            }

            //Check if we have any computer targets
            if (_targetVehicle != null)
            {
                //     Vehicle waypoint = getWaypoint(402);
                //      if (waypoint != null && Helpers.distanceTo(_state, waypoint._state) > Helpers.distanceTo(_state, _targetVehicle._state))
                attackVehicle();
                //       else
                //           goToWaypoint();

                return base.poll();
            }

            //Find out if we have to go back home
            //We have the option of moving farther away from home when engaged in combat
            if (Helpers.distanceTo(_state, _home._state) < _attackRadius && _targetPlayer != null)
            {
                Vehicle waypoint = getWaypoint(402);
                if (waypoint != null && Helpers.distanceTo(_state, waypoint._state) < Helpers.distanceTo(_state, _targetPlayer._state))
                    goToWaypoint();
                else
                    attackPlayer();

                return base.poll();
            }
            if (Helpers.distanceTo(_state, _home._state) > _distanceFromHome && _targetPlayer == null)
            {
                Vehicle waypoint = getWaypoint(402);
                if (waypoint != null && Helpers.distanceTo(_state, waypoint._state) < Helpers.distanceTo(_state, _home._state))

                    goToWaypoint();
                else
                    goHome();
                return base.poll();
            }
            //Causing memory issues
            //No targets found, just roam the base
            Vehicle wp = getWaypoint(402);
            if (wp != null && Helpers.distanceTo(_state, wp._state) < Helpers.distanceTo(_state, _home._state))

                goToWaypoint();
            else
                goPatrol();


            //Handle normal functionality
            return base.poll();
        }

        /// <summary>
        /// Causes our bot to attack a player
        /// </summary>
        private void attackPlayer()
        {
            int now = Environment.TickCount;

            bool bClearPath = Helpers.calcBresenhemsPredicate(_arena, _state.positionX, _state.positionY, _targetPlayer._state.positionX, _targetPlayer._state.positionY,
                     delegate(LvlInfo.Tile t)
                     {
                         return !t.Blocked;
                     }
                 );
            if (bClearPath)
            {	//Persue directly!
                steering.steerDelegate = steerForPersuePlayer;
                //Equip the right weapon for the moment
                getWeapon((int)Helpers.distanceTo(_state, _targetPlayer._state));
                //Can we shoot?
                if (_weapon.ableToFire())
                {
                    int aimResult = _weapon.getAimAngle(_targetPlayer._state);

                    if (_weapon.isAimed(aimResult))
                    {	//Spot on! Fire?
                        _itemUseID = _weapon.ItemID;
                        _weapon.shotFired();
                    }
                    steering.bSkipAim = true;
                    steering.angle = aimResult;
                }
                else
                    steering.bSkipAim = false;
            }
            else
            {
                //Does our path need to be updated?
                if (now - _tickLastPath > _pathUpdateInterval)
                {
                    //Update it!
                    _tickLastPath = int.MaxValue;

                    _arena._pathfinder.queueRequest(
                        (short)(_state.positionX / 16), (short)(_state.positionY / 16),
                        (short)(_targetPlayer._state.positionX / 16), (short)(_targetPlayer._state.positionY / 16),
                        delegate(List<Vector3> path, int pathLength)
                        {
                            if (path != null)
                            {
                                _path = path;
                                _pathTarget = 1;

                            }
                            _tickLastPath = now;
                        }
                    );
                }
            }
        }
        /// <summary>
        /// Causes our bot to attack a vehicle
        /// </summary>
        private void attackVehicle()
        {
            int now = Environment.TickCount;

            bool bClearPath = Helpers.calcBresenhemsPredicate(_arena, _state.positionX, _state.positionY, _targetVehicle._state.positionX, _targetVehicle._state.positionY,
                     delegate(LvlInfo.Tile t)
                     {
                         return !t.Blocked;
                     }
                 );
            if (bClearPath)
            {	//Persue directly!
                steering.steerDelegate = steerForPersueVehicle;
                //Equip the right weapon for the moment
                getWeapon((int)Helpers.distanceTo(_state, _targetVehicle._state));
                //Can we shoot?
                if (_weapon.ableToFire())
                {
                    int aimResult = _weapon.getAimAngle(_targetVehicle._state);

                    if (_weapon.isAimed(aimResult))
                    {	//Spot on! Fire?
                        _itemUseID = _weapon.ItemID;
                        _weapon.shotFired();
                    }
                    steering.bSkipAim = true;
                    steering.angle = aimResult;
                }
                else
                    steering.bSkipAim = false;
            }
            else
            {
                //Does our path need to be updated?
                if (now - _tickLastPath > _pathUpdateInterval)
                {
                    //Update it!
                    _tickLastPath = int.MaxValue;

                    _arena._pathfinder.queueRequest(
                        (short)(_state.positionX / 16), (short)(_state.positionY / 16),
                        (short)(_targetVehicle._state.positionX / 16), (short)(_targetVehicle._state.positionY / 16),
                        delegate(List<Vector3> path, int pathLength)
                        {
                            if (path != null)
                            {
                                _path = path;
                                _pathTarget = 1;

                            }
                            _tickLastPath = now;
                        }
                    );
                }
            }

        }
        /// <summary>
        /// Causes our bot to patrol the base
        /// </summary>
        private void goPatrol()
        {
            int now = Environment.TickCount;
            //    _arena._pathfinder.queueRequest(
            //     (short)(_state.positionX / 16), (short)(_state.positionY / 16),
            //       (short)((_state.positionX + rnd.Next(-_radiusToPatrol, _radiusToPatrol)) / 16), (short)((_state.positionY + rnd.Next(-_radiusToPatrol, _radiusToPatrol)) / 16),
            bool bClearPath = true;
            bClearPath = Helpers.calcBresenhemsPredicate(_arena, _state.positionX, _state.positionY, (short)(_state.positionX + rnd.Next(-_radiusToPatrol, _radiusToPatrol)), (short)(_state.positionY + rnd.Next(-_radiusToPatrol, _radiusToPatrol)),
                 delegate(LvlInfo.Tile t)
                 {
                     return (!t.Blocked);
                 }
             );

            if (bClearPath)
            {	//Persue directly!
                steering.steerDelegate = steerForFollowOwner;
            }
            else
            {
                //int now = Environment.TickCount;
                //Does our path need to be updated?
                if (now - _tickLastPath > _pathUpdateInterval)
                {
                    //Update it!
                    _tickLastPath = int.MaxValue;
                    _attacking = false;

                    _arena._pathfinder.queueRequest(
                             (short)(_state.positionX / 16), (short)(_state.positionY / 16),
                               (short)((_state.positionX + rnd.Next(-_radiusToPatrol, _radiusToPatrol)) / 16), (short)((_state.positionY + rnd.Next(-_radiusToPatrol, _radiusToPatrol)) / 16),
                        delegate(List<Vector3> path, int pathLength)
                        {
                            if (path != null)
                            {
                                _path = path;
                                _pathTarget = 1;
                            }
                            _tickLastPath = now;
                        }
                    );
                }
                if (_path == null)
                    //If we can't find our way to vehicle, just mindlessly walk in its direction for now [this should never happen
                    steering.steerDelegate = steerForFollowOwner;
                else
                    steering.steerDelegate = steerAlongPath;
            }
        }

        /// <summary>
        /// Causes our bot to patrol the base
        /// </summary>
        private void goToWaypoint()
        {
            Vehicle waypoint = getWaypoint(1);
            if (waypoint == null)
                return;

            bool bClearPath = true;
            bClearPath = Helpers.calcBresenhemsPredicate(_arena, _state.positionX, _state.positionY, waypoint._state.positionX, waypoint._state.positionY,
                 delegate(LvlInfo.Tile t)
                 {
                     return (!t.Blocked);
                 }
             );

            if (bClearPath)
            {	//Persue directly!
                steering.steerDelegate = steerForFollowOwner;
            }
            else
            {
                int now = Environment.TickCount;
                //Does our path need to be updated?
                if (now - _tickLastPath > _pathUpdateInterval)
                {
                    //Update it!
                    _tickLastPath = int.MaxValue;
                    _attacking = false;

                    _arena._pathfinder.queueRequest(
                        (short)(_state.positionX / 16), (short)(_state.positionY / 16),
                        (short)(waypoint._state.positionX / 16), (short)(waypoint._state.positionY / 16),
                        delegate(List<Vector3> path, int pathLength)
                        {
                            if (path != null)
                            {
                                _path = path;
                                _pathTarget = 1;
                            }
                            _tickLastPath = now;
                        }
                    );
                }
                if (_path == null)
                    //If we can't find our way to vehicle, just mindlessly walk in its direction for now [this should never happen
                    steering.steerDelegate = steerForFollowOwner;
                else
                    steering.steerDelegate = steerAlongPath;
            }

        }

        /// <summary>
        /// Steers our bot back home
        /// </summary>
        private void goHome()
        {
            bool bClearPath = true;
            bClearPath = Helpers.calcBresenhemsPredicate(_arena, _state.positionX, _state.positionY, _home._state.positionX, _home._state.positionY,
                 delegate(LvlInfo.Tile t)
                 {
                     return (!t.Blocked);
                 }
             );

            if (bClearPath)
            {	//Persue directly!
                steering.steerDelegate = steerForFollowOwner;
            }
            else
            {
                int now = Environment.TickCount;
                //Does our path need to be updated?
                if (now - _tickLastPath > _pathUpdateInterval)
                {
                    //Update it!
                    _tickLastPath = int.MaxValue;
                    _attacking = false;

                    _arena._pathfinder.queueRequest(
                        (short)(_state.positionX / 16), (short)(_state.positionY / 16),
                        (short)(_home._state.positionX / 16), (short)(_home._state.positionY / 16),
                        delegate(List<Vector3> path, int pathLength)
                        {
                            if (path != null)
                            {
                                _path = path;
                                _pathTarget = 1;
                            }
                            _tickLastPath = now;
                        }
                    );
                }
                if (_path == null)
                    //If we can't find our way to vehicle, just mindlessly walk in its direction for now [this should never happen
                    steering.steerDelegate = steerForFollowOwner;
                else
                    steering.steerDelegate = steerAlongPath;
            }
        }
        /// <summary>
        /// Obtains the current weapon to be used by the bot
        /// </summary>
        private Vehicle getWaypoint(int wID)
        {
            double lastx = 0;
            //Make a list of all waypoints 
            List<Vehicle> wpts = new List<Vehicle>();
            wpts = _arena.getVehiclesInRange(_state.positionX, _state.positionY, 500, j => j._type.Id == wID);
            //Go through all waypoints
            foreach (Vehicle v in wpts)
            {
                //Check how far it is from us (x)
                double x = Helpers.distanceTo(_state, v._state);

                //Check to make sure we are going in order
                if (lastx == 0)
                    lastx = x;
                if (x > lastx)
                    continue;

                //Check if we are facing it 
                int aimAngle = _weapon.getAimAngle(v._state);
                if (!_weapon.isAimed(aimAngle))
                    continue;

                //Now check if it makes sense to use that waypoint
                /*   if (_targetPlayer != null)
                   {
                       Console.WriteLine("CHECKING AIM ANGLE  " + _weapon.getAimAngle(_targetPlayer._state));
                       aimAngle = _weapon.getAimAngle(_targetPlayer._state);
                       if (!_weapon.isAimed(aimAngle))
                           continue;
                   }*/
                /*    else if (_targetVehicle != null)
                    {
                        aimAngle = _weapon.getAimAngle(_targetVehicle._state);
                        if (!_weapon.isAimed(aimAngle))
                            continue;
                    }*/
                //Check if we have vision of it just in case
                bool bClearPath = true;
                bClearPath = Helpers.calcBresenhemsPredicate(_arena, _state.positionX, _state.positionY, _home._state.positionX, _home._state.positionY,
                     delegate(LvlInfo.Tile t)
                     {
                         return (!t.Blocked);
                     }
                 );
                if (!bClearPath)
                    continue;
                //Found our waypoint
                return v;
            }
            return null;
        }
        /// <summary>
        /// Obtains the current weapon to be used by the bot
        /// </summary>
        private void getWeapon(int distance)
        {
            int now = Environment.TickCount;
            if (now - _lastWeaponSwitch < _weaponSwitchTime)
                return;
            //Make a list of all weapons to use for given distance
            //todo: randomize them
            List<int> weaponChoices = new List<int>();
            //Go through weapons
            foreach (var w in _weapons)
            {//Find the ones for close range if that is where we are
                if (distance <= _midRange)
                {//Close range
                    //Find out if we can use this
                    double chance = w.shortChance;
                    int random = rnd.Next(0, 1000);
                    if (chance < random)
                        weaponChoices.Add(w.weaponID);
                    else
                        continue;
                }
                //Check mid range
                else if (distance <= _longRange)
                {//Mid range
                    //Find out if we can use this
                    double chance = w.midChance;
                    int random = rnd.Next(0, 1000);
                    if (chance < random)
                        weaponChoices.Add(w.weaponID);
                    else
                        continue;
                }
                //Check long range
                else
                {//Long range
                    //Find out if we can use this
                    double chance = w.longChance;
                    int random = rnd.Next(0, 1000);
                    if (chance < random)
                        weaponChoices.Add(w.weaponID);
                    else
                        continue;
                }
            }
            //Now pick the appropriate weapon to use in this circumstance
            if (weaponChoices.Count > 0)
            {
                int r = rnd.Next(weaponChoices.Count);
                _weapon.equip(AssetManager.Manager.getItemByID(weaponChoices[r]));
            }

        }
        /// <summary>
        /// Obtains the home of the bot
        /// </summary>
        private Vehicle getHome()
        {
            //Go through the list and find the nearest vehicle that is our home
            foreach (Vehicle v in _arena.Vehicles.ToList())
                if (v._type.Id == _homeID)
                    return v;
            //Our home is missing
            return null;
        }
        /// <summary>
        /// Obtains a vehicle target
        /// </summary>
        private Vehicle getTargetVehicle()
        {
            List<Vehicle> inTrackingRange =
                _arena.getVehiclesInRange(_state.positionX, _state.positionY, _defenseRadius);
            List<int> homeIDs = new List<int>();
            //Go through the list and find nearest target
            foreach (Vehicle v in inTrackingRange)
            {
                //Check some things
                if (v._team == _team)
                    continue;


                foreach (var s in _script._spawns)
                    homeIDs.Add(s.spawnID);

                if (homeIDs.Contains(v._type.Id))
                    continue;

                if (v._type.Id == _homeID)
                    continue;

                if (v.IsDead)
                    continue;

                //Are they within our line of sight
                bool bClearPath = true;
                bClearPath = Helpers.calcBresenhemsPredicate(_arena, _state.positionX, _state.positionY, v._state.positionX, v._state.positionY,
                     delegate(LvlInfo.Tile t)
                     {
                         return (!t.Blocked);
                     }
                 );
                if (!bClearPath)
                    continue;

                //Check to see if it is a bot and if we attack those
                if (v._bBotVehicle && targetBot)
                    return v;

                //Check to see if it is a computer vehicle and if we attack those
                if (targetVehicles)
                    return v;
            }
            //There are no vehicles in our defense radius
            return null;
        }
        /// <summary>
        /// Obtains a suitable target player
        /// </summary>
        protected Player getTargetPlayer()
        {
            Player target = null;
            //Make a list of players around us within a radius
            List<Player> inTrackingRange =
                _arena.getPlayersInRange(_state.positionX, _state.positionY, _defenseRadius);

            //Check if anyone was found
            if (inTrackingRange.Count == 0)
                return null;

            //Sort by distance to bot
            inTrackingRange.Sort(
                delegate(Player p, Player q)
                {
                    return Comparer<double>.Default.Compare(
                        Helpers.distanceSquaredTo(_state, p._state), Helpers.distanceSquaredTo(_state, q._state));
                }
            );

            //Go through all the players and find the closest one that is not on our team and is not dead            
            foreach (Player p in inTrackingRange)
            {
                //See if they are dead
                if (p.IsDead)
                    continue;
                //Check if cloaked

                //Find a clear path
                bool bClearPath = Helpers.calcBresenhemsPredicate(_arena, _state.positionX, _state.positionY, p._state.positionX, p._state.positionY,
                     delegate(LvlInfo.Tile t)
                     {
                         return !t.Blocked;
                     }
                 );
                //They are not in our sight, ignore them
                if (!bClearPath)
                    continue;

                if (Helpers.distanceTo(_home._state, p._state) > _attackRadius)
                    continue;

                return p;
            }
            return null;
        }

        #region Steer Delegates
        /// <summary>
        /// Steers the zombie along the defined path
        /// </summary>
        public Vector3 steerAlongPath(InfantryVehicle vehicle)
        {	//Are we at the end of the path?
            if (_pathTarget >= _path.Count)
            {	//Invalidate the path
                _path = null;
                _tickLastPath = 0;
                return Vector3.Zero;
            }

            //Find the nearest path point
            Vector3 point = _path[_pathTarget];

            //Are we close enough to go to the next?
            if (_pathTarget < _path.Count && vehicle.Position.Distance(point) < 0.8f)
                point = _path[_pathTarget++];

            return vehicle.SteerForSeek(point);
        }

        /// <summary>
        /// Keeps the bot around his spawning point
        /// </summary>
        public Vector3 steerForFollowOwner(InfantryVehicle vehicle)
        {
            List<Vehicle> bots = _arena.getVehiclesInRange(vehicle.state.positionX, vehicle.state.positionY, _type.Id,
                                                                delegate(Vehicle v)
                                                                { return (v is Bot); });
            IEnumerable<IVehicle> gbots = bots.ConvertAll<IVehicle>(
                delegate(Vehicle v)
                {
                    return (v as Bot).Abstract;
                }
            );

            Vector3 seperationSteer = vehicle.SteerForSeparation(50f, -0.707f, gbots);
            //Vector3 seperationSteer = vehicle.SteerForAlignment(100f, -0.707f, gbots);
            // Vector3 wanderSteer = vehicle.SteerForWander(0.5f);
            Vector3 pursuitSteer = vehicle.SteerForPursuit(_home.Abstract, 0.2f);
            return (seperationSteer * 0.6f) + pursuitSteer;
        }
        /// <summary>
        /// Steers bot towards target vehicle
        /// </summary>
        public Vector3 steerForPersueVehicle(InfantryVehicle vehicle)
        {
            List<Vehicle> bots = _arena.getVehiclesInRange(vehicle.state.positionX, vehicle.state.positionY, _type.Id,
                                                                delegate(Vehicle v)
                                                                { return (v is Bot); });
            IEnumerable<IVehicle> gbots = bots.ConvertAll<IVehicle>(
                delegate(Vehicle v)
                {
                    return (v as Bot).Abstract;
                }
            );

            Vector3 seperationSteer = vehicle.SteerForSeparation(50f, -0.707f, gbots);
            //Vector3 seperationSteer = vehicle.SteerForAlignment(100f, -0.707f, gbots);
            // Vector3 wanderSteer = vehicle.SteerForWander(0.5f);
            Vector3 pursuitSteer = vehicle.SteerForPursuit(_targetVehicle.Abstract, 0.2f);
            return (seperationSteer * 0.6f) + pursuitSteer;
        }
        /// <summary>
        /// Steers bot towards waypoint
        /// </summary>
        public Vector3 steerForPersueWaypoint(InfantryVehicle vehicle)
        {
            List<Vehicle> bots = _arena.getVehiclesInRange(vehicle.state.positionX, vehicle.state.positionY, _type.Id,
                                                                delegate(Vehicle v)
                                                                { return (v is Bot); });
            IEnumerable<IVehicle> gbots = bots.ConvertAll<IVehicle>(
                delegate(Vehicle v)
                {
                    return (v as Bot).Abstract;
                }
            );

            Vector3 seperationSteer = vehicle.SteerForSeparation(50f, -0.707f, gbots);
            //Vector3 seperationSteer = vehicle.SteerForAlignment(100f, -0.707f, gbots);
            // Vector3 wanderSteer = vehicle.SteerForWander(0.5f);
            Vector3 pursuitSteer = vehicle.SteerForPursuit(getWaypoint(402).Abstract, 0.2f);
            return (seperationSteer * 0.6f) + pursuitSteer;
        }
        #endregion

        /// <summary>
        /// Moves the bot on a persuit course towards the player, while keeping seperated from other bots
        /// </summary>
        public Vector3 steerForPersuePlayer(InfantryVehicle vehicle)
        {

            List<Vehicle> bots = _arena.getVehiclesInRange(vehicle.state.positionX, vehicle.state.positionY, _type.Id,
                                                                delegate(Vehicle v)
                                                                { return (v is Bot); });
            IEnumerable<IVehicle> gbots = bots.ConvertAll<IVehicle>(
                delegate(Vehicle v)
                {
                    return (v as Bot).Abstract;
                }
            );

            Vector3 seperationSteer = vehicle.SteerForSeparation(_seperation, -0.707f, gbots);
            Vector3 pursuitSteer = vehicle.SteerForPursuit(_targetPlayer._baseVehicle.Abstract, 0.2f);

            return (seperationSteer * 0.6f) + pursuitSteer;
        }
    }
}