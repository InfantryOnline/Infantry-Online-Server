using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{	/// <summary>
	/// CS_ChartRequest keeps the server notified of the amount of frames lapsed
	/// </summary>
	public class CS_ChartRequest : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public Helpers.ChartType type;
		public string options;

		//Packet routing
		public const ushort TypeID = (ushort)Helpers.PacketIDs.C2S.ChartRequest;
		static public event Action<CS_ChartRequest, Player> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
		public CS_ChartRequest(ushort typeID, byte[] buffer, int index, int count)
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
		{	
			type = (Helpers.ChartType)_contentReader.ReadByte();
			options = ReadString(64);
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Chart request";
			}
		}
	}
}
