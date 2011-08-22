using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// DataPacketRcv is a packet type used for reliable transmitting
	/// large potions of data at once.
	/// </summary>
	public class DataPacketRcv : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public ushort rNumber;		//The number which identifies this message
		public int dataSize;		//The size of the data as a whole

		public byte[] data;			//The packet message we're carrying

		public int streamID;		//Which data stream is this? (Maximum of 4)

		//Packet routing
		public const ushort TypeID = (ushort)0x0D;
		static public event Action<DataPacketRcv, Client> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public DataPacketRcv()
			: base(TypeID)
		{ }

		/// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
		public DataPacketRcv(ushort typeID, byte[] buffer, int index, int count, int sID)
			: base(typeID, buffer, index, count)
		{
			streamID = sID;
		}

		/// <summary>
		/// Routes a new packet to various relevant handlers
		/// </summary>
		public override void Route()
		{	//Call all handlers!
			if (Handlers != null)
				Handlers(this, (Client)_client);
		}

		/// <summary>
		/// Deserializes the data present in the packet contents into data fields in the class.
		/// </summary>
		public override void Deserialize()
		{	//Since this packet can come in 2 formats depending on the client state, we need to serialize for both
			rNumber = Flip(_contentReader.ReadUInt16());

			data = _contentReader.ReadBytes((int)(_content.Length - 2)); 
			dataSize = Flip(BitConverter.ToInt32(data, 0));
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return String.Format("Data message #{0} Stream #{1}", rNumber, streamID);
			}
		}
	}
}
