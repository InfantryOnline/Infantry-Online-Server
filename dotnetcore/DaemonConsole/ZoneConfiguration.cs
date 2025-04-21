using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DaemonConsole
{
    public class ZoneConfiguration
    {
        public List<ZoneEntry> Zones { get; set; } = new List<ZoneEntry>();
    }

    public class ZoneEntry
    {
        public string Name { get; set; }

        public string Folder { get; set; }
    }
}
