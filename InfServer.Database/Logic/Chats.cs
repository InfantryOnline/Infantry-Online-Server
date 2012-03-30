using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Protocol;
using InfServer.Data;
using InfServer;

namespace InfServer.Logic
{
    class Logic_Chats
    {

        static public void Handle_CS_JoinChat(CS_JoinChat<Zone> pkt, Zone zone)
        {
            DBServer server = zone._server;
            Zone.Player player = server.getPlayer(pkt.from);
            Chat chat = server.getChat(pkt.chat);
            string name = pkt.chat.ToLower();

            //Hey, how'd you get here?!
            if (player == null)
                return;

            //New chat
            if (chat == null)
            {
                chat = new Chat(server, pkt.chat);
            }

            //Add him
            if (!chat.hasPlayer(pkt.from))
            {
                chat.newPlayer(pkt.from);
                chat.sendList(pkt.from);
            }

            //Custom preset chat stuff...
            switch (name)
            {
                case "off:":
                    foreach (var c in server._chats)
                    {
                        if (c.Value.hasPlayer(pkt.from))
                            c.Value.lostPlayer(pkt.from);
                    }
                    server.sendMessage(player.zone, player.alias, "No Chat Channels Defined");
                    break;

                case "modchat":
                    break;

                case "list":

                    break;
            }
        }



        static public void Handle_CS_LeaveChat(CS_LeaveChat<Zone> pkt, Zone zone)
        {
        }


        static public void Handle_CS_Chat(CS_PrivateChat<Zone> pkt, Zone zone)
        {
            DBServer server = zone._server;
            Chat chat = server.getChat(pkt.chat);

            //WTF MATE?
            if (chat == null)
                return;

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




        /// <summary>
        /// Registers all handlers
        /// </summary>
        [RegistryFunc]
        static public void Register()
        {
            CS_JoinChat<Zone>.Handlers += Handle_CS_JoinChat;
            CS_LeaveChat<Zone>.Handlers += Handle_CS_LeaveChat;
            CS_PrivateChat<Zone>.Handlers += Handle_CS_Chat;
        }
    }


}
