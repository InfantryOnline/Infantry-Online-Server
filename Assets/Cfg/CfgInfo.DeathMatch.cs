using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class DeathMatch
        {
            public int startDelay;
            public int timer;
            public int teamKills;
            public int personalKills;
            public int minimumPlayers;
            public int teamVictory;
            public int personalVictory;
            public int victoryBong;
            public int timerBubble;

            public DeathMatch(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["DeathMatch"];

                startDelay = Parser.GetInt("StartDelay");
                timer = Parser.GetInt("Timer");
                teamKills = Parser.GetInt("TeamKills");
                personalKills = Parser.GetInt("PersonalKills");
                minimumPlayers = Parser.GetInt("MinimumPlayers");
                teamVictory = Parser.GetInt("TeamVictory");
                personalVictory = Parser.GetInt("PersonalVictory");
                victoryBong = Parser.GetInt("VictoryBong");
                timerBubble = Parser.GetInt("TimerBubble");
            }
        }
    }
}
