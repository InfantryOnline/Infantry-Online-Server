using System;
using System.Collections.Generic;
using System.Linq;

using InfServer.Protocol;
using System.Text.RegularExpressions;

namespace InfServer.Logic
{
    class Logic_Chats
    {
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
                {
                    Log.write(TLog.Normal, "Opened chat: '{0}'", chat);
                    _chat = new Chat(server, chat);
                }

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
            reply.users = chat.List();

            foreach (Zone z in server._zones)
            {
                if (z == null)
                    continue;

                z._client.send(reply);
            }
        }

        static public void Handle_CS_ModCommand(CS_ModCommand<Zone> pkt, Zone zone)
        {
            using (Data.InfantryDataContext db = zone._server.getContext())
            {
                Data.DB.history hist = new Data.DB.history();
                hist.sender = pkt.sender;
                hist.recipient = pkt.recipient;
                hist.zone = pkt.zone;
                hist.arena = pkt.arena;
                hist.command = pkt.command;
                hist.date = DateTime.Now;
                db.histories.InsertOnSubmit(hist);
                db.SubmitChanges();
            }
        }

        static public void Handle_CS_ChatCommand(CS_ChatCommand<Zone> pkt, Zone zone)
        {
            using (Data.InfantryDataContext db = zone._server.getContext())
            {
                Data.DB.helpcall help = new Data.DB.helpcall();
                help.sender = pkt.sender;
                help.zone = pkt.zone;
                help.arena = pkt.arena;
                help.reason = pkt.reason;
                help.date = DateTime.Now;
                db.helpcalls.InsertOnSubmit(help);
                db.SubmitChanges();
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