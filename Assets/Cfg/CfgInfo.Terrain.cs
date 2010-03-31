using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class Terrain
        {
            public string message;
            public int stripShadows;
            public int safety;
            public int storeEnabled;
            public int skillEnabled;
            public int trickleKill;
            public int teamChangeEnabled;
            public int energyRate;
            public int healthRate;
            public int repairRate;
            public int prizeEnableMode;
            public int repelVehicle;
            public int repelWeapons;
            public int prizeExpire;
            public int percentOfCashReward;
            public int percentOfExperienceReward;
            public int percentOfPointsReward;
            public int maxTimeAllowed;
            public int bountyAutoMax;
            public int bountyAutoRate;
            public int goalFrequency;
            public int soccerEnabled;
            public int fontColor;
            public int starfield;
            public int goalPoints;
            public string eventString;
            public int deleteLiveWeapons;
            public int prizeBountyMultiplier;
            public int flagTimerSpeed;
            public int allowChangeArena;
            public int allowGoToSpec;
            public int quitDelaySecs;
            public int goToSpecDelaySecs;
            public int changeArenaDelaySecs;
            public int allowQuitting;

            public Terrain(ref Dictionary<string, Dictionary<string, string>> stringTree, int i)
            {
                Parser.values = stringTree["Terrain" + i];

                message = Parser.GetString("Message");
                stripShadows = Parser.GetInt("StripShadows");
                safety = Parser.GetInt("Safety");
                storeEnabled = Parser.GetInt("StoreEnabled");
                skillEnabled = Parser.GetInt("SkillEnabled");
                trickleKill = Parser.GetInt("TrickleKill");
                teamChangeEnabled = Parser.GetInt("TeamChangeEnabled");
                energyRate = Parser.GetInt("EnergyRate");
                healthRate = Parser.GetInt("HealthRate");
                repairRate = Parser.GetInt("RepairRate");
                prizeEnableMode = Parser.GetInt("PrizeEnableMode");
                repelVehicle = Parser.GetInt("RepelVehicle");
                repelWeapons = Parser.GetInt("RepelWeapons");
                prizeExpire = Parser.GetInt("PrizeExpire");
                percentOfCashReward = Parser.GetInt("PercentOfCashReward");
                percentOfExperienceReward = Parser.GetInt("PercentOfExperienceReward");
                percentOfPointsReward = Parser.GetInt("PercentOfPointsReward");
                maxTimeAllowed = Parser.GetInt("MaxTimeAllowed");
                bountyAutoMax = Parser.GetInt("BountyAutoMax");
                bountyAutoRate = Parser.GetInt("BountyAutoRate");
                goalFrequency = Parser.GetInt("GoalFrequency");
                soccerEnabled = Parser.GetInt("SoccerEnabled");
                fontColor = Parser.GetInt("FontColor");
                starfield = Parser.GetInt("Starfield");
                goalPoints = Parser.GetInt("GoalPoints");
                eventString = Parser.GetString("EventString");
                deleteLiveWeapons = Parser.GetInt("DeleteLiveWeapons");
                prizeBountyMultiplier = Parser.GetInt("PrizeBountyMultiplier");
                flagTimerSpeed = Parser.GetInt("FlagTimerSpeed");
                allowChangeArena = Parser.GetInt("AllowChangeArena");
                allowGoToSpec = Parser.GetInt("AllowGoToSpec");
                quitDelaySecs = Parser.GetInt("QuitDelaySecs");
                goToSpecDelaySecs = Parser.GetInt("GoToSpecDelaySecs");
                changeArenaDelaySecs = Parser.GetInt("ChangeArenaDelaySecs");
                allowQuitting = Parser.GetInt("AllowQuitting");
            }
        }
    }
}
