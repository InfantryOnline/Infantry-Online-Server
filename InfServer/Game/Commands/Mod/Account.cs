using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;

using Assets;
using InfServer.Game;
using InfServer.Bots;
using InfServer.Protocol;

namespace InfServer.Game.Commands.Mod
{
    public class Account
    {

        /// <summary>
        /// *whois
        /// </summary>
        public static void whois(Player player, Player recipient, string payload, int bong)
        {
            //Sanity
            if (payload == "" && recipient == null)
            {
                player.sendMessage(-1, "Recipient/payload can not be empty. (*whois alias or :alias:*whois)");
                return;
            }

            //Create a new query packet.
            CS_Query<Data.Database> query = new CS_Query<Data.Database>();
            query.alias = player._alias;
            query.queryType = CS_Query<Data.Database>.QueryType.whois;

            IPAddress ipAddress;
            //Whoising a IP
            if (IPAddress.TryParse(payload, out ipAddress))
            {
                query.ipaddress = payload;
            }
            //Payload alias
            else if (payload.Length > 0 && recipient == null)
            {
                query.recipient = payload;
            }
            //Recipient alias
            else if (recipient != null && payload.Length == 0)
            {
                query.recipient = recipient._alias;
            }

            //Send it!
            player._server._db.send(query);
        }

         /// <summary>
        /// Registers all handlers
        /// </summary>
        [Commands.RegistryFunc(HandlerType.ModCommand)]
        static public IEnumerable<Commands.HandlerDescriptor> Register()
        {
            yield return new HandlerDescriptor(whois, "whois",
                "Displays account related information about a single player.",
                "*whois [ipaddress/alias] or :alias:*whois",
                InfServer.Data.PlayerPermission.Mod);
        }
    }
}
