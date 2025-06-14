using System;
using System.Collections.Generic;
using System.Linq;

using InfServer.Protocol;
using InfServer.Data;
using Database.SqlServer;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace InfServer.Logic
{
    class Logic_ChatCommands
    {
        /// <summary>
        /// Handles a query packet
        /// </summary>
        static public void Handle_CS_ChatQuery(CS_ChatQuery<Zone> pkt, Zone zone)
        {
            using (DataContext db = zone._server.getContext())
            {
                switch (pkt.queryType)
                {
                    case CS_ChatQuery<Zone>.QueryType.accountinfo:
                        Handle_CS_ChatQuery_AccountInfo(pkt, zone, db);
                        break;

                    case CS_ChatQuery<Zone>.QueryType.accountignore:
                        Handle_CS_ChatQuery_AccountIgnore(pkt, zone, db);
                        break;

                    case CS_ChatQuery<Zone>.QueryType.whois:
                        Handle_CS_ChatQuery_Whois(pkt, zone, db);
                        break;

                    case CS_ChatQuery<Zone>.QueryType.deletealias:
                        Handle_CS_ChatQuery_DeleteAlias(pkt, zone, db);
                        break;

                    case CS_ChatQuery<Zone>.QueryType.emailupdate:
                        Handle_CS_ChatQuery_EmailUpdate(pkt, zone, db);
                        break;

                    case CS_ChatQuery<Zone>.QueryType.find:
                        Handle_CS_ChatQuery_Find(pkt, zone, db);
                        break;

                    case CS_ChatQuery<Zone>.QueryType.online:
                        Handle_CS_ChatQuery_Online(pkt, zone);
                        break;

                    case CS_ChatQuery<Zone>.QueryType.zonelist:
                        Handle_CS_ChatQuery_ZoneList(pkt, zone);
                        break;

                    case CS_ChatQuery<Zone>.QueryType.history:
                        Handle_CS_ChatQuery_History(pkt, zone, db);
                        break;

                    case CS_ChatQuery<Zone>.QueryType.cmdhistory:
                        Handle_CS_ChatQuery_CommandHistory(pkt, zone, db);
                        break;

                    case CS_ChatQuery<Zone>.QueryType.global:
                        Handle_CS_ChatQuery_GlobalMessage(pkt, zone);
                        break;

                    case CS_ChatQuery<Zone>.QueryType.ban:
                        Handle_CS_ChatQuery_GetBans(pkt, zone, db);
                        break;

                    case CS_ChatQuery<Zone>.QueryType.helpcall:
                        Handle_CS_ChatQuery_GetHelpCalls(pkt, zone, db);
                        break;

                    case CS_ChatQuery<Zone>.QueryType.alert:
                        Handle_CS_ChatQuery_SendAlert(pkt, zone, db);
                        break;

                    case CS_ChatQuery<Zone>.QueryType.modChat:
                        Handle_CS_ChatQuery_ModChat(pkt, zone, db);
                        break;

                    case CS_ChatQuery<Zone>.QueryType.wipe:
                        Handle_CS_ChatQuery_Wipe(pkt, zone, db);
                        break;

                    case CS_ChatQuery<Zone>.QueryType.adminlist:
                        Handle_CS_ChatQuery_AdminList(pkt, zone);
                        break;
                }
            }
        }

        private static void Handle_CS_ChatQuery_AdminList(CS_ChatQuery<Zone> pkt, Zone zone)
        {
            if (string.IsNullOrWhiteSpace(pkt.payload))
            {
                zone._server.sendMessage(zone, pkt.sender, "Payload cannot be empty.");
                return;
            }

            zone._server.sendMessage(zone, pkt.sender, "Current Admin List:");

            if (pkt.payload.Equals("list"))
            {
                zone._server.sendMessage(zone, pkt.sender, Logic_Admins.listAdmins());
            }
        }

        private static void Handle_CS_ChatQuery_Wipe(CS_ChatQuery<Zone> pkt, Zone zone, DataContext db)
        {
            if (String.IsNullOrWhiteSpace(pkt.payload))
            {
                zone._server.sendMessage(zone, pkt.sender, "Invalid payload.");
                return;
            }

            //Get the associated player making the command
            Player dbplayer = db.Players
                .Include(p => p.AliasNavigation)
                .FirstOrDefault(p => p.ZoneId == zone._zone.ZoneId && p.AliasNavigation.Name == pkt.sender);

            if (dbplayer == null)
            {
                zone._server.sendMessage(zone, pkt.sender, "Cannot find your player structure.");
                return;
            }

            Stat stat;
            //Sanity checks
            if (pkt.payload.Equals("all"))
            {
                //Change all stats to zero
                List<Player> players = db.Players
                    .Include(p => p.StatsNavigation)
                    .Where(z => z.ZoneId == dbplayer.ZoneId).ToList();

                if (players.Count == 0)
                {
                    zone._server.sendMessage(zone, pkt.sender, "Cannot find any players attached to this zone.");
                    return;
                }

                foreach (var P in players)
                {
                    P.Inventory = null;
                    P.Skills = null;

                    stat = P.StatsNavigation;

                    stat.Cash = 0;
                    stat.Experience = 0;
                    stat.ExperienceTotal = 0;
                    stat.Kills = 0;
                    stat.Deaths = 0;
                    stat.KillPoints = 0;
                    stat.DeathPoints = 0;
                    stat.AssistPoints = 0;
                    stat.BonusPoints = 0;
                    stat.VehicleKills = 0;
                    stat.VehicleDeaths = 0;
                    stat.PlaySeconds = 0;

                    stat.Zonestat1 = 0;
                    stat.Zonestat2 = 0;
                    stat.Zonestat3 = 0;
                    stat.Zonestat4 = 0;
                    stat.Zonestat5 = 0;
                    stat.Zonestat6 = 0;
                    stat.Zonestat7 = 0;
                    stat.Zonestat8 = 0;
                    stat.Zonestat9 = 0;
                    stat.Zonestat10 = 0;
                    stat.Zonestat11 = 0;
                    stat.Zonestat12 = 0;
                }

                db.SaveChanges();
                zone._server.sendMessage(zone, pkt.sender, "Wipe all has been completed.");
                return;
            }

            //Recipient lookup
            Alias recipientAlias = db.Aliases.FirstOrDefault(a => a.Name == pkt.payload);

            Player recipientPlayer = db.Players
                .Include(p => p.StatsNavigation)
                .FirstOrDefault(p => p.AliasId == recipientAlias.AliasId && p.ZoneId == dbplayer.ZoneId);

            if (recipientPlayer == null)
            {
                zone._server.sendMessage(zone, pkt.sender, "No such alias to wipe.");
                return;
            }

            recipientPlayer.Skills = null;
            recipientPlayer.Inventory = null;

            //Change all stats to zero
            stat = recipientPlayer.StatsNavigation;

            stat.Cash = 0;
            stat.Experience = 0;
            stat.ExperienceTotal = 0;
            stat.Kills = 0;
            stat.Deaths = 0;
            stat.KillPoints = 0;
            stat.DeathPoints = 0;
            stat.AssistPoints = 0;
            stat.BonusPoints = 0;
            stat.VehicleKills = 0;
            stat.VehicleDeaths = 0;
            stat.PlaySeconds = 0;

            stat.Zonestat1 = 0;
            stat.Zonestat2 = 0;
            stat.Zonestat3 = 0;
            stat.Zonestat4 = 0;
            stat.Zonestat5 = 0;
            stat.Zonestat6 = 0;
            stat.Zonestat7 = 0;
            stat.Zonestat8 = 0;
            stat.Zonestat9 = 0;
            stat.Zonestat10 = 0;
            stat.Zonestat11 = 0;
            stat.Zonestat12 = 0;

            db.SaveChanges();
            zone._server.sendMessage(zone, pkt.sender, "Character wipe has been completed.");
        }

        private static void Handle_CS_ChatQuery_ModChat(CS_ChatQuery<Zone> pkt, Zone zone, DataContext db)
        {
            if (String.IsNullOrEmpty(pkt.payload))
            {
                return;
            }

            foreach (KeyValuePair<int, Zone.Player> Player in zone._players)
            {
                string pAlias = Player.Value.alias;

                var alias = db.Aliases
                    .Include(a => a.AccountNavigation)
                    .SingleOrDefault(p => p.Name == pAlias);

                if (alias == null || alias.Name == pkt.sender)
                {
                    continue;
                }

                if (alias.AccountNavigation.Permission > 0) //Are they a global mod?
                {
                    zone._server.sendMessage(Player.Value.zone, Player.Value.alias, pkt.payload);
                }
                else // No, check dev powers
                {
                    var player = db.Players
                        .FirstOrDefault(p => p.ZoneId == zone._zone.ZoneId && p.AliasId == alias.AliasId);

                    if (player != null && player.Permission > 0)
                        zone._server.sendMessage(Player.Value.zone, Player.Value.alias, pkt.payload);
                }
            }
        }

        private static void Handle_CS_ChatQuery_SendAlert(CS_ChatQuery<Zone> pkt, Zone zone, DataContext db)
        {
            string pAlias;
            foreach (Zone z in zone._server._zones)
                foreach (KeyValuePair<int, Zone.Player> player in z._players)
                {
                    pAlias = player.Value.alias;
                    Alias check = db.Aliases
                        .Include(p => p.AccountNavigation)
                        .SingleOrDefault(a => a.Name == pAlias);

                    if ((check != null) && check.AccountNavigation.Permission > 0 && player.Value.alias == check.Name)
                        z._server.sendMessage(player.Value.zone, player.Value.alias, pkt.payload);
                    if (player.Value.permission > (int)Data.PlayerPermission.Normal)
                        z._server.sendMessage(player.Value.zone, player.Value.alias, pkt.payload);
                }
        }

        private static void Handle_CS_ChatQuery_GetHelpCalls(CS_ChatQuery<Zone> pkt, Zone zone, DataContext db)
        {
            int pageNum = Convert.ToInt32(pkt.payload);
            int resultseachpage = 30;

            zone._server.sendMessage(zone, pkt.sender, "!Command Help History (" + pageNum + ")");

            //Find all commands!
            Helpcall end = db.Helpcalls.OrderByDescending(a => a.HelpCallId).First();
            List<Helpcall> helps;

            //Check the results first
            if (end.HelpCallId <= resultseachpage)
                helps = db.Helpcalls.Where(e => e.HelpCallId <= end.HelpCallId).ToList();
            else
                helps = db.Helpcalls.Where(e =>
                    e.HelpCallId >= (end.HelpCallId - (resultseachpage * (pageNum + 1))) &&
                    e.HelpCallId < (end.HelpCallId - (resultseachpage * pageNum))).ToList();

            //List them
            foreach (Helpcall h in helps)
            {
                zone._server.sendMessage(zone, pkt.sender, string.Format("!{0} [{1}:{2}] {3}> {4}",
                    Convert.ToString(h.Date), h.Zone, h.Arena, h.Sender, h.Reason));
            }

            zone._server.sendMessage(zone, pkt.sender, "End of page, use *helpcall 1, *helpcall 2, etc to navigate previous pages");
        }

        private static void Handle_CS_ChatQuery_GetBans(CS_ChatQuery<Zone> pkt, Zone zone, DataContext db)
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
            IQueryable<Alias> dbAliases;

            zone._server.sendMessage(zone, pkt.sender, "Current Bans for player");

            //Check for an ip lookup first
            if (pkt.payload.Contains('.') && System.Net.IPAddress.TryParse(pkt.payload, out ipaddress))
            {
                dbAliases = db.Aliases
                    .Include(a => a.AccountNavigation)
                    .Where(a => a.IpAddress == ipaddress.ToString());
            }
            //Check for an account id
            else if (pkt.payload.StartsWith("#") && Int64.TryParse(pkt.payload.TrimStart('#'), out accountID))
            {
                dbAliases = db.Aliases
                    .Include(a => a.AccountNavigation)
                    .Where(a => a.AccountId == accountID);
            }
            //Alias!
            else
            {
                Alias who = db.Aliases
                    .Include(a => a.AccountNavigation)
                    .SingleOrDefault(a => a.Name == pkt.payload);

                if (who == null)
                {
                    zone._server.sendMessage(zone, pkt.sender, "None");
                    return;
                }

                dbAliases = db.Aliases
                    .Include(a => a.AccountNavigation)
                    .Where(a => a.AccountId == who.AccountId);
            }

            if (dbAliases != null)
            {
                //
                // Materialize to prevent Open Data Reader exception.
                //
                var aliases = dbAliases
                    .Select(a => new { AliasName = a.Name, AccountIp = a.AccountNavigation.IpAddress, AccountId = a.AccountNavigation.AccountId })
                    .ToList();

                foreach (var alias in aliases)
                {
                    var dbBans = db.Bans
                        .Where(b => b.Name == alias.AliasName && (b.AccountId == alias.AccountId || b.IpAddress == alias.AccountIp))
                        .ToList();

                    foreach (var b in dbBans)
                    {
                        //Is it the correct zone?
                        if (b.ZoneId != null && (b.Type == (int)Logic_Bans.Ban.BanType.ZoneBan && b.ZoneId != zone._zone.ZoneId))
                            continue;

                        //Find all bans for each alias
                        if (b.Type > (int)Logic_Bans.Ban.BanType.None)
                        {
                            expires = b.Expires.Value;
                            type = (Logic_Bans.Ban.BanType)b.Type;
                            created = b.Created;
                            reason = b.Reason;
                            found = true;
                            zone._server.sendMessage(zone, pkt.sender, string.Format("Alias: {0} Type: {1} Created: {2} Expires: {3} Reason: {4}", alias.AliasName, type, Convert.ToString(created), Convert.ToString(expires), reason));
                        }
                    }
                }
            }

            if (!found)
                zone._server.sendMessage(zone, pkt.sender, "None");
        }

        private static void Handle_CS_ChatQuery_GlobalMessage(CS_ChatQuery<Zone> pkt, Zone zone)
        {
            foreach (Zone z in zone._server._zones)
                z._server.sendMessage(z, "*", pkt.payload);
        }

        private static void Handle_CS_ChatQuery_CommandHistory(CS_ChatQuery<Zone> pkt, Zone zone, DataContext db)
        {
            const int resultsPerPage = 30;
            string[] args = pkt.payload.Split(':');
            string cmd = args[0];
            int page = Convert.ToInt32(args[1]);

            zone._server.sendMessage(zone, pkt.sender, "Command History (" + (page + 1) + ")"); //We use + 1 because indexing starts at 0

            cmd = cmd.ToLower();

            var commandHistory =
                db.Histories.Where(h => h.Command.ToLower().Contains(cmd))
                    .OrderByDescending(hist => hist.HistoryId)
                    .Skip(page * resultsPerPage)
                    .Take(resultsPerPage)
                    .ToList();

            //List them
            foreach (var h in commandHistory)
            {
                zone._server.sendMessage(zone, pkt.sender, string.Format("!{0} [{1}:{2}] {3}> :{4}: {5}",
                    Convert.ToString(h.Date), h.Zone, h.Arena, h.Sender, h.Recipient, h.Command));
            }
            zone._server.sendMessage(zone, pkt.sender, "End of page, use *cmdhistory 2, *cmdhistory 3, etc to navigate full history OR *cmdhistory cmd:2 *cmdhistory cmd:3 for command filtering.");
        }

        private static void Handle_CS_ChatQuery_History(CS_ChatQuery<Zone> pkt, Zone zone, DataContext db)
        {
            const int resultsPerPage = 30;
            string[] args = pkt.payload.Split(':');
            string name = args[0];
            int page = Convert.ToInt32(args[1]);
            bool emptyName = string.IsNullOrWhiteSpace(name);

            zone._server.sendMessage(zone, pkt.sender, "Command History (" + (page + 1) + ")"); //We use + 1 because indexing starts at 0

            List<History> commandHistory =
                db.Histories.Where(hist => emptyName || hist.Sender.ToLower() == name.ToLower())
                    .OrderByDescending(hist => hist.HistoryId)
                    .Skip(page * resultsPerPage)
                    .Take(resultsPerPage)
                    .ToList();

            //List them
            foreach (History h in commandHistory)
            {
                zone._server.sendMessage(zone, pkt.sender, string.Format("!{0} [{1}:{2}] {3}> :{4}: {5}",
                    Convert.ToString(h.Date), h.Zone, h.Arena, h.Sender, h.Recipient, h.Command));
            }
            zone._server.sendMessage(zone, pkt.sender, "End of page, use *history 2, *history 3, etc to navigate pages OR *history alias:2 *history alias:3 for aliases.");
        }

        private static void Handle_CS_ChatQuery_ZoneList(CS_ChatQuery<Zone> pkt, Zone zone)
        {
            //Collect the list of zones and send it over
            List<ZoneInstance> zoneList = new List<ZoneInstance>();

            try
            {
                foreach (Zone z in zone._server._zones.Where((Func<Zone, bool>)(zn => zn._zone.Active == 1)))
                {
                    int playercount = z._players.Count;

                    //Invert player count of our current zone
                    if (z._zone.Port == Convert.ToInt32(pkt.payload))
                    {
                        playercount = -z._players.Count;
                    }

                    //Add it to our list
                    zoneList.Add(new ZoneInstance(0,
                        z._zone.Name,
                        z._zone.Ip,
                        Convert.ToInt16(z._zone.Port),
                        playercount));
                }
            }
            catch (Exception e)
            {
                Log.write(TLog.Warning, e.ToString());
                zone._server.sendMessage(zone, pkt.sender, "Internal server error, could not generate the zonelist.");
                return;
            }
            SC_Zones<Zone> zl = new SC_Zones<Zone>();
            zl.requestee = pkt.sender;
            zl.zoneList = zoneList;
            zone._client.sendReliable(zl, 1);
        }

        private static void Handle_CS_ChatQuery_Online(CS_ChatQuery<Zone> pkt, Zone zone)
        {
            DBServer server = zone._server;

            foreach (Zone z in zone._server._zones)
            {
                if (z._players.Count() < 1)
                    continue;
                server.sendMessage(zone, pkt.sender, string.Format("~Server={0} Players={1}", z._zone.Name, z._players.Where(p => !p.Value.stealth).Count()));
            }
            zone._server.sendMessage(zone, pkt.sender, string.Format("Infantry (Total={0}) (Peak={1})", server._players.Where(p => !p.Value.stealth).Count(), server.playerPeak));
        }

        private static void Handle_CS_ChatQuery_Find(CS_ChatQuery<Zone> pkt, Zone zone, DataContext db)
        {
            int minlength = 3;
            var results = new List<KeyValuePair<string, Zone.Player>>();
            
            Player pPlayer = db.Players
                .Include(p => p.AliasNavigation)
                    .ThenInclude(p => p.AccountNavigation)
                .First(p => p.AliasNavigation.Name == pkt.sender);

            var pAccount = pPlayer.AliasNavigation.AccountNavigation;

            foreach (KeyValuePair<string, Zone.Player> player in zone._server._players)
            {
                if (player.Value.stealth && player.Value.permission > pPlayer.Permission)
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
                    if (pAccount.Permission < 1 && result.Value.arena.StartsWith("#"))
                    {
                        //We are, is this the same zone?
                        if (result.Value.zone._zone.ZoneId == zone._zone.ZoneId)
                        {
                            //It is, get the info needed
                            var find = db.Players
                                .First(p => p.ZoneId == zone._zone.ZoneId && p.AliasId == result.Value.aliasid);

                            //Are we on the same squad?
                            if (find.SquadId != pPlayer.SquadId || (find.SquadId == null && pPlayer.SquadId == null))
                                zone._server.sendMessage(zone, pkt.sender,
                                    string.Format("*Found: {0} (Zone: {1}) (Arena:{2})",
                                    result.Value.alias, result.Value.zone._zone.Name, "Hidden"));
                            else
                                zone._server.sendMessage(zone, pkt.sender,
                                    string.Format("*Found: {0} (Zone: {1}) (Arena:{2})",
                                    result.Value.alias, result.Value.zone._zone.Name, result.Value.arena));
                        }
                        else
                            zone._server.sendMessage(zone, pkt.sender,
                                string.Format("*Found: {0} (Zone: {1}) (Arena:{2})",
                                result.Value.alias, result.Value.zone._zone.Name, "Hidden"));
                    }
                    else
                        zone._server.sendMessage(zone, pkt.sender,
                            string.Format("*Found: {0} (Zone: {1}) (Arena:{2})",
                            result.Value.alias, result.Value.zone._zone.Name, result.Value.arena));
                }
            }
            else if (pkt.payload.Length < minlength)
                zone._server.sendMessage(zone, pkt.sender, "Search query must contain at least " + minlength + " characters");
            else
                zone._server.sendMessage(zone, pkt.sender, "Sorry, we couldn't locate any players online by that alias");
        }

        private static void Handle_CS_ChatQuery_EmailUpdate(CS_ChatQuery<Zone> pkt, Zone zone, DataContext db)
        {
            zone._server.sendMessage(zone, pkt.sender, "&Email Update");

            Account account = db.Aliases
                .Include(a => a.AccountNavigation)
                .SingleOrDefault(a => a.Name == pkt.sender).AccountNavigation;

            //Update his email
            account.Email = pkt.payload;
            db.SaveChanges();
            zone._server.sendMessage(zone, pkt.sender, "*Email updated to: " + pkt.payload);
        }

        private static void Handle_CS_ChatQuery_DeleteAlias(CS_ChatQuery<Zone> pkt, Zone zone, DataContext db)
        {
            if (string.IsNullOrWhiteSpace(pkt.payload))
            {
                zone._server.sendMessage(zone, pkt.sender, "Wrong format typed.");
                return;
            }

            Alias sender = db.Aliases.FirstOrDefault(sndr => sndr.Name == pkt.sender);
            if (sender == null)
                return;

            //Single alias
            if (!pkt.payload.Contains(','))
            {
                //Lets get all account related info then delete it
                Alias palias = db.Aliases.FirstOrDefault(a => a.Name == pkt.payload);
                if (palias == null)
                {
                    zone._server.sendMessage(zone, pkt.sender, "Cannot find the specified alias.");
                    return;
                }

                //First and most important, check to see if this alias is on the account
                if (palias.AccountId != sender.AccountId)
                {
                    zone._server.sendMessage(zone, pkt.sender, "You must be on the account that this alias belongs to.");
                    return;
                }

                var players = db.Players
                    .Include(p => p.SquadNavigation)
                    .Where(t => t.AliasId == palias.AliasId).ToList();

                foreach (var player in players)
                {
                    //Check for a squad
                    if (player.SquadId != null)
                    {
                        if (player.SquadNavigation.OwnerPlayerId == player.PlayerId)
                        {
                            var squadmates = db.Players
                                .Include(p => p.SquadNavigation)
                                .Where(p => p.ZoneId == player.ZoneId
                                     && p.SquadId == player.SquadId
                                     && p.PlayerId != player.PlayerId).ToList();

                            if (squadmates.Count > 0)
                            {
                                var sq = squadmates.First();

                                //Since the player is the owner, lets just give it to someone else
                                sq.SquadNavigation.OwnerPlayerId = sq.PlayerId;
                            }
                            else
                            {
                                //Lets delete the squad
                                db.Squads.Remove(player.SquadNavigation);
                            }
                        }

                        player.SquadNavigation = null;
                        player.SquadId = null;
                    }

                    // Remove the historic stuff too.
                    var dailies = db.StatsDailies.Where(s => s.PlayerId == player.PlayerId);
                    var weeklies = db.StatsWeeklies.Where(s => s.PlayerId == player.PlayerId);
                    var monthlies = db.StatsMonthlies.Where(s => s.PlayerId == player.PlayerId);
                    var yearlies = db.StatsYearlies.Where(s => s.PlayerId == player.PlayerId);

                    db.StatsDailies.RemoveRange(dailies);
                    db.StatsWeeklies.RemoveRange(weeklies);
                    db.StatsMonthlies.RemoveRange(monthlies);
                    db.StatsYearlies.RemoveRange(yearlies);

                    db.SaveChanges();

                    //
                    // FK is on wrong table; delete Player first and then stats.
                    //

                    db.Players.Remove(player);

                    db.SaveChanges();

                    var stats = db.Stats.Where(s => s.StatId == player.StatsId);
                    db.Stats.RemoveRange(stats);

                    db.SaveChanges();
                }

                db.Aliases.Remove(palias);

                db.SaveChanges();
                zone._server.sendMessage(zone, pkt.sender, "Alias has been deleted.");
            }
            else
            {
                zone._server.sendMessage(zone, pkt.sender, String.Format("Please remove aliases one at a time."));
            }
        }

        private static void Handle_CS_ChatQuery_Whois(CS_ChatQuery<Zone> pkt, Zone zone, DataContext db)
        {
            zone._server.sendMessage(zone, pkt.sender, "&Whois Information");
            zone._server.sendMessage(zone, pkt.sender, "*" + pkt.payload);

            //Query for an IP?
            System.Net.IPAddress ip;
            long accountID;
            List<Alias> aliases;

            //Are we using wildcards?
            if (!pkt.payload.Contains('*'))
            {   //No we aren't, treat this as general matching
                //IP Lookup?
                if (pkt.payload.Contains('.') && System.Net.IPAddress.TryParse(pkt.payload, out ip))
                    aliases = db.Aliases.Where(a => a.IpAddress.Equals(ip.ToString())).ToList();
                else if (pkt.payload.StartsWith("#") && Int64.TryParse(pkt.payload.TrimStart('#'), out accountID))
                {   //Account ID
                    aliases = db.Aliases.Where(a => a.AccountId == accountID).ToList();
                }
                else
                {   //Alias
                    Alias who = db.Aliases.SingleOrDefault(a => a.Name == pkt.payload);
                    if (who == null)
                    {
                        zone._server.sendMessage(zone, pkt.sender, "That IP, account id or alias doesn't exist.");
                        return;
                    }
                    aliases = db.Aliases.Where(a => a.AccountId == who.AccountId).ToList();
                }

                if (aliases != null && aliases.Count() > 0)
                {
                    zone._server.sendMessage(zone, pkt.sender, "&Aliases: " + aliases.Count());
                    foreach (var alias in aliases)
                    {
                        TimeSpan timeplayed = TimeSpan.FromMinutes(alias.TimePlayed);
                        var days = (int)timeplayed.Days;
                        var hrs = (int)timeplayed.Hours;
                        var mins = (int)timeplayed.Minutes;

                        zone._server.sendMessage(zone, pkt.sender, string.Format("*[{0}] {1} (IP={2} Created={3} LastAccess={4} TimePlayed={5}d {6}h {7}m)",
                            alias.AccountId, // 0
                            alias.Name, // 1
                            alias.IpAddress, // 2
                            alias.Creation.ToString(), // 3
                            alias.LastAccess.ToString(), // 4
                            days, // 5
                            hrs, // 6
                            mins) // 7
                            );

                    }
                }
                else
                    zone._server.sendMessage(zone, pkt.sender, "That IP, account id or alias doesn't exist.");

                return;
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
                    aliases = (from w in db.Aliases
                               where EF.Functions.Like(w.Name, trimmed)
                               select w).ToList();
                }
                else
                {   //Validated IP
                    //Ranged ip parser method, looks for wildcard as a string stopping point
                    foreach (string str in IP)
                        if (!str.Trim().Equals("*"))
                            findIP += str.Trim() + ".";

                    aliases = db.Aliases.Where(w => w.IpAddress.Contains(findIP)).ToList();
                }
            }
            else
            {
                //Alias Wildcard Lookup
                string trimmed = pkt.payload.Replace('*', '%');
                aliases = (from w in db.Aliases
                           where EF.Functions.Like(w.Name, trimmed)
                           select w).ToList();
            }

            if (aliases != null && aliases.Count() > 0)
            {
                zone._server.sendMessage(zone, pkt.sender, "&Aliases: " + aliases.Count());
                //Loop and display
                foreach (var alias in aliases)
                {
                    TimeSpan timeplayed = TimeSpan.FromMinutes(alias.TimePlayed);
                    var days = (int)timeplayed.Days;
                    var hrs = (int)timeplayed.Hours;
                    var mins = (int)timeplayed.Minutes;

                    zone._server.sendMessage(zone, pkt.sender, string.Format("*[{0}] {1} (IP={2} Created={3} LastAccess={4} TimePlayed={5}d {6}h {7}m)",
                        alias.AccountId, // 0
                        alias.Name, // 1
                        alias.IpAddress, // 2
                        alias.Creation.ToString(), // 3
                        alias.LastAccess.ToString(), // 4
                        days, // 5
                        hrs, // 6
                        mins) // 7
                        );

                }
            }
            else
                zone._server.sendMessage(zone, pkt.sender, "No matches found for the given string.");
        }

        private static void Handle_CS_ChatQuery_AccountIgnore(CS_ChatQuery<Zone> pkt, Zone zone, DataContext db)
        {
            Alias player = db.Aliases.SingleOrDefault(a => a.Name == pkt.payload);
            if (player != null)
            {
                SC_ChatQuery<Zone> cQuery = new SC_ChatQuery<Zone>();
                cQuery.type = pkt.queryType;
                cQuery.sender = pkt.sender;
                cQuery.payload = string.Format("{0},{1}", player.Name, player.IpAddress);
                zone._client.sendReliable(cQuery);
            }
        }

        private static void Handle_CS_ChatQuery_AccountInfo(CS_ChatQuery<Zone> pkt, Zone zone, DataContext db)
        {
            Alias from = db.Aliases.SingleOrDefault(a => a.Name == pkt.sender);
            var aliases = db.Aliases.Where(a => a.AccountId == from.AccountId);
            zone._server.sendMessage(zone, pkt.sender, "Account Info");

            Int64 total = 0;
            int days = 0;
            int hrs = 0;
            int mins = 0;
            //Loop through each alias to calculate time played
            foreach (var alias in aliases)
            {
                TimeSpan timeplayed = TimeSpan.FromMinutes(alias.TimePlayed);
                days = (int)timeplayed.Days;
                hrs = (int)timeplayed.Hours;
                mins = (int)timeplayed.Minutes;

                total += alias.TimePlayed;

                //Send it
                zone._server.sendMessage(zone, pkt.sender, string.Format("~{0} ({1}d {2}h {3}m)", alias.Name, days, hrs, mins));
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


        /// <summary>
        /// Handles a ?squad command query
        /// </summary>
        static public void Handle_CS_SquadQuery(CS_Squads<Zone> pkt, Zone zone)
        {
            //Clean up the payload to Infantry standards (dont use clean payload for anything involving aliases/player names)
            string cleanPayload = Logic_Chats.CleanIllegalCharacters(pkt.payload);
            switch (pkt.queryType)
            {
                case CS_Squads<Zone>.QueryType.create:
                    CS_Squads_QueryType_CreateSquad(pkt, zone, cleanPayload);
                    break;

                case CS_Squads<Zone>.QueryType.invite:
                    CS_Squads_QueryType_Invite(pkt, zone);
                    break;

                case CS_Squads<Zone>.QueryType.kick:
                    CS_Squads_QueryType_Kick(pkt, zone);
                    break;

                case CS_Squads<Zone>.QueryType.transfer:
                    CS_Squads_QueryType_TransferSquad(pkt, zone);
                    break;

                case CS_Squads<Zone>.QueryType.leave:
                    CS_Squads_QueryType_LeaveSquad(pkt, zone);
                    break;

                case CS_Squads<Zone>.QueryType.dissolve:
                    CS_Squads_QueryType_DissolveSquad(pkt, zone);
                    break;

                case CS_Squads<Zone>.QueryType.online:
                    CS_Squads_QueryType_Online(pkt, zone, cleanPayload);
                    break;

                case CS_Squads<Zone>.QueryType.list:
                    CS_Squads_QueryType_List(pkt, zone, cleanPayload);
                    break;

                case CS_Squads<Zone>.QueryType.invitessquad:
                    CS_Squads_QueryType_SquadInvites(pkt, zone);
                    break;

                case CS_Squads<Zone>.QueryType.invitesplayer:
                    CS_Squads_QueryType_PlayerInvites(pkt, zone, cleanPayload);
                    break;

                case CS_Squads<Zone>.QueryType.invitesreponse:
                    CS_Squads_QueryType_InviteResponse(pkt, zone, cleanPayload);
                    break;

                case CS_Squads<Zone>.QueryType.stats:
                    CS_Squads_QueryType_Stats(pkt, zone);
                    break;
            }
        }

        private static void CS_Squads_QueryType_Stats(CS_Squads<Zone> pkt, Zone zone)
        {
            var player = zone.getPlayer(pkt.alias);

            using var ctx = zone._server.getContext();

            string name = null;
            long? id = null;

            if (!string.IsNullOrWhiteSpace(pkt.payload))
            {
                var found = ctx.Squads.Where(s => s.Name == pkt.payload && s.ZoneId == zone._zone.ZoneId).Select(s => new { s.SquadId, s.Name }).FirstOrDefault();

                if (found != null)
                {
                    id = found.SquadId;
                    name = found.Name;
                }
            }
            else
            {
                if (player.squadid != null)
                {
                    var found = ctx.Squads.Where(s => s.Name == pkt.payload && s.ZoneId == zone._zone.ZoneId).Select(s => new { s.SquadId, s.Name }).FirstOrDefault();

                    id = found.SquadId;
                    name = found.Name;
                }
            }

            if (id == null)
            {
                zone._server.sendMessage(zone, pkt.alias, "Squad not found or you are not in a squad.");
                return;
            }

            else
            {
                SquadStat squadstats = ctx.Squadstats.FirstOrDefault(s => s.SquadId == id);

                if (squadstats != null)
                {
                    zone._server.sendMessage(zone, pkt.alias, String.Format("#~~{0} Stats", name));
                    zone._server.sendMessage(zone, pkt.alias, String.Format("*--Kills={0}", squadstats.Kills));
                    zone._server.sendMessage(zone, pkt.alias, String.Format("*--Deaths={0}", squadstats.Deaths));
                    zone._server.sendMessage(zone, pkt.alias, String.Format("*--Points={0}", squadstats.Points));
                    zone._server.sendMessage(zone, pkt.alias, String.Format("*--Wins={0}", squadstats.Wins));
                    zone._server.sendMessage(zone, pkt.alias, String.Format("*--Losses={0}", squadstats.Losses));
                    zone._server.sendMessage(zone, pkt.alias, String.Format("&--Rating={0}", squadstats.Rating));
                }
                else
                    zone._server.sendMessage(zone, pkt.alias, "This squad has no stats for this zone.");
            }
        }

        private static void CS_Squads_QueryType_InviteResponse(CS_Squads<Zone> pkt, Zone zone, string cleanPayload)
        {
            var player = zone.getPlayer(pkt.alias);
            using var db = zone._server.getContext();
            var dbplayer = db.Players
                .Include(p => p.SquadNavigation)
                .Where(p => p.PlayerId == player.dbid)
                .FirstOrDefault();

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
            Squad responseSquad = db.Squads.FirstOrDefault(s => s.Name == sResponse[1] && s.ZoneId == zone._zone.ZoneId);
            KeyValuePair<int, int> responsePair = new KeyValuePair<int, int>((int)responseSquad.SquadId, (int)dbplayer.PlayerId);

            if (responseSquad == null || !zone._server._squadInvites.Contains(responsePair))
            {   //Either squad doesn't exist... or he's a filthy liar
                zone._server.sendMessage(zone, pkt.alias, "Invalid squad invitation response.");
                return;
            }

            if (bAccept)
            {   //Acceptance! Get in there, buddy
                if (dbplayer.SquadId != null)
                {
                    zone._server.sendMessage(zone, pkt.alias, "You can't accept squad invites if you're already in a squad.");
                    return;
                }

                //Add him to the squad!
                dbplayer.SquadId = responseSquad.SquadId;
                zone._server.sendMessage(zone, pkt.alias, "You've joined " + responseSquad.Name + "! Quit and rejoin to be able to use # to squad chat.");
                zone._server._squadInvites.Remove(responsePair);

                //
                // TODO: Add player to squad chats without them having to rejoin.
                //

                db.SaveChanges();
            }
            else
            {   //He's getting rid of a squad invite...
                zone._server._squadInvites.Remove(responsePair);
                zone._server.sendMessage(zone, pkt.alias, "Revoked squad invitation from " + responseSquad.Name);
            }
        }

        private static void CS_Squads_QueryType_PlayerInvites(CS_Squads<Zone> pkt, Zone zone, string cleanPayload)
        {
            var player = zone.getPlayer(pkt.alias);
            using var db = zone._server.getContext();
            var dbplayer = db.Players
                .Include(p => p.SquadNavigation)
                .Where(p => p.PlayerId == player.dbid)
                .FirstOrDefault();

            //Is the zone asking?
            if (cleanPayload == "zone")
            {
                foreach (KeyValuePair<int, int> invite in zone._server._squadInvites)
                {
                    if (invite.Value == dbplayer.PlayerId)
                    {
                        Squad sq = db.Squads.First(s => s.SquadId == invite.Key && s.ZoneId == zone._zone.ZoneId);
                        if (sq != null)
                            zone._server.sendMessage(zone, pkt.alias, string.Format("You have been invited to join a squad: {0}", sq.Name));
                    }
                }
                return;
            }

            zone._server.sendMessage(zone, pkt.alias, "&Current Squad Invitations:");
            bool found = true;
            foreach (KeyValuePair<int, int> invite in zone._server._squadInvites)
            {
                if (invite.Value == dbplayer.PlayerId)
                {
                    zone._server.sendMessage(zone, pkt.alias, "*" + db.Squads.First(s => s.SquadId == invite.Key && s.ZoneId == zone._zone.ZoneId).Name);
                    found = true;
                }
            }

            if (!found)
                zone._server.sendMessage(zone, pkt.alias, "&None.");
        }

        private static void CS_Squads_QueryType_SquadInvites(CS_Squads<Zone> pkt, Zone zone)
        {
            var player = zone.getPlayer(pkt.alias);

            using var db = zone._server.getContext();

            var dbplayer = db.Players
                .Include(p => p.SquadNavigation)
                .Where(p => p.PlayerId == player.dbid)
                .FirstOrDefault();

            //Lists the players squads outstanding invitations
            if (dbplayer.SquadId == null || dbplayer.SquadNavigation.OwnerPlayerId != dbplayer.PlayerId)
            {   //No squad found!
                zone._server.sendMessage(zone, pkt.alias, "You aren't the owner of a squad.");
                return;
            }
            zone._server.sendMessage(zone, pkt.alias, "&Outstanding Player Invitations:");
            bool found = false;
            foreach (KeyValuePair<int, int> invite in zone._server._squadInvites)
            {
                if (invite.Key == dbplayer.SquadId)
                {
                    zone._server.sendMessage(zone, pkt.alias, "*" + db.Players.First(p => p.PlayerId == invite.Value).AliasNavigation.Name);
                    found = true;
                }
            }

            if (!found)
                zone._server.sendMessage(zone, pkt.alias, "&None.");
        }

        private static void CS_Squads_QueryType_List(CS_Squads<Zone> pkt, Zone zone, string cleanPayload)
        {
            var player = zone.getPlayer(pkt.alias);

            if (player == null)
            {
                return;
            }

            using var ctx = zone._server.getContext();

            //
            // Chain the query. We are using the IQueryable type explicitly due to the chain.
            //

            IQueryable<Squad> results = ctx.Squads.Include(s => s.Players).ThenInclude(p => p.AliasNavigation);

            if (!string.IsNullOrWhiteSpace(cleanPayload))
            {
                results = results.Where(s => s.Name == cleanPayload && s.ZoneId == zone._zone.ZoneId);
            }
            else
            {
                if (player.squadid == null)
                {
                    zone._server.sendMessage(zone, pkt.alias, "No squad found.");
                    return;
                    
                }

                results = results.Where(s => s.SquadId == player.squadid);
            }

            var squad = results.Select(s => new
            {
                s.Name,
                Players = s.Players.Select(p => new { p.AliasNavigation.Name, Owner = p.PlayerId == s.OwnerPlayerId })
            }).FirstOrDefault();

            if (squad == null)
            {
                zone._server.sendMessage(zone, pkt.alias, "No squad found.");
                return;
            }

            zone._server.sendMessage(zone, pkt.alias, $"&Squad Online List: {squad.Name}");
            zone._server.sendMessage(zone, pkt.alias, $"&Captain: {squad.Players.First(p => p.Owner).Name}");
            zone._server.sendMessage(zone, pkt.alias, "*" + string.Join(", ", squad.Players.Where(p => !p.Owner).Select(p => p.Name)));
        }

        private static void CS_Squads_QueryType_Online(CS_Squads<Zone> pkt, Zone zone, string cleanPayload)
        {
            var player = zone.getPlayer(pkt.alias);

            if (player == null)
            {
                return;
            }

            Squad? squad = null;
            string? owner = null;

            using var ctx = zone._server.getContext();

            if (!string.IsNullOrWhiteSpace(cleanPayload))
            {
                squad = ctx.Squads.Where(s => s.Name == cleanPayload && s.ZoneId == zone._zone.ZoneId).FirstOrDefault();
            }
            else
            {
                if (player.squadid != null)
                {
                    squad = ctx.Squads.Find(player.squadid);

                    if (squad!.OwnerPlayerId == player.dbid)
                    {
                        owner = player.alias;
                    }
                }
            }

            if (squad == null)
            {
                zone._server.sendMessage(zone, pkt.alias, "No squad found.");
                return;
            }

            if (owner == null)
            {
                owner = ctx.Players
                    .Include(p => p.AliasNavigation)
                    .Where(p => p.PlayerId == squad.OwnerPlayerId)
                    .Select(p => p.AliasNavigation.Name)
                    .FirstOrDefault();
            }

            zone._server.sendMessage(zone, pkt.alias, $"&Squad Online List: {squad.Name}");
            zone._server.sendMessage(zone, pkt.alias, $"&Captain: {owner}");

            var onlineSquadmates = zone._players.Where(p => p.Value.squadid == player.squadid).Select(s => s.Value.alias);

            zone._server.sendMessage(zone, pkt.alias, "*" + string.Join(", ", onlineSquadmates));
        }

        private static void CS_Squads_QueryType_DissolveSquad(CS_Squads<Zone> pkt, Zone zone)
        {
            var player = zone.getPlayer(pkt.payload);

            if (player == null || player.squadid == null)
            {
                zone._server.sendMessage(zone, pkt.alias, "You aren't in a squad.");
                return;
            }

            using var ctx = zone._server.getContext();

            var isSquadOwner = ctx.Squads.Any(s => s.ZoneId == zone._zone.ZoneId && s.OwnerPlayerId == player.dbid);

            if (!isSquadOwner)
            {
                zone._server.sendMessage(zone, pkt.alias, "You cannot dissolve a squad you are not captain of!");
                return;
            }

            ctx.Players
                .Where(p => p.SquadId == player.squadid)
                .ExecuteUpdate(setters => setters.SetProperty(p => p.SquadId, (int?)null));

            ctx.Squads
                .Where(s => s.SquadId == player.squadid)
                .ExecuteDelete();

            // Alert any online teammates.
            var squadmates = zone._players.Where(p => p.Value.squadid == player.squadid);

            foreach(var sm in squadmates)
            {
                sm.Value.squadid = null;
                zone._server.sendMessage(zone, sm.Value.alias, "Your squad has been dissolved.");
            }
        }

        private static void CS_Squads_QueryType_LeaveSquad(CS_Squads<Zone> pkt, Zone zone)
        {
            var player = zone.getPlayer(pkt.alias);

            //Sanity checks
            if (player == null || player.squadid == null)
            {
                zone._server.sendMessage(zone, pkt.alias, "You aren't in a squad.");
                return;
            }

            using var ctx = zone._server.getContext();

            var isSquadOwner = ctx.Squads.Any(s => s.ZoneId == zone._zone.ZoneId && s.OwnerPlayerId == player.dbid);

            if (isSquadOwner)
            {
                var dbSquadmates = ctx.Players
                    .Include(p => p.AliasNavigation)
                    .Where(p => p.SquadId == player.squadid && p.PlayerId != player.dbid)
                    .Select(p => new { p.AliasNavigation.Name, p.PlayerId })
                    .ToList();

                ctx.Players.Where(p => p.PlayerId == player.dbid).ExecuteUpdate(setters => setters.SetProperty(p => p.SquadId, (int?)null));

                if (dbSquadmates.Count == 0)
                {
                    // Dissolve the squad.
                    ctx.Squads.Where(s => s.SquadId == player.squadid).ExecuteDelete();
                    zone._server.sendMessage(zone, pkt.alias, "Your squad has been dissolved.");
                }
                else
                {
                    // Transfer to next player.
                    var sm = dbSquadmates[0];
                    ctx.Squads.Where(s => s.SquadId == player.squadid).ExecuteUpdate(setters => setters.SetProperty(s => s.OwnerPlayerId, sm.PlayerId));

                    zone._server.sendMessage(zone, pkt.alias, string.Format("You have left your squad while giving ownership to {0}. Please rejoin the zone to complete this process.", sm.Name));
                    zone._server.sendMessage(zone, sm.Name, "You have been promoted to squad captain of " + sm.Name);
                }
            }
            else
            {
                ctx.Players.Where(p => p.PlayerId == player.dbid).ExecuteUpdate(setters => setters.SetProperty(p => p.SquadId, (int?)null));
            }

            // Alert any online teammates.
            var squadmates = zone._players.Where(p => p.Value.squadid == player.squadid && p.Value.dbid != player.dbid);

            foreach (var s in squadmates)
            {
                zone._server.sendMessage(zone, s.Value.alias, $"{pkt.alias} has left your squad.");
            }

            player.squadid = null;

            zone._server.sendMessage(zone, pkt.alias, "You have left your squad, please rejoin the zone to complete the process.");
        }

        private static void CS_Squads_QueryType_TransferSquad(CS_Squads<Zone> pkt, Zone zone)
        {
            //Sanity checks
            if (string.IsNullOrEmpty(pkt.payload))
            {
                zone._server.sendMessage(zone, pkt.alias, "Who are you transferring it to?");
                return;
            }

            var player = zone.getPlayer(pkt.alias);

            if (player == null || player.squadid == null)
            {
                zone._server.sendMessage(zone, pkt.alias, "You aren't in a squad.");
                return;
            }

            using var ctx = zone._server.getContext();

            var isSquadOwner = ctx.Squads.Any(s => s.ZoneId == zone._zone.ZoneId && s.OwnerPlayerId == player.dbid);

            if (!isSquadOwner)
            {
                zone._server.sendMessage(zone, pkt.alias, "Only squad owners may transfer squad ownership.");
                return;
            }

            var targetPlayer = ctx.Players
                .Include(p => p.AliasNavigation)
                .Include(p => p.SquadNavigation)
                .Where(p => p.AliasNavigation.Name == pkt.payload && p.SquadId == player.squadid && p.ZoneId == zone._zone.ZoneId)
                .Select(p => new { p.PlayerId, p.AliasNavigation.Name, SquadName = p.SquadNavigation!.Name })
                .FirstOrDefault();

            if (targetPlayer == null)
            {
                zone._server.sendMessage(zone, pkt.alias, "No player found in your squad by that alias.");
                return;
            }

            ctx.Squads
                .Where(s => s.SquadId == player.squadid)
                .ExecuteUpdate(setters => setters.SetProperty(s => s.OwnerPlayerId, targetPlayer.PlayerId));

            zone._server.sendMessage(zone, pkt.alias, "You have promoted " + targetPlayer.Name + " to squad captain.");
            zone._server.sendMessage(zone, targetPlayer.Name, "You have been promoted to squad captain of " + targetPlayer.SquadName);
        }

        private static void CS_Squads_QueryType_Kick(CS_Squads<Zone> pkt, Zone zone)
        {
            var player = zone.getPlayer(pkt.alias);

            if (player == null || player.squadid == null)
            {
                return;
            }

            using var ctx = zone._server.getContext();

            var isSquadOwner = ctx.Squads.Any(s => s.ZoneId == zone._zone.ZoneId && s.OwnerPlayerId == player.dbid);

            if (!isSquadOwner)
            {
                zone._server.sendMessage(zone, pkt.alias, "Only squad owners may kick players.");
                return;
            }

            //
            // See if player is online first, and elide the db call.
            //

            var targetPlayer = zone._server.getPlayer(pkt.payload);

            long targetPlayerId;

            if (targetPlayer != null)
            {
                if (targetPlayer.dbid == player.dbid)
                {
                    zone._server.sendMessage(zone, pkt.alias, "You can't kick yourself");
                    return;
                }

                if (targetPlayer.squadid != player.squadid)
                {
                    zone._server.sendMessage(zone, pkt.alias, "You may only kick players from your own squad.");
                    return;
                }

                targetPlayerId = targetPlayer.dbid;
                targetPlayer.squadid = null;
            }
            else
            {
                var dbTargetPlayer = ctx.Players
                    .Include(p => p.AliasNavigation)
                    .Include(p => p.SquadNavigation)
                    .Where(p => p.AliasNavigation.Name == pkt.payload && p.ZoneId == zone._zone.ZoneId && p.SquadId == player.squadid)
                    .Select(p => new { p.PlayerId, AliasName = p.AliasNavigation.Name, SquadName = p.SquadNavigation!.Name})
                    .FirstOrDefault();

                if (dbTargetPlayer == null)
                {
                    zone._server.sendMessage(zone, pkt.alias, "No player found in this zone by that alias within your squad.");
                    return;
                }

                zone._server.sendMessage(zone, pkt.alias, "You have kicked " + dbTargetPlayer.AliasName + " from your squad.");
                zone._server.sendMessage(zone, dbTargetPlayer.AliasName, "You have been kicked from squad " + dbTargetPlayer.SquadName);

                targetPlayerId = dbTargetPlayer.PlayerId;
            }

            ctx.Players
                .Where(p => p.PlayerId == targetPlayerId)
                .ExecuteUpdate(setters => setters.SetProperty(p => p.SquadId, (int?)null));
        }

        private static void CS_Squads_QueryType_Invite(CS_Squads<Zone> pkt, Zone zone)
        {
            var player = zone.getPlayer(pkt.alias);

            if (player == null || player.squadid == null)
            {
                return;
            }

            using var ctx = zone._server.getContext();

            var squad = ctx.Squads
                .Where(s => s.SquadId == player.squadid)
                .Select(s => new { s.OwnerPlayerId })
                .FirstOrDefault();

            if (squad == null || squad.OwnerPlayerId != player.dbid)
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

            var invitedPlayer = ctx.Players
                .Include(p => p.AliasNavigation)
                .Where(p => p.AliasNavigation.Name == sInvite[1] && p.ZoneId == zone._zone.ZoneId)
                .Select(p => new { p.PlayerId, p.AliasNavigation.Name })
                .FirstOrDefault();

            if (invitedPlayer == null)
            {   //No such player!
                zone._server.sendMessage(zone, pkt.alias, "No player found in this zone by that alias.");
                return;
            }

            var squadName = ctx.Squads.Where(s => s.SquadId == player.squadid).Select(s => s.Name).FirstOrDefault();

            KeyValuePair<int, int> squadInvite = new KeyValuePair<int, int>((int)player.squadid, (int)invitedPlayer.PlayerId);
            if (bAdd)
            {   //Send a squad invite
                if (zone._server._squadInvites.Contains(squadInvite))
                {   //Exists
                    zone._server.sendMessage(zone, pkt.alias, "You have already sent a squad invite to " + invitedPlayer.Name);
                }
                else
                {   //Doesn't exist
                    zone._server._squadInvites.Add(squadInvite);
                    zone._server.sendMessage(zone, pkt.alias, "Squad invite sent to " + invitedPlayer.Name);
                    zone._server.sendMessage(zone, invitedPlayer.Name, "You have been invited to join a squad: " + squadName);
                }
            }
            else
            {   //Remove a squad invite
                if (zone._server._squadInvites.Contains(squadInvite))
                {   //Exists
                    zone._server._squadInvites.Remove(squadInvite);
                    zone._server.sendMessage(zone, pkt.alias, "Revoked squad invitation from " + invitedPlayer.Name);
                }
                else
                {   //Doesn't exist
                    zone._server.sendMessage(zone, pkt.alias, "Found no squad invititations sent to " + invitedPlayer.Name);
                }
            }
        }

        private static void CS_Squads_QueryType_CreateSquad(CS_Squads<Zone> pkt, Zone zone, string cleanPayload)
        {
            var player = zone.getPlayer(pkt.alias);

            if (player == null)
            {
                // Bad.
                return;
            }

            if (player.squadid != null)
            {
                using var ctx = zone._server.getContext();

                var squadName = ctx.Squads.Where(s => s.SquadId == player.squadid).Select(s => s.Name).FirstOrDefault();
                zone._server.sendMessage(zone, pkt.alias, $"You cannot create a squad if you are already in one ({squadName}.");
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

            using (var ctx = zone._server.getContext())
            {
                var exists = ctx.Squads.Any(s => s.Name == squadname && s.ZoneId == zone._zone.ZoneId);

                if (exists)
                {
                    zone._server.sendMessage(zone, pkt.alias, "A squad with specified name already exists.");
                    return;
                }
            }

            using (var ctx = zone._server.getContext())
            {
                //Create Some Stats first
                SquadStat stats = new SquadStat();
                stats.Kills = 0;
                stats.Deaths = 0;
                stats.Wins = 0;
                stats.Losses = 0;
                stats.Rating = 0;
                stats.Points = 0;

                ctx.Squadstats.Add(stats);
                ctx.SaveChanges();

                //Create the new squad
                Squad newsquad = new Squad();

                newsquad.Name = squadname;
                newsquad.Password = squadpassword;
                newsquad.OwnerPlayerId = player.dbid;
                newsquad.DateCreated = DateTime.Now;
                newsquad.ZoneId = zone._zone.ZoneId;
                stats.SquadId = newsquad.SquadId;

                ctx.Squads.Add(newsquad);
                ctx.SaveChanges();

                ctx.Players
                    .Where(p => p.PlayerId == player.dbid)
                    .ExecuteUpdate(p => p.SetProperty(s => s.SquadId, newsquad.SquadId));

                player.squadid = newsquad.SquadId;

                zone._server.sendMessage(zone, pkt.alias, "Successfully created squad: " + newsquad.Name + ". Quit and rejoin to be able to use # to squad chat.");
                Log.write(TLog.Normal, "Player {0} created squad {1} in zone {2}", pkt.alias, newsquad.Name, zone._zone.Name);
            }
        }

        /// <summary>
        /// Handles a ?*chart query
        /// </summary>
        static public void Handle_CS_ChartQuery(CS_ChartQuery<Zone> pkt, Zone zone)
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

                        foreach (var p in results)
                        {
                            var arenaName = p.Item2.arena;

                            if (p.Item2.arena.StartsWith("#") && zonePlayer.arena != p.Item2.arena)
                            {
                                arenaName = "(private)";
                            }

                            var row = String.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\" \"", p.Item2.alias, p.Item2.zone._zone.Name, arenaName, p.Item1);

                            respond.rows.Add(row);
                        }

                        zone._client.sendReliable(respond, 1);
                    }
                    break;
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
