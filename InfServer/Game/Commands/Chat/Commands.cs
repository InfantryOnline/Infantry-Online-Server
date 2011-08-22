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
        /// Presents the player with a list of arenas available to join
        /// </summary>
		public static void arena(Player player, Player recipient, string payload)
		{	//Form the list packet to send to him..
			SC_ArenaList arenaList = new SC_ArenaList(player._server._arenas.Values, player);

			player._client.sendReliable(arenaList);
		}

        /// <summary>
        /// Purchases items in the form item1:x1, item2:x2 and so on
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
                int buyAmount = 1;

				if (split.Length > 1)
				{
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
            player._arena.individualBreakdown(player, true);
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
		/// Displays all players which are spectating
		/// </summary>
		public static void spec(Player player, Player recipient, string payload)
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
        /// Displays lag statistics for a particular player
        /// </summary>
		public static void info(Player player, Player recipient, string payload)
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
		public static void lag(Player player, Player recipient, string payload)
		{
			if (recipient != null)
				return;

			player.sendMessage(0, String.Format("PACKET LOSS ClientToServer={0}%  ServerToClient={1}%",
				player._client._stats.C2SPacketLoss.ToString("F"), player._client._stats.S2CPacketLoss.ToString("F")));
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

			yield return new HandlerDescriptor(arena, "arena",
				"Displays all arenas availble to join",
				"?arena");

			yield return new HandlerDescriptor(spec, "spec",
				"Displays all players which are spectating you or another player",
				"?spec or ::?spec");

			yield return new HandlerDescriptor(info, "info",
				"Displays lag statistics for you or another player",
				"?info or ::?info");

			yield return new HandlerDescriptor(lag, "lag",
				"Displays lag statistics for yourself",
				"?lag");
        }
    }
}
