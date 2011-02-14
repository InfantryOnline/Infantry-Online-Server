using System;
using InfServer.Network;

namespace InfServer.DirectoryServer.Directory.Protocol.Packets
{
    public class SC_ZoneList : PacketBase
    {
        public const ushort TypeID = 3;
        public byte[] data;
        public UInt32 frameNum;
        public UInt32 streamSizeInBytes;

        public SC_ZoneList() : base(TypeID)
        {
        }

        public SC_ZoneList(ushort typeID, byte[] buffer, int index, int count) : base(typeID, buffer, index, count)
        {
        }

        public override void Serialize()
        {
            Write((UInt16) (TypeID << 8));
            Write((short) 0);
            Write((UInt16)frameNum);
            Write(Flip(0x08));
            Write((short) 0);
            Write(streamSizeInBytes);
            Write(data);
        }
    }
}
