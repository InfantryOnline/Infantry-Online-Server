using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class Level
        {
            public string lvlFile;
            public string vehFile;
            public string itmFile;
            public int ambientPercent;
            public string briefingFile;
            public int gravity;
            public string rpgFile;
            public string nwsFile;
            public string lioFile;
            public bool forceViewNews;
            public string lightColorWhite;
            public string lightColorRed;
            public string lightColorGreen;
            public string lightColorBlue;
            public string tipFile;
            public int ceilingHeight;
            public int smartDistance;
            public bool isometric;
            public int radarPrizeDistance;
            public bool allowUnqualifiedPickup;
            public int autoPrizePickupDelay;

            public Level(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["Level"];

                lvlFile = Parser.GetString("LvlFile");
                vehFile = Parser.GetString("VehFile");
                itmFile = Parser.GetString("ItmFile");
                ambientPercent = Parser.GetInt("AmbientPercent");
                briefingFile = Parser.GetString("BriefingFile");
                gravity = Parser.GetInt("Gravity");
                rpgFile = Parser.GetString("RpgFile");
                nwsFile = Parser.GetString("NwsFile");
                lioFile = Parser.GetString("LioFile");
                forceViewNews = Parser.GetBool("ForceViewNews");
                lightColorWhite = Parser.GetString("LightColorWhite");
                lightColorRed = Parser.GetString("LightColorRed");
                lightColorGreen = Parser.GetString("LightColorGreen");
                lightColorBlue = Parser.GetString("LightColorBlue");
                tipFile = Parser.GetString("TipFile");
                ceilingHeight = Parser.GetInt("CeilingHeight");
                smartDistance = Parser.GetInt("SmartDistance");
                isometric = Parser.GetBool("Isometric");
                radarPrizeDistance = Parser.GetInt("RadarPrizeDistance");
                allowUnqualifiedPickup = Parser.GetBool("AllowUnqualifiedPickup");
                autoPrizePickupDelay = Parser.GetInt("AutoPrizePickupDelay");
            }
        }
    }
}
