using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Protocol;
using InfServer.Data;
using InfServer;
using System.Globalization;
using InfServer.Database;
using InfServer.Database;

namespace InfServer.Logic
{	// Logic_Player Class
    /// Handles various player related functionality
    ///////////////////////////////////////////////////////
    class Logic_Player
    {
        /// <summary>
        /// Handles a player update request
        /// </summary>
        static public void Handle_CS_PlayerUpdate(CS_PlayerUpdate<Zone> pkt, Zone zone)
        {	//Attempt to find the player in question
            Zone.Player player = zone.getPlayer(pkt.player.id);
            if (player == null)
            {	//Make a note
                Log.write(TLog.Warning, "Ignoring player update for #{0}, not present in zone mirror.", pkt.player.id);
                return;
            }

            using (InfServer.Database.InfantryDataContext db = zone._server.getContext())
            {	//Get the associated player entry
                InfServer.Database.player dbplayer = db.players.SingleOrDefault(plyr => plyr.id == player.dbid);

                if (dbplayer == null)
                {	//Make a note
                    Log.write(TLog.Warning, "Ignoring player update for {0}, not present in database.", player.alias);
                    return;
                }

                UpdateDailyWeeklyMonthlyYearlyStats(db, pkt, zone, dbplayer);

                // Write the new stats to the stats table.
                stats stats = dbplayer.stats1;

                stats.zonestat1 = pkt.stats.zonestat1;
                stats.zonestat2 = pkt.stats.zonestat2;
                stats.zonestat3 = pkt.stats.zonestat3;
                stats.zonestat4 = pkt.stats.zonestat4;
                stats.zonestat5 = pkt.stats.zonestat5;
                stats.zonestat6 = pkt.stats.zonestat6;
                stats.zonestat7 = pkt.stats.zonestat7;
                stats.zonestat8 = pkt.stats.zonestat8;
                stats.zonestat9 = pkt.stats.zonestat9;
                stats.zonestat10 = pkt.stats.zonestat10;
                stats.zonestat11 = pkt.stats.zonestat11;
                stats.zonestat12 = pkt.stats.zonestat12;

                stats.kills = pkt.stats.kills;
                stats.deaths = pkt.stats.deaths;
                stats.killPoints = pkt.stats.killPoints;
                stats.deathPoints = pkt.stats.deathPoints;
                stats.assistPoints = pkt.stats.assistPoints;
                stats.bonusPoints = pkt.stats.bonusPoints;
                stats.vehicleKills = pkt.stats.vehicleKills;
                stats.vehicleDeaths = pkt.stats.vehicleDeaths;
                stats.playSeconds = pkt.stats.playSeconds;

                stats.cash = pkt.stats.cash;
                stats.experience = pkt.stats.experience;
                stats.experienceTotal = pkt.stats.experienceTotal;

                //Convert inventory and skills
                dbplayer.inventory = DBHelpers.inventoryToBin(pkt.stats.inventory);
                dbplayer.skills = DBHelpers.skillsToBin(pkt.stats.skills);

                //Update all changes
                db.SubmitChanges();
            }
        }

        static private void UpdateDailyWeeklyMonthlyYearlyStats(InfantryDataContext db, CS_PlayerUpdate<Zone> pkt, Zone zone, InfServer.Database.player player)
        {
            // 1. Get the deltas from the current stats table, and then add it to each of the long-term stat categories.

            stats stats = player.stats1;

            var zs1 = pkt.stats.zonestat1 - stats.zonestat1;
            var zs2 = pkt.stats.zonestat2 - stats.zonestat2;
            var zs3 = pkt.stats.zonestat3 - stats.zonestat3;
            var zs4 = pkt.stats.zonestat4 - stats.zonestat4;
            var zs5 = pkt.stats.zonestat5 - stats.zonestat5;
            var zs6 = pkt.stats.zonestat6 - stats.zonestat6;
            var zs7 = pkt.stats.zonestat7 - stats.zonestat7;
            var zs8 = pkt.stats.zonestat8 - stats.zonestat8;
            var zs9 = pkt.stats.zonestat9 - stats.zonestat9;
            var zs10 = pkt.stats.zonestat10 - stats.zonestat10;
            var zs11 = pkt.stats.zonestat11 - stats.zonestat11;
            var zs12 = pkt.stats.zonestat12 - stats.zonestat12;

            var kills = pkt.stats.kills - stats.kills;
            var deaths = pkt.stats.deaths - stats.deaths;
            var killPoints = pkt.stats.killPoints - stats.killPoints;
            var deathPoints = pkt.stats.deathPoints - stats.deathPoints;
            var assistPoints = pkt.stats.assistPoints - stats.assistPoints;
            var bonusPoints = pkt.stats.bonusPoints - stats.bonusPoints;
            var vehicleKills = pkt.stats.vehicleKills - stats.vehicleKills;
            var vehicleDeaths = pkt.stats.vehicleDeaths - stats.vehicleDeaths;
            var playSeconds = pkt.stats.playSeconds - stats.playSeconds;

            // 2. For each type of stat, we need to query and see if it exists. Be mindful of the logic used for date filtering.

            var day = DateTime.Today;
            var week = DateTime.Today;
            var month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var year = new DateTime(DateTime.Today.Year, 1, 1);

            if (week.DayOfWeek > 0)
            {
                week = week.AddDays(-(int)week.DayOfWeek);
            }

            // Update Daily

            var daily = db.statsDailies.FirstOrDefault(s => s.date == day && s.player == player.id);

            if (daily == null)
            {
                daily = new statsDaily();
                daily.zone = player.zone;
                daily.player = player.id;
                daily.date = day;

                db.statsDailies.InsertOnSubmit(daily);
            }

            daily.kills += kills;
            daily.deaths += deaths;
            daily.killPoints += killPoints;
            daily.deathPoints += deathPoints;
            daily.assistPoints += assistPoints;
            daily.bonusPoints += bonusPoints;
            daily.vehicleKills += vehicleKills;
            daily.vehicleDeaths += vehicleDeaths;
            daily.playSeconds += playSeconds;

            daily.zonestat1 += zs1;
            daily.zonestat2 += zs2;
            daily.zonestat3 += zs3;
            daily.zonestat4 += zs4;
            daily.zonestat5 += zs5;
            daily.zonestat6 += zs6;
            daily.zonestat7 += zs7;
            daily.zonestat8 += zs8;
            daily.zonestat9 += zs9;
            daily.zonestat10 += zs10;
            daily.zonestat11 += zs11;
            daily.zonestat12 += zs12;

            // Update Weekly

            var weekly = db.statsWeeklies.FirstOrDefault(s => s.date == week && s.player == player.id);

            if (weekly == null)
            {
                weekly = new statsWeekly();
                weekly.zone = player.zone;
                weekly.player = player.id;
                weekly.date = week;

                db.statsWeeklies.InsertOnSubmit(weekly);
            }

            weekly.kills += kills;
            weekly.deaths += deaths;
            weekly.killPoints += killPoints;
            weekly.deathPoints += deathPoints;
            weekly.assistPoints += assistPoints;
            weekly.bonusPoints += bonusPoints;
            weekly.vehicleKills += vehicleKills;
            weekly.vehicleDeaths += vehicleDeaths;
            weekly.playSeconds += playSeconds;

            weekly.zonestat1 += zs1;
            weekly.zonestat2 += zs2;
            weekly.zonestat3 += zs3;
            weekly.zonestat4 += zs4;
            weekly.zonestat5 += zs5;
            weekly.zonestat6 += zs6;
            weekly.zonestat7 += zs7;
            weekly.zonestat8 += zs8;
            weekly.zonestat9 += zs9;
            weekly.zonestat10 += zs10;
            weekly.zonestat11 += zs11;
            weekly.zonestat12 += zs12;

            // Update Monthly

            var monthly = db.statsMonthlies.FirstOrDefault(s => s.date == month && s.player == player.id);

            if (monthly == null)
            {
                monthly = new statsMonthly();
                monthly.zone = player.zone;
                monthly.player = player.id;
                monthly.date = month;

                db.statsMonthlies.InsertOnSubmit(monthly);
            }

            monthly.kills += kills;
            monthly.deaths += deaths;
            monthly.killPoints += killPoints;
            monthly.deathPoints += deathPoints;
            monthly.assistPoints += assistPoints;
            monthly.bonusPoints += bonusPoints;
            monthly.vehicleKills += vehicleKills;
            monthly.vehicleDeaths += vehicleDeaths;
            monthly.playSeconds += playSeconds;

            monthly.zonestat1 += zs1;
            monthly.zonestat2 += zs2;
            monthly.zonestat3 += zs3;
            monthly.zonestat4 += zs4;
            monthly.zonestat5 += zs5;
            monthly.zonestat6 += zs6;
            monthly.zonestat7 += zs7;
            monthly.zonestat8 += zs8;
            monthly.zonestat9 += zs9;
            monthly.zonestat10 += zs10;
            monthly.zonestat11 += zs11;
            monthly.zonestat12 += zs12;

            // Update Yearly

            var yearly = db.statsYearlies.FirstOrDefault(s => s.date == year && s.player == player.id);

            if (yearly == null)
            {
                yearly = new statsYearly();
                yearly.zone = player.zone;
                yearly.player = player.id;
                yearly.date = year;

                db.statsYearlies.InsertOnSubmit(yearly);
            }

            yearly.kills += kills;
            yearly.deaths += deaths;
            yearly.killPoints += killPoints;
            yearly.deathPoints += deathPoints;
            yearly.assistPoints += assistPoints;
            yearly.bonusPoints += bonusPoints;
            yearly.vehicleKills += vehicleKills;
            yearly.vehicleDeaths += vehicleDeaths;
            yearly.playSeconds += playSeconds;

            yearly.zonestat1 += zs1;
            yearly.zonestat2 += zs2;
            yearly.zonestat3 += zs3;
            yearly.zonestat4 += zs4;
            yearly.zonestat5 += zs5;
            yearly.zonestat6 += zs6;
            yearly.zonestat7 += zs7;
            yearly.zonestat8 += zs8;
            yearly.zonestat9 += zs9;
            yearly.zonestat10 += zs10;
            yearly.zonestat11 += zs11;
            yearly.zonestat12 += zs12;
        }


        /// <summary>
        /// Handles a player banner update
        /// </summary>
        static public void Handle_CS_PlayerBanner(CS_PlayerBanner<Zone> pkt, Zone zone)
        {	//Attempt to find the player in question
            Zone.Player player = zone.getPlayer(pkt.player.id);
            if (player == null)
            {	//Make a note
                Log.write(TLog.Warning, "Ignoring player banner update for #{0}, not present in zone mirror.", pkt.player.id);
                return;
            }

            using (InfServer.Database.InfantryDataContext db = zone._server.getContext())
            {	//Get the associated player entry
                InfServer.Database.player dbplayer = db.players.SingleOrDefault(plyr => plyr.id == player.dbid);
                if (dbplayer == null)
                {	//Make a note
                    Log.write(TLog.Warning, "Ignoring player banner update for {0}, not present in database.", player.alias);
                    return;
                }

                dbplayer.banner = pkt.banner;

                //Update all changes
                db.SubmitChanges();
            }
        }

        /// <summary>
        /// Handles a chat whisper
        /// </summary>
        static public void Handle_CS_Whisper(CS_Whisper<Zone> pkt, Zone zone)
        {
            foreach (Zone z in zone._server._zones)
            {
                if (z.hasAliasPlayer(pkt.recipient))
                {
                    SC_Whisper<Zone> reply = new SC_Whisper<Zone>();
                    reply.bong = pkt.bong;
                    reply.message = pkt.message;
                    reply.recipient = pkt.recipient;
                    reply.from = pkt.from;
                    z._client.send(reply);
                }
            }
        }

        /// <summary>
        /// Handles an arena update from a player
        /// </summary>
        static public void Handle_CS_ArenaUpdate(CS_ArenaUpdate<Zone> pkt, Zone zone)
        {
            //Attempt to find the player in question
            Zone.Player player = zone.getPlayer(pkt.player.id);
            if (player == null)
            {	//Make a note
                Log.write(TLog.Warning, "Ignoring arena update for #{0}, not present in zone mirror.", pkt.player.id);
                return;
            }

            player.arena = pkt.arena;
        }

        /// <summary>
        /// Registers all handlers
        /// </summary>
        [RegistryFunc]
        static public void Register()
        {
            CS_PlayerUpdate<Zone>.Handlers += Handle_CS_PlayerUpdate;
            CS_PlayerBanner<Zone>.Handlers += Handle_CS_PlayerBanner;
            CS_Whisper<Zone>.Handlers += Handle_CS_Whisper;
            CS_ArenaUpdate<Zone>.Handlers += Handle_CS_ArenaUpdate;
        }
    }
}