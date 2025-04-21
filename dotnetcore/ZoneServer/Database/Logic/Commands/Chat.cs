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
        /// Receives a chat query from the server and directs or reads the data
        /// </summary>
        static public void Handle_SC_ChatQuery(SC_ChatQuery<Database> pkt, Database db)
        {
            Player sender = null;
            //Find the sender
            if (pkt.sender.Length > 0)
            {
                sender = db._server.getPlayer(pkt.sender);
                if (sender == null)
                    return;
            }

            Player recipient = null;
            //Find the player going to
            if (pkt.recipient.Length > 0)
            {
                recipient = db._server.getPlayer(pkt.recipient);
                if (recipient == null)
                    return;
            }

            //Find the player to respond to
            if (String.IsNullOrEmpty(pkt.payload))
                return;

            switch (pkt.type)
            {
                case CS_ChatQuery<Database>.QueryType.accountignore:
                    {
                        if (sender != null)
                        {   //Parse it
                            string[] split = pkt.payload.Split(',');
                            if (sender._accountIgnore.ContainsKey(split.First()) && split.Length > 1)
                            {
                                System.Net.IPAddress address;
                                if (System.Net.IPAddress.TryParse(split[1], out address))
                                    sender._accountIgnore[split[0]] = address;
                            }
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Receives a chart response from the server and directs it to the appropriate player
        /// </summary>
        static public void Handle_SC_ChartResponse(SC_ChartResponse<Database> pkt, Database db)
        {   //Find the player in question
            Player player = db._server.getPlayer(pkt.alias);

            if (player == null)
                return;

            if (pkt.rows.Count == 0)
                return;

            //Form and send a response
            SC_Chart chart = new SC_Chart();

            chart.title = pkt.title;
            chart.columns = pkt.columns;

            foreach (string str in pkt.rows)
            {
                chart.rows.Add(str);
            }
                

            player._client.sendReliable(chart, 1);
        }

        /// <summary>
        /// Registers all handlers
        /// </summary>
        [Logic.RegistryFunc]
        static public void Register()
        {
            SC_ChartResponse<Database>.Handlers += Handle_SC_ChartResponse;
            SC_ChatQuery<Database>.Handlers += Handle_SC_ChatQuery;
        }
    }
}
