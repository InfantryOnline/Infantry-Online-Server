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

namespace InfServer.Script.GameType_CTFHQ
{
    //Captain bot for perimeter defense bots
    //Will spawn other bots to defend a perimeter and defend his team's HQ
    class BasicDefense : Bot
    {
        ///////////////////////////////////////////////////
        // Member variables
        ///////////////////////////////////////////////////
        public Player owner;					//The headquarters we defend
        private Player targetEnemy;             //The enemy that is by our HQ
        private Vehicle vHq;                    //Our HQ

        protected SteeringController steering;	//System for controlling the bot's steering
        protected Script_CTFHQ ctfhq;			//The CTFHQ script
        private Random _rand = new Random(System.Environment.TickCount);
        protected List<Vector3> _path;			//The path to our destination
        protected int _pathTarget;				//The next target node of the path
        protected int _tickLastPath;			//The time at which we last made a path to the player

        private WeaponController _weaponClose;  //Our weapon for close range
        private WeaponController _weaponFar;    //Our weapon for anything that is not close range

        protected int radius = 1000;

        private float _seperation;

        ///////////////////////////////////////////////////
        // Member Functions
        ///////////////////////////////////////////////////

        /// <summary>
        /// Generic constructor
        /// </summary>
        public BasicDefense(VehInfo.Car type, Helpers.ObjectState state, Arena arena, Script_CTFHQ _ctfhq, Player _owner)
            : base(type, state, arena,
                    new SteeringController(type, state, arena))
        {
            Random rnd = new Random();

            //Handle movements
            _seperation = (float)rnd.NextDouble();
            steering = _movement as SteeringController;

            //Our weapon to use when we are close to the enemy
            _weaponClose = new WeaponController(_state, new WeaponController.WeaponSettings());
            _weaponFar = new WeaponController(_state, new WeaponController.WeaponSettings());

            //Equip our normal weapon
            if (type.InventoryItems[0] != 0)
                _weaponFar.equip(AssetManager.Manager.getItemByID(type.InventoryItems[0]));

            //Setup our second weapon
            if (type.InventoryItems[1] != 0)
                _weaponClose.equip(AssetManager.Manager.getItemByID(type.InventoryItems[1]));



            ctfhq = _ctfhq;
            owner = _owner;
        }
        /// <summary>
        /// Looks after the bot's functionality
        /// </summary>
        public override bool poll()
        {
            short x = 0, y = 0;

            if (IsDead)
            {//Dead
                steering.steerDelegate = null; //Stop movements
                ctfhq.botCount[_team]--; //Signal to our captain we died
                if (ctfhq.botCount[_team] < 0)
                    ctfhq.botCount[_team] = 0;
                bCondemned = true; //Make sure the bot gets removed in polling
                return base.poll();
            }

            //Find out if our owner is gone
            if (owner == null && !_team._name.Contains("Bot Team"))
            {//Find a new owner if not a bot team
                if (_team.ActivePlayerCount >= 0)
                    owner = _team.ActivePlayers.Last();
                else
                {
                    kill(null);
                    ctfhq.botCount[_team] = 0; //Signal to our captain we died
                    bCondemned = true; //Make sure the bot gets removed in polling
                    return base.poll();
                }
            }

            //Find out if our captain died
            if (!ctfhq.captainBots.ContainsKey(_team))
            {
                kill(null);
                ctfhq.botCount[_team] = 0; //Signal to our captain we died
                bCondemned = true; //Make sure the bot gets removed in polling
                return base.poll();
            }

            //Do we have an HQ to defend?
            if (ctfhq._hqs[_team] == null)
            {
                kill(null);
                bCondemned = true;
                ctfhq.captainBots.Remove(_team);
                return false;
            }

            int now = Environment.TickCount;

            //Get a list of all the HQs in the arena
            IEnumerable<Vehicle> hqs = _arena.Vehicles.Where(v => v._type.Id == ctfhq._hqVehId);

            //Find our HQ
            foreach (Vehicle v in hqs)
            {
                if (v._team == _team)
                {//We found it
                    x = v._state.positionX;
                    y = v._state.positionY;
                    vHq = v;
                }
            }

            //Check if an HQ was found, if not return to polling
            if (vHq == null)
                return base.poll();

            //Find out of we are suppose to be attacking anyone
            if (targetEnemy == null || !isValidTarget(targetEnemy))
                targetEnemy = getTargetPlayer();

            //Find out how far we are from base
            double distance = Math.Pow((Math.Pow(_state.positionX - vHq._state.positionX, 2) + Math.Pow(_state.positionY - vHq._state.positionY, 2)) / 2, 0.5);

            //Make sure we are not too far away from base
            if (distance > radius)
                targetEnemy = null; //Too far, go back a little bit

            if (targetEnemy != null)
            {//Go and attack them
                bool bClearPath = Helpers.calcBresenhemsPredicate(_arena, _state.positionX, _state.positionY, targetEnemy._state.positionX, targetEnemy._state.positionY,
                     delegate(LvlInfo.Tile t)
                     {
                         return !t.Blocked;
                     }
                 );

                distance = Math.Pow((Math.Pow(_state.positionX - targetEnemy._state.positionX, 2) + Math.Pow(_state.positionY - targetEnemy._state.positionY, 2)) / 2, 0.5);

                //Check how far they are to decide what weapon to use
                if (distance < 50)
                    _weapon = _weaponClose;
                else
                    _weapon = _weaponFar;

                distance = Math.Pow((Math.Pow(_state.positionX - vHq._state.positionX, 2) + Math.Pow(_state.positionY - vHq._state.positionY, 2)) / 2, 0.5);

                if (bClearPath)
                {	//Persue directly!
                    steering.steerDelegate = steerForPersuePlayer;
                    //Can we shoot?
                    if (_weapon.ableToFire())
                    {
                        int aimResult = _weapon.getAimAngle(targetEnemy._state);

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
                    if (now - _tickLastPath > Script_CTFHQ.c_defensePathUpdateInterval)
                    {
                        //Update it!
                        _tickLastPath = int.MaxValue;

                        _arena._pathfinder.queueRequest(
                            (short)(_state.positionX / 16), (short)(_state.positionY / 16),
                            (short)(targetEnemy._state.positionX / 16), (short)(targetEnemy._state.positionY / 16),
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

                //Navigate to him
                if (_path == null)
                    //If we can't find out way to him, just mindlessly walk in his direction for now
                    steering.steerDelegate = steerForPersuePlayer;
                else
                    steering.steerDelegate = steerAlongPath;

            }
            else
            {//No enemy, defend and patrol or go back to base
                //Are we at our base?
                if (distance <= 150)
                {
                    if (now - _tickLastPath > Script_CTFHQ.c_CaptainPathUpdateInterval)
                    {	//Update it!
                        _tickLastPath = int.MaxValue;

                        _arena._pathfinder.queueRequest(
                            (short)(_state.positionX / 16), (short)(_state.positionY / 16),
                            (short)((_state.positionX + _rand.Next(-150, 150)) / 16), (short)((_state.positionY + _rand.Next(-150, 150)) / 16),
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

                    //Navigate to base
                    if (_path == null)
                        //If we can't find our way to base, just mindlessly walk in its direction for now
                        steering.steerDelegate = steerForFollowOwner;
                    else
                        steering.steerDelegate = steerAlongPath;
                }
                else
                {//Go back to base
                    //Check again for enemies?
                    //?
                    //Find a clear path back
                    bool bClearPath = Helpers.calcBresenhemsPredicate(
                        _arena, _state.positionX, _state.positionY, x, y,
                        delegate(LvlInfo.Tile t)
                        {
                            return !t.Blocked;
                        }
                    );
                    if (bClearPath)
                    //Persue directly!
                        steering.steerDelegate = steerForFollowOwner;
                    
                    else
                    {//Find a new path home
                        //Does our path need to be updated?
                        if (now - _tickLastPath > Script_CTFHQ.c_CaptainPathUpdateInterval)
                        {	//Update it!
                            _tickLastPath = int.MaxValue;

                            _arena._pathfinder.queueRequest(
                                (short)(_state.positionX / 16), (short)(_state.positionY / 16),
                                (short)(x / 16), (short)(y / 16),
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

                        //Navigate to base
                        if (_path == null)
                            //If we can't find our way to base, just mindlessly walk in its direction for now
                            steering.steerDelegate = steerForFollowOwner;
                        else
                            steering.steerDelegate = steerAlongPath;

                    }
                }
            }

            //Handle normal functionality
            return base.poll();
        }

        /// <summary>
        /// Obtains a suitable target player
        /// </summary>
        protected bool isValidTarget(Player enemy)
        {	//Don't shoot a dead player
            if (enemy.IsDead)
                return false;

            //Is it too far away?
            if (Helpers.distanceTo(this, enemy) > 800)
                return false;

            //We must have vision on it
            bool bVision = Helpers.calcBresenhemsPredicate(_arena,
                _state.positionX, _state.positionY, enemy._state.positionX, enemy._state.positionY,
                delegate(LvlInfo.Tile t)
                {
                    return !t.Blocked;
                }
            );

            return bVision;
        }
        /// <summary>
        /// Obtains a suitable target player
        /// </summary>
        protected Player getTargetPlayer()
        {
            Player target = null;
            //Make a list of players around us within a radius
            List<Player> inTrackingRange =
                _arena.getPlayersInRange(_state.positionX, _state.positionY, radius);

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

                double distance = Math.Pow((Math.Pow(_state.positionX - vHq._state.positionX, 2) + Math.Pow(_state.positionY - vHq._state.positionY, 2)) / 2, 0.5);
                
                //Make sure we are not too far from our base
                if (distance > 1000)
                    return null;

                distance = Math.Pow((Math.Pow(_state.positionX - p._state.positionX, 2) + Math.Pow(_state.positionY - p._state.positionY, 2)) / 2, 0.5);

                //Find a clear path
                double dist = Helpers.distanceSquaredTo(_state, p._state);
               /* bool bClearPath = Helpers.calcBresenhemsPredicate(_arena, _state.positionX, _state.positionY, p._state.positionX, p._state.positionY,
                    delegate(LvlInfo.Tile t)
                    {
                        return !t.Blocked;
                    }
                );
                
                //They are not in our sight, ignore them
                if (!bClearPath)
                    continue;    */
      
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

            Vector3 wanderSteer = vehicle.SteerForWander(0.5f);
            Vector3 pursuitSteer = vehicle.SteerForPursuit(vHq.Abstract, 0.2f);

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
            Vector3 pursuitSteer = vehicle.SteerForPursuit(targetEnemy._baseVehicle.Abstract, 0.2f);

            return (seperationSteer * 0.6f) + pursuitSteer;
        }
    }
}
