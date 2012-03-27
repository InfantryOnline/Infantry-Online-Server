using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Protocol;
using InfServer.Data;
using InfServer.Game;

namespace InfServer.Logic
{
    class Logic_Commands
    {
        static public void Handle_SC_FindPlayer(SC_FindPlayer<Database> pkt, Database db)
        {
            Player player = db._server.getPlayer(pkt.alias);

            //Sanity check
            if (player == null)
                return;

            //Offline?
            if (pkt.result == SC_FindPlayer<Database>.FindResult.Offline)
            {
                player.sendMessage(0, "Player is not online");
                return;
            }
            //Online!
            if (pkt.result == SC_FindPlayer<Database>.FindResult.Online)
            {
                player.sendMessage(0, String.Format("Found {0} Zone={1} Arena={2}", pkt.findAlias, pkt.zone, pkt.arena));
            }
        }


        static public void Handle_SC_Online(SC_Online<Database> pkt, Database db)
        {
            Player player = db._server.getPlayer(pkt.alias);

            if (player == null)
               return;


            player.sendMessage(0, String.Format("~Server={0} Players={1}", pkt.zone, pkt.online));

        }

        /// <summary>
        /// Registers all handlers
        /// </summary>
        [Logic.RegistryFunc]
        static public void Register()
        {
            SC_FindPlayer<Database>.Handlers += Handle_SC_FindPlayer;
            SC_Online<Database>.Handlers += Handle_SC_Online;
        }
    }
}
