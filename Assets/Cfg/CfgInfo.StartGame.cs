using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class StartGame
        {
            public bool prizeReset;
            public bool vehicleReset;
            public bool initialHides;
            public bool resetInventory;
            public bool resetCharacter;
            public bool clearProjectiles;

            public StartGame(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["StartGame"];

                prizeReset = Parser.GetBool("PrizeReset");
                vehicleReset = Parser.GetBool("VehicleReset");
                initialHides = Parser.GetBool("InitialHides");
                resetInventory = Parser.GetBool("ResetInventory");
                resetCharacter = Parser.GetBool("ResetCharacter");
                clearProjectiles = Parser.GetBool("ClearProjectiles");
            }
        }
    }
}
