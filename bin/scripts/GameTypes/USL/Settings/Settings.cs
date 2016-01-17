using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfServer.Script.GameType_USL
{
    public abstract class Settings
    {
        public bool Events = false;			//Are events enabled? (mini-maps/etc)
        public bool Voting = true;	        //Can we vote?
        public int VotingTime = 30;         //How long our voting time is
        public bool SpawnEvent = false;     //Are we doing a spawning event?
        public int SpawnTimer;              //When to spawn a 30k item
        public int LeagueSeason;
        public int OvertimeCount = 0;
        public bool AwardMVP = false;       //Can we award an mvp now? (Only true after a game has ended)
        public List<string> CurrentEventTypes;
        private string defaultOT = "Game ended in a draw. ";

        public enum GameTypes
        {
            NULL,
            TDM,
            LEAGUEMATCH,
            LEAGUEOVERTIME,
            EVENT,
            SPAWNEVENT
        }

        public enum EventTypes
        {
            REDBLUE,
            GREENYELLOW,
            WHITEBLACK,
            PINKPURPLE,
            GOLDSILVER,
            BRONZEDIAMOND,
            ORANGEGRAY
        }

        public enum SpawnEventTypes
        {
            SOLOTHIRTYK,
            TEAMTHIRTYK
        }

        /// <summary>
        /// Gets the overtime string used for an arena message
        /// </summary>
        public string GetOT()
        {
            switch (OvertimeCount)
            {
                case 0:
                    return defaultOT + "Going into OVERTIME!!";
                case 1:
                    return defaultOT + "Going into DOUBLE OVERTIME!!";
                case 2:
                    return defaultOT + "Going into TRIPLE OVERTIME!!";
                case 3:
                    return defaultOT + "Going into QUADRUPLE OVERTIME?!?";
                default:
                    return defaultOT + "Script is tired of counting, ref's take over. AFK!!";
            }
        }
    }
}