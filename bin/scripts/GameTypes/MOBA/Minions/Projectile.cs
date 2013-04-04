using System;
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

namespace InfServer.Script.GameType_MOBA
{
    //Projectile bot -- a projectile that will follow an enemy and explode on them
    //Allows for variable damage
    class Projectile : Bot
    {
        ///////////////////////////////////////////////////
        // Member variables
        ///////////////////////////////////////////////////

        protected SteeringController steering;	//System for controlling the bot's steering
        protected Script_MOBA _moba;			    //The MOBA script
        protected List<Vector3> _path;			//The path to our destination
        protected int _pathTarget;				//The next target node of the path
        protected int _tickLastPath;			//The time at which we last made a path to follow
        protected int _pathUpdateInterval = 1000;      //The tick at which we update our path
        protected Player _targetPlayer;         //Our target player or turret
        protected Vehicle _targetVehicle;       //Our target tower/inhibitor/HQ/bot
        protected int _lane;                    //The lane the minion is supposed to focus on
        private int _maxLifetime = 500000;      //How long we stay alive for
        private int _lifeTime;


        private float _seperation;

        ///////////////////////////////////////////////////
        // Member Functions
        ///////////////////////////////////////////////////

        /// <summary>
        /// Generic constructor
        /// Type = Vehicle bot uses
        /// State = direction and location of bot for spawn
        /// Arena = the arena the bot is in
        /// Team = the team the bot is on
        /// Lane = the lane the bot is assigned to [0=top,1=mid,2=bottom]
        /// </summary>
        public Projectile(VehInfo.Car type, Helpers.ObjectState state, Arena arena, Script_MOBA moba, Team team)
            //: base(
            : base(type, state, arena,
                    new SteeringController(type, state, arena))
        {
            Random rnd = new Random(Environment.TickCount);
            _team = team;
            _moba = moba;

            _seperation = (float)rnd.NextDouble();
            steering = _movement as SteeringController;

            if (type.InventoryItems[0] != 0)
                _weapon.equip(AssetManager.Manager.getItemByID(type.InventoryItems[0]));

            _lifeTime = Environment.TickCount;

        }

        /// <summary>
        /// Looks after the bot's functionality
        /// </summary>
        public override bool poll()
        {           

            if (IsDead)
            {//Dead
                steering.steerDelegate = null; //Stop movements                
                bCondemned = true; //Make sure the bot gets removed in polling
                return base.poll();
            }

            int now = Environment.TickCount;

            //Check for expiring projectile
            if (now - _lifeTime > _maxLifetime)
                kill(null);

            //Find the nearest tower/inhibitor/HQ/bot in our lane
            _targetVehicle = getTargetVehhicle();
            //Find the nearest player
            _targetPlayer = getTargetPlayer();

            if (_targetPlayer == null)
            {
                //Find a clear path

                bool bClearPath = Helpers.calcBresenhemsPredicate(_arena, _state.positionX, _state.positionY, (short)(_targetVehicle._state.positionX + 150), (short)(_targetVehicle._state.positionY + 150),
                    delegate(LvlInfo.Tile t)
                    {
                        return (!t.Blocked);
                    }
                );



                if (bClearPath && _targetVehicle._type.Id != 999)
                {	//Persue directly!
                    steering.steerDelegate = steerForFollowOwner;
                    //Are we close enough?
                    if (Helpers.distanceTo(_state, _targetVehicle._state) < 55)
                    {
                        //Blow up!
                        kill(null);
                        //Simulate the damage 
                        _targetVehicle.applyExplosionDamage(true, null, _targetVehicle._state.positionX, _targetVehicle._state.positionY, _weapon.Projectile);

                    }
                }
                else
                {
                    //Does our path need to be updated?
                    if (now - _tickLastPath > _pathUpdateInterval)
                    {
                        //Update it!
                        _tickLastPath = int.MaxValue;
                        Random rnd = new Random(Environment.TickCount);
                        _arena._pathfinder.queueRequest(
                            (short)(_state.positionX / 16), (short)(_state.positionY / 16),
                            (short)((_targetVehicle._state.positionX + (rnd.NextDouble() * (2000) - 1000)) / 16), (short)((_targetVehicle._state.positionY) / 16),
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
            else
            {//The player is closer
                //Find a clear path
                bool bClearPath = Helpers.calcBresenhemsPredicate(_arena, _state.positionX, _state.positionY, _targetPlayer._state.positionX, _targetPlayer._state.positionY,
                     delegate(LvlInfo.Tile t)
                     {
                         return !t.Blocked;
                     }
                 );
                if (bClearPath)
                {	//Persue directly!
                    steering.steerDelegate = steerForPersuePlayer;
                    //Can we shoot?
                    if (_weapon.ableToFire())
                    {
                        int aimResult = _weapon.getAimAngle(_targetPlayer._state);

                        if (_weapon.isAimed(aimResult) && Helpers.distanceTo(_targetPlayer._state, _state) < 200)
                        {	//Spot on! Fire?
                            //Are we close enough?
                            if (Helpers.distanceTo(_state, _targetPlayer._state) < 35)
                            {   //Blow up!
                                kill(null);
                                //Simulate the damage 
                                _targetVehicle.applyExplosionDamage(true, _targetPlayer, _targetPlayer._state.positionX, _targetPlayer._state.positionY, _weapon.Projectile);

                            }
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
            //Handle normal functionality
            return base.poll();
        }

        /// <summary>
        /// Obtains a suitable target vehicle
        /// </summary>
        protected Vehicle getTargetVehhicle()
        {
            //Make a list of all the vehicles around us
            List<Vehicle> inTrackingRange = _arena.getVehiclesInRange(_state.positionX, _state.positionY, 10000000);

            //Check if any were found
            if (inTrackingRange.Count == 0)
                return null;

            //Sort list by distance from minion
            inTrackingRange.Sort(
                delegate(Vehicle p, Vehicle q)
                {
                    return Comparer<double>.Default.Compare(
                        Helpers.distanceSquaredTo(_state, p._state), Helpers.distanceSquaredTo(_state, q._state));
                }
            );

            //Go through the list and find the nearest vehicle
            //From highest to lowest priority [based on distances from starting location this should work automatically]
            //Bot --> Tower --> Inhibitor --> HQ
            foreach (Vehicle v in inTrackingRange)
            {

                //Make sure the vehicle is not on our team and ----not a spectator----
                if (v._team == _team)
                    continue;

                //Check if it is a tower
                if (v._type.Id == 400)
                {//Make sure they stay in their lane at all times
                    foreach (KeyValuePair<int, Script_MOBA.towerObject> obj in _moba._towers)
                        if (v._state.positionX == obj.Value.getX() && v._state.positionY == obj.Value.getY())
                            return v;
                        else
                            continue;
                }
                //Check if it is an enemy minion and if they are close enough for us to attack
                if (v._type.Id == 315 || v._type.Id == 324 || v._type.Id == 325 || v._type.Id == 314 || v._type.Id == 313 || v._type.Id == 317)
                    if (Helpers.distanceTo(_state, v._state) < 300)
                    {
                        return v;
                    }
                //Check to see if it is an inhibitor
                if (v._type.Id == 480)
                {//Make sure they stay in their lane at all times
                    foreach (KeyValuePair<int, Script_MOBA.inhibObject> obj in _moba._inhibitors)
                        if (v._state.positionX == obj.Value.getX() && v._state.positionY == obj.Value.getY())
                            return v;
                        else
                            continue;
                }
                //Check to see if it the HQ
                //  else if (v._type.Id == _moba._hqVehId)                
                //      return v;    
            }
            //There was nothing in which case the game should have ended by now
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
                _arena.getPlayersInRange(_state.positionX, _state.positionY, 300);

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

                //See if they are on our team
                if (p._team == _team)
                    continue;

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

                target = p;
            }
            return target;
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
        /// Keeps the combat bot around the engineer
        /// Change to keeping him around the HQ
        /// </summary>
        public Vector3 steerForFollowOwner(InfantryVehicle vehicle)
        {

            Vector3 wanderSteer = vehicle.SteerForWander(0);
            Vector3 pursuitSteer = vehicle.SteerForPursuit(_targetVehicle.Abstract, 0.2f);
            return (wanderSteer * 1.6f) + pursuitSteer;
        }
        #endregion

        /// <summary>
        /// Moves the bot on a persuit course towards the player, while keeping seperated from other bots
        /// </summary>
        public Vector3 steerForPersuePlayer(InfantryVehicle vehicle)
        {

            List<Vehicle> bots = _arena.getVehiclesInRange(vehicle.state.positionX, vehicle.state.positionY, 400,
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

            return pursuitSteer;
        }
    }
}