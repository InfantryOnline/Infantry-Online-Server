using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class UiartFont
        {
            public string font0;
            public string font1;
            public string font2;
            public string font3;
            public string font4;
            public string font5;
            public string font6;
            public string font7;
            public string font8;
            public string font9;
            public string font10;
            public string font11;
            public string font12;
            public string font13;
            public string font14;

            public UiartFont(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["UiartFont"];

                font0 = Parser.GetString("Font0");
                font1 = Parser.GetString("Font1");
                font2 = Parser.GetString("Font2");
                font3 = Parser.GetString("Font3");
                font4 = Parser.GetString("Font4");
                font5 = Parser.GetString("Font5");
                font6 = Parser.GetString("Font6");
                font7 = Parser.GetString("Font7");
                font8 = Parser.GetString("Font8");
                font9 = Parser.GetString("Font9");
                font10 = Parser.GetString("Font10");
                font11 = Parser.GetString("Font11");
                font12 = Parser.GetString("Font12");
                font13 = Parser.GetString("Font13");
                font14 = Parser.GetString("Font14");
            }
        }
    }
}
