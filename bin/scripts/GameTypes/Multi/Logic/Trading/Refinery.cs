using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text.RegularExpressions;

using InfServer.Logic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;

using Assets;

namespace InfServer.Script.GameType_Multi
{
    partial class Script_Multi : Scripts.IScript
    {
        public bool tryIronRefinery(Player player, Computer computer, VehInfo.Computer.ComputerProduct product)
        {
            int idx = Convert.ToInt32(product.Title.Substring(0, 1));

            switch (idx)
            {
                //1 Iron (2 Scrap Metal + $250)
                case 1:
                    {
                        if (player.getInventoryAmount(2026) < 2)
                        {
                            player.sendMessage(-1, "Refinery Worker> I'm sorry, you don't have enough scrap metal for this option");
                            return false;
                        }

                        if (player.Cash < 250)
                        {
                            player.sendMessage(-1, "Refinery Worker> I'm sorry, you don't have enough cash for this option");
                            return false;
                        }

                        player.inventoryModify(2027, 1);
                        player.inventoryModify(2026, -2);
                        player.Cash -= 250;
                        player.sendMessage(0, "1 Iron has been added to your inventory");
                        player.syncState();
                    }
                    break;
                //5 Iron (10 Scrap Metal + $1000)
                case 2:
                    {
                        if (player.getInventoryAmount(2026) < 10)
                        {
                            player.sendMessage(-1, "Refinery Worker> I'm sorry, you don't have enough scrap metal for this option");
                            return false;
                        }

                        if (player.Cash < 1000)
                        {
                            player.sendMessage(-1, "Refinery Worker> I'm sorry, you don't have enough cash for this option");
                            return false;
                        }

                        player.inventoryModify(2027, 5);
                        player.inventoryModify(2026, -10);
                        player.Cash -= 1000;
                        player.sendMessage(0, "5 Iron has been added to your inventory");
                        player.syncState();
                    }
                    break;
                //10 Iron (18 Scrap Metal + $2000)
                case 3:
                    {
                        if (player.getInventoryAmount(2026) < 18)
                        {
                            player.sendMessage(-1, "Refinery Worker> I'm sorry, you don't have enough scrap metal for this option");
                            return false;
                        }

                        if (player.Cash < 2000)
                        {
                            player.sendMessage(-1, "Refinery Worker> I'm sorry, you don't have enough cash for this option");
                            return false;
                        }

                        player.inventoryModify(2027, 10);
                        player.inventoryModify(2026, -18);
                        player.Cash -= 2000;
                        player.sendMessage(0, "10 Iron has been added to your inventory");
                        player.syncState();
                    }
                    break;
                //20 Iron (20 Scrap Metal + $2500)
                case 4:
                    {
                        if (player.getInventoryAmount(2026) < 20)
                        {
                            player.sendMessage(-1, "Refinery Worker> I'm sorry, you don't have enough scrap metal for this option");
                            return false;
                        }

                        if (player.Cash < 2500)
                        {
                            player.sendMessage(-1, "Refinery Worker> I'm sorry, you don't have enough cash for this option");
                            return false;
                        }

                        player.inventoryModify(2027, 20);
                        player.inventoryModify(2026, -20);
                        player.Cash -= 2500;
                        player.sendMessage(0, "20 Iron has been added to your inventory");
                        player.syncState();
                    }
                    break;

            }

            return false;
        }
    }
}
