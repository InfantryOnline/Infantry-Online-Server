using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class Rts
        {
            public bool game;
            public int startDelay;
            public int minimumPlayers;
            public bool startBubble;
            public int victoryPointReward;
            public int victoryExperienceReward;
            public int victoryCashReward;
            public int victoryBong;
            public int lossPointReward;
            public int lossExperienceReward;
            public int lossCashReward;

            public Rts(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["Rts"];

                game = Parser.GetBool("Game");
                startDelay = Parser.GetInt("StartDelay");
                minimumPlayers = Parser.GetInt("MinimumPlayers");
                startBubble = Parser.GetBool("StartBubble");
                victoryPointReward = Parser.GetInt("VictoryPointReward");
                victoryExperienceReward = Parser.GetInt("VictoryExperienceReward");
                victoryCashReward = Parser.GetInt("VictoryCashReward");
                victoryBong = Parser.GetInt("VictoryBong");
                lossPointReward = Parser.GetInt("LossPointReward");
                lossExperienceReward = Parser.GetInt("LossExperienceReward");
                lossCashReward = Parser.GetInt("LossCashReward");
            }
        }
    }
}
