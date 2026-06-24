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
using InfServer.Data;

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
        {
            if (!_commandRegistrar._modCommands.TryGetValue(command.ToLower(), out HandlerDescriptor handler))
            {
                if (String.IsNullOrEmpty(command) && !String.IsNullOrWhiteSpace(payload) && from.PermissionLevelLocal > Data.PlayerPermission.Normal)
                {
                    //
                    // Nothing follows the asterisk prefix (so it is not a command) - echo out to Mod Chat as a normal message instead.
                    //
                    foreach (Player p in Players)
                    {
                        if (p != from && p.PermissionLevelLocal >= Data.PlayerPermission.Level1)
                        {
                            p.sendMessage(0, String.Format($"![ModChat] [{from._alias}]> {payload}"));
                        }
                    }
                }
                else
                {
                    //
                    // Command does not exist in our registrar. Maybe a custom scripted command,
                    // although we really shouldn't use these.
                    //
                    from._arena.handlePlayerModCommand(from, recipient, command, payload);

                    //
                    // TODO: Emit a warning saying that commands should be registered through
                    //       the command registrar.
                    //
                }

                return;
            }

            //
            // Extract the permission levels from the command, and from the player; if the player
            // is allowed to use this command, then go ahead and execute.
            //

            var minLevel = (sbyte)handler.Permission.PermissionLevel;
            var aa = handler.Permission.AllowedAuthorities;

            var hasPermission = aa.HasFlag(PermissionAuthority.Player)
                                || (aa.HasFlag(PermissionAuthority.Mod) && minLevel <= from.modpermission)
                                || (aa.HasFlag(PermissionAuthority.ZoneMod) && minLevel <= from.zmodpermission)
                                || (aa.HasFlag(PermissionAuthority.Host) && minLevel <= from.hostpermission);

            if (!hasPermission)
            {
                return;
            }

            if (minLevel > (sbyte)PlayerPermission.Normal) // Authority-gated command, perform additional checks, and echo command out.
            {
                string sRecipient = (recipient != null) ? " :" + recipient._alias + ":" : "(none)";

                foreach (var p in Players)
                {
                    if (p != from && !p._watchMod) { continue; } // Player does not want to be notified.

                    if (p.hostpermission >= from.hostpermission || p.zmodpermission >= from.zmodpermission || p.modpermission >= from.modpermission)
                    {
                        p.sendMessage(0, String.Format($"&[Arena: {from._arena._name}] {from._alias}>{sRecipient} *{command} {payload}"));
                    }
                }

                var hostPowered = from.modpermission == 0 && from.zmodpermission == 0;

                if (recipient != null && hostPowered)
                {
                    if (!Players.Any(p => p._id == recipient._id))
                    {
                        from.sendMessage(-1, "You cannot use commands from one arena to another.");
                        return;
                    }
                }

                //
                // Log usage to the database.
                //
                if (!_server.IsStandalone)
                {
                    CS_ModCommand<Data.Database> pkt = new CS_ModCommand<Data.Database>();
                    pkt.sender = from._alias;
                    pkt.recipient = (recipient != null) ? recipient._alias : "(none)";
                    pkt.zone = from._server.Name;
                    pkt.arena = from._arena._name;
                    pkt.command = command + " " + payload;

                    from._server._db.send(pkt);
                }
            }

            try
            {
                // Go ahead and execute.
                handler.handler(from, recipient, payload, bong);
            }
            catch (Exception ex)
            {
                if (recipient != null)
                {
                    Log.write(TLog.Exception, $"Exception while executing mod command '{command}' from '{from}' to '{recipient}'.\r\nPayload: {payload}\r\n{ex}");
                }
                else
                {
                    Log.write(TLog.Exception, $"Exception while executing mod command '{command}' from '{from}'.\r\nPayload: {payload}\r\n{ex}");
                }
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
        /// Handles a typical chat/communication command received from a player
        /// </summary>
        public void playerCommCommand(Player from, Player recipient, string command, string payload, int bong)
        {	//Attempt to find the appropriate handler
            HandlerDescriptor handler;

            if (!_commandRegistrar._commCommands.TryGetValue(command.ToLower(), out handler))
            {
                from._arena.handlePlayerCommCommand(from, recipient, command, payload);
                return;
            }

            try
            {	//Handle it!
                handler.handler(from, recipient, payload, bong);
            }
            catch (Exception ex)
            {
                if (recipient != null)
                    Log.write(TLog.Exception, "Exception while executing a communication command '{0}' from '{1}' to '{2}'.\r\nPayload: {3}\r\n{4}",
                        command, from, recipient, payload, ex);
                else
                    Log.write(TLog.Exception, "Exception while executing a communication command '{0}' from '{1}'.\r\nPayload: {2}\r\n{3}",
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
