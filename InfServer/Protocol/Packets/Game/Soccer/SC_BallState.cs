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

                //Are we resetting?
                if (bDisable)
                {
                    Write((short)-1); //No player id necessary (unk0 + unk1)
                    Skip(6); //unk2 + unk3 + unk4 - 7
                    return;
                }

                //Lets see whats going on with the ball
                switch (singleUpdate.ballStatus)
                {
                    case -1: //Spawning
                        Write((short)-1); //unk0 + unk1 aka player id
                        Write(singleUpdate.ballFriction); //unk2 + unk3
                        Write(singleUpdate.tickCount); //unk4 - 7
                        break;

                    case 0: //Picked up
                        if (singleUpdate._owner != null)
                        {
                            Write(singleUpdate._owner._id); //unk0 + unk1 aka player id
                        }
                        else
                        {
                            Write((ushort)(singleUpdate._lastOwner != null ? singleUpdate._lastOwner._id : -1)); //unk0 + unk1 aka player id
                        }
                        Skip(6); //unk2 + unk3 + unk4 - 7
                        break;

                    case 1: //Dropped
                        if (singleUpdate._lastOwner != null)
                        {
                            Write(singleUpdate._lastOwner._id);
                        }
                        else
                        {
                            Write((ushort)(singleUpdate._owner != null ? singleUpdate._owner._id : -1));
                        }
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

                //Are we resetting?
                if (bDisable)
                {
                    Write((short)-1); //No player id necessary (unk0 + unk1)
                    Skip(6); //unk2 + unk3 + unk4 - 7
                    return;
                }

                //Lets see whats going on with the ball
                switch (ball.ballStatus)
                {
                    case -1: //Spawning
                        Write((short)-1); //No player id necessary (unk0 + unk1)
                        Write(ball.ballFriction); //unk2 + unk3
                        Write(ball.tickCount); //unk4 - 7
                        break;

                    case 0: //Picked up
                        if (ball._owner != null)
                        {
                            Write(ball._owner._id); //unk0 + unk1 aka player id
                        }
                        else
                        {
                            Write((ushort)(ball._lastOwner != null ? ball._lastOwner._id : -1)); //unk0 + unk1 aka player id
                        }
                        Skip(6); //unk2 + unk3 + unk4 - 7
                        break;

                    case 1: //Dropped
                        if (ball._lastOwner != null)
                        {
                            Write(ball._lastOwner._id);
                        }
                        else
                        {
                            Write((ushort)(ball._owner != null ? ball._owner._id : -1));
                        }
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