using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Protocol;

namespace InfServer.Data
{
	// PacketFactory Class
	/// Implements the Infantry protocol
	///////////////////////////////////////////////////////
	public class S2CPacketFactory<T> : IPacketFactory
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
				case SC_Initial.TypeID:
					packet = new SC_Initial(typeID, buffer, offset, size);
					break;

				case SC_State.TypeID:
					packet = new SC_State(typeID, buffer, offset, size);
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

				case Reliable.TypeID:
					packet = new Reliable(typeID, buffer, offset, size);
					break;

				case OutOfSync.TypeID:
					packet = new OutOfSync(typeID, buffer, offset, size);
					break;

				case ReliableEcho.TypeID:
					packet = new ReliableEcho(typeID, buffer, offset, size);
					break;

				case ReliableBox.TypeID:
					packet = new ReliableBox(typeID, buffer, offset, size);
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
				case SC_Auth<T>.TypeID:
					packet = new SC_Auth<T>(typeID, buffer, offset, size);
					break;

				case SC_PlayerLogin<T>.TypeID:
					packet = new SC_PlayerLogin<T>(typeID, buffer, offset, size);
					break;

				default:
					//An undefined packet.
					packet = new PacketDummy(typeID, buffer, offset, size);
					break;
			}

			return packet;
		}
	}
}
