using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{	/// <summary>
    /// CS_RegResponse retrieves the query made to a player's registry
    /// </summary>
    public class CS_RegResponse : PacketBase
    {	// Member Variables
        ///////////////////////////////////////////////////
        public Int16 unk1, unk2;
        public Int32 unk3;
        public string RegistryValue;

        public const ushort TypeID = (ushort)Helpers.PacketIDs.C2S.RegResponse;
        static public Action<CS_RegResponse, Player> Handlers;

        ///////////////////////////////////////////////////
        // Member Functions
        //////////////////////////////////////////////////
        /// <summary>
        /// Creates an empty packet of the specified type.
        /// </summary>
        public CS_RegResponse(ushort typeID, byte[] buffer, int index, int count)
			: base(typeID, buffer, index, count)
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
		/// Deserializes the data present in the packet contents into data fields in the class.
		/// </summary>
		public override void Deserialize()
		{   //16 bytes = max
            Console.WriteLine("Found");
            unk1 = _contentReader.ReadInt16();
            unk2 = _contentReader.ReadInt16();
            RegistryValue = ReadNullString();
            Console.WriteLine(RegistryValue);
        }

        /// <summary>
        /// Returns a meaningful of the packet's data
        /// </summary>
        public override string Dump
        {
            get
            {
                return "Registry response";
            }
        }
    }
}
