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

        public bool tryIronRefinery(Player player, Computer computer, VehInfo.Computer.ComputerProduct product)
        {
            int idx = Convert.ToInt32(product.Title.Substring(0, 2));
            Structure structure = getStructureByVehicleID(computer._id);

            switch (idx)
            {
                //1 - 1 Iron (2 Scrap Metal)
                case 1:
                    {
                        if (player.getInventoryAmount(2026) < 2)
                        {
                            player.sendMessage(-1, "Refinery Worker> I'm sorry, you don't have enough scrap metal for this option");
                            return false;
                        }

                        player.inventoryModify(2027, 1);
                        player.inventoryModify(2026, -2);
                        player.sendMessage(0, "1 Iron has been added to your inventory");
                        player.syncState();
                    }
                    break;
                //2 - 5 Iron (10 Scrap Metal)
                case 2:
                    {
                        if (player.getInventoryAmount(2026) < 10)
                        {
                            player.sendMessage(-1, "Refinery Worker> I'm sorry, you don't have enough scrap metal for this option");
                            return false;
                        }

                        player.inventoryModify(2027, 5);
                        player.inventoryModify(2026, -10);
                        player.sendMessage(0, "5 Iron has been added to your inventory");
                        player.syncState();
                    }
                    break;
                //3 - 10 Iron (18 Scrap Metal)
                case 3:
                    {
                        if (player.getInventoryAmount(2026) < 18)
                        {
                            player.sendMessage(-1, "Refinery Worker> I'm sorry, you don't have enough scrap metal for this option");
                            return false;
                        }
                        player.inventoryModify(2027, 10);
                        player.inventoryModify(2026, -18);
                        player.sendMessage(0, "10 Iron has been added to your inventory");
                        player.syncState();
                    }
                    break;
                //4 - 35 Iron (25 Scrap Metal)
                case 4:
                    {
                        if (player.getInventoryAmount(2026) < 25)
                        {
                            player.sendMessage(-1, "Refinery Worker> I'm sorry, you don't have enough scrap metal for this option");
                            return false;
                        }

                        player.inventoryModify(2027, 35);
                        player.inventoryModify(2026, -25);
                        player.sendMessage(0, "35 Iron has been added to your inventory");
                        player.syncState();
                    }
                    break;
                //5 - 50 Iron (30 Scrap Metal)
                case 5:
                    {
                        if (player.getInventoryAmount(2026) < 30)
                        {
                            player.sendMessage(-1, "Refinery Worker> I'm sorry, you don't have enough scrap metal for this option");
                            return false;
                        }

                        player.inventoryModify(2027, 50);
                        player.inventoryModify(2026, -30);
                        player.sendMessage(0, "50 Iron has been added to your inventory");
                        player.syncState();
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
                //16 - Info
                case 16:
                    {
                        int repairCost = calculateRepairCost(structure._vehicle);
                        player.sendMessage(0, String.Format("&-Repair Cost: {0}", repairCost));
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
            }

            return false;
        }
    }
}
