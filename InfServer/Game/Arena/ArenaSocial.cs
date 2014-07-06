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
            {
                if (String.IsNullOrEmpty(command) && !String.IsNullOrWhiteSpace(payload) 
                    && from.PermissionLevelLocal > Data.PlayerPermission.Normal)
                {
                    //Mod chat
                    if (_server.IsStandalone)
                    {
                        foreach (Player p in Players)
                            if (p != from && p.PermissionLevelLocal >= Data.PlayerPermission.ArenaMod)
                            {
                                p.sendMessage(0, String.Format("&[ModChat] [{0}]> {1}",
                                    from._alias, payload));
                            }
                    }
                    else
                    {
                        //For arena owners only
                        foreach(Player p in Players)
                            if (p != from && p._permissionTemp >= Data.PlayerPermission.ArenaMod)
                            {
                                p.sendMessage(0, String.Format("&[ModChat] [{0}]> {1}", from._alias, payload));
                            }

                        //For all other mods
                        CS_Query<Data.Database> pkt = new CS_Query<Data.Database>();
                        pkt.queryType = CS_Query<Data.Database>.QueryType.modChat;
                        pkt.sender = from._alias;
                        pkt.payload = String.Format("&[ModChat] [{0}]> {1}", from._alias, payload);
                        //Send it!
                        _server._db.send(pkt);
                    }
                }
                else
                    //Possibly a scripted mod command, lets pass it
                    from._arena.handlePlayerModCommand(from, recipient, command, payload);
                return;
            }

            //Are they a developer?
            if (from.PermissionLevelLocal != Data.PlayerPermission.Developer)
            {   //No
                //Check the permission levels
                if ((int)from.PermissionLevelLocal < (int)handler.permissionLevel)
                {	//Not going to happen.
                    return;
                }               
            }
            else
            {
                //They are, is this a dev command?
                if (!handler.isDevCommand)
                    return;
            }

            //Command logging (ignore normal player permission commands like *help, etc)
            if (from.PermissionLevelLocal != Data.PlayerPermission.Normal)
            {   //Notify his superiors in the arena
                string sRecipient;
                foreach (Player p in Players)
                {
                    if (p != from && !p._arena._watchMod)
                        //We have watchmod commands off, bypass this player
                        continue;
                    if (p != from && (int)from.PermissionLevelLocal <= (int)p.PermissionLevelLocal)
                    {
                        p.sendMessage(0, String.Format("&[Arena: {0}] {1}>{2} *{3} {4}",
                            from._arena._name,
                            from._alias,
                            sRecipient = (recipient != null)
                                ? " :" + recipient._alias + ":"
                                : String.Empty,
                            command,
                            payload));
                    }
                    //Developer?
                    else if (from.PermissionLevelLocal == Data.PlayerPermission.Developer)
                    {
                        if (p.PermissionLevelLocal >= Data.PlayerPermission.Mod && p != from)
                            p.sendMessage(0, String.Format("&[Arena: {0}] {1}>{2} *{3} {4}",
                            from._arena._name,
                            from._alias,
                            sRecipient = (recipient != null)
                                ? " :" + recipient._alias + ":"
                                : String.Empty,
                            command,
                            payload));
                    }
                }

                //Log it in the history database
                if (!_server.IsStandalone)
                {
                    CS_ModCommand<Data.Database> pkt = new CS_ModCommand<Data.Database>();
                    pkt.sender = from._alias;
                    pkt.recipient = (recipient != null) ? recipient._alias : "none";
                    pkt.zone = from._server.Name;
                    pkt.arena = from._arena._name;
                    pkt.command = command + " " + payload;

                    //Send it!
                    from._server._db.send(pkt);
                }
            }

            try
            {
                //Security hole fix
                //Lets check their level and their arena
                if (from.PermissionLevel < Data.PlayerPermission.Mod)
                {
                    if (recipient != null && !recipient._arena._name.Equals(from._arena._name))
                    {
                        from.sendMessage(-1, "You cannot use commands from one arena to another.");
                        return;
                    }
                }

                //Handle it!
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
