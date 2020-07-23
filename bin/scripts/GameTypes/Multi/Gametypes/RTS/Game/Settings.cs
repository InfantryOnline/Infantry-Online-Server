using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using InfServer.Logic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;

using Assets;

namespace InfServer.Script.GameType_Multi
{   // Script Class
    /// Provides the interface between the script and arena
    ///////////////////////////////////////////////////////
    public partial class RTS
    {
        public const int c_baseShackProduction = 850;
        public const double c_shackProductionInterval = 4; //4 hours
        public const int c_baseShackUpgrade = 25;
        public const double c_shackUpgradeMultiplier = 1.17;

        public const int c_baseHouseProduction = 1650;
        public const double c_houseProductionInterval = 8; //8 hours
        public const int c_baseHouseUpgrade = 40;
        public const double c_houseUpgradeMultiplier = 1.27;

        public const int c_baseVillaProduction = 3250;
        public const double c_villaProductionInterval = 24; //24 hours
        public const int c_basevillaUpgrade = 50;
        public const double c_villaUpgradeMultiplier = 1.42;

        public const int c_baseIronProduction = 5;
        public const double c_baseIronProductionInterval = 4; //4 hours
        public const int c_baseIronMineUpgrade = 50;
        public const double c_ironMineUpgradeMultiplier = 1.56;

        public const int c_baseScrapProduction = 12;
        public const double c_baseScrapProductionInterval = 4; //4 hours
        public const int c_baseScrapUpgrade = 25;
        public const double c_scrapUpgradeMultiplier = 1.56;


        public const int c_maxStructures = 80;
        public const int c_baseCCUpgrade = 500;
        public const double c_ccUpgradeMultiplier = 1.63;

        public enum DefenseProduction
        {
            Marine,
            Ripper,
            EliteRipper,
            EliteMarine
        }
        public enum ProductionBuilding
        {
            Shack = 1,
            House = 2,
            Villa = 3,
            Scrapyard = 4,
            Ironmine = 5,
            CommandCenter = 6
        }
    }
}
