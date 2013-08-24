/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Logic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;

using Assets;

namespace InfServer.Script.GameType_Fantasy
{
    public class BotHandler
    {
        private Arena _arena;

        //Bots
        private int _tickLastBotSpawn;
        private int _nestID = 400;
        private int _botSpawnRate = 8000;

        //Bot spawns
        private List<fantasyBot> _spawns = new List<fantasyBot>();
        public class fantasyBot
        {
            //General settings
            public int ID;                  //Our bot ID
            public int vehID;               //Our vehicle ID
            public int spawnID;             //Vehicle ID of where we spawn
            public int count;               //Current amount of this bot
            public int max;                 //Max amount of this bot for assigned spawn
            public int multiple;            //Multiple of bots to spawn at one time
            public int ticksBetweenSpawn;   //How many ticks to wait in between spawns

            //Targetting settings
            public bool atkBots;    //Do we attack bots
            public bool atkVeh;     //Do we attack computer vehicles?
            public bool atkPlayer;  //Do we attack players?
            public bool atkCloak;   //Do we attack cloakers?

            //Spawn point settings
            public bool relyOnSpawn;//Do we cease to exist when our spawn point is missing

            //Distances
            public int defenseRadius;       //Distance from player until we attack them
            public int distanceFromHome;    //Distance from home we have to be to ignore targets and run back [if not fighting]
            public int patrolRadius;        //Radius to patrol around home
            public int lockInTime;          //Amount of time we spend focused on one target
            public int attackRadius;        //Distance from home that we keep engaging units
            //      public bool patrolWaypoints;       //Whether or not to use waypoints for patrol

            //Weapon settings

            //Utility settings


            public fantasyBot(int id)
            {
                //General settings
                ID = id;

            }

        }

        /// <summary>
        /// Generic Constructor
        /// </summary>
        public BotHandler(Arena arena)
        {
            _arena = arena;

            //Handles spawns (bot vehicle id, spawn location vehicle id, max amount of bots for this location)
            _spawns.Add(new fantasyBot(105, 400, 5));
        }

        //Keeps our bots in check.
        public bool poll()
        {
            int now = Environment.TickCount;
            //Find the nest and spawn a spider at it or whatever
            if (now - _tickLastBotSpawn > _botSpawnRate)
            {
                _tickLastBotSpawn = now;

                foreach (Vehicle v in _arena.Vehicles.ToList())
                    foreach (var p in _spawns)
                        if (p.spawnID == v._type.Id)
                        {
                            //First check to see if we have enough room to spawn this one
                            p.count = 0;
                            foreach (Vehicle existing in _arena.getVehiclesInRange(v._state.positionX, v._state.positionY, 200))
                                if (existing._type.Id == p.vehID)
                                    p.count++;

                            if (p.count < p.max)
                                _arena.newBot(typeof(RangeMinion), (ushort)p.vehID, _arena.Teams.ElementAt(5), null, v._state);
                        }
            }



            return true;
        }
    }
}
*/