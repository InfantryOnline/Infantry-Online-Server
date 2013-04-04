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


    ///////////////////////////////////////////////////////
    public class Chopper : Bot
    {
        public Vehicle _targetLocation;
        protected SteeringController steering;	//System for controlling the bot's steering


        public Chopper(VehInfo.Car type, Helpers.ObjectState state, Arena arena)
            //: base(
            : base(type, state, arena,
                    new SteeringController(type, state, arena))
        {
            Random rnd = new Random(Environment.TickCount);
            steering = _movement as SteeringController;

            if (type.InventoryItems[0] != 0)
                _weapon.equip(AssetManager.Manager.getItemByID(type.InventoryItems[0]));
        }

        /// <summary>
        /// Looks after the bot's functionality
        /// </summary>
        public override bool poll()
        {
            //Find the place we want to land at
            List<Vehicle> inTrackingRange = _arena.getVehiclesInRange(_state.positionX, _state.positionY, 10000000);

            foreach (Vehicle v in inTrackingRange)
            {
                if (v._type.Id == 410)
                    _targetLocation = v;
            }
            //Get to landing zone
            if (Helpers.distanceTo(_targetLocation._state, _state) > 45)
                steering.steerDelegate = steerForFollowOwner;
            else
                steering.freezeMovement(1000);

            //    if (_targetLocation._state.positionZ == _state.positionZ)


            return base.poll();
        }



        /// <summary>
        /// Keeps the combat bot around the engineer
        /// Change to keeping him around the HQ
        /// </summary>
        public Vector3 steerForFollowOwner(InfantryVehicle vehicle)
        {

            Vector3 pursuitSteer = vehicle.SteerForPursuit(_targetLocation.Abstract, 0.2f);
            return pursuitSteer;
        }
    }
}
