using System.Collections.Generic;
using System;
namespace Assets
{
    public partial class CfgInfo
    {
        public class Bot
        {
            public int shareRadius;
            public int sharePercent;
            public int cashKillReward;
            public int expKillReward;
            public int pointsKillReward;
            public int fixedBountyToKiller;

            public Bot(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                try
                {
                    Parser.values = stringTree["Bot"];

                    shareRadius = Parser.GetInt("KillShareRadius");
                    sharePercent = Parser.GetInt("KillSharePercent");
                    cashKillReward = Parser.GetInt("KillCashReward");
                    expKillReward = Parser.GetInt("KillExpReward");
                    pointsKillReward = Parser.GetInt("KillPointsReward");
                    fixedBountyToKiller = Parser.GetInt("FixedBountyToKiller");
                }
                catch
                {
                }
            }
        }
    }
}
