using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class Flag
        {
            public bool trickleKill;
            public int pointMultiplier;
            public int experienceMultiplier;
            public int cashMultiplier;
            public int carryCount;
            public int enterGameDelay;
            public int resetDelay;
            public int periodicRewardDelay;
            public int pointReward;
            public int experienceReward;
            public int cashReward;
            public int victoryBong;
            public int resetBong;
            public int periodicBong;
            public int carrierOnRadar;
            public int carrierLineOfSight;
            public int dropPointRadius;
            public int victoryWarningBong;
            public int victoryHoldTime;
            public int prizeDistance;
            public int victoryAbortedBong;
            public int allowSafety;
            public int startDelay;
            public int warpPickupDelay;
            public bool showTimer;
            public bool victoryWhenOneTeam;
            public bool startBubble;
            public bool autoPickup;
            public bool accessibleFlagToDropPoint;
            public bool accessibleDropPoint;
            public bool displayStatus;
            public bool useJackpot;
            public int dropPointRadiusTimeTolerance;
            public bool announceTransfers;
            public bool allowJoiningWinningTeam;
            public int defaultWinFrequency;
            public bool restoreUnownedDroppedFlags;
            public int mvpBubble;
            public int winnerJackpotFixedPercent;
            public int winnerJackpotMvpPercent;
            public int loserJackpotFixedPercent;
            public int loserJackpotMvpPercent;

            public Flag(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["Flag"];

                trickleKill = Parser.GetBool("TrickleKill");
                pointMultiplier = Parser.GetInt("PointMultiplier");
                experienceMultiplier = Parser.GetInt("ExperienceMultiplier");
                cashMultiplier = Parser.GetInt("CashMultiplier");
                carryCount = Parser.GetInt("CarryCount");
                enterGameDelay = Parser.GetInt("EnterGameDelay");
                resetDelay = Parser.GetInt("ResetDelay");
                periodicRewardDelay = Parser.GetInt("PeriodicRewardDelay");
                pointReward = Parser.GetInt("PointReward");
                experienceReward = Parser.GetInt("ExperienceReward");
                cashReward = Parser.GetInt("CashReward");
                victoryBong = Parser.GetInt("VictoryBong");
                resetBong = Parser.GetInt("ResetBong");
                periodicBong = Parser.GetInt("PeriodicBong");
                carrierOnRadar = Parser.GetInt("CarrierOnRadar");
                carrierLineOfSight = Parser.GetInt("CarrierLineOfSight");
                dropPointRadius = Parser.GetInt("DropPointRadius");
                victoryWarningBong = Parser.GetInt("VictoryWarningBong");
                victoryHoldTime = Parser.GetInt("VictoryHoldTime");
                prizeDistance = Parser.GetInt("PrizeDistance");
                victoryAbortedBong = Parser.GetInt("VictoryAbortedBong");
                allowSafety = Parser.GetInt("AllowSafety");
                startDelay = Parser.GetInt("StartDelay");
                warpPickupDelay = Parser.GetInt("WarpPickupDelay");
                showTimer = Parser.GetBool("ShowTimer");
                victoryWhenOneTeam = Parser.GetBool("VictoryWhenOneTeam");
                startBubble = Parser.GetBool("StartBubble");
                autoPickup = Parser.GetBool("AutoPickup");
                accessibleFlagToDropPoint = Parser.GetBool("AccessibleFlagToDropPoint");
                accessibleDropPoint = Parser.GetBool("AccessibleDropPoint");
                displayStatus = Parser.GetBool("DisplayStatus");
                useJackpot = Parser.GetBool("UseJackpot");
                dropPointRadiusTimeTolerance = Parser.GetInt("DropPointRadiusTimeTolerance");
                announceTransfers = Parser.GetBool("AnnounceTransfers");
                allowJoiningWinningTeam = Parser.GetBool("AllowJoiningWinningTeam");
                defaultWinFrequency = Parser.GetInt("DefaultWinFrequency");
                restoreUnownedDroppedFlags = Parser.GetBool("RestoreUnownedDroppedFlags");
                mvpBubble = Parser.GetInt("MvpBubble");
                winnerJackpotFixedPercent = Parser.GetInt("WinnerJackpotFixedPercent");
                winnerJackpotMvpPercent = Parser.GetInt("WinnerJackpotMvpPercent");
                loserJackpotFixedPercent = Parser.GetInt("LoserJackpotFixedPercent");
                loserJackpotMvpPercent = Parser.GetInt("LoserJackpotMvpPercent");
            }
        }
    }
}
