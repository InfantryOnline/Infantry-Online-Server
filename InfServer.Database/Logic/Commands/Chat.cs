using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Protocol;
using InfServer.Data;

namespace InfServer.Logic
{
    class Logic_ChatCommands
    {
        /// <summary>
		/// Handles a ?find packet.
		/// </summary>
        static public void Handle_CS_FindPlayer(CS_FindPlayer<Zone> pkt, Zone zone)
        {
            bool found = false;
            foreach (KeyValuePair<string, Zone.Player> player in zone._server._players)
            {
                if (player.Key.ToLower().Contains(pkt.findAlias.ToLower()))
                {
                    zone._server.sendMessage(zone, pkt.alias,
                        String.Format("Found {0} - (Zone={1}) (Arena={2})",
                        player.Value.alias, player.Value.zone._zone.name, player.Value.arena));
                    found = true;
                }
            }

            if (!found)
                zone._server.sendMessage(zone, pkt.alias, "Sorry, we couldn't locate any players online by that alias");
        }

        /// <summary>
        /// Handles a query packet
        /// </summary>
        static public void Handle_CS_Query(CS_Query<Zone> pkt, Zone zone)
        {
            using (InfantryDataContext db = zone._server.getContext())
            {

                switch (pkt.queryType)
                {
                    case CS_Query<Zone>.QueryType.accountinfo:
                        Data.DB.alias from = db.alias.SingleOrDefault(a => a.name.Equals(pkt.alias));
                        var aliases = db.alias.Where(a => a.account == from.account);
                        zone._server.sendMessage(zone, pkt.alias, "!Account Info:");
                        foreach (var alias in aliases)
                        {
                            int hrs = 0;
                            int mins = 0;

                            if (alias.timeplayed.HasValue)
                            {
                                hrs = (int)alias.timeplayed / 60;
                                mins = (int)alias.timeplayed - (hrs * 60);
                            }

                            zone._server.sendMessage(zone, pkt.alias, String.Format("{0}({1}:{2}h)", alias.name, hrs, mins));
                        }
                        break;

                    case CS_Query<Zone>.QueryType.whois:
                        //Query for an IP?
                        if (pkt.ipaddress.Length > 0)
                            aliases = db.alias.Where(a => a.IPAddress.Equals(pkt.ipaddress));
                        //Alias!
                        else
                        {
                            Data.DB.alias who = db.alias.SingleOrDefault(a => a.name.Equals(pkt.recipient));
                            aliases = db.alias.Where(a => a.account.Equals(who.account));
                        }
                       

                        //Loop through them and display
                        foreach (var alias in aliases)
                            zone._server.sendMessage(zone, pkt.alias, String.Format("&{0} (IP={1}) - (Account={2})", alias.name, alias.IPAddress, alias.account));
                        break;

                    case CS_Query<Zone>.QueryType.aliastransfer:
                        break;
                }
            }
        }


        /// <summary>
        /// Handles a ?online packet.
        /// </summary>
        static public void Handle_CS_Online(CS_Online<Zone> pkt, Zone zone)
        {
            DBServer server = zone._server;

            foreach (Zone z in zone._server._zones)
            {
                server.sendMessage(zone, pkt.alias, String.Format("~Server={0} Players={1}", z._zone.name, z._players.Count()));
            }
            zone._server.sendMessage(zone, pkt.alias, String.Format("Infantry (Total={0}) (Peak={1})", server._players.Count(), server.playerPeak));
        }

        /// <summary>
        /// Registers all handlers
        /// </summary>
        [RegistryFunc]
        static public void Register()
        {
            CS_FindPlayer<Zone>.Handlers += Handle_CS_FindPlayer;
            CS_Online<Zone>.Handlers += Handle_CS_Online;
            CS_Query<Zone>.Handlers += Handle_CS_Query;
        }
    }
}
