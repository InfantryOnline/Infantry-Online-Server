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
        public UInt16 ballID;
        public UInt16 playerID;
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
        public Int16 uTest;
        public Int16 nTest;
        public Int32 tickcount;
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
            //22 bytes is the max read
            //NOTE - 15th byte is the playerID who drops it
            //NOTE - 2nd byte is ballID
            bPickup = _contentReader.ReadByte(); // wtf is this? // always zero ?
            ballID = _contentReader.ReadByte();
            velocityX = _contentReader.ReadInt16();
            velocityY = _contentReader.ReadInt16();
            velocityZ = _contentReader.ReadInt16();
            positionX = _contentReader.ReadInt16(); // Confirmed
            positionY = _contentReader.ReadInt16(); // Confirmed
            positionZ = _contentReader.ReadInt16(); // Confirmed
            playerID = _contentReader.ReadByte();
//            uTest = _contentReader.ReadInt16();
            unk1 = _contentReader.ReadByte();
            unk2 = _contentReader.ReadByte();
            unk3 = _contentReader.ReadByte();
            unk4 = _contentReader.ReadByte();
            unk5 = _contentReader.ReadByte();
            unk6 = _contentReader.ReadByte();
            unk7 = _contentReader.ReadByte();
            //tickcount = _contentReader.ReadInt32();
            string format = String.Format("velX {0} velY {1} velZ {2} posX {3} posY {4} posZ {5}", velocityX, velocityY, velocityZ, positionX, positionY, positionZ);
            Log.write(String.Format("balllll DROP123 bID {0} {1} pID {2} bPickup {3} unk1 {4} unk2 {5} unk3 {6} unk4 {7} unk5 {8} unk6 {9} unk7 {10} time {11} current time {12} uTest {13} nTest {14}", ballID, format, playerID, bPickup, unk1, unk2, unk3, unk4, unk5, unk6, unk7, tickcount, (Environment.TickCount),uTest,nTest));
            Log.write("BallDrop {0}",DataDump);
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