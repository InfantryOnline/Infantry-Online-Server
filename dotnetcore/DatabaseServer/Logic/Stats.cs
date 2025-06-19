using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using InfServer.Protocol;
using InfServer.Data;
using Database.SqlServer;
using Database;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace InfServer.Logic
{	// Logic_Stats Class
    /// Handles statistics functionality
    ///////////////////////////////////////////////////////
    class Logic_Stats
    {
        /// <summary>
        /// Writes a scorechart element to a memory stream
        /// </summary>
        static private bool writeElementToBuffer(Stat stat, MemoryStream stream)
        {
            try
            {
                Player player = stat.Players.SingleOrDefault(s => s.StatId == stat.StatId);
                if (player == null)
                {	//Make a note
                    Log.write(TLog.Warning, "No player found for stat ID {0}.", stat.StatId);
                    return false;
                }

                BinaryWriter bw = new BinaryWriter(stream);

                bw.Write(player.AliasNavigation.Name.ToCharArray());
                bw.Write((byte)0);

                Squad squad = player.SquadNavigation;
                string squadname = "";
                if (squad != null)
                    squadname = squad.Name;

                bw.Write(squadname.ToCharArray());
                bw.Write((byte)0);

                bw.Write((short)2);
                bw.Write(stat.VehicleDeaths);
                bw.Write(stat.VehicleKills);
                bw.Write(stat.KillPoints);
                bw.Write(stat.DeathPoints);
                //bw.Write((int)0); //-Assist Points
                bw.Write(stat.AssistPoints);
                bw.Write(stat.BonusPoints);
                bw.Write(stat.Kills);
                bw.Write(stat.Deaths);
                bw.Write((int)0);
                bw.Write(stat.PlaySeconds);
                bw.Write(stat.Zonestat1);
                bw.Write(stat.Zonestat2);
                bw.Write(stat.Zonestat3);
                bw.Write(stat.Zonestat4);
                bw.Write(stat.Zonestat5);
                bw.Write(stat.Zonestat6);
                bw.Write(stat.Zonestat7);
                bw.Write(stat.Zonestat8);
                bw.Write(stat.Zonestat9);
                bw.Write(stat.Zonestat10);
                bw.Write(stat.Zonestat11);
                bw.Write(stat.Zonestat12);
            }

            catch (Exception e)
            {
                Log.write(TLog.Warning, "WriteElementToBuffer stat.id " + stat.StatId + ":" + e);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Handles a player stat request
        /// </summary>
        static public void Handle_CS_PlayerStatsRequest(CS_PlayerStatsRequest<Zone> pkt, Zone zone)
        {	//Attempt to find the player in question
            var player = zone.getPlayer(pkt.player.id);

            if (player == null)
            {	//Make a note
                Log.write(TLog.Warning, "Ignoring player stats request for #{0}, not present in zone mirror.", pkt.player.id);
                return;
            }

            using (SqlServerDbContext db = zone._server.getContext())
            {	//What sort of request are we dealing with?
                switch (pkt.type)
                {
                    case CS_PlayerStatsRequest<Zone>.ChartType.ScoreLifetime:
                        {	//Get the top100 stats sorted by points
                            var stats = (from st in db.Stats
                                         where st.ZoneNavigation == zone._zone
                                         orderby st.AssistPoints + st.BonusPoints + st.KillPoints descending
                                         select st).Take(100).ToList();

                            MemoryStream stream = new MemoryStream();
                            foreach (Stat lifetime in stats)
                                if (lifetime != null && writeElementToBuffer(lifetime, stream) == false)
                                    continue;

                            SC_PlayerStatsResponse<Zone> response = new SC_PlayerStatsResponse<Zone>();

                            response.player = pkt.player;
                            response.type = CS_PlayerStatsRequest<Zone>.ChartType.ScoreLifetime;
                            response.columns = "Top100 Lifetime Score,Name,Squad";
                            response.data = stream.ToArray();

                            zone._client.sendReliable(response, 1);
                        }
                        break;

                    case CS_PlayerStatsRequest<Zone>.ChartType.ScoreDaily:
                        {
                            DateTime now = DateTime.Today;

                            //Get the top100 stats sorted by points
                            //For today's date
                            var daily = (from dt in db.StatsDailies
                                         where dt.ZoneNavigation == zone._zone && dt.Date >= now
                                         orderby dt.AssistPoints + dt.BonusPoints + dt.KillPoints descending
                                         select dt).Take(100).ToList();

                            //Are they requesting a specific date?
                            if (pkt.options != "")
                            {
                                //Player wants to see yesterday's date
                                if (pkt.options.Equals("-1"))
                                {
                                    DateTime today = now;
                                    now = now.AddDays(-1);
                                    daily = (from dt in db.StatsDailies
                                             where dt.ZoneNavigation == zone._zone && dt.Date >= now && dt.Date < today
                                             orderby dt.AssistPoints + dt.BonusPoints + dt.KillPoints descending
                                             select dt).Take(100).ToList();
                                }
                                else //Specific date
                                {
                                    string[] args = pkt.options.Split('-');
                                    string final = string.Join("/", args);
                                    try
                                    {
                                        now = DateTime.Parse(final, System.Threading.Thread.CurrentThread.CurrentCulture.DateTimeFormat);
                                    }
                                    catch (FormatException)
                                    {
                                        //Wrong format, use yesterday as default
                                        now = (now.AddDays(-1));
                                    }
                                    DateTime add = now.AddDays(1);

                                    daily = (from dt in db.StatsDailies
                                             where dt.ZoneNavigation == zone._zone && dt.Date >= now && dt.Date < add
                                             orderby dt.AssistPoints + dt.BonusPoints + dt.KillPoints descending
                                             select dt).Take(100).ToList();
                                }
                            }

                            MemoryStream stream = new MemoryStream();
                            try
                            {
                                foreach (StatsDaily day in daily)
                                {
                                    BinaryWriter bw = new BinaryWriter(stream);
                                    bw.Write(day.PlayerNavigation.AliasNavigation.Name.ToCharArray());
                                    bw.Write((byte)0);

                                    Squad squad = day.PlayerNavigation.SquadNavigation;
                                    string squadname = "";
                                    if (squad != null)
                                        squadname = squad.Name;

                                    bw.Write(squadname.ToCharArray());
                                    bw.Write((byte)0);

                                    bw.Write((short)2);
                                    bw.Write(day.VehicleDeaths);
                                    bw.Write(day.VehicleKills);
                                    bw.Write(day.KillPoints);
                                    bw.Write(day.DeathPoints);
                                    bw.Write(day.AssistPoints);
                                    bw.Write(day.BonusPoints);
                                    bw.Write(day.Kills);
                                    bw.Write(day.Deaths);
                                    bw.Write((int)0);
                                    bw.Write(day.PlaySeconds);
                                    bw.Write(day.Zonestat1);
                                    bw.Write(day.Zonestat2);
                                    bw.Write(day.Zonestat3);
                                    bw.Write(day.Zonestat4);
                                    bw.Write(day.Zonestat5);
                                    bw.Write(day.Zonestat6);
                                    bw.Write(day.Zonestat7);
                                    bw.Write(day.Zonestat8);
                                    bw.Write(day.Zonestat9);
                                    bw.Write(day.Zonestat10);
                                    bw.Write(day.Zonestat11);
                                    bw.Write(day.Zonestat12);
                                }
                            }
                            catch (Exception e)
                            {
                                Log.write(TLog.Warning, "WriteElementDaily " + e);
                            }

                            SC_PlayerStatsResponse<Zone> response = new SC_PlayerStatsResponse<Zone>();

                            response.player = pkt.player;
                            response.type = CS_PlayerStatsRequest<Zone>.ChartType.ScoreDaily;
                            response.columns = "Top100 Daily Score,Name,Squad";
                            response.data = stream.ToArray();

                            zone._client.sendReliable(response, 1);
                        }
                        break;

                    case CS_PlayerStatsRequest<Zone>.ChartType.ScoreWeekly:
                        {
                            DateTime now = DateTime.Today;
                            if (((int)now.DayOfWeek) > 0)
                                now = now.AddDays(-((int)now.DayOfWeek));

                            //Get the top100 stats sorted by points
                            //For this week
                            var weekly = (from wt in db.StatsWeeklies
                                          where wt.ZoneNavigation == zone._zone && wt.Date >= now
                                          orderby wt.AssistPoints + wt.BonusPoints + wt.KillPoints descending
                                          select wt).Take(100).ToList();

                            //Are they requesting a specific date?
                            if (pkt.options != "")
                            {
                                //Player wants to see last week's date
                                if (pkt.options.Equals("-1"))
                                {
                                    DateTime today = now;
                                    now = now.AddDays(-7);
                                    weekly = (from wt in db.StatsWeeklies
                                              where wt.ZoneNavigation == zone._zone && wt.Date >= now && wt.Date < today
                                              orderby wt.AssistPoints + wt.BonusPoints + wt.KillPoints descending
                                              select wt).Take(100).ToList();
                                }
                                else //Specific date
                                {
                                    string[] args = pkt.options.Split('-');
                                    string final = string.Join("/", args);
                                    try
                                    {
                                        now = DateTime.Parse(final, System.Threading.Thread.CurrentThread.CurrentCulture.DateTimeFormat);
                                    }
                                    catch (FormatException)
                                    {
                                        //Wrong format, use last week as default
                                        now = (now.AddDays(-7));
                                    }
                                    DateTime add = now.AddDays(7);

                                    weekly = (from wt in db.StatsWeeklies
                                              where wt.ZoneNavigation == zone._zone && wt.Date >= now && wt.Date < add
                                              orderby wt.AssistPoints + wt.BonusPoints + wt.KillPoints descending
                                              select wt).Take(100).ToList();
                                }
                            }

                            MemoryStream stream = new MemoryStream();
                            try
                            {
                                foreach (StatsWeekly week in weekly)
                                {
                                    BinaryWriter bw = new BinaryWriter(stream);
                                    bw.Write(week.PlayerNavigation.AliasNavigation.Name.ToCharArray());
                                    bw.Write((byte)0);

                                    Squad squad = week.PlayerNavigation.SquadNavigation;
                                    string squadname = "";
                                    if (squad != null)
                                        squadname = squad.Name;

                                    bw.Write(squadname.ToCharArray());
                                    bw.Write((byte)0);

                                    bw.Write((short)2);
                                    bw.Write(week.VehicleDeaths);
                                    bw.Write(week.VehicleKills);
                                    bw.Write(week.KillPoints);
                                    bw.Write(week.DeathPoints);
                                    bw.Write(week.AssistPoints);
                                    bw.Write(week.BonusPoints);
                                    bw.Write(week.Kills);
                                    bw.Write(week.Deaths);
                                    bw.Write((int)0);
                                    bw.Write(week.PlaySeconds);
                                    bw.Write(week.Zonestat1);
                                    bw.Write(week.Zonestat2);
                                    bw.Write(week.Zonestat3);
                                    bw.Write(week.Zonestat4);
                                    bw.Write(week.Zonestat5);
                                    bw.Write(week.Zonestat6);
                                    bw.Write(week.Zonestat7);
                                    bw.Write(week.Zonestat8);
                                    bw.Write(week.Zonestat9);
                                    bw.Write(week.Zonestat10);
                                    bw.Write(week.Zonestat11);
                                    bw.Write(week.Zonestat12);
                                }
                            }
                            catch (Exception e)
                            {
                                Log.write(TLog.Warning, "WriteElementWeekly " + e);
                            }

                            SC_PlayerStatsResponse<Zone> response = new SC_PlayerStatsResponse<Zone>();

                            response.player = pkt.player;
                            response.type = CS_PlayerStatsRequest<Zone>.ChartType.ScoreWeekly;
                            response.columns = "Top100 Weekly Score,Name,Squad";
                            response.data = stream.ToArray();

                            zone._client.sendReliable(response, 1);
                        }
                        break;

                    case CS_PlayerStatsRequest<Zone>.ChartType.ScoreMonthly:
                        {
                            DateTime now = DateTime.Today;
                            if (((int)now.Day - 1) > 1)
                                now = now.AddDays(-(((int)now.Day) - 1));

                            //Get the top100 stats sorted by points
                            //For this month
                            var monthly = (from mt in db.StatsMonthlies
                                           where mt.ZoneNavigation == zone._zone && mt.Date >= now
                                           orderby mt.AssistPoints + mt.BonusPoints + mt.KillPoints descending
                                           select mt).Take(100).ToList();

                            //Are they requesting a specific date?
                            if (pkt.options != "")
                            {
                                //Player wants to see last month's date
                                if (pkt.options.Equals("-1"))
                                {
                                    DateTime today = now;
                                    now = now.AddMonths(-1);
                                    monthly = (from mt in db.StatsMonthlies
                                               where mt.ZoneNavigation == zone._zone && mt.Date >= now && mt.Date < today
                                               orderby mt.AssistPoints + mt.BonusPoints + mt.KillPoints descending
                                               select mt).Take(100).ToList();
                                }
                                else //Specific date
                                {
                                    string[] args = pkt.options.Split('-');
                                    string final = string.Join("/", args);
                                    //Since the client only gives month/year, lets start from day 1
                                    final = String.Format("{0}/01", final);
                                    try
                                    {
                                        now = DateTime.Parse(final, System.Threading.Thread.CurrentThread.CurrentCulture.DateTimeFormat);
                                    }
                                    catch (FormatException)
                                    {
                                        //Wrong format, use last month as default
                                        now = (now.AddMonths(-1));
                                    }
                                    DateTime add = now.AddMonths(1);

                                    monthly = (from mt in db.StatsMonthlies
                                               where mt.ZoneNavigation == zone._zone && mt.Date >= now && mt.Date < add
                                               orderby mt.AssistPoints + mt.BonusPoints + mt.KillPoints descending
                                               select mt).Take(100).ToList();
                                }
                            }

                            MemoryStream stream = new MemoryStream();
                            try
                            {
                                foreach (StatsMonthly month in monthly)
                                {
                                    BinaryWriter bw = new BinaryWriter(stream);
                                    bw.Write(month.PlayerNavigation.AliasNavigation.Name.ToCharArray());
                                    bw.Write((byte)0);

                                    Squad squad = month.PlayerNavigation.SquadNavigation;
                                    string squadname = "";
                                    if (squad != null)
                                        squadname = squad.Name;

                                    bw.Write(squadname.ToCharArray());
                                    bw.Write((byte)0);

                                    bw.Write((short)2);
                                    bw.Write(month.VehicleDeaths);
                                    bw.Write(month.VehicleKills);
                                    bw.Write(month.KillPoints);
                                    bw.Write(month.DeathPoints);
                                    bw.Write(month.AssistPoints);
                                    bw.Write(month.BonusPoints);
                                    bw.Write(month.Kills);
                                    bw.Write(month.Deaths);
                                    bw.Write((int)0);
                                    bw.Write(month.PlaySeconds);
                                    bw.Write(month.Zonestat1);
                                    bw.Write(month.Zonestat2);
                                    bw.Write(month.Zonestat3);
                                    bw.Write(month.Zonestat4);
                                    bw.Write(month.Zonestat5);
                                    bw.Write(month.Zonestat6);
                                    bw.Write(month.Zonestat7);
                                    bw.Write(month.Zonestat8);
                                    bw.Write(month.Zonestat9);
                                    bw.Write(month.Zonestat10);
                                    bw.Write(month.Zonestat11);
                                    bw.Write(month.Zonestat12);
                                }
                            }
                            catch (Exception e)
                            {
                                Log.write(TLog.Warning, "WriteElementMonthly " + e);
                            }

                            SC_PlayerStatsResponse<Zone> response = new SC_PlayerStatsResponse<Zone>();

                            response.player = pkt.player;
                            response.type = CS_PlayerStatsRequest<Zone>.ChartType.ScoreMonthly;
                            response.columns = "Top100 Monthly Score,Name,Squad";
                            response.data = stream.ToArray();

                            zone._client.sendReliable(response, 1);
                        }
                        break;

                    case CS_PlayerStatsRequest<Zone>.ChartType.ScoreYearly:
                        {
                            DateTime now = DateTime.Today;
                            if (((int)now.Month) > 1)
                                now = now.AddMonths(-((int)DateTime.Now.Month));

                            //Get the top100 stats sorted by points
                            var yearly = (from yt in db.StatsYearlies
                                          where yt.ZoneNavigation == zone._zone && yt.Date >= now
                                          orderby yt.AssistPoints + yt.BonusPoints + yt.KillPoints descending
                                          select yt).Take(100).ToList();

                            //Are they requesting a specific date?
                            if (pkt.options != "")
                            {
                                //Player wants to see last years date
                                if (pkt.options.Equals("-1"))
                                {
                                    now = now.AddYears(-1);
                                    yearly = (from yt in db.StatsYearlies
                                              where yt.ZoneNavigation == zone._zone && yt.Date >= now
                                              orderby yt.AssistPoints + yt.BonusPoints + yt.KillPoints descending
                                              select yt).Take(100).ToList();
                                }
                                else //Specific date
                                {
                                    //Since the client only gives the year, lets start from jan 1st
                                    string final = String.Format("{0}/01/01", pkt.options);
                                    try
                                    {
                                        now = DateTime.Parse(final, System.Threading.Thread.CurrentThread.CurrentCulture.DateTimeFormat);
                                    }
                                    catch (FormatException)
                                    {
                                        //Wrong format, use last year as default
                                        now = (now.AddYears(-1));
                                    }
                                    DateTime add = now.AddYears(1);

                                    yearly = (from yt in db.StatsYearlies
                                              where yt.ZoneNavigation == zone._zone && yt.Date >= now && yt.Date <= add
                                              orderby yt.AssistPoints + yt.BonusPoints + yt.KillPoints descending
                                              select yt).Take(100).ToList();
                                }
                            }

                            MemoryStream stream = new MemoryStream();
                            try
                            {
                                foreach (StatsYearly year in yearly)
                                {
                                    BinaryWriter bw = new BinaryWriter(stream);
                                    bw.Write(year.PlayerNavigation.AliasNavigation.Name.ToCharArray());
                                    bw.Write((byte)0);

                                    Squad squad = year.PlayerNavigation.SquadNavigation;
                                    string squadname = "";
                                    if (squad != null)
                                        squadname = squad.Name;

                                    bw.Write(squadname.ToCharArray());
                                    bw.Write((byte)0);

                                    bw.Write((short)2);
                                    bw.Write(year.VehicleDeaths);
                                    bw.Write(year.VehicleKills);
                                    bw.Write(year.KillPoints);
                                    bw.Write(year.DeathPoints);
                                    bw.Write(year.AssistPoints);
                                    bw.Write(year.BonusPoints);
                                    bw.Write(year.Kills);
                                    bw.Write(year.Deaths);
                                    bw.Write((int)0);
                                    bw.Write(year.PlaySeconds);
                                    bw.Write(year.Zonestat1);
                                    bw.Write(year.Zonestat2);
                                    bw.Write(year.Zonestat3);
                                    bw.Write(year.Zonestat4);
                                    bw.Write(year.Zonestat5);
                                    bw.Write(year.Zonestat6);
                                    bw.Write(year.Zonestat7);
                                    bw.Write(year.Zonestat8);
                                    bw.Write(year.Zonestat9);
                                    bw.Write(year.Zonestat10);
                                    bw.Write(year.Zonestat11);
                                    bw.Write(year.Zonestat12);
                                }
                            }
                            catch (Exception e)
                            {
                                Log.write(TLog.Warning, "WriteElementYear " + e);
                            }

                            SC_PlayerStatsResponse<Zone> response = new SC_PlayerStatsResponse<Zone>();

                            response.player = pkt.player;
                            response.type = CS_PlayerStatsRequest<Zone>.ChartType.ScoreYearly;
                            response.columns = "Top100 Yearly Score,Name,Squad";
                            response.data = stream.ToArray();

                            zone._client.sendReliable(response, 1);
                        }
                        break;

                    case CS_PlayerStatsRequest<Zone>.ChartType.ScoreHistoryDaily:
                        {
                            Alias getAlias = db.Aliases.FirstOrDefault(a => a.Name == pkt.options);
                            Player getPlayer = db.Players.FirstOrDefault(p => p.AliasNavigation == getAlias && p.ZoneId == zone._zone.ZoneId);
                            if (getPlayer == null)
                                return;

                            //Lets give them a year's worth
                            DateTime now = DateTime.Today;
                            if (((int)now.DayOfYear - 1) > 1)
                                now = now.AddDays(-(((int)now.DayOfYear) - 1));

                            DateTime today = DateTime.Today;
                            var daily = (from dt in db.StatsDailies
                                         where dt.ZoneNavigation == zone._zone && dt.Date >= now && dt.Date < today
                                         orderby dt.Date descending
                                         select dt).ToList();

                            MemoryStream stream = new MemoryStream();
                            try
                            {
                                foreach (StatsDaily day in daily)
                                {
                                    BinaryWriter bw = new BinaryWriter(stream);
                                    bw.Write(day.PlayerNavigation.AliasNavigation.Name.ToCharArray());
                                    bw.Write((byte)0);

                                    Squad squad = day.PlayerNavigation.SquadNavigation;
                                    string squadname = "";
                                    if (squad != null)
                                        squadname = squad.Name;

                                    bw.Write(squadname.ToCharArray());
                                    bw.Write((byte)0);

                                    bw.Write((short)2);
                                    bw.Write(day.VehicleDeaths);
                                    bw.Write(day.VehicleKills);
                                    bw.Write(day.KillPoints);
                                    bw.Write(day.DeathPoints);
                                    bw.Write(day.AssistPoints);
                                    bw.Write(day.BonusPoints);
                                    bw.Write(day.Kills);
                                    bw.Write(day.Deaths);
                                    bw.Write((int)0);
                                    bw.Write(day.PlaySeconds);
                                    bw.Write(day.Zonestat1);
                                    bw.Write(day.Zonestat2);
                                    bw.Write(day.Zonestat3);
                                    bw.Write(day.Zonestat4);
                                    bw.Write(day.Zonestat5);
                                    bw.Write(day.Zonestat6);
                                    bw.Write(day.Zonestat7);
                                    bw.Write(day.Zonestat8);
                                    bw.Write(day.Zonestat9);
                                    bw.Write(day.Zonestat10);
                                    bw.Write(day.Zonestat11);
                                    bw.Write(day.Zonestat12);
                                }
                            }
                            catch (Exception e)
                            {
                                Log.write(TLog.Warning, "WriteElementHistoryDailly " + e);
                            }

                            SC_PlayerStatsResponse<Zone> response = new SC_PlayerStatsResponse<Zone>();

                            response.player = pkt.player;
                            response.type = CS_PlayerStatsRequest<Zone>.ChartType.ScoreHistoryDaily;
                            response.columns = "ScoreHistory Daily Score,Name,Squad";
                            response.data = stream.ToArray();

                            zone._client.sendReliable(response, 1);
                        }
                        break;

                    case CS_PlayerStatsRequest<Zone>.ChartType.ScoreHistoryWeekly:
                        {
                            Alias getAlias = db.Aliases.FirstOrDefault(a => a.Name == pkt.options);
                            Player getPlayer = db.Players.FirstOrDefault(p => p.AliasNavigation == getAlias && p.ZoneId == zone._zone.ZoneId);
                            if (getPlayer == null)
                                return;

                            //Lets give them a year's worth
                            DateTime now = DateTime.Today;
                            if (((int)now.DayOfWeek) > 0)
                                now = now.AddDays(-(((int)now.DayOfWeek) - 1));
                            DateTime today = now;
                            now = now.AddMonths(-((int)now.Month - 1));

                            var weekly = (from wt in db.StatsWeeklies
                                          where wt.ZoneNavigation == zone._zone && wt.Date >= now && wt.Date < today
                                          orderby wt.Date descending
                                          select wt).ToList();

                            MemoryStream stream = new MemoryStream();
                            try
                            {
                                foreach (StatsWeekly week in weekly)
                                {
                                    BinaryWriter bw = new BinaryWriter(stream);
                                    bw.Write(week.PlayerNavigation.AliasNavigation.Name.ToCharArray());
                                    bw.Write((byte)0);

                                    Squad squad = week.PlayerNavigation.SquadNavigation;
                                    string squadname = "";
                                    if (squad != null)
                                        squadname = squad.Name;

                                    bw.Write(squadname.ToCharArray());
                                    bw.Write((byte)0);

                                    bw.Write((short)2);
                                    bw.Write(week.VehicleDeaths);
                                    bw.Write(week.VehicleKills);
                                    bw.Write(week.KillPoints);
                                    bw.Write(week.DeathPoints);
                                    bw.Write(week.AssistPoints);
                                    bw.Write(week.BonusPoints);
                                    bw.Write(week.Kills);
                                    bw.Write(week.Deaths);
                                    bw.Write((int)0);
                                    bw.Write(week.PlaySeconds);
                                    bw.Write(week.Zonestat1);
                                    bw.Write(week.Zonestat2);
                                    bw.Write(week.Zonestat3);
                                    bw.Write(week.Zonestat4);
                                    bw.Write(week.Zonestat5);
                                    bw.Write(week.Zonestat6);
                                    bw.Write(week.Zonestat7);
                                    bw.Write(week.Zonestat8);
                                    bw.Write(week.Zonestat9);
                                    bw.Write(week.Zonestat10);
                                    bw.Write(week.Zonestat11);
                                    bw.Write(week.Zonestat12);
                                }
                            }
                            catch (Exception e)
                            {
                                Log.write(TLog.Warning, "WriteElementHistoryWeekly " + e);
                            }

                            SC_PlayerStatsResponse<Zone> response = new SC_PlayerStatsResponse<Zone>();

                            response.player = pkt.player;
                            response.type = CS_PlayerStatsRequest<Zone>.ChartType.ScoreHistoryWeekly;
                            response.columns = "ScoreHistory Weekly Score,Name,Squad";
                            response.data = stream.ToArray();

                            zone._client.sendReliable(response, 1);
                        }
                        break;

                    case CS_PlayerStatsRequest<Zone>.ChartType.ScoreHistoryMonthly:
                        {
                            Alias getAlias = db.Aliases.FirstOrDefault(a => a.Name == pkt.options);
                            Player getPlayer = db.Players.FirstOrDefault(p => p.AliasNavigation == getAlias && p.ZoneId == zone._zone.ZoneId);
                            if (getPlayer == null)
                                return;

                            //Lets give them a year's worth
                            DateTime now = DateTime.Today;
                            if (((int)now.Day - 1) > 1)
                                now = now.AddDays(-(((int)now.Day) - 1));
                            DateTime today = now;
                            now = now.AddMonths(-((int)now.Month - 1));

                            var monthly = (from mt in db.StatsMonthlies
                                           where mt.ZoneNavigation == zone._zone && mt.Date >= now && mt.Date < today
                                           orderby mt.Date descending
                                           select mt).ToList();

                            MemoryStream stream = new MemoryStream();
                            try
                            {
                                foreach (StatsMonthly month in monthly)
                                {
                                    BinaryWriter bw = new BinaryWriter(stream);
                                    bw.Write(month.PlayerNavigation.AliasNavigation.Name.ToCharArray());
                                    bw.Write((byte)0);

                                    Squad squad = month.PlayerNavigation.SquadNavigation;
                                    string squadname = "";
                                    if (squad != null)
                                        squadname = squad.Name;

                                    bw.Write(squadname.ToCharArray());
                                    bw.Write((byte)0);

                                    bw.Write((short)2);
                                    bw.Write(month.VehicleDeaths);
                                    bw.Write(month.VehicleKills);
                                    bw.Write(month.KillPoints);
                                    bw.Write(month.DeathPoints);
                                    bw.Write(month.AssistPoints);
                                    bw.Write(month.BonusPoints);
                                    bw.Write(month.Kills);
                                    bw.Write(month.Deaths);
                                    bw.Write((int)0);
                                    bw.Write(month.PlaySeconds);
                                    bw.Write(month.Zonestat1);
                                    bw.Write(month.Zonestat2);
                                    bw.Write(month.Zonestat3);
                                    bw.Write(month.Zonestat4);
                                    bw.Write(month.Zonestat5);
                                    bw.Write(month.Zonestat6);
                                    bw.Write(month.Zonestat7);
                                    bw.Write(month.Zonestat8);
                                    bw.Write(month.Zonestat9);
                                    bw.Write(month.Zonestat10);
                                    bw.Write(month.Zonestat11);
                                    bw.Write(month.Zonestat12);
                                }
                            }
                            catch (Exception e)
                            {
                                Log.write(TLog.Warning, "WriteElementHistoryMonthly " + e);
                            }

                            SC_PlayerStatsResponse<Zone> response = new SC_PlayerStatsResponse<Zone>();

                            response.player = pkt.player;
                            response.type = CS_PlayerStatsRequest<Zone>.ChartType.ScoreHistoryMonthly;
                            response.columns = "ScoreHistory Monthly Score,Name,Squad";
                            response.data = stream.ToArray();

                            zone._client.sendReliable(response, 1);
                        }
                        break;

                    case CS_PlayerStatsRequest<Zone>.ChartType.ScoreHistoryYearly:
                        {
                            Alias getAlias = db.Aliases.FirstOrDefault(a => a.Name == pkt.options);
                            Player getPlayer = db.Players.FirstOrDefault(p => p.AliasNavigation == getAlias && p.ZoneId == zone._zone.ZoneId);
                            if (getPlayer == null)
                                return;

                            //Lets give them a year's worth
                            DateTime now = DateTime.Today;
                            if (((int)now.Day - 1) > 1)
                                now = now.AddDays(-(((int)now.Day) - 1));
                            DateTime today = now;
                            now = now.AddMonths(-((int)now.Month - 1));

                            var yearly = (from yt in db.StatsYearlies
                                          where yt.ZoneNavigation == zone._zone && yt.Date >= now && yt.Date < today
                                          orderby yt.Date descending
                                          select yt).ToList();

                            MemoryStream stream = new MemoryStream();
                            try
                            {
                                foreach (StatsYearly year in yearly)
                                {
                                    BinaryWriter bw = new BinaryWriter(stream);
                                    bw.Write(year.PlayerNavigation.AliasNavigation.Name.ToCharArray());
                                    bw.Write((byte)0);

                                    var squad = year!.PlayerNavigation!.SquadNavigation;
                                    string squadname = "";
                                    if (squad != null)
                                        squadname = squad.Name;

                                    bw.Write(squadname.ToCharArray());
                                    bw.Write((byte)0);

                                    bw.Write((short)2);
                                    bw.Write(year.VehicleDeaths);
                                    bw.Write(year.VehicleKills);
                                    bw.Write(year.KillPoints);
                                    bw.Write(year.DeathPoints);
                                    bw.Write(year.AssistPoints);
                                    bw.Write(year.BonusPoints);
                                    bw.Write(year.Kills);
                                    bw.Write(year.Deaths);
                                    bw.Write((int)0);
                                    bw.Write(year.PlaySeconds);
                                    bw.Write(year.Zonestat1);
                                    bw.Write(year.Zonestat2);
                                    bw.Write(year.Zonestat3);
                                    bw.Write(year.Zonestat4);
                                    bw.Write(year.Zonestat5);
                                    bw.Write(year.Zonestat6);
                                    bw.Write(year.Zonestat7);
                                    bw.Write(year.Zonestat8);
                                    bw.Write(year.Zonestat9);
                                    bw.Write(year.Zonestat10);
                                    bw.Write(year.Zonestat11);
                                    bw.Write(year.Zonestat12);
                                }
                            }
                            catch (Exception e)
                            {
                                Log.write(TLog.Warning, "WriteElementHistoryYearly " + e);
                            }

                            SC_PlayerStatsResponse<Zone> response = new SC_PlayerStatsResponse<Zone>();

                            response.player = pkt.player;
                            response.type = CS_PlayerStatsRequest<Zone>.ChartType.ScoreHistoryYearly;
                            response.columns = "ScoreHistory Yearly Score,Name,Squad";
                            response.data = stream.ToArray();

                            zone._client.sendReliable(response, 1);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Handles a player update request
        /// </summary>
        static public void Handle_CS_SquadMatch(CS_SquadMatch<Zone> pkt, Zone zone)
        {
            throw new Exception("This should not even be called.");
            //using (DataContext db = zone._server.getContext())
            //{
            //    var ids = new[] { pkt.winner, pkt.loser };

            //    var dbSquads = db.Squads.Where(s => ids.Contains(s.SquadId)).ToList();

            //    var winner = dbSquads.FirstOrDefault(t => t.SquadId == pkt.winner);
            //    var loser = dbSquads.FirstOrDefault(t => t.SquadId == pkt.loser);

            //    //Try to trick me, I dare you, do it.
            //    if (winner == null || loser == null)
            //        return;

            //    var dbSquadStats = db.Squadstats.Where(s => ids.Contains(s.SquadStatId)).ToList();

            //    var wStats = dbSquadStats.FirstOrDefault(s => s.SquadId == winner.SquadId);
            //    var lStats = dbSquadStats.FirstOrDefault(s => s.SquadId == loser.SquadId);

            //    //Again, try it!
            //    if (wStats == null || lStats == null)
            //        return;

            //    //Update our winners!
            //    wStats.Kills += pkt.wStats.kills;
            //    wStats.Deaths += pkt.wStats.deaths;
            //    wStats.Points += pkt.wStats.points;
            //    wStats.Wins++;

            //    //Update our losers!
            //    lStats.Kills += pkt.wStats.kills;
            //    lStats.Deaths += pkt.wStats.deaths;
            //    lStats.Points += pkt.wStats.points;
            //    lStats.Losses++; //Sad trombone.....

            //    //Grab our associated match.
            //    var match = db.Squadmatches.FirstOrDefault(m => m.Squad1 == winner.SquadId | m.Squad2 == winner.SquadId | m.Squad1 == loser.SquadId | m.Squad2 == loser.SquadId);

            //    if (match == null)
            //    {
            //        // TODO: Log, this is a critical error.

            //        return;
            //    }

            //    //Update it
            //    match.Winner = pkt.winner;
            //    match.Loser = pkt.loser;
            //    match.DateEnd = DateTime.Now;

            //    //Submit
            //    db.SaveChanges();
            //}
        }

        /// <summary>
        /// Registers all handlers
        /// </summary>
        [RegistryFunc]
        static public void Register()
        {
            CS_PlayerStatsRequest<Zone>.Handlers += Handle_CS_PlayerStatsRequest;
            // CS_StatsUpdate<Zone>.Handlers += Handle_CS_StatsUpdate;
            CS_SquadMatch<Zone>.Handlers += Handle_CS_SquadMatch;
        }
    }
}