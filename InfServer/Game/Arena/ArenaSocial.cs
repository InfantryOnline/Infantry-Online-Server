using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using InfServer.Network;
using InfServer.Protocol;
using InfServer.Game.Commands;

using Assets;

namespace InfServer.Game
{
	// Arena Class
	/// Represents a single arena in the server
	///////////////////////////////////////////////////////
	public abstract partial class Arena : IChatTarget
	{	// Member variables
		///////////////////////////////////////////////////
		

		///////////////////////////////////////////////////
		// Member Classes
		///////////////////////////////////////////////////
		

		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		#region Actions
		/// <summary>
		/// Sends an arena message to the entire arena
		/// </summary>
		public void sendArenaMessage(string message)
		{	//Relay the message
			Helpers.Social_ArenaChat(this, message, 0);
		}

		/// <summary>
		/// Sends an arena message to the entire arena
		/// </summary>
		public void sendArenaMessage(string message, int bong)
		{	//Relay the message
			Helpers.Social_ArenaChat(this, message, bong);
		}

		/// <summary>
		/// Sends a new infoarea message
		/// </summary>
		public void triggerMessage(byte colour, int timer, string message)
		{	//Relay the message
			Helpers.Arena_Message(Players, colour, timer, message, (Player)null);
		}

		/// <summary>
		/// Sends a new infoarea message
		/// </summary>
		public void triggerMessage(byte colour, int timer, string message, Player except)
		{	//Relay the message
			Helpers.Arena_Message(Players, colour, timer, message, except);
		}

		/// <summary>
		/// Sends a new infoarea message
		/// </summary>
		public void triggerMessage(byte colour, int timer, string message, Team except)
		{	//Relay the message
			Helpers.Arena_Message(Players, colour, timer, message, except);
		}

		/// <summary>
		/// Sets a new ticker message
		/// </summary>
		public void setTicker(byte colour, int idx, int timer, Func<Player, String> customTicker)
		{	//Redirect
			setTicker(colour, idx, timer, null, null, customTicker);
		}

		/// <summary>
		/// Sets a new ticker message
		/// </summary>
		public void setTicker(byte colour, int idx, int timer, string message)
		{	//Redirect
			setTicker(colour, idx, timer, message, null);
		}

		/// <summary>
		/// Sets a new ticker message
		/// </summary>
		public void setTicker(byte colour, int idx, int timer, string message, Action expireCallback)
		{	//Set the new ticker
			TickerInfo ticker = _tickers[idx] = new TickerInfo(message, timer, idx, colour, expireCallback, null);

			//Relay the change
			Helpers.Arena_Message(Players, ticker);
		}

		/// <summary>
		/// Sets a new ticker message
		/// </summary>
		public void setTicker(byte colour, int idx, int timer, string message, Action expireCallback, Func<Player, String> customTicker)
		{	//Set the new ticker
			TickerInfo ticker = _tickers[idx] = new TickerInfo(message, timer, idx, colour, expireCallback, customTicker);

			//Relay the change
			Helpers.Arena_Message(Players, ticker);
		}
		#endregion

		#region Handlers
		/// <summary>        
		/// Returns the list of player targets for public chat
		/// </summary>        
		public IEnumerable<Player> getChatTargets()
		{
			return Players;
		}

		/// <summary>
		/// Handles a moderator command received from a player
		/// </summary>
		public void playerModCommand(Player from, Player recipient, string command, string payload, int bong)
		{	//Attempt to find the appropriate handler
			HandlerDescriptor handler;

			if (!_commandRegistrar._modCommands.TryGetValue(command.ToLower(), out handler))
				return;

			//Check the permission levels
			if ((int)from.PermissionLevelLocal < (int)handler.permissionLevel)
			{	//Not going to happen.
				return;
			}

            //Notify his superiors
            string sRecipient;
            foreach(Arena a in from._server._arenas.Values)
                foreach (Player p in a.Players)
                    if (p != from && (int)from.PermissionLevelLocal <= (int)p.PermissionLevelLocal)
                        p.sendMessage(0, String.Format("@[Arena: {0}] {1}>{2} *{3} {4}",
                            from._arena._name,
                            from._alias,
                            sRecipient = (recipient != null)
                                ? " :" + recipient._alias + ":"
                                : String.Empty,
                            command,
                            payload));

            //Log it in the history
            //TODO: Log it in the database

			try
			{	//Handle it!
				handler.handler(from, recipient, payload, bong);
			}
			catch (Exception ex)
			{
				if (recipient != null)
					Log.write(TLog.Exception, "Exception while executing mod command '{0}' from '{1}' to '{2}'.\r\nPayload: {3}\r\n{4}",
						command, from, recipient, payload, ex);
				else
					Log.write(TLog.Exception, "Exception while executing mod command '{0}' from '{1}'.\r\nPayload: {2}\r\n{3}",
						command, from, payload, ex);
			}
		}

		/// <summary>
		/// Handles a typical chat command received from a player
		/// </summary>
		public void playerChatCommand(Player from, Player recipient, string command, string payload, int bong)
		{	//Attempt to find the appropriate handler
			HandlerDescriptor handler;

			if (!_commandRegistrar._chatCommands.TryGetValue(command.ToLower(), out handler))
            {
                from._arena.handlePlayerChatCommand(from, recipient, command, payload);
                return;
            }

			try
			{	//Handle it!
				handler.handler(from, recipient, payload, bong);
			}
			catch (Exception ex)
			{
				if (recipient != null)
					Log.write(TLog.Exception, "Exception while executing chat command '{0}' from '{1}' to '{2}'.\r\nPayload: {3}\r\n{4}",
						command, from, recipient, payload, ex);
				else
					Log.write(TLog.Exception, "Exception while executing chat command '{0}' from '{1}'.\r\nPayload: {2}\r\n{3}",
						command, from, payload, ex);
			}
		}

		/// <summary>
		/// Triggered when a player has sent chat to the entire arena
		/// </summary>
		public void playerArenaChat(Player from, CS_Chat chat)
		{	//Route it to our entire player list!
			Helpers.Player_RouteChat(this, from, chat);
		}
		#endregion
	}
}
