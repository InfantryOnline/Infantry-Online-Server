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
                            zone._server.sendMessage(zone, pkt.sender, "Can't find the player structure of said alias.");
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
                                {
                                    //Lets delete the squad
                                    db.squads.DeleteOnSubmit(playerA.squad1);
                                    playerA.squad1 = null;
                                }
                            }
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
                        alias.IPAddress = paliasTo.IPAddress;
                        alias.timeplayed = 0;
                        alias.account1.id = paliasTo.account1.id;
                        db.SubmitChanges();
                        zone._server.sendMessage(zone, pkt.sender, "Alias transfer completed.");
                        break;

                    case CS_Alias<Zone>.AliasType.remove:
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
                                {
                                    //Lets delete the squad
                                    db.squads.DeleteOnSubmit(player.squad1);
                                    player.squad1 = null;
                                }
                            }
                            player.squad = null;
                            db.SubmitChanges();
                        }

                        //Now lets remove stats
                        db.stats.DeleteOnSubmit(player.stats1);
                        //Next the player structure
                        db.players.DeleteOnSubmit(player);
                        //Finally the alias
                        db.alias.DeleteOnSubmit(palias);
                        db.SubmitChanges();
                        zone._server.sendMessage(zone, pkt.sender, "Alias has been deleted.");
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
                //Data.DB.alias dbplayer = db.alias.First(p => p.name == pkt.alias);
           
                //Lets check to see if they are banned already
                bool update = false;
                foreach (Data.DB.ban b in db.bans.Where(b => b.account == dbplayer.account1.id))
                    if (b.type == (short)pkt.banType)
                    {
                        //It does exist, lets check and update it
                        if ((short)pkt.banType == (int)Logic_Bans.Ban.BanType.ZoneBan && b.zone != null && b.zone != zone._zone.id)
                            continue;
                        update = true;
                        break;
                    }

                //Create the new ban
                Data.DB.ban newBan = new Data.DB.ban();
                switch (pkt.banType)
                {
                    case CS_Ban<Zone>.BanType.zone:
                        {
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
                        }
                        break;
                }
                if (!update)
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
