using System;
using System.Collections.Generic;
using System.Linq;

using InfServer.Protocol;
using InfServer.Data;
using InfServer.Database;

namespace InfServer.Logic
{
    class Logic_ChatCommands
    {
        /// <summary>
        /// Handles a query packet
        /// </summary>
        static public void Handle_CS_ChatQuery(CS_ChatQuery<Zone> pkt, Zone zone)
        {
            using (InfServer.Database.InfantryDataContext db = zone._server.getContext())
            {
                switch (pkt.queryType)
                {
                    case CS_ChatQuery<Zone>.QueryType.accountinfo:
                        {
                            InfServer.Database.alias from = db.alias.SingleOrDefault(a => string.Compare(a.name, pkt.sender, true) == 0);
                            var aliases = db.alias.Where(a => a.account == from.account);
                            zone._server.sendMessage(zone, pkt.sender, "Account Info");

                            Int64 total = 0;
                            int days = 0;
                            int hrs = 0;
                            int mins = 0;
                            //Loop through each alias to calculate time played
                            foreach (var alias in aliases)
                            {
                                TimeSpan timeplayed = TimeSpan.FromMinutes(alias.timeplayed);
                                days = (int)timeplayed.Days;
                                hrs = (int)timeplayed.Hours;
                                mins = (int)timeplayed.Minutes;

                                total += alias.timeplayed;

                                //Send it
                                zone._server.sendMessage(zone, pkt.sender, string.Format("~{0} ({1}d {2}h {3}m)", alias.name, days, hrs, mins));
                            }

                            //Calculate total time played across all aliases.
                            if (total != 0)
                            {
                                TimeSpan totaltime = TimeSpan.FromMinutes(total);
                                days = (int)totaltime.Days;
                                hrs = (int)totaltime.Hours;
                                mins = (int)totaltime.Minutes;
                                //Send it
                                zone._server.sendMessage(zone, pkt.sender, string.Format("!Grand Total: {0}d {1}h {2}m", days, hrs, mins));
                            }
                        }
                        break;

                    case CS_ChatQuery<Zone>.QueryType.accountignore:
                        {
                            InfServer.Database.alias player = db.alias.SingleOrDefault(a => string.Compare(a.name, pkt.payload, true) == 0);
                            if (player != null)
                            {
                                SC_ChatQuery<Zone> cQuery = new SC_ChatQuery<Zone>();
                                cQuery.type = pkt.queryType;
                                cQuery.sender = pkt.sender;
                                cQuery.payload = string.Format("{0},{1}", player.name, player.IPAddress);
                                zone._client.sendReliable(cQuery);
                            }
                        }
                        break;

                    case CS_ChatQuery<Zone>.QueryType.whois:
                        {
                            zone._server.sendMessage(zone, pkt.sender, "&Whois Information");
                            zone._server.sendMessage(zone, pkt.sender, "*" + pkt.payload);

                            //Query for an IP?
                            System.Net.IPAddress ip;
                            long accountID;
                            IQueryable<InfServer.Database.alias> aliases = null;

                            //Are we using wildcards?
                            if (!pkt.payload.Contains('*'))
                            {   //No we aren't, treat this as general matching
                                //IP Lookup?
                                if (pkt.payload.Contains('.') && System.Net.IPAddress.TryParse(pkt.payload, out ip))
                                    aliases = db.alias.Where(a => a.IPAddress.Equals(ip.ToString()));
                                else if (pkt.payload.StartsWith("#") && Int64.TryParse(pkt.payload.TrimStart('#'), out accountID))
                                {   //Account ID
                                    aliases = db.alias.Where(a => a.account == accountID);
                                }
                                else
                                {   //Alias
                                    InfServer.Database.alias who = db.alias.SingleOrDefault(a => string.Compare(a.name, pkt.payload, true) == 0);
                                    if (who == null)
                                    {
                                        zone._server.sendMessage(zone, pkt.sender, "That IP, account id or alias doesn't exist.");
                                        break;
                                    }
                                    aliases = db.alias.Where(a => a.account.Equals(who.account));
                                }

                                if (aliases != null && aliases.Count() > 0)
                                {
                                    zone._server.sendMessage(zone, pkt.sender, "&Aliases: " + aliases.Count());
                                    foreach (var alias in aliases)
                                    {
                                        TimeSpan timeplayed = TimeSpan.FromMinutes(alias.timeplayed);
                                        var days = (int)timeplayed.Days;
                                        var hrs = (int)timeplayed.Hours;
                                        var mins = (int)timeplayed.Minutes;
    
                                        zone._server.sendMessage(zone, pkt.sender, string.Format("*[{0}] {1} (IP={2} Created={3} LastAccess={4} TimePlayed={5}d {6}h {7}m)",
                                            alias.account, // 0
                                            alias.name, // 1
                                            alias.IPAddress, // 2
                                            alias.creation.ToString(), // 3
                                            alias.lastAccess.ToString(), // 4
                                            days, // 5
                                            hrs, // 6
                                            mins) // 7
                                            );
    
                                    }
                                }
                                else
                                    zone._server.sendMessage(zone, pkt.sender, "That IP, account id or alias doesn't exist.");
                                break;
                            }

                            //We are
                            //IP Wildcard Lookup?
                            if (pkt.payload.Contains('.'))
                            {
                                string[] IP = pkt.payload.Split('.');
                                string findIP = "";
                                int result;
                                bool validated = true;

                                //First check if this is a valid ip and not a person with a period in their name
                                //We do this by checking conversions
                                foreach (string str in IP)
                                    if (!str.Trim().Equals("*") && !Int32.TryParse(str, out result))
                                        validated = false;

                                if (!validated)
                                {
                                    //Failed, must be an alias
                                    string trimmed = pkt.payload.Replace('*', '%');
                                    aliases = from w in db.alias
                                              where System.Data.Linq.SqlClient.SqlMethods.Like(w.name, trimmed)
                                              select w;
                                }
                                else
                                {   //Validated IP
                                    //Ranged ip parser method, looks for wildcard as a string stopping point
                                    foreach (string str in IP)
                                        if (!str.Trim().Equals("*"))
                                            findIP += str.Trim() + ".";

                                    aliases = db.alias.Where(w => w.IPAddress.Contains(findIP));
                                }
                            }
                            else
                            {
                                //Alias Wildcard Lookup
                                string trimmed = pkt.payload.Replace('*', '%');
                                aliases = from w in db.alias
                                          where System.Data.Linq.SqlClient.SqlMethods.Like(w.name, trimmed)
                                          select w;
                            }

                            if (aliases != null && aliases.Count() > 0)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "&Aliases: " + aliases.Count());
                                //Loop and display
                                foreach (var alias in aliases)
                                {
                                    TimeSpan timeplayed = TimeSpan.FromMinutes(alias.timeplayed);
                                    var days = (int)timeplayed.Days;
                                    var hrs = (int)timeplayed.Hours;
                                    var mins = (int)timeplayed.Minutes;

                                    zone._server.sendMessage(zone, pkt.sender, string.Format("*[{0}] {1} (IP={2} Created={3} LastAccess={4} TimePlayed={5}d {6}h {7}m)",
                                        alias.account, // 0
                                        alias.name, // 1
                                        alias.IPAddress, // 2
                                        alias.creation.ToString(), // 3
                                        alias.lastAccess.ToString(), // 4
                                        days, // 5
                                        hrs, // 6
                                        mins) // 7
                                        );

                                }
                            }
                            else
                                zone._server.sendMessage(zone, pkt.sender, "No matches found for the given string.");
                        }
                        break;

                    case CS_ChatQuery<Zone>.QueryType.deletealias:
                        {
                            if (string.IsNullOrWhiteSpace(pkt.payload))
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Wrong format typed.");
                                return;
                            }

                            InfServer.Database.alias sender = db.alias.FirstOrDefault(sndr => string.Compare(sndr.name, pkt.sender, true) == 0);
                            if (sender == null)
                                return;

                            //Single alias
                            if (!pkt.payload.Contains(','))
                            {
                                //Lets get all account related info then delete it
                                InfServer.Database.alias palias = db.alias.FirstOrDefault(a => string.Compare(a.name, pkt.payload, true) == 0);
                                if (palias == null)
                                {
                                    zone._server.sendMessage(zone, pkt.sender, "Cannot find the specified alias.");
                                    return;
                                }

                                //First and most important, check to see if this alias is on the account
                                if (palias.account != sender.account)
                                {
                                    zone._server.sendMessage(zone, pkt.sender, "You must be on the account that this alias belongs to.");
                                    return;
                                }

                                var players = db.players.Where(t => t.alias == palias.id).ToList();

                                foreach(var player in players)
                                {
                                    //Check for a squad
                                    if (player.squad != null)
                                    {
                                        IQueryable<InfServer.Database.player> squadmates = db.players.Where(plyr => plyr.zone == player.zone && plyr.squad != null && plyr.squad == player.squad);
                                        if (player.squad1.owner == player.id)
                                        {
                                            if (squadmates.Count() > 1)
                                            {
                                                InfServer.Database.player temp = squadmates.FirstOrDefault(p => p.id != player.id);
                                                //Since the player is the owner, lets just give it to someone else
                                                temp.squad1.owner = temp.id;
                                            }
                                            else if (squadmates.Count() == 1)
                                            {
                                                //Lets delete the squad
                                                db.squads.DeleteOnSubmit(player.squad1);
                                            }
                                        }

                                        player.squad1 = null;
                                        player.squad = null;
                                    }

                                    // Remove the historic stuff too.
                                    var dailies = db.statsDailies.Where(s => s.player == player.id);
                                    var weeklies = db.statsWeeklies.Where(s => s.player == player.id);
                                    var monthlies = db.statsMonthlies.Where(s => s.player == player.id);
                                    var yearlies = db.statsYearlies.Where(s => s.player == player.id);

                                    db.statsDailies.DeleteAllOnSubmit(dailies);
                                    db.statsWeeklies.DeleteAllOnSubmit(weeklies);
                                    db.statsMonthlies.DeleteAllOnSubmit(monthlies);
                                    db.statsYearlies.DeleteAllOnSubmit(yearlies);

                                    db.SubmitChanges();

                                    var stats = db.stats.Where(s => s.id == player.stats);

                                    db.stats.DeleteAllOnSubmit(stats);
                                    db.players.DeleteOnSubmit(player);

                                    db.SubmitChanges();
                                }

                                db.alias.DeleteOnSubmit(palias);

                                db.SubmitChanges();
                                zone._server.sendMessage(zone, pkt.sender, "Alias has been deleted.");
                                break;
                            }
                            else
                            {
                                zone._server.sendMessage(zone, pkt.sender, String.Format("Please remove aliases one at a time."));

                                //Player wants to delete multiple aliases
                                //List<string> cannotFind = new List<string>();
                                //List<string> notOnAccount = new List<string>();
                                //string[] payload = pkt.payload.Split(',');
                                //int deleted = 0;

                                //foreach (string str in payload)
                                //{
                                //    InfServer.Database.alias palias = db.alias.FirstOrDefault(a => string.Compare(a.name, str, true) == 0);
                                //    InfServer.Database.player player = db.players.FirstOrDefault(p => string.Compare(p.alias1.name, str, true) == 0);
                                //    if (palias == null)
                                //    {
                                //        cannotFind.Add(str);
                                //        continue;
                                //    }
                                //    if (player == null)
                                //    {
                                //        cannotFind.Add(str);
                                //        continue;
                                //    }

                                //    //First and most important, check to see if this alias is on the account
                                //    if (palias.account1 != sender.account1)
                                //    {
                                //        notOnAccount.Add(str);
                                //        continue;
                                //    }

                                //    //Check for a squad
                                //    if (player.squad != null)
                                //    {
                                //        IQueryable<InfServer.Database.player> squadmates = db.players.Where(plyr => plyr.zone == player.zone && plyr.squad != null && plyr.squad == player.squad);
                                //        if (player.squad1.owner == player.id)
                                //        {
                                //            if (squadmates.Count() > 1)
                                //            {
                                //                InfServer.Database.player temp = squadmates.FirstOrDefault(p => p.id != player.id);
                                //                //Since the player is the owner, lets just give it to someone else
                                //                temp.squad1.owner = temp.id;
                                //                zone._server.sendMessage(zone, temp.alias1.name, "You have been promoted to squad captain of " + temp.squad1.name);
                                //            }
                                //            else if (squadmates.Count() == 1)
                                //            {
                                //                //Lets delete the squad
                                //                db.squads.DeleteOnSubmit(player.squad1);
                                //            }
                                //            db.SubmitChanges();
                                //        }
                                //        player.squad1 = null;
                                //        player.squad = null;
                                //    }
                                //    //Now lets remove stats
                                //    db.stats.DeleteOnSubmit(player.stats1);
                                //    //Next the player structure
                                //    db.players.DeleteOnSubmit(player);
                                //    //Finally the alias
                                //    db.alias.DeleteOnSubmit(palias);
                                //    db.SubmitChanges();

                                //    //Lets update the counter
                                //    deleted++;
                                //}

                                //if (notOnAccount.Count > 0)
                                //{
                                //    zone._server.sendMessage(zone, pkt.sender, String.Format("{0} alias(es) are not on the account.", notOnAccount.Count));
                                //    string getAlias = "";
                                //    foreach (string str in notOnAccount)
                                //        getAlias += str + ", ";

                                //    getAlias = (getAlias.Substring(0, getAlias.Length - 2));
                                //    zone._server.sendMessage(zone, pkt.sender, getAlias);
                                //}

                                //if (cannotFind.Count > 0)
                                //{
                                //    zone._server.sendMessage(zone, pkt.sender, String.Format("{0} alias(es) cannot be found.", cannotFind.Count));
                                //    string getAlias = "";
                                //    foreach (string str in cannotFind)
                                //        getAlias += str + ", ";

                                //    getAlias = (getAlias.Substring(0, getAlias.Length - 2));
                                //    zone._server.sendMessage(zone, pkt.sender, getAlias);
                                //}

                                //if (deleted > 0)
                                //    zone._server.sendMessage(zone, pkt.sender, String.Format("{0} alias(es) have been deleted.", deleted));
                            }
                        }
                        break;

                    case CS_ChatQuery<Zone>.QueryType.emailupdate:
                        {
                            zone._server.sendMessage(zone, pkt.sender, "&Email Update");

                            InfServer.Database.account account = db.alias.SingleOrDefault(a => string.Compare(a.name, pkt.sender, true) == 0).account1;

                            //Update his email
                            account.email = pkt.payload;
                            db.SubmitChanges();
                            zone._server.sendMessage(zone, pkt.sender, "*Email updated to: " + pkt.payload);
                        }
                        break;

                    case CS_ChatQuery<Zone>.QueryType.find:
                        {
                            int minlength = 3;
                            var results = new List<KeyValuePair<string, Zone.Player>>();
                            //Get our info
                            InfServer.Database.account pAccount = db.alias.FirstOrDefault(f => string.Compare(f.name, pkt.sender, true) == 0).account1;
                            InfServer.Database.player pPlayer = db.zones.First(z => z.id == zone._zone.id).players.First(p => string.Compare(p.alias1.name, pkt.sender, true) == 0);

                            foreach (KeyValuePair<string, Zone.Player> player in zone._server._players)
                            {
                                if (player.Value.stealth && player.Value.permission > pPlayer.permission)
                                {
                                    continue;
                                }

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
                                    //Are we not powered and in a private arena?
                                    if (pAccount.permission < 1 && result.Value.arena.StartsWith("#"))
                                    {
                                        //We are, is this the same zone?
                                        if (result.Value.zone._zone.id == zone._zone.id)
                                        {
                                            //It is, get the info needed
                                            InfServer.Database.player find = db.zones.First(z => z.id == zone._zone.id).players.First(p => p.alias1.name == result.Value.alias);

                                            //Are we on the same squad?
                                            if (find.squad != pPlayer.squad || (find.squad == null && pPlayer.squad == null))
                                                zone._server.sendMessage(zone, pkt.sender,
                                                    string.Format("*Found: {0} (Zone: {1}) (Arena:{2})",
                                                    result.Value.alias, result.Value.zone._zone.name, "Hidden"));
                                            else
                                                zone._server.sendMessage(zone, pkt.sender,
                                                    string.Format("*Found: {0} (Zone: {1}) (Arena:{2})",
                                                    result.Value.alias, result.Value.zone._zone.name, result.Value.arena));
                                        }
                                        else
                                            zone._server.sendMessage(zone, pkt.sender,
                                                string.Format("*Found: {0} (Zone: {1}) (Arena:{2})",
                                                result.Value.alias, result.Value.zone._zone.name, "Hidden"));
                                    }
                                    else
                                        zone._server.sendMessage(zone, pkt.sender,
                                            string.Format("*Found: {0} (Zone: {1}) (Arena:{2})",
                                            result.Value.alias, result.Value.zone._zone.name, result.Value.arena));
                                }
                            }
                            else if (pkt.payload.Length < minlength)
                                zone._server.sendMessage(zone, pkt.sender, "Search query must contain at least " + minlength + " characters");
                            else
                                zone._server.sendMessage(zone, pkt.sender, "Sorry, we couldn't locate any players online by that alias");
                        }
                        break;

                    case CS_ChatQuery<Zone>.QueryType.online:
                        {
                            DBServer server = zone._server;

                            foreach (Zone z in zone._server._zones)
                            {
                                if (z._players.Count() < 1)
                                    continue;
                                server.sendMessage(zone, pkt.sender, string.Format("~Server={0} Players={1}", z._zone.name, z._players.Where(p => !p.Value.stealth).Count()));
                            }
                            zone._server.sendMessage(zone, pkt.sender, string.Format("Infantry (Total={0}) (Peak={1})", server._players.Where(p => !p.Value.stealth).Count(), server.playerPeak));
                        }
                        break;

                    case CS_ChatQuery<Zone>.QueryType.zonelist:
                        {
                            //Collect the list of zones and send it over
                            List<ZoneInstance> zoneList = new List<ZoneInstance>();

                            try
                            {
                                foreach (Zone z in zone._server._zones.Where(zn => zn._zone.active == 1))
                                {
                                    int playercount = z._players.Count;

                                    //Invert player count of our current zone
                                    if (z._zone.port == Convert.ToInt32(pkt.payload))
                                    {
                                        playercount = -z._players.Count;
                                    }

                                    //Add it to our list
                                    zoneList.Add(new ZoneInstance(0,
                                        z._zone.name,
                                        z._zone.ip,
                                        Convert.ToInt16(z._zone.port),
                                        playercount));
                                }
                            }
                            catch(Exception e)
                            {
                                Log.write(TLog.Warning, e.ToString());
                                zone._server.sendMessage(zone, pkt.sender, "Internal server error, could not generate the zonelist.");
                                break;
                            }
                            SC_Zones<Zone> zl = new SC_Zones<Zone>();
                            zl.requestee = pkt.sender;
                            zl.zoneList = zoneList;
                            zone._client.sendReliable(zl, 1);
                        }
                        break;

                    case CS_ChatQuery<Zone>.QueryType.history:
                        {
                            const int resultsPerPage = 30;
                            string[] args = pkt.payload.Split(':');
                            string name = args[0];
                            int page = Convert.ToInt32(args[1]);
                            bool emptyName = string.IsNullOrWhiteSpace(name);

                            zone._server.sendMessage(zone, pkt.sender, "Command History (" + (page + 1) + ")"); //We use + 1 because indexing starts at 0

                            List<InfServer.Database.history> commandHistory =
                                db.histories.Where(hist => emptyName || hist.sender.ToLower() == name.ToLower())
                                    .OrderByDescending(hist => hist.id)
                                    .Skip(page * resultsPerPage)
                                    .Take(resultsPerPage)
                                    .ToList();

                            //List them
                            foreach (InfServer.Database.history h in commandHistory)
                            {
                                zone._server.sendMessage(zone, pkt.sender, string.Format("!{0} [{1}:{2}] {3}> :{4}: {5}",
                                    Convert.ToString(h.date), h.zone, h.arena, h.sender, h.recipient, h.command));
                            }
                            zone._server.sendMessage(zone, pkt.sender, "End of page, use *history 2, *history 3, etc to navigate pages OR *history alias:2 *history alias:3 for aliases.");
                        }
                        break;

                    case CS_ChatQuery<Zone>.QueryType.cmdhistory:
                        {
                            const int resultsPerPage = 30;
                            string[] args = pkt.payload.Split(':');
                            string cmd = args[0];
                            int page = Convert.ToInt32(args[1]);

                            zone._server.sendMessage(zone, pkt.sender, "Command History (" + (page + 1) + ")"); //We use + 1 because indexing starts at 0

                            cmd = cmd.ToLower();

                            var commandHistory =
                                db.histories.Where(h => h.command.ToLower().Contains(cmd))
                                    .OrderByDescending(hist => hist.id)
                                    .Skip(page * resultsPerPage)
                                    .Take(resultsPerPage)
                                    .ToList();

                            //List them
                            foreach (var h in commandHistory)
                            {
                                zone._server.sendMessage(zone, pkt.sender, string.Format("!{0} [{1}:{2}] {3}> :{4}: {5}",
                                    Convert.ToString(h.date), h.zone, h.arena, h.sender, h.recipient, h.command));
                            }
                            zone._server.sendMessage(zone, pkt.sender, "End of page, use *cmdhistory 2, *cmdhistory 3, etc to navigate full history OR *cmdhistory cmd:2 *cmdhistory cmd:3 for command filtering.");
                        }
                        break;

                    case CS_ChatQuery<Zone>.QueryType.global:
                        foreach (Zone z in zone._server._zones)
                            z._server.sendMessage(z, "*", pkt.payload);
                        break;

                    case CS_ChatQuery<Zone>.QueryType.ban:
                        {
                            if (pkt.payload == "")
                                return;

                            Logic_Bans.Ban.BanType type = Logic_Bans.Ban.BanType.None;
                            DateTime expires;
                            DateTime created;
                            string reason;
                            bool found = false;

                            System.Net.IPAddress ipaddress;
                            long accountID;
                            IQueryable<InfServer.Database.alias> aliases = null;

                            zone._server.sendMessage(zone, pkt.sender, "Current Bans for player");

                            //Check for an ip lookup first
                            if (pkt.payload.Contains('.') && System.Net.IPAddress.TryParse(pkt.payload, out ipaddress))
                                aliases = db.alias.Where(a => a.IPAddress.Equals(ipaddress.ToString()));
                            //Check for an account id
                            else if (pkt.payload.StartsWith("#") && Int64.TryParse(pkt.payload.TrimStart('#'), out accountID))
                                aliases = db.alias.Where(a => a.account == accountID);
                            //Alias!
                            else
                            {
                                InfServer.Database.alias who = db.alias.SingleOrDefault(a => string.Compare(a.name, pkt.payload, true) == 0);
                                if (who == null)
                                {
                                    zone._server.sendMessage(zone, pkt.sender, "None");
                                    break;
                                }
                                aliases = db.alias.Where(a => a.account == who.account);
                            }

                            if (aliases != null)
                            {
                                foreach (InfServer.Database.alias what in aliases)
                                {
                                    foreach (InfServer.Database.ban b in db.bans.Where(b =>
                                        b.account == what.account1.id ||
                                        b.IPAddress == what.account1.IPAddress))
                                    {
                                        //Does the alias match the ban name?
                                        if (string.Compare(b.name, what.name, true) != 0) //If it isnt 0, it is false
                                            continue;

                                        //Is it the correct zone?
                                        if (b.zone != null && (b.type == (int)Logic_Bans.Ban.BanType.ZoneBan && b.zone != zone._zone.id))
                                            continue;

                                        //Find all bans for each alias
                                        if (b.type > (int)Logic_Bans.Ban.BanType.None)
                                        {
                                            expires = b.expires.Value;
                                            type = (Logic_Bans.Ban.BanType)b.type;
                                            created = b.created;
                                            reason = b.reason;
                                            found = true;
                                            zone._server.sendMessage(zone, pkt.sender, string.Format("Alias: {0} Type: {1} Created: {2} Expires: {3} Reason: {4}", what.name, type, Convert.ToString(created), Convert.ToString(expires), reason));
                                        }
                                    }
                                }
                            }

                            if (!found)
                                zone._server.sendMessage(zone, pkt.sender, "None");
                        }
                        break;

                    case CS_ChatQuery<Zone>.QueryType.helpcall:
                        {
                            int pageNum = Convert.ToInt32(pkt.payload);
                            int resultseachpage = 30;

                            zone._server.sendMessage(zone, pkt.sender, "!Command Help History (" + pageNum + ")");

                            //Find all commands!
                            InfServer.Database.helpcall end = db.helpcalls.OrderByDescending(a => a.id).First();
                            List<InfServer.Database.helpcall> helps;

                            //Check the results first
                            if (end.id <= resultseachpage)
                                helps = db.helpcalls.Where(e => e.id <= end.id).ToList();
                            else
                                helps = db.helpcalls.Where(e =>
                                    e.id >= (end.id - (resultseachpage * (pageNum + 1))) &&
                                    e.id < (end.id - (resultseachpage * pageNum))).ToList();

                            //List them
                            foreach (InfServer.Database.helpcall h in helps)
                            {
                                zone._server.sendMessage(zone, pkt.sender, string.Format("!{0} [{1}:{2}] {3}> {4}",
                                    Convert.ToString(h.date), h.zone, h.arena, h.sender, h.reason));
                            }

                            zone._server.sendMessage(zone, pkt.sender, "End of page, use *helpcall 1, *helpcall 2, etc to navigate previous pages");
                        }
                        break;

                    case CS_ChatQuery<Zone>.QueryType.alert:
                        {
                            string pAlias;
                            foreach (Zone z in zone._server._zones)
                                foreach (KeyValuePair<int, Zone.Player> player in z._players)
                                {
                                    pAlias = player.Value.alias;
                                    InfServer.Database.alias check = db.alias.SingleOrDefault(a => a.name == pAlias);
                                    if ((check != null) && check.account1.permission > 0 && player.Value.alias == check.name)
                                        z._server.sendMessage(player.Value.zone, player.Value.alias, pkt.payload);
                                    if (player.Value.permission > (int)Data.PlayerPermission.Normal)
                                        z._server.sendMessage(player.Value.zone, player.Value.alias, pkt.payload);
                                }
                        }
                        break;

                    case CS_ChatQuery<Zone>.QueryType.modChat:
                        {
                            if (String.IsNullOrEmpty(pkt.payload))
                                return;

                            string pAlias;
                            foreach (Zone z in zone._server._zones)
                                foreach (KeyValuePair<int, Zone.Player> Player in z._players)
                                {
                                    pAlias = Player.Value.alias;
                                    var alias = db.alias.SingleOrDefault(p => p.name == pAlias);
                                    if (alias == null)
                                        continue;
                                    if (alias.name == pkt.sender)
                                        continue;
                                    //Are they a global mod?
                                    if (alias.account1.permission > 0)
                                        zone._server.sendMessage(Player.Value.zone, Player.Value.alias, pkt.payload);
                                    else
                                    {   //No, check dev powers
                                        var player = db.zones.First(zones => zones.id == z._zone.id).players.First(p => p.alias1 == alias);
                                        if (player != null && player.permission > 0)
                                            z._server.sendMessage(Player.Value.zone, Player.Value.alias, pkt.payload);
                                    }
                                }
                        }
                        break;

                    case CS_ChatQuery<Zone>.QueryType.wipe:
                        {
                            if (String.IsNullOrWhiteSpace(pkt.payload))
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Invalid payload.");
                                return;
                            }

                            //Get the associated player making the command
                            InfServer.Database.player dbplayer = db.zones.First(z => z.id == zone._zone.id).players.First(p => p.alias1.name == pkt.sender);
                            if (dbplayer == null)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Cannot find your player structure.");
                                return;
                            }

                            InfServer.Database.stats stat;
                            //Sanity checks
                            if (pkt.payload.Equals("all"))
                            {
                                //Change all stats to zero
                                List<InfServer.Database.player> players = db.players.Where(z => z.zone == dbplayer.zone).ToList();
                                if (players.Count == 0)
                                {
                                    zone._server.sendMessage(zone, pkt.sender, "Cannot find any players attached to this zone.");
                                    return;
                                }

                                foreach (InfServer.Database.player P in players)
                                {
                                    P.inventory = null;
                                    P.skills = null;

                                    stat = P.stats1;

                                    stat.cash = 0;
                                    stat.experience = 0;
                                    stat.experienceTotal = 0;
                                    stat.kills = 0;
                                    stat.deaths = 0;
                                    stat.killPoints = 0;
                                    stat.deathPoints = 0;
                                    stat.assistPoints = 0;
                                    stat.bonusPoints = 0;
                                    stat.vehicleKills = 0;
                                    stat.vehicleDeaths = 0;
                                    stat.playSeconds = 0;

                                    stat.zonestat1 = 0;
                                    stat.zonestat2 = 0;
                                    stat.zonestat3 = 0;
                                    stat.zonestat4 = 0;
                                    stat.zonestat5 = 0;
                                    stat.zonestat6 = 0;
                                    stat.zonestat7 = 0;
                                    stat.zonestat8 = 0;
                                    stat.zonestat9 = 0;
                                    stat.zonestat10 = 0;
                                    stat.zonestat11 = 0;
                                    stat.zonestat12 = 0;
                                }

                                db.SubmitChanges();
                                zone._server.sendMessage(zone, pkt.sender, "Wipe all has been completed.");
                                break;
                            }

                            //Recipient lookup
                            InfServer.Database.alias recipientAlias = db.alias.FirstOrDefault(a => string.Compare(a.name, pkt.payload, true) == 0);
                            InfServer.Database.player recipientPlayer = db.players.FirstOrDefault(p => p.alias1 == recipientAlias && p.zone == dbplayer.zone);

                            if (recipientPlayer == null)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "No such alias to wipe.");
                                return;
                            }

                            recipientPlayer.skills = null;
                            recipientPlayer.inventory = null;

                            //Change all stats to zero
                            stat = recipientPlayer.stats1;

                            stat.cash = 0;
                            stat.experience = 0;
                            stat.experienceTotal = 0;
                            stat.kills = 0;
                            stat.deaths = 0;
                            stat.killPoints = 0;
                            stat.deathPoints = 0;
                            stat.assistPoints = 0;
                            stat.bonusPoints = 0;
                            stat.vehicleKills = 0;
                            stat.vehicleDeaths = 0;
                            stat.playSeconds = 0;

                            stat.zonestat1 = 0;
                            stat.zonestat2 = 0;
                            stat.zonestat3 = 0;
                            stat.zonestat4 = 0;
                            stat.zonestat5 = 0;
                            stat.zonestat6 = 0;
                            stat.zonestat7 = 0;
                            stat.zonestat8 = 0;
                            stat.zonestat9 = 0;
                            stat.zonestat10 = 0;
                            stat.zonestat11 = 0;
                            stat.zonestat12 = 0;

                            db.SubmitChanges();
                            zone._server.sendMessage(zone, pkt.sender, "Character wipe has been completed.");
                        }
                        break;

                    case CS_ChatQuery<Zone>.QueryType.adminlist:
                        {
                            if (string.IsNullOrWhiteSpace(pkt.payload))
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Payload cannot be empty.");
                                return;
                            }

                            zone._server.sendMessage(zone, pkt.sender, "Current Admin List:");

                            if (pkt.payload.Equals("list"))
                                zone._server.sendMessage(zone, pkt.sender, Logic_Admins.listAdmins());
                        }
                        break;

                }
            }
        }


        /// <summary>
        /// Handles a ?squad command query
        /// </summary>
        static public void Handle_CS_SquadQuery(CS_Squads<Zone> pkt, Zone zone)
        {
            //Clean up the payload to Infantry standards (dont use clean payload for anything involving aliases/player names)
            string cleanPayload = Logic_Chats.CleanIllegalCharacters(pkt.payload);
            using (InfServer.Database.InfantryDataContext db = zone._server.getContext())
            {
                //Get the associated player making the command
                InfServer.Database.player dbplayer = db.zones.First(z => z.id == zone._zone.id).players.First(p => string.Compare(p.alias1.name, pkt.alias, true) == 0);

                switch (pkt.queryType)
                {   //Differentiate the type of query
                    case CS_Squads<Zone>.QueryType.create:
                        {
                            //Sanity checks
                            if (dbplayer.squad != null)
                            {   //traitor is already in a squad
                                zone._server.sendMessage(zone, pkt.alias, "You cannot create a squad if you are already in one (" + dbplayer.squad1.name + ").");
                                return;
                            }

                            if (!cleanPayload.Contains(':'))
                            {   //invalid payload
                                zone._server.sendMessage(zone, pkt.alias, "Invalid syntax. Use: ?squadcreate [squadname]:[squadpassword]");
                                return;
                            }

                            string squadname = cleanPayload.Split(':').ElementAt(0);
                            string squadpassword = cleanPayload.Split(':').ElementAt(1);
                            if (!char.IsLetterOrDigit(squadname[0]) || squadname.Length == 0 || squadname.Length >= 32)
                            {   //invalid name
                                zone._server.sendMessage(zone, pkt.alias, "Invalid squad name, must start with a letter or number and be less than 32 characters long.");
                                return;
                            }

                            if (db.squads.Where(s => string.Compare(s.name, squadname, true) == 0 && s.zone == zone._zone.id).Count() > 0)
                            {   //This squad already exists!
                                zone._server.sendMessage(zone, pkt.alias, "A squad with specified name already exists.");
                                return;
                            }

                            //Create Some Stats first
                            InfServer.Database.squadstats stats = new InfServer.Database.squadstats();
                            stats.kills = 0;
                            stats.deaths = 0;
                            stats.wins = 0;
                            stats.losses = 0;
                            stats.rating = 0;
                            stats.points = 0;

                            db.squadstats.InsertOnSubmit(stats);
                            db.SubmitChanges();

                            //Create the new squad
                            InfServer.Database.squad newsquad = new InfServer.Database.squad();

                            newsquad.name = squadname;
                            newsquad.password = squadpassword;
                            newsquad.owner = dbplayer.id;
                            newsquad.dateCreated = DateTime.Now;
                            newsquad.zone = zone._zone.id;
                            stats.squad = newsquad.id;

                            db.squads.InsertOnSubmit(newsquad);

                            //We need to submit changes now to obtain squad ID and assign it to our creator
                            db.SubmitChanges();
                            dbplayer.squad = newsquad.id;

                            zone._server.sendMessage(zone, pkt.alias, "Successfully created squad: " + newsquad.name + ". Quit and rejoin to be able to use # to squad chat.");
                            Log.write(TLog.Normal, "Player {0} created squad {1} in zone {2}", pkt.alias, newsquad.name, zone._zone.name);
                        }
                        break;

                    case CS_Squads<Zone>.QueryType.invite:
                        {
                            //Sanity checks
                            if (dbplayer.squad == null)
                                return;

                            if (dbplayer.squad1.owner != dbplayer.id)
                            {
                                zone._server.sendMessage(zone, pkt.alias, "Only squad owners may send or revoke squad invitations");
                                return;
                            }

                            if (string.IsNullOrWhiteSpace(pkt.payload) || !pkt.payload.Contains(':'))
                            {
                                zone._server.sendMessage(zone, pkt.alias, "Invalid syntax. Use: ?squadinvite [add/remove]:[playername]:[squadname]");
                                return;
                            }

                            //We dont want to use the clean payload since players have crazy names!
                            string[] sInvite = pkt.payload.Split(':');
                            if (sInvite.Count() != 3)
                            {
                                zone._server.sendMessage(zone, pkt.alias, "Invalid syntax. Use: ?squadinvite [add/remove]:[playername]:[squadname]");
                                return;
                            }

                            //Adding or removing a squad invitation?
                            bool bAdd = (sInvite[0].ToLower().Equals("add")) ? true : false;
                            //The target player
                            InfServer.Database.alias inviteAlias = db.alias.FirstOrDefault(a => string.Compare(a.name, sInvite[1], true) == 0);
                            InfServer.Database.player invitePlayer = db.players.FirstOrDefault(p => p.alias1 == inviteAlias && p.zone == dbplayer.zone);
                            if (invitePlayer == null)
                            {   //No such player!
                                zone._server.sendMessage(zone, pkt.alias, "No player found in this zone by that alias.");
                                return;
                            }

                            KeyValuePair<int, int> squadInvite = new KeyValuePair<int, int>((int)dbplayer.squad, (int)invitePlayer.id);
                            if (bAdd)
                            {   //Send a squad invite
                                if (zone._server._squadInvites.Contains(squadInvite))
                                {   //Exists
                                    zone._server.sendMessage(zone, pkt.alias, "You have already sent a squad invite to " + invitePlayer.alias1.name);
                                }
                                else
                                {   //Doesn't exist
                                    zone._server._squadInvites.Add(squadInvite);
                                    zone._server.sendMessage(zone, pkt.alias, "Squad invite sent to " + invitePlayer.alias1.name);
                                    zone._server.sendMessage(zone, invitePlayer.alias1.name, "You have been invited to join a squad: " + dbplayer.squad1.name);
                                }
                            }
                            else
                            {   //Remove a squad invite
                                if (zone._server._squadInvites.Contains(squadInvite))
                                {   //Exists
                                    zone._server._squadInvites.Remove(squadInvite);
                                    zone._server.sendMessage(zone, pkt.alias, "Revoked squad invitation from " + invitePlayer.alias1.name);
                                }
                                else
                                {   //Doesn't exist
                                    zone._server.sendMessage(zone, pkt.alias, "Found no squad invititations sent to " + invitePlayer.alias1.name);
                                }
                            }
                        }
                        break;

                    case CS_Squads<Zone>.QueryType.kick:
                        {
                            //Sanity checks
                            if (dbplayer.squad == null)
                                return;

                            if (dbplayer.squad1.owner != dbplayer.id)
                            {
                                zone._server.sendMessage(zone, pkt.alias, "Only squad owners may kick players.");
                                return;
                            }

                            //The target player
                            InfServer.Database.alias kickAlias = db.alias.FirstOrDefault(a => string.Compare(a.name, pkt.payload, true) == 0);
                            InfServer.Database.player kickPlayer = db.players.FirstOrDefault(p => p.alias1 == kickAlias && p.zone == dbplayer.zone);
                            if (kickPlayer == null)
                            {   //No such player!
                                zone._server.sendMessage(zone, pkt.alias, "No player found in this zone by that alias.");
                                return;
                            }

                            if (kickPlayer.squad == null || kickPlayer.squad != dbplayer.squad)
                            {   //Liar!
                                zone._server.sendMessage(zone, pkt.alias, "You may only kick players from your own squad.");
                                return;
                            }

                            if (kickPlayer == dbplayer)
                            {   //crazy
                                zone._server.sendMessage(zone, pkt.alias, "You can't kick yourself");
                                return;
                            }

                            //Kick him!
                            kickPlayer.squad = null;
                            zone._server.sendMessage(zone, pkt.alias, "You have kicked " + kickPlayer.alias1.name + " from your squad.");
                            zone._server.sendMessage(zone, kickPlayer.alias1.name, "You have been kicked from squad " + dbplayer.squad1.name);
                        }
                        break;

                    case CS_Squads<Zone>.QueryType.transfer:
                        {
                            //Sanity checks
                            if (string.IsNullOrEmpty(pkt.payload))
                            {
                                zone._server.sendMessage(zone, pkt.alias, "Who are you transferring it to?");
                                return;
                            }

                            if (dbplayer.squad == null)
                            {
                                zone._server.sendMessage(zone, pkt.alias, "You aren't in a squad.");
                                return;
                            }

                            if (dbplayer.squad1.owner != dbplayer.id)
                            {
                                zone._server.sendMessage(zone, pkt.alias, "Only squad owners may transfer squad ownership.");
                                return;
                            }

                            //The target player
                            InfServer.Database.alias transferAlias = db.alias.FirstOrDefault(a => string.Compare(a.name, pkt.payload, true) == 0);
                            InfServer.Database.player transferPlayer = db.players.FirstOrDefault(p => p.alias1 == transferAlias && p.zone == dbplayer.zone);
                            if (transferPlayer == null || transferPlayer.squad != dbplayer.squad)
                            {   //No such player!
                                zone._server.sendMessage(zone, pkt.alias, "No player found in your squad by that alias.");
                                return;
                            }

                            //Transfer ownership to him
                            transferPlayer.squad1.owner = transferPlayer.id;
                            zone._server.sendMessage(zone, pkt.alias, "You have promoted " + transferPlayer.alias1.name + " to squad captain.");
                            zone._server.sendMessage(zone, transferPlayer.alias1.name, "You have been promoted to squad captain of " + transferPlayer.squad1.name);
                        }
                        break;

                    case CS_Squads<Zone>.QueryType.leave:
                        {
                            //Sanity checks
                            if (dbplayer.squad == null)
                            {
                                zone._server.sendMessage(zone, pkt.alias, "You aren't in a squad.");
                                return;
                            }

                            //Get his squad brothers! (if any...)
                            IQueryable<InfServer.Database.player> squadmates = db.players.Where(p => p.zone == dbplayer.zone && p.squad != null && p.squad == dbplayer.squad);

                            //Is he the captain?
                            if (dbplayer.squad1.owner == dbplayer.id)
                            {   //We might need to dissolve the team!
                                if (squadmates.Count() == 1)
                                {
                                    //He's the only one left on the squad... dissolve it!
                                    var squad = dbplayer.squad1;
                                    dbplayer.squad1 = null;
                                    dbplayer.squad = null;
                                    db.squads.DeleteOnSubmit(squad);

                                    db.SubmitChanges();

                                    zone._server.sendMessage(zone, pkt.alias, "Your squad has been dissolved.");
                                }
                                else
                                {   //There are other people on the squad!
                                    //Transfer ownership automatically
                                    InfServer.Database.player transfer = squadmates.FirstOrDefault(p => p.id != dbplayer.id);
                                    dbplayer.squad1.owner = transfer.id;
                                    //Leave the squad...
                                    dbplayer.squad1 = null;
                                    dbplayer.squad = null;
                                    db.SubmitChanges();
                                    zone._server.sendMessage(zone, pkt.alias, string.Format("You have left your squad while giving ownership to {0}. Please relog to complete this process.", transfer.alias1.name));
                                    zone._server.sendMessage(zone, transfer.alias1.name, "You have been promoted to squad captain of " + transfer.squad1.name);

                                    //Notify his squadmates
                                    foreach (InfServer.Database.player sm in squadmates)
                                    {
                                        if (sm.id == dbplayer.id)
                                            continue;
                                        zone._server.sendMessage(zone, sm.alias1.name, pkt.alias + " has left your squad.");
                                    }
                                    break;
                                }
                            }
                            else
                            {
                                //Leave the squad...
                                dbplayer.squad1 = null;
                                dbplayer.squad = null;
                                zone._server.sendMessage(zone, pkt.alias, "You have left your squad, please relog to complete the process.");
                                //Notify his squadmates
                                foreach (InfServer.Database.player sm in squadmates)
                                {
                                    if (sm.id == dbplayer.id)
                                        continue;
                                    zone._server.sendMessage(zone, sm.alias1.name, pkt.alias + " has left your squad.");
                                }
                            }
                        }
                        break;

                    case CS_Squads<Zone>.QueryType.dissolve:
                        {
                            //Sanity checks
                            if (dbplayer.squad == null)
                            {
                                zone._server.sendMessage(zone, pkt.alias, "You aren't in a squad.");
                                return;
                            }

                            //Get his squad brothers! (if any...)
                            IQueryable<InfServer.Database.player> squadmates = db.players.Where(p => p.zone == dbplayer.zone && p.squad != null && p.squad == dbplayer.squad);

                            //Is he the captain?
                            if (dbplayer.squad1.owner == dbplayer.id)
                            {   //We might need to dissolve the team!
                                if (squadmates.Count() == 1)
                                {   //He's the only one left on the squad... dissolve it!
                                    var squad = dbplayer.squad1;
                                    dbplayer.squad1 = null;
                                    dbplayer.squad = null;
                                    db.squads.DeleteOnSubmit(squad);
                                    db.SubmitChanges();
                                    
                                    zone._server.sendMessage(zone, pkt.alias, "Your squad has been dissolved.");
                                }
                                else
                                {   //There are other people on the squad, lets kick them off
                                    foreach (InfServer.Database.player P in squadmates)
                                    {
                                        if (P.id == dbplayer.id)
                                            continue;

                                        P.squad1 = null;
                                        P.squad = null;
                                        zone._server.sendMessage(zone, P.alias1.name, "Your squad has been dissolved.");
                                    }
                                    //Everyone was kicked, lets dissolve
                                    var squad = dbplayer.squad1;
                                    dbplayer.squad1 = null;
                                    dbplayer.squad = null;
                                    db.squads.DeleteOnSubmit(squad);

                                    db.SubmitChanges();

                                    zone._server.sendMessage(zone, pkt.alias, "Your squad has been dissolved.");
                                    break;
                                }
                            }
                            else
                                zone._server.sendMessage(zone, pkt.alias, "You cannot dissolve a squad you are not captain of!");
                        }
                        break;

                    case CS_Squads<Zone>.QueryType.online:
                        {
                            //Do we list his own squad or another?
                            InfServer.Database.squad targetSquadOnline;
                            if (cleanPayload == "")
                                targetSquadOnline = db.squads.FirstOrDefault(s => s.id == dbplayer.squad && s.zone == zone._zone.id);
                            else
                                targetSquadOnline = db.squads.FirstOrDefault(s => s.name == cleanPayload && s.zone == zone._zone.id);

                            if (targetSquadOnline == null)
                            {   //No squad found!
                                zone._server.sendMessage(zone, pkt.alias, "No squad found.");
                                return;
                            }

                            //List his online squadmates!
                            zone._server.sendMessage(zone, pkt.alias, "&Squad Online List: " + dbplayer.squad1.name);
                            zone._server.sendMessage(zone, pkt.alias, "&Captain: " + db.players.First(p => p.id == dbplayer.squad1.owner).alias1.name);
                            List<string> sonline = new List<string>();
                            foreach (InfServer.Database.player smate in db.players.Where(p => p.squad == targetSquadOnline.id))
                                //Make sure he's online!
                                if (zone.getPlayer(smate.alias1.name) != null)
                                    sonline.Add(smate.alias1.name);
                            zone._server.sendMessage(zone, pkt.alias, "*" + string.Join(", ", sonline));
                        }
                        break;

                    case CS_Squads<Zone>.QueryType.list:
                        {
                            InfServer.Database.squad targetSquadList;
                            if (cleanPayload == "")
                                targetSquadList = db.squads.FirstOrDefault(s => s.id == dbplayer.squad && s.zone == zone._zone.id);
                            else
                                targetSquadList = db.squads.FirstOrDefault(s => s.name == cleanPayload && s.zone == zone._zone.id);

                            if (targetSquadList == null)
                            {   //No squad found!
                                zone._server.sendMessage(zone, pkt.alias, "No squad found.");
                                return;
                            }

                            //List the squad name, captain, and members!
                            zone._server.sendMessage(zone, pkt.alias, "&Squad List: " + targetSquadList.name);
                            zone._server.sendMessage(zone, pkt.alias, "&Captain: " + db.players.First(p => p.id == targetSquadList.owner).alias1.name);
                            zone._server.sendMessage(zone, pkt.alias, "Players: ");
                            List<string> splayers = new List<string>();
                            foreach (InfServer.Database.player splayer in db.players.Where(p => p.squad == targetSquadList.id))
                                splayers.Add(splayer.alias1.name);
                            zone._server.sendMessage(zone, pkt.alias, string.Join(", ", splayers));
                        }
                        break;

                    case CS_Squads<Zone>.QueryType.invitessquad:
                        {
                            //Lists the players squads outstanding invitations
                            if (dbplayer.squad == null || dbplayer.squad1.owner != dbplayer.id)
                            {   //No squad found!
                                zone._server.sendMessage(zone, pkt.alias, "You aren't the owner of a squad.");
                                return;
                            }
                            zone._server.sendMessage(zone, pkt.alias, "&Outstanding Player Invitations:");
                            bool found = false;
                            foreach (KeyValuePair<int, int> invite in zone._server._squadInvites)
                            {
                                if (invite.Key == dbplayer.squad)
                                {
                                    zone._server.sendMessage(zone, pkt.alias, "*" + db.players.First(p => p.id == invite.Value).alias1.name);
                                    found = true;
                                }
                            }

                            if (!found)
                                zone._server.sendMessage(zone, pkt.alias, "&None.");
                        }
                        break;

                    case CS_Squads<Zone>.QueryType.invitesplayer:
                        {
                            //Is the zone asking?
                            if (cleanPayload == "zone")
                            {
                                foreach (KeyValuePair<int, int> invite in zone._server._squadInvites)
                                {
                                    if (invite.Value == dbplayer.id)
                                    {
                                        InfServer.Database.squad sq = db.squads.First(s => s.id == invite.Key && s.zone == zone._zone.id);
                                        if (sq != null)
                                            zone._server.sendMessage(zone, pkt.alias, string.Format("You have been invited to join a squad: {0}", sq.name));
                                    }
                                }
                                break;
                            }

                            zone._server.sendMessage(zone, pkt.alias, "&Current Squad Invitations:");
                            bool found = true;
                            foreach (KeyValuePair<int, int> invite in zone._server._squadInvites)
                            {
                                if (invite.Value == dbplayer.id)
                                {
                                    zone._server.sendMessage(zone, pkt.alias, "*" + db.squads.First(s => s.id == invite.Key && s.zone == zone._zone.id).name);
                                    found = true;
                                }
                            }

                            if (!found)
                                zone._server.sendMessage(zone, pkt.alias, "&None.");
                        }
                        break;

                    case CS_Squads<Zone>.QueryType.invitesreponse:
                        {
                            if (string.IsNullOrWhiteSpace(cleanPayload) || !cleanPayload.Contains(':'))
                            {
                                zone._server.sendMessage(zone, pkt.alias, "Invalid syntax. Use: ?squadIresponse [accept/reject]:[squadname]");
                                return;
                            }

                            //Response to a squad invitation
                            string[] sResponse = cleanPayload.Split(':');
                            //Sanity checks
                            if (sResponse.Count() != 2)
                            {   //Invalid syntax
                                zone._server.sendMessage(zone, pkt.alias, "Invalid syntax. Use: ?squadIresponse [accept/reject]:[squadname]");
                                return;
                            }

                            bool bAccept = (sResponse[0].ToLower() == "accept") ? true : false;
                            InfServer.Database.squad responseSquad = db.squads.FirstOrDefault(s => s.name == sResponse[1] && s.zone == zone._zone.id);
                            KeyValuePair<int, int> responsePair = new KeyValuePair<int, int>((int)responseSquad.id, (int)dbplayer.id);

                            if (responseSquad == null || !zone._server._squadInvites.Contains(responsePair))
                            {   //Either squad doesn't exist... or he's a filthy liar
                                zone._server.sendMessage(zone, pkt.alias, "Invalid squad invitation response.");
                                return;
                            }

                            if (bAccept)
                            {   //Acceptance! Get in there, buddy
                                if (dbplayer.squad != null)
                                {
                                    zone._server.sendMessage(zone, pkt.alias, "You can't accept squad invites if you're already in a squad.");
                                    return;
                                }

                                //Add him to the squad!
                                dbplayer.squad = responseSquad.id;
                                zone._server.sendMessage(zone, pkt.alias, "You've joined " + dbplayer.squad1.name + "! Quit and rejoin to be able to use # to squad chat.");
                                zone._server._squadInvites.Remove(responsePair);
                            }
                            else
                            {   //He's getting rid of a squad invite...
                                zone._server._squadInvites.Remove(responsePair);
                                zone._server.sendMessage(zone, pkt.alias, "Revoked squad invitation from " + responseSquad.name);
                            }
                        }
                        break;

                    case CS_Squads<Zone>.QueryType.stats:
                        {
                            InfServer.Database.squad targetSquad;
                            if (pkt.payload.Length > 0)
                            {
                                targetSquad = db.squads.FirstOrDefault(s => s.name == pkt.payload && s.zone == zone._zone.id);
                            }
                            else
                            {
                                targetSquad = db.squads.FirstOrDefault(s => s.id == dbplayer.squad && s.zone == zone._zone.id);
                            }

                            if (targetSquad == null)
                            {
                                zone._server.sendMessage(zone, pkt.alias, "Squad not found or you are not in a squad.");
                            }
                            else
                            {
                                InfServer.Database.squadstats squadstats = db.squadstats.FirstOrDefault(s => s.squad == targetSquad.id);
                                if (squadstats != null)
                                {
                                    zone._server.sendMessage(zone, pkt.alias, String.Format("#~~{0} Stats", targetSquad.name));
                                    zone._server.sendMessage(zone, pkt.alias, String.Format("*--Kills={0}", squadstats.kills));
                                    zone._server.sendMessage(zone, pkt.alias, String.Format("*--Deaths={0}", squadstats.deaths));
                                    zone._server.sendMessage(zone, pkt.alias, String.Format("*--Points={0}", squadstats.points));
                                    zone._server.sendMessage(zone, pkt.alias, String.Format("*--Wins={0}", squadstats.wins));
                                    zone._server.sendMessage(zone, pkt.alias, String.Format("*--Losses={0}", squadstats.losses));
                                    zone._server.sendMessage(zone, pkt.alias, String.Format("&--Rating={0}", squadstats.rating));
                                }
                                else
                                    zone._server.sendMessage(zone, pkt.alias, "This squad has no stats for this zone.");
                            }
                        }
                        break;
                }

                //Save our changes to the database!
                db.SubmitChanges();
            }
        }

        /// <summary>
        /// Handles a ?*chart query
        /// </summary>
        static public void Handle_CS_ChartQuery(CS_ChartQuery<Zone> pkt, Zone zone)
        {
            using (InfantryDataContext db = zone._server.getContext())
            {
                switch (pkt.type)
                {
                    case CS_ChartQuery<Zone>.ChartType.chatchart:
                        {
                            var zpKvp = zone._server._zones.SelectMany(z => z._players).FirstOrDefault(p => p.Value.alias == pkt.alias);

                            if (zpKvp.Equals(default(KeyValuePair<int, Zone.Player>)))
                            {
                                zone._server.sendMessage(zone, pkt.alias, "Critical: Alias not found.");
                                return;
                            }

                            var zonePlayer = zpKvp.Value;

                            var results = new List<Tuple<string, Zone.Player>>();

                            foreach (var chat in zonePlayer.chats)
                            {
                                foreach (Zone z in zone._server._zones)
                                {
                                    foreach (var kvpPlayer in z._players)
                                    {
                                        var zp = kvpPlayer.Value;

                                        if (zp == null || zp.alias == zonePlayer.alias)
                                        {
                                            continue;
                                        }

                                        if (zp.chats.Contains(chat, StringComparer.OrdinalIgnoreCase))
                                        {
                                            results.Add(Tuple.Create(chat, zp));
                                        }
                                    }
                                }
                            }

                            SC_ChartResponse<Zone> respond = new SC_ChartResponse<Zone>();
                            respond.alias = pkt.alias;
                            respond.type = CS_ChartQuery<Zone>.ChartType.chatchart;
                            respond.title = pkt.title;
                            respond.columns = pkt.columns;

                            foreach(var p in results)
                            {
                                var arenaName = p.Item2.arena;

                                if (p.Item2.arena.StartsWith("#") && zonePlayer.arena != p.Item2.arena)
                                {
                                    arenaName = "(private)";
                                }

                                var row = String.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\" \"", p.Item2.alias, p.Item2.zone._zone.name, arenaName, p.Item1);

                                respond.rows.Add(row);
                            }

                            zone._client.sendReliable(respond, 1);
                        }
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
            CS_ChatQuery<Zone>.Handlers += Handle_CS_ChatQuery;
            CS_Squads<Zone>.Handlers += Handle_CS_SquadQuery;
            CS_ChartQuery<Zone>.Handlers += Handle_CS_ChartQuery;
        }
    }
}
