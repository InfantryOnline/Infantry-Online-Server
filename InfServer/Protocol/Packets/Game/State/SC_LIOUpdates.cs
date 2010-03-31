using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

using Assets;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_LIOUpdates contains updates regarding level interactive objects in the arena 
	/// </summary>
	public class SC_LIOUpdates : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public Arena.SwitchState singleUpdate;
		public IEnumerable<Arena.SwitchState> objects;

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.LIOUpdates;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_LIOUpdates()
			: base(TypeID)
		{
		}

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{	//Just a single update?
			if (singleUpdate != null)
			{
				Write((byte)TypeID);

				Write(singleUpdate.bOpen);
				Write(singleUpdate.Switch.GeneralData.Id);
				return;
			}

			//Write out each asset
			bool bEmpty = true;

			foreach (Arena.SwitchState obj in objects)
			{	//Not sure why it does this for each entry
				Write((byte)TypeID);

				Write(obj.bOpen);
				Write(obj.Switch.GeneralData.Id);

				bEmpty = false;
			}

			if (bEmpty)
				Write((byte)TypeID);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "LIO update.";
			}
		}
	}
}
