using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Protocol;

namespace InfClient
{
    // PacketFactory Class
    /// Implements the Infantry protocol
    ///////////////////////////////////////////////////////
    public class C2SPacketFactory<T> : IPacketFactory
        where T : IClient
    {	/// <summary>
        /// Creates a new system protocol packet.
        /// </summary>
        public PacketBase createSystemPacket(ushort typeID, byte[] buffer, int offset, int size)
        {	//Ready our packet base
            PacketBase packet = null;
            offset++;
            size--;

            //What are we dealing with?
            switch (typeID)
            {
                case CS_Initial.TypeID:
                    packet = new CS_Initial(typeID, buffer, offset, size);
                    break;

                case BoxPacket.TypeID:
                    packet = new BoxPacket(typeID, buffer, offset, size);
                    break;

                case Disconnect.TypeID:
                    packet = new Disconnect(typeID, buffer, offset, size);
                    break;

                case PingPacket.TypeID:
                    packet = new PingPacket(typeID, buffer, offset, size);
                    break;

                case CS_State.TypeID:
                    packet = new CS_State(typeID, buffer, offset, size);
                    break;

                case Reliable.TypeID:
                    packet = new Reliable(typeID, buffer, offset, size, 0);
                    break;
                case Reliable.TypeID + 1:
                    packet = new Reliable(typeID, buffer, offset, size, 1);
                    break;
                case Reliable.TypeID + 2:
                    packet = new Reliable(typeID, buffer, offset, size, 2);
                    break;
                case Reliable.TypeID + 3:
                    packet = new Reliable(typeID, buffer, offset, size, 3);
                    break;

                case OutOfSync.TypeID:
                    packet = new OutOfSync(typeID, buffer, offset, size, 0);
                    break;
                case OutOfSync.TypeID + 1:
                    packet = new OutOfSync(typeID, buffer, offset, size, 1);
                    break;
                case OutOfSync.TypeID + 2:
                    packet = new OutOfSync(typeID, buffer, offset, size, 2);
                    break;
                case OutOfSync.TypeID + 3:
                    packet = new OutOfSync(typeID, buffer, offset, size, 3);
                    break;

                case ReliableEcho.TypeID:
                    packet = new ReliableEcho(typeID, buffer, offset, size, 0);
                    break;
                case ReliableEcho.TypeID + 1:
                    packet = new ReliableEcho(typeID, buffer, offset, size, 1);
                    break;
                case ReliableEcho.TypeID + 2:
                    packet = new ReliableEcho(typeID, buffer, offset, size, 2);
                    break;
                case ReliableEcho.TypeID + 3:
                    packet = new ReliableEcho(typeID, buffer, offset, size, 3);
                    break;

                case ReliableBox.TypeID:
                    packet = new ReliableBox(typeID, buffer, offset, size, 0);
                    break;
                case ReliableBox.TypeID + 1:
                    packet = new ReliableBox(typeID, buffer, offset, size, 1);
                    break;
                case ReliableBox.TypeID + 2:
                    packet = new ReliableBox(typeID, buffer, offset, size, 2);
                    break;
                case ReliableBox.TypeID + 3:
                    packet = new ReliableBox(typeID, buffer, offset, size, 3);
                    break;

                case DataPacketRcv.TypeID:
                    packet = new DataPacketRcv(typeID, buffer, offset, size, 0);
                    break;
                case DataPacketRcv.TypeID + 1:
                    packet = new DataPacketRcv(typeID, buffer, offset, size, 1);
                    break;
                case DataPacketRcv.TypeID + 2:
                    packet = new DataPacketRcv(typeID, buffer, offset, size, 2);
                    break;
                case DataPacketRcv.TypeID + 3:
                    packet = new DataPacketRcv(typeID, buffer, offset, size, 3);
                    break;

                default:
                    //An undefined packet.
                    packet = new PacketDummy(typeID, buffer, offset, size);
                    break;
            }

            return packet;
        }

        /// <summary>
        /// Creates a new packet based on the typeID and the received content
        /// inside the buffer. The user has to create an own implementation 
        /// of this interface.
        /// </summary>
        public PacketBase createPacket(NetworkClient client, ushort typeID, byte[] buffer, int offset, int size)
        {	//Ready our packet base
            PacketBase packet = null;
            size--;

            //Was it a system packet?
            if (buffer[offset++] == 0)
                //Yes, find the appropriate type
                return createSystemPacket(typeID, buffer, offset, size);

            //So what was the typeid?
            switch (typeID)
            {
                default:
                    //An undefined packet.
                    packet = new PacketDummy(typeID, buffer, offset, size);
                    break;
            }

            return packet;
        }
    }
}
