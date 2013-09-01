using System;
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
            string[] chats = pkt.chat.Trim().Split(splitArr, StringSplitOptions.RemoveEmptyEntries);

            //Hey, how'd you get here?!
            if (player == null)
            {
                Log.write(TLog.Error, "Handle_CS_JoinChat(): Called with null player.");
                return;
            }

            //He wants to see the player list of each chat..
            if (pkt.chat.Length == 0)
            {
                foreach (var chat in server._chats.Values)
                {
                    if (chat.hasPlayer(pkt.from))
                        server.sendMessage(zone, pkt.from, String.Format("{0}: {1}", chat._name, chat.List()));
                }
                return;
            }

            foreach (string chat in chats)
            {
                string name = chat.ToLower();
                Chat _chat = server.getChat(chat);

                //Remove him from everything..
                if (name == "off")
                {
                    foreach (var c in server._chats)
                    {
                        if (c.Value.hasPlayer(pkt.from))
                            c.Value.lostPlayer(pkt.from);
                    }
                    server.sendMessage(zone, pkt.from, "No Chat Channels Defined");
                    return;
                }

                //New chat
                if (!server._chats.ContainsValue(_chat))
                {
                    Log.write(TLog.Normal, "Chat created: {0}", chat);
                    _chat = new Chat(server, chat);
                }

                //Add him
                if (!_chat.hasPlayer(pkt.from))
                    _chat.newPlayer(pkt.from, chats);

                //Send him the updated list..
                server.sendMessage(zone, pkt.from, String.Format("{0}: {1}", chat, _chat.List()));
            }

            //Remove him from any chats that didn't come over in the packet.
            foreach (Chat c in server._chats.Values)
            {
                if (!chats.Contains(c._name))
                {
                    if (c.hasPlayer(pkt.from))
                        c.lostPlayer(pkt.from);
                }
            }
        }

        static public void Handle_CS_Chat(CS_PrivateChat<Zone> pkt, Zone zone)
        {
            DBServer server = zone._server;
            Chat chat = server.getChat(pkt.chat.ToLower());

            //WTF MATE?
            if (chat == null)
            {
                Log.write(TLog.Error, "Handle_CS_Chat(): Called with null chat.");
                return;
            }

            SC_PrivateChat<Zone> reply = new SC_PrivateChat<Zone>();
            reply.chat = pkt.chat;
            reply.from = pkt.from;
            reply.message = pkt.message;
            reply.users = chat.List();

            foreach (Zone z in server._zones)
            {
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