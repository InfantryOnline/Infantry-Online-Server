using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{	/// <summary>
    /// SC_BallState contains updates regarding ball state
    /// </summary>
    public class SC_BallState : PacketBase
    {	// Member Variables
        ///////////////////////////////////////////////////

        public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.BallState;
        public ushort ballID;
        public Int16 playerID;
        public Int16 positionX;
        public Int16 positionY;
        public Int16 positionZ;
        public Int16 velocityX;
        public Int16 velocityY;
        public Int16 velocityZ;
        public Int32 TimeStamp;
        public Int32 something1;
        public Int16 something2;
        public Int16 unk1;
        public Int16 unk2;
        public Int16 unk3;
        public Int16 unk4;
        public Int16 unk5;
        public Int16 unk6;
        public Int16 unk7;
        public Int16 delete; // if set to 1, delete it
        public Int16 bPickup;
        public Int16 ballPickupID; // Id of the person with posession of the ball! //kon - this is playerid?
        public UInt16 test;

        ///////////////////////////////////////////////////
        // Member Functions
        //////////////////////////////////////////////////
        /// <summary>
        /// Creates an empty packet of the specified type. This is used
        /// for constructing new packets for sending.
        /// </summary>
        public SC_BallState()
            : base(TypeID)
        {
        }

        /// <summary>
        /// Serializes the data stored in the packet class into a byte array ready for sending.
        /// </summary>
        public override void Serialize()
        {
            Write((byte)TypeID); // The type of the packet
            Write((byte)ballID); // The ID of the ball we're updating
            Write(velocityX);
            Write(velocityY);
            Write(velocityZ);
            Write(positionX); //Confirmed
            Write(positionY); //Confirmed
            Write(positionZ); //Confirmed

            // This is the byte appears to act as if it has to be the ID of the person dropping/pickingup the ball. Maybe used for the re-pickup delay
            Write((byte)playerID); //??trying to send playerid instead of 0 -- no difference so far
            Write((byte)unk1); //Has to be 0 otherwise you cant pick the ball up Note: putting this to -1 wont show even on radar

            if (unk2 == -1) // We use this, somehow, to make the ball packet work for pickups and drops etc. For some reason having 0's works in some cases.
            {//confirmed -- this is used for spawning a new ball
                Write((byte)unk2);
                Write((byte)unk3);
                //Kon 4-7 is ticks.. reversing goal packet helped me solve this but i believe there is a switch involved with creating and owning
                Write((byte)unk4);
                Write((byte)unk5);
                Write((byte)unk6);
                unk7 = 1;
                Write((byte)unk7);
            }
            else if (unk2 == 0)//This is the one for pickups, -1 will glitch everything
            {
                //Write((byte)playerID);
                Log.write("SC 0");
                Skip(6);
            }
            else
            {   //Performed when dropping ball
                Log.write("SC else");
                Write((byte)unk2);
                Write((byte)unk3); // This has to do with how far the ball will travel upon dropping the ball, the higher the number.. the shorter the distance
                Write((byte)unk4);
                Write((byte)unk5);
                Write((byte)unk6);
                Write((byte)unk7); // This has to do with making the ball visible, set to 0 = cant see except radar?, set to any other number and it shows up
            }
            //write three more bytes
            string mat = String.Format("something1 {0} somthing2 {1} something3 {2}", something2, something2, something2);
            string format = String.Format("velX {0} velY {1} velZ {2} posX {3} posY {4} posZ {5}", velocityX, velocityY, velocityZ, positionX, positionY, positionZ);
            Log.write("SC_BallState bID {0} {1} pID {2} unk1 {3} unk2 {4} unk3 {5} unk4 {6} unk5 {7} unk6 {8} unk7 {9} tickcount {10} {11}", ballID, format, playerID, unk1, unk2, unk3, unk4, unk5, unk6, unk7, TimeStamp, mat);
            
            Log.write("Sending SC_BallState {0}", DataDump);
        }

        public override string Dump
        {
            get
            {
                return "Ball state update";
            }
        }

    }
}