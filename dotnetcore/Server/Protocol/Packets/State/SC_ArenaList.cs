﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using InfServer.Game;
using InfServer.Network;
using Assets;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_ArenaList contains a list of arenas joinable by the player
	/// </summary>
	public class SC_ArenaList : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public IEnumerable<Arena> arenas;
		public Player requestee;

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.ArenaList;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_ArenaList(IEnumerable<Arena> arenaList, Player forPlayer)
			: base(TypeID)
		{
			arenas = arenaList;
			requestee = forPlayer;
		}

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{	
			//Write out each asset
			foreach (Arena arena in arenas)
			{	//Is he able to see this arena?
				if (!arena.isVisibleToPlayer(requestee) && requestee._arena != arena)
					continue;

				//Not sure why it does this for each entry
				Write((byte)TypeID);

				Write(arena._name, 32);
                //Is he in the arena?
                if (requestee._arena == arena)
                    Write((short)(arena.TotalPlayerCount * -1));
                else
                    Write((short)arena.TotalPlayerCount);
			}
			foreach (CfgInfo.NamedArena arena in requestee._server._namedArenas)
            {
				//Is it active? Handled above
				if (arenas.Count(a => a._name.ToLower() == arena.name.ToLower()) == 1)
					continue;

				//Not sure why it does this for each entry
				Write((byte)TypeID);

				Write(arena.name, 32);
				Write((short)0);
			}
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Arena info.";
			}
		}
	}
}
