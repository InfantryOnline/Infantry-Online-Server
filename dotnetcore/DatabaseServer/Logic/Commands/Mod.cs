using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
using Database;
using InfServer.Protocol;
using Microsoft.EntityFrameworkCore;
using Database.SqlServer;

namespace InfServer.Logic
{
    class Logic_ModCommands
    {
        /// <summary>
        /// Handles a mod/admin query.
        /// </summary>
        static public void Handle_CS_ModQuery(CS_ModQuery<Zone> pkt, Zone zone)
        {
            using (SqlServerDbContext db = zone._server.getContext())
            {
                switch (pkt.queryType)
                {
                    case CS_ModQuery<Zone>.QueryType.aliastransfer:
                        Handle_CS_ModQuery_AliasTransfer(pkt, zone, db);
                        break;

                    case CS_ModQuery<Zone>.QueryType.aliasremove:
                        Handle_CS_ModQuery_AliasRemove(pkt, zone, db);
                        break;

                    case CS_ModQuery<Zone>.QueryType.aliasrename:
                        Handle_CS_ModQuery_AliasRename(pkt, zone, db);
                        break;

                    case CS_ModQuery<Zone>.QueryType.mod:
                        Handle_CS_ModQuery_ModPermissionChange(pkt, zone, db);
                        break;

                    case CS_ModQuery<Zone>.QueryType.host:
                        Handle_CS_ModQuery_HostPermissionChange(pkt, zone, db);
                        break;

                    case CS_ModQuery<Zone>.QueryType.squadtransfer:
                        Handle_CS_ModQuery_SquadTransfer(pkt, zone, db);
                        break;

                    case CS_ModQuery<Zone>.QueryType.squadjoin:
                        Handle_CS_ModQuery_SquadJoin(pkt, zone, db);
                        break;

                    case CS_ModQuery<Zone>.QueryType.powered:
                        Handle_CS_ModQuery_GetPoweredAliases(pkt, zone);
                        break;

                    case CS_ModQuery<Zone>.QueryType.find:
                        Handle_CS_ModQuery_Find(pkt, zone, db);
                        break;

                    case CS_ModQuery<Zone>.QueryType.globalsilence:
                        Handle_CS_ModQuery_GlobalSilence(pkt, zone, db);
                        break;
                }
            }
        }

        private static void Handle_CS_ModQuery_GlobalSilence(CS_ModQuery<Zone> pkt, Zone zone, SqlServerDbContext db)
        {
            if (string.IsNullOrWhiteSpace(pkt.query))
            {
                zone._server.sendMessage(zone, pkt.sender, "Payload cannot be empty.");
                return;
            }

            var data = pkt.query.Split(':');
            int silencedDuration;

            if (data.Length != 2 || !int.TryParse(data[1], out silencedDuration))
            {
                zone._server.sendMessage(zone, pkt.sender, "Badly formatted packet. Please follow <alias>:<minutes> pattern.  0 minutes will unsilence upon next login.");
                return;
            }

            var targetAlias = data[0];

            var dbAlias = db.Aliases
                .Include(a => a.AccountNavigation)
                .FirstOrDefault(a => a.Name == targetAlias);

            if (dbAlias == null)
            {
                zone._server.sendMessage(zone, pkt.sender, $"Alias \"{data[0]}\" not found.");
                return;
            }

            var silencedAccount = db.Accounts.First(t => t.AccountId == dbAlias.AccountId);

            if (silencedDuration < 0)
            {
                silencedDuration = 0;
            }

            var silencedTime = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();

            silencedAccount.SilencedDuration = silencedDuration;
            silencedAccount.SilencedAtMillisecondsUnix = silencedDuration == 0 ? 0 : silencedTime;

            db.SaveChanges();

            var silencePkt = new SC_Silence<Zone>
            {
                alias = targetAlias,
                minutes = silencedDuration,
                silencedAtUnixMs = silencedTime
            };

            foreach(var z in zone._server._zones)
            {
                if (z == null)
                {
                    continue; // hack?
                }

                z._client.sendReliable(silencePkt);
            }
        }

        private static void Handle_CS_ModQuery_Find(CS_ModQuery<Zone> pkt, Zone zone, SqlServerDbContext db)
        {
            zone._server.sendMessage(zone, pkt.sender, "&Search Results:");

            Alias alias = db.Aliases.SingleOrDefault(ali => ali.Name == pkt.query);
            if (alias == null)
            {
                zone._server.sendMessage(zone, pkt.sender, "Cannot find the specified alias.");
                return;
            }

            bool found = false;
            var foundAlias = db.Aliases.Where(d => (d.IpAddress.Equals(alias.IpAddress) || d.AccountId == alias.AccountId)).ToList();

            foreach (KeyValuePair<string, Zone.Player> player in zone._server._players)
            {
                foreach (Alias p in foundAlias)
                {
                    if (player.Value.alias.Equals(p.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        zone._server.sendMessage(zone, pkt.sender, string.Format("*Found: {0} Zone: {1} Arena: {2}", p.Name, player.Value.zone._zone.Name, !String.IsNullOrWhiteSpace(player.Value.arena) ? player.Value.arena : "Unknown Arena"));
                        found = true;
                    }
                }
            }
            if (!found)
                zone._server.sendMessage(zone, pkt.sender, "Cannot find the specified alias.");
        }

        private static void Handle_CS_ModQuery_GetPoweredAliases(CS_ModQuery<Zone> pkt, Zone zone)
        {
            if (string.IsNullOrWhiteSpace(pkt.query))
            {
                zone._server.sendMessage(zone, pkt.sender, "Payload cannot be empty.");
                return;
            }

            if (pkt.query.Equals("list"))
            {
                var senderPlayer = zone.getPlayer(pkt.sender);
                var poweredPlayers = zone._server._players
                    .Where(p => p.Value.permission > 0 || p.Value.accountpermission > 0)
                    .Select(t => t.Value)
                    .ToList();

                bool sent = false;

                foreach (var p in poweredPlayers)
                {
                    string msg;

                    if (p.accountpermission > 0)
                    {
                        if (p.accountpermission > senderPlayer.accountpermission && p.accountpermission > senderPlayer.permission)
                        {
                            continue;
                        }

                        msg = string.Format("*{0} - Lvl({1})", p.alias, p.accountpermission.ToString());
                    }
                    else
                    {
                        if (p.permission > senderPlayer.accountpermission && p.permission > senderPlayer.permission)
                        {
                            continue;
                        }

                        msg = string.Format("*{0} - Lvl({1}) (dev)", p.alias, p.permission.ToString());
                    }

                    sent = true;
                    zone._server.sendMessage(zone, pkt.sender, msg);
                }

                if (!sent)
                {
                    zone._server.sendMessage(zone, pkt.sender, "Empty.");
                }
            }
        }

        private static void Handle_CS_ModQuery_SquadJoin(CS_ModQuery<Zone> pkt, Zone zone, SqlServerDbContext db)
        {
            if (string.IsNullOrWhiteSpace(pkt.aliasTo) || string.IsNullOrWhiteSpace(pkt.query))
            {
                zone._server.sendMessage(zone, pkt.sender, "Wrong format typed.");
                return;
            }

            //Lets find the player first
            Player dbplayer = db.Zones.First(z => z.ZoneId == zone._zone.ZoneId).Players.FirstOrDefault(p => p.AliasNavigation.Name == pkt.aliasTo);
            if (dbplayer == null)
            {
                zone._server.sendMessage(zone, pkt.sender, "Cannot find the player.");
                return;
            }

            //Lets find the squad in question
            Squad squad = db.Squads.FirstOrDefault(s => s.Name == pkt.query && s.ZoneId == zone._zone.ZoneId);
            if (squad == null)
            {
                zone._server.sendMessage(zone, pkt.sender, "Cannot find the specified squad.");
                return;
            }

            //Already squad joined somewhere?
            if (dbplayer.SquadId != null)
            {
                //Get his squad brothers! (if any...)
                IQueryable<Player> squadmates = db.Players.Where(p => p.ZoneId == dbplayer.ZoneId && p.SquadId != null && p.SquadId == dbplayer.SquadId);

                //Is he the captain?
                if (dbplayer.SquadNavigation.OwnerPlayerId == dbplayer.PlayerId)
                {   //We might need to dissolve the team!
                    if (squadmates.Count() == 1)
                    {   //He's the only one left on the squad... dissolve it!
                        var s1 = dbplayer.SquadNavigation;
                        dbplayer.SquadNavigation = null;
                        dbplayer.SquadId = null;
                        db.Squads.Remove(s1);

                        db.SaveChanges();
                    }
                    else
                    {   //There are other people on the squad, transfer it to someone
                        Player transferPlayer = squadmates.FirstOrDefault(p => p.PlayerId != dbplayer.PlayerId);
                        dbplayer.SquadNavigation.OwnerPlayerId = transferPlayer.PlayerId;
                        db.SaveChanges();
                        zone._server.sendMessage(zone, transferPlayer.AliasNavigation.Name, "You have been promoted to squad captain of " + transferPlayer.SquadNavigation.Name);
                    }
                }
            }

            dbplayer.SquadId = squad.SquadId;
            db.SaveChanges();
            zone._server.sendMessage(zone, dbplayer.AliasNavigation.Name, "You have joined " + squad.Name);
            zone._server.sendMessage(zone, dbplayer.AliasNavigation.Name, "Please rejoin the zone to complete the process.");
            zone._server.sendMessage(zone, pkt.sender, "Squad joining completed.");
        }

        private static void Handle_CS_ModQuery_SquadTransfer(CS_ModQuery<Zone> pkt, Zone zone, SqlServerDbContext db)
        {
            if (string.IsNullOrEmpty(pkt.aliasTo) || string.IsNullOrEmpty(pkt.query))
            {
                zone._server.sendMessage(zone, pkt.sender, "Wrong format typed.");
                return;
            }

            //Lets find the player first
            Player dbplayer = db.Zones.First(z => z.ZoneId == zone._zone.ZoneId).Players.FirstOrDefault(p => p.AliasNavigation.Name == pkt.aliasTo);
            if (dbplayer == null)
            {
                zone._server.sendMessage(zone, pkt.sender, "Cannot find the player.");
                return;
            }

            //Lets find the squad in question
            Squad squad = db.Squads.First(s => s.Name == pkt.query && s.ZoneId == zone._zone.ZoneId);
            if (squad == null)
            {
                zone._server.sendMessage(zone, pkt.sender, "Cannot find the specified squad.");
                return;
            }

            //Are they in a squad?
            if (dbplayer.SquadId != null)
            {
                //Is it the same squad?
                if (dbplayer.SquadId != squad.SquadId)
                {
                    zone._server.sendMessage(zone, pkt.sender, "That player isn't on the same squad.");
                    return;
                }
                //Transfer
                dbplayer.SquadNavigation.OwnerPlayerId = dbplayer.PlayerId;
            }
            else
            {
                dbplayer.SquadId = squad.SquadId;
                dbplayer.SquadNavigation.OwnerPlayerId = dbplayer.PlayerId;
            }
            db.SaveChanges();
            zone._server.sendMessage(zone, dbplayer.AliasNavigation.Name, "You have been promoted to squad captain of " + dbplayer.SquadNavigation.Name);
            zone._server.sendMessage(zone, dbplayer.AliasNavigation.Name, "Please rejoin the zone to complete the process.");
            zone._server.sendMessage(zone, pkt.sender, "Squad transferring is complete.");
        }

        private static void Handle_CS_ModQuery_HostPermissionChange(CS_ModQuery<Zone> pkt, Zone zone, SqlServerDbContext ctx)
        {
            if (string.IsNullOrEmpty(pkt.query))
            {
                zone._server.sendMessage(zone, pkt.sender, "Wrong format typed.");
                return;
            }

            var player = zone.getPlayer(pkt.query);

            if (player != null)
            {
                player.permission = pkt.level;

                var dbPlayer = ctx.Players.Find(player.dbid);
                dbPlayer.Permission = (short)pkt.level;

                ctx.SaveChanges();
            }
            else
            {
                var dbPlayer = ctx.Players
                    .Include(p => p.AliasNavigation)
                    .Where(p => p.ZoneId == zone._zone.ZoneId && p.AliasNavigation.Name == pkt.query)
                    .FirstOrDefault();

                if (dbPlayer == null)
                {
                    zone._server.sendMessage(zone, pkt.sender, "Cannot find the specified alias.");
                    return;
                }

                dbPlayer.Permission = (short)pkt.level;

                ctx.SaveChanges();
            }

            zone._server.sendMessage(zone, pkt.sender, $"Changing player {pkt.query} dev level to {pkt.level} has been completed.");
        }

        private static void Handle_CS_ModQuery_ModPermissionChange(CS_ModQuery<Zone> pkt, Zone zone, SqlServerDbContext ctx)
        {
            if (string.IsNullOrEmpty(pkt.query))
            {
                zone._server.sendMessage(zone, pkt.sender, "Wrong format typed.");
                return;
            }

            if (zone._server._players.ContainsKey(pkt.query))
            {
                var player = zone._server._players[pkt.query];
                player.accountpermission = pkt.level;

                var dbAccount = ctx.Accounts.Find(player.acctid);

                dbAccount.Permission = pkt.level;
                ctx.SaveChanges();
            }
            else
            {
                Alias dbAlias = ctx.Aliases
                    .Include(a => a.AccountNavigation)
                    .FirstOrDefault(a => a.Name == pkt.query);

                if (dbAlias == null)
                {
                    zone._server.sendMessage(zone, pkt.sender, "Cannot find the specified alias.");
                    return;
                }

                dbAlias.AccountNavigation.Permission = pkt.level;

                ctx.SaveChanges();
            }
            
            zone._server.sendMessage(zone, pkt.sender, $"Changing player {pkt.query} level to {pkt.level} has been completed.");
        }

        private static void Handle_CS_ModQuery_AliasRename(CS_ModQuery<Zone> pkt, Zone zone, SqlServerDbContext ctx)
        {
            if (string.IsNullOrEmpty(pkt.query))
            {
                zone._server.sendMessage(zone, pkt.sender, "Wrong format typed.");
                return;
            }

            // Renamed for ease of understanding.

            var oldAlias = pkt.aliasTo;
            var newAlias = pkt.query;        

            var newAliasExists = ctx.Aliases.Any(a => a.Name == newAlias);

            if (newAliasExists)
            {
                zone._server.sendMessage(zone, pkt.sender, $"Alias {newAlias} already in use.");
                return;
            }

            Alias dbOldAlias = ctx.Aliases.FirstOrDefault(a => a.Name == oldAlias);

            if (dbOldAlias == null)
            {
                zone._server.sendMessage(zone, pkt.sender, $"Alias {oldAlias} does not exist. Typo?");
                return;
            }

            dbOldAlias.Name = newAlias;
            ctx.SaveChanges();

            zone._server.sendMessage(zone, pkt.sender, $"Renamed: {oldAlias} => {newAlias}");
        }

        private static void Handle_CS_ModQuery_AliasRemove(CS_ModQuery<Zone> pkt, Zone zone, SqlServerDbContext db)
        {
            if (string.IsNullOrEmpty(pkt.query))
            {
                zone._server.sendMessage(zone, pkt.sender, "Wrong format typed.");
                return;
            }

            //Lets get all account related info then delete it
            Alias palias = db.Aliases.FirstOrDefault(a => a.Name == pkt.query);

            if (palias == null)
            {
                zone._server.sendMessage(zone, pkt.sender, "Cannot find the specified alias.");
                return;
            }

            var players = db.Players.Where(p => p.AliasId == palias.AliasId).ToList();

            // Remove all players under this alias
            foreach (var p in players)
            {
                if (p.SquadId != null)
                {
                    var squadmates = db.Players.Where(plyr => plyr.ZoneId == p.ZoneId && plyr.SquadId != null && plyr.SquadId == p.SquadId).ToList();

                    if (p.SquadNavigation.OwnerPlayerId == p.PlayerId)
                    {
                        if (squadmates.Count() > 1)
                        {
                            var otherPlayer = squadmates.FirstOrDefault(plyr => plyr.PlayerId != p.PlayerId);
                            //Since the player is the owner, lets just give it to someone else
                            otherPlayer.SquadNavigation.OwnerPlayerId = otherPlayer.PlayerId;
                        }
                        else if (squadmates.Count() == 1)
                        {
                            //Lets delete the squad
                            db.Squads.Remove(p.SquadNavigation);
                        }
                    }
                    p.SquadNavigation = null;
                    p.SquadId = null;
                }

                db.Players.Remove(p);
                db.Stats.Remove(p.StatsNavigation);

                var dailies = db.StatsDailies.Where(s => s.PlayerId == p.PlayerId);
                var weeklies = db.StatsWeeklies.Where(s => s.PlayerId == p.PlayerId);
                var monthlies = db.StatsMonthlies.Where(s => s.PlayerId == p.PlayerId);
                var yearlies = db.StatsYearlies.Where(s => s.PlayerId == p.PlayerId);

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

        private static void Handle_CS_ModQuery_AliasTransfer(CS_ModQuery<Zone> pkt, Zone zone, SqlServerDbContext ctx)
        {
            //
            // SQL Optimization TODO:
            //
            //  We don't necessarily need to do ctx.SaveChanges() here; we can
            //  get away with ExecuteUpdate() and ExecuteDelete() commands as we
            //  are not really tracking changes to begin with.
            //
            //

            if (string.IsNullOrEmpty(pkt.query) || string.IsNullOrEmpty(pkt.aliasTo))
            {
                zone._server.sendMessage(zone, pkt.sender, "Wrong format typed.");
                return;
            }

            Alias dbAliasRecipient = ctx.Aliases
                .Include(p => p.AccountNavigation)
                .FirstOrDefault(a => a.Name == pkt.aliasTo);

            if (dbAliasRecipient == null)
            {
                zone._server.sendMessage(zone, pkt.sender, "Cant find the recipient's alias.");
                return;
            }

            Alias dbAlias = ctx.Aliases
                .Include(p => p.Players)
                    .ThenInclude(p => p.SquadNavigation)
                .Include(p => p.Players)
                    .ThenInclude(p => p.StatsNavigation)
                .FirstOrDefault(a => a.Name == pkt.query);

            if (dbAlias == null)
            {
                zone._server.sendMessage(zone, pkt.sender, "Can't find the alias in question, maybe its not created yet.");
                return;
            }

            //
            // Remove all the players under this alias, deleting any links
            // to squads and stats they are associated with.
            //
            foreach (var p in dbAlias.Players)
            {
                if (p.SquadId != null)
                {
                    //
                    // Player is the owner; either transfer the squad to someone else, or
                    // remove the squad entirely.
                    //
                    if (p.SquadNavigation.OwnerPlayerId == p.PlayerId)
                    {
                        var squadmate = ctx.Players
                            .Where(sq =>
                                sq.ZoneId == p.ZoneId
                                && sq.SquadId == p.SquadId
                                && sq.PlayerId != p.PlayerId)
                            .Select(m => new { m.PlayerId })
                            .FirstOrDefault();

                        if (squadmate == null)
                        {
                            ctx.Squads.Remove(p.SquadNavigation);
                        }
                        else
                        {
                            p.SquadNavigation.OwnerPlayerId = squadmate.PlayerId;
                        }
                    }

                    //
                    // Unlink from squad.
                    //
                    p.SquadId = null;
                    p.SquadNavigation = null;
                }


                //
                // Remove them from stats and remove their Player object as well.
                //         

                var dailies = ctx.StatsDailies.Where(s => s.PlayerId == p.PlayerId);
                var weeklies = ctx.StatsWeeklies.Where(s => s.PlayerId == p.PlayerId);
                var monthlies = ctx.StatsMonthlies.Where(s => s.PlayerId == p.PlayerId);
                var yearlies = ctx.StatsYearlies.Where(s => s.PlayerId == p.PlayerId);

                ctx.StatsDailies.RemoveRange(dailies);
                ctx.StatsWeeklies.RemoveRange(weeklies);
                ctx.StatsMonthlies.RemoveRange(monthlies);
                ctx.StatsYearlies.RemoveRange(yearlies);

                // WARNING: FK relationship is wrong here.
                ctx.Players.Remove(p);
                ctx.Stats.Remove(p.StatsNavigation);
            }

            //
            // Lastly, perform the transfer.
            //

            dbAlias.IpAddress = dbAliasRecipient.IpAddress.Trim();
            dbAlias.TimePlayed = 0;
            dbAlias.AccountId = dbAliasRecipient.AccountId;

            ctx.SaveChanges();

            zone._server.sendMessage(zone, pkt.sender, "Alias transfer completed.");
        }

        /// <summary>
        /// Bans/unbans a player.
        /// </summary>
        static public void Handle_CS_Ban(CS_Ban<Zone> pkt, Zone zone)
        {
            long? accountId = null;
            string ipAddress = null;
            bool broadcastBan = false;

            //
            // Elide call to DB in case the player is online.
            //

            if (zone._server._players.ContainsKey(pkt.alias))
            {
                var p = zone._server._players[pkt.alias];

                accountId = p.acctid;
                ipAddress = p.IPAddress;
            }

            using (var ctx = zone._server.getContext())
            {
                //
                // Player is offline - so query the database to find their account information.
                //

                if (accountId == null)
                {
                    var dbAccount = ctx.Aliases
                        .Include(i => i.AccountNavigation)
                        .Where(a => a.Name == pkt.alias)
                        .Select(a => new { Id = a.AccountNavigation.AccountId, IpAddress = a.IpAddress })
                        .FirstOrDefault();

                    if (dbAccount == null)
                    {
                        // TODO: Log warning/error.

                        return;
                    }

                    accountId = dbAccount.Id;
                    ipAddress = dbAccount.IpAddress;
                }

                //
                // Check to see if they have an existing, active ban of that type; if so,
                // go ahead and update the record with that information. Construct the query
                // to best match the intent of the packet.
                //

                var type = (short)pkt.banType;

                var banQuery = ctx.Bans.Where(b => b.AccountId == accountId && b.Type == type);

                if (type == (short)Logic_Bans.Ban.BanType.ZoneBan)
                {
                    banQuery = banQuery.Where(b => b.ZoneId == zone._zone.ZoneId); // Filter bans for this zone only.
                }

                if (pkt.time != 0)
                {
                    banQuery = banQuery.Where(b => b.Expires > DateTime.Now); // Active bans.
                }

                var bans = banQuery.ToList();

                if (bans.Count == 0)
                {
                    if (pkt.time <= 0)
                    {
                        return; // Setting time to 0 clears the ban; no need to create anything.
                    }
                    else
                    {
                        var newBan = new Ban(); // Create a new ban of the appropriate type.

                        newBan.Created = DateTime.Now;
                        newBan.Expires = DateTime.Now.AddMinutes(pkt.time);
                        newBan.Uid1 = pkt.UID1;
                        newBan.Uid2 = pkt.UID2;
                        newBan.Uid3 = pkt.UID3;
                        newBan.AccountId = accountId;
                        newBan.IpAddress = ipAddress;
                        newBan.Reason = pkt.reason;
                        newBan.Name = pkt.alias;

                        if (type == (short)Logic_Bans.Ban.BanType.ZoneBan)
                        {
                            newBan.Type = (short)Logic_Bans.Ban.BanType.ZoneBan;
                            newBan.ZoneId = zone._zone.ZoneId;
                        }
                        else
                        {
                            // Treat any other type as an account ban.
                            newBan.Type = (short)Logic_Bans.Ban.BanType.AccountBan;
                        }

                        ctx.Bans.Add(newBan);
                        ctx.SaveChanges();

                        broadcastBan = true;
                    }
                }
                else
                {
                    foreach(var b in bans)
                    {
                        if (pkt.time == 0)
                        {
                            // Clear the ban.
                            b.Expires = DateTime.Now;
                        }
                        else
                        {
                            // Move (extend or subtract?) the active ban period.
                            b.Expires = b.Expires.Value.AddMinutes(pkt.time);
                            b.Reason = pkt.reason;

                            if (b.Expires > DateTime.Now)
                            {
                                // If the ban is active, broadcast to zones to
                                // remove the player.
                                broadcastBan = true;
                            }
                        }
                    }

                    ctx.SaveChanges();
                }
            }

            if (broadcastBan)
            {
                var dc = new SC_DisconnectPlayer<Zone>();
                dc.alias = pkt.alias;

                foreach (Zone z in zone._server._zones)
                {
                    if (z == null)
                    {
                        continue;
                    }

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

            using (var ctx = zone._server.getContext())
            {
                var alias = ctx.Aliases.FirstOrDefault(x => x.Name == pkt.alias);

                if (alias == null)
                {
                    zone._server.sendMessage(zone, pkt.sender, "Cannot find the specified alias.");
                    return;
                }

                var bans = ctx.Bans.Where(t => t.AccountId == alias.AccountId && t.Type == (short)banType);

                if (banType == Logic_Bans.Ban.BanType.ZoneBan)
                {
                    bans = bans.Where(t => t.ZoneId == zone._zone.ZoneId);
                }

                ctx.Bans.RemoveRange(bans);
                ctx.SaveChanges();

                zone._server.sendMessage(zone, pkt.sender, "Any bans of the requested type has been removed.");
            }
        }

        static public void Handle_CS_Stealth(CS_Stealth<Zone> pkt, Zone zone)
        {
            var zonePlayer = zone.getPlayer(pkt.sender);
            
            if (zonePlayer == null)
            {
                Console.WriteLine("Alias not found in zone:" + pkt.sender);
                return;
            }

            using (var ctx = zone._server.getContext())
            {
                int dbValue = pkt.stealth ? 1 : 0;

                var updateCount = ctx.Aliases.Where(a => a.AliasId == zonePlayer.aliasid)
                    .ExecuteUpdate(setters => setters.SetProperty(a => a.Stealth, dbValue));

                if (updateCount != 1)
                {
                    Console.WriteLine("Failed to update Stealth! Alias not found in db?" + pkt.sender);
                    return;
                }

                zonePlayer.stealth = pkt.stealth;
            }

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
