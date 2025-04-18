using System;
using System.Collections.Generic;
using System.Linq;

using InfServer.Protocol;
using System.Text.RegularExpressions;
using Database;

namespace InfServer.Logic
{
    class Logic_Chats
    {
        /// <summary>
        /// Handles a packet being sent directly from a player's client or using ?chat command
        /// </summary>
        static public void Handle_CS_JoinChat(CS_JoinChat<Zone> pkt, Zone zone)
        {
            DBServer server = zone._server;
            Zone.Player player = server.getPlayer(pkt.from);
            char[] splitArr = { ',' };
            List<string> chats = new List<string>();

            //Hey, how'd you get here?!
            if (player == null)
            {
                Log.write(TLog.Error, "Handle_CS_JoinChat(): Unable to find player.");
                return;
            }

            //He wants to see the player list of each chat..
            if (String.IsNullOrWhiteSpace(pkt.chat))
            {
                if (player.chats.Count > 0)
                {
                    foreach (var chat in player.chats)
                    {
                        if (String.IsNullOrEmpty(chat))
                            continue;

                        Chat _chat = server.getChat(chat);
                        if (_chat != null && _chat.hasPlayer(player))
                            server.sendMessage(zone, pkt.from, String.Format("{0}: {1}", _chat._name, _chat.List()));
                    }
                }
                return;
            }

            //Split and trim our chats
            chats = pkt.chat.Split(splitArr, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToList();

            foreach (string chat in chats)
            {
                if (String.IsNullOrEmpty(chat))
                    continue;

                string name = chat.ToLower();

                //Remove him from everything..
                if (name == "off")
                {
                    foreach (var c in server._chats.Values.ToList())
                    {
                        if (c == null)
                            continue;

                        if (c.hasPlayer(player))
                            c.lostPlayer(player);
                    }
                    server.sendMessage(zone, pkt.from, "No Chat Channels Defined");
                    return;
                }

                Chat _chat = server.getChat(chat);

                //New chat
                if (_chat == null)
                    _chat = new Chat(server, chat);

                //Add him
                if (!_chat.hasPlayer(player))
                    _chat.newPlayer(player);

                //Send him the updated list..
                server.sendMessage(zone, pkt.from, String.Format("{0}: {1}", chat, _chat.List()));
            }

            //Remove him from any chats that didn't come over in the packet.
            //Rewrite me! Just make sure I'm case-insensitive
            foreach (Chat c in server._chats.Values.ToList())
            {
                if (c == null)
                    continue;

                if (!chats.Contains(c._name, StringComparer.OrdinalIgnoreCase))
                {
                    if (c.hasPlayer(player))
                        c.lostPlayer(player);
                }
            }

            // In case the player reshuffled the order of the chats, save the newly provided order.
            player.chats = chats;
        }

        static public void Handle_CS_Chat(CS_PrivateChat<Zone> pkt, Zone zone)
        {
            DBServer server = zone._server;
            Chat chat = server.getChat(pkt.chat);

            //WTF MATE?
            if (chat == null)
            {
                Log.write(TLog.Error, "Handle_CS_Chat(): Unable to get chat.");
                return;
            }

            SC_PrivateChat<Zone> reply = new SC_PrivateChat<Zone>();
            reply.chat = pkt.chat;
            reply.from = pkt.from;
            reply.message = pkt.message;
            reply.users = chat.All();

            foreach (Zone z in server._zones)
            {
                if (z == null)
                    continue;

                z._client.send(reply);
            }
        }

        static public void Handle_CS_ModCommand(CS_ModCommand<Zone> pkt, Zone zone)
        {
            using (DataContext db = zone._server.getContext())
            {
                History hist = new History();
                hist.Sender = pkt.sender;
                hist.Recipient = pkt.recipient;
                hist.Zone = pkt.zone;
                hist.Arena = pkt.arena;
                hist.Command = pkt.command;
                hist.Date = DateTime.Now;
                db.Histories.Add(hist);
                db.SaveChanges();
            }
        }

        static public void Handle_CS_ChatCommand(CS_ChatCommand<Zone> pkt, Zone zone)
        {
            using (DataContext db = zone._server.getContext())
            {
                Helpcall help = new Helpcall();
                help.Sender = pkt.sender;
                help.Zone = pkt.zone;
                help.Arena = pkt.arena;
                help.Reason = pkt.reason;
                help.Date = DateTime.Now;
                db.Helpcalls.Add(help);
                db.SaveChanges();
            }
        }

        static public string CleanIllegalCharacters(string str)
        {   //Remove non-Infantry characters... trim whitespaces, and remove duplicate spaces
            string sb = "";
            foreach (char c in str)
                if (c >= ' ' && c <= '~')
                    sb += c;
            //Get rid of duplicate spaces
            Regex regex = new Regex(@"[ ]{2,}", RegexOptions.None);
            sb = regex.Replace(sb, @" ");
            //Trim it
            sb = sb.Trim();
            //We have our new Infantry compatible string!
            return sb;
        }

        /// <summary>
        /// Registers all handlers
        /// </summary>
        [RegistryFunc]
        static public void Register()
        {
            CS_JoinChat<Zone>.Handlers += Handle_CS_JoinChat;
            CS_PrivateChat<Zone>.Handlers += Handle_CS_Chat;
            CS_ModCommand<Zone>.Handlers += Handle_CS_ModCommand;
            CS_ChatCommand<Zone>.Handlers += Handle_CS_ChatCommand;
        }
    }
}