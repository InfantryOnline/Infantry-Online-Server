using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using InfServer.Protocol;
using InfServer.Game;

using Assets;

namespace InfServer.Logic
{	// Logic_Assets Class
	/// Handles various asset-related functions
	///////////////////////////////////////////////////////
	public partial class Logic_Assets
	{

        /// <summary>
        /// Prunes items from a player's inventory
        /// </summary>
        /// <param name="from">Who we are taking items from</param>
        /// <returns></returns>
        static public Dictionary<ItemInfo, int> pruneItems(Player from)
        {
            Dictionary<ItemInfo, int> items = new Dictionary<ItemInfo,int>();

            foreach (KeyValuePair<int, Player.InventoryItem> itm in from._inventory)
            {
                ItemInfo item = itm.Value.item;

                //Are we dropping this item at all?
                if (item.pruneOdds == 0)
                    continue;

                int chance = from._arena._rand.Next(item.pruneOdds, 1000);

                //Do our bits of math..blahblahblah
                decimal i = Math.Abs(item.pruneDropPercent);
                decimal percent = (i / 1000);
                int quantity = (int)(itm.Value.quantity * percent);

                //Don't drop 0
                if (quantity == 0)
                    continue;

                //You've got to ask yourself one question: 'Do I feel lucky?' Well, do ya punk?
                if (chance >= item.pruneOdds)
                {   //BOOM, drop some items.
                    items.Add(item, quantity);
                }
                //Done
            }

            return items;
        }
	}
}