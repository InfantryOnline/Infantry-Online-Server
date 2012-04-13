using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class QuickSkill
        {
            public string backGraphic;
            public int listRectLeft;
            public int listRectRight;
            public int listRectTop;
            public int listRectBottom;
            public int textRectLeft;
            public int textRectRight;
            public int textRectTop;
            public int textRectBottom;
            public int thumbX;
            public int thumbY;
            public string selectGraphic;
            public string cancelGraphic;
            public int selectRectLeft;
            public int selectRectTop;
            public int selectRectRight;
            public int selectRectBottom;
            public int cancelRectLeft;
            public int cancelRectTop;
            public int cancelRectRight;
            public int cancelRectBottom;
            public int selectX;
            public int selectY;
            public int cancelX;
            public int cancelY;
            public string selectColor;
            public int listCenter;
            public int autoEnterGame;
            public int listFont;
            public int listFontColor;
            public int listSelectFont;
            public int listSelectFontColor;
            public int textFont;
            public int textFontColor;

            public QuickSkill(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["QuickSkill"];

                backGraphic = Parser.GetString("BackGraphic");
                listRectLeft = Parser.GetInt("ListRectLeft");
                listRectRight = Parser.GetInt("ListRectRight");
                listRectTop = Parser.GetInt("ListRectTop");
                listRectBottom = Parser.GetInt("ListRectBottom");
                textRectLeft = Parser.GetInt("TextRectLeft");
                textRectRight = Parser.GetInt("TextRectRight");
                textRectTop = Parser.GetInt("TextRectTop");
                textRectBottom = Parser.GetInt("TextRectBottom");
                thumbX = Parser.GetInt("ThumbX");
                thumbY = Parser.GetInt("ThumbY");
                selectGraphic = Parser.GetString("SelectGraphic");
                cancelGraphic = Parser.GetString("CancelGraphic");
                selectRectLeft = Parser.GetInt("SelectRectLeft");
                selectRectTop = Parser.GetInt("SelectRectTop");
                selectRectRight = Parser.GetInt("SelectRectRight");
                selectRectBottom = Parser.GetInt("SelectRectBottom");
                cancelRectLeft = Parser.GetInt("CancelRectLeft");
                cancelRectTop = Parser.GetInt("CancelRectTop");
                cancelRectRight = Parser.GetInt("CancelRectRight");
                cancelRectBottom = Parser.GetInt("CancelRectBottom");
                selectX = Parser.GetInt("SelectX");
                selectY = Parser.GetInt("SelectY");
                cancelX = Parser.GetInt("CancelX");
                cancelY = Parser.GetInt("CancelY");
                selectColor = Parser.GetString("SelectColor");
                listCenter = Parser.GetInt("ListCenter");
                autoEnterGame = Parser.GetInt("AutoEnterGame");
                listFont = Parser.GetInt("ListFont");
                listFontColor = Parser.GetInt("ListFontColor");
                listSelectFont = Parser.GetInt("ListSelectFont");
                listSelectFontColor = Parser.GetInt("ListSelectFontColor");
                textFont = Parser.GetInt("TextFont");
                textFontColor = Parser.GetInt("TextFontColor");

                //Load the blobs
                BlobsToLoad.Add(Parser.GetBlob(backGraphic));
                BlobsToLoad.Add(Parser.GetBlob(cancelGraphic));
                BlobsToLoad.Add(Parser.GetBlob(selectGraphic));
            }
        }
    }
}
