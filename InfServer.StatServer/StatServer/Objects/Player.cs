using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfServer.StatServer.Objects
{
    public class Player
    {
        public String name { get; set; }
        public long id { get; set; }


        public class PlayerStats
        {
            public Int32 kills { get; set; }
            public Int32 deaths { get; set; }
            public Int32 points { get; set; }
            public Int32 wins { get; set; }
            public Int32 losses { get; set; }
            public Int32 rating { get; set; }
        }
    }
}
