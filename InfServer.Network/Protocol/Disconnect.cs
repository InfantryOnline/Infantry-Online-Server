using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// Disconnect is used to signal that the connection has been terminated
	/// </summary>
	public class Disconnect : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public Int32 connectionID;			//The connection ID given at the start of the communication
		public Int16 reasonID;				//The reason for disconnecting

		//Packet routing
		public const ushort TypeID = (ushort)5;
		static public event Action<Disconnect, Client> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public Disconnect()
			: base(TypeID)
		{ }

		/// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
		public Disconnect(ushort typeID, byte[] buffer, int index, int count)
			: base(typeID, buffer, index, count)
		{ }

		/// <summary>
		/// Routes a new packet to various relevant handlers
		/// </summary>
		public override void Route()
		{	//Call all handlers!
			if (Handlers != null)
				Handlers(this, (Client)_client);
		}

		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{	//Type ID
			Write((byte)TypeID);

			Write(connectionID);
			Write(Flip(reasonID));
		}

		/// <summary>
		/// Deserializes the data present in the packet contents into data fields in the class.
		/// </summary>
		public override void Deserialize()
		{	//Get our values!
			connectionID = _contentReader.ReadInt32();
			reasonID = Flip(_contentReader.ReadInt16());
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Disconnect signal (" + reasonID + ")";
			}
		}
	}
}
