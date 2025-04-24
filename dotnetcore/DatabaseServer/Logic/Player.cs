using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Protocol;
using InfServer.Data;
using InfServer;
using System.Globalization;
using Database;
using Database;
using Microsoft.EntityFrameworkCore;

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
        {
            var player = zone.getPlayer(pkt.player.id);

            if (player == null)
            {
                Log.write(TLog.Warning, "Ignoring player update for #{0}, not present in zone mirror.", pkt.player.id);
                return;
            }

            using var ctx = zone._server.getContext();

            //
            // Query the current stats. Yes, we need to make a query to get the stats for purposes of substraction.
            // While we could keep the most recent stats in memory, we'll omit that for now and incur this call.
            //

            var previousStat = ctx.Stats
                .AsNoTracking()
                .Where(s => s.Id == player.statsid)
                .First();

            // Now update.

            var statUpdateRowCount = ctx.Stats
                .Where(s => s.Id == player.statsid)
                .ExecuteUpdate(setters => setters
                    .SetProperty(s => s.Zonestat1, pkt.stats.zonestat1)
                    .SetProperty(s => s.Zonestat2, pkt.stats.zonestat2)
                    .SetProperty(s => s.Zonestat3, pkt.stats.zonestat3)
                    .SetProperty(s => s.Zonestat4, pkt.stats.zonestat4)
                    .SetProperty(s => s.Zonestat5, pkt.stats.zonestat5)
                    .SetProperty(s => s.Zonestat6, pkt.stats.zonestat6)
                    .SetProperty(s => s.Zonestat7, pkt.stats.zonestat7)
                    .SetProperty(s => s.Zonestat8, pkt.stats.zonestat8)
                    .SetProperty(s => s.Zonestat9, pkt.stats.zonestat9)
                    .SetProperty(s => s.Zonestat10, pkt.stats.zonestat10)
                    .SetProperty(s => s.Zonestat11, pkt.stats.zonestat11)
                    .SetProperty(s => s.Zonestat12, pkt.stats.zonestat12)

                    .SetProperty(s => s.Kills, pkt.stats.kills)
                    .SetProperty(s => s.Deaths, pkt.stats.deaths)
                    .SetProperty(s => s.KillPoints, pkt.stats.killPoints)
                    .SetProperty(s => s.DeathPoints, pkt.stats.deathPoints)
                    .SetProperty(s => s.AssistPoints, pkt.stats.assistPoints)
                    .SetProperty(s => s.BonusPoints, pkt.stats.bonusPoints)
                    .SetProperty(s => s.VehicleKills, pkt.stats.vehicleKills)
                    .SetProperty(s => s.VehicleDeaths, pkt.stats.vehicleDeaths)
                    .SetProperty(s => s.PlaySeconds, pkt.stats.playSeconds)
                    .SetProperty(s => s.Cash, pkt.stats.cash)
                    .SetProperty(s => s.Experience, pkt.stats.experience)
                    .SetProperty(s => s.ExperienceTotal, pkt.stats.experienceTotal));

            //
            // Sanity check, make sure that we actually have a record. Maybe not needed
            // but the previous code had it so we'll keep it for now.
            //

            if (statUpdateRowCount == 0)
            {
                Log.write(TLog.Warning, "Ignoring player update for {0}, not present in database.", player.alias);
                return;
            }

            ctx.Players
                .Where(p => p.Id == player.dbid)
                .ExecuteUpdate(setters => setters
                    .SetProperty(p => p.Inventory, DatabaseBinaryUtils.inventoryToBin(pkt.stats.inventory))
                    .SetProperty(p => p.Skills, DatabaseBinaryUtils.skillsToBin(pkt.stats.skills)));


            UpdateDailyWeeklyMonthlyYearlyStats(pkt, zone, player, ctx, previousStat);
        }

        static private void UpdateDailyWeeklyMonthlyYearlyStats(CS_PlayerUpdate<Zone> pkt, Zone zone, Zone.Player player, DataContext ctx, Database.Stat previousStat)
        {
            //
            // Subtract to get the delta from our previous stats,
            // and then proceed to add this delta to the accruals.
            //

            var zs1 = pkt.stats.zonestat1 - previousStat.Zonestat1;
            var zs2 = pkt.stats.zonestat2 - previousStat.Zonestat2;
            var zs3 = pkt.stats.zonestat3 - previousStat.Zonestat3;
            var zs4 = pkt.stats.zonestat4 - previousStat.Zonestat4;
            var zs5 = pkt.stats.zonestat5 - previousStat.Zonestat5;
            var zs6 = pkt.stats.zonestat6 - previousStat.Zonestat6;
            var zs7 = pkt.stats.zonestat7 - previousStat.Zonestat7;
            var zs8 = pkt.stats.zonestat8 - previousStat.Zonestat8;
            var zs9 = pkt.stats.zonestat9 - previousStat.Zonestat9;
            var zs10 = pkt.stats.zonestat10 - previousStat.Zonestat10;
            var zs11 = pkt.stats.zonestat11 - previousStat.Zonestat11;
            var zs12 = pkt.stats.zonestat12 - previousStat.Zonestat12;

            var kills = pkt.stats.kills - previousStat.Kills;
            var deaths = pkt.stats.deaths - previousStat.Deaths;
            var killPoints = pkt.stats.killPoints - previousStat.KillPoints;
            var deathPoints = pkt.stats.deathPoints - previousStat.DeathPoints;
            var assistPoints = pkt.stats.assistPoints - previousStat.AssistPoints;
            var bonusPoints = pkt.stats.bonusPoints - previousStat.BonusPoints;
            var vehicleKills = pkt.stats.vehicleKills - previousStat.VehicleKills;
            var vehicleDeaths = pkt.stats.vehicleDeaths - previousStat.VehicleDeaths;
            var playSeconds = pkt.stats.playSeconds - previousStat.PlaySeconds;

            //
            // Create a date object for each type of stat.
            //

            var day = DateTime.Today;
            var week = DateTime.Today;
            var month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var year = new DateTime(DateTime.Today.Year, 1, 1);

            if (week.DayOfWeek > 0)
            {
                week = week.AddDays(-(int)week.DayOfWeek);
            }

            // Update or Insert Daily

            var dailyRowsUpdated = ctx.StatsDailies
                .Where(s => s.Date == day && s.Player == player.dbid && s.Zone == zone._zone.Id)
                .ExecuteUpdate(setters => setters
                    .SetProperty(s => s.Zonestat1, s => s.Zonestat1 + pkt.stats.zonestat1)
                    .SetProperty(s => s.Zonestat2, s => s.Zonestat2 + pkt.stats.zonestat2)
                    .SetProperty(s => s.Zonestat3, s => s.Zonestat3 + pkt.stats.zonestat3)
                    .SetProperty(s => s.Zonestat4, s => s.Zonestat4 + pkt.stats.zonestat4)
                    .SetProperty(s => s.Zonestat5, s => s.Zonestat5 + pkt.stats.zonestat5)
                    .SetProperty(s => s.Zonestat6, s => s.Zonestat6 + pkt.stats.zonestat6)
                    .SetProperty(s => s.Zonestat7, s => s.Zonestat7 + pkt.stats.zonestat7)
                    .SetProperty(s => s.Zonestat8, s => s.Zonestat8 + pkt.stats.zonestat8)
                    .SetProperty(s => s.Zonestat9, s => s.Zonestat9 + pkt.stats.zonestat9)
                    .SetProperty(s => s.Zonestat10, s => s.Zonestat10 + pkt.stats.zonestat10)
                    .SetProperty(s => s.Zonestat11, s => s.Zonestat11 + pkt.stats.zonestat11)
                    .SetProperty(s => s.Zonestat12, s => s.Zonestat12 + pkt.stats.zonestat12)

                    .SetProperty(s => s.Kills, s => s.Kills + pkt.stats.kills)
                    .SetProperty(s => s.Deaths, s => s.Deaths + pkt.stats.deaths)
                    .SetProperty(s => s.KillPoints, s => s.KillPoints + pkt.stats.killPoints)
                    .SetProperty(s => s.DeathPoints, s => s.DeathPoints + pkt.stats.deathPoints)
                    .SetProperty(s => s.AssistPoints, s => s.AssistPoints + pkt.stats.assistPoints)
                    .SetProperty(s => s.BonusPoints, s => s.BonusPoints + pkt.stats.bonusPoints)
                    .SetProperty(s => s.VehicleKills, s => s.VehicleKills + pkt.stats.vehicleKills)
                    .SetProperty(s => s.VehicleDeaths, s => s.VehicleDeaths + pkt.stats.vehicleDeaths)
                    .SetProperty(s => s.PlaySeconds, s => s.PlaySeconds + pkt.stats.playSeconds)
                    .SetProperty(s => s.Experience, s => s.Experience + pkt.stats.experience)
                    .SetProperty(s => s.ExperienceTotal, s => s.ExperienceTotal + pkt.stats.experienceTotal));

            if (dailyRowsUpdated == 0)
            {
                var stat = new StatsDaily();

                stat.Zone = zone._zone.Id;
                stat.Date = day;
                stat.Player = player.dbid;

                stat.Kills += kills;
                stat.Deaths += deaths;
                stat.KillPoints += killPoints;
                stat.DeathPoints += deathPoints;
                stat.AssistPoints += assistPoints;
                stat.BonusPoints += bonusPoints;
                stat.VehicleKills += vehicleKills;
                stat.VehicleDeaths += vehicleDeaths;
                stat.PlaySeconds += playSeconds;

                stat.Zonestat1 += zs1;
                stat.Zonestat2 += zs2;
                stat.Zonestat3 += zs3;
                stat.Zonestat4 += zs4;
                stat.Zonestat5 += zs5;
                stat.Zonestat6 += zs6;
                stat.Zonestat7 += zs7;
                stat.Zonestat8 += zs8;
                stat.Zonestat9 += zs9;
                stat.Zonestat10 += zs10;
                stat.Zonestat11 += zs11;
                stat.Zonestat12 += zs12;

                ctx.StatsDailies.Add(stat);
                ctx.SaveChanges();
            }

            // Update or Insert Weekly

            var weeklyRowsUpdated = ctx.StatsWeeklies
                .Where(s => s.Date == week && s.Player == player.dbid && s.Zone == zone._zone.Id)
                .ExecuteUpdate(setters => setters
                    .SetProperty(s => s.Zonestat1, s => s.Zonestat1 + pkt.stats.zonestat1)
                    .SetProperty(s => s.Zonestat2, s => s.Zonestat2 + pkt.stats.zonestat2)
                    .SetProperty(s => s.Zonestat3, s => s.Zonestat3 + pkt.stats.zonestat3)
                    .SetProperty(s => s.Zonestat4, s => s.Zonestat4 + pkt.stats.zonestat4)
                    .SetProperty(s => s.Zonestat5, s => s.Zonestat5 + pkt.stats.zonestat5)
                    .SetProperty(s => s.Zonestat6, s => s.Zonestat6 + pkt.stats.zonestat6)
                    .SetProperty(s => s.Zonestat7, s => s.Zonestat7 + pkt.stats.zonestat7)
                    .SetProperty(s => s.Zonestat8, s => s.Zonestat8 + pkt.stats.zonestat8)
                    .SetProperty(s => s.Zonestat9, s => s.Zonestat9 + pkt.stats.zonestat9)
                    .SetProperty(s => s.Zonestat10, s => s.Zonestat10 + pkt.stats.zonestat10)
                    .SetProperty(s => s.Zonestat11, s => s.Zonestat11 + pkt.stats.zonestat11)
                    .SetProperty(s => s.Zonestat12, s => s.Zonestat12 + pkt.stats.zonestat12)

                    .SetProperty(s => s.Kills, s => s.Kills + pkt.stats.kills)
                    .SetProperty(s => s.Deaths, s => s.Deaths + pkt.stats.deaths)
                    .SetProperty(s => s.KillPoints, s => s.KillPoints + pkt.stats.killPoints)
                    .SetProperty(s => s.DeathPoints, s => s.DeathPoints + pkt.stats.deathPoints)
                    .SetProperty(s => s.AssistPoints, s => s.AssistPoints + pkt.stats.assistPoints)
                    .SetProperty(s => s.BonusPoints, s => s.BonusPoints + pkt.stats.bonusPoints)
                    .SetProperty(s => s.VehicleKills, s => s.VehicleKills + pkt.stats.vehicleKills)
                    .SetProperty(s => s.VehicleDeaths, s => s.VehicleDeaths + pkt.stats.vehicleDeaths)
                    .SetProperty(s => s.PlaySeconds, s => s.PlaySeconds + pkt.stats.playSeconds)
                    .SetProperty(s => s.Experience, s => s.Experience + pkt.stats.experience)
                    .SetProperty(s => s.ExperienceTotal, s => s.ExperienceTotal + pkt.stats.experienceTotal));

            if (weeklyRowsUpdated == 0)
            {
                var stat = new StatsWeekly();

                stat.Zone = zone._zone.Id;
                stat.Date = week;
                stat.Player = player.dbid;

                stat.Kills += kills;
                stat.Deaths += deaths;
                stat.KillPoints += killPoints;
                stat.DeathPoints += deathPoints;
                stat.AssistPoints += assistPoints;
                stat.BonusPoints += bonusPoints;
                stat.VehicleKills += vehicleKills;
                stat.VehicleDeaths += vehicleDeaths;
                stat.PlaySeconds += playSeconds;

                stat.Zonestat1 += zs1;
                stat.Zonestat2 += zs2;
                stat.Zonestat3 += zs3;
                stat.Zonestat4 += zs4;
                stat.Zonestat5 += zs5;
                stat.Zonestat6 += zs6;
                stat.Zonestat7 += zs7;
                stat.Zonestat8 += zs8;
                stat.Zonestat9 += zs9;
                stat.Zonestat10 += zs10;
                stat.Zonestat11 += zs11;
                stat.Zonestat12 += zs12;

                ctx.StatsWeeklies.Add(stat);
                ctx.SaveChanges();
            }

            // Update or Insert Monthly

            var monthlyRowsUpdated = ctx.StatsMonthlies
                .Where(s => s.Date == month && s.Player == player.dbid && s.Zone == zone._zone.Id)
                .ExecuteUpdate(setters => setters
                    .SetProperty(s => s.Zonestat1, s => s.Zonestat1 + pkt.stats.zonestat1)
                    .SetProperty(s => s.Zonestat2, s => s.Zonestat2 + pkt.stats.zonestat2)
                    .SetProperty(s => s.Zonestat3, s => s.Zonestat3 + pkt.stats.zonestat3)
                    .SetProperty(s => s.Zonestat4, s => s.Zonestat4 + pkt.stats.zonestat4)
                    .SetProperty(s => s.Zonestat5, s => s.Zonestat5 + pkt.stats.zonestat5)
                    .SetProperty(s => s.Zonestat6, s => s.Zonestat6 + pkt.stats.zonestat6)
                    .SetProperty(s => s.Zonestat7, s => s.Zonestat7 + pkt.stats.zonestat7)
                    .SetProperty(s => s.Zonestat8, s => s.Zonestat8 + pkt.stats.zonestat8)
                    .SetProperty(s => s.Zonestat9, s => s.Zonestat9 + pkt.stats.zonestat9)
                    .SetProperty(s => s.Zonestat10, s => s.Zonestat10 + pkt.stats.zonestat10)
                    .SetProperty(s => s.Zonestat11, s => s.Zonestat11 + pkt.stats.zonestat11)
                    .SetProperty(s => s.Zonestat12, s => s.Zonestat12 + pkt.stats.zonestat12)

                    .SetProperty(s => s.Kills, s => s.Kills + pkt.stats.kills)
                    .SetProperty(s => s.Deaths, s => s.Deaths + pkt.stats.deaths)
                    .SetProperty(s => s.KillPoints, s => s.KillPoints + pkt.stats.killPoints)
                    .SetProperty(s => s.DeathPoints, s => s.DeathPoints + pkt.stats.deathPoints)
                    .SetProperty(s => s.AssistPoints, s => s.AssistPoints + pkt.stats.assistPoints)
                    .SetProperty(s => s.BonusPoints, s => s.BonusPoints + pkt.stats.bonusPoints)
                    .SetProperty(s => s.VehicleKills, s => s.VehicleKills + pkt.stats.vehicleKills)
                    .SetProperty(s => s.VehicleDeaths, s => s.VehicleDeaths + pkt.stats.vehicleDeaths)
                    .SetProperty(s => s.PlaySeconds, s => s.PlaySeconds + pkt.stats.playSeconds)
                    .SetProperty(s => s.Experience, s => s.Experience + pkt.stats.experience)
                    .SetProperty(s => s.ExperienceTotal, s => s.ExperienceTotal + pkt.stats.experienceTotal));

            if (monthlyRowsUpdated == 0)
            {
                var stat = new StatsMonthly();

                stat.Zone = zone._zone.Id;
                stat.Date = month;
                stat.Player = player.dbid;

                stat.Kills += kills;
                stat.Deaths += deaths;
                stat.KillPoints += killPoints;
                stat.DeathPoints += deathPoints;
                stat.AssistPoints += assistPoints;
                stat.BonusPoints += bonusPoints;
                stat.VehicleKills += vehicleKills;
                stat.VehicleDeaths += vehicleDeaths;
                stat.PlaySeconds += playSeconds;

                stat.Zonestat1 += zs1;
                stat.Zonestat2 += zs2;
                stat.Zonestat3 += zs3;
                stat.Zonestat4 += zs4;
                stat.Zonestat5 += zs5;
                stat.Zonestat6 += zs6;
                stat.Zonestat7 += zs7;
                stat.Zonestat8 += zs8;
                stat.Zonestat9 += zs9;
                stat.Zonestat10 += zs10;
                stat.Zonestat11 += zs11;
                stat.Zonestat12 += zs12;

                ctx.StatsMonthlies.Add(stat);
                ctx.SaveChanges();
            }

            // Update or Insert Monthly

            var yearlyRowsUpdated = ctx.StatsYearlies
                .Where(s => s.Date == year && s.Player == player.dbid && s.Zone == zone._zone.Id)
                .ExecuteUpdate(setters => setters
                    .SetProperty(s => s.Zonestat1, s => s.Zonestat1 + pkt.stats.zonestat1)
                    .SetProperty(s => s.Zonestat2, s => s.Zonestat2 + pkt.stats.zonestat2)
                    .SetProperty(s => s.Zonestat3, s => s.Zonestat3 + pkt.stats.zonestat3)
                    .SetProperty(s => s.Zonestat4, s => s.Zonestat4 + pkt.stats.zonestat4)
                    .SetProperty(s => s.Zonestat5, s => s.Zonestat5 + pkt.stats.zonestat5)
                    .SetProperty(s => s.Zonestat6, s => s.Zonestat6 + pkt.stats.zonestat6)
                    .SetProperty(s => s.Zonestat7, s => s.Zonestat7 + pkt.stats.zonestat7)
                    .SetProperty(s => s.Zonestat8, s => s.Zonestat8 + pkt.stats.zonestat8)
                    .SetProperty(s => s.Zonestat9, s => s.Zonestat9 + pkt.stats.zonestat9)
                    .SetProperty(s => s.Zonestat10, s => s.Zonestat10 + pkt.stats.zonestat10)
                    .SetProperty(s => s.Zonestat11, s => s.Zonestat11 + pkt.stats.zonestat11)
                    .SetProperty(s => s.Zonestat12, s => s.Zonestat12 + pkt.stats.zonestat12)

                    .SetProperty(s => s.Kills, s => s.Kills + pkt.stats.kills)
                    .SetProperty(s => s.Deaths, s => s.Deaths + pkt.stats.deaths)
                    .SetProperty(s => s.KillPoints, s => s.KillPoints + pkt.stats.killPoints)
                    .SetProperty(s => s.DeathPoints, s => s.DeathPoints + pkt.stats.deathPoints)
                    .SetProperty(s => s.AssistPoints, s => s.AssistPoints + pkt.stats.assistPoints)
                    .SetProperty(s => s.BonusPoints, s => s.BonusPoints + pkt.stats.bonusPoints)
                    .SetProperty(s => s.VehicleKills, s => s.VehicleKills + pkt.stats.vehicleKills)
                    .SetProperty(s => s.VehicleDeaths, s => s.VehicleDeaths + pkt.stats.vehicleDeaths)
                    .SetProperty(s => s.PlaySeconds, s => s.PlaySeconds + pkt.stats.playSeconds)
                    .SetProperty(s => s.Experience, s => s.Experience + pkt.stats.experience)
                    .SetProperty(s => s.ExperienceTotal, s => s.ExperienceTotal + pkt.stats.experienceTotal));

            if (yearlyRowsUpdated == 0)
            {
                var stat = new StatsYearly();

                stat.Zone = zone._zone.Id;
                stat.Date = year;
                stat.Player = player.dbid;

                stat.Kills += kills;
                stat.Deaths += deaths;
                stat.KillPoints += killPoints;
                stat.DeathPoints += deathPoints;
                stat.AssistPoints += assistPoints;
                stat.BonusPoints += bonusPoints;
                stat.VehicleKills += vehicleKills;
                stat.VehicleDeaths += vehicleDeaths;
                stat.PlaySeconds += playSeconds;

                stat.Zonestat1 += zs1;
                stat.Zonestat2 += zs2;
                stat.Zonestat3 += zs3;
                stat.Zonestat4 += zs4;
                stat.Zonestat5 += zs5;
                stat.Zonestat6 += zs6;
                stat.Zonestat7 += zs7;
                stat.Zonestat8 += zs8;
                stat.Zonestat9 += zs9;
                stat.Zonestat10 += zs10;
                stat.Zonestat11 += zs11;
                stat.Zonestat12 += zs12;

                ctx.StatsYearlies.Add(stat);
                ctx.SaveChanges();
            }
        }


        /// <summary>
        /// Handles a player banner update
        /// </summary>
        static public void Handle_CS_PlayerBanner(CS_PlayerBanner<Zone> pkt, Zone zone)
        {
            var player = zone.getPlayer(pkt.player.id);

            if (player == null)
            {
                Log.write(TLog.Warning, $"Ignoring player banner update for #{pkt.player.id}, not present in zone mirror.");
                return;
            }

            using (var ctx = zone._server.getContext())
            {
                var results = ctx.Players
                    .Where(p => p.Id == player.dbid)
                    .ExecuteUpdate(t => t.SetProperty(p => p.Banner, pkt.banner));

                if (results != 1)
                {
                    Log.write(TLog.Warning, $"Ignoring player banner update for {player.alias}, not present in database.");
                }
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
            Zone.Player player = zone.getPlayer(pkt.player.id);

            if (player == null)
            {
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