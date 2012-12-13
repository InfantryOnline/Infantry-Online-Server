using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
        
        #region accountinfo
        /// <summary>
        /// Queries the database and returns a list of aliases associated with the player
        /// </summary>
        public static void accountinfo(Player player, Player recipient, string payload, int bong)
        {   //Sanity checks
            if (player._server.IsStandalone)
                return;

            CS_Query<Data.Database> query = new CS_Query<Data.Database>();
            query.sender = player._alias;
            query.queryType = CS_Query<Data.Database>.QueryType.accountinfo;
            player._server._db.send(query);
        }
        #endregion

        #region aid
        /// <summary>
        /// Allows one player to send aid (cash) to another.
        /// </summary>
        public static void aid(Player player, Player recipient, string payload, int bong)
        {
            //Is aid enabled for this zone?
            if (!player._arena._server._zoneConfig.addon.aidEnabled)
            {
                player.sendMessage(-1, "Sorry, aid is disabled in this zone");
                return;
            }

            //Check syntax...
            if (!payload.Contains(':'))
            {
                player.sendMessage(-1, "Invalid Syntax, Correct syntax: ?aid targetalias:1");
                return;
            }
            //Setup our payload for reading..
            string[] elements = payload.Split(':');

            //Check payload again.
            if (elements.Count() <= 1)
            {
                player.sendMessage(-1, "Invalid Syntax, Correct syntax: ?aid targetalias:1");
                return;
            }

            //Find our target
            Player target = player._arena.getPlayerByName(elements[0]);
            int amount = Int32.Parse(elements[1]);

            //Oops
            if (target == null)
            {
                player.sendMessage(-1, "Target player does not exist");
                return;
            }
            //Prevent them from spamming someone with low amounts
            if (amount < 5000)
            {
                player.sendMessage(-1, "Must send at least 5000 cash");
                return;
            }
            //Does he meet the requirements? gotta stop statpadding...
            if (player.Experience < player._server._zoneConfig.addon.aidLogic)
            {
                player.sendMessage(-1, "Sorry, You do not meet the requirements to aid another player");
                return;
            }

            //Does he have enough?
            if (player.StatsTotal.cash < amount)
            {
                player.sendMessage(-1, "Sorry, you do not have that much cash to give");
                return;
            }

            //Success!
            player.StatsTotal.cash -= amount;
            target.StatsTotal.cash += amount;
            player.sendMessage(0, String.Format("{0} sent to {1} ({2} remaining)", amount, target._alias, player.StatsTotal.cash));
            target.sendMessage(0, String.Format("You have received {0} cash from {1}", amount, player._alias));

            //Sync up!
            player.syncState();
            target.syncState();
        }
        #endregion

        #region arena
        /// <summary>
        /// Presents the player with a list of arenas available to join
        /// </summary>
        public static void arena(Player player, Player recipient, string payload, int bong)
		{	//Form the list packet to send to him..
			SC_ArenaList arenaList = new SC_ArenaList(player._server._arenas.Values, player);

			player._client.sendReliable(arenaList);
		}
        #endregion

        #region breakdown
        /// <summary>
        /// displays current game statistics
        /// </summary>
        public static void breakdown(Player player, Player recipient, string payload, int bong)
        {
            player._arena.individualBreakdown(player, true);
        }
        #endregion

        #region buy
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
        #endregion

        #region sell
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
        #endregion

        #region chat
        public static void chat(Player player, Player recipient, string payload, int bong)
        {   //Sanity checks
            if (player._server.IsStandalone)
                return;

            if (payload.Contains(':'))
                return;

            CS_JoinChat<Data.Database> join = new CS_JoinChat<Data.Database>();
            join.chat = payload;
            join.from = player._alias;
            player._server._db.send(join);
        }
        #endregion

        #region drop
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
                if (!item.droppable) { 
                    player.sendMessage(-1, "You cannot drop that item."); 
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

                if (!player._arena.exists("Player.ItemDrop") || (bool)player._arena.callsync("Player.ItemDrop", false, player, item, dropAmount))
                {
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
                            player._arena.itemSpawn(item, (ushort)dropAmount, player._state.positionX, player._state.positionY, 0, (int)player._team._id);
                        }
                    }


                    player.sendMessage(0, String.Format("Drop Confirmed: {0} {1}", dropAmount, item.name));
                    //Remove items from inventory
                    player.inventoryModify(item, -dropAmount);
                }
            }
        }
        #endregion

        #region email
        /// <summary>
        /// Updates email address associated with players account
        /// </summary>
        public static void email(Player player, Player recipient, string payload, int bong)
        {   //Sanity checks
            if (player._server.IsStandalone)
                return;

            if (payload=="")
            {
                player.sendMessage(-1, "Invalid syntax. Use ?email newemail");
                return;
            }

            Regex ematch = new Regex(@"^[\w!#$%&'*+\-/=?\^_`{|}~]+(\.[\w!#$%&'*+\-/=?\^_`{|}~]+)*" + "@" + @"((([\-\w]+\.)+[a-zA-Z]{2,4})|(([0-9]{1,3}\.){3}[0-9]{1,3}))$");
            if (ematch.IsMatch(payload) && !payload.EndsWith("."))
            {
                //Pass the payload off to the database
                CS_Query<Data.Database> query = new CS_Query<Data.Database>();
                query.queryType = CS_Query<Data.Database>.QueryType.emailupdate;
                query.sender = player._alias;
                query.payload = payload;
                player._server._db.send(query);
            }
            else
                player.sendMessage(-1, "Invalid email, try again.");
        }
        #endregion

        #region find
        /// <summary>
        /// Searches for a player and returns location
        /// </summary>        
        public static void find(Player player, Player recipient, string payload, int bong)
        {   //Sanity checks
            if (player._server.IsStandalone)
                return;

            CS_Query<Data.Database> findPlayer = new CS_Query<Data.Database>();
            findPlayer.queryType = CS_Query<Data.Database>.QueryType.find;
            findPlayer.payload = payload;
            findPlayer.sender = player._alias;

            player._server._db.send(findPlayer);
        }
        #endregion

        #region help
        /// <summary>
        /// Sends help request to moderators..
        /// </summary>
        public static void help(Player player, Player recipient, string payload, int bong)
        {
            //payload empty?
            if (payload == "")
                payload = "None specified";

            //Check our arena for moderators...
            int mods = 0;
            foreach (Player mod in player._arena.Players)
            {   //Display to every type of "moderator"
                if (mod._permissionStatic > 0)
                {
                    mod.sendMessage(0, String.Format("&HELP:(Zone={0} Arena={1} Player={2}) Reason={3}", player._server.Name, player._arena._name, player._alias, payload));
                    mods++;
                }
            }
            
            //Alert any mods online
            if (mods == 0)
            {
                //Send it to any mods online first then log it
                if (!player._server.IsStandalone)
                {
                    CS_Query<Data.Database> pktquery = new CS_Query<Data.Database>();
                    pktquery.queryType = CS_Query<Data.Database>.QueryType.alert;
                    pktquery.sender = player._alias;
                    pktquery.payload = payload;
                    //Send it!
                    player._server._db.send(pktquery);

                    //Log it in the helpcall database
                    CS_ChatCommand<Data.Database> pkt = new CS_ChatCommand<Data.Database>();
                    pkt.sender = player._alias.ToString();
                    pkt.zone = player._server.Name;
                    pkt.arena = player._arena._name;
                    pkt.reason = payload;
                    //Send it!
                    player._server._db.send(pkt);
                }
            }

            //Notify the player all went well..
            player.sendMessage(0, "Help request sent, when a moderator replies, use :: syntax to reply back");
        }
        #endregion

        #region info
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
        #endregion

        #region lag
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
        #endregion

        #region online
        /// <summary>
        /// Displays the number of players in each zone
        /// </summary>      
        public static void online(Player player, Player recipient, string payload, int bong)
        {   //Sanity checks
            if (player._server.IsStandalone)
                return;

            CS_Query<Data.Database> online = new CS_Query<Data.Database>();
            online.queryType = CS_Query<Data.Database>.QueryType.online;
            online.sender = player._alias;

            player._server._db.send(online);
        }
        #endregion

        #region poll
        ///<summary>
        /// Starts an arena poll for any question asked
        ///</summary>
        public static void poll(Player player, Player recipient, string payload, int bong)
        {
            //Check for a running poll
            if (player._arena._poll != null && player._arena._poll.start)
            {
                if (player._arena._poll._alias != null && player._arena._poll._alias.ContainsKey(player._alias.ToString()))
                {
                    player.sendMessage(-1, "You have already voted.");
                    return;
                }

                player._arena._poll._alias = new Dictionary<String, Arena.PollSettings.PlayerAlias>();
                Arena.PollSettings.PlayerAlias temp = new Arena.PollSettings.PlayerAlias();
                if (payload.ToLower() == "yes" || payload.ToLower() == "y")
                {
                    player._arena._poll.yes++;
                    player._arena._poll._alias.Add(player._alias.ToString(), temp);
                    player.sendMessage(0, "Your vote has been counted.");
                    return;
                }

                else if (payload.ToLower() == "no" || payload.ToLower() == "n")
                {
                    player._arena._poll.no++;
                    player._arena._poll._alias.Add(player._alias.ToString(), temp);
                    player.sendMessage(0, "Your vote has been counted.");
                    return;
                }

                else
                {
                    player.sendMessage(-1, "To answer type: ?poll y/n");
                    return;
                }
            }

            player.sendMessage(-1, "Currently there is no poll topic started.");
        }
        #endregion

        #region structures
        /// <summary>
		/// Displays all structures and resources belonging to a team.
		/// </summary>
        public static void structures(Player player, Player recipient, string payload, int bong)
        {
            IEnumerable<Vehicle> comps = player._arena.Vehicles.Where(v => v != null && v._team._id == player._team._id &&
                v._type.Type == VehInfo.Types.Computer);
            IEnumerable<Vehicle> vehicles = player._arena.Vehicles.Where(v => v != null && v._team._id == player._team._id &&
                v._type.Type == VehInfo.Types.Car);

            //Display computers
            player.sendMessage(0, "&~Team Structures:");
            foreach (Vehicle veh in comps)
            {
                player.sendMessage(0, 
                    String.Format("~{0} Location={1} Health={2}", 
                    veh._type.Name, 
                    Helpers.posToLetterCoord(veh._state.positionX, veh._state.positionY), 
                    veh._state.health));
            }

            //Display Car Vehicles
            player.sendMessage(0, "&~Team Vehicles:");
            foreach (Vehicle veh in vehicles)
            {
                player.sendMessage(0,
                    String.Format("~{0} Location={1} Health={2}",
                    veh._type.Name,
                    Helpers.posToLetterCoord(veh._state.positionX, veh._state.positionY),
                    veh._state.health));
            }
            
        }
        #endregion

        #region spec
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

            //Remove mods from list of spectators
            List<Player> speclist = target._spectators;
            speclist.RemoveAll(s => s.PermissionLevel >= Data.PlayerPermission.Mod);

			if (speclist.Count == 0)
			{
				player.sendMessage(0, "No spectators.");
				return;
			}

			string result = "Spectating: ";

            foreach (Player spectator in speclist)
				result += spectator._alias + ", ";

			player.sendMessage(0, result.TrimEnd(',', ' '));
		}
        #endregion

        #region squad
        /// <summary>
        /// Lists online players of current squad or a specific squad
        /// </summary>
        public static void squad(Player player, Player recipient, string payload, int bong)
        {   //Sanity checks
            if (player._server.IsStandalone)
                return;
            CS_Squads<Data.Database> sqdquery = new CS_Squads<Data.Database>();
            sqdquery.queryType = CS_Squads<Data.Database>.QueryType.online;
            sqdquery.alias = player._alias;
            sqdquery.payload = payload;
            player._server._db.send(sqdquery);
        }
        #endregion

        #region squadlist
        /// <summary>
        /// Lists all players of current squad or a specific squad
        /// </summary>
        public static void squadlist(Player player, Player recipient, string payload, int bong)
        {   //Sanity checks
            if (player._server.IsStandalone)
                return;
            CS_Squads<Data.Database> sqdquery = new CS_Squads<Data.Database>();
            sqdquery.queryType = CS_Squads<Data.Database>.QueryType.list;
            sqdquery.alias = player._alias;
            sqdquery.payload = payload;
            player._server._db.send(sqdquery);
        }
        #endregion

        #region squadlistinvites
        /// <summary>
        /// Lists current player invites or outstanding squad invites
        /// </summary>
        public static void squadlistinvites(Player player, Player recipient, string payload, int bong)
        {   //Sanity checks
            if (player._server.IsStandalone)
                return;
            CS_Squads<Data.Database> sqdquery = new CS_Squads<Data.Database>();
            if (payload.Trim().ToLower() == "squad")
                sqdquery.queryType = CS_Squads<Data.Database>.QueryType.invitessquad;
            else
                sqdquery.queryType = CS_Squads<Data.Database>.QueryType.invitesplayer;
            sqdquery.alias = player._alias;
            sqdquery.payload = payload;
            player._server._db.send(sqdquery);
        }
        #endregion

        #region squadcreate
        /// <summary>
        /// Creates a squad in the current zone
        /// </summary>
        public static void squadcreate(Player player, Player recipient, string payload, int bong)
        {   //Sanity checks
            if (player._server.IsStandalone)
                return;
            CS_Squads<Data.Database> sqdquery = new CS_Squads<Data.Database>();
            sqdquery.queryType = CS_Squads<Data.Database>.QueryType.create;
            sqdquery.alias = player._alias;
            sqdquery.payload = payload;
            player._server._db.send(sqdquery);
        }
        #endregion

        #region squadinvite
        /// <summary>
        /// Extends or revokes a squad invitation from a specific player
        /// </summary>
        public static void squadinvite(Player player, Player recipient, string payload, int bong)
        {   //Sanity checks
            if (player._server.IsStandalone)
                return;
            CS_Squads<Data.Database> sqdquery = new CS_Squads<Data.Database>();
            sqdquery.queryType = CS_Squads<Data.Database>.QueryType.invite;
            sqdquery.alias = player._alias;
            sqdquery.payload = payload;
            player._server._db.send(sqdquery);
        }
        #endregion

        #region squadkick
        /// <summary>
        /// Kicks a player from the current squad
        /// </summary>
        public static void squadkick(Player player, Player recipient, string payload, int bong)
        {   //Sanity checks
            if (player._server.IsStandalone)
                return;
            CS_Squads<Data.Database> sqdquery = new CS_Squads<Data.Database>();
            sqdquery.queryType = CS_Squads<Data.Database>.QueryType.kick;
            sqdquery.alias = player._alias;
            sqdquery.payload = payload;
            player._server._db.send(sqdquery);
        }
        #endregion

        #region squadleave
        /// <summary>
        /// Leaves (or dissolves) current squad
        /// </summary>
        public static void squadleave(Player player, Player recipient, string payload, int bong)
        {   //Sanity checks
            if (player._server.IsStandalone)
                return;
            CS_Squads<Data.Database> sqdquery = new CS_Squads<Data.Database>();
            sqdquery.queryType = CS_Squads<Data.Database>.QueryType.leave;
            sqdquery.alias = player._alias;
            sqdquery.payload = payload;
            player._server._db.send(sqdquery);
        }
        #endregion

        #region squadiresponse
        /// <summary>
        /// Accepts or rejects a squad invitation
        /// </summary>
        public static void squadiresponse(Player player, Player recipient, string payload, int bong)
        {   //Sanity checks
            if (player._server.IsStandalone)
                return;
            CS_Squads<Data.Database> sqdquery = new CS_Squads<Data.Database>();
            sqdquery.queryType = CS_Squads<Data.Database>.QueryType.invitesreponse;
            sqdquery.alias = player._alias;
            sqdquery.payload = payload;
            player._server._db.send(sqdquery);
        }
        #endregion

        #region squadstats
        /// <summary>
        /// Allows a player to view the stats of a squad for his/her zone
        /// </summary>
        public static void squadstats(Player player, Player recipient, string payload, int bong)
        {
            CS_Squads<Data.Database> query = new CS_Squads<Data.Database>();

            query.queryType = CS_Squads<Data.Database>.QueryType.stats;
            query.payload = payload;
            query.alias = player._alias;

            player._server._db.send(query);
        }
        #endregion

        #region squadtransfer
        /// <summary>
        /// Transfers squad ownership to a specified player
        /// </summary>
        public static void squadtransfer(Player player, Player recipient, string payload, int bong)
        {   //Sanity checks
            if (player._server.IsStandalone)
                return;
            CS_Squads<Data.Database> sqdquery = new CS_Squads<Data.Database>();
            sqdquery.queryType = CS_Squads<Data.Database>.QueryType.transfer;
            sqdquery.alias = player._alias;
            sqdquery.payload = payload;
            player._server._db.send(sqdquery);
        }
        #endregion

        #region summon
        /// <summary>
        /// Ignore Summon Request
        /// </summary>
        public static void summon(Player player, Player recipient, string payload, int bong)
        {
            if (payload == "")
            {
                //Tell him who he's currently ignoring
                string ignoreList = "";
                foreach (string p in player._summonIgnore)
                    ignoreList += p + ", ";

                player.sendMessage(0, "&Summon Ignore List");
                if (ignoreList.Length > 0)
                    player.sendMessage(0, "*" + ignoreList);
                else
                    player.sendMessage(0, "*Empty");

                return;
            }

            if (player._summonIgnore.Contains(payload))
            {
                player._summonIgnore.Remove(payload);
                player.sendMessage(0, "Removed '" + payload + "' from summon-ignore list");
            }
            else
            {
                player._summonIgnore.Add(payload);
                player.sendMessage(0, "Added '" + payload + "' to summon-ignore list");
            }
        }
        #endregion

        #region team
        /// <summary>
        /// Allows the user to change teams or create private teams
        /// </summary>
        public static void team(Player player, Player recipient, string payload, int bong)
        {
            if (payload == "")
                //Don't do anything if there is no payload, client will handle it
                return;

            //Valid terrain?
            if (player._arena.getTerrain(player._state.positionX, player._state.positionY).teamChangeEnabled != 1 && player.IsSpectator == false)
            {
                player.sendMessage(-1, "Can't change team from this terrain");
                return;
            }

            //Enough energy?
            int minEnergy = player._server._zoneConfig.arena.teamSwitchMinEnergy / 1000;
            if (player._state.energy < minEnergy)
            {
                player.sendMessage(-1, "Cannot switch teams unless you have at least " + minEnergy + " energy (you have " + player._state.energy + ")");
                return;
            }
            try
            {
                //First check to make sure they're allowed to switch teams
                if (player._arena._server._zoneConfig.arena.allowManualTeamSwitch)
                {
                    //Manual team switching is enabled
                    string teamname;
                    string teampassword;

                    if (payload.Contains(":"))
                    {

                        teamname = payload.Split(':').ElementAt(0);
                        teampassword = payload.Split(':').ElementAt(1);
                    }
                    else
                    {
                        teamname = payload;
                        teampassword = "";
                    }

                    //Are they looking to switch to an existing team?
                    Team newteam = player._arena.getTeamByName(teamname);

                    //Does the team exist?
                    if (newteam != null)
                    {
                        //The team exists!
                        if (newteam.IsSpec)
                        {
                            //Only spectators may join team spec
                            if (player.IsSpectator)
                            {
                                newteam.addPlayer(player);
                                return;
                            }

                            //They're not a spectator!
                            player.sendMessage(-1, "Must be a spectator to join this team");
                            return;
                        }
                        else if (newteam.IsPublic || newteam._password == teampassword)
                        {
                            //Public team or password for private teams match!
                            if (!newteam.IsFull)
                            {
                                newteam.addPlayer(player, true);
                                return;
                            }

                            player.sendMessage(-1, "Team is full");
                            return;
                        }
                        else
                        {
                            //Team is private and passwords don't match
                            if (newteam._isPrivate && newteam.ActivePlayerCount == 0)
                            {
                                //The team is empty. We should put the player on and change the password
                                newteam._password = teampassword; //update the password
                                newteam.addPlayer(player, true); //add the player
                                return;
                            }

                            //The team isn't empty! Invalid password, brah
                            player.sendMessage(-1, "Invalid password for specified team");
                            return;
                        }
                    }
                    else
                    {
                        //Team they're trying to join doesn't exist
                        if (!player._arena._server._zoneConfig.arena.allowPrivateFrequencies)
                        {
                            //Private Frequencies are disabled
                            player.sendMessage(-1, "Private teams are disabled");
                            return;
                        }
                        else
                        {
                            //They want to create a private team
                            Team privteam = new Team(player._arena, player._arena._server);

                            //Assign some information to the team
                            privteam._name = teamname;
                            privteam._isPrivate = true;
                            privteam._password = teampassword;
                            privteam._id = (short)player._arena.Teams.Count();
                            privteam._owner = player;

                            player.sendMessage(0, "You are now the owner of this team, You may kick unwanted players using ?teamkick targetalias, You can also change the password using ?teampassword newpassword");

                            //Create the team and add the player
                            player._arena.createTeam(privteam);
                            privteam.addPlayer(player, true);
                            return;
                        }
                    }
                }
                else
                {
                    //Manual team switching is disabled
                    player.sendMessage(-1, "Manual team switching is disabled");
                    return;
                }
            }
            catch (NullReferenceException e)
            {
                Log.write(TLog.Warning, "Error thrown while switching teams. Sender: '" + player._alias + "' payload: '" + payload + "'" + e);
            }
        }
        #endregion

        #region teamkick
        /// <summary>
        /// Allows the user to kick a player from a privately owned team.
        /// </summary>
        public static void teamkick(Player player, Player recipient, string payload, int bong)
        {   //Check syntax
            if (payload == "")
            {
                player.sendMessage(-1, "Invalid syntax, Corrrect: ?teamkick targetalias");
                return;
            }

            //Is it a public team?
            if (player._team.IsPublic)
            {
                player.sendMessage(-1, "You don't own this team");
                return;
            }

            //Does he own it?
            try
            {
                if (player._alias != player._team._owner._alias)
                {
                    player.sendMessage(-1, "You don't own this team");
                    return;
                }
            }
            catch (Exception e)
            {
                Log.write(TLog.Warning, " " + e);
            }

            //Find the target player and see if he exists..
            Player target = player._arena.getPlayerByName(payload);
            if (target == null)
            {
                player.sendMessage(-1, "Target player doesn't exist");
                return;
            }

            //Is he even on the same team?
            if (target._team != player._team)
            {
                player.sendMessage(-1, "Target player isn't on your team!");
                return;
            }
            //Spec them
            target.spec();
        }
        #endregion

        #region teampassword
        /// <summary>
        /// Allows the user to change the password of a private team
        /// </summary>
        public static void teampassword(Player player, Player recipient, string payload, int bong)
        {   //Check syntax
            if (payload == "")
            {
                player.sendMessage(-1, "Invalid syntax, Corrrect: ?teampassword newpassword");
                return;
            }

            //Is it a public team?
            if (player._team.IsPublic)
            {
                player.sendMessage(-1, "You don't own this team");
                return;
            }

            //Does he own it?
            if (player._alias != player._team._owner._alias)
            {
                player.sendMessage(-1, "You don't own this team");
                return;
            }
            
            //Success, change it.
            player._team._password = payload;
            player.sendMessage(0, "Password sucessfully changed");

        }
        #endregion

        #region wipecharacter
        /// <summary>
        /// Wipes your characters stats and sets them back to initial state
        /// </summary>
        public static void wipecharacter(Player player, Player recipient, string payload, int bong)
        {   //Sanity checks
            if (!player.IsSpectator)
            {
                player.sendMessage(-1, "Must be in spectator mode to wipe character.");
                return;
            }

            if (payload == "")
            {
                player.sendMessage(-1, "Are you sure you want to wipe your character? Type ?wipecharacter yes to confirm");
                return;
            }

            if (payload.ToLower() == "yes")
            {
                //Wiping all stats/inv etc
                player.assignFirstTimeStats(true);
                player.syncInventory();
                player.syncState();
                Logic_Assets.RunEvent(player, player._server._zoneConfig.EventInfo.firstTimeSkillSetup);
                Logic_Assets.RunEvent(player, player._server._zoneConfig.EventInfo.firstTimeInvSetup);
                player.sendMessage(0, "Your character has been wiped.");
            }
        }
        #endregion

        #region zonelist
        /// <summary>
        /// Provides the user with a list of zones
        /// </summary>
        public static void zonelist(Player player, Player recipient, string payload, int bong)
        {
            //If database isn't connected, send a zonelist containing only this zone
            if (player._server.IsStandalone)
            {
                List<Data.ZoneInstance> zoneList = new List<Data.ZoneInstance>();
                zoneList.Add(new Data.ZoneInstance(0,
                    player._server.Name,
                    player._server.IP,
                    (short)player._server.Port,
                    player._server._clients.Count));
                SC_ZoneList zl = new SC_ZoneList(zoneList, player);

                player._client.sendReliable(zl);
            }
            else
            {
                //Defer query to the database
                CS_Query<Data.Database> zonelist = new CS_Query<Data.Database>();
                zonelist.queryType = CS_Query<Data.Database>.QueryType.zonelist;
                zonelist.sender = player._alias;
                zonelist.payload = player._server.Port.ToString();
                player._server._db.send(zonelist);
            }
        }
        #endregion

        #region Registrar
        /// <summary>
        /// Registers all handlers
        /// </summary>
        [Commands.RegistryFunc(HandlerType.ChatCommand)]
        public static IEnumerable<Commands.HandlerDescriptor> Register()
        {
            yield return new HandlerDescriptor(arena, "arena",
                "Displays all arenas availble to join",
                "?arena");

            yield return new HandlerDescriptor(squadstats, "squadstats",
                "Displays squad stats for a particular squad in the requestee's current zone",
                "?squadstats squad or ?squadstats");

            yield return new HandlerDescriptor(aid, "aid",
                "Allows a player to aid another player in the form of money",
                "?aid targetalias:#");

            yield return new HandlerDescriptor(teamkick, "teamkick",
                "Allows a player to kick another from a private team",
                "?teamkick targetalias");

            yield return new HandlerDescriptor(teampassword, "teampassword",
                "Allows a player to change the password of a private team",
                "?teampasword newpassword");

            yield return new HandlerDescriptor(structures, "structures",
                "Displays all structures and vehicles that belong to a team",
                "?structures");

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

            yield return new HandlerDescriptor(email, "email",
                "Updates email address associated with players account",
                "?email password,newemail");

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

            yield return new HandlerDescriptor(poll, "poll",
                "Answers a poll topic",
                "?poll yes/no");

            yield return new HandlerDescriptor(spec, "spec",
                "Displays all players which are spectating you or another player",
                "?spec or ::?spec");

            yield return new HandlerDescriptor(squad, "squad",
                "Lists online players of current squad or a specific squad",
                "?squad or ?squad [squadname]");

            yield return new HandlerDescriptor(squadlist, "squadlist",
                "Lists all players of current squad or a specific squad",
                "?squadlist or ?squadlist [squadname]");

            yield return new HandlerDescriptor(squadlistinvites, "squadlistinvites",
                "Lists current player invites or outstanding squad invites",
                "?squadlistinvites [player/squad]");

            yield return new HandlerDescriptor(squadcreate, "squadcreate",
                "Creates a squad in the current zone",
                "?squadcreate [squadname]:[squadpassword]");

            yield return new HandlerDescriptor(squadinvite, "squadinvite",
                "Extends or revokes a squad invitation from a specific player",
                "?squadinvite [add/remove]:[player]:[squadname]");

            yield return new HandlerDescriptor(squadkick, "squadkick",
                "Kicks a player from the current squad",
                "?squadkick [player]");

            yield return new HandlerDescriptor(squadtransfer, "squadtransfer",
                "Transfers squad ownership to a specified player",
                "?squadtransfer [player]");

            yield return new HandlerDescriptor(squadleave, "squadleave",
                "Leaves (or dissolves) current squad",
                "?squadleave");

            yield return new HandlerDescriptor(squadiresponse, "squadiresponse",
                "Accepts or rejects a squad invitation",
                "?squadIresponse [accept/reject]:[squadname]");

            yield return new HandlerDescriptor(summon, "summon",
                "Ignores summons from the specified player(s)",
                "?summon Player1,Player2,Player3");

            yield return new HandlerDescriptor(team, "team",
                "Displays a list of teams or joins a specified team",
                "?team or ?team name:password");

            yield return new HandlerDescriptor(wipecharacter, "wipecharacter",
                "Wipes your characters stats and sets them back to initial state",
                "?wipecharacter");

            yield return new HandlerDescriptor(zonelist, "zonelist",
                "Displays a list of zones",
                "?zonelist");
        }
        #endregion
    }
}
