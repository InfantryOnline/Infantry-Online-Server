using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class Los
        {
            public int defaultDistance;
            public int defaultAngle;
            public int defaultXray;
            public bool teamSharing;

            public Los(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["LOS"];

                defaultDistance = Parser.GetInt("DefaultDistance");
                defaultAngle = Parser.GetInt("DefaultAngle");
                defaultXray = Parser.GetInt("DefaultXray");
                teamSharing = Parser.GetBool("TeamSharing");
            }
        }
    }
}
