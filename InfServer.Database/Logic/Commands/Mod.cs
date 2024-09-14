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
            using (InfServer.Database.InfantryDataContext db = zone._server.getContext())
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
                            InfServer.Database.alias paliasTo = db.alias.FirstOrDefault(aTo => string.Compare(aTo.name, pkt.aliasTo, true) == 0);
                            if (paliasTo == null)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Cant find the recipient's alias.");
                                return;
                            }

                            //The alias in question
                            InfServer.Database.alias alias = db.alias.FirstOrDefault(a => string.Compare(a.name, pkt.query, true) == 0);
                            InfServer.Database.player anyPlayer = db.players.FirstOrDefault(p => string.Compare(p.alias1.name, pkt.query, true) == 0);

                            if (alias == null)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Can't find the alias in question, maybe its not created yet.");
                                return;
                            }

                            if (anyPlayer == null)
                            {
                                //Since structure doesn't exist, go ahead and transfer
                                alias.IPAddress = paliasTo.IPAddress.Trim();
                                alias.timeplayed = 0;
                                alias.account = paliasTo.account;
                                alias.account1 = paliasTo.account1;
                                db.SubmitChanges();
                                zone._server.sendMessage(zone, pkt.sender, "Alias transfer completed.");
                                return;
                            }

                            var players = db.players.Where(p => p.alias == alias.id).ToList();

                            // Remove all players under this alias

                            foreach (var p in players)
                            {
                                if (p.squad != null)
                                {
                                    var squadmates = db.players.Where(plyr => plyr.zone == p.zone && plyr.squad != null && plyr.squad == p.squad).ToList();

                                    if (p.squad1.owner == p.id)
                                    {
                                        if (squadmates.Count() > 1)
                                        {
                                            var otherPlayer = squadmates.FirstOrDefault(plyr => plyr.id != p.id);
                                            //Since the player is the owner, lets just give it to someone else
                                            otherPlayer.squad1.owner = otherPlayer.id;
                                        }
                                        else if (squadmates.Count() == 1)
                                        {
                                            //Lets delete the squad
                                            db.squads.DeleteOnSubmit(p.squad1);
                                        }
                                    }
                                    p.squad1 = null;
                                    p.squad = null;
                                }

                                db.stats.DeleteOnSubmit(p.stats1);
                                db.players.DeleteOnSubmit(p);

                                var dailies = db.statsDailies.Where(s => s.player == p.id);
                                var weeklies = db.statsWeeklies.Where(s => s.player == p.id);
                                var monthlies = db.statsMonthlies.Where(s => s.player == p.id);
                                var yearlies = db.statsYearlies.Where(s => s.player == p.id);

                                db.statsDailies.DeleteAllOnSubmit(dailies);
                                db.statsWeeklies.DeleteAllOnSubmit(weeklies);
                                db.statsMonthlies.DeleteAllOnSubmit(monthlies);
                                db.statsYearlies.DeleteAllOnSubmit(yearlies);

                                db.SubmitChanges();
                            }

                            //Now lets transfer
                            alias.IPAddress = paliasTo.IPAddress.Trim();
                            alias.timeplayed = 0;
                            alias.account = paliasTo.account;
                            alias.account1 = paliasTo.account1;
                            db.SubmitChanges();
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
                            InfServer.Database.alias palias = db.alias.FirstOrDefault(a => string.Compare(a.name, pkt.query, true) == 0);

                            if (palias == null)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Cannot find the specified alias.");
                                return;
                            }

                            var players = db.players.Where(p => p.alias == palias.id).ToList();

                            // Remove all players under this alias
                            foreach (var p in players)
                            {
                                if (p.squad != null)
                                {
                                    var squadmates = db.players.Where(plyr => plyr.zone == p.zone && plyr.squad != null && plyr.squad == p.squad).ToList();

                                    if (p.squad1.owner == p.id)
                                    {
                                        if (squadmates.Count() > 1)
                                        {
                                            var otherPlayer = squadmates.FirstOrDefault(plyr => plyr.id != p.id);
                                            //Since the player is the owner, lets just give it to someone else
                                            otherPlayer.squad1.owner = otherPlayer.id;
                                        }
                                        else if (squadmates.Count() == 1)
                                        {
                                            //Lets delete the squad
                                            db.squads.DeleteOnSubmit(p.squad1);
                                        }
                                    }
                                    p.squad1 = null;
                                    p.squad = null;
                                }

                                db.stats.DeleteOnSubmit(p.stats1);
                                db.players.DeleteOnSubmit(p);

                                var dailies = db.statsDailies.Where(s => s.player == p.id);
                                var weeklies = db.statsWeeklies.Where(s => s.player == p.id);
                                var monthlies = db.statsMonthlies.Where(s => s.player == p.id);
                                var yearlies = db.statsYearlies.Where(s => s.player == p.id);

                                db.statsDailies.DeleteAllOnSubmit(dailies);
                                db.statsWeeklies.DeleteAllOnSubmit(weeklies);
                                db.statsMonthlies.DeleteAllOnSubmit(monthlies);
                                db.statsYearlies.DeleteAllOnSubmit(yearlies);

                                db.SubmitChanges();
                            }

                            db.alias.DeleteOnSubmit(palias);
                            db.SubmitChanges();
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
                            InfServer.Database.alias paliasTo = db.alias.FirstOrDefault(aTo => string.Compare(aTo.name, pkt.aliasTo, true) == 0);
                            InfServer.Database.alias alias = db.alias.FirstOrDefault(a => string.Compare(a.name, pkt.query, true) == 0);
                            //Player even alive?
                            if (paliasTo == null)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Cannot find the specified alias.");
                                return;
                            }

                            string name = paliasTo.name;

                            //Does the payload already exist?
                            if (alias == null)
                            {
                                paliasTo.name = pkt.query;
                                db.SubmitChanges();
                                zone._server.sendMessage(zone, pkt.sender, "Renamed player " + name + " to " + pkt.query + " has been completed.");
                                return;
                            }

                            if (alias.account1 != paliasTo.account1)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "That alias is already being used.");
                                return;
                            }

                            if (alias.id != paliasTo.id)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Cannot change an alias to one already existing on the account.");
                                return;
                            }

                            paliasTo.name = pkt.query;
                            db.SubmitChanges();

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
                            InfServer.Database.alias palias = db.alias.FirstOrDefault(a => string.Compare(a.name, pkt.query, true) == 0);
                            InfServer.Database.account account = db.accounts.FirstOrDefault(p => p.id == palias.account1.id);
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
                            account.permission = pkt.level;
                            db.SubmitChanges();
                            zone._server.sendMessage(zone, pkt.sender, "Changing player " + palias.name + "'s level to " + pkt.level + " has been completed.");
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
                            InfServer.Database.player player = (from plyr in db.players
                                                                where string.Compare(plyr.alias1.name, pkt.query, true) == 0 && plyr.zone1 == zone._zone
                                                                select plyr).FirstOrDefault();
                            if (player == null)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Cannot find the specified alias.");
                                return;
                            }

                            //Lets mod/de-mod them
                            player.permission = (short)pkt.level;

                            db.SubmitChanges();
                            zone._server.sendMessage(zone, pkt.sender, "Changing player " + player.alias1.name + "'s dev level to " + pkt.level + " has been completed.");
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
                            InfServer.Database.player dbplayer = db.zones.First(z => z.id == zone._zone.id).players.FirstOrDefault(p => string.Compare(p.alias1.name, pkt.aliasTo, true) == 0);
                            if (dbplayer == null)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Cannot find the player.");
                                return;
                            }

                            //Lets find the squad in question
                            InfServer.Database.squad squad = db.squads.First(s => string.Compare(s.name, pkt.query, true) == 0 && s.zone == zone._zone.id);
                            if (squad == null)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Cannot find the specified squad.");
                                return;
                            }

                            //Are they in a squad?
                            if (dbplayer.squad != null)
                            {
                                //Is it the same squad?
                                if (dbplayer.squad != squad.id)
                                {
                                    zone._server.sendMessage(zone, pkt.sender, "That player isn't on the same squad.");
                                    return;
                                }
                                //Transfer
                                dbplayer.squad1.owner = dbplayer.id;
                            }
                            else
                            {
                                dbplayer.squad = squad.id;
                                dbplayer.squad1.owner = dbplayer.id;
                            }
                            db.SubmitChanges();
                            zone._server.sendMessage(zone, dbplayer.alias1.name, "You have been promoted to squad captain of " + dbplayer.squad1.name);
                            zone._server.sendMessage(zone, dbplayer.alias1.name, "Please relog to complete the process.");
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
                            InfServer.Database.player dbplayer = db.zones.First(z => z.id == zone._zone.id).players.FirstOrDefault(p => string.Compare(p.alias1.name, pkt.aliasTo, true) == 0);
                            if (dbplayer == null)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Cannot find the player.");
                                return;
                            }

                            //Lets find the squad in question
                            InfServer.Database.squad squad = db.squads.FirstOrDefault(s => string.Compare(s.name, pkt.query, true) == 0 && s.zone == zone._zone.id);
                            if (squad == null)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Cannot find the specified squad.");
                                return;
                            }

                            //Already squad joined somewhere?
                            if (dbplayer.squad != null)
                            {
                                //Get his squad brothers! (if any...)
                                IQueryable<InfServer.Database.player> squadmates = db.players.Where(p => p.zone == dbplayer.zone && p.squad != null && p.squad == dbplayer.squad);

                                //Is he the captain?
                                if (dbplayer.squad1.owner == dbplayer.id)
                                {   //We might need to dissolve the team!
                                    if (squadmates.Count() == 1)
                                    {   //He's the only one left on the squad... dissolve it!
                                        db.squads.DeleteOnSubmit(dbplayer.squad1);
                                        db.SubmitChanges();
                                        dbplayer.squad1 = null;
                                        dbplayer.squad = null;
                                    }
                                    else
                                    {   //There are other people on the squad, transfer it to someone
                                        InfServer.Database.player transferPlayer = squadmates.FirstOrDefault(p => p.id != dbplayer.id);
                                        dbplayer.squad1.owner = transferPlayer.id;
                                        db.SubmitChanges();
                                        zone._server.sendMessage(zone, transferPlayer.alias1.name, "You have been promoted to squad captain of " + transferPlayer.squad1.name);
                                    }
                                }
                            }

                            dbplayer.squad = squad.id;
                            db.SubmitChanges();
                            zone._server.sendMessage(zone, dbplayer.alias1.name, "You have joined " + squad.name);
                            zone._server.sendMessage(zone, dbplayer.alias1.name, "Please relog to complete the process.");
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
                                InfServer.Database.player sender = db.players.FirstOrDefault(p => string.Compare(p.alias1.name, pkt.sender, true) == 0 && p.zone == zone._zone.id);
                                if (sender == null)
                                    return;

                                SortedDictionary<string, string> powered = new SortedDictionary<string, string>();
                                string pAlias;
                                foreach (Zone z in zone._server._zones)
                                {
                                    foreach (KeyValuePair<int, Zone.Player> Player in z._players)
                                    {
                                        pAlias = Player.Value.alias;
                                        var alias = db.alias.SingleOrDefault(p => string.Compare(p.name, pAlias, true) == 0);
                                        if (alias == null)
                                            continue;
                                        if (alias.name == pkt.sender)
                                            continue;
                                        //Are they a global mod?
                                        if (alias.account1.permission > 0)
                                        {
                                            //Are they higher than us?
                                            if (alias.account1.permission > sender.alias1.account1.permission
                                                && alias.account1.permission > sender.permission)
                                                continue;
                                            powered.Add(pAlias, string.Format("*{0} - Lvl({1})", pAlias, alias.account1.permission.ToString()));
                                        }
                                        else
                                        {
                                            var player = db.zones.First(zones => zones.id == z._zone.id).players.First(p => p.alias1 == alias);
                                            if (player != null && player.permission > 0)
                                            {
                                                //Are they higher than us?
                                                if (player.permission > sender.permission
                                                    && player.alias1.account1.permission > sender.alias1.account1.permission)
                                                    continue;
                                                powered.Add(pAlias, string.Format("*{0} - Lvl({1})(dev)", pAlias, player.permission.ToString()));
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

                            InfServer.Database.alias alias = db.alias.SingleOrDefault(ali => string.Compare(ali.name, pkt.query, true) == 0);
                            if (alias == null)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Cannot find the specified alias.");
                                return;
                            }

                            bool found = false;
                            IQueryable<InfServer.Database.alias> foundAlias = db.alias.Where(d => (d.IPAddress.Equals(alias.IPAddress) || d.account == alias.account));
                            foreach (KeyValuePair<string, Zone.Player> player in zone._server._players)
                            {
                                foreach (InfServer.Database.alias p in foundAlias)
                                    if (player.Value.alias.Equals(p.name))
                                    {
                                        zone._server.sendMessage(zone, pkt.sender, string.Format("*Found: {0} Zone: {1} Arena: {2}", p.name, player.Value.zone._zone.name, !String.IsNullOrWhiteSpace(player.Value.arena) ? player.Value.arena : "Unknown Arena"));
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

                        var silencedAlias = db.alias.FirstOrDefault(a => a.name.ToLower() == data[0].ToLower());

                        if (silencedAlias == null)
                        {
                            zone._server.sendMessage(zone, pkt.sender, $"Alias \"{data[0]}\" not found.");
                            return;
                        }

                        var silencedAccount = db.accounts.First(t => t.id == silencedAlias.account);

                        if (silencedDuration < 0)
                        {
                            silencedDuration = 0;
                        }

                        var silencedTime = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();

                        silencedAccount.SilencedDuration = silencedDuration;
                        silencedAccount.SilencedAtMillisecondsUnix = silencedDuration == 0 ? 0 : silencedTime;

                        db.SubmitChanges();

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
            using (InfServer.Database.InfantryDataContext db = zone._server.getContext())
            {
                InfServer.Database.alias dbplayer = db.alias.First(p => string.Compare(p.name, pkt.alias, true) == 0);

                //Lets check to see if they are banned already
                foreach (InfServer.Database.ban b in db.bans.Where(b => b.account == dbplayer.account1.id))
                {
                    //Same type?
                    if (b.type != (short)pkt.banType)
                        continue;

                    //Zone ban?
                    if ((short)pkt.banType == (int)Logic_Bans.Ban.BanType.ZoneBan)
                    {
                        if (b.zone == null)
                            continue;
                        if (b.zone != zone._zone.id)
                            continue;
                    }

                    if (pkt.time != 0)
                    {
                        //Dont update old bans
                        if (DateTime.Now > b.expires)
                            continue;
                        b.expires = b.expires.Value.AddMinutes(pkt.time);
                    }
                    //Are we unbanning them?
                    else if (pkt.time == 0)
                        b.expires = DateTime.Now;

                    b.reason = b.reason.ToString();

                    db.SubmitChanges();
                    return;
                }

                InfServer.Database.ban newBan = new InfServer.Database.ban();
                switch (pkt.banType)
                {
                    case CS_Ban<Zone>.BanType.zone:
                        {
                            //Check for updating the ban
                            newBan.type = (short)Logic_Bans.Ban.BanType.ZoneBan;
                            if (pkt.time == 0)
                                newBan.expires = DateTime.Now;
                            else
                                newBan.expires = DateTime.Now.AddMinutes(pkt.time);
                            newBan.created = DateTime.Now;
                            newBan.uid1 = pkt.UID1;
                            newBan.uid2 = pkt.UID2;
                            newBan.uid3 = pkt.UID3;
                            newBan.account = dbplayer.account;
                            newBan.IPAddress = dbplayer.IPAddress;
                            newBan.zone = zone._zone.id;
                            newBan.reason = pkt.reason;
                            newBan.name = dbplayer.name;
                        }
                        break;

                    case CS_Ban<Zone>.BanType.account:
                        {
                            newBan.type = (short)Logic_Bans.Ban.BanType.AccountBan;
                            if (pkt.time == 0)
                                newBan.expires = DateTime.Now;
                            else
                                newBan.expires = DateTime.Now.AddMinutes(pkt.time);
                            newBan.created = DateTime.Now;
                            newBan.uid1 = pkt.UID1;
                            newBan.uid2 = pkt.UID2;
                            newBan.uid3 = pkt.UID3;
                            newBan.account = dbplayer.account;
                            newBan.IPAddress = dbplayer.IPAddress;
                            newBan.reason = pkt.reason;
                            newBan.name = dbplayer.name;
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

                db.bans.InsertOnSubmit(newBan);
                db.SubmitChanges();

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
                var alias = db.alias.FirstOrDefault(x => x.name == pkt.alias);

                if (alias == null)
                {
                    zone._server.sendMessage(zone, pkt.sender, "Cannot find the specified alias.");
                    return;
                }

                Debug.Assert(alias.account1 != null, "Missing DB FK Constraint.");

                var bans = db.bans.Where(t => t.account == alias.account && t.type == (short)banType);

                if (banType == Logic_Bans.Ban.BanType.ZoneBan)
                {
                    bans = bans.Where(t => t.zone == zone._zone.id);
                }

                db.bans.DeleteAllOnSubmit(bans);
                db.SubmitChanges();

                zone._server.sendMessage(zone, pkt.sender, "Any bans of the requested type has been removed.");
            }
        }

        static public void Handle_CS_Stealth(CS_Stealth<Zone> pkt, Zone zone)
        {
            using (InfServer.Database.InfantryDataContext db = zone._server.getContext())
            {
                var dbalias = db.alias.First(p => string.Compare(p.name, pkt.sender, true) == 0);
                var zonePlayer = zone.getPlayer(pkt.sender);

                if (dbalias == null || zonePlayer == null)
                {
                    Console.WriteLine("Alias not found:" + pkt.sender);
                    return;
                }

                zonePlayer.stealth = pkt.stealth;

                dbalias.stealth = pkt.stealth ? 1 : 0;
                db.SubmitChanges();

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