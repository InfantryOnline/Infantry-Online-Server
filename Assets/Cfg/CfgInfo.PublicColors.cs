using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class PublicColors
        {
            public int count;
            public string unownedUniform;
            public int unownedHAdjust;
            public int unownedSAdjust;
            public int unownedVAdjust;

            public PublicColors(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["PublicColors"];

                count = Parser.GetInt("Count");
                unownedUniform = Parser.GetString("UnownedUniform");
                unownedHAdjust = Parser.GetInt("UnownedHAdjust");
                unownedSAdjust = Parser.GetInt("UnownedSAdjust");
                unownedVAdjust = Parser.GetInt("UnownedVAdjust");
            }
        }
    }
}
