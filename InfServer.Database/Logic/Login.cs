﻿using System;
using System.Collections.Generic;
using System.Linq;

using InfServer.Data;
using InfServer.Network;
using InfServer.Protocol;
using System.Globalization;
using InfServer.Data.DB;
using System.Text.RegularExpressions;

namespace InfServer.Logic
{	// Logic_Login Class
    /// Handles everything related to the zone server's login
    ///////////////////////////////////////////////////////
    class Logic_Login
    {
        public static string RemoveIllegalCharacters(string str)
        {   //Remove non-Infantry characters... trim whitespaces, and remove duplicate spaces
            string sb = "";
            foreach (char c in str)
                if (c >= ' ' && c <= '~')
                    sb += c;
            //Get rid of duplicate spaces
            Regex regex = new Regex(@"[ ]{2,}", RegexOptions.None);
            sb = regex.Replace(sb, @" ");
            //Trim it
            sb = sb.Trim();
            //We have our new Infantry compatible string!
            return sb;
        }

        /// <summary>
        /// Handles the zone login request packet 
        /// </summary>
        static public void Handle_CS_Auth(CS_Auth<Zone> pkt, Client<Zone> client)
        {	//Note the login request
            Log.write(TLog.Normal, "Login request from ({0}): {1} / {2}", client._ipe, pkt.zoneID, pkt.password);

            //Attempt to find the associated zone
            DBServer server = client._handler as DBServer;
            Data.DB.zone dbZone;

            using (InfantryDataContext db = server.getContext())
                dbZone = db.zones.SingleOrDefault(z => z.id == pkt.zoneID);

            //Does the zone exist?
            if (dbZone == null)
            {	//Reply with failure
                SC_Auth<Zone> reply = new SC_Auth<Zone>();

                reply.result = SC_Auth<Zone>.LoginResult.Failure;
                reply.message = "Invalid zone.";
                client.sendReliable(reply);
                return;
            }

            //Are the passwords a match?
            if (dbZone.password != pkt.password)
            {	//Oh dear.
                SC_Auth<Zone> reply = new SC_Auth<Zone>();
                reply.result = SC_Auth<Zone>.LoginResult.BadCredentials;
                client.sendReliable(reply);
                return;
            }

            //Great! Escalate our client object to a zone
            Zone zone = new Zone(client, server, dbZone);
            client._obj = zone;
            zone._zone.active = 1; //Set it as active

            server._zones.Add(zone);

            //Called on connection close / timeout
            zone._client.Destruct += delegate(NetworkClient nc)
            {
                zone.destroy();
            };

            //Success!
            SC_Auth<Zone> success = new SC_Auth<Zone>();

            success.result = SC_Auth<Zone>.LoginResult.Success;
            success.message = dbZone.notice;

            client.sendReliable(success);

            using (InfantryDataContext db = server.getContext())
            {
                //Update and activate the zone for our directory server
                Data.DB.zone zoneentry = db.zones.SingleOrDefault(z => z.id == pkt.zoneID);

                zoneentry.name = pkt.zoneName;
                zoneentry.description = pkt.zoneDescription;
                zoneentry.ip = pkt.zoneIP;
                zoneentry.port = pkt.zonePort;
                zoneentry.advanced = Convert.ToInt16(pkt.zoneIsAdvanced);
                zoneentry.active = 1;

                db.SubmitChanges();
            }
            Log.write("Successful login from {0} ({1})", dbZone.name, client._ipe);
        }

        /// <summary>
        /// Handles the zone login request packet 
        /// </summary>
        static public void Handle_CS_PlayerLogin(CS_PlayerLogin<Zone> pkt, Zone zone)
        {	//Make a note
            Log.write(TLog.Inane, "Player login request for '{0}' on '{1}'", pkt.alias, zone);

            SC_PlayerLogin<Zone> plog = new SC_PlayerLogin<Zone>();
            plog.player = pkt.player;

            if (String.IsNullOrWhiteSpace(pkt.alias))
            {
                plog.bSuccess = false;
                plog.loginMessage = "Please enter an alias.";

                zone._client.send(plog);
                return;
            }

            //Are they using the launcher?
            if (String.IsNullOrWhiteSpace(pkt.ticketid))
            {	//They're trying to trick us, jim!
                plog.bSuccess = false;
                plog.loginMessage = "Please use the Infantry launcher to run the game.";

                zone._client.send(plog);
                return;
            }

            if (pkt.ticketid.Contains(':'))
            {   //They're using the old, outdated launcher
                plog.bSuccess = false;
                plog.loginMessage = "Please use the updated launcher from the website.";

                zone._client.send(plog);
                return;
            }


            using (InfantryDataContext db = zone._server.getContext())
            {
                Data.DB.player player = null;
                Data.DB.account account = db.accounts.SingleOrDefault(acct => acct.ticket.Equals(pkt.ticketid));

                if (account == null)
                {	//They're trying to trick us, jim!
                    plog.bSuccess = false;
                    plog.loginMessage = "Your session id has expired. Please re-login.";

                    zone._client.send(plog);
                    return;
                }

                //Is there already a player online under this account?
                if (!DBServer.bAllowMulticlienting && zone._server._zones.Any(z => z.hasAccountPlayer(account.id)))
                {
                    plog.bSuccess = false;
                    plog.loginMessage = "Account is currently in use.";

                    zone._client.send(plog);
                    return;
                }

                //Check for IP and UID bans
                Logic_Bans.Ban banned = Logic_Bans.checkBan(pkt, db, account, zone._zone.id);

                if (banned.type == Logic_Bans.Ban.BanType.GlobalBan)
                {   //We don't respond to globally banned player requests
                    plog.bSuccess = false;
                    plog.loginMessage = "Banned.";

                    Log.write(TLog.Warning, "Failed login: " + zone._zone.name + " Alias: " + pkt.alias + " Reason: " + banned.type.ToString());
                    zone._client.send(plog);
                    return;
                }

                if (banned.type == Logic_Bans.Ban.BanType.IPBan)
                {   //Their IP has been banned, make something up!
                    plog.bSuccess = false;
                    plog.loginMessage = "You have been temporarily suspended until " + banned.expiration.ToString("f", CultureInfo.CreateSpecificCulture("en-US"));

                    Log.write(TLog.Warning, "Failed login: " + zone._zone.name + " Alias: " + pkt.alias + " Reason: " + banned.type.ToString());
                    zone._client.send(plog);
                    return;
                }

                if (banned.type == Logic.Logic_Bans.Ban.BanType.ZoneBan)
                {   //They've been blocked from entering the zone, tell them how long they've got left on their ban
                    plog.bSuccess = false;
                    plog.loginMessage = "You have been temporarily suspended from this zone until " + banned.expiration.ToString("f", CultureInfo.CreateSpecificCulture("en-US"));

                    Log.write(TLog.Warning, "Failed login: " + zone._zone.name + " Alias: " + pkt.alias + " Reason: " + banned.type.ToString());
                    zone._client.send(plog);
                    return;
                }

                if (banned.type == Logic.Logic_Bans.Ban.BanType.AccountBan)
                {   //They've been blocked from entering any zone, tell them when to come back
                    plog.bSuccess = false;
                    plog.loginMessage = "Your account has been temporarily suspended until " + banned.expiration.ToString("f", CultureInfo.CreateSpecificCulture("en-US"));

                    Log.write(TLog.Warning, "Failed login: " + zone._zone.name + " Alias: " + pkt.alias + " Reason: " + banned.type.ToString());
                    zone._client.send(plog);
                    return;
                }

                //They made it!

                //We have the account associated!
                plog.permission = (PlayerPermission)account.permission;
                if (account.permission > (int)PlayerPermission.Sysop)
                    plog.permission = PlayerPermission.Sysop;

                // Jovan - Allow symbols as login names for admins.
                var pktAlias = pkt.alias;

                if (plog.permission != PlayerPermission.Sysop)
                {
                    

                    try
                    {
                        if (!char.IsLetterOrDigit(pktAlias, 0) ||
                            char.IsWhiteSpace(pktAlias, 0) ||
                            char.IsWhiteSpace(pktAlias, pktAlias.Length - 1) ||
                            pktAlias != RemoveIllegalCharacters(pktAlias))
                        {   //Boot him..
                            plog.bSuccess = false;
                            plog.loginMessage = "Alias contains illegal characters, must start with a letter or number and cannot end with a space.";
                            zone._client.send(plog);
                            return;
                        }
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        Log.write(TLog.Warning, "Player login name is {0}", pktAlias);
                        plog.bSuccess = false;
                        plog.loginMessage = "Alias contains illegal characters, must start with a letter or number and cannot end with a space.";
                        zone._client.send(plog);
                        return;
                    }
                }
                else
                {
                    if (!char.IsLetterOrDigit(pktAlias[0]) && pkt.bCreateAlias)
                    {
                        plog.bSuccess = false;
                        plog.loginMessage = "Symbolic aliases must be created through the database directly. Contact Jovan if anything.";

                        zone._client.send(plog);
                        return;
                    }
                }

                //Attempt to find the related alias
                Data.DB.alias alias = db.alias.SingleOrDefault(a => a.name.Equals(pkt.alias));
                Data.DB.stats stats = null;

                //Is there already a player online under this alias?
                if (alias != null && zone._server._zones.Any(z => z.hasAliasPlayer(alias.id)))
                {
                    plog.bSuccess = false;
                    plog.loginMessage = "Alias is currently in use.";

                    zone._client.send(plog);
                    return;
                }

                if (alias == null && !pkt.bCreateAlias)
                {	//Prompt him to create a new alias if he has room
                    int maxAliases = 30;
                    if (account.alias.Count < maxAliases)
                    {   //He has space! Prompt him to make a new alias
                        plog.bSuccess = false;
                        plog.bNewAlias = true;

                        zone._client.send(plog);
                        return;
                    }
                    else
                    {
                        plog.bSuccess = false;
                        plog.loginMessage = "Your account has reached the maximum number of " + maxAliases.ToString() + " aliases allowed.";

                        zone._client.send(plog);
                        return;
                    }
                }
                else if (alias == null && pkt.bCreateAlias)
                {	//We want to create a new alias!
                    alias = new InfServer.Data.DB.alias();

                    alias.name = pkt.alias;
                    alias.creation = DateTime.Now;
                    alias.account1 = account;
                    alias.IPAddress = pkt.ipaddress;
                    alias.lastAccess = DateTime.Now;
                    alias.timeplayed = 0;

                    db.alias.InsertOnSubmit(alias);

                    Log.write(TLog.Normal, "Creating new alias '{0}' on account '{1}'", pkt.alias, account.name);
                }
                else if (alias != null)
                {	//We can't recreate an existing alias or login to one that isn't ours..
                    if (pkt.bCreateAlias ||
                        alias.account1 != account)
                    {
                        plog.bSuccess = false;
                        plog.loginMessage = "The specified alias already exists.";

                        zone._client.send(plog);
                        return;
                    }
                }

                //Do we have a player row for this zone?
                player = db.players.SingleOrDefault(
                    plyr => plyr.alias1 == alias && plyr.zone1 == zone._zone);

                if (player == null)
                {	//We need to create another!
                    player = new InfServer.Data.DB.player();

                    player.squad1 = null;
                    player.zone = zone._zone.id;
                    player.alias1 = alias;

                    player.lastAccess = DateTime.Now;
                    player.permission = 0;

                    //Create a blank stats row
                    stats = new InfServer.Data.DB.stats();

                    stats.zone = zone._zone.id;
                    player.stats1 = stats;

                    db.stats.InsertOnSubmit(stats);
                    db.players.InsertOnSubmit(player);

                    //It's a first-time login, so no need to load stats
                    plog.bFirstTimeSetup = true;
                }
                else
                {	//Load the player details and stats!
                    plog.banner = player.banner;
                    plog.permission = (PlayerPermission)Math.Max(player.permission, (int)plog.permission);

                    if (player.permission > account.permission)
                        //He's a dev here, set the bool
                        plog.developer = true;

                    //Check for admin
                    Data.DB.alias aliasMatch = null;
                    foreach (string str in Logic_Admins.ServerAdmins)
                    {
                        if ((aliasMatch = db.alias.SingleOrDefault(a => string.Compare(a.name, str, true) == 0)) != null)
                        {
                            if (account.id == aliasMatch.account && account.permission >= 5)
                            {
                                plog.admin = true;
                                break;
                            }
                        }
                    }
                    //If he isn't part of the admin list, he doesn't get powers
                    if (!plog.admin && account.permission >= 5)
                    { account.permission = 0; }

                    plog.squad = (player.squad1 == null) ? "" : player.squad1.name;
                    if (player.squad1 != null)
                        plog.squadID = player.squad1.id;

                    stats = player.stats1;

                    plog.stats.zonestat1 = stats.zonestat1;
                    plog.stats.zonestat2 = stats.zonestat2;
                    plog.stats.zonestat3 = stats.zonestat3;
                    plog.stats.zonestat4 = stats.zonestat4;
                    plog.stats.zonestat5 = stats.zonestat5;
                    plog.stats.zonestat6 = stats.zonestat6;
                    plog.stats.zonestat7 = stats.zonestat7;
                    plog.stats.zonestat8 = stats.zonestat8;
                    plog.stats.zonestat9 = stats.zonestat9;
                    plog.stats.zonestat10 = stats.zonestat10;
                    plog.stats.zonestat11 = stats.zonestat11;
                    plog.stats.zonestat12 = stats.zonestat12;

                    plog.stats.kills = stats.kills;
                    plog.stats.deaths = stats.deaths;
                    plog.stats.killPoints = stats.killPoints;
                    plog.stats.deathPoints = stats.deathPoints;
                    plog.stats.assistPoints = stats.assistPoints;
                    plog.stats.bonusPoints = stats.bonusPoints;
                    plog.stats.vehicleKills = stats.vehicleKills;
                    plog.stats.vehicleDeaths = stats.vehicleDeaths;
                    plog.stats.playSeconds = stats.playSeconds;

                    plog.stats.cash = stats.cash;
                    plog.stats.inventory = new List<PlayerStats.InventoryStat>();
                    plog.stats.experience = stats.experience;
                    plog.stats.experienceTotal = stats.experienceTotal;
                    plog.stats.skills = new List<PlayerStats.SkillStat>();

                    //Convert the binary inventory/skill data
                    if (player.inventory != null)
                        DBHelpers.binToInventory(plog.stats.inventory, player.inventory);
                    if (player.skills != null)
                        DBHelpers.binToSkills(plog.stats.skills, player.skills);
                }

                //Rename him
                plog.alias = alias.name;

                //Try and submit any new rows before we try and use them
                try
                {
                    db.SubmitChanges();
                }
                catch (Exception e)
                {
                    plog.bSuccess = false;
                    plog.loginMessage = "Unable to create new player / alias, please try again.";
                    Log.write(TLog.Exception, "Exception adding player or alias to DB: {0}", e);
                    zone._client.send(plog);
                    return;
                }

                //Add them
                if (zone.newPlayer(pkt.player.id, alias.name, player))
                {
                    plog.bSuccess = true;
                    Log.write("Player '{0}' logged into zone '{1}'", alias.name, zone._zone.name);

                    //Modify his alias IP address and access times
                    alias.IPAddress = pkt.ipaddress.Trim();
                    alias.lastAccess = DateTime.Now;

                    //Change it
                    db.SubmitChanges();
                }
                else
                {
                    plog.bSuccess = false;
                    plog.loginMessage = "Unknown login failure.";
                    Log.write("Failed adding player '{0}' from '{1}'", alias.name, zone._zone.name);
                }

                //Give them an answer
                zone._client.sendReliable(plog);
            }
        }

        /// <summary>
        /// Handles a player leave notification 
        /// </summary>
        static public void Handle_CS_PlayerLeave(CS_PlayerLeave<Zone> pkt, Zone zone)
        {	//He's gone!
            if (zone == null)
            {
                Log.write(TLog.Error, "Handle_CS_PlayerLeave(): Called with null zone.");
                return;
            }
            if (string.IsNullOrWhiteSpace(pkt.alias))
            {
                Log.write(TLog.Warning, "Handle_CS_PlayerLeave(): No alias provided.");
            }

            Zone.Player p = zone.getPlayer(pkt.alias);
            if (p == null)
            {
                Log.write(TLog.Warning, "Handle_CS_PlayerLeave(): Player '{0}' not found by alias.", pkt.alias);
                //Don't return until I have a better grasp of this
            }

            //Remove the player from the zone
            zone.lostPlayer(pkt.player.id);

            Log.write("Player '{0}' left zone '{1}'", pkt.alias, zone._zone.name);

            // Update their playtime
            using (InfantryDataContext db = zone._server.getContext())
            {
                Data.DB.alias alias = db.alias.SingleOrDefault(a => a.name.Equals(pkt.alias));
                //Data.DB.alias alias = db.alias.SingleOrDefault(a => a.id == p.aliasid);
                //If person was loaded correctly, save their info
                if (alias != null)
                {
                    TimeSpan ts = DateTime.Now - alias.lastAccess;
                    Int64 minutes = ts.Minutes;
                    alias.timeplayed += minutes;

                    db.SubmitChanges();
                }
            }
        }

        /// <summary>
        /// Handles a zone update packet and updates the database
        /// </summary>
        static public void Handle_CS_ZoneUpdate(CS_ZoneUpdate<Zone> pkt, Zone zone)
        {
            using (InfantryDataContext db = zone._server.getContext())
            {
                Data.DB.zone zEntry = db.zones.SingleOrDefault(z => z.id == zone._zone.id);
                if (zEntry != null)
                {
                    //Update the zone for our directory server
                    zEntry.name = pkt.zoneName;
                    zEntry.description = pkt.zoneDescription;
                    zEntry.ip = pkt.zoneIP;
                    zEntry.port = pkt.zonePort;
                    zEntry.advanced = Convert.ToInt16(pkt.zoneIsAdvanced);
                    zEntry.active = pkt.zoneActive;

                    db.SubmitChanges();
                }
            }
        }

        /// <summary>
        /// Handles a graceful zone disconnect
        /// </summary>
        static public void Handle_Disconnect(Disconnect<Zone> pkt, Zone zone)
        {
            Log.write("{0} disconnected gracefully", zone._zone.name);

            //Close our connection; calls zone._client.Destruct
            zone._client.destroy();
        }

        /// <summary>
        /// Registers all handlers
        /// </summary>
        [RegistryFunc]
        static public void Register()
        {
            CS_Auth<Zone>.Handlers += Handle_CS_Auth;
            CS_PlayerLogin<Zone>.Handlers += Handle_CS_PlayerLogin;
            CS_PlayerLeave<Zone>.Handlers += Handle_CS_PlayerLeave;
            CS_ZoneUpdate<Zone>.Handlers += Handle_CS_ZoneUpdate;
            Disconnect<Zone>.Handlers += Handle_Disconnect;
        }
    }
}