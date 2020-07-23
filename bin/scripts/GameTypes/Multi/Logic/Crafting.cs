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
    public class Crafting
    {
        public Dictionary<string, CraftItem> _craftItems;

        public Crafting()
        {
            _craftItems = new Dictionary<string, CraftItem>();

            string[] tables = Directory.GetFiles(System.Environment.CurrentDirectory + "/Crafting/", "*.xml");

            foreach (string table in tables)
            {
                CraftItem newTable = new CraftItem(table);
                _craftItems.Add(newTable.name, newTable);
            }
        }


        public bool playerCraftItem(Player player, string itemName)
        {
            bool bSuccess = false;

            CraftItem item = getCraftItemByName(itemName);

            if (item == null)
              return false;

            //Check if he has all of the required parts
            foreach (var part in item._parts)
            {
                if (player.getInventoryAmount(part.Key.id) < part.Value)
                {
                    player.sendMessage(-1, String.Format("$Blacksmith> You don't have the required items to assemble {0}!", item.name));
                    return false;
                }
            }
            
            //If we're here, we can assume they had all of the parts
            bSuccess = true;
            player.sendMessage(0, "$Blacksmith> It looks like you have all of the parts necessary! This will cost you 2500 cash but I'll have it done in a jiffy!");

            //Remove the parts used
            foreach (var part in item._parts)
            {
                player.inventoryModify(part.Key, -part.Value);
            }

            return bSuccess;
        }

        /// <summary>
        /// Grabs a loot table with the specified ID if it exists
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public CraftItem getCraftItemByName(string name)
        {
            if (_craftItems.ContainsKey(name))
                return _craftItems[name];
            else
                return null;
        }
    }
}
