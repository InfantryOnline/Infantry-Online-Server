using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfServer.Protocol
{
    /// <summary>
    /// Provides a series of functions for easily serialization of packets
    /// </summary>
    public partial class DBHelpers
    {
        ///<summary>
        ///Houses the packet id enums
        ///</summary>
        public class PacketIDs
        {
            ///<summary>
            ///Contains C2S packet ids
            ///</summary>
            public enum C2S
            {
                Auth = 01,
                PlayerLogin = 02,
                PlayerUpdate = 03,
                PlayerLeave = 04,
                PlayerBanner = 05,
                PlayerStatsRequest = 06,
                ModCommand = 07,
                SquadQuery = 08,
                Whisper = 09,
                PrivateChat = 10,
                JoinChat = 12,
                LeaveChat = 13,
                ZoneList = 15,
                Query = 16,
                Disconnect = 17,
                Ban = 18,
                SquadStats,
                Alias = 20,
                ChatCommand = 21,
                StatsUpdate = 22
            }

            ///<summary>
            ///Contains S2C packet ids
            ///</summary>
            public enum S2C
            {
                Auth = 01,
                PlayerLogin = 02,
                PlayerStatsResponse = 03,
                Whisper = 09,
                PrivateChat = 10,
                JoinChat = 12,
                LeaveChat = 13,
                Chat = 14,
                ZoneList = 15,
                Disconnect = 17,
            }
        }
    }
}
