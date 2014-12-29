using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{	/// <summary>
    /// SC_BallState contains updates regarding balls in the arena
    /// </summary>
    public class SC_BallState : PacketBase
    {	// Member Variables
        ///////////////////////////////////////////////////
        public bool bDisable;                       //Are we resetting?

        public Ball singleUpdate;                   //Which ball we are updating
        public IEnumerable<Ball> balls;             //List of balls to update

        public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.BallState;

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
            //A single update?
            if (singleUpdate != null)
            {
                Write((byte)TypeID);
                Write((byte)singleUpdate._id); //The id of the ball we're updating
                Write(singleUpdate._state.velocityX);
                Write(singleUpdate._state.velocityY);
                Write(singleUpdate._state.velocityZ);
                Write(singleUpdate._state.positionX);
                Write(singleUpdate._state.positionY);
                Write(singleUpdate._state.positionZ);

                //Are we being held? (unk0 + unk1)
                if (singleUpdate._owner != null)
                    Write(singleUpdate._owner._id);
                else
                {
                    if (bDisable)
                    {
                        Write((short)-1);
                        Skip(6);
                        return;
                    }

                    //Send who held it last
                    Write((ushort)(singleUpdate._lastOwner != null ? singleUpdate._lastOwner._id : 0)); //unk0 + unk1
                }

                //Lets see whats going on with the ball
                switch (singleUpdate.ballStatus)
                {
                    case -1: //Spawning
                        Write(singleUpdate.ballFriction); //unk2 + unk3
                        Write(singleUpdate.tickCount); //unk4 - 7
                        break;

                    case 0: //Picked up
                        Skip(6);
                        break;

                    case 1: //Dropped
                        Write(singleUpdate.ballFriction);
                        Write(singleUpdate.tickCount);
                        break;
                }
                return;
            }

            foreach (Ball ball in balls)
            {
                Write((byte)TypeID);
                Write((byte)ball._id); // The ID of the ball we're updating
                Write(ball._state.velocityX);
                Write(ball._state.velocityY);
                Write(ball._state.velocityZ);
                Write(ball._state.positionX);
                Write(ball._state.positionY);
                Write(ball._state.positionZ);

                //Are we being held? (unk0 + unk1)
                if (ball._owner != null)
                    Write(ball._owner._id);
                else
                {
                    if (bDisable)
                    {
                        Write((short)-1);
                        Skip(6);
                        return;
                    }
                    //Send who held it last
                    Write((ushort)(ball._lastOwner != null ? ball._lastOwner._id : 0)); //unk0 + unk1
                }

                //Lets see whats going on with the ball
                switch (ball.ballStatus)
                {
                    case -1: //Spawning
                        Write(ball.ballFriction); //unk2 + unk3
                        Write(ball.tickCount); //unk4 - 7
                        break;

                    case 0: //Picked up
                        Skip(6);
                        break;

                    case 1: //Dropped
                        Write(ball.ballFriction);
                        Write(ball.tickCount);
                        break;
                }
            }
        }

        /// <summary>
        /// Returns a meaning of the packet's data
        /// </summary>
        public override string Dump
        {
            get
            {
                return "Ball State Update";
            }
        }

    }
}