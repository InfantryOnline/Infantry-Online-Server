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
        /// Displays account related information about a player or IP address
        /// </summary>
        public static void whois(Player player, Player recipient, string payload, int bong)
        {   //Sanity checks
            if (player._server.IsStandalone)
                return;

            if (payload == "" && recipient == null)
            {
                player.sendMessage(-1, "Recipient/payload can not be empty. (*whois alias or *whois ipaddress or ::*whois)");
                return;
            }

            //Create a new query packet.
            CS_Query<Data.Database> query = new CS_Query<Data.Database>();
            query.queryType = CS_Query<Data.Database>.QueryType.whois;
            query.sender = player._alias;

            if (recipient != null)
                query.payload = recipient._alias;
            else if (payload.Length > 0)
                query.payload = payload;

            //Send it!
            player._server._db.send(query);
        }

        /// <summary>
        /// TODO: alias transfers between accounts
        /// </summary>
        public static void transferalias(Player player, Player recipient, string payload, int bong)
        {   //Sanity checks
            if (player._server.IsStandalone)
                return;
        }

         /// <summary>
        /// Registers all handlers
        /// </summary>
        [Commands.RegistryFunc(HandlerType.ModCommand)]
        static public IEnumerable<Commands.HandlerDescriptor> Register()
        {
            yield return new HandlerDescriptor(whois, "whois",
                "Displays account related information about a player or IP address",
                "*whois [ipaddress/alias] or ::*whois",
                InfServer.Data.PlayerPermission.Mod);
        }
    }
}
