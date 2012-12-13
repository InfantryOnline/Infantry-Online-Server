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
    public class CS_BallDrop : PacketBase
    {	// Member Variables
        ///////////////////////////////////////////////////
        public Int16 bPickup;		//Pick up or put down?
        public Int16 ballID;
        public Int16 playerID;
        public Int16 velocityX;
        public Int16 velocityY;
        public Int16 velocityZ;
        public Int16 positionX;
        public Int16 positionY;
        public Int16 positionZ;
        public Int16 unk1;
        public Int16 unk2;
        public Int16 unk3;
        public Int16 unk4;
        public Int16 unk5;
        public Int16 unk6;
        public Int16 unk7;
        public bool bSuccess;		//Was it a successful drop?
        //Packet routing
        public const ushort TypeID = (ushort)Helpers.PacketIDs.C2S.BallDrop;
        static public event Action<CS_BallDrop, Player> Handlers;


        ///////////////////////////////////////////////////
        // Member Functions
        //////////////////////////////////////////////////
        /// <summary>
        /// Creates an empty packet of the specified type. This is used
        /// for constructing new packets for sending.
        /// </summary>
        public CS_BallDrop()
            : base(TypeID)
        {
        }

        /// <summary>
        /// Creates an instance of the dummy packet used to debug communication or 
        /// to represent unknown packets.
        /// </summary>
        /// <param name="typeID">The type of the received packet.</param>
        /// <param name="buffer">The received data.</param>
        public CS_BallDrop(ushort typeID, byte[] buffer, int index, int count)
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

            Write((byte)12);
            Write((byte)12);
            Write((byte)12);
            Log.write(BitConverter.ToString(Data));
        }

        /// <summary>
        /// Deserializes the data present in the packet contents into data fields in the class.
        /// </summary>
        public override void Deserialize()
        {
            //NOTE - 15th byte is the playerID who drops it
            //NOTE - 2nd byte is ballID
            bPickup = _contentReader.ReadByte(); // wtf is this?
            ballID = _contentReader.ReadByte();
            velocityX = _contentReader.ReadInt16();
            velocityY = _contentReader.ReadInt16();
            velocityZ = _contentReader.ReadInt16();
            positionX = _contentReader.ReadInt16();
            positionY = _contentReader.ReadInt16();
            positionZ = _contentReader.ReadInt16();
            playerID = _contentReader.ReadByte();
            unk1 = _contentReader.ReadByte();
            unk2 = _contentReader.ReadByte();
            unk3 = _contentReader.ReadByte();
            unk4 = _contentReader.ReadByte();
            unk5 = _contentReader.ReadByte();
            unk6 = _contentReader.ReadByte();
            unk7 = _contentReader.ReadByte();
            Log.write(String.Format("balllll DROP123 {0}-{1}-{2}", playerID, positionY, positionZ));
            Log.write(DataDump);
        }

        /// <summary>
        /// Returns a meaningful of the packet's data
        /// </summary>
        public override string Dump
        {
            get
            {

                return String.Format("balllll DROP {0}-{1}-{2}", positionX, positionY, positionZ);
            }
        }
    }
}