using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// SC_State contains information regarding the state of the connection
	/// </summary>
	public class SC_State : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public UInt16 tickCount;		//16bit tickcount provided by the client
		public Int32 serverTickCount;	//The server's current tickcount

		public UInt64 clientSentCount;
		public UInt64 clientRecvCount;
		public UInt64 serverRecvCount;
		public UInt64 serverSentCount;

		public const ushort TypeID = (ushort)8;
		static public event Action<SC_State, Client> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public SC_State()
			: base(TypeID)
		{ }

		/// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
		public SC_State(ushort typeID, byte[] buffer, int index, int count)
			: base(typeID, buffer, index, count)
		{
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
		{	//Type ID
			Write((UInt16)(TypeID << 8));

			//Contents
			Write(tickCount);
			Write(Flip(serverTickCount));
			Write(Flip(clientSentCount));
			Write(Flip(clientRecvCount));
			Write(Flip(serverRecvCount));
			Write(Flip(serverSentCount));
		}

		/// <summary>
		/// Deserializes the data present in the packet contents into data fields in the class.
		/// </summary>
		public override void Deserialize()
		{	//Contents
			tickCount = Flip(_contentReader.ReadUInt16());
			serverTickCount = Flip(_contentReader.ReadInt32());
			clientSentCount = Flip(_contentReader.ReadUInt64());
			clientRecvCount = Flip(_contentReader.ReadUInt64());
			serverRecvCount = Flip(_contentReader.ReadUInt64());
			serverSentCount = Flip(_contentReader.ReadUInt64());
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Init tick: " + tickCount;
			}
		}
	}
}
