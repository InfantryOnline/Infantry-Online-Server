using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class Soccer
        {
            public int ballCount;
            public int minimumPlayers;
            public int sendTime;
            public int victoryPointReward;
            public int victoryExperienceReward;
            public int victoryCashReward;
            public int victoryGoals;
            public int passDelay;
            public bool showRadar;
            public int defaultThrowTime;
            public int defaultProximity;
            public int defaultFriction;
            public int defaultBallSpeed;
            public int gravityAcceleration;
            public int bounceHorzSpeedPercent;
            public string ballGraphic;
            public string trailGraphic;
            public string shadowGraphic;
            public string catchSound;
            public string throwSound;
            public string bounceSound;
            public int goalBong;
            public int victoryBong;
            public int startDelay;
            public int playersPerBall;
            public int killerCatchBall;
            public int deadBallTimer;
            public int ignoreWalls;
            public int invisibleTime;
            public int pickupTime;
            public int loserPointReward;
            public int loserExperienceReward;
            public int loserCashReward;
            public int defaultInheritSpeedPercent;
            public int defaultInheritZSpeedPercent;
            public int warpCatchDelay;
            public int floorBounceVertSpeedPercent;
            public int floorBounceHorzSpeedPercent;
            public int ballAddedBong;
            public int showTimer;
            public int catchCountDelay;
            public int timer;
            public int timerOvertime;
            public int zProximityAdjust;
            public bool bounceForGoal;
            public string requestSound;
            public int ballWarpGroup;
            public bool scoreBubble;
            public bool mvpBubble;
            public int dropBallOnVehicleChange;
            public int invisibleFulfillTime;
            public bool countMultiPointGoals;
            public int relativeId;

            public Soccer(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["Soccer"];

                ballCount = Parser.GetInt("BallCount");
                minimumPlayers = Parser.GetInt("MinimumPlayers");
                sendTime = Parser.GetInt("SendTime");
                victoryPointReward = Parser.GetInt("VictoryPointReward");
                victoryExperienceReward = Parser.GetInt("VictoryExperienceReward");
                victoryCashReward = Parser.GetInt("VictoryCashReward");
                victoryGoals = Parser.GetInt("VictoryGoals");
                passDelay = Parser.GetInt("PassDelay");
                showRadar = Parser.GetBool("ShowRadar");
                defaultThrowTime = Parser.GetInt("DefaultThrowTime");
                defaultProximity = Parser.GetInt("DefaultProximity");
                defaultFriction = Parser.GetInt("DefaultFriction");
                defaultBallSpeed = Parser.GetInt("DefaultBallSpeed");
                gravityAcceleration = Parser.GetInt("GravityAcceleration");
                bounceHorzSpeedPercent = Parser.GetInt("BounceHorzSpeedPercent");
                ballGraphic = Parser.GetString("BallGraphic");
                trailGraphic = Parser.GetString("TrailGraphic");
                shadowGraphic = Parser.GetString("ShadowGraphic");
                catchSound = Parser.GetString("CatchSound");
                throwSound = Parser.GetString("ThrowSound");
                bounceSound = Parser.GetString("BounceSound");
                goalBong = Parser.GetInt("GoalBong");
                victoryBong = Parser.GetInt("VictoryBong");
                startDelay = Parser.GetInt("StartDelay");
                playersPerBall = Parser.GetInt("PlayersPerBall");
                killerCatchBall = Parser.GetInt("KillerCatchBall");
                deadBallTimer = Parser.GetInt("DeadBallTimer");
                ignoreWalls = Parser.GetInt("IgnoreWalls");
                invisibleTime = Parser.GetInt("InvisibleTime");
                pickupTime = Parser.GetInt("PickupTime");
                loserPointReward = Parser.GetInt("LoserPointReward");
                loserExperienceReward = Parser.GetInt("LoserExperienceReward");
                loserCashReward = Parser.GetInt("LoserCashReward");
                defaultInheritSpeedPercent = Parser.GetInt("DefaultInheritSpeedPercent");
                defaultInheritZSpeedPercent = Parser.GetInt("DefaultInheritZSpeedPercent");
                warpCatchDelay = Parser.GetInt("WarpCatchDelay");
                floorBounceVertSpeedPercent = Parser.GetInt("FloorBounceVertSpeedPercent");
                floorBounceHorzSpeedPercent = Parser.GetInt("FloorBounceHorzSpeedPercent");
                ballAddedBong = Parser.GetInt("BallAddedBong");
                showTimer = Parser.GetInt("ShowTimer");
                catchCountDelay = Parser.GetInt("CatchCountDelay");
                timer = Parser.GetInt("Timer");
                timerOvertime = Parser.GetInt("TimerOvertime");
                zProximityAdjust = Parser.GetInt("ZProximityAdjust");
                bounceForGoal = Parser.GetBool("BounceForGoal");
                requestSound = Parser.GetString("RequestSound");
                ballWarpGroup = Parser.GetInt("BallWarpGroup");
                scoreBubble = Parser.GetBool("ScoreBubble");
                mvpBubble = Parser.GetBool("MvpBubble");
                dropBallOnVehicleChange = Parser.GetInt("DropBallOnVehicleChange");
                invisibleFulfillTime = Parser.GetInt("InvisibleFulfillTime");
                countMultiPointGoals = Parser.GetBool("CountMultiPointGoals");
                relativeId = Parser.GetInt("RelativeId");

                //Load the blobs
                BlobsToLoad.Add(Parser.GetBlob(ballGraphic));
                BlobsToLoad.Add(Parser.GetBlob(shadowGraphic));
                BlobsToLoad.Add(Parser.GetBlob(trailGraphic));
                BlobsToLoad.Add(Parser.GetBlob(bounceSound));
                BlobsToLoad.Add(Parser.GetBlob(catchSound));
                BlobsToLoad.Add(Parser.GetBlob(throwSound));
                BlobsToLoad.Add(Parser.GetBlob(requestSound));
            }
        }
    }
}
