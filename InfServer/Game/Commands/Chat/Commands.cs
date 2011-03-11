using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Protocol;
using InfServer.Game.Commands;
using Assets;

namespace InfServer.Game.Commands.Chat
{
    /// <summary>
    /// Provides a series of functions for handling chat commands (starting with ?)
    /// Please write commands in this class in alphabetical order!
    /// </summary>
    public class Normal
    {
               
        /// <summary>
        /// buys items in the form item1:x1, item2:x2 and so on
        /// </summary>
        public static void buy(Player player, Player recipient, string payload)
        {	           
            char[] splitArr = {','};
            string[] items = payload.Split(splitArr, StringSplitOptions.RemoveEmptyEntries);          

            // parse the buy string
            foreach( string itemAmount in items ) {

                string[] split = itemAmount.Trim().Split(':');
                ItemInfo item = player._server._assets.getItemByName(split[0].Trim());

                // Did we find the item?
                if (split.Count() == 0 || item == null)
                {
                    player.sendMessage(-1, "Can't find item for " + itemAmount);
                    continue;
                }

                // Do we have the amount?
                int buyAmount;
                string limitAmount = null;
                try
                {
                    limitAmount = split[1].Trim();
                    if (limitAmount.StartsWith("#") && player.getInventory(item) != null)
                    {
                        // Check out how many we need to buy                      
                        buyAmount = Convert.ToInt32(limitAmount.Substring(1)) - player.getInventory(item).quantity;
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
                
                // Buy the item! (after parsing errors handled)
                player._arena.handlePlayerShop(player, item, buyAmount);                                
            }            

        }

        /// <summary>
        /// displays current game statistics
        /// </summary>
        public static void breakdown(Player player, Player recipient, string payload)
        {
            player._arena.breakdown(player, true);
        }

        /// <summary>
        /// Sends help request to moderators..
        /// </summary>
        public static void help(Player player, Player recipient, string payload)
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
        /// Registers all handlers
        /// </summary>
        [Commands.RegistryFunc(HandlerType.ChatCommand)]
        public static IEnumerable<Commands.HandlerDescriptor> Register()
        {
            yield return new HandlerDescriptor(help, "help",
                "Asks moderator for help.",
                "?help question");

            yield return new HandlerDescriptor(breakdown, "breakdown",
                "Displays current game statistics",
                "?breakdown");

            yield return new HandlerDescriptor(buy, "buy",
                "Buys items",
                "?buy item1:amount1,item2:#absoluteAmount2");
        }
    }
}
