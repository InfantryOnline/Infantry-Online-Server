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
        protected int _pathUpdateInterval = 1000;      //The tick at which we update our path
        private int _distanceFromHome = 1000; //Distance from home until we decide to ignore everything else and run back home 
        private int _distanceUntilFire = 300; //Distance until we start actually shooting our target
        private int _defenseRadius = 150; //People in this radius will be marked as hostiles and we will attack them
        protected Player _targetPlayer;         //Our target player or turret
        protected Vehicle _targetVehicle;       //Our target tower/inhibitor/HQ/bot
        protected bool _attacking;              //Are we attacking something?

        protected int _maxWaypoints = 19;

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
        public RangeMinion(VehInfo.Car type, Helpers.ObjectState state, Arena arena)
            //: base(
            : base(type, state, arena,
                    new SteeringController(type, state, arena))
        {
            Random rnd = new Random(Environment.TickCount);
            _attacking = false;

            _seperation = (float)rnd.NextDouble();
            steering = _movement as SteeringController;


            if (type.InventoryItems[0] != 0)
                _weapon.equip(AssetManager.Manager.getItemByID(type.InventoryItems[0]));

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
                //Drop an item!
                VehInfo vehicle = _arena._server._assets.getVehicleByID(_type.Id);
                ItemInfo item = _arena._server._assets.getItemByID(vehicle.DropItemId);
                if (item != null)
                    _arena.itemSpawn(item, (ushort)vehicle.DropItemQuantity, _state.positionX, _state.positionY, null);
                   
                return base.poll();
            }

            int now = Environment.TickCount;

            //Find the nearest tower/inhibitor/HQ/bot in our lane
            _targetVehicle = getTargetVehhicle();
            //Find the nearest player
            _targetPlayer = getTargetPlayer();
            if (_targetVehicle == null && _targetPlayer == null)
                return base.poll();

            if (_targetPlayer == null)
            {
                //Find a clear path
                bool bClearPath = true;
                bClearPath = Helpers.calcBresenhemsPredicate(_arena, _state.positionX, _state.positionY, (short)(_targetVehicle._state.positionX + 150), (short)(_targetVehicle._state.positionY + 150),
                     delegate(LvlInfo.Tile t)
                     {
                         return (!t.Blocked);
                     }
                 );

                if (bClearPath && Helpers.distanceTo(_targetVehicle._state, _state) > _distanceFromHome)
                {	//Persue directly!
                    steering.steerDelegate = steerForFollowOwner;
                    //Can we shoot?
                    if (_weapon.ableToFire())
                    {

                        int aimResult = _weapon.getAimAngle(_targetVehicle._state);

                        if (_weapon.isAimed(aimResult) && Helpers.distanceTo(_targetVehicle._state, _state) < _distanceUntilFire)
                        {	//Spot on! Fire?

                            //_movement.freezeMovement(500);
                            _itemUseID = _weapon.ItemID;
                            _weapon.shotFired();
                            _attacking = true;

                            //fire here
                            //stuff

                            //Change this to allow for minion vs minion kills to grant rewards
                            if (!_targetVehicle._bBotVehicle)
                            {
                                _targetVehicle.update(true);
                                if (_targetVehicle._state.health <= 0)
                                {
                                    _targetVehicle.destroy(false);                                    
                                }
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
                        _attacking = false;
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

                        if (_weapon.isAimed(aimResult) && Helpers.distanceTo(_targetPlayer._state, _state) < _distanceUntilFire)
                        {	//Spot on! Fire?
                            _itemUseID = _weapon.ItemID;
                            _weapon.shotFired();
                            //fire
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

            //Go through the list and find the nearest vehicle that is our home
            foreach (Vehicle v in inTrackingRange)
            {   
                if (v._type.Id == 400)
                    if (Helpers.distanceTo(_state, v._state) < 1000000)                     
                        return v;
                
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
            List<Vehicle> bots = _arena.getVehiclesInRange(vehicle.state.positionX, vehicle.state.positionY, 400,
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

            return (seperationSteer * 0.6f) + pursuitSteer;
        }
    }
}