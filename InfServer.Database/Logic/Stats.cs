using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using InfServer.Protocol;
using InfServer.Data;

namespace InfServer.Logic
{	// Logic_Stats Class
	/// Handles statistics functionality
	///////////////////////////////////////////////////////
	class Logic_Stats
	{
		/// <summary>
		/// Writes a scorechart element to a memory stream
		/// </summary>
		static private void writeElementToBuffer(Data.DB.stats stat, MemoryStream stream)
		{
            try
            {
                Data.DB.player player = stat.players.First(s => s.stats1.id == stat.id);
                BinaryWriter bw = new BinaryWriter(stream);

                //bw.Write(stat.players[0].alias1.name.ToCharArray());
                bw.Write(player.alias1.name.ToCharArray());
                bw.Write((byte)0);

                //Data.DB.squad squad = stat.players[0].squad1;
                Data.DB.squad squad = player.squad1;
                string squadname = "";
                if (squad != null)
                    squadname = squad.name;

                bw.Write(squadname.ToCharArray());
                bw.Write((byte)0);

                bw.Write((short)2);
                bw.Write(stat.vehicleDeaths);
                bw.Write(stat.vehicleKills);
                bw.Write(stat.killPoints);
                bw.Write(stat.deathPoints);
                bw.Write(stat.assistPoints);
                bw.Write(stat.bonusPoints);
                bw.Write(stat.kills);
                bw.Write(stat.deaths);
                bw.Write((int)0);
                bw.Write(stat.playSeconds);
                bw.Write(stat.zonestat1);
                bw.Write(stat.zonestat2);
                bw.Write(stat.zonestat3);
                bw.Write(stat.zonestat4);
                bw.Write(stat.zonestat5);
                bw.Write(stat.zonestat6);
                bw.Write(stat.zonestat7);
                bw.Write(stat.zonestat8);
                bw.Write(stat.zonestat9);
                bw.Write(stat.zonestat10);
                bw.Write(stat.zonestat11);
                bw.Write(stat.zonestat12);
            }
            
           catch (Exception e)
           {
                Log.write(TLog.Warning, "WriteElementToBuffer " + e);
           }
		}

		/// <summary>
		/// Handles a player stat request
		/// </summary>
		static public void Handle_CS_PlayerStatsRequest(CS_PlayerStatsRequest<Zone> pkt, Zone zone)
		{	//Attempt to find the player in question
			Zone.Player player = zone.getPlayer(pkt.player.id);
			if (player == null)
			{	//Make a note
				Log.write(TLog.Warning, "Ignoring player stats request for #{0}, not present in zone mirror.", pkt.player.id);
				return;
			}

			using (InfantryDataContext db = zone._server.getContext())
			{	//What sort of request are we dealing with?
				switch (pkt.type)
				{
					case CS_PlayerStatsRequest<Zone>.ChartType.ScoreLifetime:
						{	//Get the top100 stats sorted by points
							var stats = (from st in db.stats
										 where st.zone1 == zone._zone
										 orderby st.assistPoints + st.bonusPoints + st.killPoints descending
										 select st).Take(100);
							MemoryStream stream = new MemoryStream();
                            
                            foreach (Data.DB.stats lifetime in stats)
                                writeElementToBuffer(lifetime, stream);
                            
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
                            var daily = (from dt in db.statsDailies
                                         where dt.zone1 == zone._zone && dt.date >= now
                                         orderby dt.assistPoints + dt.bonusPoints + dt.killPoints descending
                                         select dt).Take(100);

                            //Are they requesting a specific date?
                            if (pkt.options != "")
                            {
                                //Player wants to see yesterday's date
                                if (pkt.options.Equals("-1"))
                                {
                                    DateTime today = now;
                                    now = now.AddDays(-1);
                                    daily = (from dt in db.statsDailies
                                             where dt.zone1 == zone._zone && dt.date >= now && dt.date < today
                                             orderby dt.assistPoints + dt.bonusPoints + dt.killPoints descending
                                             select dt).Take(100);
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

                                    daily = (from dt in db.statsDailies
                                             where dt.zone1 == zone._zone && dt.date >= now && dt.date < add
                                             orderby dt.assistPoints + dt.bonusPoints + dt.killPoints descending
                                             select dt).Take(100);
                                }
                            }

                            MemoryStream stream = new MemoryStream();
                            try
                            {
                                foreach (Data.DB.statsDaily day in daily)
                                {
                                    BinaryWriter bw = new BinaryWriter(stream);
                                    bw.Write(day.players[0].alias1.name.ToCharArray());
                                    bw.Write((byte)0);

                                    Data.DB.squad squad = day.players[0].squad1;
                                    string squadname = "";
                                    if (squad != null)
                                        squadname = squad.name;

                                    bw.Write(squadname.ToCharArray());
                                    bw.Write((byte)0);

                                    bw.Write((short)2);
                                    bw.Write(day.vehicleDeaths);
                                    bw.Write(day.vehicleKills);
                                    bw.Write(day.killPoints);
                                    bw.Write(day.deathPoints);
                                    bw.Write(day.assistPoints);
                                    bw.Write(day.bonusPoints);
                                    bw.Write(day.kills);
                                    bw.Write(day.deaths);
                                    bw.Write((int)0);
                                    bw.Write(day.playSeconds);
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
                            if ( ((int)now.DayOfWeek) > 0)
                                now = now.AddDays(-((int)now.DayOfWeek));

                            //Get the top100 stats sorted by points
                            //For this week
                            var weekly = (from wt in db.statsWeeklies
                                         where wt.zone1 == zone._zone && wt.date >= now
                                         orderby wt.assistPoints + wt.bonusPoints + wt.killPoints descending
                                         select wt).Take(100);

                            //Are they requesting a specific date?
                            if (pkt.options != "")
                            {
                                //Player wants to see last week's date
                                if (pkt.options.Equals("-1"))
                                {
                                    DateTime today = now;
                                    now = now.AddDays(-7);
                                    weekly = (from wt in db.statsWeeklies
                                             where wt.zone1 == zone._zone && wt.date >= now && wt.date < today
                                             orderby wt.assistPoints + wt.bonusPoints + wt.killPoints descending
                                             select wt).Take(100);
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

                                    weekly = (from wt in db.statsWeeklies
                                             where wt.zone1 == zone._zone && wt.date >= now && wt.date < add
                                             orderby wt.assistPoints + wt.bonusPoints + wt.killPoints descending
                                             select wt).Take(100);
                                }
                            }

                            MemoryStream stream = new MemoryStream();
                            try
                            {
                                foreach (Data.DB.statsWeekly week in weekly)
                                {
                                    BinaryWriter bw = new BinaryWriter(stream);
                                    bw.Write(week.players[0].alias1.name.ToCharArray());
                                    bw.Write((byte)0);

                                    Data.DB.squad squad = week.players[0].squad1;
                                    string squadname = "";
                                    if (squad != null)
                                        squadname = squad.name;

                                    bw.Write(squadname.ToCharArray());
                                    bw.Write((byte)0);

                                    bw.Write((short)2);
                                    bw.Write(week.vehicleDeaths);
                                    bw.Write(week.vehicleKills);
                                    bw.Write(week.killPoints);
                                    bw.Write(week.deathPoints);
                                    bw.Write(week.assistPoints);
                                    bw.Write(week.bonusPoints);
                                    bw.Write(week.kills);
                                    bw.Write(week.deaths);
                                    bw.Write((int)0);
                                    bw.Write(week.playSeconds);
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
                            var monthly = (from mt in db.statsMonthlies
                                         where mt.zone1 == zone._zone && mt.date >= now
                                         orderby mt.assistPoints + mt.bonusPoints + mt.killPoints descending
                                         select mt).Take(100);

                            //Are they requesting a specific date?
                            if (pkt.options != "")
                            {
                                //Player wants to see last month's date
                                if (pkt.options.Equals("-1"))
                                {
                                    DateTime today = now;
                                    now = now.AddMonths(-1);
                                    monthly = (from mt in db.statsMonthlies
                                              where mt.zone1 == zone._zone && mt.date >= now && mt.date < today
                                              orderby mt.assistPoints + mt.bonusPoints + mt.killPoints descending
                                              select mt).Take(100);
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

                                    monthly = (from mt in db.statsMonthlies
                                              where mt.zone1 == zone._zone && mt.date >= now && mt.date < add
                                              orderby mt.assistPoints + mt.bonusPoints + mt.killPoints descending
                                              select mt).Take(100);
                                }
                            }

                            MemoryStream stream = new MemoryStream();
                            try
                            {
                                foreach (Data.DB.statsMonthly month in monthly)
                                {
                                    BinaryWriter bw = new BinaryWriter(stream);
                                    bw.Write(month.players[0].alias1.name.ToCharArray());
                                    bw.Write((byte)0);

                                    Data.DB.squad squad = month.players[0].squad1;
                                    string squadname = "";
                                    if (squad != null)
                                        squadname = squad.name;

                                    bw.Write(squadname.ToCharArray());
                                    bw.Write((byte)0);

                                    bw.Write((short)2);
                                    bw.Write(month.vehicleDeaths);
                                    bw.Write(month.vehicleKills);
                                    bw.Write(month.killPoints);
                                    bw.Write(month.deathPoints);
                                    bw.Write(month.assistPoints);
                                    bw.Write(month.bonusPoints);
                                    bw.Write(month.kills);
                                    bw.Write(month.deaths);
                                    bw.Write((int)0);
                                    bw.Write(month.playSeconds);
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
                            var yearly = (from yt in db.statsYearlies
                                         where yt.zone1 == zone._zone && yt.date >= now
                                         orderby yt.assistPoints + yt.bonusPoints + yt.killPoints descending
                                         select yt).Take(100);

                            //Are they requesting a specific date?
                            if (pkt.options != "")
                            {
                                //Player wants to see last years date
                                if (pkt.options.Equals("-1"))
                                {
                                    now = now.AddYears(-1);
                                    yearly = (from yt in db.statsYearlies
                                               where yt.zone1 == zone._zone && yt.date >= now
                                               orderby yt.assistPoints + yt.bonusPoints + yt.killPoints descending
                                               select yt).Take(100);
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

                                    yearly = (from yt in db.statsYearlies
                                               where yt.zone1 == zone._zone && yt.date >= now && yt.date <= add
                                               orderby yt.assistPoints + yt.bonusPoints + yt.killPoints descending
                                               select yt).Take(100);
                                }
                            }

                            MemoryStream stream = new MemoryStream();
                            try
                            {
                                foreach (Data.DB.statsYearly year in yearly)
                                {
                                    BinaryWriter bw = new BinaryWriter(stream);
                                    bw.Write(year.players[0].alias1.name.ToCharArray());
                                    bw.Write((byte)0);

                                    Data.DB.squad squad = year.players[0].squad1;
                                    string squadname = "";
                                    if (squad != null)
                                        squadname = squad.name;

                                    bw.Write(squadname.ToCharArray());
                                    bw.Write((byte)0);

                                    bw.Write((short)2);
                                    bw.Write(year.vehicleDeaths);
                                    bw.Write(year.vehicleKills);
                                    bw.Write(year.killPoints);
                                    bw.Write(year.deathPoints);
                                    bw.Write(year.assistPoints);
                                    bw.Write(year.bonusPoints);
                                    bw.Write(year.kills);
                                    bw.Write(year.deaths);
                                    bw.Write((int)0);
                                    bw.Write(year.playSeconds);
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
                            Data.DB.alias getAlias = db.alias.FirstOrDefault(a => a.name.Equals(pkt.options));
                            Data.DB.player getPlayer = db.players.FirstOrDefault(p => p.alias1 == getAlias && p.zone == zone._zone.id);
                            if (getPlayer == null)
                                return;

                            //Lets give them a year's worth
                            DateTime now = DateTime.Today;
                            if (((int)now.DayOfYear - 1) > 1)
                                now = now.AddDays(-(((int)now.DayOfYear) - 1));

                            DateTime today = DateTime.Today;
                            var daily = (from dt in db.statsDailies
                                     where dt.zone1 == zone._zone && dt.date >= now && dt.date < today
                                     orderby dt.date descending
                                     select dt);

                            MemoryStream stream = new MemoryStream();
                            try
                            {
                                foreach (Data.DB.statsDaily day in daily)
                                {
                                    BinaryWriter bw = new BinaryWriter(stream);
                                    bw.Write(day.players[0].alias1.name.ToCharArray());
                                    bw.Write((byte)0);

                                    Data.DB.squad squad = day.players[0].squad1;
                                    string squadname = "";
                                    if (squad != null)
                                        squadname = squad.name;

                                    bw.Write(squadname.ToCharArray());
                                    bw.Write((byte)0);

                                    bw.Write((short)2);
                                    bw.Write(day.vehicleDeaths);
                                    bw.Write(day.vehicleKills);
                                    bw.Write(day.killPoints);
                                    bw.Write(day.deathPoints);
                                    bw.Write(day.assistPoints);
                                    bw.Write(day.bonusPoints);
                                    bw.Write(day.kills);
                                    bw.Write(day.deaths);
                                    bw.Write((int)0);
                                    bw.Write(day.playSeconds);
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
                            Data.DB.alias getAlias = db.alias.FirstOrDefault(a => a.name.Equals(pkt.options));
                            Data.DB.player getPlayer = db.players.FirstOrDefault(p => p.alias1 == getAlias && p.zone == zone._zone.id);
                            if (getPlayer == null)
                                return;

                            //Lets give them a year's worth
                            DateTime now = DateTime.Today;
                            if (((int)now.DayOfWeek) > 0)
                                now = now.AddDays(-(((int)now.DayOfWeek) - 1));
                            DateTime today = now;
                            now = now.AddMonths(-((int)now.Month - 1));

                            var weekly = (from wt in db.statsWeeklies
                                      where wt.zone1 == zone._zone && wt.date >= now && wt.date < today
                                      orderby wt.date descending
                                      select wt);

                            MemoryStream stream = new MemoryStream();
                            try
                            {
                                foreach (Data.DB.statsWeekly week in weekly)
                                {
                                    BinaryWriter bw = new BinaryWriter(stream);
                                    bw.Write(week.players[0].alias1.name.ToCharArray());
                                    bw.Write((byte)0);

                                    Data.DB.squad squad = week.players[0].squad1;
                                    string squadname = "";
                                    if (squad != null)
                                        squadname = squad.name;

                                    bw.Write(squadname.ToCharArray());
                                    bw.Write((byte)0);

                                    bw.Write((short)2);
                                    bw.Write(week.vehicleDeaths);
                                    bw.Write(week.vehicleKills);
                                    bw.Write(week.killPoints);
                                    bw.Write(week.deathPoints);
                                    bw.Write(week.assistPoints);
                                    bw.Write(week.bonusPoints);
                                    bw.Write(week.kills);
                                    bw.Write(week.deaths);
                                    bw.Write((int)0);
                                    bw.Write(week.playSeconds);
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
                            Data.DB.alias getAlias = db.alias.FirstOrDefault(a => a.name.Equals(pkt.options));
                            Data.DB.player getPlayer = db.players.FirstOrDefault(p => p.alias1 == getAlias && p.zone == zone._zone.id);
                            if (getPlayer == null)
                                return;

                            //Lets give them a year's worth
                            DateTime now = DateTime.Today;
                            if (((int)now.Day - 1) > 1)
                                now = now.AddDays(-(((int)now.Day) - 1));
                            DateTime today = now;
                            now = now.AddMonths(-((int)now.Month - 1));

                            var monthly = (from mt in db.statsMonthlies
                                       where mt.zone1 == zone._zone && mt.date >= now && mt.date < today
                                       orderby mt.date descending
                                       select mt);

                            MemoryStream stream = new MemoryStream();
                            try
                            {
                                foreach (Data.DB.statsMonthly month in monthly)
                                {
                                    BinaryWriter bw = new BinaryWriter(stream);
                                    bw.Write(month.players[0].alias1.name.ToCharArray());
                                    bw.Write((byte)0);

                                    Data.DB.squad squad = month.players[0].squad1;
                                    string squadname = "";
                                    if (squad != null)
                                        squadname = squad.name;

                                    bw.Write(squadname.ToCharArray());
                                    bw.Write((byte)0);

                                    bw.Write((short)2);
                                    bw.Write(month.vehicleDeaths);
                                    bw.Write(month.vehicleKills);
                                    bw.Write(month.killPoints);
                                    bw.Write(month.deathPoints);
                                    bw.Write(month.assistPoints);
                                    bw.Write(month.bonusPoints);
                                    bw.Write(month.kills);
                                    bw.Write(month.deaths);
                                    bw.Write((int)0);
                                    bw.Write(month.playSeconds);
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
                            Data.DB.alias getAlias = db.alias.FirstOrDefault(a => a.name.Equals(pkt.options));
                            Data.DB.player getPlayer = db.players.FirstOrDefault(p => p.alias1 == getAlias && p.zone == zone._zone.id);
                            if (getPlayer == null)
                                return;

                            //Lets give them a year's worth
                            DateTime now = DateTime.Today;
                            if (((int)now.Day - 1) > 1)
                                now = now.AddDays(-(((int)now.Day) - 1));
                            DateTime today = now;
                            now = now.AddMonths(-((int)now.Month - 1));

                            var yearly = (from yt in db.statsYearlies
                                       where yt.zone1 == zone._zone && yt.date >= now && yt.date < today
                                       orderby yt.date descending
                                       select yt);

                            MemoryStream stream = new MemoryStream();
                            try
                            {
                                foreach (Data.DB.statsYearly year in yearly)
                                {
                                    BinaryWriter bw = new BinaryWriter(stream);
                                    bw.Write(year.players[0].alias1.name.ToCharArray());
                                    bw.Write((byte)0);

                                    Data.DB.squad squad = year.players[0].squad1;
                                    string squadname = "";
                                    if (squad != null)
                                        squadname = squad.name;

                                    bw.Write(squadname.ToCharArray());
                                    bw.Write((byte)0);

                                    bw.Write((short)2);
                                    bw.Write(year.vehicleDeaths);
                                    bw.Write(year.vehicleKills);
                                    bw.Write(year.killPoints);
                                    bw.Write(year.deathPoints);
                                    bw.Write(year.assistPoints);
                                    bw.Write(year.bonusPoints);
                                    bw.Write(year.kills);
                                    bw.Write(year.deaths);
                                    bw.Write((int)0);
                                    bw.Write(year.playSeconds);
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
        /// Handles a player's update stat request
        /// </summary>
        static public void Handle_CS_StatsUpdate(CS_StatsUpdate<Zone> pkt, Zone zone)
		{
			//Find player
			Zone.Player player = zone.getPlayer(pkt.player.id);
			if (player == null)
			{
				Log.write(TLog.Warning, "Ignoring stat update for id {0}, not present in zone mirror.", pkt.player.id);
				return;
			}
			
			using (InfantryDataContext db = zone._server.getContext())
			{
				//Get player entry
				Data.DB.player dbplayer = db.players.SingleOrDefault(p => p.id == player.dbid);
				if (dbplayer == null)
				{
					Log.write(TLog.Warning, "Ignoring stat update for {0}, not present in database.", player.alias);
					return;
				}
				
				DateTime today = DateTime.Today;
                switch (pkt.scoreType)
                {
                    case CS_StatsUpdate<Zone>.ScoreType.ScoreDaily:
                        {
                            //Add to the database
                            Data.DB.statsDaily daily = new Data.DB.statsDaily();

                            daily.experience = pkt.stats.experience;
                            daily.experienceTotal = pkt.stats.experienceTotal;
                            daily.kills = pkt.stats.kills;
                            daily.deaths = pkt.stats.deaths;
                            daily.killPoints = pkt.stats.killPoints;
                            daily.deathPoints = pkt.stats.deathPoints;
                            daily.assistPoints = pkt.stats.assistPoints;
                            daily.bonusPoints = pkt.stats.bonusPoints;
                            daily.vehicleKills = pkt.stats.vehicleKills;
                            daily.vehicleDeaths = pkt.stats.vehicleDeaths;
                            daily.playSeconds = pkt.stats.playSeconds;
                            daily.zone = zone._zone.id;
                            daily.date = pkt.date;

                            db.SubmitChanges();
                        }
                        break;

                    case CS_StatsUpdate<Zone>.ScoreType.ScoreWeekly:
                        {
                            //Add to the database
                            Data.DB.statsWeekly weekly = new Data.DB.statsWeekly();

                            weekly.experience = pkt.stats.experience;
                            weekly.experienceTotal = pkt.stats.experienceTotal;
                            weekly.kills = pkt.stats.kills;
                            weekly.deaths = pkt.stats.deaths;
                            weekly.killPoints = pkt.stats.killPoints;
                            weekly.deathPoints = pkt.stats.deathPoints;
                            weekly.assistPoints = pkt.stats.assistPoints;
                            weekly.bonusPoints = pkt.stats.bonusPoints;
                            weekly.vehicleKills = pkt.stats.vehicleKills;
                            weekly.vehicleDeaths = pkt.stats.vehicleDeaths;
                            weekly.playSeconds = pkt.stats.playSeconds;
                            weekly.zone = zone._zone.id;
                            weekly.date = pkt.date;

                            db.SubmitChanges();
                        }
                        break;

                    case CS_StatsUpdate<Zone>.ScoreType.ScoreMonthly:
                        {
                            //Add to the database
                            Data.DB.statsMonthly monthly = new Data.DB.statsMonthly();

                            monthly.experience = pkt.stats.experience;
                            monthly.experienceTotal = pkt.stats.experienceTotal;
                            monthly.kills = pkt.stats.kills;
                            monthly.deaths = pkt.stats.deaths;
                            monthly.killPoints = pkt.stats.killPoints;
                            monthly.deathPoints = pkt.stats.deathPoints;
                            monthly.assistPoints = pkt.stats.assistPoints;
                            monthly.bonusPoints = pkt.stats.bonusPoints;
                            monthly.vehicleKills = pkt.stats.vehicleKills;
                            monthly.vehicleDeaths = pkt.stats.vehicleDeaths;
                            monthly.playSeconds = pkt.stats.playSeconds;
                            monthly.zone = zone._zone.id;
                            monthly.date = pkt.date;

                            db.SubmitChanges();
                        }
                        break;

                    case CS_StatsUpdate<Zone>.ScoreType.ScoreYearly:
                        {
                            //Add to the database
                            Data.DB.statsYearly yearly = new Data.DB.statsYearly();

                            yearly.experience = pkt.stats.experience;
                            yearly.experienceTotal = pkt.stats.experienceTotal;
                            yearly.kills = pkt.stats.kills;
                            yearly.deaths = pkt.stats.deaths;
                            yearly.killPoints = pkt.stats.killPoints;
                            yearly.deathPoints = pkt.stats.deathPoints;
                            yearly.assistPoints = pkt.stats.assistPoints;
                            yearly.bonusPoints = pkt.stats.bonusPoints;
                            yearly.vehicleKills = pkt.stats.vehicleKills;
                            yearly.vehicleDeaths = pkt.stats.vehicleDeaths;
                            yearly.playSeconds = pkt.stats.playSeconds;
                            yearly.zone = zone._zone.id;
                            yearly.date = pkt.date;

                            db.SubmitChanges();
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
            using (InfantryDataContext db = zone._server.getContext())
            {
                Data.DB.squad winner = db.squads.FirstOrDefault(s => s.id == pkt.winner);
                Data.DB.squad loser = db.squads.FirstOrDefault(s => s.id == pkt.loser);

                //Try to trick me, I dare you, do it.
                if (winner == null || loser == null)
                    return;

                Data.DB.squadstats wStats = db.squadstats.FirstOrDefault(s => s.squad == winner.id);
                Data.DB.squadstats lStats = db.squadstats.FirstOrDefault(s => s.squad == loser.id);

                //Again, try it!
                if (wStats == null || lStats == null)
                    return;

                //Update our winners!
                wStats.kills += pkt.wStats.kills;
                wStats.deaths += pkt.wStats.deaths;
                wStats.points += pkt.wStats.points;
                wStats.wins++;

                //Update our losers!
                lStats.kills += pkt.wStats.kills;
                lStats.deaths += pkt.wStats.deaths;
                lStats.points += pkt.wStats.points;
                lStats.losses++; //Sad trombone.....

                //Grab our associated match.
                Data.DB.squadmatch match = db.squadmatches.FirstOrDefault(m => m.squad1 == winner.id | m.squad2 == winner.id | m.squad1 == loser.id | m.squad2 == loser.id && m.winner == null);

                //Update it
                match.winner = pkt.winner;
                match.loser = pkt.loser;
                match.dateEnd = DateTime.Now;

                //Submit
                db.SubmitChanges();
            }
        }

		/// <summary>
		/// Registers all handlers
		/// </summary>
		[RegistryFunc]
		static public void Register()
		{
			CS_PlayerStatsRequest<Zone>.Handlers += Handle_CS_PlayerStatsRequest;
            CS_StatsUpdate<Zone>.Handlers += Handle_CS_StatsUpdate;
            CS_SquadMatch<Zone>.Handlers += Handle_CS_SquadMatch;
		}
	}
}
