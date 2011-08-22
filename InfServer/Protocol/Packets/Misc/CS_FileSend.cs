using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{	/// <summary>
	/// CS_FileSend keeps the server notified of the amount of frames lapsed
	/// </summary>
	public class CS_FileSend : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public string unk1;
		public string playerTarget;

		public byte[] fileData;

		//Packet routing
		public const ushort TypeID = (ushort)Helpers.PacketIDs.C2S.FileSend;
		static public event Action<CS_FileSend, Player> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
		public CS_FileSend(ushort typeID, byte[] buffer, int index, int count)
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
		{	//Get the file information
			_contentReader.ReadBytes(128);
			unk1 = ReadString(128);
			playerTarget = ReadString(32);

			fileData = _contentReader.ReadBytes((int)_content.Length - 128 - 128 - 32); 
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "File send";
			}
		}
	}
}
