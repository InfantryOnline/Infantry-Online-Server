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

            if (pkt.result == SC_FindPlayer<Database>.FindResult.Failure)
            {
                player.sendMessage(0, String.Format("{0} is not online", pkt.findAlias));
                return;
            }

            if (pkt.result == SC_FindPlayer<Database>.FindResult.Success)
            {
                player.sendMessage(0, String.Format("Found {0}. Zone={1} Arena={2}", pkt.findAlias, pkt.zone, pkt.arena));
            }
        }
    }
}
