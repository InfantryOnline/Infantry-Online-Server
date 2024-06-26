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

        public bool tryBarracksMenu(Player player, Computer computer, VehInfo.Computer.ComputerProduct product, DefenseProduction type)
        {
            int idx = Convert.ToInt32(product.Title.Substring(0, 2));
            Structure structure = getStructureByVehicleID(computer._id);

            if (_units.Count >= _botMax)
            {
                player.sendMessage(-1, "You have reach the maximum amount of units you can train/Have active at one time.");
                return false;
            }

            switch (idx)
            {
                //1 - Train 1 Normal ($500)
                case 1:
                    {

                        if (player.Cash < 500)
                        {
                            player.sendMessage(-1, "Insufficient funds for this option");
                            return false;
                        }
                        player.Cash -= 500;
                        player.sendMessage(0, "Unit Ready");
                        player.syncState();
                        switch (type)
                        {
                            case DefenseProduction.Marine:
                                newUnit(BotType.Marine, null, null, BotLevel.Normal, player._state);
                                break;
                            case DefenseProduction.Ripper:
                                newUnit(BotType.Ripper, null, null, BotLevel.Normal, player._state);
                                break;
                        }
                    }
                    break;
                //2 - Train 1 Adept ($1000)
                case 2:
                    {
                        if (player.Cash < 1000)
                        {
                            player.sendMessage(-1, "Insufficient funds for this option");
                            return false;
                        }
                        player.Cash -= 1000;
                        player.sendMessage(0, "Unit Ready");
                        player.syncState();
                        switch (type)
                        {
                            case DefenseProduction.Marine:
                                newUnit(BotType.Marine, null, null, BotLevel.Adept, player._state);
                                break;
                            case DefenseProduction.Ripper:
                                newUnit(BotType.Ripper, null, null, BotLevel.Adept, player._state);
                                break;
                        }
                    }
                    break;
                //3 - Train 1 Elite ($1500)
                case 3:
                    {
                        if (player.Cash < 1500)
                        {
                            player.sendMessage(-1, "Insufficient funds for this option");
                            return false;
                        }
                        player.Cash -= 1500;
                        player.sendMessage(0, "Unit Ready");
                        player.syncState();
                        switch (type)
                        {
                            case DefenseProduction.Marine:
                                newUnit(BotType.EliteMarine, null, null, BotLevel.Elite, player._state);
                                break;
                            case DefenseProduction.Ripper:
                                newUnit(BotType.EliteHeavy, null, null, BotLevel.Elite, player._state);
                                break;
                        }
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
                        player.sendMessage(0, "$Barracks Info:");
                        player.sendMessage(0, "& --- The barracks allows you to train new units (bots) that will guard your city.");
                        int repairCost = calculateRepairCost(structure._vehicle);
                        player.sendMessage(0, String.Format("& --- Repair Cost: {0}", repairCost));
                    }
                    break;
            }
            return false;
        }
    }
}
