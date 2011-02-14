using System;
using System.Collections.Generic;
using InfServer.DirectoryServer.Directory.Protocol.Packets;

namespace InfServer.DirectoryServer.Directory.Logic.Packets
{
    public class ZoneListing
    {
        public static void Handle_CS_ZoneList(CS_ZoneList pkt, DirectoryClient client)
        {
            // Begin sending the zone list packets
            client.ZoneListToken = pkt.Token;

            // Send the first packet
            List<SC_ZoneList> packets = Program.server.ZoneStream.Packets;

            if(packets.Count != 0)
                client.send(packets[0]);
        }

        public static void Handle_CS_AckZoneList(CS_AckZoneList pkt, DirectoryClient client)
        {
            // Do we have more packets to send?
            UInt16 frame = pkt.frameReceived++;

            List<SC_ZoneList> packets = Program.server.ZoneStream.Packets;

            if (frame < packets.Count)
            {
                client.send(packets[frame]);
            }
        }

        [Directory.RegistryFunc]
        static public void Register()
        {
            CS_ZoneList.Handlers += Handle_CS_ZoneList;
            CS_AckZoneList.Handlers += Handle_CS_AckZoneList;
        }
    }
}
