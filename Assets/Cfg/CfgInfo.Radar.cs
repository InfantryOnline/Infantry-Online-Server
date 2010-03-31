using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class Radar
        {
            public int colorSoccerBall;
            public int colorSelf;
            public int colorFriendly;
            public int colorEnemy;
            public int colorFlag;
            public int fontColorSelf;
            public int fontColorFriendly;
            public int fontColorEnemy;

            public Radar(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["Radar"];

                colorSoccerBall = Parser.GetInt("ColorSoccerBall");
                colorSelf = Parser.GetInt("ColorSelf");
                colorFriendly = Parser.GetInt("ColorFriendly");
                colorEnemy = Parser.GetInt("ColorEnemy");
                colorFlag = Parser.GetInt("ColorFlag");
                fontColorSelf = Parser.GetInt("FontColorSelf");
                fontColorFriendly = Parser.GetInt("FontColorFriendly");
                fontColorEnemy = Parser.GetInt("FontColorEnemy");
            }
        }
    }
}
