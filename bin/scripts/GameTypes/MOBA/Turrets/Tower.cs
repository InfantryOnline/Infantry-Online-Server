using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Game;
using InfServer.Protocol;
using InfServer.Scripting;
using InfServer.Bots;

using Axiom.Math;
using Assets;

namespace InfServer.Script.GameType_MOBA
{
    public class Tower : Computer
    {
        public Script_MOBA _moba;			    //The MOBA script
        ///////////////////////////////////////////////////
        // Member Functions
        ///////////////////////////////////////////////////
        /// <summary>
        /// Generic constructor
        /// </summary>
        public Tower(VehInfo.Computer type, Arena arena)
            : base(type, arena)
        {
        }

        /// <summary>
        /// Keeps the vehicle state updated, and sends an update packet if necessary
        /// </summary>
        /// <returns>A boolean indicating whether an update packet should be sent</returns>
        public override bool poll()
        {
            //If not reloaded yet don't fire
            int now = Environment.TickCount;

            if (_tickShotTime + _fireDelay > now ||
                _tickReloadTime > now)
            {	//But maybe send an update packet?
                if (now - _tickLastUpdate > 300)
                {
                    _tickLastUpdate = now;
                    return true;
                }

                return false;
            }

            //FIRE -- the Projectile class will take care of aiming and targeting but make sure we can shoot here
            if (getTarget() != null)            
                _moba.spawnProjectile((ushort)104, _state, _team);            

            //Adjust ammo accordingly
            if (_ammoCapacity != 0 && --_ammoRemaining <= 0)
            {
                _tickReloadTime = Environment.TickCount + _reloadTime;
                _ammoRemaining = _ammoCapacity;
            }

            return true;
        }

        /// <summary>
        /// Obtains a suitable target
        /// </summary>
        protected Vehicle getTarget()
        {
            Vehicle target = null;
            //Make a list of players around us within a radius
            List<Vehicle> inTrackingRange =
                _arena.getVehiclesInRange(_state.positionX, _state.positionY, 300);

            //Check if anyone was found
            if (inTrackingRange.Count == 0)
                return null;

            //Sort by distance to bot
            inTrackingRange.Sort(
                delegate(Vehicle p, Vehicle q)
                {
                    return Comparer<double>.Default.Compare(
                        Helpers.distanceSquaredTo(_state, p._state), Helpers.distanceSquaredTo(_state, q._state));
                }
            );
            //Go through players first
            foreach (Player p in _arena.PlayersIngame)
            {
                //Don't shoot our own
                if (p._team == _team)
                    continue;

                //Don't attack ones too far away from us
                if (Helpers.distanceTo(_state, p._state) > 300)
                    continue;

                return p._baseVehicle;

            }

            //Go through all the vehicles and find the closest one that is not on our team and is not dead            
            foreach (Vehicle p in inTrackingRange)
            {
                //See if they are dead
                if (p.IsDead)
                    continue;

                //See if they are on our team
                if (p._team == _team)
                    continue;
                
                //See if they are within our range
                if (Helpers.distanceTo(_state, p._state) > 300)
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

    }

}