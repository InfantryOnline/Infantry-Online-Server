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
            reply.findAlias = pkt.findAlias;
            reply.alias = pkt.alias;

            foreach (Zone z in zone._server._zones)
            {

                if (z.getPlayer(pkt.findAlias) != null)
                {
                    reply.zone = z._zone.name;
                    reply.arena = z._zone.name;
                    reply.result = SC_FindPlayer<Zone>.FindResult.Online;
                    break;
                }
            }

            if (reply.zone == "")
                reply.result = SC_FindPlayer<Zone>.FindResult.Offline;

            zone._client.send(reply);
        }


        /// <summary>
        /// Handles a findplayer packet.
        /// </summary>
        static public void Handle_CS_Online(CS_Online<Zone> pkt, Zone zone)
        {

            foreach (Zone z in zone._server._zones)
            {
                SC_Online<Zone> reply = new SC_Online<Zone>();
                reply.alias = pkt.alias;
                reply.zone = z._zone.name;
                reply.online = (uint)z._players.Count();
                zone._client.send(reply);
            }

            
        }

        /// <summary>
        /// Registers all handlers
        /// </summary>
        [RegistryFunc]
        static public void Register()
        {
            CS_FindPlayer<Zone>.Handlers += Handle_CS_FindPlayer;
            CS_Online<Zone>.Handlers += Handle_CS_Online;
        }
    }
}
