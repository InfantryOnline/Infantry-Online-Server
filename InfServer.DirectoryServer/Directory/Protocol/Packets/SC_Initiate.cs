using System;
using InfServer.Network;

namespace InfServer.DirectoryServer.Directory.Protocol.Packets
{
    public class SC_Initiate : PacketBase
    {
        public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.Initiate;

        public UInt32 RandChallengeToken;

        public SC_Initiate() : base(TypeID)
        {
        }

        public SC_Initiate(ushort typeID, byte[] buffer, int index, int count) : base(typeID, buffer, index, count)
        {
        }

        public override void Serialize()
        {
            Write((UInt16)(TypeID << 8));

            // Maybe max packet size?
            Write((byte) 0x42);
            Write((byte) 0x0c);

            Write(Flip(RandChallengeToken));
        }

        public override string Dump
        {
            get { return "Challenge response sent."; }
        }
    }
}
