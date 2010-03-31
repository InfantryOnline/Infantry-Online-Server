using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class WarpGroup
        {
            public int initialEntry;
            public int lostVehicle;
            public int ballEntry;
            public int ballEntryRadius;

            public WarpGroup(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["WarpGroup"];

                initialEntry = Parser.GetInt("InitialEntry");
                lostVehicle = Parser.GetInt("LostVehicle");
                ballEntry = Parser.GetInt("BallEntry");
                ballEntryRadius = Parser.GetInt("BallEntryRadius");
            }
        }
    }
}
