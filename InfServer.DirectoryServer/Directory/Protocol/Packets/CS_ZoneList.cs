using System;
using InfServer.Network;

namespace InfServer.DirectoryServer.Directory.Protocol.Packets
{
    public class CS_ZoneList : PacketBase
    {
        static public event Action<CS_ZoneList, DirectoryClient> Handlers;

        public const ushort TypeID = (ushort)Helpers.PacketIDs.C2S.ZoneList;
        public UInt32 Token;

        public CS_ZoneList() : base(TypeID)
        {
        }

        public CS_ZoneList(ushort typeID, byte[] buffer, int index, int count) : base(typeID, buffer, index, count)
        {
        }

        public override void Route()
        {	//Call all handlers!
            if (Handlers != null)
                Handlers(this, ((DirectoryClient)_client));
        }

        public override void Deserialize()
        {
            _contentReader.ReadUInt32(); // Discard leading data
            Token = Flip(_contentReader.ReadUInt32());
        }

        public override string Dump
        {
            get { return "Zone list request."; }
        }
    }
}
