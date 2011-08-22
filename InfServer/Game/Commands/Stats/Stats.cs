using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Protocol;
using InfServer.Game.Commands;
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
		public static void lagchart(Player player, Player recipient, string payload)
		{	//Set the title and columns
			SC_Chart chart = new SC_Chart();

			chart.title = "Online Player Lag Chart";
			chart.columns = "-Name:14,-Squad:14,-Team:14,Current Ping:8,Average Ping:8,Low Ping:8,High Ping:8,Last Ping:8,Server2You Loss:11,You2Server Loss:11";

			foreach (Player p in player._arena.Players)
			{	//Append his stats
				Client.ConnectionStats stats = p._client._stats;

				string row = String.Format("\"{0}\"\",\"\"{1}\"\",\"\"{2}\"\",{3},{4},{5},{6},{7},{8}%,{9}%",
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
		public static void playerchart(Player player, Player recipient, string payload)
		{	//Set the title and columns
			SC_Chart chart = new SC_Chart();

			chart.title = "Online Player Information Chart";
			chart.columns = "-Name:14,-Squad:14,-Team:14,-Main Vehicle:14,-Driving Vehicle:14,Experience:10,Lifetime Experience:10,Cash:10,-Ranking:14";

			foreach (Player p in player._arena.Players)
			{	//Append his stats
				string row = String.Format("\"{0}\"\",\"\"{1}\"\",\"\"{2}\"\",\"{3}\",\"{4}\",{5},{6},{7},\"{8}\"",
							p._alias, (p._squad == null ? "" : p._squad), (p._team == null ? "" : p._team._name),
							p._baseVehicle._type.Name, (p._occupiedVehicle == null ? "" : p._occupiedVehicle._type.Name),
							p.Experience, p.ExperienceTotal, p.Cash,
							p._server._zoneConfig.rank.getRank(p.ExperienceTotal));
				chart.rows.Add(row);
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
		}
	}
}
