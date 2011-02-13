using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class Arena
        {
            public int frequencyMax;
            public int maxPerFrequency;
            public int desiredFrequencies;
            public bool allowPrivateFrequencies;
            public int teamVisionDistance;
            public ushort spectatorVehicleId;
            public bool suicidesAllowed;
            public int itemPickupDistance;
            public int vehicleGetInDistance;
            public bool spectatorStore;
            public bool spectatorSkills;
            public bool startInSpectator;
            public string exitSpectatorLogic;
            public string exitSpectatorMessage;
            public int prizeKill;
            public bool forceEvenTeams;
            public int spectatorVisualFrequency;
            public int turretCashSharePercent;
            public int turretExperienceSharePercent;
            public int turretPointsSharePercent;
            public int inactivityTimeout;
            public bool privateSpawnsKeepScore;
            public bool forceSwitchEvenTeams;
            public int minimumKeepScorePublic;
            public int minimumKeepScorePrivate;
            public bool allowManualTeamSwitch;
            public int maxPlayers;
            public int playingMax;
            public int playingDesired;
            public int scrambleTeams;
            public bool allowInfo;
            public bool showBounty;
            public bool specsSeeEnergy;
            public bool friendlySeeEnergy;
            public bool enemySeeEnergy;
            public int pointBlankDamageMultiplier;
            public int spectatorEnergyAmount;
            public bool teamKillsAllowed;
            public int doubleWarpDelay;
            public int bannerRequestTime;
            public string bannerLogic;
            public int startingSoonTime;
            public bool skillScreenExists;
            public bool storeScreenExists;
            public bool deleteWeaponsOnClassChange;
            public bool exitSpectatorToggleSkills;
            public bool skillScreenEnergyNeeded;
            public bool deathForceUtilityOff;
            public bool allowSkillPurchaseInVehicle;
            public int doubleKillDelay;
            public int pruneDropRadius;
            public bool queueEntryRequests;
            public bool queueEntryBubble;
            public bool queueEntryDeathCount;
            public int maxFrameStall;
            public int teamSwitchMinEnergy;
            public bool persistantArena;
            public int vehicleExitWarpRadius;

            public Arena(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["Arena"];

                frequencyMax = Parser.GetInt("FrequencyMax");
                maxPerFrequency = Parser.GetInt("MaxPerFrequency");
                desiredFrequencies = Parser.GetInt("DesiredFrequencies");
                allowPrivateFrequencies = Parser.GetBool("AllowPrivateFrequencies");
                teamVisionDistance = Parser.GetInt("TeamVisionDistance");
                spectatorVehicleId = (ushort)Parser.GetInt("SpectatorVehicleId");
                suicidesAllowed = Parser.GetBool("SuicidesAllowed");
                itemPickupDistance = Parser.GetInt("ItemPickupDistance");
                vehicleGetInDistance = Parser.GetInt("VehicleGetInDistance");
                spectatorStore = Parser.GetBool("SpectatorStore");
                spectatorSkills = Parser.GetBool("SpectatorSkills");
                startInSpectator = Parser.GetBool("StartInSpectator");
                exitSpectatorLogic = Parser.GetString("ExitSpectatorLogic");
                exitSpectatorMessage = Parser.GetString("ExitSpectatorMessage");
                prizeKill = Parser.GetInt("PrizeKill");
                forceEvenTeams = Parser.GetBool("ForceEvenTeams");
                spectatorVisualFrequency = Parser.GetInt("SpectatorVisualFrequency");
                turretCashSharePercent = Parser.GetInt("TurretCashSharePercent");
                turretExperienceSharePercent = Parser.GetInt("TurretExperienceSharePercent");
                turretPointsSharePercent = Parser.GetInt("TurretPointsSharePercent");
                inactivityTimeout = Parser.GetInt("InactivityTimeout");
                privateSpawnsKeepScore = Parser.GetBool("PrivateSpawnsKeepScore");
                forceSwitchEvenTeams = Parser.GetBool("ForceSwitchEvenTeams");
                minimumKeepScorePublic = Parser.GetInt("MinimumKeepScorePublic");
                minimumKeepScorePrivate = Parser.GetInt("MinimumKeepScorePrivate");
                allowManualTeamSwitch = Parser.GetBool("AllowManualTeamSwitch");
                maxPlayers = Parser.GetInt("MaxPlayers");
                playingMax = Parser.GetInt("PlayingMax");
                playingDesired = Parser.GetInt("PlayingDesired");
                scrambleTeams = Parser.GetInt("ScrambleTeams");
                allowInfo = Parser.GetBool("AllowInfo");
                showBounty = Parser.GetBool("ShowBounty");
                specsSeeEnergy = Parser.GetBool("SpecsSeeEnergy");
                friendlySeeEnergy = Parser.GetBool("FriendlySeeEnergy");
                enemySeeEnergy = Parser.GetBool("EnemySeeEnergy");
                pointBlankDamageMultiplier = Parser.GetInt("PointBlankDamageMultiplier");
                spectatorEnergyAmount = Parser.GetInt("SpectatorEnergyAmount");
                teamKillsAllowed = Parser.GetBool("TeamKillsAllowed");
                doubleWarpDelay = Parser.GetInt("DoubleWarpDelay");
                bannerRequestTime = Parser.GetInt("BannerRequestTime");
                bannerLogic = Parser.GetString("BannerLogic");
                startingSoonTime = Parser.GetInt("StartingSoonTime");
                skillScreenExists = Parser.GetBool("SkillScreenExists");
                storeScreenExists = Parser.GetBool("StoreScreenExists");
                deleteWeaponsOnClassChange = Parser.GetBool("DeleteWeaponsOnClassChange");
                exitSpectatorToggleSkills = Parser.GetBool("ExitSpectatorToggleSkills");
                skillScreenEnergyNeeded = Parser.GetBool("SkillScreenEnergyNeeded");
                deathForceUtilityOff = Parser.GetBool("DeathForceUtilityOff");
                allowSkillPurchaseInVehicle = Parser.GetBool("AllowSkillPurchaseInVehicle");
                doubleKillDelay = Parser.GetInt("DoubleKillDelay");
                pruneDropRadius = Parser.GetInt("PruneDropRadius");
                queueEntryRequests = Parser.GetBool("QueueEntryRequests");
                queueEntryBubble = Parser.GetBool("QueueEntryBubble");
                queueEntryDeathCount = Parser.GetBool("QueueEntryDeathCount");
                maxFrameStall = Parser.GetInt("MaxFrameStall");
                teamSwitchMinEnergy = Parser.GetInt("TeamSwitchMinEnergy");
                persistantArena = Parser.GetBool("PersistantArena");
                vehicleExitWarpRadius = Parser.GetInt("VehicleExitWarpRadius");
            }
        }
    }
}
