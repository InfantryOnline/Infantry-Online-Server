using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Protocol;
using InfServer.Game.Commands;
using InfServer.Logic;
using Assets;

namespace InfServer.Game.Commands.Chat
{
	/// <summary>
	/// Provides a series of functions for handling chat commands (starting with ?)
	/// Please write commands in this class in alphabetical order!
	/// </summary>
	public class Stats
	{
		/// <summary>
		/// Displays a chart of each player's lag statistics
		/// </summary>
        public static void lagchart(Player player, Player recipient, string payload, int bong)
		{	//Set the title and columns
			SC_Chart chart = new SC_Chart();

			chart.title = "Online Player Lag Chart";
			chart.columns = "-Name:14,-Squad:14,-Team:14,Current Ping:8,Average Ping:8,Low Ping:8,High Ping:8,Last Ping:8,Server2You Loss:11,You2Server Loss:11";

			foreach (Player p in player._arena.Players)
			{	//Append his stats
				Client.ConnectionStats stats = p._client._stats;

                if (p.IsStealth)
                {
                    continue;
                }

				string row = String.Format("\"{0}\",\"{1}\",\"{2}\",{3},{4},{5},{6},{7},{8}%,{9}%",
							p._alias, p._squad, (p._team == null ? "" : p._team._name),
							stats.clientCurrentUpdate, stats.clientAverageUpdate, stats.clientShortestUpdate, stats.clientLongestUpdate, stats.clientLastUpdate,
							stats.S2CPacketLoss.ToString("F1"), stats.C2SPacketLoss.ToString("F1"));
				chart.rows.Add(row);
			}

			player._client.sendReliable(chart, 1);
		}

		/// <summary>
		/// Displays a chart containing information regarding each player
		/// </summary>
        public static void playerchart(Player player, Player recipient, string payload, int bong)
		{	//Set the title and columns
			SC_Chart chart = new SC_Chart();

			chart.title = "Online Player Information Chart";
			chart.columns = "-Name:14,-Squad:14,-Team:14,-Main Vehicle:14,-Driving Vehicle:14,Experience:10,Lifetime Experience:10,Cash:10,-Ranking:14";

			foreach (Player p in player._arena.Players)
			{	
                
                if (p.IsStealth)
                {
                    continue;
                }

                //Append his stats
				string row = String.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",{5},{6},{7},\"{8}\"",
							p._alias, (p._squad == null ? "" : p._squad), (p._team == null ? "" : p._team._name),
							p._baseVehicle._type.Name, (p._occupiedVehicle == null ? "" : p._occupiedVehicle._type.Name),
							p.Experience, p.ExperienceTotal, p.Cash,
							p._server._zoneConfig.rank.getRank(p.ExperienceTotal));
				chart.rows.Add(row);
			}

			player._client.sendReliable(chart, 1);
		}

        /// <summary>
        /// Displays a chart containing information reguarding each players chats
        /// </summary>
        public static void chatchart(Player player, Player recipient, string payload, int bong)
        {
            if (player._server.IsStandalone)
            {
                //Set the title and colums
                SC_Chart chart = new SC_Chart();

                chart.title = "Online Chat Information Chart";
                chart.columns = "-Name:32,-Zone:32,-Arena:32,-Chats:28,";

                foreach (Player p in player._arena.Players)
                {
                    if (p.IsStealth)
                    {
                        continue;
                    }

                    //Append his stats
                    string row = String.Format("\"{0}\"\",\"{1}\",\"{2}\",\"{3}\",\" \"",
                        p._alias, p._server.Name, p._arena._name, "");
                    chart.rows.Add(row);
                }

                player._client.sendReliable(chart, 1);
            }
            else
            {
                CS_ChartQuery<Data.Database> query = new CS_ChartQuery<Data.Database>();
                query.type = CS_ChartQuery<Data.Database>.ChartType.chatchart;
                query.title = "Online Chat Information Chart";
                query.columns = "-Name:32,-Zone:32,-Arena:32,-Chats:28,";
                query.alias = player._alias;

                player._server._db.send(query);
            }
        }

        /// <summary>
        /// Displays a chart containing info reguarding each players squad
        /// </summary>
        public static void squadchart(Player player, Player recipient, string payload, int bong)
        {
            if (string.IsNullOrWhiteSpace(player._squad) && string.IsNullOrWhiteSpace(payload))
            {
                player.sendMessage(-1, "You are not in a squad.");
                return;
            }

            //Set the title and colums
            SC_Chart chart = new SC_Chart();

            chart.title = "Online Squad Chart Information";
            chart.columns = "-Name:14,-Squad:14,-Arena:14";

            //if (!player.IsStealth)
            //{
            //    chart.rows.Add(String.Format("\"{0}\",\"{1}\",\"{2}\"", player._alias, player._squad, player._arena._name));
            //}

            var squadName = (string.IsNullOrWhiteSpace(payload) ? player._squad : payload).ToLower();

            foreach (Arena arena in player._server._arenas.Values.ToList())
            {
                foreach (Player p in arena.Players)
                {
                    if (p.IsStealth || String.IsNullOrWhiteSpace(p._squad))
                    {
                        continue;
                    }

                    //if (p._id == player._id)
                    //{
                    //    continue;
                    //}

                    if (p._squad.ToLower() != squadName)
                    {
                        continue;
                    }

                    var arenaName = p._arena.isVisibleToPlayer(player) || (p._arena._name == player._arena._name) ? p._arena._name : "(private)";

                    string row = String.Format("\"{0}\",\"{1}\",\"{2}\"", p._alias, p._squad, arenaName);
                    chart.rows.Add(row);
                }
            }

            player._client.sendReliable(chart, 1);
        }

		/// <summary>
		/// Registers all handlers
		/// </summary>
		[Commands.RegistryFunc(HandlerType.ChatCommand)]
		public static IEnumerable<Commands.HandlerDescriptor> Register()
		{
			yield return new HandlerDescriptor(lagchart, "lagchart",
				"Displays a chart of each player's lag statistics.",
				"?lagchart");

			yield return new HandlerDescriptor(playerchart, "playerchart",
				"Displays a chart containing information regarding each player.",
				"?playerchart");

            yield return new HandlerDescriptor(chatchart, "chatchart",
                "Displays a chart containing information about who's chat is in yours.",
                "?chatchart");

            yield return new HandlerDescriptor(squadchart, "squadchart",
                "Displays a chart containing who's online in your squad.",
                "?squadchart");
		}
	}
}
