using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class Bounty
        {
            public int start;
            public int percentToKillerBounty;
            public int percentToAssistBounty;
            public int fixedToKillerBounty;

            public Bounty(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["Bounty"];

                start = Parser.GetInt("Start");
                percentToKillerBounty = Parser.GetInt("PercentToKillerBounty");
                percentToAssistBounty = Parser.GetInt("PercentToAssistBounty");
                fixedToKillerBounty = Parser.GetInt("FixedToKillerBounty");
            }
        }
    }
}
