using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Web;
using System.IO;

using Assets;

using InfServer.Bots;
using InfServer.Protocol;

namespace InfServer.Game.Commands.Mod
{
	/// <summary>
	/// Provides a series of functions for handling mod commands
	/// </summary>
	public class Network
	{
		/// <summary>
		/// Provides server networking stats per-player
		/// </summary>
		static private String bytesToClosestUnit(double bytes)
		{
			if (bytes <= 1024)
				return String.Format("{0}B", bytes);

			bytes /= 1024;

			if (bytes <= 1024)
				return String.Format("{0}KB", bytes.ToString("F"));

			bytes /= 1024;

			if (bytes <= 1024)
				return String.Format("{0}MB", bytes.ToString("F"));

			bytes /= 1024;

			if (bytes <= 1024)
				return String.Format("{0}GB", bytes.ToString("F"));

			bytes /= 1024;

			if (bytes <= 1024)
				return String.Format("{0}TB", bytes.ToString("F"));

			return "Invalid";
		}

		/// <summary>
		/// Provides server networking stats per-player
		/// </summary>
        static public void playernetstat(Player player, Player recipient, string payload, int bong)
		{	//Generate some server statistics!
			double totalSendBPS = 0;
			double totalRecvBPS = 0;

			player.sendMessage(0, "&Player Network Statistics:");

			foreach (Player p in player._arena.Players)
			{
				totalSendBPS += p._client._stats.SendSpeed;
				totalRecvBPS += p._client._stats.ReceiveSpeed;

				player.sendMessage(0, String.Format("*{0} (Send={1}KB/s Recv={2}KB/s)", p._alias,
					(((double)p._client._stats.SendSpeed) / 1024).ToString("F"), 
					(((double)p._client._stats.ReceiveSpeed) / 1024).ToString("F")));
			}

			player.sendMessage(0, String.Format("Total (Send={0}KB/s Recv={1}KB/s)",
				(totalSendBPS / 1024).ToString("F"), (totalRecvBPS / 1024).ToString("F")));
		}

		/// <summary>
		/// Provides server networking stats
		/// </summary>
        static public void netstat(Player player, Player recipient, string payload, int bong)
		{	//Are we targetting a player?
			if (recipient != null)
			{
				player.sendMessage(0, String.Format("Player Net Info: {0}", recipient._alias));

				player.sendMessage(0, String.Format("~-    Client Tick: {0}  Server Tick: {1}", recipient._state.lastUpdate, recipient._state.lastUpdateServer));
				player.sendMessage(0, String.Format("~-    TimeDiff: {0}  Diff: {1}", recipient._client._timeDiff, recipient._state.lastUpdateServer - recipient._state.lastUpdate));

				return;
			}
			
			//Generate some server statistics!
			double totalSendBPS = 0;
			double totalRecvBPS = 0;

			player.sendMessage(0, "&Arena Statistics:");

			//Calculate Stats per arena
			foreach (Arena arena in player._server._arenas.Values)
			{
				double arenaSendBPS = 0;
				double arenaRecvBPS = 0;

				foreach (Player p in arena.Players)
				{
					arenaSendBPS += p._client._stats.SendSpeed;
					arenaRecvBPS += p._client._stats.ReceiveSpeed;
				}

				totalSendBPS += arenaSendBPS;
				totalRecvBPS += arenaRecvBPS;

				player.sendMessage(0, String.Format("*{0} (Send={1}KB/s Recv={2}KB/s)", arena._name, 
					(arenaSendBPS / 1024).ToString("F"), (arenaRecvBPS / 1024).ToString("F")));
			}

			player.sendMessage(0, "&Global Statistics:");

			player.sendMessage(0, String.Format("Speed (Send={0}KB/s Recv={1}KB/s)", 
				(totalSendBPS / 1024).ToString("F"), (totalRecvBPS / 1024).ToString("F")));

			if (!player._server.IsStandalone)
				player.sendMessage(0, String.Format("Database (Send={0}KB/s Recv={1}KB/s)",
					(player._server._db.getStats().SendSpeed / 1024).ToString("F"),
					(player._server._db.getStats().ReceiveSpeed / 1024).ToString("F")));

			player.sendMessage(0, String.Format("Bandwidth Used (Sent={0} Recv={1})",
				bytesToClosestUnit(player._server._totalBytesSent), bytesToClosestUnit(player._server._totalBytesReceived)));
		}

		/// <summary>
		/// Registers all handlers
		/// </summary>
		[Commands.RegistryFunc(HandlerType.ModCommand)]
		static public IEnumerable<Commands.HandlerDescriptor> Register()
		{
			yield return new HandlerDescriptor(playernetstat, "playernetstat",
				"Provides server networking stats per-player",
				"*playernetstat",
				InfServer.Data.PlayerPermission.Sysop, false);

			yield return new HandlerDescriptor(netstat, "netstat",
				"Provides server networking stats",
				"*netstat",
				InfServer.Data.PlayerPermission.Sysop, false);
		}
	}
}