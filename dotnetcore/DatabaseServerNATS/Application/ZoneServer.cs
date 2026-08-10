using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DatabaseServerNATS.Application
{
    public class ZoneServer
    {
        public Stopwatch LastHeartbeat { get; set; } = new Stopwatch();

        public ConcurrentDictionary<string, Player> Players = new ConcurrentDictionary<string, Player>();
    }
}
