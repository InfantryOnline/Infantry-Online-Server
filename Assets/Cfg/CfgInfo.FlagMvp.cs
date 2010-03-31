using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class FlagMvp
        {
            public int kills;
            public int deaths;
            public int vehicleKills;
            public int vehicleDeaths;
            public int captures;
            public int carryTimeFactor;
            public int carryTimeCombinedFactor;
            public int carryingKills;
            public int carrierKills;
            public int teamOwnageFactor;
            public int teamOwnageCombinedFactor;
            public int killedWhileCarrying;

            public FlagMvp(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["FlagMvp"];

                kills = Parser.GetInt("Kills");
                deaths = Parser.GetInt("Deaths");
                vehicleKills = Parser.GetInt("VehicleKills");
                vehicleDeaths = Parser.GetInt("VehicleDeaths");
                captures = Parser.GetInt("Captures");
                carryTimeFactor = Parser.GetInt("CarryTimeFactor");
                carryTimeCombinedFactor = Parser.GetInt("CarryTimeCombinedFactor");
                carryingKills = Parser.GetInt("CarryingKills");
                carrierKills = Parser.GetInt("CarrierKills");
                teamOwnageFactor = Parser.GetInt("TeamOwnageFactor");
                teamOwnageCombinedFactor = Parser.GetInt("TeamOwnageCombinedFactor");
                killedWhileCarrying = Parser.GetInt("KilledWhileCarrying");
            }
        }
    }
}
