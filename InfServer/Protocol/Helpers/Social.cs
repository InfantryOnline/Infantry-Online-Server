using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;


namespace InfServer.Protocol
{	/// <summary>
	/// Provides a series of functions for easily serialization of packets
	/// </summary>
	public partial class Helpers
	{	// Member Classes
		//////////////////////////////////////////////////
		public enum ChartType
		{
			ScoreOnlinePlayers = 0,
			ScoreLifetime = 1,
			ScoreCurrentGame = 2,
			ScoreDaily = 3,
			ScoreWeekly = 4,
			ScoreMonthly = 5,
			ScoreYearly = 6,
			ScoreHistoryDaily = 7,
			ScoreHistoryWeekly = 8,
			ScoreHistoryMonthly = 9,
			ScoreHistoryYearly = 10,
			ScorePreviousGame = 17,
			ScoreCurrentSession = 18,
		}
		
		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Sends arena messages to a player
		/// </summary>
		static public void Social_ArenaChat(Player target, string message, int bong)
		{	//Prepare the chat packet
			SC_Chat chat = new SC_Chat();

			chat.bong = (byte)bong;
			chat.chatType = Chat_Type.Arena;
			chat.message = message;
			chat.from = "";

			//Send it
			target._client.sendReliable(chat);
		}

		/// <summary>
		/// Sends arena messages to players
		/// </summary>
		static public void Social_ArenaChat(IChatTarget a, string message, int bong)
		{	//Prepare the chat packet
			SC_Chat chat = new SC_Chat();

			chat.bong = (byte)bong;
			chat.chatType = Chat_Type.Arena;
			chat.message = message;
			chat.from = "";

			//Send it to all arena participants
			foreach (Player player in a.getChatTargets())
				player._client.sendReliable(chat);
		}

		/// <summary>
		/// Sends arena messages to players
		/// </summary>
		static public void Social_ArenaChat(IEnumerable<Player> players, string message, int bong)
		{	//Prepare the chat packet
			SC_Chat chat = new SC_Chat();

			chat.bong = (byte)bong;
			chat.chatType = Chat_Type.Arena;
			chat.message = message;
			chat.from = "";

			//Send it to all arena participants
			foreach (Player player in players)
				player._client.sendReliable(chat);
		}

		/// <summary>
		/// Sends a player banner update to a list of players
		/// </summary>
		static public void Social_ArenaBanners(IEnumerable<Player> players, Player player)
		{	//Got a banner?
			if (player._bannerData == null)
				return;

			//Send each player's banner
			SC_Banner banner = new SC_Banner();
			banner.player = player;

			foreach (Player plyr in players)
				if (plyr != player)
					plyr._client.sendReliable(banner, 1);
		}

		/// <summary>
		/// Sends all player banners in an arena to a specific player
		/// </summary>
		static public void Social_ArenaBanners(Player player, Arena arena)
		{	//Send each player's banner
			foreach (Player plyr in arena.Players)
			{	//Got a banner?
				if (plyr._bannerData == null)
					return;

				SC_Banner banner = new SC_Banner();
				banner.player = plyr;

				player._client.sendReliable(banner, 1);
			}
		}

		/// <summary>
		/// Sets a player's banner
		/// </summary>
		static public void Social_UpdateBanner(Player player)
		{	//Got a banner?
			if (player._bannerData == null)
				return;

			SC_Banner banner = new SC_Banner();
			banner.player = player;

			player._client.sendReliable(banner, 1);
		}

		/// <summary>
		/// Updates/sends an arena message
		/// </summary>
		static public void Social_TickerMessage(Player p, byte colour, int timer, string tickerMessage)
		{	//Prepare the packet
			SC_ArenaMessage msg = new SC_ArenaMessage();

			msg.colour = colour;
			msg.tickerMessage = tickerMessage;
			msg.timer = (uint)timer;

			p._client.sendReliable(msg);
		}
	}
}
