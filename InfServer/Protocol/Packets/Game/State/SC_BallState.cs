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
        public Int16 positionX;
        public Int16 positionY;
        public Int16 positionZ;
        public Int16 velocityX;
        public Int16 velocityY;
        public Int16 velocityZ;
        public Int16 playerID;
        public Int32 TimeStamp;
        public Int16 unk1;

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
            Skip(6);
            Write(positionX);
            Write(positionY);
            Write(positionZ);
            Write(velocityX);
            Write(velocityY);
            Write(velocityZ);
            Write(playerID);
            
            

            Log.write(BitConverter.ToString(Data));
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
