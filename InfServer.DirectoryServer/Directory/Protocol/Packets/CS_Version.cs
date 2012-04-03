using InfServer.Network;

namespace InfServer.DirectoryServer.Directory.Protocol.Packets
{
    public class CS_Version : PacketBase
    {
        public const ushort TypeID = (ushort)Helpers.PacketIDs.C2S.Version;

        public CS_Version() : base(TypeID)
        {
        }

        public CS_Version(ushort typeID, byte[] buffer, int index, int count) : base(typeID, buffer, index, count)
        {
        }

        public override string Dump
        {
            get { return "Version received."; }
        }
    }
}
