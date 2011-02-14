using System;
using InfServer.Network;

namespace InfServer.DirectoryServer.Directory.Protocol.Packets
{
    public class CS_AckZoneList : PacketBase
    {
        static public event Action<CS_AckZoneList, DirectoryClient> Handlers;

        public const ushort TypeID = 0x0b;

        public UInt16 frameReceived;

        public CS_AckZoneList() : base(TypeID)
        {
        }

        public CS_AckZoneList(ushort typeID, byte[] buffer, int index, int count) : base(typeID, buffer, index, count)
        {
        }

        public override void Route()
        {	//Call all handlers!
            if (Handlers != null)
                Handlers(this, ((DirectoryClient)_client));
        }

        public override void Deserialize()
        {
            Skip(4);
            frameReceived = _contentReader.ReadUInt16();
        }

        public override string Dump
        {
            get { return String.Format("Frame Ack Received: {0}", frameReceived); }
        }
    }
}
