using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// DataPacket is a packet type used for reliable transmitting
	/// large potions of data at once.
	/// </summary>
	public class DataPacket : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public bool _bFirstPacket;	//Is this the first packet of the data sequence?
		public ushort _dataRead;	//The amount of data actually read from the buffer

		public ushort rNumber;		//The number which identifies this message
		
		public byte[] data;			//The packet message we're carrying
		public int offset;			//

		//Packet routing
		public const ushort TypeID = (ushort)13;
		static public event Action<DataPacket, Client> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public DataPacket()
			: base(TypeID)
		{ }

		/// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
		public DataPacket(ushort typeID, byte[] buffer, int index, int count)
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
		{	//Packet ID
			Write((UInt16)(TypeID << 8));

			//Insert our number
			Write(Flip(rNumber));

			//If this is our first transmission, we want to include the size
			if (_bFirstPacket)
				Write(Flip(data.Length));

			//Write our packet contents
			Client protocol = (Client)_client;
			int remaining = data.Length - offset;

			if (remaining > (protocol._C2S_UDPSize - _size - protocol._CRCLength))
			{
				_dataRead = (ushort)(protocol._C2S_UDPSize - _size - protocol._CRCLength);
				Write(data, offset, _dataRead);
			}
			else
			{
				_dataRead = (ushort)remaining;
				Write(data, offset, _dataRead);
			}
		}

		/// <summary>
		/// Deserializes the data present in the packet contents into data fields in the class.
		/// </summary>
		public override void Deserialize()
		{	
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Data message #" + rNumber;
			}
		}
	}
}
