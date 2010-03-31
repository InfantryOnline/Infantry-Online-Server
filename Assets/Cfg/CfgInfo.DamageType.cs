using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class DamageType
        {
            public string name0;
            public string name1;
            public string name2;
            public string name3;
            public string name4;
            public string name5;

            public DamageType(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["DamageType"];

                name0 = Parser.GetString("Name0");
                name1 = Parser.GetString("Name1");
                name2 = Parser.GetString("Name2");
                name3 = Parser.GetString("Name3");
                name4 = Parser.GetString("Name4");
                name5 = Parser.GetString("Name5");
            }
        }
    }
}
