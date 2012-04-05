using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class Rank
        {
            public struct RankList
            {
                public string name;
                public int points;
                public RankList(string rankName, int rankPoints)
                {
                    name = rankName;
                    points = rankPoints;
                }
            }

            public bool enabled;
            public int bong;
            public string label;
            public List<RankList> ranks = new List<RankList>();

            public Rank(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["Rank"];

                enabled = Parser.GetBool("Enabled");
                bong = Parser.GetInt("Bong");
                label = Parser.GetString("Label");
                for (int i = 0; i <= 69; i++)
                {
                    if(Parser.GetString(string.Format("Name{0}", i)) != "")
                        //Add it to the list if it's not blank
                        ranks.Add(new RankList(Parser.GetString(string.Format("Name{0}", i)),
                            Parser.GetInt(string.Format("Points{0}", i))));
                }
            }

			public string getRank(int experience)
			{
                //Count backwards and find the first rank they qualify for
                for (int i = ranks.Count - 1; i >= 0; i--)
                {
                    if (experience >= ranks[i].points)
                        return ranks[i].name;
                }
                //They don't have a rank?
                return "";
			}
        }
    }
}
