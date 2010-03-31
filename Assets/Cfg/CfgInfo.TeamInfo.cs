using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class TeamInfo
        {
            public string name;
            public int defaultVehicle;
            public string colors;
            public int relativeVehicle;
            public string radarColor;
            public int nameFontColor;
            public int maxPlayers;
            public string eventString;
            public int hAdjust;
            public int sAdjust;
            public int vAdjust;
            public int disallowWinFlagGame;
            public int rtsState0;
            public int rtsState1;
            public int rtsState2;
            public int rtsState3;
            public int rtsState4;
            public int rtsState5;
            public int rtsState6;
            public int rtsState7;
            public string teamEventStartGame;

            public TeamInfo(ref Dictionary<string, Dictionary<string, string>> stringTree, int i)
            {
                Parser.values = stringTree["TeamInfo" + i];

                name = Parser.GetString("Name");
                defaultVehicle = Parser.GetInt("DefaultVehicle");
                colors = Parser.GetString("Colors");
                relativeVehicle = Parser.GetInt("RelativeVehicle");
                radarColor = Parser.GetString("RadarColor");
                nameFontColor = Parser.GetInt("NameFontColor");
                maxPlayers = Parser.GetInt("MaxPlayers");
                eventString = Parser.GetString("EventString");
                hAdjust = Parser.GetInt("hAdjust");
                sAdjust = Parser.GetInt("sAdjust");
                vAdjust = Parser.GetInt("vAdjust");
                disallowWinFlagGame = Parser.GetInt("DisallowWinFlagGame");
                rtsState0 = Parser.GetInt("RtsState0");
                rtsState1 = Parser.GetInt("RtsState1");
                rtsState2 = Parser.GetInt("RtsState2");
                rtsState3 = Parser.GetInt("RtsState3");
                rtsState4 = Parser.GetInt("RtsState4");
                rtsState5 = Parser.GetInt("RtsState5");
                rtsState6 = Parser.GetInt("RtsState6");
                rtsState7 = Parser.GetInt("RtsState7");
                teamEventStartGame = Parser.GetString("TeamEventStartGame");
            }
        }
    }
}
