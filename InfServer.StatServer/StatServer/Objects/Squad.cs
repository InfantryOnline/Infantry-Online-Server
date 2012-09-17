using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfServer.StatServer.Objects
{
    public class Squad
    {
        public long id { get; set; }
        public long statsID { get; set; }
        public String name { get; set; }
        public String captain { get; set; }
        public SquadStats stats { get; set; }

        public class SquadStats
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
