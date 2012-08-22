using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{
	// PacketFactory Class
	/// Implements the Infantry protocol
	///////////////////////////////////////////////////////
	public class PacketFactory : IPacketFactory
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
				case CS_Login.TypeID:
					packet = new CS_Login(typeID, buffer, offset, size);
					break;

				case CS_ArenaJoin.TypeID:
					packet = new CS_ArenaJoin(typeID, buffer, offset, size);
					break;

				case CS_PlayerJoin.TypeID:
					packet = new CS_PlayerJoin(typeID, buffer, offset, size);
					break;

				case CS_Ready.TypeID:
					packet = new CS_Ready(typeID, buffer, offset, size);
					break;

				case CS_PlayerPickup.TypeID:
					packet = new CS_PlayerPickup(typeID, buffer, offset, size);
					break;

				case CS_PlayerDrop.TypeID:
					packet = new CS_PlayerDrop(typeID, buffer, offset, size);
					break;

				case CS_Chat.TypeID:
					packet = new CS_Chat(typeID, buffer, offset, size);
					break;

				case CS_PlayerUseItem.TypeID:
					packet = new CS_PlayerUseItem(typeID, buffer, offset, size);
					break;

				case CS_Explosion.TypeID:
					packet = new CS_Explosion(typeID, buffer, offset, size);
					break;

				case CS_PlayerUpdate.TypeID:
					packet = new CS_PlayerUpdate(typeID, buffer, offset, size);
					break;

				case CS_Shop.TypeID:
					packet = new CS_Shop(typeID, buffer, offset, size);
					break;

				case CS_ShopSkill.TypeID:
					packet = new CS_ShopSkill(typeID, buffer, offset, size);
					break;

				case CS_PlayerSwitch.TypeID:
					packet = new CS_PlayerSwitch(typeID, buffer, offset, size);
					break;

				case CS_PlayerFlag.TypeID:
					packet = new CS_PlayerFlag(typeID, buffer, offset, size);
					break;

				case CS_RequestUpdate.TypeID:
					packet = new CS_RequestUpdate(typeID, buffer, offset, size);
					break;

				case CS_StartUpdate.TypeID:
					packet = new CS_StartUpdate(typeID, buffer, offset, size);
					break;

				case CS_VehicleDeath.TypeID:
					packet = new CS_VehicleDeath(typeID, buffer, offset, size);
					break;

				case CS_PlayerPortal.TypeID:
					packet = new CS_PlayerPortal(typeID, buffer, offset, size);
					break;

                case CS_AllowSpectator.TypeID:
                    packet = new CS_AllowSpectator(typeID, buffer, offset, size);
                    break;

				case CS_RequestSpectator.TypeID:
					packet = new CS_RequestSpectator(typeID, buffer, offset, size);
					break;

				case CS_ItemExpired.TypeID:
					packet = new CS_ItemExpired(typeID, buffer, offset, size);
					break;

				case CS_PlayerProduce.TypeID:
					packet = new CS_PlayerProduce(typeID, buffer, offset, size);
					break;

				case CS_SetBanner.TypeID:
					packet = new CS_SetBanner(typeID, buffer, offset, size);
					break;

				case CS_FileSend.TypeID:
					packet = new CS_FileSend(typeID, buffer, offset, size);
					break;

				case CS_Environment.TypeID:
					packet = new CS_Environment(typeID, buffer, offset, size);
					break;

				case CS_Frames.TypeID:
					packet = new CS_Frames(typeID, buffer, offset, size);
					break;

				case CS_ChartRequest.TypeID:
					packet = new CS_ChartRequest(typeID, buffer, offset, size);
					break;

                case CS_SecurityCheck.TypeID:
                    packet = new CS_SecurityCheck(typeID, buffer, offset, size);
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
