using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;


namespace InfServer.Protocol
{	/// <summary>
	/// SC_ConfirmFileSend used for testing client functionality
	/// </summary>
	public class CS_AllowSpec : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
        public bool bAllowSpec;

		public const ushort TypeID = (ushort)Helpers.PacketIDs.C2S.AllowSpec;
        static public event Action<CS_AllowSpec, Player> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
        public CS_AllowSpec()
			: base(TypeID)
		{ }

        /// <summary>
        /// Routes a new packet to various relevant handlers
        /// </summary>
        public override void Route()
        {	//Call all handlers!
            if (Handlers != null)
                Handlers(this, ((Client<Player>)_client)._obj);
        }

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{
			Write((byte)TypeID);
            Write(bAllowSpec);
		}

		/// <summary>
		/// Deserializes the data present in the packet contents into data fields in the class.
		/// </summary>
        public override void Deserialize()
        {
            bAllowSpec = _contentReader.ReadBoolean();
        }

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Allow/Disallow Spectator Request";
			}
		}
	}
}
