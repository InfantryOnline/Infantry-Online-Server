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
        public int _playercount;
        public string _name;
        public string _ip;
        public short _port;

        public ZoneInstance(ushort id, string name, string ip, short port, int playercount)
        {
            _playercount = playercount;
            _name = name;
            _ip = ip;
            _port = port;
        }
    }

}