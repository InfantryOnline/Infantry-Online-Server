using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class Uiart
        {
            public string button1;
            public string button2;
            public string button3;
            public string button4;
            public string button5;
            public string menuTitle;
            public string cashBack;
            public string header1;
            public string header2;
            public string physics;
            public string vision;
            public string cursors;
            public string warp;
            public string warFog;
            public string mainBorder;
            public string scroll1;
            public string scroll2;
            public string portal;
            public string guage;
            public string messageBorder;
            public string tinyNumbers;
            public string notepadBorder;
            public string radarBorder;
            public string logo;
            public string guageBack;
            public string notepadInteriorPlayer;
            public string notepadInteriorInventory;

            public Uiart(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["Uiart"];

                button1 = Parser.GetString("Button1");
                button2 = Parser.GetString("Button2");
                button3 = Parser.GetString("Button3");
                button4 = Parser.GetString("Button4");
                button5 = Parser.GetString("Button5");
                menuTitle = Parser.GetString("MenuTitle");
                cashBack = Parser.GetString("CashBack");
                header1 = Parser.GetString("Header1");
                header2 = Parser.GetString("Header2");
                physics = Parser.GetString("Physics");
                vision = Parser.GetString("Vision");
                cursors = Parser.GetString("Cursors");
                warp = Parser.GetString("Warp");
                warFog = Parser.GetString("WarFog");
                mainBorder = Parser.GetString("MainBorder");
                scroll1 = Parser.GetString("Scroll1");
                scroll2 = Parser.GetString("Scroll2");
                portal = Parser.GetString("Portal");
                guage = Parser.GetString("Guage");
                messageBorder = Parser.GetString("MessageBorder");
                tinyNumbers = Parser.GetString("TinyNumbers");
                notepadBorder = Parser.GetString("NotepadBorder");
                radarBorder = Parser.GetString("RadarBorder");
                logo = Parser.GetString("Logo");
                guageBack = Parser.GetString("GuageBack");
                notepadInteriorPlayer = Parser.GetString("NotepadInteriorPlayer");
                notepadInteriorInventory = Parser.GetString("NotepadInteriorInventory");
            }
        }
    }
}
