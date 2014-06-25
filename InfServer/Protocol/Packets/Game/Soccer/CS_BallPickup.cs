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
    public class CS_BallPickup : PacketBase
    {	// Member Variables
        ///////////////////////////////////////////////////
        public Int16 bPickup;		//Pick up or put down?
        public UInt16 ballID;
        public UInt32 test;
        public Int16 ntest;
        public UInt16 playerID;
        public Int16 unk1;
        public Int16 unk2;
        public Int16 unk3;
        public Int16 unk4;
        public Int32 tickcount;
        public bool bSuccess;		//Was it a successful drop?
        //Packet routing
        public const ushort TypeID = (ushort)Helpers.PacketIDs.C2S.BallPickup;
        static public event Action<CS_BallPickup, Player> Handlers;


        ///////////////////////////////////////////////////
        // Member Functions
        //////////////////////////////////////////////////
        /// <summary>
        /// Creates an empty packet of the specified type. This is used
        /// for constructing new packets for sending.
        /// </summary>
        public CS_BallPickup()
            : base(TypeID)
        {
        }

        /// <summary>
        /// Creates an instance of the dummy packet used to debug communication or 
        /// to represent unknown packets.
        /// </summary>
        /// <param name="typeID">The type of the received packet.</param>
        /// <param name="buffer">The received data.</param>
        public CS_BallPickup(ushort typeID, byte[] buffer, int index, int count)
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
            //6 bytes = max read for this
            //Kon, this is the max read for pickup.. anymore and its beyond the stream length
            ballID = _contentReader.ReadUInt16(); // this is 100% the ballID
            //Skip(1); //always 0
            //test = _contentReader.ReadUInt32();
            //ntest = _contentReader.ReadByte();
            //UInt16 tests = test;
            //playerID = test;
            //Log.write(TLog.Warning, "test {0} ntest {1}", test, 0);
            //unk1 = _contentReader.ReadByte(); // This is the EXACT same 4 bytes as the one present in the 19th/20th/21st/22nd byte of ballstate
            //unk2 = _contentReader.ReadByte(); // This is the EXACT same 4 bytes as the one present in the 19th/20th/21st/22nd byte of ballstate
            //unk3 = _contentReader.ReadByte(); // This is the EXACT same 5 bytes as the one present in the 19th/20th/21st/22nd byte of ballstate
            //unk4 = _contentReader.ReadByte(); // This is the EXACT same 5 bytes as the one present in the 19th/20th/21st/22nd byte of ballstate
            //ntest = _contentReader.ReadInt16();
            tickcount = _contentReader.ReadInt32();
            /*
            TimeSpan ts = new TimeSpan(0);
            double ms = ts.TotalMilliseconds;
            ms = ms / 1000;
            Log.write(String.Format("balllll PICKUP bID {0} test {1} unk1 {2} unk2 {3} unk3 {4} unk4 {5} tickcount {6} current tick {7} converted {8}", ballID, test, unk1, unk2, unk3, unk4, tickcount, Environment.TickCount, ms));
            Log.write(String.Format("playerID {0} ntest {1}", playerID, ntest));
            Log.write(DataDump);
             */
        }

        /// <summary>
        /// Returns a meaningful of the packet's data
        /// </summary>
        public override string Dump
        {
            get
            {
                return String.Format("Ball Pickup, Ball ID: {0}", ballID);
            }
        }
    }
}