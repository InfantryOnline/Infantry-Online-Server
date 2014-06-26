using System;
using System.Collections.Generic;
using System.Linq;

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
                        {
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
                                TimeSpan timeplayed = TimeSpan.FromMinutes(alias.timeplayed);
                                days = (int)timeplayed.Days;
                                hrs = (int)timeplayed.Hours;
                                mins = (int)timeplayed.Minutes;

                                total += alias.timeplayed;

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
                        }
                        break;

                    case CS_Query<Zone>.QueryType.whois:
                        {
                            zone._server.sendMessage(zone, pkt.sender, "&Whois Information");
                            zone._server.sendMessage(zone, pkt.sender, "*" + pkt.payload);

                            //Query for an IP?
                            System.Net.IPAddress ip;
                            IQueryable<Data.DB.alias> aliases;

                            /* Old way
                            if (pkt.payload.Contains('.') && System.Net.IPAddress.TryParse(pkt.payload, out ip))
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

                            if (aliases.Count() > 0)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "&Aliases: " + aliases.Count());
                                //Loop through them and display
                                foreach (var alias in aliases)
                                    zone._server.sendMessage(zone, pkt.sender, String.Format("*[{0}] {1} (IP={2} Created={3} LastAccess={4})", alias.account, alias.name, alias.IPAddress, alias.creation.ToString(), alias.lastAccess.ToString()));
                            }
                            else
                            {
                                //Didnt find any, lets try just a contains method
                                if (!pkt.payload.Contains('.'))
                                {   //Alias Contains
                                    IQueryable<Data.DB.alias> args = db.alias.Where(w => w.name.Contains(pkt.payload));
                                    if (args.Count() > 0)
                                        foreach (var alias in args)
                                            zone._server.sendMessage(zone, pkt.sender, String.Format("*[{0}] {1} (IP={2} Created={3} LastAccess={4})", alias.account, alias.name, alias.IPAddress, alias.creation.ToString(), alias.lastAccess.ToString()));
                                }
                            }
                            */

                            //Are we using wildcards?
                            if (!pkt.payload.Contains('*') || pkt.payload.Length > pkt.payload.IndexOf('*'))
                            {   //No we aren't, treat this as general matching
                                //IP Lookup?
                                if (pkt.payload.Contains('.') && System.Net.IPAddress.TryParse(pkt.payload, out ip))
                                    aliases = db.alias.Where(a => a.IPAddress.Equals(ip.ToString()));
                                else
                                {   //Alias
                                    Data.DB.alias who = db.alias.SingleOrDefault(a => a.name.Equals(pkt.payload));
                                    aliases = db.alias.Where(a => a.account.Equals(who.account));
                                }

                                if (aliases.Count() > 0)
                                {
                                    zone._server.sendMessage(zone, pkt.sender, "&Aliases: " + aliases.Count());
                                    //Loop and display
                                    foreach (var alias in aliases)
                                        zone._server.sendMessage(zone, pkt.sender, String.Format("*[{0}] {1} (IP={2} Created={3} LastAccess={4})",
                                            alias.account, alias.name, alias.IPAddress, alias.creation.ToString(), alias.lastAccess.ToString()));
                                }
                                else
                                    zone._server.sendMessage(zone, pkt.sender, "That IP or alias doesn't exist.");
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
                                    //Failed, must be an alias
                                    aliases = db.alias.Where(w => w.name.Contains(pkt.payload.TrimEnd('*')));
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
                                //Alias Wildcard Lookup
                                aliases = db.alias.Where(w => w.name.Contains(pkt.payload.TrimEnd('*')));

                            if (aliases.Count() > 0)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "&Aliases: " + aliases.Count());
                                //Loop and display
                                foreach (var alias in aliases)
                                    zone._server.sendMessage(zone, pkt.sender, String.Format("*[{0}] {1} (IP={2} Created={3} LastAccess={4})",
                                        alias.account, alias.name, alias.IPAddress, alias.creation.ToString(), alias.lastAccess.ToString()));
                            }
                            else
                                zone._server.sendMessage(zone, pkt.sender, "No matches found for the given string.");
                        }
                        break;

                    case CS_Query<Zone>.QueryType.emailupdate:
                        {
                            zone._server.sendMessage(zone, pkt.sender, "&Email Update");

                            Data.DB.account account = db.alias.SingleOrDefault(a => a.name.Equals(pkt.sender)).account1;

                            //Update his email
                            account.email = pkt.payload;
                            db.SubmitChanges();
                            zone._server.sendMessage(zone, pkt.sender, "*Email updated to: " + pkt.payload);
                        }
                        break;

                    case CS_Query<Zone>.QueryType.find:
                        {
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
                                        String.Format("*Found: {0} (Zone: {1}) (Arena:{2})", //TODO: Arena??
                                        result.Value.alias, result.Value.zone._zone.name, result.Value.arena));
                                }
                            }
                            else if (pkt.payload.Length < minlength)
                                zone._server.sendMessage(zone, pkt.sender, "Search query must contain at least " + minlength + " characters");
                            else
                                zone._server.sendMessage(zone, pkt.sender, "Sorry, we couldn't locate any players online by that alias");
                        }
                        break;

                    case CS_Query<Zone>.QueryType.online:
                        {
                            DBServer server = zone._server;

                            foreach (Zone z in zone._server._zones)
                            {
                                server.sendMessage(zone, pkt.sender, String.Format("~Server={0} Players={1}", z._zone.name, z._players.Count()));
                            }
                            zone._server.sendMessage(zone, pkt.sender, String.Format("Infantry (Total={0}) (Peak={1})", server._players.Count(), server.playerPeak));
                        }
                        break;

                    case CS_Query<Zone>.QueryType.zonelist:
                        {
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
                        }
                        break;

                    case CS_Query<Zone>.QueryType.history:
                        {
                            //TODO: rework this to make it accurate for sender lookups
                            string[] name = pkt.payload.Split(':');
                            int page = (!pkt.payload.Contains(':') ? Convert.ToInt32(name[0].Trim()) : Convert.ToInt32(name[1]));
                            int resultsperpage = 30;

                            zone._server.sendMessage(zone, pkt.sender, "!Command History (" + page + ")");

                            //Find all commands!
                            Data.DB.history last;
                            if (pkt.payload.Contains(':'))
                                last = (from hist in db.histories
                                        where hist.sender.ToLower().Equals(name[0].ToLower())
                                        orderby hist.id descending
                                        select hist).ToList().First();
                            else
                                last = (db.histories.OrderByDescending(a => a.id).ToList()).First();

                            List<Data.DB.history> cmds;
                            //If less then 30 results, just show what we have
                            if (last.id <= resultsperpage)
                                cmds = db.histories.Where(c => c.id <= last.id).ToList();
                            else
                                cmds = db.histories.Where(c =>
                                    c.id >= (last.id - (resultsperpage * (page + 1))) &&
                                    c.id < (last.id - (resultsperpage * page))).ToList();

                            //List them
                            foreach (Data.DB.history h in cmds)
                                zone._server.sendMessage(zone, pkt.sender, String.Format("!{0} [{1}:{2}] {3}> :{4}: {5}",
                                    Convert.ToString(h.date), h.zone, h.arena, h.sender, h.recipient, h.command));

                            zone._server.sendMessage(zone, pkt.sender, "End of page, use *history 1, *history 2, etc to navigate previous pages");
                        }
                        break;

                    case CS_Query<Zone>.QueryType.global:
                        foreach(Zone z in zone._server._zones)
                            z._server.sendMessage(z, "*", pkt.payload);
                        break;

                    case CS_Query<Zone>.QueryType.ban:
                        {
                            if (pkt.payload == "")
                                return;

                            Logic_Bans.Ban.BanType type = Logic_Bans.Ban.BanType.None;
                            DateTime expires;
                            DateTime created;
                            string reason;
                            bool found = false;

                            System.Net.IPAddress ipaddress;
                            IQueryable<Data.DB.alias> aliases;

                            //Check for an ip lookup first
                            if (pkt.payload.Contains('.') && System.Net.IPAddress.TryParse(pkt.payload, out ipaddress))
                                aliases = db.alias.Where(a => a.IPAddress.Equals(ipaddress.ToString()));
                            //Alias!
                            else
                            {
                                Data.DB.alias who = db.alias.SingleOrDefault(a => a.name.Equals(pkt.payload));
                                aliases = db.alias.Where(a => a.account.Equals(who.account));
                            }

                            zone._server.sendMessage(zone, pkt.sender, "Current Bans for player");
                            if (aliases.Count() > 0)
                            {
                                foreach (Data.DB.alias what in aliases)
                                {
                                    foreach (Data.DB.ban b in db.bans.Where(b =>
                                        b.account == what.account1.id ||
                                        b.IPAddress == what.account1.IPAddress))
                                    {
                                        //Is it the correct zone?
                                        if (b.zone != null && (b.type == (int)Logic_Bans.Ban.BanType.ZoneBan && b.zone != zone._zone.id))
                                            continue;

                                        //Find all bans for each alias
                                        if (b.type > (int)Logic_Bans.Ban.BanType.None)
                                        {   
                                            expires = b.expires;
                                            type = (Logic_Bans.Ban.BanType)b.type;
                                            created = b.created;
                                            reason = b.reason;
                                            found = true;
                                            zone._server.sendMessage(zone, pkt.sender, String.Format("Alias: {0} Type: {1} Created: {2} Expires: {3} Reason: {4}", what.name, type, Convert.ToString(created), Convert.ToString(expires), reason));
                                        }
                                    }
                                }
                            }

                            if (!found)
                                zone._server.sendMessage(zone, pkt.sender, "None");
                        }
                        break;

                    case CS_Query<Zone>.QueryType.helpcall:
                        {
                            int pageNum = Convert.ToInt32(pkt.payload);
                            int resultseachpage = 30;

                            zone._server.sendMessage(zone, pkt.sender, "!Command Help History (" + pageNum + ")");

                            //Find all commands!
                            Data.DB.helpcall end = (db.helpcalls.OrderByDescending(a => a.id).ToList()).First();
                            List<Data.DB.helpcall> helps;

                            //Check the results first
                            if (end.id <= resultseachpage)
                                helps = db.helpcalls.Where(e => e.id <= end.id).ToList();
                            else
                                helps = db.helpcalls.Where(e =>
                                    e.id >= (end.id - (resultseachpage * (pageNum + 1))) &&
                                    e.id < (end.id - (resultseachpage * pageNum))).ToList();

                            //List them
                            foreach (Data.DB.helpcall h in helps)
                                zone._server.sendMessage(zone, pkt.sender, String.Format("!{0} [{1}:{2}] {3}> {4}",
                                    Convert.ToString(h.date), h.zone, h.arena, h.sender, h.reason));

                            zone._server.sendMessage(zone, pkt.sender, "End of page, use *helpcall 1, *helpcall 2, etc to navigate previous pages");
                        }
                        break;

                    case CS_Query<Zone>.QueryType.alert:
                        {
                            string pAlias;
                            foreach (Zone z in zone._server._zones)
                                foreach (KeyValuePair<int, Zone.Player> player in z._players)
                                {
                                    pAlias = player.Value.alias;
                                    Data.DB.alias check = db.alias.SingleOrDefault(a => a.name.Equals(pAlias));
                                    if ((check != null) && check.account1.permission > 0 && player.Value.alias.Equals(check.name))
                                        z._server.sendMessage(player.Value.zone, player.Value.alias, pkt.payload);
                                    if (player.Value.permission > (int)Data.PlayerPermission.Normal)
                                        z._server.sendMessage(player.Value.zone, player.Value.alias, pkt.payload);
                                }
                        }
                        break;

                    case CS_Query<Zone>.QueryType.modChat:
                        {
                            if (String.IsNullOrEmpty(pkt.payload))
                                return;

                            string pAlias;
                            foreach(Zone z in zone._server._zones)
                                foreach (KeyValuePair<int, Zone.Player> Player in z._players)
                                {
                                    pAlias = Player.Value.alias;
                                    var alias = db.alias.SingleOrDefault(p => p.name.Equals(pAlias));
                                    if (alias == null)
                                        continue;
                                    if (alias.name == pkt.sender)
                                        continue;
                                    var player = db.players.FirstOrDefault(plr => plr.alias1 == alias);
                                    if (player != null)
                                    {
                                        if ((alias.account1.permission > 0)
                                            || (player.zone == z._zone.id && player.permission > 0))
                                            z._server.sendMessage(Player.Value.zone, Player.Value.alias, pkt.payload);
                                    }
                                }
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
            using (InfantryDataContext db = zone._server.getContext())
            {
                //Get the associated player making the command
                Data.DB.player dbplayer = db.zones.First(z => z.id == zone._zone.id).players.First(p => p.alias1.name == pkt.alias);

                switch (pkt.queryType)
                {   //Differentiate the type of query
                    case CS_Squads<Zone>.QueryType.create:
                        {
                            //Sanity checks
                            if (dbplayer.squad != null)
                            {   //traitor is already in a squad
                                zone._server.sendMessage(zone, pkt.alias, "You cannot create a squad if you are already in one (" + dbplayer.squad1.name + ")");
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
                                zone._server.sendMessage(zone, pkt.alias, "Invalid squad name, must start with a letter or number and be less than 32 characters long");
                                return;
                            }

                            if (db.squads.Where(s => s.name == squadname && s.zone == zone._zone.id).Count() > 0)
                            {   //This squad already exists!
                                zone._server.sendMessage(zone, pkt.alias, "A squad with specified name already exists");
                                return;
                            }

                            //Create Some Stats first
                            Data.DB.squadstats stats = new Data.DB.squadstats();
                            stats.kills = 0;
                            stats.deaths = 0;
                            stats.wins = 0;
                            stats.losses = 0;
                            stats.rating = 0;
                            stats.points = 0;

                            db.squadstats.InsertOnSubmit(stats);
                            db.SubmitChanges();

                            //Create the new squad
                            Data.DB.squad newsquad = new Data.DB.squad();

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

                            zone._server.sendMessage(zone, pkt.alias, "Successfully created squad: " + newsquad.name + ". Quit and rejoin to be able to use # to squad chat");
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
                            Data.DB.alias inviteAlias = db.alias.FirstOrDefault(a => a.name == sInvite[1]);
                            Data.DB.player invitePlayer = db.players.FirstOrDefault(p => p.alias1 == inviteAlias && p.zone == dbplayer.zone);
                            if (invitePlayer == null)
                            {   //No such player!
                                zone._server.sendMessage(zone, pkt.alias, "No player found in this zone by that alias");
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
                                zone._server.sendMessage(zone, pkt.alias, "Only squad owners may kick players");
                                return;
                            }

                            //The target player
                            Data.DB.alias kickAlias = db.alias.FirstOrDefault(a => a.name == pkt.payload);
                            Data.DB.player kickPlayer = db.players.FirstOrDefault(p => p.alias1 == kickAlias && p.zone == dbplayer.zone);
                            if (kickPlayer == null)
                            {   //No such player!
                                zone._server.sendMessage(zone, pkt.alias, "No player found in this zone by that alias");
                                return;
                            }

                            if (kickPlayer.squad == null || kickPlayer.squad != dbplayer.squad)
                            {   //Liar!
                                zone._server.sendMessage(zone, pkt.alias, "You may only kick players from your own squad");
                                return;
                            }

                            if (kickPlayer == dbplayer)
                            {   //crazy
                                zone._server.sendMessage(zone, pkt.alias, "You can't kick yourself");
                                return;
                            }

                            //Kick him!
                            kickPlayer.squad = null;
                            zone._server.sendMessage(zone, pkt.alias, "You have kicked " + kickPlayer.alias1.name + " from your squad");
                            zone._server.sendMessage(zone, kickPlayer.alias1.name, "You have been kicked from squad " + dbplayer.squad1.name);
                        }
                        break;

                    case CS_Squads<Zone>.QueryType.transfer:
                        {
                            //Sanity checks
                            if (dbplayer.squad == null || pkt.payload == "")
                                return;

                            if (dbplayer.squad1.owner != dbplayer.id)
                            {
                                zone._server.sendMessage(zone, pkt.alias, "Only squad owners may transfer squad ownership");
                                return;
                            }

                            //The target player
                            Data.DB.alias transferAlias = db.alias.FirstOrDefault(a => a.name.Equals(pkt.payload));
                            Data.DB.player transferPlayer = db.players.FirstOrDefault(p => p.alias1 == transferAlias && p.zone == dbplayer.zone);
                            if (transferPlayer == null || transferPlayer.squad != dbplayer.squad)
                            {   //No such player!
                                zone._server.sendMessage(zone, pkt.alias, "No player found in your squad by that alias");
                                return;
                            }

                            //Transfer ownership to him
                            transferPlayer.squad1.owner = transferPlayer.id;
                            zone._server.sendMessage(zone, pkt.alias, "You have promoted " + transferPlayer.alias1.name + " to squad captain");
                            zone._server.sendMessage(zone, transferPlayer.alias1.name, "You have been promoted to squad captain of " + transferPlayer.squad1.name);
                        }
                        break;

                    case CS_Squads<Zone>.QueryType.leave:
                        {
                            //Sanity checks
                            if (dbplayer.squad == null)
                            {
                                zone._server.sendMessage(zone, pkt.alias, "You aren't in a squad");
                                return;
                            }

                            //Get his squad brothers! (if any...)
                            IQueryable<Data.DB.player> squadmates = db.players.Where(p => p.squad == dbplayer.squad && p.squad != null);

                            //Is he the captain?
                            if (dbplayer.squad1.owner == dbplayer.id)
                            {   //We might need to dissolve the team!
                                if (squadmates.Count() == 1)
                                {   //He's the only one left on the squad... dissolve it!
                                    db.squads.DeleteOnSubmit(dbplayer.squad1);
                                    dbplayer.squad1 = null;
                                    dbplayer.squad = null;
                                    zone._server.sendMessage(zone, pkt.alias, "Your squad has been dissolved");
                                }
                                else
                                {   //There are other people on the squad!
                                    zone._server.sendMessage(zone, pkt.alias, "You can't leave a squad that you're the captain of! Either transfer ownership or kick everybody first");
                                    return;
                                }
                            }
                            else
                            {
                                //Leave the squad...
                                dbplayer.squad1 = null;
                                dbplayer.squad = null;
                                //db.SubmitChanges();
                                zone._server.sendMessage(zone, pkt.alias, "You have left your squad");
                                //Notify his squadmates
                                foreach (Data.DB.player sm in squadmates)
                                    zone._server.sendMessage(zone, sm.alias1.name, pkt.alias + " has left your squad");
                            }
                        }
                        break;

                    case CS_Squads<Zone>.QueryType.dissolve:
                        {
                            //Sanity checks
                            if (dbplayer.squad == null)
                            {
                                zone._server.sendMessage(zone, pkt.alias, "You aren't in a squad");
                                return;
                            }

                            //Get his squad brothers! (if any...)
                            IQueryable<Data.DB.player> squadmates = db.players.Where(p => p.squad == dbplayer.squad && p.squad != null);

                            //Is he the captain?
                            if (dbplayer.squad1.owner == dbplayer.id)
                            {   //We might need to dissolve the team!
                                if (squadmates.Count() == 1)
                                {   //He's the only one left on the squad... dissolve it!
                                    db.squads.DeleteOnSubmit(dbplayer.squad1);
                                    dbplayer.squad1 = null;
                                    dbplayer.squad = null;
                                    zone._server.sendMessage(zone, pkt.alias, "Your squad has been dissolved.");
                                }
                                else
                                {   //There are other people on the squad, lets kick them off
                                    foreach (Data.DB.player P in squadmates.Reverse())
                                    {
                                        if (P.id == dbplayer.id)
                                            continue;

                                        P.squad1 = null;
                                        P.squad = null;
                                        zone._server.sendMessage(zone, P.alias1.name, "Your squad has been dissolved.");
                                    }
                                    //Everyone was kicked, lets dissolve
                                    db.squads.DeleteOnSubmit(dbplayer.squad1);
                                    dbplayer.squad1 = null;
                                    dbplayer.squad = null;
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
                            Data.DB.squad targetSquadOnline;
                            if (cleanPayload == "")
                                targetSquadOnline = db.squads.FirstOrDefault(s => s.id == dbplayer.squad && s.zone == zone._zone.id);
                            else
                                targetSquadOnline = db.squads.FirstOrDefault(s => s.name == cleanPayload && s.zone == zone._zone.id);

                            if (targetSquadOnline == null)
                            {   //No squad found!
                                zone._server.sendMessage(zone, pkt.alias, "No squad found");
                                return;
                            }

                            //List his online squadmates!
                            zone._server.sendMessage(zone, pkt.alias, "&Squad Online List: " + dbplayer.squad1.name);
                            zone._server.sendMessage(zone, pkt.alias, "&Captain: " + db.players.First(p => p.id == dbplayer.squad1.owner).alias1.name);
                            List<string> sonline = new List<string>();
                            foreach (Data.DB.player smate in db.players.Where(p => p.squad == targetSquadOnline.id))
                                //Make sure he's online!
                                if (zone.getPlayer(smate.alias1.name) != null)
                                    sonline.Add(smate.alias1.name);
                            zone._server.sendMessage(zone, pkt.alias, "*" + string.Join(", ", sonline));
                        }
                        break;

                    case CS_Squads<Zone>.QueryType.list:
                        {
                            Data.DB.squad targetSquadList;
                            if (cleanPayload == "")
                                targetSquadList = db.squads.FirstOrDefault(s => s.id == dbplayer.squad && s.zone == zone._zone.id);
                            else
                                targetSquadList = db.squads.FirstOrDefault(s => s.name == cleanPayload && s.zone == zone._zone.id);

                            if (targetSquadList == null)
                            {   //No squad found!
                                zone._server.sendMessage(zone, pkt.alias, "No squad found");
                                return;
                            }

                            //List the squad name, captain, and members!
                            zone._server.sendMessage(zone, pkt.alias, "&Squad List: " + targetSquadList.name);
                            zone._server.sendMessage(zone, pkt.alias, "&Captain: " + db.players.First(p => p.id == targetSquadList.owner).alias1.name);
                            zone._server.sendMessage(zone, pkt.alias, "Players: ");
                            List<string> splayers = new List<string>();
                            foreach (Data.DB.player splayer in db.players.Where(p => p.squad == targetSquadList.id))
                                splayers.Add(splayer.alias1.name);
                            zone._server.sendMessage(zone, pkt.alias, string.Join(", ", splayers));
                        }
                        break;

                    case CS_Squads<Zone>.QueryType.invitessquad:
                        {
                            //Lists the players squads outstanding invitations
                            if (dbplayer.squad == null || dbplayer.squad1.owner != dbplayer.id)
                            {   //No squad found!
                                zone._server.sendMessage(zone, pkt.alias, "You aren't the owner of a squad");
                                return;
                            }
                            zone._server.sendMessage(zone, pkt.alias, "&Outstanding Player Invitations");
                            foreach (KeyValuePair<int, int> invite in zone._server._squadInvites)
                                if (invite.Key == dbplayer.squad)
                                    zone._server.sendMessage(zone, pkt.alias, "*" + db.players.First(p => p.id == invite.Value).alias1.name);
                        }
                        break;

                    case CS_Squads<Zone>.QueryType.invitesplayer:
                        {
                            zone._server.sendMessage(zone, pkt.alias, "&Current Squad Invitations");
                            foreach (KeyValuePair<int, int> invite in zone._server._squadInvites)
                                if (invite.Value == dbplayer.id)
                                    zone._server.sendMessage(zone, pkt.alias, "*" + db.squads.First(s => s.id == invite.Key && s.zone == zone._zone.id).name);
                        }
                        break;

                    case CS_Squads<Zone>.QueryType.invitesreponse:
                        {
                            //Response to a squad invitation
                            string[] sResponse = cleanPayload.Split(':');
                            //Sanity checks
                            if (sResponse.Count() != 2)
                            {   //Invalid syntax
                                zone._server.sendMessage(zone, pkt.alias, "Invalid syntax. Use: ?squadIresponse [accept/reject]:[squadname]");
                                return;
                            }

                            bool bAccept = (sResponse[0].ToLower() == "accept") ? true : false;
                            Data.DB.squad responseSquad = db.squads.FirstOrDefault(s => s.name == sResponse[1] && s.zone == zone._zone.id);
                            KeyValuePair<int, int> responsePair = new KeyValuePair<int, int>((int)responseSquad.id, (int)dbplayer.id);

                            if (responseSquad == null || !zone._server._squadInvites.Contains(responsePair))
                            {   //Either squad doesn't exist... or he's a filthy liar
                                zone._server.sendMessage(zone, pkt.alias, "Invalid squad invitation response");
                                return;
                            }

                            if (bAccept)
                            {   //Acceptance! Get in there, buddy
                                if (dbplayer.squad != null)
                                {
                                    zone._server.sendMessage(zone, pkt.alias, "You can't accept squad invites if you're already in a squad");
                                    return;
                                }

                                //Add him to the squad!
                                dbplayer.squad = responseSquad.id;
                                zone._server.sendMessage(zone, pkt.alias, "You've joined " + dbplayer.squad1.name + "! Quit and rejoin to be able to use # to squad chat");
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
                            Data.DB.squad targetSquad;
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
                                zone._server.sendMessage(zone, pkt.alias, "Squad not found or you are not in a squad");
                            }
                            else
                            {
                                Data.DB.squadstats squadstats = db.squadstats.FirstOrDefault(s => s.squad == targetSquad.id);
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
                                    zone._server.sendMessage(zone, pkt.alias, "This squad has no stats for this zone");
                            }
                        }
                        break;
                }

                //Save our changes to the database!
                db.SubmitChanges();
            }
        }

        /// <summary>
        /// Registers all handlers
        /// </summary>
        [RegistryFunc]
        static public void Register()
        {
            CS_Query<Zone>.Handlers += Handle_CS_Query;
            CS_Squads<Zone>.Handlers += Handle_CS_SquadQuery;
        }
    }
}
