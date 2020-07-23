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
{ 	// Script Class
    /// Provides the interface between the script and arena
    ///////////////////////////////////////////////////////
    public partial class RTS
    {
        public int calculateRepairCost(Vehicle vehicle)
        {
            int damage = vehicle._type.Hitpoints - vehicle._state.health;

            if (damage == 0)
                return 0;
            else
                return Convert.ToInt32(damage * 1.32);
        }

        public int calculateProduction(int level, ProductionBuilding buildingType)
        {
            int result = 0;

 
            switch (buildingType)
            {
                case ProductionBuilding.Shack:
                    result = c_baseShackProduction * level;
                    break;
                case ProductionBuilding.House:
                    result = c_baseHouseProduction * level;
                    break;
                case ProductionBuilding.Villa:
                    result = c_baseVillaProduction * level;
                    break;
                case ProductionBuilding.Ironmine:
                    result = c_baseIronProduction * level;
                    break;
                case ProductionBuilding.Scrapyard:
                    result = c_baseScrapProduction * level;
                    break;
            }
            


            return result;
        }


        public int calculateUpgradeCost(int currentCost, ProductionBuilding buildingType)
        {
            int result = 0;


            switch (buildingType)
            {
                case ProductionBuilding.Shack:
                    result = (int)(currentCost * c_shackUpgradeMultiplier);
                    break;
                case ProductionBuilding.House:
                    result = (int)(currentCost * c_houseUpgradeMultiplier);
                    break;
                case ProductionBuilding.Villa:
                    result = (int)(currentCost * c_villaUpgradeMultiplier);
                    break;
                case ProductionBuilding.Scrapyard:
                    result = (int)(currentCost * c_scrapUpgradeMultiplier);
                    break;
                case ProductionBuilding.Ironmine:
                    result = (int)(currentCost * c_ironMineUpgradeMultiplier);
                    break;
                case ProductionBuilding.CommandCenter:
                    result = (int)(currentCost * c_ccUpgradeMultiplier);
                    break;
            }



            return result;
        }

        public int calculateCityValue(IEnumerable<Structure> structures)
        {
            int result = 0;

            foreach (Structure b in structures)
            {
                switch (b._type.Id)
                {
                    //CommandCenter
                    case 414:
                        {
                            result += 150;

                            int baseCost = c_baseCCUpgrade;
                            for (int i = 1; i < b._productionLevel;)
                            {
                                if (i == 1)
                                    result += baseCost;
                                else
                                {
                                    int currentCost = (int)(baseCost * c_ccUpgradeMultiplier);
                                    result += currentCost;
                                    baseCost = currentCost;
                                }

                                i++;
                            }
                        }
                        break;
                    //Factory - Defense
                    case 416:
                        {
                            result += 100;

                            int baseCost = 500;
                            for (int i = 1; i < b._productionLevel; i++)
                            {
                                int currentCost = (int)(baseCost * c_ccUpgradeMultiplier);
                                result += currentCost;
                                baseCost = currentCost;
                            }
                        }
                        break;
                    //Factory - Housing
                    case 426:
                        {
                            result += 200;

                            int baseCost = 500;
                            for (int i = 1; i < b._productionLevel; i++)
                            {
                                int currentCost = (int)(baseCost * c_ccUpgradeMultiplier);
                                result += currentCost;
                                baseCost = currentCost;
                            }
                        }
                        break;
                    //Factory - Production
                    case 421:
                        {
                            result += 125;

                            int baseCost = 500;
                            for (int i = 1; i < b._productionLevel; i++)
                            {
                                int currentCost = (int)(baseCost * c_ccUpgradeMultiplier);
                                result += currentCost;
                                baseCost = currentCost;
                            }
                        }
                        break;
                    //House
                    case 424:
                        {
                            result += 125;

                            int baseCost = c_baseHouseUpgrade;
                            for (int i = 1; i < b._productionLevel;)
                            {
                                if (i == 2)
                                    result += baseCost;
                                if (i > 2)
                                {
                                    int currentCost = (int)(baseCost * c_houseUpgradeMultiplier);
                                    result += currentCost;
                                    baseCost = currentCost;

                                }
                                i++;
                            }
                        }
                        break;
                    //Iron Mine
                    case 427:
                        {
                            result += 100;

                            int baseCost = c_baseIronMineUpgrade;
                            for (int i = 1; i < b._productionLevel;)
                            {
                                if (i == 2)
                                    result += baseCost;
                                if (i > 2)
                                {
                                    int currentCost = (int)(baseCost * c_ironMineUpgradeMultiplier);
                                    result += currentCost;
                                    baseCost = currentCost;
                                }

                                i++;
                            }
                        }
                        break;
                    //Marine Barracks
                    case 419:
                        {
                            result += 100;

                            int baseCost = 500;
                            for (int i = 1; i < b._productionLevel; i++)
                            {
                                int currentCost = (int)(baseCost * c_ccUpgradeMultiplier);
                                result += currentCost;
                                baseCost = currentCost;
                            }
                        }
                        break;
                    //Power Station
                    case 423:
                        {
                            result += 25;

                            int baseCost = 500;
                            for (int i = 1; i < b._productionLevel; i++)
                            {
                                int currentCost = (int)(baseCost * c_ccUpgradeMultiplier);
                                result += currentCost;
                                baseCost = currentCost;
                            }
                        }
                        break;
                    //Refinery - Iron
                    case 422:
                        {
                            result += 200;

                            int baseCost = 500;
                            for (int i = 1; i < b._productionLevel; i++)
                            {
                                int currentCost = (int)(baseCost * c_ccUpgradeMultiplier);
                                result += currentCost;
                                baseCost = currentCost;
                            }
                        }
                        break;
                    //Ripper Barracks
                    case 420:
                        {
                            result += 100;

                            int baseCost = 500;
                            for (int i = 1; i < b._productionLevel; i++)
                            {
                                int currentCost = (int)(baseCost * c_ccUpgradeMultiplier);
                                result += currentCost;
                                baseCost = currentCost;
                            }
                        }
                        break;
                    //Scrapyard
                    case 428:
                        {
                            result += 100;

                            int baseCost = c_baseScrapUpgrade;
                            for (int i = 1; i < b._productionLevel;)
                            {
                                if (i == 2)
                                    result += baseCost;
                                if (i > 2)
                                { 
                                    int currentCost = (int)(baseCost * c_scrapUpgradeMultiplier);
                                    result += currentCost;
                                    baseCost = currentCost;
                                }

                                i++;
                            }
                        }
                        break;
                    //Shack
                    case 415:
                        {
                            result += 75;

                            int baseCost = c_baseShackUpgrade;
                            for (int i = 1; i < b._productionLevel;)
                            {
                                if (i == 2)
                                    result += baseCost;
                                if (i > 2)
                                {
                                    int currentCost = (int)(baseCost * c_shackUpgradeMultiplier);
                                    result += currentCost;
                                    baseCost = currentCost;
                                }

                                i++;
                            }
                        }
                        break;
                    //Villa
                    case 425:
                        {
                            result += 175;

                            int baseCost = c_basevillaUpgrade;
                            for (int i = 1; i < b._productionLevel;)
                            {
                                if (i == 2)
                                    result += baseCost;
                                if (i > 2)
                                {
                                    int currentCost = (int)(baseCost * c_villaUpgradeMultiplier);
                                    result += currentCost;
                                    baseCost = currentCost;
                                }

                                i++;
                            }
                        }
                        break;
                }
            }

            return result;
        }
    }
}
