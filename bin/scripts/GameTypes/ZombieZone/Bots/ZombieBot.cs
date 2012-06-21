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

namespace InfServer.Script.GameType_ZombieZone
{
    // ZombieBot Class
    /// A simple zombie-type bot
    ///////////////////////////////////////////////////////
    public class ZombieBot : Bot
    {	// Member variables
        ///////////////////////////////////////////////////
        public Team targetTeam;					//The team of which players we're targetting

        protected bool bOverriddenPoll;			//Do we have custom actions for poll?

        protected Player victim;				//The player we're currently stalking
        protected SteeringController steering;	//System for controlling the bot's steering
        protected Script_ZombieZone zz;			//The zombiezone script

        protected List<Vector3> _path;			//The path to our destination
        protected int _pathTarget;				//The next target node of the path
        protected int _tickLastPath;			//The time at which we last made a path to the player

        private float _seperation;
        private Script_ZombieZone.ZombieDistraction distraction;


        ///////////////////////////////////////////////////
        // Member Functions
        ///////////////////////////////////////////////////
        /// <summary>
        /// Generic constructor
        /// </summary>
        public ZombieBot(VehInfo.Car type, Helpers.ObjectState state, Arena arena, Script_ZombieZone _zz)
            : base(type, state, arena,
                    new SteeringController(type, state, arena))
        {
            Random rnd = new Random();

            _seperation = (float)rnd.NextDouble();
            steering = _movement as SteeringController;

            if (type.InventoryItems[0] != 0)
                _weapon.equip(AssetManager.Manager.getItemByID(type.InventoryItems[0]));

            zz = _zz;
        }

        /// <summary>
        /// Looks after the bot's functionality
        /// </summary>
        public override bool poll()
        {	//Overridden?
            if (bOverriddenPoll)
                return base.poll();

            //Dead? Do nothing
            if (IsDead)
            {
                steering.steerDelegate = null;
                return base.poll();
            }

            int now = Environment.TickCount;

            if (checkCircumstances())
                return base.poll();

            //Get the closest player
            bool bClearPath = false;
            victim = getTargetPlayer(ref bClearPath);

            if (victim != null)
            {
                if (bClearPath)
                {	//Persue directly!
                    steering.steerDelegate = steerForPersuePlayer;

                    //Can we shoot?
                    if (_weapon.ableToFire())
                    {
                        int aimResult = _weapon.getAimAngle(victim._state);

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
                {	//Does our path need to be updated?
                    if (now - _tickLastPath > Script_ZombieZone.c_zombiePathUpdateInterval)
                    {	//Are we close enough to the team?
                        if (!checkTeamDistance())
                        {
                            _path = null;
                            destroy(true);
                        }
                        else
                        {	//Update it!
                            _tickLastPath = int.MaxValue;

                            _arena._pathfinder.queueRequest(
                                (short)(_state.positionX / 16), (short)(_state.positionY / 16),
                                (short)(victim._state.positionX / 16), (short)(victim._state.positionY / 16),
                                delegate(List<Vector3> path, int pathLength)
                                {
                                    if (path != null)
                                    {	//Is the path too long?
                                        if (pathLength > Script_ZombieZone.c_zombieMaxPath)
                                        {	//Destroy ourself and let another zombie take our place
                                            _path = null;
                                            destroy(true);
                                        }
                                        else
                                        {
                                            _path = path;
                                            _pathTarget = 1;
                                        }
                                    }

                                    _tickLastPath = now;
                                }
                            );
                        }
                    }

                    //Navigate to him
                    if (_path == null)
                        //If we can't find out way to him, just mindlessly walk in his direction for now
                        steering.steerDelegate = steerForPersuePlayer;
                    else
                        steering.steerDelegate = steerAlongPath;
                }
            }

            //Handle normal functionality
            return base.poll();
        }

        /// <summary>
        /// Obtains a suitable target player
        /// </summary>
        protected Player getTargetPlayer(ref bool bInSight)
        {	//Look at the players on the target team
            if (targetTeam == null)
                return null;

            Player target = null;
            double lastDist = double.MaxValue;
            bInSight = false;

            foreach (Player p in targetTeam.ActivePlayers.ToList())
            {	//Find the closest player
               if (p.IsDead)
                   continue;

                double dist = Helpers.distanceSquaredTo(_state, p._state);
                bool bClearPath = Helpers.calcBresenhemsPredicate(_arena,  _state.positionX, _state.positionY, p._state.positionX, p._state.positionY,
                    delegate(LvlInfo.Tile t)
                     {
                       return !t.Blocked;
                     }
                );
  
               if ((!bInSight || (bInSight && bClearPath)) && lastDist > dist)
               {
                   bInSight = bClearPath;
                   lastDist = dist;
                   target = p;
               }
           }
            

            return target;
        }

        /// <summary>
        /// Checks to see if we're too far from the team
        /// </summary>
        protected bool checkTeamDistance()
        {
            //sanity check
            if (targetTeam.ActivePlayerCount == 0)
                return true;

            int minDist = int.MaxValue;

            //finds minimum distance from any one of the activeplayers
            foreach (Player player in targetTeam.ActivePlayers)
            {
                //uses the max{dx,dy} metric for distance estimate
                int dist = Math.Max(Math.Abs(_state.positionX - player._state.positionX), Math.Abs(_state.positionY - player._state.positionY));

                if (dist < minDist)
                    minDist = dist;
            }

            return minDist < Script_ZombieZone.c_zombieMaxRespawnDist + Script_ZombieZone.c_zombieDistanceLeeway;
        }

        /// <summary>
        /// Checks for any special circumstances we should be handling
        /// </summary>
        protected bool checkCircumstances()
        {	//Check for extraordinary circumstances
            Script_ZombieZone.TeamState state = zz.getTeamState(targetTeam);
            if (state != null)
            {
                if (checkForDistractions(state))
                    return true;

                if (checkForCloak(state))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks for any distractions we should approach
        /// </summary>
        protected virtual bool checkForCloak(Script_ZombieZone.TeamState state)
        {	//Is the team cloaked?
            if (!state.bCloaked)
                return false;

            //Just wander about!
            steering.steerDelegate = delegate(InfantryVehicle vehicle)
            {
                return vehicle.SteerForWander(0.5f);
            };

            return true;
        }

        /// <summary>
        /// Checks for any distractions we should approach
        /// </summary>
        protected virtual bool checkForDistractions(Script_ZombieZone.TeamState state)
        {	//Do we already have a distraction?
            if (distraction != null)
            {
                if (distraction.bActive)
                {
                    if (!headToDistraction(distraction))
                    {
                        distraction = null;
                        return false;
                    }
                    else
                        return true;
                }
                else
                    distraction = null;
            }

            //Are there any distractions we can see?
            foreach (Script_ZombieZone.ZombieDistraction distract in state.distractions)
            {	//At it's limit?
                if (distract.distractLimit <= 0)
                    continue;

                //If it works, use it
                if (headToDistraction(distract))
                {
                    distraction = distract;
                    distract.distractLimit--;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Heads towards the current distraction
        /// </summary>
        private bool headToDistraction(Script_ZombieZone.ZombieDistraction distract)
        {
            bool bClearPath = Helpers.calcBresenhemsPredicate(_arena,
                    _state.positionX, _state.positionY, distract.x, distract.y,
                    delegate(LvlInfo.Tile t)
                    {
                        return !t.Blocked;
                    }
                );
            if (!bClearPath)
                return false;

            //It's clear, let's steer towards it
            Vector3 distractpos = new Vector3(((float)distract.x) / 100.0, ((float)distract.y) / 100.0, 0);
            steering.steerDelegate = delegate(InfantryVehicle vehicle)
            {
                Vector3 wander = vehicle.SteerForWander(10.5f);
                Vector3 seek = vehicle.SteerForSeek(distractpos);
                return wander + (seek * 10.8f);
            };

            if (distract.bStillHostile && _weapon.bEquipped && victim != null)
            {	//Can we shoot?
                if (_weapon.ableToFire())
                {
                    int aimResult = _weapon.getAimAngle(victim._state);

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

            return true;
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
        /// Moves the zombie on a persuit course towards the player, while keeping seperated from other zombies
        /// </summary>
        public Vector3 steerForPersuePlayer(InfantryVehicle vehicle)
        {
            if (victim == null)
                return Vector3.Zero;

            List<Vehicle> zombies = _arena.getVehiclesInRange(vehicle.state.positionX, vehicle.state.positionY, 400,
                                                                delegate(Vehicle v)
                                                                { return (v is ZombieBot); });
            IEnumerable<IVehicle> zombiebots = zombies.ConvertAll<IVehicle>(
                delegate(Vehicle v)
                {
                    return (v as ZombieBot).Abstract;
                }
            );

            Vector3 seperationSteer = vehicle.SteerForSeparation(_seperation, -0.707f, zombiebots);
            Vector3 pursuitSteer = vehicle.SteerForPursuit(victim._baseVehicle.Abstract, 0.2f);

            return (seperationSteer * 0.6f) + pursuitSteer;
        }
        #endregion
    }
}
