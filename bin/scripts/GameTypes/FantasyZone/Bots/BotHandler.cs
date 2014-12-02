using System;
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
        private class fantasyBot
        {
            public int vehID;
            public int spawnID;
            public int count;
            public int max;

            public fantasyBot(int bid, int sid, int m)
            {
                vehID = bid;
                spawnID = sid;
                count = 0;
                max = m;
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