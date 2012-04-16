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
            int minlength = 3;
            var results = new List<KeyValuePair<string,Zone.Player>>();

            foreach (KeyValuePair<string, Zone.Player> player in zone._server._players)
            {
                if (player.Key.ToLower() == pkt.findAlias.ToLower())
                {
                    //Have they found the exact player they were looking for?
                    results.Add(player);
                    break;
                }
                else if (pkt.findAlias.Length < minlength)
                {
                    zone._server.sendMessage(zone, pkt.alias, "Search query must contain at least " + minlength + " characters");
                    return;
                }
                else if (player.Key.ToLower().Contains(pkt.findAlias.ToLower()))
                    results.Add(player);
            }

            if(results.Count > 0)
            {
                zone._server.sendMessage(zone, pkt.alias, "&Search Results");
                foreach (KeyValuePair<string, Zone.Player> result in results)
                {
                    zone._server.sendMessage(zone, pkt.alias,
                        String.Format("*Found: {0} (Zone: {1})", //TODO: Arena??
                        result.Value.alias, result.Value.zone._zone.name, result.Value.arena));
                }
            }
            else
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
                        zone._server.sendMessage(zone, pkt.alias, "Account Info");


                        Int64 total = 0;
                        int days = 0;
                        int hrs = 0;
                        int mins = 0;
                        //Loop through each alias to calculate time played
                        foreach (var alias in aliases)
                        {


                            //Does this alias even have any time played?
                            if (alias.timeplayed.HasValue)
                            {
                                TimeSpan timeplayed = TimeSpan.FromMinutes(alias.timeplayed.Value);
                                days = (int)timeplayed.Days;
                                hrs = (int)timeplayed.Hours;
                                mins = (int)timeplayed.Minutes;

                                total += alias.timeplayed.Value;
                            }

                            //Send it
                            zone._server.sendMessage(zone, pkt.alias, String.Format("~{0} ({1}d {2}h {3}m)", alias.name, days, hrs, mins));
                            
                        }
                        //Calculate total time played across all aliases.
                        if (total != 0)
                        {
                            TimeSpan totaltime = TimeSpan.FromMinutes(total);
                            days = (int)totaltime.Days;
                            hrs = (int)totaltime.Hours;
                            mins = (int)totaltime.Minutes;
                            //Send it
                            zone._server.sendMessage(zone, pkt.alias, String.Format("!Grand Total: {0}d {1}h {2}m", days, hrs, mins));
                        }
                        break;

                    case CS_Query<Zone>.QueryType.whois:
                        zone._server.sendMessage(zone, pkt.alias, "&Whois Information");

                        //Query for an IP?
                        if (pkt.ipaddress.Length > 0)
                        {
                            aliases = db.alias.Where(a => a.IPAddress.Equals(pkt.ipaddress));
                            zone._server.sendMessage(zone, pkt.alias, "*" + pkt.ipaddress);
                        }
                        //Alias!
                        else
                        {
                            Data.DB.alias who = db.alias.SingleOrDefault(a => a.name.Equals(pkt.recipient));
                            aliases = db.alias.Where(a => a.account.Equals(who.account));
                            zone._server.sendMessage(zone, pkt.alias, "*" + pkt.recipient);
                        }

                        zone._server.sendMessage(zone, pkt.alias, "&Aliases: " + aliases.Count());
                        //Loop through them and display
                        foreach (var alias in aliases)
                            zone._server.sendMessage(zone, pkt.alias, String.Format("*[{0}] {1} (IP={2} Created={3} LastAccess={4})", alias.account, alias.name, alias.IPAddress, alias.creation.ToString(), alias.lastAccess.ToString()));
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
