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
        /// Handles a query packet
        /// </summary>
        static public void Handle_CS_Query(CS_Query<Zone> pkt, Zone zone)
        {
            using (InfantryDataContext db = zone._server.getContext())
            {
                switch (pkt.queryType)
                {
                    case CS_Query<Zone>.QueryType.accountinfo:
                        Data.DB.alias from = db.alias.SingleOrDefault(a => a.name.Equals(pkt.sender));
                        var aliases = db.alias.Where(a => a.account == from.account);
                        zone._server.sendMessage(zone, pkt.sender, "Account Info");


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
                            zone._server.sendMessage(zone, pkt.sender, String.Format("~{0} ({1}d {2}h {3}m)", alias.name, days, hrs, mins));

                        }
                        //Calculate total time played across all aliases.
                        if (total != 0)
                        {
                            TimeSpan totaltime = TimeSpan.FromMinutes(total);
                            days = (int)totaltime.Days;
                            hrs = (int)totaltime.Hours;
                            mins = (int)totaltime.Minutes;
                            //Send it
                            zone._server.sendMessage(zone, pkt.sender, String.Format("!Grand Total: {0}d {1}h {2}m", days, hrs, mins));
                        }
                        break;

                    case CS_Query<Zone>.QueryType.whois:
                        zone._server.sendMessage(zone, pkt.sender, "&Whois Information");

                        //Query for an IP?
                        System.Net.IPAddress ip;

                        if (System.Net.IPAddress.TryParse(pkt.payload, out ip))
                        {
                            aliases = db.alias.Where(a => a.IPAddress.Equals(ip.ToString()));
                            zone._server.sendMessage(zone, pkt.sender, "*" + ip.ToString());
                        }
                        //Alias!
                        else
                        {
                            Data.DB.alias who = db.alias.SingleOrDefault(a => a.name.Equals(pkt.payload));
                            aliases = db.alias.Where(a => a.account.Equals(who.account));
                            zone._server.sendMessage(zone, pkt.sender, "*" + pkt.payload);
                        }

                        zone._server.sendMessage(zone, pkt.sender, "&Aliases: " + aliases.Count());
                        //Loop through them and display
                        foreach (var alias in aliases)
                            zone._server.sendMessage(zone, pkt.sender, String.Format("*[{0}] {1} (IP={2} Created={3} LastAccess={4})", alias.account, alias.name, alias.IPAddress, alias.creation.ToString(), alias.lastAccess.ToString()));
                        break;

                    case CS_Query<Zone>.QueryType.emailupdate:
                        zone._server.sendMessage(zone, pkt.sender, "&Email Update");

                        Data.DB.account account = db.alias.SingleOrDefault(a => a.name.Equals(pkt.sender)).account1;

                        //Update his email
                        account.email = pkt.payload;
                        db.SubmitChanges();
                        zone._server.sendMessage(zone, pkt.sender, "*Email updated to: " + pkt.payload);
                        break;

                    case CS_Query<Zone>.QueryType.find:
                        int minlength = 3;
                        var results = new List<KeyValuePair<string, Zone.Player>>();

                        foreach (KeyValuePair<string, Zone.Player> player in zone._server._players)
                        {
                            if (player.Key.ToLower() == pkt.payload.ToLower())
                            {
                                //Have they found the exact player they were looking for?
                                results.Add(player);
                                break;
                            }
                            else if (player.Key.ToLower().Contains(pkt.payload.ToLower()) && pkt.payload.Length >= minlength)
                                results.Add(player);
                        }

                        if (results.Count > 0)
                        {
                            zone._server.sendMessage(zone, pkt.sender, "&Search Results");
                            foreach (KeyValuePair<string, Zone.Player> result in results)
                            {
                                zone._server.sendMessage(zone, pkt.sender,
                                    String.Format("*Found: {0} (Zone: {1})", //TODO: Arena??
                                    result.Value.alias, result.Value.zone._zone.name, result.Value.arena));
                            }
                        }
                        else if (pkt.payload.Length < minlength)
                            zone._server.sendMessage(zone, pkt.sender, "Search query must contain at least " + minlength + " characters");
                        else
                            zone._server.sendMessage(zone, pkt.sender, "Sorry, we couldn't locate any players online by that alias");
                        break;

                    case CS_Query<Zone>.QueryType.online:
                        DBServer server = zone._server;

                        foreach (Zone z in zone._server._zones)
                        {
                            server.sendMessage(zone, pkt.sender, String.Format("~Server={0} Players={1}", z._zone.name, z._players.Count()));
                        }
                        zone._server.sendMessage(zone, pkt.sender, String.Format("Infantry (Total={0}) (Peak={1})", server._players.Count(), server.playerPeak));
                        break;

                    case CS_Query<Zone>.QueryType.zonelist:
                        //Collect the list of zones and send it over
                        List<ZoneInstance> zoneList = new List<ZoneInstance>();
                        foreach (Zone z in zone._server._zones.Where(zn => zn._zone.active == 1))
                        {
                            int playercount;
                            //Invert player count of our current zone
                            if (z._zone.port == Convert.ToInt32(pkt.payload))
                                playercount = -z._players.Count;
                            else
                                playercount = z._players.Count;
                            //Add it to our list
                            zoneList.Add(new ZoneInstance(0,
                                z._zone.name,
                                z._zone.ip,
                                Convert.ToInt16(z._zone.port),
                                playercount));
                        }
                        SC_Zones<Zone> zl = new SC_Zones<Zone>();
                        zl.requestee = pkt.sender;
                        zl.zoneList = zoneList;
                        zone._client.sendReliable(zl, 1);
                        break;

                    case CS_Query<Zone>.QueryType.history:
                        int page = Convert.ToInt32(pkt.payload);
                        int resultsperpage = 10;

                        zone._server.sendMessage(zone, pkt.sender, "!Command History (" + page + ")");

                        //Find all commands!
                        List<Data.DB.history> cmds = db.histories.Where(c =>
                            c.id >= (db.histories.Count() - (resultsperpage * (page + 1))) &&
                            c.id < (db.histories.Count() - (resultsperpage * page))).ToList();

                        //List them
                        foreach (Data.DB.history h in cmds)
                            zone._server.sendMessage(zone, pkt.sender, String.Format("!{0} [{1}:{2}] {3}> :{4}: {5}",
                                Convert.ToString(h.date), h.zone, h.arena, h.sender, h.recipient, h.command));

                        zone._server.sendMessage(zone, pkt.sender, "End of page, use ?history 1, ?history 2, etc to navigate previous pages");
                        break;
                }
            }
        }

        /// <summary>
        /// Registers all handlers
        /// </summary>
        [RegistryFunc]
        static public void Register()
        {
            CS_Query<Zone>.Handlers += Handle_CS_Query;
        }
    }
}
