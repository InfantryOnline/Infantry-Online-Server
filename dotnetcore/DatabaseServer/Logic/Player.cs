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

            using (Database.DataContext db = zone._server.getContext())
            {	//Get the associated player entry
                Database.Player dbplayer = db.Players.SingleOrDefault(plyr => plyr.Id == player.dbid);

                if (dbplayer == null)
                {	//Make a note
                    Log.write(TLog.Warning, "Ignoring player update for {0}, not present in database.", player.alias);
                    return;
                }

                UpdateDailyWeeklyMonthlyYearlyStats(db, pkt, zone, dbplayer);

                // Write the new stats to the stats table.
                Stat stats = dbplayer.StatsNavigation;

                stats.Zonestat1 = pkt.stats.zonestat1;
                stats.Zonestat2 = pkt.stats.zonestat2;
                stats.Zonestat3 = pkt.stats.zonestat3;
                stats.Zonestat4 = pkt.stats.zonestat4;
                stats.Zonestat5 = pkt.stats.zonestat5;
                stats.Zonestat6 = pkt.stats.zonestat6;
                stats.Zonestat7 = pkt.stats.zonestat7;
                stats.Zonestat8 = pkt.stats.zonestat8;
                stats.Zonestat9 = pkt.stats.zonestat9;
                stats.Zonestat10 = pkt.stats.zonestat10;
                stats.Zonestat11 = pkt.stats.zonestat11;
                stats.Zonestat12 = pkt.stats.zonestat12;

                stats.Kills = pkt.stats.kills;
                stats.Deaths = pkt.stats.deaths;
                stats.KillPoints = pkt.stats.killPoints;
                stats.DeathPoints = pkt.stats.deathPoints;
                stats.AssistPoints = pkt.stats.assistPoints;
                stats.BonusPoints = pkt.stats.bonusPoints;
                stats.VehicleKills = pkt.stats.vehicleKills;
                stats.VehicleDeaths = pkt.stats.vehicleDeaths;
                stats.PlaySeconds = pkt.stats.playSeconds;

                stats.Cash = pkt.stats.cash;
                stats.Experience = pkt.stats.experience;
                stats.ExperienceTotal = pkt.stats.experienceTotal;

                //Convert inventory and skills
                dbplayer.Inventory = DBHelpers.inventoryToBin(pkt.stats.inventory);
                dbplayer.Skills = DBHelpers.skillsToBin(pkt.stats.skills);

                //Update all changes
                db.SaveChanges();
            }
        }

        static private void UpdateDailyWeeklyMonthlyYearlyStats(DataContext db, CS_PlayerUpdate<Zone> pkt, Zone zone, Database.Player player)
        {
            // 1. Get the deltas from the current stats table, and then add it to each of the long-term stat categories.

            Stat stats = player.StatsNavigation;

            var zs1 = pkt.stats.zonestat1 - stats.Zonestat1;
            var zs2 = pkt.stats.zonestat2 - stats.Zonestat2;
            var zs3 = pkt.stats.zonestat3 - stats.Zonestat3;
            var zs4 = pkt.stats.zonestat4 - stats.Zonestat4;
            var zs5 = pkt.stats.zonestat5 - stats.Zonestat5;
            var zs6 = pkt.stats.zonestat6 - stats.Zonestat6;
            var zs7 = pkt.stats.zonestat7 - stats.Zonestat7;
            var zs8 = pkt.stats.zonestat8 - stats.Zonestat8;
            var zs9 = pkt.stats.zonestat9 - stats.Zonestat9;
            var zs10 = pkt.stats.zonestat10 - stats.Zonestat10;
            var zs11 = pkt.stats.zonestat11 - stats.Zonestat11;
            var zs12 = pkt.stats.zonestat12 - stats.Zonestat12;

            var kills = pkt.stats.kills - stats.Kills;
            var deaths = pkt.stats.deaths - stats.Deaths;
            var killPoints = pkt.stats.killPoints - stats.KillPoints;
            var deathPoints = pkt.stats.deathPoints - stats.DeathPoints;
            var assistPoints = pkt.stats.assistPoints - stats.AssistPoints;
            var bonusPoints = pkt.stats.bonusPoints - stats.BonusPoints;
            var vehicleKills = pkt.stats.vehicleKills - stats.VehicleKills;
            var vehicleDeaths = pkt.stats.vehicleDeaths - stats.VehicleDeaths;
            var playSeconds = pkt.stats.playSeconds - stats.PlaySeconds;

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

            var daily = db.StatsDailies.FirstOrDefault(s => s.Date == day && s.Player == player.Id);

            if (daily == null)
            {
                daily = new StatsDaily();
                daily.Zone = player.Zone;
                daily.Player = player.Id;
                daily.Date = day;

                db.StatsDailies.Add(daily);
            }

            daily.Kills += kills;
            daily.Deaths += deaths;
            daily.KillPoints += killPoints;
            daily.DeathPoints += deathPoints;
            daily.AssistPoints += assistPoints;
            daily.BonusPoints += bonusPoints;
            daily.VehicleKills += vehicleKills;
            daily.VehicleDeaths += vehicleDeaths;
            daily.PlaySeconds += playSeconds;

            daily.Zonestat1 += zs1;
            daily.Zonestat2 += zs2;
            daily.Zonestat3 += zs3;
            daily.Zonestat4 += zs4;
            daily.Zonestat5 += zs5;
            daily.Zonestat6 += zs6;
            daily.Zonestat7 += zs7;
            daily.Zonestat8 += zs8;
            daily.Zonestat9 += zs9;
            daily.Zonestat10 += zs10;
            daily.Zonestat11 += zs11;
            daily.Zonestat12 += zs12;

            // Update Weekly

            var weekly = db.StatsWeeklies.FirstOrDefault(s => s.Date == week && s.Player == player.Id);

            if (weekly == null)
            {
                weekly = new StatsWeekly();
                weekly.Zone = player.Zone;
                weekly.Player = player.Id;
                weekly.Date = week;

                db.StatsWeeklies.Add(weekly);
            }

            weekly.Kills += kills;
            weekly.Deaths += deaths;
            weekly.KillPoints += killPoints;
            weekly.DeathPoints += deathPoints;
            weekly.AssistPoints += assistPoints;
            weekly.BonusPoints += bonusPoints;
            weekly.VehicleKills += vehicleKills;
            weekly.VehicleDeaths += vehicleDeaths;
            weekly.PlaySeconds += playSeconds;

            weekly.Zonestat1 += zs1;
            weekly.Zonestat2 += zs2;
            weekly.Zonestat3 += zs3;
            weekly.Zonestat4 += zs4;
            weekly.Zonestat5 += zs5;
            weekly.Zonestat6 += zs6;
            weekly.Zonestat7 += zs7;
            weekly.Zonestat8 += zs8;
            weekly.Zonestat9 += zs9;
            weekly.Zonestat10 += zs10;
            weekly.Zonestat11 += zs11;
            weekly.Zonestat12 += zs12;

            // Update Monthly

            var monthly = db.StatsMonthlies.FirstOrDefault(s => s.Date == month && s.Player == player.Id);

            if (monthly == null)
            {
                monthly = new StatsMonthly();
                monthly.Zone = player.Zone;
                monthly.Player = player.Id;
                monthly.Date = month;

                db.StatsMonthlies.Add(monthly);
            }

            monthly.Kills += kills;
            monthly.Deaths += deaths;
            monthly.KillPoints += killPoints;
            monthly.DeathPoints += deathPoints;
            monthly.AssistPoints += assistPoints;
            monthly.BonusPoints += bonusPoints;
            monthly.VehicleKills += vehicleKills;
            monthly.VehicleDeaths += vehicleDeaths;
            monthly.PlaySeconds += playSeconds;

            monthly.Zonestat1 += zs1;
            monthly.Zonestat2 += zs2;
            monthly.Zonestat3 += zs3;
            monthly.Zonestat4 += zs4;
            monthly.Zonestat5 += zs5;
            monthly.Zonestat6 += zs6;
            monthly.Zonestat7 += zs7;
            monthly.Zonestat8 += zs8;
            monthly.Zonestat9 += zs9;
            monthly.Zonestat10 += zs10;
            monthly.Zonestat11 += zs11;
            monthly.Zonestat12 += zs12;

            // Update Yearly

            var yearly = db.StatsYearlies.FirstOrDefault(s => s.Date == year && s.Player == player.Id);

            if (yearly == null)
            {
                yearly = new StatsYearly();
                yearly.Zone = player.Zone;
                yearly.Player = player.Id;
                yearly.Date = year;

                db.StatsYearlies.Add(yearly);
            }

            yearly.Kills += kills;
            yearly.Deaths += deaths;
            yearly.KillPoints += killPoints;
            yearly.DeathPoints += deathPoints;
            yearly.AssistPoints += assistPoints;
            yearly.BonusPoints += bonusPoints;
            yearly.VehicleKills += vehicleKills;
            yearly.VehicleDeaths += vehicleDeaths;
            yearly.PlaySeconds += playSeconds;

            yearly.Zonestat1 += zs1;
            yearly.Zonestat2 += zs2;
            yearly.Zonestat3 += zs3;
            yearly.Zonestat4 += zs4;
            yearly.Zonestat5 += zs5;
            yearly.Zonestat6 += zs6;
            yearly.Zonestat7 += zs7;
            yearly.Zonestat8 += zs8;
            yearly.Zonestat9 += zs9;
            yearly.Zonestat10 += zs10;
            yearly.Zonestat11 += zs11;
            yearly.Zonestat12 += zs12;
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

            using (Database.DataContext db = zone._server.getContext())
            {	//Get the associated player entry
                Database.Player dbplayer = db.Players.SingleOrDefault(plyr => plyr.Id == player.dbid);
                if (dbplayer == null)
                {	//Make a note
                    Log.write(TLog.Warning, "Ignoring player banner update for {0}, not present in database.", player.alias);
                    return;
                }

                dbplayer.Banner = pkt.banner;

                //Update all changes
                db.SaveChanges();
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