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
        public bool tryHelmetTrade(Player player, Computer computer, VehInfo.Computer.ComputerProduct product)
        {
            int idx = Convert.ToInt32(product.Title.Substring(0, 1));

            switch (idx)
            {
                //250 Marine Helmets for 10 Dice
                case 1:
                    {
                        //Not enough!
                        if (player.getInventoryAmount(1492) < 250)
                        {
                            player.sendMessage(0, "Helmet Trader> You don't have enough Marine helmets for this!!");
                            return false;
                        }

                        //Success
                        player.inventoryModify(1492, -250);
                        player.inventoryModify(2018, 10);
                        player.sendMessage(0, "10 Lucky Dice have been added to your inventory");
                    }
                    break;
                //250 Ripper Helmets for 10 Dice
                case 2:
                    {
                        //Not enough!
                        if (player.getInventoryAmount(2017) < 250)
                        {
                            player.sendMessage(0, "Helmet Trader> You don't have enough Ripper helmets for this!!");
                            return false;
                        }

                        //Success
                        player.inventoryModify(2017, -250);
                        player.inventoryModify(2018, 10);
                        player.sendMessage(0, "10 Lucky Dice have been added to your inventory");
                    }
                    break;
                //5000 Ripper Helmets for Tiara
                case 3:
                    {
                        //Not enough!
                        if (player.getInventoryAmount(2017) < 5000)
                        {
                            player.sendMessage(0, "Helmet Trader> You don't have enough Ripper helmets for this!!");
                            return false;
                        }

                        //Success
                        player.inventoryModify(2017, -5000);
                        player.inventoryModify(2019, 1);
                        player.sendMessage(0, "1 Abbath's Tiara has been added to your inventory");
                    }
                    break;
                //5000 Marine Helmets for Tiara
                case 4:
                    {
                        //Not enough!
                        if (player.getInventoryAmount(1492) < 5000)
                        {
                            player.sendMessage(0, "Helmet Trader> You don't have enough Marine helmets for this!!");
                            return false;
                        }

                        //Success
                        player.inventoryModify(1492, -5000);
                        player.inventoryModify(2019, 1);
                        player.sendMessage(0, "1 Abbath's Tiara has been added to your inventory");
                    }
                    break;
                //500 Ripper Helmets for 2 Iron
                case 5:
                    {
                        //Not enough!
                        if (player.getInventoryAmount(2017) < 500)
                        {
                            player.sendMessage(0, "Helmet Trader> You don't have enough Ripper helmets for this!!");
                            return false;
                        }

                        //Success
                        player.inventoryModify(2017, -500);
                        player.inventoryModify(2027, 2);
                        player.sendMessage(0, "2 Refined Iron have been added to your inventory");
                    }
                    break;
                //500 Marine Helmets for 2 Iron
                case 6:
                    {
                        //Not enough!
                        if (player.getInventoryAmount(1492) < 500)
                        {
                            player.sendMessage(0, "Helmet Trader> You don't have enough Marine helmets for this!!");
                            return false;
                        }

                        //Success
                        player.inventoryModify(1492, -500);
                        player.inventoryModify(2027, 2);
                        player.sendMessage(0, "2 Refined Iron have been added to your inventory");
                    }
                    break;
            }

            return false;
        }      
    }
}
