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
    public partial class EliteMarine : Bot
    {

        private List<Action> _actionQueue;

        public void fireAtEnemy(int now)
        {
            //Allows the bot to update the target every poll
            _target = null;

            //Get the closest player
            bool bClearPath = false;
            _target = getTargetPlayer(ref bClearPath, _targetTeam);


            if (_target != null)
            {
                if (bClearPath)
                {   //What is our distance to the target?
                    double distance = (_state.position() - _target._state.position()).Length;
                    bool bFleeing = false;

                    //Too far?
                    if (distance > pursueDist)
                        steering.steerDelegate = steerForPersuePlayer;
                    //Quite short?
                    else if (distance < shortDist)
                    {
                        steering.bSkipRotate = true;
                        steering.steerDelegate = delegate (InfantryVehicle vehicle)
                        {
                            if (_target != null)
                                return vehicle.SteerForFlee(_target._state.position());
                            else
                                return Vector3.Zero;
                        };
                    }
                    //Just right
                    else
                        steering.steerDelegate = null;


                    if (!bFleeing)
                    {
                        //Should we be firing our rifle?
                        if (distance <= fireDist && distance > sgDist)
                        {
                            if (_target._occupiedVehicle != null && _target._occupiedVehicle._type.ClassId > 0 && _lawQuantity >= 1)
                                _weapon.equip(_arena._server._assets.getItemByID(1004));
                            else
                                _weapon.equip(AssetManager.Manager.getItemByID(_type.InventoryItems[0]));

                            if (_weapon.ableToFire())
                            {
                                int aimResult = _weapon.getAimAngle(_target._state);

                                if (_weapon.isAimed(aimResult))
                                {
                                    if (_target._occupiedVehicle != null && _target._occupiedVehicle._type.ClassId > 0 && _lawQuantity >= 1)
                                    {
                                        _lawQuantity--;
                                        _movement.freezeMovement(3000);
                                    }

                                    //Spot on! Fire?
                                    _itemUseID = _weapon.ItemID;
                                    _weapon.shotFired();
                                }

                                steering.bSkipAim = true;
                                steering.angle = aimResult;
                            }
                        }
                        //Should we be firing our SG?
                        else if (distance <= sgDist && distance > meleeDist)
                        {
                            _weapon.equip(AssetManager.Manager.getItemByID(_type.InventoryItems[1]));

                            if (_weapon.ableToFire())
                            {
                                int aimResult = _weapon.getAimAngle(_target._state);

                                if (_weapon.isAimed(aimResult))
                                {   //Spot on! Fire?
                                    _itemUseID = _weapon.ItemID;
                                    _weapon.shotFired();
                                }

                                steering.bSkipAim = true;
                                steering.angle = aimResult;
                            }

                        }
                        //Should we be firing our melee?
                        else if (distance <= meleeDist)
                        {
                            _weapon.equip(AssetManager.Manager.getItemByID(_type.InventoryItems[2]));

                            if (_weapon.ableToFire())
                            {
                                int aimResult = _weapon.getAimAngle(_target._state);

                                if (_weapon.isAimed(aimResult))
                                {   //Spot on! Fire?
                                    _itemUseID = _weapon.ItemID;
                                    _weapon.shotFired();
                                }

                                steering.bSkipAim = true;
                                steering.angle = aimResult;
                            }

                        }
                        else
                            steering.bSkipAim = false;
                    }
                }
                else
                {
                    updatePath(now);

                    //Navigate to him
                    if (_path == null)
                        //If we can't find out way to him, just mindlessly walk in his direction for now
                        steering.steerDelegate = steerForPersuePlayer;
                    else
                        steering.steerDelegate = steerAlongPath;
                }


            }
        }

        public void pushToEnemyFlag(int now)
        {
            Arena.FlagState targetFlag;
            List<Arena.FlagState> enemyflags;
            List<Arena.FlagState> flags;

            flags = _arena._flags.Values.OrderBy(f => f.posX).ToList();

            if (_team._name == "Titan Militia")
            {
                enemyflags = flags.Where(f => f.team != _team).Take(3).ToList();
            }
            else
            {
                enemyflags = flags.Where(f => f.team != _team).ToList();
            }

            if (enemyflags.Count >= 3)
                targetFlag = enemyflags[_rand.Next(0, 2)];
            else
                targetFlag = enemyflags[0];
            

            Helpers.ObjectState target = new Helpers.ObjectState();
                target.positionX = targetFlag.posX;
                target.positionY = targetFlag.posY;
            


            //What is our distance to the target?
            double distance = (_state.position() - target.position()).Length;


                //Does our path need to be updated?
                if (now - _tickLastPath > c_pathUpdateInterval)
            {
                _arena._pathfinder.queueRequest(
                           (short)(_state.positionX / 16), (short)(_state.positionY / 16),
                           (short)(target.positionX / 16), (short)(target.positionY / 16),
                           delegate (List<Vector3> path, int pathLength)
                           {
                               if (path != null)
                               {   //Is the path too long?
                                   if (pathLength > c_MaxPath)
                                   {   //Destroy ourself and let another zombie take our place
                                       //_path = null; Destroying Disasbled for now, may replace with a distance from enemy check
                                       //destroy(true);
                                       _path = path;
                                       _pathTarget = 1;
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

            //Navigate to him
            if (_path == null)
                //If we can't find out way to him, just mindlessly walk in his direction for now
                steering.steerDelegate = steerForPersuePlayer;
            else
                steering.steerDelegate = steerAlongPath;
        }
        public void patrolBetweenFlags(int now)
        {
            Arena.FlagState friendlyflag;
            Arena.FlagState targetFlag;
            List<Arena.FlagState> enemyflags;
            List<Arena.FlagState> flags;

            flags = _arena._flags.Values.OrderBy(f => f.posX).ToList();

            if (_team._name == "Titan Militia")
            {
                friendlyflag = flags.Where(f => f.team == _team).Last();
                enemyflags = flags.Where(f => f.team != _team).Take(3).ToList();
            }
            else
            {
                friendlyflag = _arena._flags.Values.OrderBy(f => f.posX).Where(f => f.team == _team).First();
                enemyflags = _arena._flags.Values.OrderByDescending(f => f.posX).Where(f => f.team != _team).Take(2).ToList();
            }

            if (enemyflags.Count >= 3)
                targetFlag = enemyflags[_rand.Next(0, 1)];
            else
                targetFlag = enemyflags[0];





            Helpers.ObjectState target = new Helpers.ObjectState();
            if (_bPatrolEnemy)
            {
                target.positionX = targetFlag.posX;
                target.positionY = targetFlag.posY;
            }
            else
            {
                target.positionX = friendlyflag.posX;
                target.positionY = friendlyflag.posY;
            }


            //What is our distance to the target?
            double distance = (_state.position() - target.position()).Length;

            //Are we there yet?
            if (distance < patrolDist)
            {
                //change our direction
                _bPatrolEnemy = !_bPatrolEnemy;
            }

            //Does our path need to be updated?
            if (now - _tickLastPath > c_pathUpdateInterval)
            {
                _arena._pathfinder.queueRequest(
                           (short)(_state.positionX / 16), (short)(_state.positionY / 16),
                           (short)(target.positionX / 16), (short)(target.positionY / 16),
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

            //Navigate to him
            if (_path == null)
                //If we can't find out way to him, just mindlessly walk in his direction for now
                steering.steerDelegate = steerForPersuePlayer;
            else
                steering.steerDelegate = steerAlongPath;
        }


        public void wander(int now)
        {

            if (now - _tickLastWander < 6000)
                return;


            if (_targetPoint == null)
                _targetPoint = getTargetPoint();


            //What is our distance to the target?
            double distance = (_state.position() - _targetPoint.position()).Length;

            //Are we there yet?
            if (distance < patrolDist)
            {
                _targetPoint = null;
                _tickLastWander = now;
                return;
            }

            //Does our path need to be updated?
            if (now - _tickLastPath > c_pathUpdateInterval)
            {
                _arena._pathfinder.queueRequest(
                           (short)(_state.positionX / 16), (short)(_state.positionY / 16),
                           (short)(_targetPoint.positionX / 16), (short)(_targetPoint.positionY / 16),
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

            //Navigate to him
            if (_path == null)
                //If we can't find out way to him, just mindlessly walk in his direction for now
                steering.steerDelegate = steerForPersuePlayer;
            else
                steering.steerDelegate = steerAlongPath;
        }


        public Helpers.ObjectState getTargetPoint()
        {
            Helpers.ObjectState target = new Helpers.ObjectState();

            Vehicle potentialTarget = _arena.getVehiclesInRange(_state.positionX, _state.positionY, 5000).
                FirstOrDefault(v => v._type.Name == "[RTS] Marine Barracks");

            if (potentialTarget == null)
                return null;

            int blockedAttempts = 30;
            short pX;
            short pY;
            while (true)
            {
                pX = potentialTarget._state.positionX;
                pY = potentialTarget._state.positionY;
                Helpers.randomPositionInArea(_arena, 1000, ref pX, ref pY);
                if (_arena.getTile(pX, pY).Blocked)
                {
                    blockedAttempts--;
                    if (blockedAttempts <= 0)
                        //Consider the spawn to be blocked
                        return null;
                    continue;
                }

                target.positionX = pX;
                target.positionY = pY;
                break;
            }
            return target;
        }

        public class Action
        {
            public Priority priority;
            public Type type;

            public Action(Priority pr, Type ty)
            {
                type = ty;
                priority = pr;
            }

            public enum Priority
            {
                None,
                Low,
                Medium,
                High
            }

            public enum Type
            {
                fireAtEnemy,
                retreat
            }
        }
    }
}
