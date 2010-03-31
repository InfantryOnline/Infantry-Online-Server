using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class PublicProfile
        {
            public int defaultVItemId;

            public PublicProfile(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["PublicProfile"];

                defaultVItemId = Parser.GetInt("DefaultVItemId");
            }
        }
    }
}
