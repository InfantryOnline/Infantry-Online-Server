using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{	/// <summary>
    /// CS_PlayerUpdate contains player statistical information for updating
    /// </summary>
    public class CS_GoalScored : PacketBase
    {	// Member Variables
        ///////////////////////////////////////////////////
        public Int16 bPickup;		//Pick up or put down?
        public Int16 ballID;
        public Int16 playerID;
        public Int16 teamScoredID;
        public Int16 positionX;
        public Int16 positionY;
        public Int16 positionZ;
        public bool bSuccess;		//Was it a successful drop?
        //Packet routing
        public const ushort TypeID = (ushort)Helpers.PacketIDs.C2S.GoalScored;
        static public event Action<CS_GoalScored, Player> Handlers;


        ///////////////////////////////////////////////////
        // Member Functions
        //////////////////////////////////////////////////
        /// <summary>
        /// Creates an empty packet of the specified type. This is used
        /// for constructing new packets for sending.
        /// </summary>
        public CS_GoalScored()
            : base(TypeID)
        {
        }

        /// <summary>
        /// Creates an instance of the dummy packet used to debug communication or 
        /// to represent unknown packets.
        /// </summary>
        /// <param name="typeID">The type of the received packet.</param>
        /// <param name="buffer">The received data.</param>
        public CS_GoalScored(ushort typeID, byte[] buffer, int index, int count)
            : base(typeID, buffer, index, count)
        {
        }

        /// <summary>
        /// Routes a new packet to various relevant handlers
        /// </summary>
        public override void Route()
        {	//Call all handlers!
            if (Handlers != null)
                Handlers(this, (_client as Client<Player>)._obj);
        }

        /// <summary>
        /// Serializes the data stored in the packet class into a byte array ready for sending.
        /// </summary>
        public override void Serialize()
        {	//Type ID


            Log.write(BitConverter.ToString(Data));
        }

        /// <summary>
        /// Deserializes the data present in the packet contents into data fields in the class.
        /// </summary>
        public override void Deserialize()
        {
            //bPickup = _contentReader.ReadBoolean();
            ballID = _contentReader.ReadByte(); // Ball id
            playerID = _contentReader.ReadByte(); // Last 4 of ballstate
            playerID = _contentReader.ReadByte();// Last 4 of ballstate
            playerID = _contentReader.ReadByte();// Last 4 of ballstate
            playerID = _contentReader.ReadByte();// Last 4 of ballstate
            playerID = _contentReader.ReadByte();// unknown
            teamScoredID = _contentReader.ReadByte();// team who scored ID
            playerID = _contentReader.ReadByte();// seems to be a reference to where the ball crosses the line? maybe uses next byte as well?
            playerID = _contentReader.ReadByte();// unknown
            Log.write(DataDump);

        }

        /// <summary>
        /// Returns a meaningful of the packet's data
        /// </summary>
        public override string Dump
        {
            get
            {

                return String.Format("goal SCORED {0}-{1}-{2}", ballID, positionX, positionY);
            }
        }
    }
}