using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


using InfServer.Logic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;

namespace InfServer.Script.GameType_HQ
{
    public class HQ
    {
        public Team team;
        public int level;
        public int bounty;
        public int nextLvl;
        public Vehicle vehicle;
        public Vehicle pylon;
        public int maxHealth;

        public HQ(Vehicle hq)
        {
            level = 1;
            vehicle = hq;
            team = hq._team;
            nextLvl = Script_HQ.baseBounty;
            Events.newHQ(this);
            maxHealth = hq._type.Hitpoints;

        }
    }
}
