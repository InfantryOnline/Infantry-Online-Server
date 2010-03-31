using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class HelpMenu
        {
            public string link0;
            public string link1;
            public string link2;
            public string link3;
            public string link4;
            public string link5;
            public string link6;
            public string link7;
            public string link8;
            public string link9;
            public string link10;
            public string link11;
            public string link12;
            public string link13;
            public string link14;
            public string link15;
            public string link16;
            public string link17;
            public string link18;
            public string link19;
            public string link20;
            public string link21;
            public string link22;
            public string link23;
            public string link24;
            public string link25;
            public string link26;
            public string link27;
            public string link28;
            public string link29;

            public HelpMenu(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["HelpMenu"];

                link0 = Parser.GetString("Link0");
                link1 = Parser.GetString("Link1");
                link2 = Parser.GetString("Link2");
                link3 = Parser.GetString("Link3");
                link4 = Parser.GetString("Link4");
                link5 = Parser.GetString("Link5");
                link6 = Parser.GetString("Link6");
                link7 = Parser.GetString("Link7");
                link8 = Parser.GetString("Link8");
                link9 = Parser.GetString("Link9");
                link10 = Parser.GetString("Link10");
                link11 = Parser.GetString("Link11");
                link12 = Parser.GetString("Link12");
                link13 = Parser.GetString("Link13");
                link14 = Parser.GetString("Link14");
                link15 = Parser.GetString("Link15");
                link16 = Parser.GetString("Link16");
                link17 = Parser.GetString("Link17");
                link18 = Parser.GetString("Link18");
                link19 = Parser.GetString("Link19");
                link20 = Parser.GetString("Link20");
                link21 = Parser.GetString("Link21");
                link22 = Parser.GetString("Link22");
                link23 = Parser.GetString("Link23");
                link24 = Parser.GetString("Link24");
                link25 = Parser.GetString("Link25");
                link26 = Parser.GetString("Link26");
                link27 = Parser.GetString("Link27");
                link28 = Parser.GetString("Link28");
                link29 = Parser.GetString("Link29");
            }
        }
    }
}
