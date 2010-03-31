using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class Soul
        {
            public int combatAwarenessTime;
            public int energyDefaultMax;
            public int energyDefaultRate;
            public int utilitySlots;
            public int energyShieldMode;
            public int weightNormalMax;
            public int weightStopMax;
            public int energyDefaultStart;
            public int utilitySlots1_99;
            public int utilitySlots100_199;
            public int utilitySlots200_299;
            public int utilitySlots300_399;
            public int utilitySlots400_499;
            public int weightPrunePercentage;

            public Soul(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["Soul"];

                combatAwarenessTime = Parser.GetInt("CombatAwarenessTime");
                energyDefaultMax = Parser.GetInt("EnergyDefaultMax");
                energyDefaultRate = Parser.GetInt("EnergyDefaultRate");
                utilitySlots = Parser.GetInt("UtilitySlots");
                energyShieldMode = Parser.GetInt("EnergyShieldMode");
                weightNormalMax = Parser.GetInt("WeightNormalMax");
                weightStopMax = Parser.GetInt("WeightStopMax");
                energyDefaultStart = Parser.GetInt("EnergyDefaultStart");
                utilitySlots1_99 = Parser.GetInt("UtilitySlots1-99");
                utilitySlots100_199 = Parser.GetInt("UtilitySlots100-199");
                utilitySlots200_299 = Parser.GetInt("UtilitySlots200-299");
                utilitySlots300_399 = Parser.GetInt("UtilitySlots300-399");
                utilitySlots400_499 = Parser.GetInt("UtilitySlots400-499");
                weightPrunePercentage = Parser.GetInt("WeightPrunePercentage");
            }
        }
    }
}
