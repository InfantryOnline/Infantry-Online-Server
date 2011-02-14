using InfServer.DirectoryServer.Directory.Protocol.Packets;
using InfServer.Network;

namespace DirectoryServer.Directory.Protocol
{
    public class Factory : IPacketFactory
    {
        public PacketBase createPacket(NetworkClient client, ushort typeID, byte[] buffer, int index, int count)
        {
            PacketBase packet = null;

            switch(typeID)
            {
                case CS_Initiate.TypeID:
                    packet = new CS_Initiate(typeID, buffer, index, count);
                    break;

                case CS_Version.TypeID:
                    packet = new CS_Version(typeID, buffer, index, count);
                    break;

                case CS_ZoneList.TypeID:
                    packet = new CS_ZoneList(typeID, buffer, index, count);
                    break;

                case CS_AckZoneList.TypeID:
                    packet = new CS_AckZoneList(typeID, buffer, index, count);
                    break;
            }

            return packet;
        }
    }
}
