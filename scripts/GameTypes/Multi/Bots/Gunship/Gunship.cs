using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;


using InfServer.Game;
using InfServer.Protocol;
using InfServer.Scripting;
using InfServer.Bots;

using Assets;
using Axiom.Math;
using Bnoerj.AI.Steering;

namespace InfServer.Script.GameType_Multi
{   // Script Class
    /// Provides the interface between the script and bot
    ///////////////////////////////////////////////////////
    public partial class Gunship : Bot
    {   ///////////////////////////////////////////////////
        // Member Variables
        ///////////////////////////////////////////////////
        //private Bot _bot;							//Pointer to our bot class
        private Random _rand;

        public Helpers.ObjectState _spawnState; //Where we spawned
        private Vehicle target;            //The target we're attacking, if any
        public bool _returnToBase;                  //Have we dropped our payload?
        public bool _isOnSite;
        public BotType type;
        protected bool bOverriddenPoll;         //Do we have custom actions for poll?
        public Script_Multi _script;

        protected List<Vector3> _path;			//The path to our destination
        protected int _pathTarget;				//The next target node of the path
        protected int _tickLastPath;			//The time at which we last made a path to the player

        private Vehicle _target;
        private Vehicle _spawnMarker;

        protected SteeringController steering;	//System for controlling the bot's steering
        private float _seperation;
        private int _tickArrivedAtLocation;
        private int _tickLastShotFired;
        private int _tickLastRadarDot;

        private class Projectile
        {
            public Projectile(Vehicle vehicle, int tickCreation)
            {
                target = vehicle;
                tickShotFired = tickCreation;
            }

            public Vehicle target; //The target we were shooting at.
            public int tickShotFired; //The time at which we fired our projectile
        }
        private List<Projectile> _shotsFired;




        public Gunship(VehInfo.Car type, Helpers.ObjectState state, Arena arena)
            : base(type, state, arena,
            new SteeringController(type, state, arena))
        {
            Random rnd = new Random();
            _seperation = (float)rnd.NextDouble();
            steering = _movement as SteeringController;
            _rand = new Random();
            if (type.InventoryItems[0] != 0)
                _weapon.equip(AssetManager.Manager.getItemByID(type.InventoryItems[0]));

            _shotsFired = new List<Projectile>();
        }

        public void init(Helpers.ObjectState spawn, Script_Multi script, Vehicle target, Player owner, Settings.GameTypes gameType)
        {
            _script = script;
            spawn.positionZ = 0;
            _target = target;

            _target.destroy(true);
            _target = null;
            _spawnMarker = _arena.newVehicle(AssetManager.Manager.getVehicleByID(407), _team, null, spawn);


            if (_spawnMarker == null)
                Log.write(TLog.Warning, "Unable to spawn spawn marker");

            _tickArrivedAtLocation = Environment.TickCount;
        }

        /// <summary>
        /// Looks after the bot's functionality
        /// </summary>
        public override bool poll()
        {   //Dead? Do nothing
            if (IsDead)
            {
                steering.steerDelegate = null;
                return base.poll();
            }
            int now = Environment.TickCount;

            //Radar Dot
            if (now - _tickLastRadarDot >= 900)
            {
                _tickLastRadarDot = now;
                IEnumerable<Player> enemies = _arena.Players.Where(p => p._team != _team);
                //Helpers.Player_RouteExplosion(_team.ActivePlayers, 1131, _state.positionX, _state.positionY, 0, 0, 0);
                //Helpers.Player_RouteExplosion(enemies, 1130, _state.positionX, _state.positionY, 0, 0, 0);
            }

            //Do we have any projectiles to spoof damage for?
            if (_shotsFired.Count > 0)
            {
                Projectile proj = _shotsFired[0];

                //Ballpark figure for chaingun
                if (now - proj.tickShotFired >= 500)
                {
                    //Is it still valid?
                    if (proj.target != null && !proj.target.IsDead)
                    {
                        //Simulate the damage if we're attacking a bot
                        if (proj.target._bBotVehicle)
                            proj.target.applyExplosionDamage(true, _creator, proj.target._state.positionX, proj.target._state.positionY, _weapon.Projectile);
                    }

                    //Erase it
                    _shotsFired.Remove(proj);
                }
            }

            if (now - _tickArrivedAtLocation >= 120000 && _tickArrivedAtLocation != 0 && !_returnToBase)
            {
                _returnToBase = true;
                _team.sendArenaMessage("&Pavelow Pilot> We're bingo on fuel! Returning to base", 4);
            }

            if (_returnToBase)
            {
                target = _spawnMarker;
                bool onSite = false;

                //Get to landing zone
                if (Helpers.distanceTo(target._state, _state) > 45)
                    steering.steerDelegate = steerForSeekTarget;
                else
                {
                    steering.freezeMovement(5000);
                    onSite = true;
                }

                //Can we drop?
                if (onSite)
                {
                    base.destroy(true);
                }
                return base.poll();
            }

            if (_creator == null || _creator.IsSpectator)
                destroy(true);

            bool bAttacking = false;
            bool bBackPedal = false;
            //Do we need to seek a new target?
            if (target == null || !isValidTarget(target))
                target = getTargetBot();

            if (target != null)
            {
                bAttacking = true;
                //Too far?
                if (Helpers.distanceTo(this, target) > 420)
                {
                    steering.bSkipRotate = false;
                    steering.steerDelegate = steerForSeekTarget;
                }
                //Too short?
                else if (Helpers.distanceTo(this, target) < 370)
                {
                    bBackPedal = true;
                    steering.bSkipRotate = false;
                    steering.steerDelegate = delegate (InfantryVehicle vehicle)
                    {
                        if (target != null)
                            return vehicle.SteerForFlee(target._state.position());
                        else
                            return Vector3.Zero;
                    };
                }
                else
                {
                    steering.steerDelegate = null;
                    //Can we shoot?
                    if (_weapon.ableToFire() && !bBackPedal)
                    {
                        int aimResult = _weapon.getAimAngle(target._state);

                        if (_weapon.isAimed(aimResult))
                        {   //Spot on! Fire?
                            _itemUseID = _weapon.ItemID;
                            _weapon.shotFired();

                            //Damage spoofing
                            Projectile proj = new Projectile(target, now);
                            _shotsFired.Add(proj);
                        }

                        steering.bSkipAim = true;
                        steering.angle = aimResult;
                    }
                    else
                        steering.bSkipAim = false;
                }
                followBot(now);
            }

            //Do we have a player to follow?
            if (_creator.IsDead || _arena.getTerrain(_creator._state.positionX, _creator._state.positionY).safety)
                return base.poll();

            //Are we out of range of our player and not attacking anything?
            if (!bAttacking && Helpers.distanceTo(_creator._state, _state) > 50)
                followPlayer(now);

            //Handle normal functionality
            return base.poll();
        }

        /// <summary>
        /// Obtains a suitable target player
        /// </summary>
        protected bool isValidTarget(Vehicle target)
        {   //Don't shoot a dead zombie
            if (target.IsDead)
                return false;

            if (target._type.Id == 406 || target._type.Id == 407)
                return false;

            if (target._type.ClassId >= 1)
                return false;

            if (target._team == _team)
                return false;

            //Is it too far away?
            if (Helpers.distanceTo(this, target) >= 1000)
                return false;

            return true;
        }

        /// <summary>
        /// Obtains a suitable target player
        /// </summary>
        protected Vehicle getTargetBot()
        {   //Find the closest valid target
            Vector3 selfpos = _state.position();
            IEnumerable<Vehicle> targets = _arena.getVehiclesInRange(_state.positionX, _state.positionY, 1000).OrderBy(t => Helpers.distanceTo(this, t));

            foreach (Vehicle target in targets)
                if (isValidTarget(target))
                    return target;

            return null;
        }

        #region Steer Delegates
        /// <summary>
        /// Keeps the combat bot around the engineer
        /// Change to keeping him around the HQ
        /// </summary>
        public Vector3 steerForSeekTarget(InfantryVehicle vehicle)
        { 
            Vector3 pursuitSteer = vehicle.SteerForPursuit(target.Abstract, 0.2f);
            return pursuitSteer;
        }

        /// <summary>
        /// Steers the zombie along the defined path
        /// </summary>
        public Vector3 steerAlongPath(InfantryVehicle vehicle)
        {   //Are we at the end of the path?
            if (_pathTarget >= _path.Count)
            {   //Invalidate the path
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
        /// </summary>
        public Vector3 steerForFollowOwner(InfantryVehicle vehicle)
        {

            Vector3 wanderSteer = vehicle.SteerForWander(1.3f);
            Vector3 pursuitSteer = vehicle.SteerForPursuit(_creator._baseVehicle.Abstract, 0.2f);

            return (wanderSteer * 0.75f) + pursuitSteer;
        }
        #endregion

        public void followPlayer(int now)
        {
            //Does our path need to be updated?
            if (now - _tickLastPath > c_pathUpdateInterval)
            {   //Update it!
                _tickLastPath = int.MaxValue;

                _arena._pathfinder.queueRequest(
                    (short)(_state.positionX / 16), (short)(_state.positionY / 16),
                    (short)(_creator._state.positionX / 16), (short)(_creator._state.positionY / 16),
                    delegate (List<Vector3> path, int pathLength)
                    {
                        if (path != null)
                        {   //Is the path too long?
                          
                                _path = path;
                                _pathTarget = 1;
                        }

                        _tickLastPath = now;
                    }
                );
            }

            //Navigate to him
            if (_path == null)
                //If we can't find out way to him, just mindlessly walk in his direction for now
                steering.steerDelegate = steerForFollowOwner;
            else
                steering.steerDelegate = steerForFollowOwner;
        }

        public void followBot(int now)
        {
            //Does our path need to be updated?
            if (now - _tickLastPath > c_pathUpdateInterval)
            {   //Update it!
                _tickLastPath = int.MaxValue;

                _arena._pathfinder.queueRequest(
                    (short)(_state.positionX / 16), (short)(_state.positionY / 16),
                    (short)(target._state.positionX / 16), (short)(target._state.positionY / 16),
                    delegate (List<Vector3> path, int pathLength)
                    {
                        if (path != null)
                        {   //Is the path too long?
                            if (pathLength > c_MaxPath)
                            {   //Destroy ourself and let another zombie take our place
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
    }
}