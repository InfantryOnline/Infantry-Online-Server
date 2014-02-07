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
        static public void Handle_CS_Alias(CS_Alias<Zone> pkt, Zone zone)
        {
            using (InfantryDataContext db = zone._server.getContext())
            {
                switch (pkt.aliasType)
                {
                    case CS_Alias<Zone>.AliasType.transfer:
                        {

                            if (pkt.alias == "" || pkt.aliasTo == "")
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
                            Data.DB.alias alias = db.alias.FirstOrDefault(a => a.name.Equals(pkt.alias));
                            Data.DB.player playerA = db.players.FirstOrDefault(p => p.alias1.name.Equals(pkt.alias));
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
                            /*
                             Data.DB.player newP = new Data.DB.player();
                             playerA = newP;
                             //Lets clear all stats and create a new stat row
                             Data.DB.stats newStat = new Data.DB.stats();
                             playerA.stats1 = newStat;
                             */
                            //Now lets transfer
                            alias.IPAddress = paliasTo.IPAddress.Trim();
                            alias.timeplayed = 0;
                            alias.account = paliasTo.account;
                            alias.account1 = paliasTo.account1;
                            db.SubmitChanges();
                            zone._server.sendMessage(zone, pkt.sender, "Alias transfer completed.");
                        }
                        break;

                    case CS_Alias<Zone>.AliasType.remove:
                        {
                            if (pkt.alias == "")
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Wrong format typed.");
                                return;
                            }

                            //Lets get all account related info then delete it
                            Data.DB.alias palias = db.alias.FirstOrDefault(a => a.name.Equals(pkt.alias));
                            Data.DB.player player = db.players.FirstOrDefault(p => p.alias1.name.Equals(pkt.alias));
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

                    case CS_Alias<Zone>.AliasType.rename:
                        {
                            if (pkt.alias == "")
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Wrong format typed.");
                                return;
                            }

                            //Get all account related info
                            Data.DB.alias paliasTo = db.alias.FirstOrDefault(aTo => aTo.name.Equals(pkt.aliasTo));
                            Data.DB.alias alias = db.alias.FirstOrDefault(a => a.name.Equals(pkt.alias));
                            if (paliasTo == null)
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Cannot find the specified alias.");
                                return;
                            }

                            if (alias == null)
                            {
                                paliasTo.name = pkt.alias;
                                db.SubmitChanges();
                                zone._server.sendMessage(zone, pkt.sender, "Renamed player " + paliasTo.name + " to " + pkt.alias + " has been completed.");
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

                            paliasTo.name = pkt.alias;
                            db.SubmitChanges();
                            zone._server.sendMessage(zone, pkt.sender, "Renamed player " + paliasTo.name + " to " + pkt.alias + " has been completed.");
                        }
                        break;

                    case CS_Alias<Zone>.AliasType.mod:
                        {
                            if (pkt.alias == "")
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Wrong format typed.");
                                return;
                            }

                            //Lets get all account related info then delete it
                            Data.DB.alias palias = db.alias.FirstOrDefault(a => a.name.Equals(pkt.alias));
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

                    case CS_Alias<Zone>.AliasType.dev:
                        {
                            if (pkt.alias == "")
                            {
                                zone._server.sendMessage(zone, pkt.sender, "Wrong format typed.");
                                return;
                            }

                            //Lets get all account related info
                            Data.DB.player player = db.players.FirstOrDefault(p => p.alias1.name.Equals(pkt.alias));
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
                Data.DB.ban newBan = new Data.DB.ban();
                bool found = false;
           
                //Lets check to see if they are banned already
                foreach (Data.DB.ban b in db.bans.Where(b => b.account == dbplayer.account1.id))
                    if (b.type == (short)pkt.banType && b.name.Equals(dbplayer.name))
                    {
                        //It does exist, lets check and update it
                        if ((short)pkt.banType == (int)Logic_Bans.Ban.BanType.ZoneBan && b.zone != null && b.zone != zone._zone.id)
                            continue;

                        //Lets update what we need then submit
                        if (pkt.time != 0)
                            b.expires = b.expires.AddMinutes(pkt.time);
                        else if (pkt.time == 0)
                            b.expires = DateTime.Now;

                        newBan.account = dbplayer.account;
                        newBan.created = b.created;
                        newBan.expires = b.expires;
                        newBan.IPAddress = dbplayer.IPAddress;
                        newBan.reason = b.reason;
                        newBan.type = b.type;
                        newBan.uid1 = b.uid1;
                        newBan.uid2 = b.uid2;
                        newBan.uid3 = b.uid3;
                        if ((short)pkt.banType == (int)Logic_Bans.Ban.BanType.ZoneBan && b.zone != null && b.zone == zone._zone.id)
                            newBan.zone = b.zone;
                        db.bans.DeleteOnSubmit(b);
                        found = true;
                        break;
                    }

                if (found)
                {
                    //Lets insert and submit
                    db.bans.InsertOnSubmit(newBan);
                    db.SubmitChanges();
                    return;
                }

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
            CS_Alias<Zone>.Handlers += Handle_CS_Alias;
        }
    }
}
