using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipeComm
{
    public enum PacketTypes
    {
        ConsoleHealth = 1,
        ConsoleStatus,
        ConsoleMessage,
        ConsoleStart,
        ConsoleStop,
        ConsoleRestart,

        ZoneRestart,
        ZoneMessage,
        ZoneStatus,
        ZoneHealth
    }
}
