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
    public partial class Dropship : Bot
    {   ///////////////////////////////////////////////////
        // Member Variables
        ///////////////////////////////////////////////////
        //private Bot _bot;							//Pointer to our bot class
        private Random _rand;


        public Vehicle _targetLocation;
        public Helpers.ObjectState _spawnState; //Where we spawned
        private Vehicle targetEnemy;            //The target we're attacking, if any
        public bool _bDropped;                  //Have we dropped our payload?
        public bool _dropItemFired;
        public BotType type;
        protected bool bOverriddenPoll;         //Do we have custom actions for poll?
        public Script_Multi _script;

        protected List<Vector3> _path;			//The path to our destination
        protected int _pathTarget;				//The next target node of the path
        protected int _tickLastPath;			//The time at which we last made a path to the player

        private Vehicle _supplyMarker;
        private Vehicle _spawnMarker;

        protected SteeringController steering;	//System for controlling the bot's steering
        private float _seperation;
        private int _tickNextStrafeChange;          //The last time we changed strafe direction
        private bool _bStrafeLeft;					//Are we strafing left or right?
        private int _tickSuppliesDropped;
        private int _tickLastRadarDot;


        public Dropship(VehInfo.Car type, Helpers.ObjectState state, Arena arena)
            : base(type, state, arena,
            new SteeringController(type, state, arena))
        {
            Random rnd = new Random();
            _seperation = (float)rnd.NextDouble();
            steering = _movement as SteeringController;
            _rand = new Random();
            if (type.InventoryItems[0] != 0)
                _weapon.equip(AssetManager.Manager.getItemByID(type.InventoryItems[0]));

            _actionQueue = new List<Action>();
        }

        public void init(Helpers.ObjectState spawn, Script_Multi script)
        {
            _script = script;
            Arena.FlagState flag;
            if (_team._name == "Titan Militia")
                flag = _arena._flags.Values.OrderByDescending(f => f.posX).Where(f => f.team == _team).First();
            else
                flag = _arena._flags.Values.OrderByDescending(f => f.posX).Where(f => f.team == _team).First();

            Helpers.ObjectState dropPoint = findOpenSpawn((short)(flag.posX + 900), 1744, 1000);

            spawn.positionZ = 0;
            _supplyMarker = _arena.newVehicle(AssetManager.Manager.getVehicleByID(406), _team, null, dropPoint);
            _spawnMarker = _arena.newVehicle(AssetManager.Manager.getVehicleByID(407), _team, null, spawn);

            if (_supplyMarker == null)
                Log.write(TLog.Warning, "Unable to spawn supply drop marker");

            if (_spawnMarker == null)
                Log.write(TLog.Warning, "Unable to spawn supply drop spawn marker");


            _arena.sendArenaMessage(String.Format("&Supplies inbound to your coordinates ({0}), Sit tight Soldiers!",
                Helpers.posToLetterCoord(_supplyMarker._state.positionX, _supplyMarker._state.positionY)), 4);

            _targetLocation = _supplyMarker;
        }

        /// <summary>
        /// '
        /// Allows the script to maintain itself
        /// </summary>
        public override bool poll()
        {
            if (bOverriddenPoll)
                return base.poll();

            //Dead? Do nothing
            if (IsDead)
            {
                steering.steerDelegate = null;
                return base.poll();
            }

            if (!_arena._bGameRunning)
                base.destroy(false, true);

            int now = Environment.TickCount;
            //Radar Dot
            if (now - _tickLastRadarDot >= 900)
            {
                _tickLastRadarDot = now;
                IEnumerable<Player> enemies = _arena.Players.Where(p => p._team != _team);
               // Helpers.Player_RouteExplosion(_team.ActivePlayers, 1131, _state.positionX, _state.positionY, 0, 0, 0);
               // Helpers.Player_RouteExplosion(enemies, 1130, _state.positionX, _state.positionY, 0, 0, 0);
            }

            if (now - _tickSuppliesDropped >= 4350 && !_bDropped && _tickSuppliesDropped != 0)
            {
                VehInfo supplyVehicle = AssetManager.Manager.getVehicleByID(405);
                Helpers.ObjectState objState = new Helpers.ObjectState();

                objState.positionX = _targetLocation._state.positionX;
                objState.positionY = _targetLocation._state.positionY;

                Computer newVehicle = _arena.newVehicle(supplyVehicle, _team, null, objState) as Computer;
                SupplyDrop newDrop = new SupplyDrop(_team, newVehicle, objState.positionX, objState.positionY);
                _script._supplyDrops.Add(newDrop);

                _team.sendArenaMessage(String.Format("&Supplies have been dropped at {0}.", newVehicle._state.letterCoord()), 4);
                _bDropped = true;
                //Get rid of our marker
                _supplyMarker.destroy(false);
            }


            if (!_bDropped)
            {
                bool onSite = false;

                //Get to landing zone
                if (Helpers.distanceTo(_targetLocation._state, _state) > 45)
                    steering.steerDelegate = steerForFollowOwner;
                else
                {
                    steering.freezeMovement(10000);
                    onSite = true;
                }

                //Can we drop?
                if (onSite && !_dropItemFired)
                {
                    _tickSuppliesDropped = now;
                    _dropItemFired = true;

                    Helpers.Player_RouteExplosion(_arena.Players, 3215, _targetLocation._state.positionX, 
                        _targetLocation._state.positionY, 800, 0, 0);
                }
            }
            else
            {

                _targetLocation = _spawnMarker;
                bool onSite = false;

                //Get to landing zone
                if (Helpers.distanceTo(_targetLocation._state, _state) > 45)
                    steering.steerDelegate = steerForFollowOwner;
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
            }

            //Handle normal functionality
            return base.poll();
        }

        public void updatePath(int now)
        {
            //Does our path need to be updated?
            if (now - _tickLastPath > c_pathUpdateInterval)
            {   //Are we close enough to the team?

                //Update it!
                _tickLastPath = int.MaxValue;

                _arena._pathfinder.queueRequest(
                    (short)(_state.positionX / 16), (short)(_state.positionY / 16),
                    (short)(_targetLocation._state.positionX / 16), (short)(_targetLocation._state.positionY / 16),
                    delegate (List<Vector3> path, int pathLength)
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

        public Helpers.ObjectState findOpenSpawn(short posX, short posY, int radius)
        {
            Helpers.ObjectState warpPoint = null;

            try
            {
                int blockedAttempts = 20;

                short pX;
                short pY;

                while (true)
                {
                    pX = posX;
                    pY = posY;
                    Helpers.randomPositionInArea(_arena, radius, ref pX, ref pY);
                    if (_arena.getTile(pX, pY).Blocked)
                    {
                        blockedAttempts--;
                        if (blockedAttempts <= 0)
                            //Consider the area to be blocked
                            return null;
                        else
                            continue;
                    }

                    warpPoint = new Helpers.ObjectState();
                    warpPoint.positionX = pX;
                    warpPoint.positionY = pY;


                    break;

                }
            }
            catch (Exception ex)
            {
                Log.write(TLog.Exception, ex.Message);
            }
            return warpPoint;
        }

    }
}