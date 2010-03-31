using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class Bong
        {
            public int publicLow;
            public int publicHigh;
            public int privateLow;
            public int privateHigh;
            public int teamLow;
            public int teamHigh;
            public int chatLow;
            public int chatHigh;
            public int squadLow;
            public int squadHigh;
            public string bong1;
            public string bong2;
            public string bong3;
            public string bong4;
            public string bong5;
            public string bong6;
            public string bong7;
            public string bong8;
            public string bong9;
            public string bong10;
            public string bong11;
            public string bong12;
            public string bong13;
            public string bong14;
            public string bong15;
            public string bong16;
            public string bong17;
            public string bong18;
            public string bong19;
            public string bong20;
            public string bong21;
            public string bong22;
            public string bong23;
            public string bong24;
            public string bong25;
            public string bong26;
            public string bong27;
            public string bong28;
            public string bong29;
            public string bong30;

            public Bong(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["Bong"];

                publicLow = Parser.GetInt("PublicLow");
                publicHigh = Parser.GetInt("PublicHigh");
                privateLow = Parser.GetInt("PrivateLow");
                privateHigh = Parser.GetInt("PrivateHigh");
                teamLow = Parser.GetInt("TeamLow");
                teamHigh = Parser.GetInt("TeamHigh");
                chatLow = Parser.GetInt("ChatLow");
                chatHigh = Parser.GetInt("ChatHigh");
                squadLow = Parser.GetInt("SquadLow");
                squadHigh = Parser.GetInt("SquadHigh");
                bong1 = Parser.GetString("Bong1");
                bong2 = Parser.GetString("Bong2");
                bong3 = Parser.GetString("Bong3");
                bong4 = Parser.GetString("Bong4");
                bong5 = Parser.GetString("Bong5");
                bong6 = Parser.GetString("Bong6");
                bong7 = Parser.GetString("Bong7");
                bong8 = Parser.GetString("Bong8");
                bong9 = Parser.GetString("Bong9");
                bong10 = Parser.GetString("Bong10");
                bong11 = Parser.GetString("Bong11");
                bong12 = Parser.GetString("Bong12");
                bong13 = Parser.GetString("Bong13");
                bong14 = Parser.GetString("Bong14");
                bong15 = Parser.GetString("Bong15");
                bong16 = Parser.GetString("Bong16");
                bong17 = Parser.GetString("Bong17");
                bong18 = Parser.GetString("Bong18");
                bong19 = Parser.GetString("Bong19");
                bong20 = Parser.GetString("Bong20");
                bong21 = Parser.GetString("Bong21");
                bong22 = Parser.GetString("Bong22");
                bong23 = Parser.GetString("Bong23");
                bong24 = Parser.GetString("Bong24");
                bong25 = Parser.GetString("Bong25");
                bong26 = Parser.GetString("Bong26");
                bong27 = Parser.GetString("Bong27");
                bong28 = Parser.GetString("Bong28");
                bong29 = Parser.GetString("Bong29");
                bong30 = Parser.GetString("Bong30");
            }
        }
    }
}
