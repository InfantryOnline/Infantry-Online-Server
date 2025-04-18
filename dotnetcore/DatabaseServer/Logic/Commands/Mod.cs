using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;

using InfServer.Protocol;
using InfServer.Data;
using InfServer.Network;
using System.Xml.Linq;
using System.Diagnostics;

namespace InfServer.Logic
{
    class Logic_ModCommands
    {
        /// <summary>
        /// Handles a query packet
        /// </summary>
        static public void Handle_CS_ModQuery(CS_ModQuery<Zone> pkt, Zone zone)
        {
            using (Database.DataContext db = zone._server.getContext())
            {
                switch (pkt.queryType)
                {
                    case CS_ModQuery<Zone>.QueryType.aliastransfer:
                        {
                            if (string.IsNullOrEmpty(pkt.query) || string.IsNullOrEmpty(pkt.aliasTo))
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Wrong format typed.");
                                return;
                            }

                            //Who the alias is going to
                            Database.Alias paliasTo = db.Aliases.FirstOrDefault(aTo => aTo.Name == pkt.aliasTo);
                            if (paliasTo == null)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Cant find the recipient's alias.");
                                return;
                            }

                            //The alias in question
                            Database.Alias alias = db.Aliases.FirstOrDefault(a => a.Name == pkt.query);
                            Database.Player anyPlayer = db.Players.FirstOrDefault(p => p.AliasNavigation.Name == pkt.query);

                            if (alias == null)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Can't find the alias in question, maybe its not created yet.");
                                return;
                            }

                            if (anyPlayer == null)
                            {
                                //Since structure doesn't exist, go ahead and transfer
                                alias.Ipaddress = paliasTo.Ipaddress.Trim();
                                alias.Timeplayed = 0;
                                alias.Account = paliasTo.Account;
                                alias.AccountNavigation = paliasTo.AccountNavigation;
                                db.SaveChanges();
                                zone._server.sendMessage(zone, pkt.sender, "Alias transfer completed.");
                                return;
                            }

                            var players = db.Players.Where(p => p.Alias == alias.Id).ToList();

                            // Remove all players under this alias

                            foreach (var p in players)
                            {
                                if (p.Squad != null)
                                {
                                    var squadmates = db.Players.Where(plyr => plyr.Zone == p.Zone && plyr.Squad != null && plyr.Squad == p.Squad).ToList();

                                    if (p.SquadNavigation.Owner == p.Id)
                                    {
                                        if (squadmates.Count() > 1)
                                        {
                                            var otherPlayer = squadmates.FirstOrDefault(plyr => plyr.Id != p.Id);
                                            //Since the player is the owner, lets just give it to someone else
                                            otherPlayer.SquadNavigation.Owner = otherPlayer.Id;
                                        }
                                        else if (squadmates.Count() == 1)
                                        {
                                            //Lets delete the squad
                                            db.Squads.Remove(p.SquadNavigation);
                                        }
                                    }
                                    p.SquadNavigation = null;
                                    p.Squad = null;
                                }

                                db.Stats.Remove(p.StatsNavigation);
                                db.Players.Remove(p);

                                var dailies = db.StatsDailies.Where(s => s.Player == p.Id);
                                var weeklies = db.StatsWeeklies.Where(s => s.Player == p.Id);
                                var monthlies = db.StatsMonthlies.Where(s => s.Player == p.Id);
                                var yearlies = db.StatsYearlies.Where(s => s.Player == p.Id);

                                db.StatsDailies.RemoveRange(dailies);
                                db.StatsWeeklies.RemoveRange(weeklies);
                                db.StatsMonthlies.RemoveRange(monthlies);
                                db.StatsYearlies.RemoveRange(yearlies);

                                db.SaveChanges();
                            }

                            //Now lets transfer
                            alias.Ipaddress = paliasTo.Ipaddress.Trim();
                            alias.Timeplayed = 0;
                            alias.Account = paliasTo.Account;
                            alias.AccountNavigation = paliasTo.AccountNavigation;
                            db.SaveChanges();
                            zone._server.sendMessage(zone, pkt.sender, "Alias transfer completed.");
                        }
                        break;

                    case CS_ModQuery<Zone>.QueryType.aliasremove:
                        {
                            if (string.IsNullOrEmpty(pkt.query))
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Wrong format typed.");
                                return;
                            }

                            //Lets get all account related info then delete it
                            Database.Alias palias = db.Aliases.FirstOrDefault(a => a.Name == pkt.query);

                            if (palias == null)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Cannot find the specified alias.");
                                return;
                            }

                            var players = db.Players.Where(p => p.Alias == palias.Id).ToList();

                            // Remove all players under this alias
                            foreach (var p in players)
                            {
                                if (p.Squad != null)
                                {
                                    var squadmates = db.Players.Where(plyr => plyr.Zone == p.Zone && plyr.Squad != null && plyr.Squad == p.Squad).ToList();

                                    if (p.SquadNavigation.Owner == p.Id)
                                    {
                                        if (squadmates.Count() > 1)
                                        {
                                            var otherPlayer = squadmates.FirstOrDefault(plyr => plyr.Id != p.Id);
                                            //Since the player is the owner, lets just give it to someone else
                                            otherPlayer.SquadNavigation.Owner = otherPlayer.Id;
                                        }
                                        else if (squadmates.Count() == 1)
                                        {
                                            //Lets delete the squad
                                            db.Squads.Remove(p.SquadNavigation);
                                        }
                                    }
                                    p.SquadNavigation = null;
                                    p.Squad = null;
                                }

                                db.Stats.Remove(p.StatsNavigation);
                                db.Players.Remove(p);

                                var dailies = db.StatsDailies.Where(s => s.Player == p.Id);
                                var weeklies = db.StatsWeeklies.Where(s => s.Player == p.Id);
                                var monthlies = db.StatsMonthlies.Where(s => s.Player == p.Id);
                                var yearlies = db.StatsYearlies.Where(s => s.Player == p.Id);

                                db.StatsDailies.RemoveRange(dailies);
                                db.StatsWeeklies.RemoveRange(weeklies);
                                db.StatsMonthlies.RemoveRange(monthlies);
                                db.StatsYearlies.RemoveRange(yearlies);

                                db.SaveChanges();
                            }

                            db.Aliases.Remove(palias);
                            db.SaveChanges();
                            zone._server.sendMessage(zone, pkt.sender, "Alias has been deleted.");
                        }
                        break;

                    case CS_ModQuery<Zone>.QueryType.aliasrename:
                        {
                            if (string.IsNullOrEmpty(pkt.query))
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Wrong format typed.");
                                return;
                            }

                            //Get all account related info
                            Database.Alias paliasTo = db.Aliases.FirstOrDefault(aTo => aTo.Name == pkt.aliasTo);
                            Database.Alias alias = db.Aliases.FirstOrDefault(a => a.Name == pkt.query);
                            //Player even alive?
                            if (paliasTo == null)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Cannot find the specified alias.");
                                return;
                            }

                            string name = paliasTo.Name;

                            //Does the payload already exist?
                            if (alias == null)
                            {
                                paliasTo.Name = pkt.query;
                                db.SaveChanges();
                                zone._server.sendMessage(zone, pkt.sender, "Renamed player " + name + " to " + pkt.query + " has been completed.");
                                return;
                            }

                            if (alias.AccountNavigation != paliasTo.AccountNavigation)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "That alias is already being used.");
                                return;
                            }

                            if (alias.Id != paliasTo.Id)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Cannot change an alias to one already existing on the account.");
                                return;
                            }

                            paliasTo.Name = pkt.query;
                            db.SaveChanges();

                            zone._server.sendMessage(zone, pkt.sender, "Renamed player " + name + " to " + pkt.query + " has been completed.");
                        }
                        break;

                    case CS_ModQuery<Zone>.QueryType.mod:
                        {
                            if (string.IsNullOrEmpty(pkt.query))
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Wrong format typed.");
                                return;
                            }

                            //Lets get all account related info
                            Database.Alias palias = db.Aliases.FirstOrDefault(a => a.Name == pkt.query);
                            Database.Account account = db.Accounts.FirstOrDefault(p => p.Id == palias.AccountNavigation.Id);
                            if (palias == null)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Cannot find the specified alias.");
                                return;
                            }

                            if (account == null)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Cannot find the specified account.");
                                return;
                            }

                            //Lets mod/de-mod them
                            account.Permission = pkt.level;
                            db.SaveChanges();
                            zone._server.sendMessage(zone, pkt.sender, "Changing player " + palias.Name + "'s level to " + pkt.level + " has been completed.");
                        }
                        break;

                    case CS_ModQuery<Zone>.QueryType.dev:
                        {
                            if (string.IsNullOrEmpty(pkt.query))
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Wrong format typed.");
                                return;
                            }

                            //Lets get all account related info
                            Database.Player player = (from plyr in db.Players
                                                                where plyr.AliasNavigation.Name == pkt.query && plyr.ZoneNavigation == zone._zone
                                                                select plyr).FirstOrDefault();
                            if (player == null)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Cannot find the specified alias.");
                                return;
                            }

                            //Lets mod/de-mod them
                            player.Permission = (short)pkt.level;

                            db.SaveChanges();
                            zone._server.sendMessage(zone, pkt.sender, "Changing player " + player.AliasNavigation.Name + "'s dev level to " + pkt.level + " has been completed.");
                        }
                        break;

                    case CS_ModQuery<Zone>.QueryType.squadtransfer:
                        {
                            if (string.IsNullOrEmpty(pkt.aliasTo) || string.IsNullOrEmpty(pkt.query))
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Wrong format typed.");
                                return;
                            }

                            //Lets find the player first
                            Database.Player dbplayer = db.Zones.First(z => z.Id == zone._zone.Id).Players.FirstOrDefault(p => p.AliasNavigation.Name == pkt.aliasTo);
                            if (dbplayer == null)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Cannot find the player.");
                                return;
                            }

                            //Lets find the squad in question
                            Database.Squad squad = db.Squads.First(s => s.Name == pkt.query && s.Zone == zone._zone.Id);
                            if (squad == null)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Cannot find the specified squad.");
                                return;
                            }

                            //Are they in a squad?
                            if (dbplayer.Squad != null)
                            {
                                //Is it the same squad?
                                if (dbplayer.Squad != squad.Id)
                                {
                                    zone._server.sendMessage(zone, pkt.sender, "That player isn't on the same squad.");
                                    return;
                                }
                                //Transfer
                                dbplayer.SquadNavigation.Owner = dbplayer.Id;
                            }
                            else
                            {
                                dbplayer.Squad = squad.Id;
                                dbplayer.SquadNavigation.Owner = dbplayer.Id;
                            }
                            db.SaveChanges();
                            zone._server.sendMessage(zone, dbplayer.AliasNavigation.Name, "You have been promoted to squad captain of " + dbplayer.SquadNavigation.Name);
                            zone._server.sendMessage(zone, dbplayer.AliasNavigation.Name, "Please relog to complete the process.");
                            zone._server.sendMessage(zone, pkt.sender, "Squad transferring is complete.");
                        }
                        break;

                    case CS_ModQuery<Zone>.QueryType.squadjoin:
                        {
                            if (string.IsNullOrWhiteSpace(pkt.aliasTo) || string.IsNullOrWhiteSpace(pkt.query))
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Wrong format typed.");
                                return;
                            }

                            //Lets find the player first
                            Database.Player dbplayer = db.Zones.First(z => z.Id == zone._zone.Id).Players.FirstOrDefault(p => p.AliasNavigation.Name == pkt.aliasTo);
                            if (dbplayer == null)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Cannot find the player.");
                                return;
                            }

                            //Lets find the squad in question
                            Database.Squad squad = db.Squads.FirstOrDefault(s => s.Name == pkt.query && s.Zone == zone._zone.Id);
                            if (squad == null)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Cannot find the specified squad.");
                                return;
                            }

                            //Already squad joined somewhere?
                            if (dbplayer.Squad != null)
                            {
                                //Get his squad brothers! (if any...)
                                IQueryable<Database.Player> squadmates = db.Players.Where(p => p.Zone == dbplayer.Zone && p.Squad != null && p.Squad == dbplayer.Squad);

                                //Is he the captain?
                                if (dbplayer.SquadNavigation.Owner == dbplayer.Id)
                                {   //We might need to dissolve the team!
                                    if (squadmates.Count() == 1)
                                    {   //He's the only one left on the squad... dissolve it!
                                        var s1 = dbplayer.SquadNavigation;
                                        dbplayer.SquadNavigation = null;
                                        dbplayer.Squad = null;
                                        db.Squads.Remove(s1);

                                        db.SaveChanges();
                                    }
                                    else
                                    {   //There are other people on the squad, transfer it to someone
                                        Database.Player transferPlayer = squadmates.FirstOrDefault(p => p.Id != dbplayer.Id);
                                        dbplayer.SquadNavigation.Owner = transferPlayer.Id;
                                        db.SaveChanges();
                                        zone._server.sendMessage(zone, transferPlayer.AliasNavigation.Name, "You have been promoted to squad captain of " + transferPlayer.SquadNavigation.Name);
                                    }
                                }
                            }

                            dbplayer.Squad = squad.Id;
                            db.SaveChanges();
                            zone._server.sendMessage(zone, dbplayer.AliasNavigation.Name, "You have joined " + squad.Name);
                            zone._server.sendMessage(zone, dbplayer.AliasNavigation.Name, "Please relog to complete the process.");
                            zone._server.sendMessage(zone, pkt.sender, "Squad joining completed.");
                        }
                        break;

                    case CS_ModQuery<Zone>.QueryType.powered:
                        {
                            if (string.IsNullOrWhiteSpace(pkt.query))
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Payload cannot be empty.");
                                return;
                            }

                            if (pkt.query.Equals("list"))
                            {
                                Database.Player sender = db.Players.FirstOrDefault(p => p.AliasNavigation.Name == pkt.sender && p.Zone == zone._zone.Id);
                                if (sender == null)
                                    return;

                                SortedDictionary<string, string> powered = new SortedDictionary<string, string>();
                                string pAlias;
                                foreach (Zone z in zone._server._zones)
                                {
                                    foreach (KeyValuePair<int, Zone.Player> Player in z._players)
                                    {
                                        pAlias = Player.Value.alias;
                                        var alias = db.Aliases.SingleOrDefault(p => p.Name == pAlias);
                                        if (alias == null)
                                            continue;
                                        if (alias.Name == pkt.sender)
                                            continue;
                                        //Are they a global mod?
                                        if (alias.AccountNavigation.Permission > 0)
                                        {
                                            //Are they higher than us?
                                            if (alias.AccountNavigation.Permission > sender.AliasNavigation.AccountNavigation.Permission
                                                && alias.AccountNavigation.Permission > sender.Permission)
                                                continue;
                                            powered.Add(pAlias, string.Format("*{0} - Lvl({1})", pAlias, alias.AccountNavigation.Permission.ToString()));
                                        }
                                        else
                                        {
                                            var player = db.Zones.First(zones => zones.Id == z._zone.Id).Players.First(p => p.AliasNavigation == alias);
                                            if (player != null && player.Permission > 0)
                                            {
                                                //Are they higher than us?
                                                if (player.Permission > sender.Permission
                                                    && player.AliasNavigation.AccountNavigation.Permission > sender.AliasNavigation.AccountNavigation.Permission)
                                                    continue;
                                                powered.Add(pAlias, string.Format("*{0} - Lvl({1})(dev)", pAlias, player.Permission.ToString()));
                                            }
                                        }
                                    }
                                }

                                //Now send it!
                                if (powered.Count > 0)
                                {
                                    foreach (string str in powered.Values)
                                        zone._server.sendMessage(zone, pkt.sender, str);
                                }
                                else
                                    zone._server.sendMessage(zone, pkt.sender, "Empty.");
                            }
                        }
                        break;

                    case CS_ModQuery<Zone>.QueryType.find:
                        {
                            zone._server.sendMessage(zone, pkt.sender, "&Search Results:");

                            Database.Alias alias = db.Aliases.SingleOrDefault(ali => ali.Name == pkt.query);
                            if (alias == null)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Cannot find the specified alias.");
                                return;
                            }

                            bool found = false;
                            IQueryable<Database.Alias> foundAlias = db.Aliases.Where(d => (d.Ipaddress.Equals(alias.Ipaddress) || d.Account == alias.Account));
                            foreach (KeyValuePair<string, Zone.Player> player in zone._server._players)
                            {
                                foreach (Database.Alias p in foundAlias)
                                    if (player.Value.alias.Equals(p.Name))
                                    {
                                        zone._server.sendMessage(zone, pkt.sender, string.Format("*Found: {0} Zone: {1} Arena: {2}", p.Name, player.Value.zone._zone.Name, !String.IsNullOrWhiteSpace(player.Value.arena) ? player.Value.arena : "Unknown Arena"));
                                        found = true;
                                    }
                            }
                            if (!found)
                                zone._server.sendMessage(zone, pkt.sender, "Cannot find the specified alias.");
                        }
                        break;

                    case CS_ModQuery<Zone>.QueryType.globalsilence:
                        if (string.IsNullOrWhiteSpace(pkt.query))
                        {
                            zone._server.sendMessage(zone, pkt.sender, "Payload cannot be empty.");
                            return;
                        }

                        var data = pkt.query.Split(':');
                        long silencedDuration;

                        if (data.Length != 2 || !Int64.TryParse(data[1], out silencedDuration))
                        {
                            zone._server.sendMessage(zone, pkt.sender, "Badly formatted packet. Please follow <alias>:<minutes> pattern.  0 minutes will unsilence upon next login.");
                            return;
                        }

                        var silencedAlias = db.Aliases.FirstOrDefault(a => a.Name.ToLower() == data[0].ToLower());

                        if (silencedAlias == null)
                        {
                            zone._server.sendMessage(zone, pkt.sender, $"Alias \"{data[0]}\" not found.");
                            return;
                        }

                        var silencedAccount = db.Accounts.First(t => t.Id == silencedAlias.Account);

                        if (silencedDuration < 0)
                        {
                            silencedDuration = 0;
                        }

                        var silencedTime = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();

                        silencedAccount.SilencedDuration = silencedDuration;
                        silencedAccount.SilencedAtMillisecondsUnix = silencedDuration == 0 ? 0 : silencedTime;

                        db.SaveChanges();

                        // TODO: Alert all zones that the player is silenced.

                        break;
                }
            }
        }

        /// <summary>
        /// Handles a query packet
        /// </summary>
        static public void Handle_CS_Ban(CS_Ban<Zone> pkt, Zone zone)
        {
            using (Database.DataContext db = zone._server.getContext())
            {
                Database.Alias dbplayer = db.Aliases.First(p => p.Name == pkt.alias);

                //Lets check to see if they are banned already
                foreach (Database.Ban b in db.Bans.Where(b => b.Account == dbplayer.AccountNavigation.Id))
                {
                    //Same type?
                    if (b.Type != (short)pkt.banType)
                        continue;

                    //Zone ban?
                    if ((short)pkt.banType == (int)Logic_Bans.Ban.BanType.ZoneBan)
                    {
                        if (b.Zone == null)
                            continue;
                        if (b.Zone != zone._zone.Id)
                            continue;
                    }

                    if (pkt.time != 0)
                    {
                        //Dont update old bans
                        if (DateTime.Now > b.Expires)
                            continue;
                        b.Expires = b.Expires.Value.AddMinutes(pkt.time);
                    }
                    //Are we unbanning them?
                    else if (pkt.time == 0)
                        b.Expires = DateTime.Now;

                    b.Reason = b.Reason.ToString();

                    db.SaveChanges();
                    return;
                }

                Database.Ban newBan = new Database.Ban();
                switch (pkt.banType)
                {
                    case CS_Ban<Zone>.BanType.zone:
                        {
                            //Check for updating the ban
                            newBan.Type = (short)Logic_Bans.Ban.BanType.ZoneBan;
                            if (pkt.time == 0)
                                newBan.Expires = DateTime.Now;
                            else
                                newBan.Expires = DateTime.Now.AddMinutes(pkt.time);
                            newBan.Created = DateTime.Now;
                            newBan.Uid1 = pkt.UID1;
                            newBan.Uid2 = pkt.UID2;
                            newBan.Uid3 = pkt.UID3;
                            newBan.Account = dbplayer.Account;
                            newBan.Ipaddress = dbplayer.Ipaddress;
                            newBan.Zone = zone._zone.Id;
                            newBan.Reason = pkt.reason;
                            newBan.Name = dbplayer.Name;
                        }
                        break;

                    case CS_Ban<Zone>.BanType.account:
                        {
                            newBan.Type = (short)Logic_Bans.Ban.BanType.AccountBan;
                            if (pkt.time == 0)
                                newBan.Expires = DateTime.Now;
                            else
                                newBan.Expires = DateTime.Now.AddMinutes(pkt.time);
                            newBan.Created = DateTime.Now;
                            newBan.Uid1 = pkt.UID1;
                            newBan.Uid2 = pkt.UID2;
                            newBan.Uid3 = pkt.UID3;
                            newBan.Account = dbplayer.Account;
                            newBan.Ipaddress = dbplayer.Ipaddress;
                            newBan.Reason = pkt.reason;
                            newBan.Name = dbplayer.Name;
                        }
                        break;

                    //case CS_Ban<Zone>.BanType.ip:
                    //    {
                    //        newBan.type = (short)Logic_Bans.Ban.BanType.IPBan;
                    //        if (pkt.time == 0)
                    //            newBan.expires = DateTime.Now;
                    //        else
                    //            newBan.expires = DateTime.Now.AddMinutes(pkt.time);
                    //        newBan.created = DateTime.Now;
                    //        newBan.uid1 = pkt.UID1;
                    //        newBan.uid2 = pkt.UID2;
                    //        newBan.uid3 = pkt.UID3;
                    //        newBan.account = dbplayer.account;
                    //        newBan.IPAddress = dbplayer.IPAddress;
                    //        newBan.reason = pkt.reason;
                    //        newBan.name = dbplayer.name;
                    //    }
                    //    break;

                    //case CS_Ban<Zone>.BanType.global:
                    //    {
                    //        newBan.type = (short)Logic_Bans.Ban.BanType.GlobalBan;
                    //        if (pkt.time == 0)
                    //            newBan.expires = DateTime.Now;
                    //        else
                    //            newBan.expires = DateTime.Now.AddMinutes(pkt.time);
                    //        newBan.created = DateTime.Now;
                    //        newBan.uid1 = pkt.UID1;
                    //        newBan.uid2 = pkt.UID2;
                    //        newBan.uid3 = pkt.UID3;
                    //        newBan.account = dbplayer.account;
                    //        newBan.IPAddress = dbplayer.IPAddress;
                    //        newBan.reason = pkt.reason;
                    //        newBan.name = dbplayer.name;
                    //    }
                    //    break;
                }

                db.Bans.Add(newBan);
                db.SaveChanges();

                SC_DisconnectPlayer<Zone> dc = new SC_DisconnectPlayer<Zone>();
                dc.alias = pkt.alias;

                var server = zone._server;

                foreach (Zone z in server._zones)
                {
                    if (z == null)
                        continue;

                    z._client.send(dc);
                }
            }
        }

        static public void Handle_CS_Unban(CS_Unban<Zone> pkt, Zone zone)
        {
            // TODO: Have one enum, not three.

            Logic_Bans.Ban.BanType banType;

            if (pkt.banType == CS_Unban<Zone>.BanType.zone)
            {
                banType = Logic_Bans.Ban.BanType.ZoneBan;
            }
            else if (pkt.banType == CS_Unban<Zone>.BanType.account)
            {
                banType = Logic_Bans.Ban.BanType.AccountBan;
            }
            else
            {
                zone._server.sendMessage(zone, pkt.sender, "Unrecognized ban type. Contact server developers.");
                return;
            }

            using (var db = zone._server.getContext())
            {
                var alias = db.Aliases.FirstOrDefault(x => x.Name == pkt.alias);

                if (alias == null)
                {
                    zone._server.sendMessage(zone, pkt.sender, "Cannot find the specified alias.");
                    return;
                }

                Debug.Assert(alias.AccountNavigation != null, "Missing DB FK Constraint.");

                var bans = db.Bans.Where(t => t.Account == alias.Account && t.Type == (short)banType);

                if (banType == Logic_Bans.Ban.BanType.ZoneBan)
                {
                    bans = bans.Where(t => t.Zone == zone._zone.Id);
                }

                db.Bans.RemoveRange(bans);
                db.SaveChanges();

                zone._server.sendMessage(zone, pkt.sender, "Any bans of the requested type has been removed.");
            }
        }

        static public void Handle_CS_Stealth(CS_Stealth<Zone> pkt, Zone zone)
        {
            using (Database.DataContext db = zone._server.getContext())
            {
                var dbalias = db.Aliases.First(p => p.Name == pkt.sender);

                var zonePlayer = zone.getPlayer(pkt.sender);

                if (dbalias == null || zonePlayer == null)
                {
                    Console.WriteLine("Alias not found:" + pkt.sender);
                    return;
                }

                zonePlayer.stealth = pkt.stealth;

                dbalias.Stealth = pkt.stealth ? 1 : 0;
                db.SaveChanges();

                var status = pkt.stealth ? "ON" : "OFF";

                zone._server.sendMessage(zone, pkt.sender, $"Stealth is now {status}");

                foreach (var chatName in zonePlayer.chats)
                {
                    var chat = zone._server._chats.FirstOrDefault(c => c.Key == chatName).Value;

                    if (chat == null || !chat.hasPlayer(zonePlayer))
                    {
                        // Log error?
                        continue;
                    }

                    if (pkt.stealth)
                    {
                        SC_LeaveChat<Zone> leave = new SC_LeaveChat<Zone>();
                        leave.from = zonePlayer.alias;
                        leave.chat = chat._name;
                        leave.users = chat.List();

                        foreach (Zone z in zone._server._zones)
                        {
                            if (z == null)
                                continue;

                            z._client.send(leave);
                        }
                    }
                    else
                    {
                        SC_JoinChat<Zone> join = new SC_JoinChat<Zone>();
                        join.from = zonePlayer.alias;
                        join.chat = chat._name;
                        join.users = chat.List();

                        foreach (Zone z in zone._server._zones)
                        {
                            if (z == null)
                                continue;

                            z._client.send(join);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Registers all handlers
        /// </summary>
        [RegistryFunc]
        static public void Register()
        {
            CS_Ban<Zone>.Handlers += Handle_CS_Ban;
            CS_Unban<Zone>.Handlers += Handle_CS_Unban;
            CS_ModQuery<Zone>.Handlers += Handle_CS_ModQuery;
            CS_Stealth<Zone>.Handlers += Handle_CS_Stealth;
        }
    }
}