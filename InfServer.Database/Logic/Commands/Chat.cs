using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Protocol;
using InfServer.Data;

namespace InfServer.Logic
{
    class Logic_ChatCommands
    {
        /// <summary>
		/// Handles a findplayer packet.
		/// </summary>
        static public void Handle_CS_FindPlayer(CS_FindPlayer<Zone> pkt, Zone zone)
        {
            SC_FindPlayer<Zone> reply = new SC_FindPlayer<Zone>();

            foreach (Zone z in zone._server._zones)
            {

                if (z.getPlayer(pkt.alias) != null)
                {
                    reply.zone = z._zone.name;
                    reply.arena = z._zone.name;
                    reply.result = SC_FindPlayer<Zone>.FindResult.Success;
                    reply.findAlias = pkt.findAlias;
                    reply.alias = pkt.alias;
                    break;
                }
            }

            if (reply.zone == "")
                reply.result = SC_FindPlayer<Zone>.FindResult.Failure;

            zone._client.send(reply);
        }
    }
}
