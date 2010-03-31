using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{	/// <summary>
	/// CS_RequestUpdate asks the server to send it a given file.
	/// </summary>
	public class CS_RequestUpdate : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public List<Update> updates;					//The name of the file to receive

		//Represents a single update request
		public class Update
		{
			public string filename;
		}

		//Packet routing
		public const ushort TypeID = (ushort)Helpers.PacketIDs.C2S.RequestUpdate;
		static public event Action<CS_RequestUpdate, Player> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
		public CS_RequestUpdate(ushort typeID, byte[] buffer, int index, int count)
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
		{	//While there's room for more
			updates = new List<Update>();
			Update update = new Update();

			//Read in our filename string
			update.filename = ReadString(32);
			updates.Add(update);

			//Room for more?
			try
			{
				while (true)
				{	//Skip the header byte
					_contentReader.ReadChar();

					update = new Update();

					//Read in our filename string
					update.filename = ReadString(32);
					updates.Add(update);
				}
			}
			catch (Exception)
			{
			}
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Client update request";
			}
		}
	}
}
