using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DatabaseServerNATS.Application
{
    public class ServerStore
    {
        public ServerStore()
        {
            TimeStarted.Start();
        }

        public Stopwatch TimeStarted = new Stopwatch();
        public ConcurrentDictionary<string, ZoneServer> ZoneServers = new ConcurrentDictionary<string, ZoneServer>();
        public ConcurrentDictionary<string, ChatChannel> ChatChannels = new ConcurrentDictionary<string, ChatChannel>();
    }
}
