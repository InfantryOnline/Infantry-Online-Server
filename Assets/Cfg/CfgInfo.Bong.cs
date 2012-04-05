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
            public List<string> bongs = new List<string>();

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

                for (int i = 1; i <= 30; i++)
                {
                    bongs.Add(Parser.GetString(string.Format("Bong{0}", i)));
                }
            }
        }
    }
}
