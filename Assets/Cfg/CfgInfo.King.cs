using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class King
        {
            public int deathCount;
            public int crownRecoverKills;
            public int expireTime;
            public int minimumPlayers;
            public int pointReward;
            public int experienceReward;
            public int cashReward;
            public int victoryBong;
            public int startDelay;
            public string crownGraphic;
            public bool giveSpecsCrowns;
            public bool teamRewards;
            public bool loseCrownExpire;
            public bool showTimer;
            public bool startBubble;

            public King(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["King"];

                deathCount = Parser.GetInt("DeathCount");
                crownRecoverKills = Parser.GetInt("CrownRecoverKills");
                expireTime = Parser.GetInt("ExpireTime");
                minimumPlayers = Parser.GetInt("MinimumPlayers");
                pointReward = Parser.GetInt("PointReward");
                experienceReward = Parser.GetInt("ExperienceReward");
                cashReward = Parser.GetInt("CashReward");
                victoryBong = Parser.GetInt("VictoryBong");
                startDelay = Parser.GetInt("StartDelay");
                crownGraphic = Parser.GetString("CrownGraphic");
                giveSpecsCrowns = Parser.GetBool("GiveSpecsCrowns");
                teamRewards = Parser.GetBool("TeamRewards");
                loseCrownExpire = Parser.GetBool("LoseCrownExpire");
                showTimer = Parser.GetBool("ShowTimer");
                startBubble = Parser.GetBool("StartBubble");

                //Load the blobs
                BlobsToLoad.Add(Parser.GetBlob(crownGraphic));
            }
        }
    }
}
