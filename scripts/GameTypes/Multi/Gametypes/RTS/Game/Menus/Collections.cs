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

        public bool tryCollectionMenu(Player player, Computer computer, VehInfo.Computer.ComputerProduct product, ProductionBuilding buildingType)
        {
            int idx = Convert.ToInt32(product.Title.Substring(0, 1));
            Structure structure = getStructureByVehicleID(computer._id);

            //Sanity
            if (structure == null)
                return false;

            int level = structure._productionLevel;
            int cash = calculateProduction(level, buildingType);
            int cashNextLevel = calculateProduction(level + 1, buildingType);
            int upgradeNextLevel = calculateUpgradeCost(structure._upgradeCost, buildingType);


            switch (idx)
            {
                //1 - Collect
                case 1:
                    {
                        //Are we ready?
                        if (!IsProductionReady(structure))
                        {
                            player.sendMessage(0, "&This building is not ready for collection yet.");
                            return false;
                        }

                        //Produce!
                        structure.produce(player);

                    }
                    break;
                //2 - Upgrade (100 Iron)
                case 2:
                    {
                        if (player.getInventoryAmount(2027) < structure._upgradeCost)
                        {
                            player.sendMessage(-1, "&You do not have sufficient iron to upgrade this building");
                            return false;
                        }

                        player.inventoryModify(2027, -structure._upgradeCost);
                        player.syncState();

                        //Up the cost for the next level
                        structure._upgradeCost = upgradeNextLevel;
                        structure._productionLevel++;
                        structure._productionQuantity = cashNextLevel;

                        player.sendMessage(0, String.Format("&Structure upgraded, Next collection: {0}", structure._productionQuantity));
                        
                    }
                    break;
                //3 More Info
                case 3:
                    {
                        player.sendMessage(0, String.Format("$Building Info:"));
                        int repairCost = calculateRepairCost(structure._vehicle);
                        player.sendMessage(0, String.Format("& --- Repair Cost: {0}", repairCost));
                        player.sendMessage(0, String.Format("& --- Current Level: {0}", structure._productionLevel));
                        player.sendMessage(0, String.Format("& --- Upgrade Cost for next level: {0} Iron", structure._upgradeCost));
                        player.sendMessage(0, String.Format("& --- Next Collection amount: {0}", structure._productionQuantity));
                        player.sendMessage(0, String.Format("& --- Next Level Collection amount: {0}", cashNextLevel));

                        TimeSpan remaining = _baseScript.timeTo(structure._nextProduction.TimeOfDay);
                        player.sendMessage(0, String.Format("Next collection ready in {0} Hour(s) & {1} minute(s)", remaining.Hours, remaining.Minutes));

                    }
                    break;
                //14 - Sell
                case 14:
                    {
                        player.inventoryModify(2026, structure._vehicle._type.DropItemQuantity);
                        player.syncState();

                        player.sendMessage(0, String.Format("&Structure sold ({0} Scrap added to your inventory)", structure._type.DropItemQuantity));

                        structure.destroyed(structure._vehicle, player);
                        structure._vehicle.destroy(false);
                    }
                    break;
                //15 - Repair
                case 15:
                    {
                        int repairCost = calculateRepairCost(structure._vehicle);

                        if (player.getInventoryAmount(2026) < repairCost)
                        {
                            player.sendMessage(-1, "I'm sorry, you don't have enough scrap metal to repair this building");
                            return false;
                        }

                        structure._vehicle._state.health = (short)structure._vehicle._type.Hitpoints;

                        player.inventoryModify(2026, -repairCost);
                        player.sendMessage(0, "Structure repaired");
                        player.syncState();
                    }
                    break;
            }
            return false;
        }
    }
}
