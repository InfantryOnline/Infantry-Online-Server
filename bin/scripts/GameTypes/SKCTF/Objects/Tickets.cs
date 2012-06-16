using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


using InfServer.Logic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;

namespace InfServer.Script.GameType_SKCTF
{
    public class Tickets
    {
        //Our ticket dictionary <int teamid, int tickets>
        private Dictionary<int, int> _tickets;

        //Our event triggered by modifications to ticket count
        public event Action<Team, int> TicketModify;

        /// <summary>
        /// Creates a new collection of tickets indexed by team ids
        /// </summary>
        /// <param name="teams">List of teams</param>
        /// <param name="numTickets">Starting number of tickets</param>
        public Tickets(IEnumerable<Team> teams, int numTickets)
        {
            _tickets = new Dictionary<int, int>();
            foreach (Team t in teams)
            {
                _tickets.Add(t._id, numTickets);
            }
        }

        /// <summary>
        /// Gets or sets number of tickets for a specific team
        /// </summary>
        public int this[Team team]
        {
            get
            {
                return _tickets[team._id];
            }
            set
            {
                //First, update the value
                _tickets[team._id] = value;

                //Now do run some events
                if (TicketModify != null)
                    TicketModify(team, _tickets[team._id]);
            }
        }
    }
}