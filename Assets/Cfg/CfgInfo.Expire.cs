using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class Expire
        {
            public bool deathCount;
            public bool allowLateJoins;

            public Expire(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["Expire"];

                deathCount = Parser.GetBool("DeathCount");
                allowLateJoins = Parser.GetBool("AllowLateJoins");
            }
        }
    }
}
