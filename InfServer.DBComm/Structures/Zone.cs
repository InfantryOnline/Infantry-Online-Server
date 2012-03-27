using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using InfServer.Network;

namespace InfServer.Data
{
    public class ZoneInstance
    {
        public ushort id;
        public int count;
        public List<PlayerInstance> players;

        ZoneInstance()
        {

        }
    }

}