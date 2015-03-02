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
	{	///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Sends the ticker information to a single player
		/// </summary>
		static public void Arena_Message(Player p, IEnumerable<Arena.TickerInfo> tickers)
		{	//For each ticker..
			foreach (Arena.TickerInfo ticker in tickers)
			{	//A -1 timer means it's inactive
				if (ticker.timer == -1)
					continue;

				//Prepare the packet
				SC_ArenaMessage msg = new SC_ArenaMessage();

				msg.colour = ticker.colour;
				msg.tickerMessage = "*" + ticker.idx + ((ticker.customTicker == null) ? ticker.message : ticker.customTicker(p));
				msg.timer = (uint)((ticker.timer - Environment.TickCount) / 10);

				p._client.sendReliable(msg);
			}
		}

		/// <summary>
		/// Updates/sends an arena message
		/// </summary>
		static public void Arena_Message(IEnumerable<Player> p, Arena.TickerInfo ticker)
		{	//Prepare the packet
			if (ticker.customTicker == null)
			{
				SC_ArenaMessage msg = new SC_ArenaMessage();

				msg.colour = ticker.colour;
				msg.tickerMessage = "*" + ticker.idx + ticker.message;
				msg.timer = (uint)((ticker.timer - Environment.TickCount) / 10);

				foreach (Player player in p.ToList())
					player._client.sendReliable(msg);
			}
			else
			{
				foreach (Player player in p.ToList())
				{
					String tickerMessage = ticker.customTicker(player);
					if (tickerMessage == null)
						continue;

					SC_ArenaMessage msg = new SC_ArenaMessage();

					msg.colour = ticker.colour;
					msg.tickerMessage = "*" + ticker.idx + tickerMessage;
					msg.timer = (uint)((ticker.timer - Environment.TickCount) / 10);

					player._client.sendReliable(msg);
				}
			}
		}

		/// <summary>
		/// Updates/sends an arena message
		/// </summary>
		static public void Arena_Message(IEnumerable<Player> p, byte colour, int timer, string tickerMessage, Player except)
		{	//Prepare the packet
			SC_ArenaMessage msg = new SC_ArenaMessage();

			msg.colour = colour;
			msg.tickerMessage = tickerMessage;
			msg.timer = (uint)timer;

			foreach (Player player in p.ToList())
				if (player != except)
					player._client.sendReliable(msg);
		}

		/// <summary>
		/// Updates/sends an arena message
		/// </summary>
		static public void Arena_Message(IEnumerable<Player> p, byte colour, int timer, string tickerMessage, Team except)
		{	//Prepare the packet
			SC_ArenaMessage msg = new SC_ArenaMessage();

			msg.colour = colour;
			msg.tickerMessage = tickerMessage;
			msg.timer = (uint)timer;

			foreach (Player player in p.ToList())
				if (player._team != except)
					player._client.sendReliable(msg);
		}
	}
}
