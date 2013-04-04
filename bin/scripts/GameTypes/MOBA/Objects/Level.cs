using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfServer.Script.GameType_MOBA
{
    public class Level
    {
        private int level;
        private int experience;

        public Level()
        {
            level = 0;
            experience = 0;
        }

        public int getLevel()
        {
            return level;
        }
        public int getExperience()
        {
            return experience;
        }
    }
}
