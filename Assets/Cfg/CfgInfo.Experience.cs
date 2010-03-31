using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class Experience
        {
            public int shareRadius;
            public int sharePercent;
            public int percentOfKiller;
            public int percentOfTarget;
            public int killReward;

            public Experience(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["Experience"];

                shareRadius = Parser.GetInt("ShareRadius");
                sharePercent = Parser.GetInt("SharePercent");
                percentOfKiller = Parser.GetInt("PercentOfKiller");
                percentOfTarget = Parser.GetInt("PercentOfTarget");
                killReward = Parser.GetInt("KillReward");
            }
        }
    }
}
