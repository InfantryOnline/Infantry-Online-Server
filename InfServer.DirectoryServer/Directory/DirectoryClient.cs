using System;
using InfServer.Network;

namespace InfServer.DirectoryServer.Directory
{
    public class DirectoryClient : NetworkClient
    {
        /// <summary>
        /// Token sent by the client at the request of zone list,
        /// and echoed back by the server once the zone list has been sent.
        /// </summary>
        public UInt32 ZoneListToken;

        public override bool checkPacket(byte[] data, ref int offset, ref int count)
        {
            return true;
        }

        public override NetworkClient newInstance()
        {
            return new DirectoryClient();
        }
    }
}
