using System;
using System.Collections.Generic;
using System.Linq;
using Database;
using InfServer.Data;
using InfServer.Network;
using InfServer.Protocol;
using System.Globalization;
using Database.SqlServer;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

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
            Database.Zone dbZone;

            using (var db = server.getContext())
            {
                dbZone = db.Zones.SingleOrDefault(z => z.ZoneId == pkt.zoneID);

                //Does the zone exist?
                if (dbZone == null)
                {   //Reply with failure
                    SC_Auth<Zone> reply = new SC_Auth<Zone>();

                    reply.result = SC_Auth<Zone>.LoginResult.Failure;
                    reply.message = "Invalid zone.";
                    client.sendReliable(reply);
                    return;
                }

                //Are the passwords a match?
                if (dbZone.Password != pkt.password)
                {   //Oh dear.
                    SC_Auth<Zone> reply = new SC_Auth<Zone>();
                    reply.result = SC_Auth<Zone>.LoginResult.BadCredentials;
                    client.sendReliable(reply);
                    return;
                }

                //Great! Escalate our client object to a zone
                Zone zone = new Zone(client, server, dbZone);
                client._obj = zone;
                zone._zone.Active = 1; //Set it as active

                server._zones.Add(zone);

                //Called on connection close / timeout
                zone._client.Destruct += delegate (NetworkClient nc)
                {
                    zone.destroy();
                };

                //Success!
                SC_Auth<Zone> success = new SC_Auth<Zone>();

                success.result = SC_Auth<Zone>.LoginResult.Success;
                success.message = dbZone.Notice;

                client.sendReliable(success);

                dbZone.Name = pkt.zoneName;
                dbZone.Description = pkt.zoneDescription;
                dbZone.Ip = pkt.zoneIP;
                dbZone.Port = pkt.zonePort;
                dbZone.Advanced = Convert.ToInt16(pkt.zoneIsAdvanced);
                dbZone.Active = 1;

                db.SaveChanges();
            }

            Log.write("Successful login from {0} ({1})", dbZone.Name, client._ipe);
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


            using (var db = zone._server.getContext())
            {
                Player player = null;
                Account account = db.Accounts.SingleOrDefault(acct => acct.Ticket == pkt.ticketid);

                if (account == null)
                {	//They're trying to trick us, jim!
                    plog.bSuccess = false;
                    plog.loginMessage = "Your session id has expired. Please re-login.";

                    zone._client.send(plog);
                    return;
                }

                //Is there already a player online under this account?
                if (!DBServer.bAllowMulticlienting && zone._server._zones.Any((Func<Zone, bool>)(z => z.hasAccountPlayer(account.AccountId))))
                {
                    plog.bSuccess = false;
                    plog.loginMessage = "Account is currently in use.";

                    zone._client.send(plog);
                    return;
                }

                //Check for IP and UID bans
                Logic_Bans.Ban banned = Logic_Bans.checkBan(pkt, db, account, zone._zone.ZoneId);

                if (banned.type == Logic_Bans.Ban.BanType.GlobalBan)
                {   //We don't respond to globally banned player requests
                    plog.bSuccess = false;
                    plog.loginMessage = "Banned.";

                    Log.write(TLog.Warning, "Failed login: " + zone._zone.Name + " Alias: " + pkt.alias + " Reason: " + banned.type.ToString());
                    zone._client.send(plog);
                    return;
                }

                if (banned.type == Logic_Bans.Ban.BanType.IPBan)
                {   //Their IP has been banned, make something up!
                    plog.bSuccess = false;
                    plog.loginMessage = "You have been temporarily suspended until " + banned.expiration.ToString("f", CultureInfo.CreateSpecificCulture("en-US"));

                    Log.write(TLog.Warning, "Failed login: " + zone._zone.Name + " Alias: " + pkt.alias + " Reason: " + banned.type.ToString());
                    zone._client.send(plog);
                    return;
                }

                if (banned.type == Logic.Logic_Bans.Ban.BanType.ZoneBan)
                {   //They've been blocked from entering the zone, tell them how long they've got left on their ban
                    plog.bSuccess = false;
                    plog.loginMessage = "You have been temporarily suspended from this zone until " + banned.expiration.ToString("f", CultureInfo.CreateSpecificCulture("en-US"));

                    Log.write(TLog.Warning, "Failed login: " + zone._zone.Name + " Alias: " + pkt.alias + " Reason: " + banned.type.ToString());
                    zone._client.send(plog);
                    return;
                }

                if (banned.type == Logic.Logic_Bans.Ban.BanType.AccountBan)
                {   //They've been blocked from entering any zone, tell them when to come back
                    plog.bSuccess = false;
                    plog.loginMessage = "Your account has been temporarily suspended until " + banned.expiration.ToString("f", CultureInfo.CreateSpecificCulture("en-US"));

                    Log.write(TLog.Warning, "Failed login: " + zone._zone.Name + " Alias: " + pkt.alias + " Reason: " + banned.type.ToString());
                    zone._client.send(plog);
                    return;
                }

                //They made it!

                //We have the account associated!
                plog.permission = (PlayerPermission)account.Permission;

                if (account.Permission > (int)Data.PlayerPermission.Level5)
                {
                    plog.permission = Data.PlayerPermission.Level5;
                }
                   
                //
                // Allow symbols as login names for admins.
                //
                var pktAlias = pkt.alias;

                if (plog.permission != Data.PlayerPermission.Level5)
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
                Alias alias = db.Aliases.SingleOrDefault(a => a.Name == pkt.alias);
                Stat stats = null;

                //Is there already a player online under this alias?
                if (alias != null && zone._server._zones.Any((Func<Zone, bool>)(z => z.hasAliasPlayer(alias.AliasId))))
                {
                    plog.bSuccess = false;
                    plog.loginMessage = "Alias is currently in use.";

                    zone._client.send(plog);
                    return;
                }

                if (alias == null && !pkt.bCreateAlias)
                {	//Prompt him to create a new alias if he has room
                    int maxAliases = 30;
                    if (plog.permission == Data.PlayerPermission.Level5 || account.Aliases.Count < maxAliases)
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
                    alias = new Database.Alias();

                    alias.Name = pkt.alias;
                    alias.Creation = DateTime.Now;
                    alias.AccountNavigation = account;
                    alias.IpAddress = pkt.ipaddress;
                    alias.LastAccess = DateTime.Now;
                    alias.TimePlayed = 0;

                    db.Aliases.Add(alias);

                    Log.write(TLog.Normal, "Creating new alias '{0}' on account '{1}'", pkt.alias, account.Name);
                }
                else if (alias != null)
                {	//We can't recreate an existing alias or login to one that isn't ours..
                    if (pkt.bCreateAlias ||
                        alias.AccountNavigation != account)
                    {
                        plog.bSuccess = false;
                        plog.loginMessage = "The specified alias already exists.";

                        zone._client.send(plog);
                        return;
                    }
                }

                //Do we have a player row for this zone?
                player = db.Players
                    .Include(p => p.StatsNavigation)
                    .Include(p => p.AliasNavigation).ThenInclude(p => p.AccountNavigation)
                    .Include(p => p.ZoneNavigation)
                    .SingleOrDefault(plyr => plyr.AliasNavigation == alias && plyr.ZoneNavigation == zone._zone);

                if (player == null)
                {	//We need to create another!
                    player = new Database.Player();

                    player.SquadNavigation = null;
                    player.ZoneId = zone._zone.ZoneId;
                    player.AliasNavigation = alias;

                    player.LastAccess = DateTime.Now;
                    player.Permission = 0;

                    //Create a blank stats row
                    stats = new Stat();

                    stats.ZoneId = zone._zone.ZoneId;
                    player.StatsNavigation = stats;

                    db.Stats.Add(stats);
                    db.Players.Add(player);

                    //It's a first-time login, so no need to load stats
                    plog.bFirstTimeSetup = true;
                }
                else
                {	//Load the player details and stats!
                    if (player.Banner != null)
                    {
                        plog.banner = player.Banner.ToArray();
                    }

                    plog.permission = (PlayerPermission)Math.Max(player.Permission, (int)plog.permission);

                    if (player.Permission > account.Permission)
                        //He's a dev here, set the bool
                        plog.developer = true;

                    // Check for admin
                    foreach (var adminId in Logic_Admins.ServerAdminAccountIds)
                    {
                        if (account.AccountId == adminId && account.Permission >= 5)
                        {
                            plog.admin = true;
                            break;
                        }
                    }

                    // If they are not marked as admin, but DB says otherwise
                    // then treat the admin file as final say.
                    if (!plog.admin && account.Permission >= 5)
                    {
                        account.Permission = 0;
                    }

                    plog.squadID = player.SquadId.GetValueOrDefault();

                    if (plog.squadID != 0)
                    {
                        var squad = db.Squads.Find(plog.squadID);

                        if (squad != null)
                        {
                            plog.squad = squad.Name;
                        }
                    }

                    stats = player.StatsNavigation;

                    plog.stats.zonestat1 = stats.Zonestat1;
                    plog.stats.zonestat2 = stats.Zonestat2;
                    plog.stats.zonestat3 = stats.Zonestat3;
                    plog.stats.zonestat4 = stats.Zonestat4;
                    plog.stats.zonestat5 = stats.Zonestat5;
                    plog.stats.zonestat6 = stats.Zonestat6;
                    plog.stats.zonestat7 = stats.Zonestat7;
                    plog.stats.zonestat8 = stats.Zonestat8;
                    plog.stats.zonestat9 = stats.Zonestat9;
                    plog.stats.zonestat10 = stats.Zonestat10;
                    plog.stats.zonestat11 = stats.Zonestat11;
                    plog.stats.zonestat12 = stats.Zonestat12;

                    plog.stats.kills = stats.Kills;
                    plog.stats.deaths = stats.Deaths;
                    plog.stats.killPoints = stats.KillPoints;
                    plog.stats.deathPoints = stats.DeathPoints;
                    plog.stats.assistPoints = stats.AssistPoints;
                    plog.stats.bonusPoints = stats.BonusPoints;
                    plog.stats.vehicleKills = stats.VehicleKills;
                    plog.stats.vehicleDeaths = stats.VehicleDeaths;
                    plog.stats.playSeconds = stats.PlaySeconds;

                    plog.stats.cash = stats.Cash;
                    plog.stats.inventory = new List<PlayerStats.InventoryStat>();
                    plog.stats.experience = stats.Experience;
                    plog.stats.experienceTotal = stats.ExperienceTotal;
                    plog.stats.skills = new List<PlayerStats.SkillStat>();
                    plog.stealth = alias.Stealth == 1;

                    //Convert the binary inventory/skill data
                    if (player.Inventory != null)
                        DatabaseBinaryUtils.binToInventory(plog.stats.inventory, player.Inventory);
                    if (player.Skills != null)
                        DatabaseBinaryUtils.binToSkills(plog.stats.skills, player.Skills);
                }

                plog.silencedAtUnixMilliseconds = account.SilencedAtMillisecondsUnix;
                plog.silencedDurationMinutes = account.SilencedDuration;

                // Deal with global silencing...
                if (plog.silencedDurationMinutes > 0)
                {
                    var silenceDateTime = DateTimeOffset
                    .FromUnixTimeMilliseconds(account.SilencedAtMillisecondsUnix)
                    .LocalDateTime
                    .AddMinutes(account.SilencedDuration);

                    // Do we have an expired silence to clear?
                    if (silenceDateTime < DateTime.Now)
                    {
                        plog.silencedAtUnixMilliseconds = 0;
                        plog.silencedDurationMinutes = 0;

                        account.SilencedAtMillisecondsUnix = 0;
                        account.SilencedDuration = 0;
                    }
                }

                plog.bannermode = account.BannerMode;
                plog.alias = pkt.alias;

                //Try and submit any new rows before we try and use them
                try
                {
                    db.SaveChanges();
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
                if (zone.newPlayer(pkt.player.id, alias.Name, player))
                {
                    plog.bSuccess = true;
                    Log.write("Player '{0}' logged into zone '{1}'", alias.Name, zone._zone.Name);

                    //Modify his alias IP address and access times
                    alias.IpAddress = pkt.ipaddress.Trim();
                    alias.LastAccess = DateTime.Now;

                    //Change it
                    db.SaveChanges();
                }
                else
                {
                    plog.bSuccess = false;
                    plog.loginMessage = "Unknown login failure.";
                    Log.write("Failed adding player '{0}' from '{1}'", alias.Name, zone._zone.Name);
                }

                // Send off ther player details.
                zone._client.sendReliable(plog);

                if (plog.silencedDurationMinutes > 0)
                {
                    var silencePkt = new SC_Silence<Zone>
                    {
                        alias = plog.alias,
                        silencedAtUnixMs = account.SilencedAtMillisecondsUnix,
                        minutes = account.SilencedDuration
                    };

                    zone._client.sendReliable(silencePkt);
                }
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

            Log.write("Player '{0}' left zone '{1}'", pkt.alias, zone._zone.Name);

            // Update their playtime
            using (SqlServerDbContext db = zone._server.getContext())
            {
                Alias alias = db.Aliases.SingleOrDefault(a => a.Name == pkt.alias);

                //If person was loaded correctly, save their info
                if (alias != null)
                {
                    TimeSpan ts = DateTime.Now - alias.LastAccess;
                    Int64 minutes = ts.Minutes;
                    alias.TimePlayed += minutes;

                    db.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Handles a zone update packet and updates the database
        /// </summary>
        static public void Handle_CS_ZoneUpdate(CS_ZoneUpdate<Zone> pkt, Zone zone)
        {
            using (SqlServerDbContext db = zone._server.getContext())
            {
                Database.Zone zEntry = db.Zones.SingleOrDefault(z => z.ZoneId == zone._zone.ZoneId);
                if (zEntry != null)
                {
                    //Update the zone for our directory server
                    zEntry.Name = pkt.zoneName;
                    zEntry.Description = pkt.zoneDescription;
                    zEntry.Ip = pkt.zoneIP;
                    zEntry.Port = pkt.zonePort;
                    zEntry.Advanced = Convert.ToInt16(pkt.zoneIsAdvanced);
                    zEntry.Active = pkt.zoneActive;

                    db.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Handles a graceful zone disconnect
        /// </summary>
        static public void Handle_Disconnect(Disconnect<Zone> pkt, Zone zone)
        {
            Log.write("{0} disconnected gracefully", zone._zone.Name);

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