using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{	/// <summary>
	/// CS_Chat contains a chat communication from a client
	/// </summary>
	public class CS_Chat : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public Helpers.Chat_Type chatType;
        public byte bong;

		public string message;
		public string recipient;

		//Packet routing
		public const ushort TypeID = (ushort)Helpers.PacketIDs.C2S.Chat;
		static public event Action<CS_Chat, Player> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
		public CS_Chat(ushort typeID, byte[] buffer, int index, int count)
			: base(typeID, buffer, index, count)
		{
		}

		/// <summary>
		/// Routes a new packet to various relevant handlers
		/// </summary>
		public override void Route()
		{	//Call all handlers!
			if (Handlers != null)
				Handlers(this, ((Client<Player>)_client)._obj);
		}

		/// <summary>
		/// Deserializes the data present in the packet contents into data fields in the class.
		/// </summary>
		public override void Deserialize()
		{	//What sort of chat is this?
			chatType = (Helpers.Chat_Type)_contentReader.ReadByte();
            bong = _contentReader.ReadByte();

			switch (chatType)
			{
				case Helpers.Chat_Type.Normal:
					recipient = ReadNullString();
					message = ReadNullString();
					break;

                case Helpers.Chat_Type.PrivateChat:
                    recipient = ReadNullString();
                    message = ReadNullString();
                    break;

				case Helpers.Chat_Type.Whisper:
					recipient = ReadNullString();
					message = ReadNullString();
					break;

				case Helpers.Chat_Type.EnemyTeam:
					recipient = ReadNullString();
					message = ReadNullString();
					break;

				case Helpers.Chat_Type.Team:
					recipient = ReadNullString();
					message = ReadNullString();
					break;

				case Helpers.Chat_Type.Squad:
					recipient = ReadNullString();
					message = ReadNullString();
					break;

                case Helpers.Chat_Type.Macro:
                    recipient = ReadNullString();
                    message = ReadNullString();
                    break;

				default:
					message = "";
					recipient = "";
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
				return "Chat[" + chatType + "]: " + message;
			}
		}
	}
}
