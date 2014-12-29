using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{	/// <summary>
    /// CS_Unknown grabs a response from whatever packet you are looking for
    /// </summary>
    public class CS_Unknown : PacketBase
    {	// Member Variables
        ///////////////////////////////////////////////////
        public int unk1;
        public string unk2;

        //Packet routing
        //Switch this to whatever packet you are trying to find
        public const ushort TypeID = (ushort)0x30|0x31|0x32|0x33|0x34|0x35|0x37|0x38;
        static public event Action<CS_Unknown, Player> Handlers;


        ///////////////////////////////////////////////////
        // Member Functions
        //////////////////////////////////////////////////
        /// <summary>
        /// Creates an instance of the dummy packet used to debug communication or 
        /// to represent unknown packets.
        /// </summary>
        /// <param name="typeID">The type of the received packet.</param>
        /// <param name="buffer">The received data.</param>
        public CS_Unknown(ushort typeID, byte[] buffer, int index, int count)
            : base(typeID, buffer, index, count)
        {
        }

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
        {	//Get the information
            Log.write(TLog.Warning, String.Format("Found unknown cs packet {0}", (byte)TypeID));
        }

        /// <summary>
        /// Returns a meaningful of the packet's data
        /// </summary>
        public override string Dump
        {
            get
            {
                return "Unknown Packet Testing";
            }
        }
    }
}
