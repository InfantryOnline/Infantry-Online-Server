using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Protocol;
using InfServer.Data;
using InfServer.Game;

namespace InfServer.Logic
{   // Logic_Commands Class
    // Deals with player specific database packets
    /////////////////////////////////////////////////////// 
    class Logic_Commands
    {
        /// <summary>
        /// Receives a chart response from the server and directs it to the appropriate player
        /// </summary>
        static public void Handle_SC_ChartResponse(SC_ChartResponse<Database> pkt, Database db)
        {   //Find the player in question
            Player player = db._server.getPlayer(pkt.alias);
            if (player == null)
                return;

            if (String.IsNullOrEmpty(pkt.data))
                return;

            //Form and send a response
            SC_Chart chart = new SC_Chart();

            chart.title = pkt.title;
            chart.columns = pkt.columns;

            string[] chats = pkt.data.Split('\n');
            foreach (string str in chats)
                chart.rows.Add(str);

            player._client.sendReliable(chart, 1);
        }

        /// <summary>
        /// Registers all handlers
        /// </summary>
        [Logic.RegistryFunc]
        static public void Register()
        {
            SC_ChartResponse<Database>.Handlers += Handle_SC_ChartResponse;
        }
    }
}
