using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipeComm
{
    public class DaemonStatus : Packet
    {
        public List<DaemonZoneEntry> Zones { get; set; } = new List<DaemonZoneEntry>();
    }

    public class DaemonZoneEntry
    {
        public int ProcessId { get; set; }

        public string Name { get; set; }

        public int PlayerCount { get; set; }

        public int ArenaCount { get; set; }
    }
}
