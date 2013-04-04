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
    //Jungle Bot -- Will sit in a spot in the jungle and attack anyone that attacks them
    //Will drop special power ups and/or weapons
    class JungleBot : Bot
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
        protected Vehicle _home;

        private float _seperation;

        ///////////////////////////////////////////////////
        // Member Functions
        ///////////////////////////////////////////////////

        /// <summary>
        /// Generic constructor
        /// </summary>
        public JungleBot(VehInfo.Car type, Helpers.ObjectState state, Arena arena, Script_MOBA _ctfhq, Vehicle home)
            : base(type, state, arena,
                    new SteeringController(type, state, arena))
        {
            Random rnd = new Random();

            _home = home;

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

                //Drop our weapon?
                //Give them a buff?

                return base.poll();
            }

            //Check if we got damaged and if we are close enough to home to actually go attack
            if (_state.health < _type.Hitpoints && Helpers.distanceTo(_state, _home._state) < 150)
            {
                //Find the nearest player [hopefully the dude that attacked us]
                _targetPlayer = getTargetPlayer();
            }

            //Check if we need to go back to our starting position
            if (Helpers.distanceTo(_state, _home._state) > 250)
            {//Go back to our starting position
                _targetPlayer = null;
                //Find a clear path                
                bool bClearPath = Helpers.calcBresenhemsPredicate(_arena, _state.positionX, _state.positionY, (short)(_home._state.positionX), (short)(_home._state.positionY),
                    delegate(LvlInfo.Tile t)
                    {
                        return (!t.Blocked);
                    }
                );

                if (bClearPath)
                    steering.steerDelegate = steerForFollowOwner;
            }


            //Check if we need to heal back up by checking if we are close enough to home
            if (Helpers.distanceTo(_home._state, _state) < 150 && _targetPlayer == null)
            {//Heal up
                _state.health = (short)_type.Hitpoints;
                _targetPlayer = null;
                //Stand still again
                freezeMovement(1000);
            }

            int now = Environment.TickCount;

            if (_targetPlayer != null)
            {
                //Find a clear path                
                bool bClearPath = Helpers.calcBresenhemsPredicate(_arena, _state.positionX, _state.positionY, (short)(_targetPlayer._state.positionX + 150), (short)(_targetPlayer._state.positionY + 150),
                    delegate(LvlInfo.Tile t)
                    {
                        return (!t.Blocked);
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
            //Handle normal functionality
            return base.poll();
        }

        /// <summary>
        /// Obtains a suitable target player
        /// </summary>
        protected Player getTargetPlayer()
        {
            Player target = null;
            //Make a list of players around us within a radius
            List<Player> inTrackingRange =
                _arena.getPlayersInRange(_state.positionX, _state.positionY, 500);

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

        #endregion

        /// <summary>
        /// Keeps the combat bot around the engineer
        /// Change to keeping him around the HQ
        /// </summary>
        public Vector3 steerForFollowOwner(InfantryVehicle vehicle)
        {

            // Vector3 wanderSteer = vehicle.SteerForWander(0.5f);
            Vector3 pursuitSteer = vehicle.SteerForPursuit(_home.Abstract, 0.2f);
            //return (wanderSteer * 1.6f) + pursuitSteer;
            return pursuitSteer;
        }

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