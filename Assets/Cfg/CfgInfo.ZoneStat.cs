using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class ZoneStat
        {
            public string name0;
            public string name1;
            public string name2;
            public string name3;
            public string name4;
            public string name5;
            public string name6;
            public string name7;
            public string name8;
            public string name9;
            public string name10;
            public string name11;

            public ZoneStat(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["ZoneStat"];

                name0 = Parser.GetString("Name0");
                name1 = Parser.GetString("Name1");
                name2 = Parser.GetString("Name2");
                name3 = Parser.GetString("Name3");
                name4 = Parser.GetString("Name4");
                name5 = Parser.GetString("Name5");
                name6 = Parser.GetString("Name6");
                name7 = Parser.GetString("Name7");
                name8 = Parser.GetString("Name8");
                name9 = Parser.GetString("Name9");
                name10 = Parser.GetString("Name10");
                name11 = Parser.GetString("Name11");
            }
        }
    }
}
