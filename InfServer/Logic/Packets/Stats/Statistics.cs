using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Protocol;
using InfServer.Game;

namespace InfServer.Logic
{	// Logic_Statistics Class
	/// Deals with handling statistics
	///////////////////////////////////////////////////////
	class Logic_Statistics
	{	/// <summary>
		/// Handles all player update packets received from clients
		/// </summary>
		static public void Handle_CS_ChartRequest(CS_ChartRequest pkt, Player player)
		{	//Make sure he has an arena
			if (player._arena == null)
			{
				Log.write(TLog.Error, "Player {0} requested chart with no arena.", player);
				return;
			}

			//What sort of chart request?
			SC_ScoreChart chart = new SC_ScoreChart();

			switch (pkt.type)
			{
				case Helpers.ChartType.ScoreOnlinePlayers:
					{
						List<Player> players = player._arena.Players.OrderByDescending(p => p.Points).ToList();

						chart.type = Helpers.ChartType.ScoreOnlinePlayers;
						chart.columns = "Online Player Scores,Name,Squad";
						chart.playerFunc = delegate(int idx)
						{	return players[idx];	};
						chart.dataFunc = delegate(int idx)
						{
							if (idx >= players.Count)
								return null;

                            if (players[idx].StatsTotal != null)
                                return players[idx].StatsTotal;

                            return null;
						};

						player._client.sendReliable(chart, 1);
					}
					break;

				case Helpers.ChartType.ScoreCurrentGame:
					{
						List<Player> players = player._arena.Players.OrderByDescending(p => p.Points).ToList();

						chart.type = Helpers.ChartType.ScoreCurrentGame;
						chart.columns = "Current Game Scores,Name,Squad";
						chart.playerFunc = delegate(int idx)
						{ return players[idx]; };
						chart.dataFunc = delegate(int idx)
						{
							if (idx >= players.Count)
								return null;

                            if (players[idx].StatsCurrentGame != null)
                                return players[idx].StatsCurrentGame;

                            return null;
						};

						player._client.sendReliable(chart, 1);
					}
					break;

				case Helpers.ChartType.ScorePreviousGame:
					{
						List<Player> players = player._arena.Players.OrderByDescending(p => p.Points).ToList();

						chart.type = Helpers.ChartType.ScorePreviousGame;
						chart.columns = "Current Session Scores,Name,Squad";
						chart.playerFunc = delegate(int idx)
						{ return players[idx]; };
						chart.dataFunc = delegate(int idx)
						{
							if (idx >= players.Count)
								return null;

                            if (players[idx].StatsLastGame != null)
                                return players[idx].StatsLastGame;

                            return null;
						};

						player._client.sendReliable(chart, 1);
					}
					break;

				case Helpers.ChartType.ScoreCurrentSession:
					{
						List<Player> players = player._arena.Players.OrderByDescending(p => p.Points).ToList();

						chart.type = Helpers.ChartType.ScoreCurrentSession;
						chart.columns = "Current Session Scores,Name,Squad";
						chart.playerFunc = delegate(int idx)
						{ return players[idx]; };
						chart.dataFunc = delegate(int idx)
						{
							if (idx >= players.Count)
								return null;

                            if (players[idx].StatsCurrentSession != null)
                                return players[idx].StatsCurrentSession;

                            return null;
						};

						player._client.sendReliable(chart, 1);
					}
					break;

				case Helpers.ChartType.ScoreLifetime:
					{	//Send the request to the database!
						CS_PlayerStatsRequest<Data.Database> req = new CS_PlayerStatsRequest<Data.Database>();

						req.player = player.toInstance();
						req.type = CS_PlayerStatsRequest<Data.Database>.ChartType.ScoreLifetime;
						req.options = pkt.options;

                        player._arena._server._db.send(req);
					}
					break;

                case Helpers.ChartType.ScoreDaily:
                    {	//Send the request to the database!
                        CS_PlayerStatsRequest<Data.Database> req = new CS_PlayerStatsRequest<Data.Database>();

                        req.player = player.toInstance();
                        req.type = CS_PlayerStatsRequest<Data.Database>.ChartType.ScoreDaily;
                        req.options = pkt.options;

                        player._arena._server._db.send(req);
                    }
                    break;

                case Helpers.ChartType.ScoreWeekly:
                    {	//Send the request to the database!
                        CS_PlayerStatsRequest<Data.Database> req = new CS_PlayerStatsRequest<Data.Database>();

                        req.player = player.toInstance();
                        req.type = CS_PlayerStatsRequest<Data.Database>.ChartType.ScoreWeekly;
                        req.options = pkt.options;

                        player._arena._server._db.send(req);
                    }
                    break;

                case Helpers.ChartType.ScoreMonthly:
                    {	//Send the request to the database!
                        CS_PlayerStatsRequest<Data.Database> req = new CS_PlayerStatsRequest<Data.Database>();

                        req.player = player.toInstance();
                        req.type = CS_PlayerStatsRequest<Data.Database>.ChartType.ScoreMonthly;
                        req.options = pkt.options;

                        player._arena._server._db.send(req);
                    }
                    break;

                case Helpers.ChartType.ScoreYearly:
                    {	//Send the request to the database!
                        CS_PlayerStatsRequest<Data.Database> req = new CS_PlayerStatsRequest<Data.Database>();

                        req.player = player.toInstance();
                        req.type = CS_PlayerStatsRequest<Data.Database>.ChartType.ScoreYearly;
                        req.options = pkt.options;

                        player._arena._server._db.send(req);
                    }
                    break;

                case Helpers.ChartType.ScoreHistoryDaily:
                    {	//Send the request to the database!
                        CS_PlayerStatsRequest<Data.Database> req = new CS_PlayerStatsRequest<Data.Database>();

                        req.player = player.toInstance();
                        req.type = CS_PlayerStatsRequest<Data.Database>.ChartType.ScoreHistoryDaily;

                        if (pkt.options == "")
                            req.options = player._alias.ToString();
                        else
                            req.options = pkt.options;

                        player._arena._server._db.send(req);
                    }
                    break;

                case Helpers.ChartType.ScoreHistoryWeekly:
                    {	//Send the request to the database!
                        CS_PlayerStatsRequest<Data.Database> req = new CS_PlayerStatsRequest<Data.Database>();

                        req.player = player.toInstance();
                        req.type = CS_PlayerStatsRequest<Data.Database>.ChartType.ScoreHistoryWeekly;

                        if (pkt.options == "")
                            req.options = player._alias.ToString();
                        else
                            req.options = pkt.options;

                        player._arena._server._db.send(req);
                    }
                    break;

                case Helpers.ChartType.ScoreHistoryMonthly:
                    {	//Send the request to the database!
                        CS_PlayerStatsRequest<Data.Database> req = new CS_PlayerStatsRequest<Data.Database>();

                        req.player = player.toInstance();
                        req.type = CS_PlayerStatsRequest<Data.Database>.ChartType.ScoreHistoryMonthly;

                        if (pkt.options == "")
                            req.options = player._alias.ToString();
                        else
                            req.options = pkt.options;

                        player._arena._server._db.send(req);
                    }
                    break;

                case Helpers.ChartType.ScoreHistoryYearly:
                    {	//Send the request to the database!
                        CS_PlayerStatsRequest<Data.Database> req = new CS_PlayerStatsRequest<Data.Database>();

                        req.player = player.toInstance();
                        req.type = CS_PlayerStatsRequest<Data.Database>.ChartType.ScoreHistoryYearly;

                        if (pkt.options == "")
                            req.options = player._alias.ToString();
                        else
                            req.options = pkt.options;

                        player._arena._server._db.send(req);
                    }
                    break;

                case Helpers.ChartType.Score1:
                    Log.write(TLog.Warning, "Score1");
                    break;

                case Helpers.ChartType.Score2:
                    Log.write(TLog.Warning, "Score2");
                    break;

                case Helpers.ChartType.Score3:
                    Log.write(TLog.Warning, "Score3");
                    break;

                case Helpers.ChartType.Score4:
                    Log.write(TLog.Warning, "Score4");
                    break;

                case Helpers.ChartType.Score5:
                    Log.write(TLog.Warning, "Score5");
                    break;

                case Helpers.ChartType.Score6:
                    Log.write(TLog.Warning, "Score6");
                    break;

				default:
					{
						chart.type = Helpers.ChartType.ScoreCurrentGame;
						chart.columns = "Invalid Chart Request";
						chart.dataFunc = delegate(int idx) { return null; };
						player._client.sendReliable(chart, 1);
					}
					break;
			}
		}

		/// <summary>
		/// Registers all handlers
		/// </summary>
		[Logic.RegistryFunc]
		static public void Register()
		{
			CS_ChartRequest.Handlers += Handle_CS_ChartRequest;
		}
	}
}