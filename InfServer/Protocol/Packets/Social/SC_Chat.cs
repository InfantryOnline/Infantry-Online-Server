using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_Chat contains a communique routed by the server
	/// </summary>
	public class SC_Chat : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public Helpers.Chat_Type chatType;
		public byte bong;

		public string message;
		public string from;
        public string chat;

		public const ushort TypeID = (ushort)Helpers.PacketIDs.S2C.Chat;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_Chat()
			: base(TypeID)
		{ }

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{	//Type ID
			Write((byte)TypeID);
			Write((byte)chatType);
            Write(bong);

				//What sort of chat is this?
			switch (chatType)
			{
				case Helpers.Chat_Type.Normal:
					Write(from, 0);
					Write(message, 0);
					break;

				case Helpers.Chat_Type.Whisper:
					Write(from, 0);
					Write(message, 0);
					break;

				case Helpers.Chat_Type.Team:
					Write(from, 0);
					Write(message, 0);
					break;

				case Helpers.Chat_Type.EnemyTeam:
					Write(from, 0);
					Write(message, 0);
					break;

				case Helpers.Chat_Type.Arena:
					Write(from, 0);
					Write(message, 0);
					break;

				case Helpers.Chat_Type.Squad:
					Write(from, 0);
					Write(message, 0);
					break;

                case Helpers.Chat_Type.PrivateChat:
                    Write(from, 0);
                    Write(message, 0);
                    break;
			}
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Chat routing.";
			}
		}
	}
}
