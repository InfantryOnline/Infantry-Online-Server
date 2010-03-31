using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class SoccerMvp
        {
            public int goals;
            public int assists;
            public int kills;
            public int steals;
            public int passes;
            public int deaths;
            public int fumbles;
            public int catches;
            public int carryTimeFactor;
            public int saves;
            public int pinches;

            public SoccerMvp(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["SoccerMvp"];

                goals = Parser.GetInt("Goals");
                assists = Parser.GetInt("Assists");
                kills = Parser.GetInt("Kills");
                steals = Parser.GetInt("Steals");
                passes = Parser.GetInt("Passes");
                deaths = Parser.GetInt("Deaths");
                fumbles = Parser.GetInt("Fumbles");
                catches = Parser.GetInt("Catches");
                carryTimeFactor = Parser.GetInt("CarryTimeFactor");
                saves = Parser.GetInt("Saves");
                pinches = Parser.GetInt("Pinches");
            }
        }
    }
}
