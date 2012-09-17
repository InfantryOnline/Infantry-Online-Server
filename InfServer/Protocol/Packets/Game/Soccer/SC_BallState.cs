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
        public Int16 bPickup;
        public Int16 ballPickupID; // Id of the person with posession of the ball!

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
            Write((byte)TypeID);
            Write((byte)ballID);

            Write(velocityX);
            Write(velocityY);
            Write(velocityZ);



            Write(positionX);
            Write(positionY);
            Write(positionZ);



            Write((byte)playerID); // This is the byte appears to act as if it has to be the ID of the person dropping/pickingup the ball!
            Write((byte)unk1);
            Write((byte)unk2);
            Write((byte)unk3);
            Write((byte)unk4);
            Write((byte)unk5);
            Write((byte)unk6);
            Write((byte)unk7);
            //Write(something1);
            //Write(something2);
            // Skip(2);
            //Write(positionY);
            //Write(positionZ);
            //Skip(2);
            // Write(velocityX);
            // Write(velocityY);
            // Write(velocityZ);
            // Skip(1);
            //Write((byte)1);

        }

        /// <summary>
        /// Returns a meaningful of the packet's data
        /// </summary>
        public override string Dump
        {
            get
            {
                return "Ball state update";
            }
        }

    }
}