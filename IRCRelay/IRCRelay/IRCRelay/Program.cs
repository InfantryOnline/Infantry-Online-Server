using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRCRelay
{
    class Program
    {
        static void Main(string[] args)
        {
            Relay ircRelay = new Relay();
            ircRelay.start();
        }
    }
}
