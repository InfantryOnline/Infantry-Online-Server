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
