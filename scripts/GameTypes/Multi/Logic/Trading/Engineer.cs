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
        public bool tryEngineerFort(Player player, Computer computer, VehInfo.Computer.ComputerProduct product)
        {
            int idx = Convert.ToInt32(product.Title.Substring(0, 1));
            switch (idx)
            {
                //PDB Artillery Kit (8 Iron + $800)
                case 1:
                    {
                        if (player.getInventoryAmount(2027) < 8)
                        {
                            player.sendMessage(-1, "Engineer> I'm sorry, you don't have enough iron for this option");
                            return false;
                        }

                        if (player.Cash < 800)
                        {
                            player.sendMessage(-1, "Engineer> I'm sorry, you don't have enough cash for this option");
                            return false;
                        }

                        player.inventoryModify(2027, -8);
                        player.inventoryModify(163, 1);
                        player.Cash -= 800;
                        player.sendMessage(0, "1 PDB Artillery has been added to your inventory");
                        player.syncState();
                    }
                    break;
                //PDB Gatling Kit (8 Iron + $800)
                case 2:
                    {
                        if (player.getInventoryAmount(2027) < 8)
                        {
                            player.sendMessage(-1, "Engineer> I'm sorry, you don't have enough iron for this option");
                            return false;
                        }

                        if (player.Cash < 800)
                        {
                            player.sendMessage(-1, "Engineer> I'm sorry, you don't have enough cash for this option");
                            return false;
                        }

                        player.inventoryModify(2027, -8);
                        player.inventoryModify(162, 1);
                        player.Cash -= 800;
                        player.sendMessage(0, "1 PDB Gatling has been added to your inventory");
                        player.syncState();
                    }
                    break;
            }
            return false;
        }

        public bool tryEngineerVehicle(Player player, Computer computer, VehInfo.Computer.ComputerProduct product)
        {
            int idx = Convert.ToInt32(product.Title.Substring(0, 1));

            switch (idx)
            {
                //Build Exosuit Kit (50 Iron + $1000)
                case 1:
                    {
                        if (player.getInventoryAmount(2027) < 50)
                        {
                            player.sendMessage(-1, "Engineer> I'm sorry, you don't have enough iron for this option");
                            return false;
                        }

                        if (player.Cash < 1000)
                        {
                            player.sendMessage(-1, "Engineer> I'm sorry, you don't have enough cash for this option");
                            return false;
                        }

                        player.inventoryModify(2027, -50);
                        player.inventoryModify(159, 1);
                        player.Cash -= 1000;
                        player.sendMessage(0, "1 Deploy ExoSuit has been added to your inventory");
                        player.syncState();
                    }
                    break;
                //Build Slick Kit(15 Iron + $2000)
                case 2:
                    {
                        if (player.getInventoryAmount(2027) < 75)
                        {
                            player.sendMessage(-1, "Engineer> I'm sorry, you don't have enough iron for this option");
                            return false;
                        }

                        if (player.Cash < 2000)
                        {
                            player.sendMessage(-1, "Engineer> I'm sorry, you don't have enough cash for this option");
                            return false;
                        }

                        player.inventoryModify(2027, -75);
                        player.inventoryModify(161, 1);
                        player.Cash -= 2000;
                        player.sendMessage(0, "1 Deploy Slick has been added to your inventory");
                        player.syncState();
                    }
                    break;
                //3 - Command Center Kit(150 Iron)
                case 3:
                    {
                        if (player.getInventoryAmount(2027) < 150)
                        {
                            player.sendMessage(-1, "Engineer> I'm sorry, you don't have enough iron for this option");
                            return false;
                        }

                        player.inventoryModify(2027, -150);
                        player.inventoryModify(191, 1);
                        player.sendMessage(0, "1 [RTS] Command Center Kit has been added to your inventory");
                        player.syncState();

                        player.sendMessage(0, String.Format("&To use this item, you must type ?go [RTS] <citynamehere> to start your city. Good luck!"));
                    }
                    break;
    
            }
            return false;
        }
    }
}
