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

        public bool tryDefenseMenu(Player player, Computer computer, VehInfo.Computer.ComputerProduct product)
        {
            int idx = Convert.ToInt32(product.Title.Substring(0, 2));
            Structure structure = getStructureByVehicleID(computer._id);

            switch (idx)
            {
                //1 - Marine Barracks (100 Iron)
                case 1:
                    {
                        if (player.getInventoryAmount(2027) < 100)
                        {
                            player.sendMessage(-1, "&You don't have enough iron for this option");
                            return false;
                        }

                        player.inventoryModify(2027, -100);
                        player.inventoryModify(195, 1);
                        player.sendMessage(0, "1 [RTS] Marine Barracks Kit has been added to your inventory");
                        player.syncState();
                    }
                    break;
                //2 - Ripper Barracks (100 Iron)
                case 2:
                    {
                        if (player.getInventoryAmount(2027) < 100)
                        {
                            player.sendMessage(-1, "&You don't have enough iron for this option");
                            return false;
                        }

                        player.inventoryModify(2027, -100);
                        player.inventoryModify(196, 1);
                        player.sendMessage(0, "1 [RTS] Ripper Barracks Kit has been added to your inventory");
                        player.syncState();
                    }
                    break;
                //3 - Turret - HMG (25 Iron)
                case 3:
                    {
                        if (player.getInventoryAmount(2027) < 25)
                        {
                            player.sendMessage(-1, "&You don't have enough iron for this option");
                            return false;
                        }

                        player.inventoryModify(2027, -25);
                        player.inventoryModify(198, 1);
                        player.sendMessage(0, "1 [RTS] AT HMG Kit has been added to your inventory");
                        player.syncState();
                    }
                    break;
                //4 - Turret - Rocket(25 Iron)
                case 4:
                    {
                        if (player.getInventoryAmount(2027) < 25)
                        {
                            player.sendMessage(-1, "&You don't have enough iron for this option");
                            return false;
                        }

                        player.inventoryModify(2027, -25);
                        player.inventoryModify(199, 1);
                        player.sendMessage(0, "1 [RTS] AT Rocket Kit has been added to your inventory");
                        player.syncState();
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
                //16 - Click For Info
                case 16:
                    {
                        player.sendMessage(0, "$ - Factory - Defense");
                        int repairCost = calculateRepairCost(structure._vehicle);
                        player.sendMessage(0, String.Format("& --- Repair Cost: {0}", repairCost));
                        player.sendMessage(0, "& --- Marine Barracks (Allows you to train Marine bots)");
                        player.sendMessage(0, "& --- Ripper Barracks (Allows you to train Ripper bots)");
                        player.sendMessage(0, "& --- All other options should be self explanatory");
                    }
                    break;

            }
            return false;
        }
    }
}
