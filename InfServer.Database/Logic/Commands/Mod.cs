using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;

using InfServer.Protocol;
using InfServer.Data;

namespace InfServer.Logic
{
    class Logic_ModCommands
    {
        /// <summary>
        /// Handles a query packet
        /// </summary>
        static public void Handle_CS_ModQuery(CS_ModQuery<Zone> pkt, Zone zone)
        {
            using (InfantryDataContext db = zone._server.getContext())
            {
                switch (pkt.queryType)
                {
                    case CS_ModQuery<Zone>.QueryType.aliastransfer:
                        {
                            if (String.IsNullOrEmpty(pkt.query) || String.IsNullOrEmpty(pkt.aliasTo))
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Wrong format typed.");
                                return;
                            }

                            //Who the alias is going to
                            Data.DB.alias paliasTo = db.alias.FirstOrDefault(aTo => aTo.name.Equals(pkt.aliasTo));
                            if (paliasTo == null)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Cant find the recipient's alias.");
                                return;
                            }

                            //The alias in question
                            Data.DB.alias alias = db.alias.FirstOrDefault(a => a.name.Equals(pkt.query));
                            Data.DB.player playerA = db.players.FirstOrDefault(p => p.alias1.name.Equals(pkt.query));
                            if (alias == null)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Can't find the alias in question, maybe its not created yet.");
                                return;
                            }

                            if (playerA == null)
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

                            //Check for a squad
                            if (playerA.squad != null)
                            {
                                List<Data.DB.player> squadmates = new List<Data.DB.player>(db.players.Where(plyr => plyr.squad == playerA.squad && plyr.squad != null));
                                if (playerA.squad1.owner == playerA.id)
                                {
                                    if (squadmates.Count() > 1)
                                    {
                                        Random rand = new Random();
                                        Data.DB.player temp = squadmates[rand.Next(1, squadmates.Count())];
                                        //Since the player is the owner, lets just give it to someone else
                                        temp.squad1.owner = temp.id;
                                    }
                                    else if (squadmates.Count() == 1)
                                        //Lets delete the squad
                                        db.squads.DeleteOnSubmit(playerA.squad1);
                                }
                                playerA.squad1 = null;
                                playerA.squad = null;
                            }
                            //Lets delete stats/player structures
                            //Note: the server will treat this as a new alias and create structures
                            db.stats.DeleteOnSubmit(playerA.stats1);
                            db.players.DeleteOnSubmit(playerA);

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
                            if (String.IsNullOrEmpty(pkt.query))
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Wrong format typed.");
                                return;
                            }

                            //Lets get all account related info then delete it
                            Data.DB.alias palias = db.alias.FirstOrDefault(a => a.name.Equals(pkt.query));
                            Data.DB.player player = db.players.FirstOrDefault(p => p.alias1.name.Equals(pkt.query));
                            if (palias == null)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Cannot find the specified alias.");
                                return;
                            }

                            if (player == null)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Cannot find the specified player.");
                                return;
                            }

                            //Check for a squad
                            if (player.squad != null)
                            {
                                List<Data.DB.player> squadmates = new List<Data.DB.player>(db.players.Where(plyr => plyr.squad == player.squad && plyr.squad != null
                                                                                            && plyr.zone == player.zone));
                                if (player.squad1.owner == player.id)
                                {
                                    if (squadmates.Count() > 1)
                                    {
                                        Random rand = new Random();
                                        Data.DB.player temp = squadmates[rand.Next(1, squadmates.Count())];
                                        //Since the player is the owner, lets just give it to someone else
                                        temp.squad1.owner = temp.id;
                                    }
                                    else if (squadmates.Count() == 1)
                                        //Lets delete the squad
                                        db.squads.DeleteOnSubmit(player.squad1);
                                }
                                player.squad1 = null;
                                player.squad = null;
                            }

                            //Now lets remove stats
                            db.stats.DeleteOnSubmit(player.stats1);
                            //Next the player structure
                            db.players.DeleteOnSubmit(player);
                            //Finally the alias
                            db.alias.DeleteOnSubmit(palias);
                            db.SubmitChanges();
                            zone._server.sendMessage(zone, pkt.sender, "Alias has been deleted.");
                        }
                        break;

                    case CS_ModQuery<Zone>.QueryType.aliasrename:
                        {
                            if (String.IsNullOrEmpty(pkt.query))
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Wrong format typed.");
                                return;
                            }

                            //Get all account related info
                            Data.DB.alias paliasTo = db.alias.FirstOrDefault(aTo => aTo.name.Equals(pkt.aliasTo));
                            Data.DB.alias alias = db.alias.FirstOrDefault(a => a.name.Equals(pkt.query));
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
                            if (String.IsNullOrEmpty(pkt.query))
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Wrong format typed.");
                                return;
                            }

                            //Lets get all account related info
                            Data.DB.alias palias = db.alias.FirstOrDefault(a => a.name.Equals(pkt.query));
                            Data.DB.account account = db.accounts.FirstOrDefault(p => p.id == palias.account1.id);
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
                            if (String.IsNullOrEmpty(pkt.query))
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Wrong format typed.");
                                return;
                            }

                            //Lets get all account related info
                            Data.DB.player player = (from plyr in db.players
                                        where plyr.alias1.name == pkt.query && plyr.zone1 == zone._zone
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
                            if (String.IsNullOrEmpty(pkt.aliasTo) || String.IsNullOrEmpty(pkt.query))
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Wrong format typed.");
                                return;
                            }

                            //Lets find the player first
                            Data.DB.player dbplayer = db.zones.First(z => z.id == zone._zone.id).players.FirstOrDefault(p => p.alias1.name.Equals(pkt.aliasTo));
                            if (dbplayer == null)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Cannot find the player.");
                                return;
                            }

                            //Lets find the squad in question
                            Data.DB.squad squad = db.squads.First(s => s.name.Equals(pkt.query) && s.zone == zone._zone.id);
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
                            if (String.IsNullOrWhiteSpace(pkt.aliasTo) || String.IsNullOrWhiteSpace(pkt.query))
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Wrong format typed.");
                                return;
                            }

                            //Lets find the player first
                            Data.DB.player dbplayer = db.zones.First(z => z.id == zone._zone.id).players.FirstOrDefault(p => p.alias1.name.Equals(pkt.aliasTo));
                            if (dbplayer == null)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Cannot find the player.");
                                return;
                            }

                            //Lets find the squad in question
                            Data.DB.squad squad = db.squads.FirstOrDefault(s => s.name.Equals(pkt.query) && s.zone == zone._zone.id);
                            if (squad == null)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Cannot find the specified squad.");
                                return;
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
                            if (String.IsNullOrWhiteSpace(pkt.query))
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Payload cannot be empty.");
                                return;
                            }

                            if (pkt.query.Equals("list"))
                            {
                                Data.DB.player sender = db.players.FirstOrDefault(p => p.alias1.name.Equals(pkt.sender) && p.zone == zone._zone.id);
                                if (sender == null)
                                    return;

                                Console.WriteLine(sender.alias1.name);
                                SortedDictionary<string, string> powered = new SortedDictionary<string, string>();
                                string pAlias;
                                foreach (Zone z in zone._server._zones)
                                {
                                    foreach (KeyValuePair<int, Zone.Player> Player in z._players)
                                    {
                                        pAlias = Player.Value.alias;
                                        var alias = db.alias.SingleOrDefault(p => p.name.Equals(pAlias));
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
                                            powered.Add(pAlias, String.Format("*{0} - Lvl({1})", pAlias, alias.account1.permission.ToString()));
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
                                                powered.Add(pAlias, String.Format("*{0} - Lvl({1})(dev)", pAlias, player.permission.ToString()));
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
                            zone._server.sendMessage(zone, pkt.sender, "*" + pkt.query);
                            zone._server.sendMessage(zone, pkt.sender, "&Search Results:");

                            Data.DB.alias alias = db.alias.SingleOrDefault(ali => ali.name.Equals(pkt.query));
                            if (alias == null)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Cannot find the specified alias.");
                                return;
                            }

                            IQueryable<Data.DB.alias> foundAlias = db.alias.Where(d => (d.IPAddress.Equals(alias.IPAddress) || d.account == alias.account));
                            foreach (KeyValuePair<string, Zone.Player> player in zone._server._players)
                            {
                                foreach(Data.DB.alias p in foundAlias)
                                    if (player.Value.alias.Equals(p.name) && !String.IsNullOrWhiteSpace(player.Value.arena))
                                    {
                                        zone._server.sendMessage(zone, pkt.sender, String.Format("*Found: {0} Zone: {1} Arena: {2}", p.name, player.Value.zone._zone.name, player.Value.arena));
                                        return;
                                    }
                            }

                            zone._server.sendMessage(zone, pkt.sender, "Cannot find the specified alias.");
                        }
                        break;
                }
            }
        }
                        
        /// <summary>
        /// Handles a query packet
        /// </summary>
        static public void Handle_CS_Ban(CS_Ban<Zone> pkt, Zone zone)
        {
            using (InfantryDataContext db = zone._server.getContext())
            {
                Data.DB.alias dbplayer = db.alias.First(p => p.name.Equals(pkt.alias));
           
                //Lets check to see if they are banned already
                foreach (Data.DB.ban b in db.bans.Where(b => b.account == dbplayer.account1.id))
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
                        b.expires = b.expires.AddMinutes(pkt.time);
                    }
                    //Are we unbanning them?
                    else if (pkt.time == 0)
                        b.expires = DateTime.Now;

                    b.reason = b.reason.ToString();

                    db.SubmitChanges();
                    return;
                }

                Data.DB.ban newBan = new Data.DB.ban();
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

                    case CS_Ban<Zone>.BanType.ip:
                        {
                            newBan.type = (short)Logic_Bans.Ban.BanType.IPBan;
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

                    case CS_Ban<Zone>.BanType.global:
                        {
                            newBan.type = (short)Logic_Bans.Ban.BanType.GlobalBan;
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
                }
                db.bans.InsertOnSubmit(newBan);
                db.SubmitChanges();
            }
        }
                 

        /// <summary>
        /// Registers all handlers
        /// </summary>
        [RegistryFunc]
        static public void Register()
        {
            CS_Ban<Zone>.Handlers += Handle_CS_Ban;
            CS_ModQuery<Zone>.Handlers += Handle_CS_ModQuery;
        }
    }
}
