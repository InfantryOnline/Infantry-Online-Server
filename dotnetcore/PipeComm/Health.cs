using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipeComm
{
    public class Health : Packet
    {
        public int ProcessId { get; set; }

        public int Echo { get; set; }
    }
}
