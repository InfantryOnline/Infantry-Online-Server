using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class Terrain
        {
            public string message;
            public bool stripShadows;
            public bool safety;
            public bool storeEnabled;
            public bool skillEnabled;
            public bool trickleKill;
            public bool teamChangeEnabled;
            public int energyRate;
            public int healthRate;
            public int repairRate;
            public int prizeEnableMode;
            public bool repelVehicle;
            public bool repelWeapons;
            public int prizeExpire;
            public int percentOfCashReward;
            public int percentOfExperienceReward;
            public int percentOfPointsReward;
            public int maxTimeAllowed;
            public int bountyAutoMax;
            public int bountyAutoRate;
            public int goalFrequency;
            public bool soccerEnabled;
            public int fontColor;
            public bool starfield;
            public int goalPoints;
            public string eventString;
            public bool deleteLiveWeapons;
            public int prizeBountyMultiplier;
            public int flagTimerSpeed;
            public bool allowChangeArena;
            public bool allowGoToSpec;
            public int quitDelaySecs;
            public int goToSpecDelaySecs;
            public int changeArenaDelaySecs;
            public bool allowQuitting;

            public Terrain(ref Dictionary<string, Dictionary<string, string>> stringTree, int i)
            {
                Parser.values = stringTree["Terrain" + i];

                message = Parser.GetString("Message");
                stripShadows = Parser.GetBool("StripShadows");
                safety = Parser.GetBool("Safety");
                storeEnabled = Parser.GetBool("StoreEnabled");
                skillEnabled = Parser.GetBool("SkillEnabled");
                trickleKill = Parser.GetBool("TrickleKill");
                teamChangeEnabled = Parser.GetBool("TeamChangeEnabled");
                energyRate = Parser.GetInt("EnergyRate");
                healthRate = Parser.GetInt("HealthRate");
                repairRate = Parser.GetInt("RepairRate");
                prizeEnableMode = Parser.GetInt("PrizeEnableMode");
                repelVehicle = Parser.GetBool("RepelVehicle");
                repelWeapons = Parser.GetBool("RepelWeapons");
                prizeExpire = Parser.GetInt("PrizeExpire");
                percentOfCashReward = Parser.GetInt("PercentOfCashReward");
                percentOfExperienceReward = Parser.GetInt("PercentOfExperienceReward");
                percentOfPointsReward = Parser.GetInt("PercentOfPointsReward");
                maxTimeAllowed = Parser.GetInt("MaxTimeAllowed");
                bountyAutoMax = Parser.GetInt("BountyAutoMax");
                bountyAutoRate = Parser.GetInt("BountyAutoRate");
                goalFrequency = Parser.GetInt("GoalFrequency");
                soccerEnabled = Parser.GetBool("SoccerEnabled");
                fontColor = Parser.GetInt("FontColor");
                starfield = Parser.GetBool("Starfield");
                goalPoints = Parser.GetInt("GoalPoints");
                eventString = Parser.GetString("EventString");
                deleteLiveWeapons = Parser.GetBool("DeleteLiveWeapons");
                prizeBountyMultiplier = Parser.GetInt("PrizeBountyMultiplier");
                flagTimerSpeed = Parser.GetInt("FlagTimerSpeed");
                allowChangeArena = Parser.GetBool("AllowChangeArena");
                allowGoToSpec = Parser.GetBool("AllowGoToSpec");
                quitDelaySecs = Parser.GetInt("QuitDelaySecs");
                goToSpecDelaySecs = Parser.GetInt("GoToSpecDelaySecs");
                changeArenaDelaySecs = Parser.GetInt("ChangeArenaDelaySecs");
                allowQuitting = Parser.GetBool("AllowQuitting");
            }
        }
    }
}
