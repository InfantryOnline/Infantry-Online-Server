using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class TeamDefault
        {
            public List<int> inventory = new List<int>();
            public List<int> quantity = new List<int>();

            public TeamDefault(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["TeamDefault"];

                for (int i = 0; i <= 15; i++)
                {
                    inventory.Add(Parser.GetInt(string.Format("Inventory{0}", i)));
                    quantity.Add(Parser.GetInt(string.Format("Quantity{0}", i)));
                }
            }
        }
    }
}
