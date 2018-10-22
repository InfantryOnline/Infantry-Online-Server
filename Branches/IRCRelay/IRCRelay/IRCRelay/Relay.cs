using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace IRCRelay
{
    public class Relay
    {
        public IRC irc;

        public Relay()
        {
        }

        public void start()
        {
            irc = new IRC("RelayBot", "#test");
            irc.Connect("ircd.suroot.info", 6667);
        }

        public void sendMessage(string message)
        {
            irc.IrcWriter.WriteLine(String.Format("PRIVMSG {0} {1}", irc.IrcChannel, message));
        }
    }
}
