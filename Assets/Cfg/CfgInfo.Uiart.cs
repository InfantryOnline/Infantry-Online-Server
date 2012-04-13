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

                //Parse the settings and load the blobs
                button1 = Parser.GetString("Button1");
                BlobsToLoad.Add(Parser.GetBlob(button1));
                button2 = Parser.GetString("Button2");
                BlobsToLoad.Add(Parser.GetBlob(button2));
                button3 = Parser.GetString("Button3");
                BlobsToLoad.Add(Parser.GetBlob(button3));
                button4 = Parser.GetString("Button4");
                BlobsToLoad.Add(Parser.GetBlob(button4));
                button5 = Parser.GetString("Button5");
                BlobsToLoad.Add(Parser.GetBlob(button5));
                menuTitle = Parser.GetString("MenuTitle");
                BlobsToLoad.Add(Parser.GetBlob(menuTitle));
                cashBack = Parser.GetString("CashBack");
                BlobsToLoad.Add(Parser.GetBlob(cashBack));
                header1 = Parser.GetString("Header1");
                BlobsToLoad.Add(Parser.GetBlob(header1));
                header2 = Parser.GetString("Header2");
                BlobsToLoad.Add(Parser.GetBlob(header2));
                physics = Parser.GetString("Physics");
                BlobsToLoad.Add(Parser.GetBlob(physics));
                vision = Parser.GetString("Vision");
                BlobsToLoad.Add(Parser.GetBlob(vision));
                cursors = Parser.GetString("Cursors");
                BlobsToLoad.Add(Parser.GetBlob(cursors));
                warp = Parser.GetString("Warp");
                BlobsToLoad.Add(Parser.GetBlob(warp));
                warFog = Parser.GetString("WarFog");
                BlobsToLoad.Add(Parser.GetBlob(warFog));
                mainBorder = Parser.GetString("MainBorder");
                BlobsToLoad.Add(Parser.GetBlob(mainBorder));
                scroll1 = Parser.GetString("Scroll1");
                BlobsToLoad.Add(Parser.GetBlob(scroll1));
                scroll2 = Parser.GetString("Scroll2");
                BlobsToLoad.Add(Parser.GetBlob(scroll2));
                portal = Parser.GetString("Portal");
                BlobsToLoad.Add(Parser.GetBlob(portal));
                guage = Parser.GetString("Guage");
                BlobsToLoad.Add(Parser.GetBlob(guage));
                messageBorder = Parser.GetString("MessageBorder");
                BlobsToLoad.Add(Parser.GetBlob(messageBorder));
                tinyNumbers = Parser.GetString("TinyNumbers");
                BlobsToLoad.Add(Parser.GetBlob(tinyNumbers));
                notepadBorder = Parser.GetString("NotepadBorder");
                BlobsToLoad.Add(Parser.GetBlob(notepadBorder));
                radarBorder = Parser.GetString("RadarBorder");
                BlobsToLoad.Add(Parser.GetBlob(radarBorder));
                logo = Parser.GetString("Logo");
                BlobsToLoad.Add(Parser.GetBlob(logo));
                guageBack = Parser.GetString("GuageBack");
                BlobsToLoad.Add(Parser.GetBlob(guageBack));
                notepadInteriorPlayer = Parser.GetString("NotepadInteriorPlayer");
                BlobsToLoad.Add(Parser.GetBlob(notepadInteriorPlayer));
                notepadInteriorInventory = Parser.GetString("NotepadInteriorInventory");
                BlobsToLoad.Add(Parser.GetBlob(notepadInteriorInventory));
            }
        }
    }
}
