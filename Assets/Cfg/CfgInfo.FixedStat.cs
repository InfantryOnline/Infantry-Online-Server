using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class FixedStat
        {
            public string name;
            public string squad;
            public string points;
            public string kills;
            public string deaths;
            public string killPoints;
            public string deathPoints;
            public string vehKills;
            public string vehDeaths;
            public string assistPoints;
            public string bonusPoints;
            public string mvpScore;
            public string playSeconds;

            public FixedStat(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["FixedStat"];

                name = Parser.GetString("Name");
                squad = Parser.GetString("Squad");
                points = Parser.GetString("Points");
                kills = Parser.GetString("Kills");
                deaths = Parser.GetString("Deaths");
                killPoints = Parser.GetString("KillPoints");
                deathPoints = Parser.GetString("DeathPoints");
                vehKills = Parser.GetString("VehKills");
                vehDeaths = Parser.GetString("VehDeaths");
                assistPoints = Parser.GetString("AssistPoints");
                bonusPoints = Parser.GetString("BonusPoints");
                mvpScore = Parser.GetString("MvpScore");
                playSeconds = Parser.GetString("PlaySeconds");
            }
        }
    }
}
