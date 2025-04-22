using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipeComm
{
    public class Message : Packet
    {
        public string Text { get; set; }
    }
}
