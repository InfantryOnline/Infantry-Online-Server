using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class Bubble
        {
            public int sysopTimer;
            public int size0;
            public int size1;
            public int size2;
            public int size3;
            public int font0;
            public int fontColor0;
            public int font1;
            public int fontColor1;
            public int font2;
            public int fontColor2;
            public int font3;
            public int fontColor3;

            public Bubble(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["Bubble"];

                sysopTimer = Parser.GetInt("SysopTimer");
                size0 = Parser.GetInt("Size0");
                size1 = Parser.GetInt("Size1");
                size2 = Parser.GetInt("Size2");
                size3 = Parser.GetInt("Size3");
                font0 = Parser.GetInt("Font0");
                fontColor0 = Parser.GetInt("FontColor0");
                font1 = Parser.GetInt("Font1");
                fontColor1 = Parser.GetInt("FontColor1");
                font2 = Parser.GetInt("Font2");
                fontColor2 = Parser.GetInt("FontColor2");
                font3 = Parser.GetInt("Font3");
                fontColor3 = Parser.GetInt("FontColor3");
            }
        }
    }
}
