using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class Jackpot
        {
            public int start;
            public int bubble;
            public int bubbleDelay;
            public int trickleGrow;
            public int killRewardPercentage;
            public int killFixed;

            public Jackpot(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["Jackpot"];

                start = Parser.GetInt("Start");
                bubble = Parser.GetInt("Bubble");
                bubbleDelay = Parser.GetInt("BubbleDelay");
                trickleGrow = Parser.GetInt("TrickleGrow");
                killRewardPercentage = Parser.GetInt("KillRewardPercentage");
                killFixed = Parser.GetInt("KillFixed");
            }
        }
    }
}
