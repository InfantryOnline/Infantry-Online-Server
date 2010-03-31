using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

using Assets;

namespace InfServer.Protocol
{	/// <summary>
	/// Provides a series of functions for easily serialization of packets
	/// </summary>
	public partial class Helpers
	{	// Member Classes
		//////////////////////////////////////////////////
		/// <summary>
		/// Houses the packet id enums
		/// </summary>
		public class PacketIDs
		{
			/// <summary>
			/// Contains C2S packet IDs
			/// </summary>
			public enum C2S
			{
				Login = 0x01,
				PlayerJoin = 0x0A,
				StartUpdate = 0x0B,
				PlayerPickup = 0x0E,
				PlayerDrop = 0x17,
				Chat = 0x18,
				Ready = 0x0C,
				Shop = 0x05,
				Explosion = 0x06,
				ShopSkill = 0x07,
				PlayerSwitch = 0x08,
				PlayerFlag = 0x09,
				Frames = 0x15,
				PlayerUpdate = 0x1C,
				RequestUpdate = 0x1E,
				PlayerDeath = 0x21,
				PlayerPortal = 0x22,
				Security = 0x25,
				PlayerUseItem = 0x0F,
			}

			/// <summary>
			/// Contains S2C packet IDs
			/// </summary>
			public enum S2C
			{
				Login = 0x01,
				PlayerEnter = 0x02,
				PlayerLeave = 0x03,
				EnterArena = 0x04,
				ArenaMessage = 0x05,
				PatchInfo = 0x07,
				BindVehicle = 0x0B,
				ChangeTeam = 0x0C,
				PlayerState = 0x0D,
				PlayerDeath = 0x0F,
				Items = 0x10,
				ItemDrop = 0x11,
				Inventory = 0x12,
				ShopFinished = 0x15,
				GameReset = 0x16,
				LIOUpdates = 0x19,
				Flags = 0x1A,
				PlayerUpdate = 0x1B,
				SetInGame = 0x1D,
				AssetUpdateInfo = 0x20,
				Chat = 0x23,
				VehicleDestroy = 0x24,
				Vehicles = 0x25,
				AssetUpdate = 0x26,
				AssetInfo = 0x27,
				PlayerTeamUpdate = 0x28,
				PlayerWarp = 0x2C,
				ItemReload = 0x33,
			}
		}
	}
}
