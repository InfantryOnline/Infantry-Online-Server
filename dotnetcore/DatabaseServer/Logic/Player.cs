using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InfServer.Protocol;
using InfServer.Data;
using InfServer;
using System.Globalization;
using Database.SqlServer;
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
            // Update current stats, and then update historic (accrued) stats.
            //

            var statUpdateRowCount = ctx.Stats
                .Where(s => s.StatId == player.statsid)
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
                .Where(p => p.PlayerId == player.dbid)
                .ExecuteUpdate(setters => setters
                    .SetProperty(p => p.Inventory, DatabaseBinaryUtils.inventoryToBin(pkt.stats.inventory))
                    .SetProperty(p => p.Skills, DatabaseBinaryUtils.skillsToBin(pkt.stats.skills)));


            //
            // Update the accrued statistics using the existing stats as a baseline.
            //
            UpdateDailyWeeklyMonthlyYearlyStats(pkt, zone, player, ctx, player.stats);

            //
            // Lastly, write the saved stats back to the cached stats object.
            //

            player.stats.Zonestat1 = pkt.stats.zonestat1;
            player.stats.Zonestat2 = pkt.stats.zonestat2;
            player.stats.Zonestat3 = pkt.stats.zonestat3;
            player.stats.Zonestat4 = pkt.stats.zonestat4;
            player.stats.Zonestat5 = pkt.stats.zonestat5;
            player.stats.Zonestat6 = pkt.stats.zonestat6;
            player.stats.Zonestat7 = pkt.stats.zonestat7;
            player.stats.Zonestat8 = pkt.stats.zonestat8;
            player.stats.Zonestat9 = pkt.stats.zonestat9;
            player.stats.Zonestat10 = pkt.stats.zonestat10;
            player.stats.Zonestat11 = pkt.stats.zonestat11;
            player.stats.Zonestat12 = pkt.stats.zonestat12;

            player.stats.Kills = pkt.stats.kills;
            player.stats.Deaths = pkt.stats.deaths;
            player.stats.KillPoints = pkt.stats.killPoints;
            player.stats.DeathPoints = pkt.stats.deathPoints;
            player.stats.AssistPoints = pkt.stats.assistPoints;
            player.stats.BonusPoints = pkt.stats.bonusPoints;
            player.stats.VehicleKills = pkt.stats.vehicleKills;
            player.stats.VehicleDeaths = pkt.stats.vehicleDeaths;
            player.stats.PlaySeconds = pkt.stats.playSeconds;

            player.stats.Cash = pkt.stats.cash;
            player.stats.Experience = pkt.stats.experience;
            player.stats.ExperienceTotal = pkt.stats.experienceTotal;
        }

        static private int ClampStat(long value)
        {
            if (value <= 0)
                return 0;

            if (value >= int.MaxValue)
                return int.MaxValue;

            return (int)value;
        }

        static private int PositiveDelta(int current, int previous)
        {
            if (current <= previous)
                return 0;

            return ClampStat((long)current - previous);
        }

        static private int AddStat(int current, int delta)
        {
            return ClampStat((long)current + delta);
        }

        static private void UpdateDailyWeeklyMonthlyYearlyStats(CS_PlayerUpdate<Zone> pkt, Zone zone, Zone.Player player, InfantryDbContext ctx, Stat previousStat)
        {
            //
            // Subtract to get the delta from our previous stats,
            // and then proceed to add this delta to the accruals.
            //

            var zs1 = PositiveDelta(pkt.stats.zonestat1, previousStat.Zonestat1);
            var zs2 = PositiveDelta(pkt.stats.zonestat2, previousStat.Zonestat2);
            var zs3 = PositiveDelta(pkt.stats.zonestat3, previousStat.Zonestat3);
            var zs4 = PositiveDelta(pkt.stats.zonestat4, previousStat.Zonestat4);
            var zs5 = PositiveDelta(pkt.stats.zonestat5, previousStat.Zonestat5);
            var zs6 = PositiveDelta(pkt.stats.zonestat6, previousStat.Zonestat6);
            var zs7 = PositiveDelta(pkt.stats.zonestat7, previousStat.Zonestat7);
            var zs8 = PositiveDelta(pkt.stats.zonestat8, previousStat.Zonestat8);
            var zs9 = PositiveDelta(pkt.stats.zonestat9, previousStat.Zonestat9);
            var zs10 = PositiveDelta(pkt.stats.zonestat10, previousStat.Zonestat10);
            var zs11 = PositiveDelta(pkt.stats.zonestat11, previousStat.Zonestat11);
            var zs12 = PositiveDelta(pkt.stats.zonestat12, previousStat.Zonestat12);

            var kills = PositiveDelta(pkt.stats.kills, previousStat.Kills);
            var deaths = PositiveDelta(pkt.stats.deaths, previousStat.Deaths);
            var killPoints = PositiveDelta(pkt.stats.killPoints, previousStat.KillPoints);
            var deathPoints = PositiveDelta(pkt.stats.deathPoints, previousStat.DeathPoints);
            var assistPoints = PositiveDelta(pkt.stats.assistPoints, previousStat.AssistPoints);
            var bonusPoints = PositiveDelta(pkt.stats.bonusPoints, previousStat.BonusPoints);
            var vehicleKills = PositiveDelta(pkt.stats.vehicleKills, previousStat.VehicleKills);
            var vehicleDeaths = PositiveDelta(pkt.stats.vehicleDeaths, previousStat.VehicleDeaths);
            var playSeconds = PositiveDelta(pkt.stats.playSeconds, previousStat.PlaySeconds);
            var experienceTotal = PositiveDelta(pkt.stats.experienceTotal, previousStat.ExperienceTotal);

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
                .Where(s => s.Date == day && s.PlayerId == player.dbid && s.ZoneId == zone._zone.ZoneId)
                .ExecuteUpdate(setters => setters
                    .SetProperty(s => s.ExperienceTotal, s => s.ExperienceTotal > int.MaxValue - experienceTotal ? int.MaxValue : s.ExperienceTotal + experienceTotal)
                    .SetProperty(s => s.Zonestat1, s => s.Zonestat1 > int.MaxValue - zs1 ? int.MaxValue : s.Zonestat1 + zs1)
                    .SetProperty(s => s.Zonestat2, s => s.Zonestat2 > int.MaxValue - zs2 ? int.MaxValue : s.Zonestat2 + zs2)
                    .SetProperty(s => s.Zonestat3, s => s.Zonestat3 > int.MaxValue - zs3 ? int.MaxValue : s.Zonestat3 + zs3)
                    .SetProperty(s => s.Zonestat4, s => s.Zonestat4 > int.MaxValue - zs4 ? int.MaxValue : s.Zonestat4 + zs4)
                    .SetProperty(s => s.Zonestat5, s => s.Zonestat5 > int.MaxValue - zs5 ? int.MaxValue : s.Zonestat5 + zs5)
                    .SetProperty(s => s.Zonestat6, s => s.Zonestat6 > int.MaxValue - zs6 ? int.MaxValue : s.Zonestat6 + zs6)
                    .SetProperty(s => s.Zonestat7, s => s.Zonestat7 > int.MaxValue - zs7 ? int.MaxValue : s.Zonestat7 + zs7)
                    .SetProperty(s => s.Zonestat8, s => s.Zonestat8 > int.MaxValue - zs8 ? int.MaxValue : s.Zonestat8 + zs8)
                    .SetProperty(s => s.Zonestat9, s => s.Zonestat9 > int.MaxValue - zs9 ? int.MaxValue : s.Zonestat9 + zs9)
                    .SetProperty(s => s.Zonestat10, s => s.Zonestat10 > int.MaxValue - zs10 ? int.MaxValue : s.Zonestat10 + zs10)
                    .SetProperty(s => s.Zonestat11, s => s.Zonestat11 > int.MaxValue - zs11 ? int.MaxValue : s.Zonestat11 + zs11)
                    .SetProperty(s => s.Zonestat12, s => s.Zonestat12 > int.MaxValue - zs12 ? int.MaxValue : s.Zonestat12 + zs12)

                    .SetProperty(s => s.Kills, s => s.Kills > int.MaxValue - kills ? int.MaxValue : s.Kills + kills)
                    .SetProperty(s => s.Deaths, s => s.Deaths > int.MaxValue - deaths ? int.MaxValue : s.Deaths + deaths)
                    .SetProperty(s => s.KillPoints, s => s.KillPoints > int.MaxValue - killPoints ? int.MaxValue : s.KillPoints + killPoints)
                    .SetProperty(s => s.DeathPoints, s => s.DeathPoints > int.MaxValue - deathPoints ? int.MaxValue : s.DeathPoints + deathPoints)
                    .SetProperty(s => s.AssistPoints, s => s.AssistPoints > int.MaxValue - assistPoints ? int.MaxValue : s.AssistPoints + assistPoints)
                    .SetProperty(s => s.BonusPoints, s => s.BonusPoints > int.MaxValue - bonusPoints ? int.MaxValue : s.BonusPoints + bonusPoints)
                    .SetProperty(s => s.VehicleKills, s => s.VehicleKills > int.MaxValue - vehicleKills ? int.MaxValue : s.VehicleKills + vehicleKills)
                    .SetProperty(s => s.VehicleDeaths, s => s.VehicleDeaths > int.MaxValue - vehicleDeaths ? int.MaxValue : s.VehicleDeaths + vehicleDeaths)
                    .SetProperty(s => s.PlaySeconds, s => s.PlaySeconds > int.MaxValue - playSeconds ? int.MaxValue : s.PlaySeconds + playSeconds));

            if (dailyRowsUpdated == 0)
            {
                var stat = new StatsDaily();

                stat.ZoneId = zone._zone.ZoneId;
                stat.Date = day;
                stat.PlayerId = player.dbid;

                stat.ExperienceTotal = AddStat(stat.ExperienceTotal, experienceTotal);
                stat.Kills = AddStat(stat.Kills, kills);
                stat.Deaths = AddStat(stat.Deaths, deaths);
                stat.KillPoints = AddStat(stat.KillPoints, killPoints);
                stat.DeathPoints = AddStat(stat.DeathPoints, deathPoints);
                stat.AssistPoints = AddStat(stat.AssistPoints, assistPoints);
                stat.BonusPoints = AddStat(stat.BonusPoints, bonusPoints);
                stat.VehicleKills = AddStat(stat.VehicleKills, vehicleKills);
                stat.VehicleDeaths = AddStat(stat.VehicleDeaths, vehicleDeaths);
                stat.PlaySeconds = AddStat(stat.PlaySeconds, playSeconds);

                stat.Zonestat1 = AddStat(stat.Zonestat1, zs1);
                stat.Zonestat2 = AddStat(stat.Zonestat2, zs2);
                stat.Zonestat3 = AddStat(stat.Zonestat3, zs3);
                stat.Zonestat4 = AddStat(stat.Zonestat4, zs4);
                stat.Zonestat5 = AddStat(stat.Zonestat5, zs5);
                stat.Zonestat6 = AddStat(stat.Zonestat6, zs6);
                stat.Zonestat7 = AddStat(stat.Zonestat7, zs7);
                stat.Zonestat8 = AddStat(stat.Zonestat8, zs8);
                stat.Zonestat9 = AddStat(stat.Zonestat9, zs9);
                stat.Zonestat10 = AddStat(stat.Zonestat10, zs10);
                stat.Zonestat11 = AddStat(stat.Zonestat11, zs11);
                stat.Zonestat12 = AddStat(stat.Zonestat12, zs12);

                ctx.StatsDailies.Add(stat);
                ctx.SaveChanges();
            }

            // Update or Insert Weekly

            var weeklyRowsUpdated = ctx.StatsWeeklies
                .Where(s => s.Date == week && s.PlayerId == player.dbid && s.ZoneId == zone._zone.ZoneId)
                .ExecuteUpdate(setters => setters
                    .SetProperty(s => s.ExperienceTotal, s => s.ExperienceTotal > int.MaxValue - experienceTotal ? int.MaxValue : s.ExperienceTotal + experienceTotal)
                    .SetProperty(s => s.Zonestat1, s => s.Zonestat1 > int.MaxValue - zs1 ? int.MaxValue : s.Zonestat1 + zs1)
                    .SetProperty(s => s.Zonestat2, s => s.Zonestat2 > int.MaxValue - zs2 ? int.MaxValue : s.Zonestat2 + zs2)
                    .SetProperty(s => s.Zonestat3, s => s.Zonestat3 > int.MaxValue - zs3 ? int.MaxValue : s.Zonestat3 + zs3)
                    .SetProperty(s => s.Zonestat4, s => s.Zonestat4 > int.MaxValue - zs4 ? int.MaxValue : s.Zonestat4 + zs4)
                    .SetProperty(s => s.Zonestat5, s => s.Zonestat5 > int.MaxValue - zs5 ? int.MaxValue : s.Zonestat5 + zs5)
                    .SetProperty(s => s.Zonestat6, s => s.Zonestat6 > int.MaxValue - zs6 ? int.MaxValue : s.Zonestat6 + zs6)
                    .SetProperty(s => s.Zonestat7, s => s.Zonestat7 > int.MaxValue - zs7 ? int.MaxValue : s.Zonestat7 + zs7)
                    .SetProperty(s => s.Zonestat8, s => s.Zonestat8 > int.MaxValue - zs8 ? int.MaxValue : s.Zonestat8 + zs8)
                    .SetProperty(s => s.Zonestat9, s => s.Zonestat9 > int.MaxValue - zs9 ? int.MaxValue : s.Zonestat9 + zs9)
                    .SetProperty(s => s.Zonestat10, s => s.Zonestat10 > int.MaxValue - zs10 ? int.MaxValue : s.Zonestat10 + zs10)
                    .SetProperty(s => s.Zonestat11, s => s.Zonestat11 > int.MaxValue - zs11 ? int.MaxValue : s.Zonestat11 + zs11)
                    .SetProperty(s => s.Zonestat12, s => s.Zonestat12 > int.MaxValue - zs12 ? int.MaxValue : s.Zonestat12 + zs12)

                    .SetProperty(s => s.Kills, s => s.Kills > int.MaxValue - kills ? int.MaxValue : s.Kills + kills)
                    .SetProperty(s => s.Deaths, s => s.Deaths > int.MaxValue - deaths ? int.MaxValue : s.Deaths + deaths)
                    .SetProperty(s => s.KillPoints, s => s.KillPoints > int.MaxValue - killPoints ? int.MaxValue : s.KillPoints + killPoints)
                    .SetProperty(s => s.DeathPoints, s => s.DeathPoints > int.MaxValue - deathPoints ? int.MaxValue : s.DeathPoints + deathPoints)
                    .SetProperty(s => s.AssistPoints, s => s.AssistPoints > int.MaxValue - assistPoints ? int.MaxValue : s.AssistPoints + assistPoints)
                    .SetProperty(s => s.BonusPoints, s => s.BonusPoints > int.MaxValue - bonusPoints ? int.MaxValue : s.BonusPoints + bonusPoints)
                    .SetProperty(s => s.VehicleKills, s => s.VehicleKills > int.MaxValue - vehicleKills ? int.MaxValue : s.VehicleKills + vehicleKills)
                    .SetProperty(s => s.VehicleDeaths, s => s.VehicleDeaths > int.MaxValue - vehicleDeaths ? int.MaxValue : s.VehicleDeaths + vehicleDeaths)
                    .SetProperty(s => s.PlaySeconds, s => s.PlaySeconds > int.MaxValue - playSeconds ? int.MaxValue : s.PlaySeconds + playSeconds));

            if (weeklyRowsUpdated == 0)
            {
                var stat = new StatsWeekly();

                stat.ZoneId = zone._zone.ZoneId;
                stat.Date = week;
                stat.PlayerId = player.dbid;

                stat.ExperienceTotal = AddStat(stat.ExperienceTotal, experienceTotal);
                stat.Kills = AddStat(stat.Kills, kills);
                stat.Deaths = AddStat(stat.Deaths, deaths);
                stat.KillPoints = AddStat(stat.KillPoints, killPoints);
                stat.DeathPoints = AddStat(stat.DeathPoints, deathPoints);
                stat.AssistPoints = AddStat(stat.AssistPoints, assistPoints);
                stat.BonusPoints = AddStat(stat.BonusPoints, bonusPoints);
                stat.VehicleKills = AddStat(stat.VehicleKills, vehicleKills);
                stat.VehicleDeaths = AddStat(stat.VehicleDeaths, vehicleDeaths);
                stat.PlaySeconds = AddStat(stat.PlaySeconds, playSeconds);

                stat.Zonestat1 = AddStat(stat.Zonestat1, zs1);
                stat.Zonestat2 = AddStat(stat.Zonestat2, zs2);
                stat.Zonestat3 = AddStat(stat.Zonestat3, zs3);
                stat.Zonestat4 = AddStat(stat.Zonestat4, zs4);
                stat.Zonestat5 = AddStat(stat.Zonestat5, zs5);
                stat.Zonestat6 = AddStat(stat.Zonestat6, zs6);
                stat.Zonestat7 = AddStat(stat.Zonestat7, zs7);
                stat.Zonestat8 = AddStat(stat.Zonestat8, zs8);
                stat.Zonestat9 = AddStat(stat.Zonestat9, zs9);
                stat.Zonestat10 = AddStat(stat.Zonestat10, zs10);
                stat.Zonestat11 = AddStat(stat.Zonestat11, zs11);
                stat.Zonestat12 = AddStat(stat.Zonestat12, zs12);

                ctx.StatsWeeklies.Add(stat);
                ctx.SaveChanges();
            }

            // Update or Insert Monthly

            var monthlyRowsUpdated = ctx.StatsMonthlies
                .Where(s => s.Date == month && s.PlayerId == player.dbid && s.ZoneId == zone._zone.ZoneId)
                .ExecuteUpdate(setters => setters
                    .SetProperty(s => s.ExperienceTotal, s => s.ExperienceTotal > int.MaxValue - experienceTotal ? int.MaxValue : s.ExperienceTotal + experienceTotal)
                    .SetProperty(s => s.Zonestat1, s => s.Zonestat1 > int.MaxValue - zs1 ? int.MaxValue : s.Zonestat1 + zs1)
                    .SetProperty(s => s.Zonestat2, s => s.Zonestat2 > int.MaxValue - zs2 ? int.MaxValue : s.Zonestat2 + zs2)
                    .SetProperty(s => s.Zonestat3, s => s.Zonestat3 > int.MaxValue - zs3 ? int.MaxValue : s.Zonestat3 + zs3)
                    .SetProperty(s => s.Zonestat4, s => s.Zonestat4 > int.MaxValue - zs4 ? int.MaxValue : s.Zonestat4 + zs4)
                    .SetProperty(s => s.Zonestat5, s => s.Zonestat5 > int.MaxValue - zs5 ? int.MaxValue : s.Zonestat5 + zs5)
                    .SetProperty(s => s.Zonestat6, s => s.Zonestat6 > int.MaxValue - zs6 ? int.MaxValue : s.Zonestat6 + zs6)
                    .SetProperty(s => s.Zonestat7, s => s.Zonestat7 > int.MaxValue - zs7 ? int.MaxValue : s.Zonestat7 + zs7)
                    .SetProperty(s => s.Zonestat8, s => s.Zonestat8 > int.MaxValue - zs8 ? int.MaxValue : s.Zonestat8 + zs8)
                    .SetProperty(s => s.Zonestat9, s => s.Zonestat9 > int.MaxValue - zs9 ? int.MaxValue : s.Zonestat9 + zs9)
                    .SetProperty(s => s.Zonestat10, s => s.Zonestat10 > int.MaxValue - zs10 ? int.MaxValue : s.Zonestat10 + zs10)
                    .SetProperty(s => s.Zonestat11, s => s.Zonestat11 > int.MaxValue - zs11 ? int.MaxValue : s.Zonestat11 + zs11)
                    .SetProperty(s => s.Zonestat12, s => s.Zonestat12 > int.MaxValue - zs12 ? int.MaxValue : s.Zonestat12 + zs12)

                    .SetProperty(s => s.Kills, s => s.Kills > int.MaxValue - kills ? int.MaxValue : s.Kills + kills)
                    .SetProperty(s => s.Deaths, s => s.Deaths > int.MaxValue - deaths ? int.MaxValue : s.Deaths + deaths)
                    .SetProperty(s => s.KillPoints, s => s.KillPoints > int.MaxValue - killPoints ? int.MaxValue : s.KillPoints + killPoints)
                    .SetProperty(s => s.DeathPoints, s => s.DeathPoints > int.MaxValue - deathPoints ? int.MaxValue : s.DeathPoints + deathPoints)
                    .SetProperty(s => s.AssistPoints, s => s.AssistPoints > int.MaxValue - assistPoints ? int.MaxValue : s.AssistPoints + assistPoints)
                    .SetProperty(s => s.BonusPoints, s => s.BonusPoints > int.MaxValue - bonusPoints ? int.MaxValue : s.BonusPoints + bonusPoints)
                    .SetProperty(s => s.VehicleKills, s => s.VehicleKills > int.MaxValue - vehicleKills ? int.MaxValue : s.VehicleKills + vehicleKills)
                    .SetProperty(s => s.VehicleDeaths, s => s.VehicleDeaths > int.MaxValue - vehicleDeaths ? int.MaxValue : s.VehicleDeaths + vehicleDeaths)
                    .SetProperty(s => s.PlaySeconds, s => s.PlaySeconds > int.MaxValue - playSeconds ? int.MaxValue : s.PlaySeconds + playSeconds));

            if (monthlyRowsUpdated == 0)
            {
                var stat = new StatsMonthly();

                stat.ZoneId = zone._zone.ZoneId;
                stat.Date = month;
                stat.PlayerId = player.dbid;

                stat.ExperienceTotal = AddStat(stat.ExperienceTotal, experienceTotal);
                stat.Kills = AddStat(stat.Kills, kills);
                stat.Deaths = AddStat(stat.Deaths, deaths);
                stat.KillPoints = AddStat(stat.KillPoints, killPoints);
                stat.DeathPoints = AddStat(stat.DeathPoints, deathPoints);
                stat.AssistPoints = AddStat(stat.AssistPoints, assistPoints);
                stat.BonusPoints = AddStat(stat.BonusPoints, bonusPoints);
                stat.VehicleKills = AddStat(stat.VehicleKills, vehicleKills);
                stat.VehicleDeaths = AddStat(stat.VehicleDeaths, vehicleDeaths);
                stat.PlaySeconds = AddStat(stat.PlaySeconds, playSeconds);

                stat.Zonestat1 = AddStat(stat.Zonestat1, zs1);
                stat.Zonestat2 = AddStat(stat.Zonestat2, zs2);
                stat.Zonestat3 = AddStat(stat.Zonestat3, zs3);
                stat.Zonestat4 = AddStat(stat.Zonestat4, zs4);
                stat.Zonestat5 = AddStat(stat.Zonestat5, zs5);
                stat.Zonestat6 = AddStat(stat.Zonestat6, zs6);
                stat.Zonestat7 = AddStat(stat.Zonestat7, zs7);
                stat.Zonestat8 = AddStat(stat.Zonestat8, zs8);
                stat.Zonestat9 = AddStat(stat.Zonestat9, zs9);
                stat.Zonestat10 = AddStat(stat.Zonestat10, zs10);
                stat.Zonestat11 = AddStat(stat.Zonestat11, zs11);
                stat.Zonestat12 = AddStat(stat.Zonestat12, zs12);

                ctx.StatsMonthlies.Add(stat);
                ctx.SaveChanges();
            }

            // Update or Insert Yearly

            var yearlyRowsUpdated = ctx.StatsYearlies
                .Where(s => s.Date == year && s.PlayerId == player.dbid && s.ZoneId == zone._zone.ZoneId)
                .ExecuteUpdate(setters => setters
                    .SetProperty(s => s.ExperienceTotal, s => s.ExperienceTotal > int.MaxValue - experienceTotal ? int.MaxValue : s.ExperienceTotal + experienceTotal)
                    .SetProperty(s => s.Zonestat1, s => s.Zonestat1 > int.MaxValue - zs1 ? int.MaxValue : s.Zonestat1 + zs1)
                    .SetProperty(s => s.Zonestat2, s => s.Zonestat2 > int.MaxValue - zs2 ? int.MaxValue : s.Zonestat2 + zs2)
                    .SetProperty(s => s.Zonestat3, s => s.Zonestat3 > int.MaxValue - zs3 ? int.MaxValue : s.Zonestat3 + zs3)
                    .SetProperty(s => s.Zonestat4, s => s.Zonestat4 > int.MaxValue - zs4 ? int.MaxValue : s.Zonestat4 + zs4)
                    .SetProperty(s => s.Zonestat5, s => s.Zonestat5 > int.MaxValue - zs5 ? int.MaxValue : s.Zonestat5 + zs5)
                    .SetProperty(s => s.Zonestat6, s => s.Zonestat6 > int.MaxValue - zs6 ? int.MaxValue : s.Zonestat6 + zs6)
                    .SetProperty(s => s.Zonestat7, s => s.Zonestat7 > int.MaxValue - zs7 ? int.MaxValue : s.Zonestat7 + zs7)
                    .SetProperty(s => s.Zonestat8, s => s.Zonestat8 > int.MaxValue - zs8 ? int.MaxValue : s.Zonestat8 + zs8)
                    .SetProperty(s => s.Zonestat9, s => s.Zonestat9 > int.MaxValue - zs9 ? int.MaxValue : s.Zonestat9 + zs9)
                    .SetProperty(s => s.Zonestat10, s => s.Zonestat10 > int.MaxValue - zs10 ? int.MaxValue : s.Zonestat10 + zs10)
                    .SetProperty(s => s.Zonestat11, s => s.Zonestat11 > int.MaxValue - zs11 ? int.MaxValue : s.Zonestat11 + zs11)
                    .SetProperty(s => s.Zonestat12, s => s.Zonestat12 > int.MaxValue - zs12 ? int.MaxValue : s.Zonestat12 + zs12)

                    .SetProperty(s => s.Kills, s => s.Kills > int.MaxValue - kills ? int.MaxValue : s.Kills + kills)
                    .SetProperty(s => s.Deaths, s => s.Deaths > int.MaxValue - deaths ? int.MaxValue : s.Deaths + deaths)
                    .SetProperty(s => s.KillPoints, s => s.KillPoints > int.MaxValue - killPoints ? int.MaxValue : s.KillPoints + killPoints)
                    .SetProperty(s => s.DeathPoints, s => s.DeathPoints > int.MaxValue - deathPoints ? int.MaxValue : s.DeathPoints + deathPoints)
                    .SetProperty(s => s.AssistPoints, s => s.AssistPoints > int.MaxValue - assistPoints ? int.MaxValue : s.AssistPoints + assistPoints)
                    .SetProperty(s => s.BonusPoints, s => s.BonusPoints > int.MaxValue - bonusPoints ? int.MaxValue : s.BonusPoints + bonusPoints)
                    .SetProperty(s => s.VehicleKills, s => s.VehicleKills > int.MaxValue - vehicleKills ? int.MaxValue : s.VehicleKills + vehicleKills)
                    .SetProperty(s => s.VehicleDeaths, s => s.VehicleDeaths > int.MaxValue - vehicleDeaths ? int.MaxValue : s.VehicleDeaths + vehicleDeaths)
                    .SetProperty(s => s.PlaySeconds, s => s.PlaySeconds > int.MaxValue - playSeconds ? int.MaxValue : s.PlaySeconds + playSeconds));

            if (yearlyRowsUpdated == 0)
            {
                var stat = new StatsYearly();

                stat.ZoneId = zone._zone.ZoneId;
                stat.Date = year;
                stat.PlayerId = player.dbid;

                stat.ExperienceTotal = AddStat(stat.ExperienceTotal, experienceTotal);
                stat.Kills = AddStat(stat.Kills, kills);
                stat.Deaths = AddStat(stat.Deaths, deaths);
                stat.KillPoints = AddStat(stat.KillPoints, killPoints);
                stat.DeathPoints = AddStat(stat.DeathPoints, deathPoints);
                stat.AssistPoints = AddStat(stat.AssistPoints, assistPoints);
                stat.BonusPoints = AddStat(stat.BonusPoints, bonusPoints);
                stat.VehicleKills = AddStat(stat.VehicleKills, vehicleKills);
                stat.VehicleDeaths = AddStat(stat.VehicleDeaths, vehicleDeaths);
                stat.PlaySeconds = AddStat(stat.PlaySeconds, playSeconds);

                stat.Zonestat1 = AddStat(stat.Zonestat1, zs1);
                stat.Zonestat2 = AddStat(stat.Zonestat2, zs2);
                stat.Zonestat3 = AddStat(stat.Zonestat3, zs3);
                stat.Zonestat4 = AddStat(stat.Zonestat4, zs4);
                stat.Zonestat5 = AddStat(stat.Zonestat5, zs5);
                stat.Zonestat6 = AddStat(stat.Zonestat6, zs6);
                stat.Zonestat7 = AddStat(stat.Zonestat7, zs7);
                stat.Zonestat8 = AddStat(stat.Zonestat8, zs8);
                stat.Zonestat9 = AddStat(stat.Zonestat9, zs9);
                stat.Zonestat10 = AddStat(stat.Zonestat10, zs10);
                stat.Zonestat11 = AddStat(stat.Zonestat11, zs11);
                stat.Zonestat12 = AddStat(stat.Zonestat12, zs12);

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
                    .Where(p => p.PlayerId == player.dbid)
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
            var player = zone.getPlayer(pkt.player.id);

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
