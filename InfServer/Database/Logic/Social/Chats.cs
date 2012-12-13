using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Protocol;
using InfServer.Data;
using InfServer.Game;

namespace InfServer.Logic
{	// Logic_Chats Class
	/// Deals with player specific database packets
	///////////////////////////////////////////////////////
    class Logic_Chats
    {

        //Handles private chat entries.
        static public void Handle_SC_JoinChat(SC_JoinChat<Database> pkt, Database db)
        {   //Todo: refactor this entire func
            Player player = db._server.getPlayer(pkt.from);

            if (player == null)
                return;

            //char[] splitArr = { ',' };
            char[] splitArr = { ' ' };
            string[] users = pkt.users.Split(splitArr, StringSplitOptions.RemoveEmptyEntries);

            SC_Chat notify = new SC_Chat();
            notify.chatType = Helpers.Chat_Type.PrivateChat;
            notify.message = pkt.chat + ":<< Entering chat >>";
            notify.from = pkt.from;

            foreach (string user in users)
            {
                Player p = db._server.getPlayer(user);
                if (p == null)
                    continue;

                if (p._alias == pkt.from)
                    continue;

                p._client.send(notify);
            }
        }

        static public void Handle_SC_LeaveChat(SC_LeaveChat<Database> pkt, Database db)
        {
            //char[] splitArr = { ',' };
            char[] splitArr = { ' ' };
            string[] users = pkt.users.Split(splitArr, StringSplitOptions.RemoveEmptyEntries);

            SC_Chat notify = new SC_Chat();
            notify.chatType = Helpers.Chat_Type.PrivateChat;
            notify.message = pkt.chat + ":<< Leaving Chat >>";
            notify.from = pkt.from;

            foreach (string user in users)
            {
                Player p = db._server.getPlayer(user);
                if (p == null)
                    continue;

                if (p._alias == pkt.from)
                    continue;

                p._client.send(notify);
            }
        }

        static public void Handle_SC_Chat(SC_PrivateChat<Database> pkt, Database db)
        {
            SC_Chat msg = new SC_Chat();
            msg.chatType = Helpers.Chat_Type.PrivateChat;
            msg.message = pkt.chat + ":" + pkt.message;
            msg.from = pkt.from;


            //char[] splitArr = { ',' };
            char[] splitArr = { ' ' };
            string[] users = pkt.users.Split(splitArr, StringSplitOptions.RemoveEmptyEntries);
            foreach (string user in users)
            {
                Player p = db._server.getPlayer(user);
                if (p == null)
                    continue;

                if (p._alias == pkt.from)
                    continue;

                p._client.sendReliable(msg);
            }
        }
        

        /// <summary>
        /// Registers all handlers
        /// </summary>
        [Logic.RegistryFunc]
        static public void Register()
        {
            SC_JoinChat<Database>.Handlers += Handle_SC_JoinChat;
            SC_LeaveChat<Database>.Handlers += Handle_SC_LeaveChat;
            SC_PrivateChat<Database>.Handlers += Handle_SC_Chat;
        }
    }

}
