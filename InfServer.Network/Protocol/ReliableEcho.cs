using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// ReliableEcho indicates the successful retrieval of a reliable message
	/// </summary>
	public class ReliableEcho : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public ushort rNumber;			//The message number we recieved

		public int streamID;		

		//Packet routing
		public const ushort TypeID = (ushort)21;
		static public event Action<ReliableEcho, Client> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public ReliableEcho(int _streamID)
			: base(TypeID)
		{
			streamID = _streamID;
		}

		/// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
		public ReliableEcho(ushort typeID, byte[] buffer, int index, int count, int sID)
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
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public override void Serialize()
		{	//Write our info
			Write(Flip((short)(TypeID + streamID)));
			Write(Flip(rNumber));
		}

		/// <summary>
		/// Deserializes the data present in the packet contents into data fields in the class.
		/// </summary>
		public override void Deserialize()
		{	//Obtain our message number
			rNumber = Flip(_contentReader.ReadUInt16());
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return String.Format("Reliable Echo Stream #{0} Message #{1}", streamID, rNumber);
			}
		}
	}
}
