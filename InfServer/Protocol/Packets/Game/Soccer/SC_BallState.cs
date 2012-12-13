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
            Write((byte)TypeID); // The type of the packet
            Write((byte)ballID); // The ID of the ball we're updateing
            if (velocityX == 0)
            {
                Skip(2);
            }
            else
            {
                Write(velocityX);
            }
            if (velocityY == 0)
            {
                Skip(2);
            }
            else
            {
                Write(velocityY);
            }
            if (velocityZ == 0)
            {
                Skip(2);
            }
            else
            {
                Write(velocityZ);
            }
            if (positionX == 0)
            {
                Skip(2);
            }
            else
            {
                Write(positionX);
            }
            if (positionY == 0)
            {
                Skip(2);
            }
            else
            {
                Write(positionY);
            }
            if (positionZ == 0)
            {
                Skip(2);
            }
            else
            {
                Write(positionZ);
            }
            Write((byte)playerID); // This is the byte appears to act as if it has to be the ID of the person dropping/pickingup the ball. Maybe used for the re-pickup delay
            Write((byte)unk1);
            if (unk2 == -1) // We use this, somehow, to make the ball packet work for pickups and drops etc. For some reason having 0's works in some cases.
            {
                //Write((byte)playerID);
                //Skip(2);
            }
            else if (unk2 == 0)
            {
                //Write((byte)playerID);
                Skip(6);
            }
            else
            {
                Write((byte)unk2);
                Write((byte)unk3);
                Write((byte)unk4);
                Write((byte)unk5);
                Write((byte)unk6);
                Write((byte)unk7);
            }

            //Log.write(DataDump);
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