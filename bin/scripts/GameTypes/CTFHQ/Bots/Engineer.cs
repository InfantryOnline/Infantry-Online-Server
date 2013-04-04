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
    class Engineer : Bot
    {
        ///////////////////////////////////////////////////
        // Member variables
        ///////////////////////////////////////////////////

        protected SteeringController steering;	//System for controlling the bot's steering
        protected Script_CTFHQ ctfhq;			//The CTFHQ script
        protected List<Vector3> _path;			//The path to our destination

        protected Vehicle _nextRet;             //The next turret we are running to
        protected int _pathTarget;				//The next target node of the path
        protected int _tickLastPath;			//The time at which we last made a path to the player   
        protected int _tickLastRet;             //Last time we ran to a ret
        protected int _tickLastHeal;            //Last time we healed a ret
        private float _seperation;
        private bool _hq;                            //Tells us if HQ exists

        ///////////////////////////////////////////////////
        // Member Functions
        ///////////////////////////////////////////////////

        /// <summary>
        /// Generic constructor
        /// </summary>
        public Engineer(VehInfo.Car type, Helpers.ObjectState state, Arena arena, Script_CTFHQ _ctfhq)
            : base(type, state, arena,
                    new SteeringController(type, state, arena))
        {
            Random rnd = new Random();

            _seperation = (float)rnd.NextDouble();
            steering = _movement as SteeringController;

            if (type.InventoryItems[0] != 0)
                _weapon.equip(AssetManager.Manager.getItemByID(type.InventoryItems[0]));

            ctfhq = _ctfhq;
            _tickLastRet = 0;
            _tickLastHeal = 0;
        }
        /// <summary>
        /// Looks after the bot's functionality
        /// </summary>
        public override bool poll()
        {
            //Dead? Do nothing
            if (IsDead)
            {
                steering.steerDelegate = null;
                bCondemned = true;
                ctfhq._currentEngineers--;
                ctfhq.engineerBots.Remove(_team);
                return base.poll();
            }

            int now = Environment.TickCount;

            IEnumerable<Vehicle> hqs = _arena.Vehicles.Where(v => v._type.Id == ctfhq._hqVehId);
            foreach (Vehicle hq in hqs)
            {
                if (hq._team == _team)
                {
                    _hq = true;
                    break;
                }
            }

            //Lets build ourselves an HQ
            if (!_hq)
            {
                _hq = true; //Mark it as existing

                //Create their HQ
                createVehicle(463, 100, 50, _team); //Build our HQ which will spawn our captain

                ///////////////////////////////////////////
                //             G4 Base                   //
                // 400 = mg, 825 = rocket, 700  = plasma //
                ///////////////////////////////////////////
                //createVehicle(825, -289, 1, _team); // ap rocket to the left of the base in corner

                //createWall(803, -47, 0, 10, 0, 25, _team); // Vertical wall in base room
                //createWall(803, -197, -10, 5, 25, 0, _team);//Horizontal wall at door to hq room

                //createVehicle(700, 139, 149, _team); // plasma turret below base
                //createVehicle(400, 50, 149, _team);

                //////////////////////////////////////////////////
                //                  Generic Base                //
                //////////////////////////////////////////////////
                //Wall the HQ in
                /*    createWall(803, -25, -25, 6, 0, 25, _team);
                    createWall(803, -25, -25, 9, 25, 0, _team);
                    createWall(803, 200, -25, 6, 0, 25, _team);
                    createWall(803, 15, 100, 5, 25, 0, _team);*/

                //Build a rocket to the left
                createVehicle(825, -75, -25, _team);
                //Build a plasma ret in the walls on the bottom right
                createVehicle(700, 25, 80, _team);
                //Build four MGs around base
                createVehicle(400, -75, 50, _team);
                createVehicle(700, 75, -50, _team);
                createVehicle(400, 75, 150, _team);

                //Create wall around rets now
                //     createWall(803, -110, -80, 7, 50, 0, _team);
                //        createWall(803, -110, -80, 6, 0, 50, _team);
                //      createWall(803, 250, -80, 6, 0, 50, _team);
                //       createWall(803, -50, 200, 5, 50, 0, _team);

                //Giving them some bounty ??based off population??
                ctfhq._hqs[_team].Bounty = 10000;

                //Captain dBot = _arena.newBot(typeof(Captain), (ushort)161, _team, null, _state, new object[] { this, null }) as Captain;

            }

            //Run around while healing to hopefully avoid being shot            
            IEnumerable<Vehicle> turrets = _arena.Vehicles.Where(v => (v._type.Id == 400 || v._type.Id == 825 || v._type.Id == 700) && v._team == _team);
            Random _rand = new Random(System.Environment.TickCount);

            //Only heal a ret once every 7 seconds
            if (now - _tickLastHeal > 7000)
            {
                foreach (Vehicle v in turrets)
                {//Go through and heal them randomly
                    if (v._team == _team && v._state.health < _arena._server._assets.getVehicleByID(v._type.Id).Hitpoints)
                    {//Turret needs healing
                        v.assignDefaultState();
                        _tickLastHeal = now;
                        break;
                    }
                }
            }
            //Pick another ret to follow every 5 seconds
            if (now - _tickLastRet > 5000)
            {
                try
                {
                    _nextRet = turrets.ElementAt(_rand.Next(0, turrets.Count()));
                }
                catch (Exception)
                {
                    _nextRet = null;
                }
                _tickLastRet = now;
            }

            //Run towards a turret
            if (_nextRet != null)
                steering.steerDelegate = steerForFollowOwner;

            //Handle normal functionality
            return base.poll();
        }
        //Creates a wall of vechicles based on length and spacing given
        public void createWall(int id, int x_offset, int y_offset, int length, int xspacing, int yspacing, Team botTeam)
        {
            for (int i = 0; i < length; i++)
            {
                createVehicle(id, x_offset + (i * xspacing), y_offset + (i * yspacing), botTeam);
            }
        }

        //Creates a turrent, offsets are from HQ
        public void createVehicle(int id, int x_offset, int y_offset, Team botTeam)
        {
            VehInfo vehicle = _arena._server._assets.getVehicleByID(Convert.ToInt32(id));
            Helpers.ObjectState newState = new Protocol.Helpers.ObjectState();
            newState.positionX = Convert.ToInt16(_state.positionX + x_offset);
            newState.positionY = Convert.ToInt16(_state.positionY + y_offset);
            newState.positionZ = _state.positionZ;
            newState.yaw = _state.yaw;

            _arena.newVehicle(
                        vehicle,
                        botTeam, null,
                        newState);

        }

        #region Steer Delegates

        /// <summary>
        /// Keeps the combat bot around the engineer
        /// Change to keeping him around the HQ
        /// </summary>
        public Vector3 steerForFollowOwner(InfantryVehicle vehicle)
        {

            Vector3 wanderSteer = vehicle.SteerForWander(0.5f);
            Vector3 pursuitSteer = vehicle.SteerForPursuit(_nextRet.Abstract, 0.2f);

            return (wanderSteer * 1.6f) + pursuitSteer;
        }
        #endregion
    }
}
