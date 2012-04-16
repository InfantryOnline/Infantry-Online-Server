using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Protocol;
using InfServer.Game.Commands;
using Assets;
using InfServer.Logic;

namespace InfServer.Game.Commands.Chat
{
    /// <summary>
    /// Provides a series of functions for handling chat commands (starting with ?)
    /// Please write commands in this class in alphabetical order!
    /// </summary>
    public class Normal
    {



        public static void accountinfo(Player player, Player recipient, string payload, int bong)
        {
            CS_Query<Data.Database> query = new CS_Query<Data.Database>();
            query.alias = player._alias;
            query.queryType = CS_Query<Data.Database>.QueryType.accountinfo;
            player._server._db.send(query);
        }

      	/// <summary>
        /// Presents the player with a list of arenas available to join
        /// </summary>
        public static void arena(Player player, Player recipient, string payload, int bong)
		{	//Form the list packet to send to him..
			SC_ArenaList arenaList = new SC_ArenaList(player._server._arenas.Values, player);

			player._client.sendReliable(arenaList);
		}



        /// <summary>
        /// displays current game statistics
        /// </summary>
        public static void breakdown(Player player, Player recipient, string payload, int bong)
        {
            player._arena.individualBreakdown(player, true);
        }

        /// <summary>
        /// Purchases items in the form item1:x1, item2:x2 and so on
        /// </summary>
        public static void buy(Player player, Player recipient, string payload, int bong)
        {
            //Can you buy from this location?
            if ((player._arena.getTerrain(player._state.positionX, player._state.positionY).storeEnabled == 1) || (player._team.IsSpec && player._server._zoneConfig.arena.spectatorStore))
            {
                char[] splitArr = { ',' };
                string[] items = payload.Split(splitArr, StringSplitOptions.RemoveEmptyEntries);

                // parse the buy string
                foreach (string itemAmount in items)
                {

                    string[] split = itemAmount.Trim().Split(':');
                    ItemInfo item = player._server._assets.getItemByName(split[0].Trim());

                    // Did we find the item?
                    if (split.Count() == 0 || item == null)
                    {
                        player.sendMessage(-1, "Can't find item for " + itemAmount);
                        continue;
                    }

                    // Do we have the amount?
                    int buyAmount = 1;

                    if (split.Length > 1)
                    {
                        string limitAmount = null;
                        try
                        {
                            limitAmount = split[1].Trim();
                            if (limitAmount.StartsWith("#"))
                            {
                                if (player.getInventory(item) != null)
                                {
                                    // Check out how many we need to buy                      
                                    buyAmount = Convert.ToInt32(limitAmount.Substring(1)) - player.getInventory(item).quantity;
                                }
                                else
                                {
                                    buyAmount = Convert.ToInt32(limitAmount.Substring(1));
                                }
                            }
                            else
                            {
                                // Buying incremental amount
                                buyAmount = Convert.ToInt32(limitAmount);
                            }
                        }
                        catch (FormatException)
                        {
                            player.sendMessage(-1, "invalid amount " + limitAmount + " for item " + split[0]);
                            continue;
                        }
                    }

                    if (buyAmount < 0)
                    {
                        sell(player, recipient, String.Format("{0}:{1}", item.name, -buyAmount), bong);
                        continue;
                    }

                    //Get the player's related inventory item
                    Player.InventoryItem ii = player.getInventory(item);

                    //Buying. Are we able to?
                    if (item.buyPrice == 0)
                    {
                        player.sendMessage(-1, String.Format("{0} cannot be bought.", item.name));
                        continue;
                    }

                   //Check limits
                    if (item.maxAllowed != 0)
                    {
                        int constraint = Math.Abs(item.maxAllowed) - ((ii == null) ? (ushort)0 : ii.quantity);
                        if (buyAmount > constraint)
                            buyAmount = constraint;
                        if (buyAmount == 0)
                        {
                            player.sendMessage(-1, String.Format("You already have the maximum {0}", item.name));
                            continue;
                        }
                    }

                    //Held category checks (mirrored in Player#inventoryModify)
                    if (ii == null && item.heldCategoryType > 0)
                    {
                        int alreadyHolding = player._inventory
                            .Where(it => it.Value.item.heldCategoryType == item.heldCategoryType)
                            .Sum(it => 1);
                        //Veh editor says a held category is "maximum number of unique types of items of this category type"
                        //Vehicle hold categories take precedence over the cfg values
                        if (player.ActiveVehicle._type.HoldItemLimits[item.heldCategoryType] != -1)
                        {
                            if (1 + alreadyHolding > player.ActiveVehicle._type.HoldItemLimits[item.heldCategoryType])
                            {
                                player.sendMessage(-1, "You are already carrying the maximum amount of items in this category.");
                                continue;
                            }
                        }
                        else if (player.ActiveVehicle != player._baseVehicle &&
                            player._baseVehicle._type.HoldItemLimits[item.heldCategoryType] != -1)
                        {
                            if (1 + alreadyHolding > player._baseVehicle._type.HoldItemLimits[item.heldCategoryType])
                            {
                                player.sendMessage(-1, "You are already carrying the maximum amount of items in this category.");
                                continue;
                            }
                        }
                        else if (player._server._zoneConfig.heldCategory.limit[item.heldCategoryType] != -1)
                        {
                            if (1 + alreadyHolding > player._server._zoneConfig.heldCategory.limit[item.heldCategoryType])
                            {
                                player.sendMessage(-1, "You are already carrying the maximum amount of items in this category.");
                                continue;
                            }
                        }
                    }

                    //Make sure he has enough cash first..
                    int buyPrice = item.buyPrice * buyAmount;
                    if (buyPrice > player.Cash)
                    {
                        player.sendMessage(-1, String.Format("You do not have enough cash to make this purchase ({0})", item.name));
                        return;
                    }
                    else
                    {
                        player.Cash -= buyPrice;
                        player.inventoryModify(item, buyAmount);
                        player.sendMessage(0, String.Format("Purchase Confirmed: {0} {1} (cost={2}) (cash-left={3})", buyAmount, item.name, buyPrice, player.Cash));
                    }
                }
            }
            else
            {
                player.sendMessage(-1, "You cannot buy from this location");
            }
        }

        /// <summary>
        /// Purchases items in the form item1:x1, item2:x2 and so on
        /// </summary>
        public static void sell(Player player, Player recipient, string payload, int bong)
        {
            //Can you buy from this location?
            if ((player._arena.getTerrain(player._state.positionX, player._state.positionY).storeEnabled == 1) || (player._team.IsSpec && player._server._zoneConfig.arena.spectatorStore))
            {
                char[] splitArr = { ',' };
                string[] items = payload.Split(splitArr, StringSplitOptions.RemoveEmptyEntries);

                // parse the buy string
                foreach (string itemAmount in items)
                {

                    string[] split = itemAmount.Trim().Split(':');
                    ItemInfo item = player._server._assets.getItemByName(split[0].Trim());

                    // Did we find the item?
                    if (split.Count() == 0 || item == null)
                    {
                        player.sendMessage(-1, "Can't find item for " + itemAmount);
                        continue;
                    }

                    //Get the player's related inventory item
                    Player.InventoryItem ii = player.getInventory(item);
                    if (ii == null)
                    {
                        player.sendMessage(-1, String.Format("You have no {0} to sell", item.name));
                        continue;
                    }

                    // Do we have the amount?
                    int sellAmount = 1;
                    if (split.Length > 1)
                    {
                        string limitAmount = null;
                        try
                        {
                            limitAmount = split[1].Trim();
                            if (limitAmount.StartsWith("#"))
                            {
                                sellAmount = Convert.ToInt32(limitAmount.Substring(1));
                            }
                            else
                            {
                                // Buying incremental amount
                                sellAmount = Convert.ToInt32(limitAmount);
                            }
                        }
                        catch (FormatException)
                        {
                            player.sendMessage(-1, "invalid amount " + limitAmount + " for item " + split[0]);
                            continue;
                        }
                    }
                    if (sellAmount < 0)
                    {
                        player.sendMessage(-1, "Cannot sell a negative amount, use ?buy");
                        continue;
                    }
                    if (sellAmount > ii.quantity)
                        sellAmount = ii.quantity;

                    //Buying. Are we able to?
                    if (item.sellPrice == -1)
                    {
                        player.sendMessage(-1, String.Format("{0} cannot be sold", item.name));
                        continue;
                    }

                    //Check limits (we dont have to)

                    int sellPrice = item.sellPrice * sellAmount;
                    player.Cash += sellPrice;
                    player.inventoryModify(item, -sellAmount);
                    player.sendMessage(0, String.Format("Items Sold: {0} {1} (value={2}) (cash-left={3})", sellAmount, item.name, sellPrice, player.Cash));
                }
            }
            else
            {
                player.sendMessage(-1, "You cannot buy from this location");
            }
        }

        public static void chat(Player player, Player recipient, string payload, int bong)
        {
            if (payload.Contains(':'))
                return;

            CS_JoinChat<Data.Database> join = new CS_JoinChat<Data.Database>();
            join.chat = payload;
            join.from = player._alias;
            player._server._db.send(join);
        }

        /// <summary>
        /// Drops items at the player's location in the form item1:x1, item2:x2 and so on
        /// </summary>
        public static void drop(Player player, Player recipient, string payload, int bong)
        {
            
            char[] splitArr = { ',' };
            string[] items = payload.Split(splitArr, StringSplitOptions.RemoveEmptyEntries);
            // parse the drop string
            foreach (string itemAmount in items)
            {

                string[] split = itemAmount.Trim().Split(':');
                ItemInfo item = player._server._assets.getItemByName(split[0].Trim());

                // Did we find the item?
                if (split.Count() == 0 || item == null)
                {
                    player.sendMessage(-1, "Can't find item for " + itemAmount);
                    continue;
                }

                // Do we have the amount?
                int dropAmount = 1;

                if (split.Length > 1)
                {
                    string limitAmount = null;
                    try
                    {
                        limitAmount = split[1].Trim();
                        if (player.getInventory(item) != null)
                        {
                            if (limitAmount.StartsWith("#"))
                            {
                                //Handle the # if included in the drop string                    
                                dropAmount = Convert.ToInt32(limitAmount.Substring(1));
                            }
                            else
                            {
                                dropAmount = Convert.ToInt32(limitAmount);
                            }
                        }
                    }
                    catch (FormatException)
                    {
                        player.sendMessage(-1, "invalid drop amount " + limitAmount + " for item " + split[0]);
                        continue;
                    }
                }

                //Get the player's related inventory item
                Player.InventoryItem ii = player.getInventory(item);

                //Make sure item is can be dropped
                if (!item.droppable)
                {
                    continue;
                }

                //Item does not exist in their inventory
                if (ii == null)
                {
                    player.sendMessage(-1, String.Format("You do not have any ({0}) to drop", item.name));
                    continue;
                }
                
                //If the drop amount exceeds the amount in the inventory assign it to the amount in inventory
                if (ii.quantity < dropAmount)
                {
                    dropAmount = ii.quantity;
                }

                //If the terrain restricts items from being dropped remove the amount but do not spawn the items
                if (player._arena.getTerrain(player._state.positionX, player._state.positionY).prizeExpire > 1)
                {
                    if (player._arena.getItemCountInRange(item, player.getState().positionX, player.getState().positionY, 50) > 0)
                    {      
                        //If there is another item nearby increases quantity instead of spawning new item
                        player._arena.itemStackSpawn(item, (ushort)dropAmount, player._state.positionX, player._state.positionY, 50);                        
                    }
                    else
                    {                        
                        //Spawn new item since there are no other items nearby
                        player._arena.itemSpawn(item, (ushort)dropAmount, player._state.positionX, player._state.positionY);
                    }
                }

             
                player.sendMessage(0, String.Format("Drop Confirmed: {0} {1}", dropAmount, item.name));
                //Remove items from inventory
                player.inventoryModify(item, -dropAmount);
            }
        }

        /// <summary>
        /// Searches for a player and returns location
        /// </summary>        
        public static void find(Player player, Player recipient, string payload, int bong)
        {
            CS_FindPlayer<Data.Database> findPlayer = new CS_FindPlayer<Data.Database>();
            findPlayer.findAlias = payload;
            findPlayer.alias = player._alias;

            player._server._db.send(findPlayer);
        }        

        /// <summary>
        /// Sends help request to moderators..
        /// </summary>
        public static void help(Player player, Player recipient, string payload, int bong)
        {
            //Ignore help requests in stand alone mode
            if (player._server.IsStandalone)
                return;
            
            //payload empty?
            if (payload == "")
                payload = "None specified";

            //Check our arena for moderators...
            int mods = 0;
            foreach (Player mod in player._arena.Players)
            {   //Display to every type of "moderator"
                if (mod._permissionStatic > 0)
                {
                    mod.sendMessage(0, String.Format("&HELP:(Zone={0} Arena={1} Player={2}) Reason={3}", player._server._name, player._arena._name, player._alias, payload));
                    mods += 1;
                }
            }

            //TODO: Log help requests to the database when there are no moderators online.
            if (mods == 0)
            {
            }

            //Notify the player all went well..
            player.sendMessage(0, "Help request sent, when a moderator replies, use :: syntax to reply back");
        }

        /// <summary>
        /// Displays lag statistics for a particular player
        /// </summary>
        public static void info(Player player, Player recipient, string payload, int bong)
        {
            Player target = recipient;
            if (recipient == null)
                target = player;

            player.sendMessage(0, String.Format("Player Info: {0}  Squad: {1}", target._alias, target._squad == null ? "" : target._squad));
            player.sendMessage(0, String.Format("~-    PING Current={0} ms  Average={1} ms  Low={2} ms  High={3} ms  Last={4} ms",
                target._client._stats.clientCurrentUpdate, target._client._stats.clientAverageUpdate,
                target._client._stats.clientShortestUpdate, target._client._stats.clientLongestUpdate,
                target._client._stats.clientLastUpdate));
            player.sendMessage(0, String.Format("~-    PACKET LOSS ClientToServer={0}%  ServerToClient={1}%",
                target._client._stats.C2SPacketLoss.ToString("F"), target._client._stats.S2CPacketLoss.ToString("F")));
        }

        /// <summary>
        /// Displays lag statistics for self
        /// </summary>
        public static void lag(Player player, Player recipient, string payload, int bong)
        {
            if (recipient != null)
                return;

            player.sendMessage(0, String.Format("PACKET LOSS ClientToServer={0}%  ServerToClient={1}%",
                player._client._stats.C2SPacketLoss.ToString("F"), player._client._stats.S2CPacketLoss.ToString("F")));
        }

        /// <summary>
        /// Displays the number of players in each zone
        /// </summary>      
        public static void online(Player player, Player recipient, string payload, int bong)
        {
            CS_Online<Data.Database> online = new CS_Online<Data.Database>();
            online.alias = player._alias;

            player._server._db.send(online);
        }

		/// <summary>
		/// Displays all players which are spectating
		/// </summary>
        public static void spec(Player player, Player recipient, string payload, int bong)
		{
			Player target = recipient;
			if (recipient == null)
				target = player;

			if (target.IsSpectator)
				return;

			if (target._spectators.Count == 0)
			{
				player.sendMessage(0, "No spectators.");
				return;
			}

			string result = "Spectating: ";

			foreach (Player spectator in target._spectators)
				result += spectator._alias + ", ";

			player.sendMessage(0, result.TrimEnd(',', ' '));
		}

        /// <summary>
        /// Provides the user with a list of zones
        /// </summary>
        public static void zonelist(Player player, Player recipient, string payload, int bong)
        {
            //Temp zone list until I can get zone data properly (Zone name, zone players, zone ip, zone port)
            List<ZoneServer> zones = new List<ZoneServer>();
            ZoneServer tempZone = new ZoneServer();
            tempZone._name = "Ask nearest programmer to fix";
            zones.Add(tempZone);

            SC_ZoneList zoneList = new SC_ZoneList(zones, player);

            player._client.sendReliable(zoneList);
        }

        /// <summary>
        /// Registers all handlers
        /// </summary>
        [Commands.RegistryFunc(HandlerType.ChatCommand)]
        public static IEnumerable<Commands.HandlerDescriptor> Register()
        {
            yield return new HandlerDescriptor(arena, "arena",
                "Displays all arenas availble to join",
                "?arena");

            yield return new HandlerDescriptor(accountinfo, "accountinfo",
                "Displays all aliases registered to a single account.",
                "?accountinfo");

            yield return new HandlerDescriptor(chat, "chat",
                "Joins or leaves specified chats",
                "?chat chat1,chat2,chat3. ?chat off leaves all");

            yield return new HandlerDescriptor(breakdown, "breakdown",
               "Displays current game statistics",
               "?breakdown");

            yield return new HandlerDescriptor(buy, "buy",
                "Buys items",
                "?buy item1:amount1,item2:#absoluteAmount2");

            yield return new HandlerDescriptor(sell, "sell",
                "Sells items",
                "?sell item1:amount1,item2:amount2");

            yield return new HandlerDescriptor(drop, "drop",
               "Drops items",
               "?drop item1:amount1,item2:#absoluteAmount2");            

            yield return new HandlerDescriptor(find, "find",
                "Finds a player.",
                "?find alias");

            yield return new HandlerDescriptor(help, "help",
                "Asks moderator for help.",
                "?help question");

            yield return new HandlerDescriptor(info, "info",
                "Displays lag statistics for you or another player",
                "?info or ::?info");

            yield return new HandlerDescriptor(lag, "lag",
                "Displays lag statistics for yourself",
                "?lag");

            yield return new HandlerDescriptor(online, "online",
                "Lists zones and their playercount",
                 "?online");

            yield return new HandlerDescriptor(spec, "spec",
                "Displays all players which are spectating you or another player",
                "?spec or ::?spec");

            yield return new HandlerDescriptor(zonelist, "zonelist",
                "Displays a list of zones",
                "?zonelist");
        }
    }
}
