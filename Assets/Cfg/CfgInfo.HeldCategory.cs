using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class HeldCategory
        {
            public List<int> limit = new List<int>();
            public List<int> extendedLimit = new List<int>();

            public HeldCategory(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["HeldCategory"];

                for (int i = 1; i <= 30; i++)
                {
                    limit.Add(Parser.GetInt(string.Format("Limit{0}", i)));
                    extendedLimit.Add(Parser.GetInt(string.Format("ExtendedLimit{0}", i)));
                }
            }
        }
    }
}
