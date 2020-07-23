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
    {

        private List<Action> _actionQueue;

        public void moveToDrop(int now)
        {
            bool onSite = false;


            //Find the place we want to land at
            List<Vehicle> inTrackingRange = _arena.getVehiclesInRange(_state.positionX, _state.positionY, 10000000);

            foreach (Vehicle v in inTrackingRange)
            {
                if (v._type.Id == 406 && v._team == _team)
                    _targetLocation = v;
            }
            //Get to landing zone
            if (Helpers.distanceTo(_targetLocation._state, _state) > 45)
                steering.steerDelegate = steerForFollowOwner;
            else
            {
                steering.freezeMovement(5000);
                onSite = true;
            }

            //Can we drop?
            if (onSite && !_bDropped)
            {
                _itemUseID = _weapon.ItemID;
                _weapon.shotFired();
                _bDropped = true;
            }

            //Does our path need to be updated?
            if (now - _tickLastPath > c_pathUpdateInterval)
            {
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

            //Navigate to him
            if (_path == null)
                //If we can't find out way to him, just mindlessly walk in his direction for now
                steering.steerDelegate = steerForFollowOwner;
            else
                steering.steerDelegate = steerAlongPath;
        }

        public void leaveMap(int now)
        {
            bool onSite = false;


            //Find the place we want to land at
            List<Vehicle> inTrackingRange = _arena.getVehiclesInRange(_state.positionX, _state.positionY, 10000000);

            foreach (Vehicle v in inTrackingRange)
            {
                if (v._type.Id == 407 && v._team == _team)
                    _targetLocation = v;
            }
            //Get to landing zone
            if (Helpers.distanceTo(_targetLocation._state, _state) > 45)
                steering.steerDelegate = steerForFollowOwner;
            else
            {
                steering.freezeMovement(5000);
                onSite = true;
            }
            //Cya
            if (onSite && !_bDropped)
            {
                base.destroy(true);
            }


            updatePath(now);

            //Navigate to him
            if (_path == null)
                //If we can't find out way to him, just mindlessly walk in his direction for now
                steering.steerDelegate = steerForPersuePlayer;
            else
                steering.steerDelegate = steerAlongPath;
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
                moveToDrop,
                dropPayload,
                fireAtEnemy,
                leaveMap
            }
        }
    }
}
