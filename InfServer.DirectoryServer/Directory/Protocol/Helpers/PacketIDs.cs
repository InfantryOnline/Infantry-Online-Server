using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfServer.DirectoryServer.Directory.Protocol.Helpers
{
    ///<summary>
    ///Houses the packet id enums
    ///</summary>
    public class PacketIDs
    {
        ///<summary>
        ///Contains C2S packet ids
        //////</summary>
        public enum C2S
        {
            Initiate = 0x01,
            Version = 0x03,
            ZoneList = 0x05,
            AckZoneList = 0x0B,
        }
        ///<summary>
        ///Contains S2C packet ids
        //////</summary>
        public enum S2C
        {
            Initiate = 0x02,
            ZoneList = 0x03,
        }
    }
}
