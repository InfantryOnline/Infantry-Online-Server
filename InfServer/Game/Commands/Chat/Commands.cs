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
        /// buys items in the form item1:x1, item2:x2 and so on
        /// </summary>
        public static void breakdown(Player player, Player recipient, string payload)
        {
            player._arena.breakDown(player, true);
        }

        /// <summary>
        /// sends commands to a mod
        /// </summary>
        public static void help(Player player, Player recipient, string payload)
        {
            // For now, just send a message to the arena
            Helpers.Social_ArenaChat(player._arena, player._alias + " asked for help: " + payload, 0);
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
