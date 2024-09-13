using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace InfServer.Game
{
    public class SilencedPlayer
    {
        public IPAddress IPAddress { get; set; }
        public int DurationMinutes { get; set; }
        public DateTime SilencedAt { get; set; }

        public string Alias { get; set; }
    }
}
