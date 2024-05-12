﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// CS_State contains information regarding the state of the connection
	/// </summary>
	public class CS_State : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public UInt16 tickCount;				//The last 16 bits of the client's tickcount

		public Int32 clientCurrentUpdate;
		public Int32 clientAverageUpdate;
		public Int32 clientShortestUpdate;
		public Int32 clientLongestUpdate;
		public Int32 clientLastUpdate;
		public UInt64 packetsSent;
		public UInt64 packetsReceived;

		//Packet routing
		public const ushort TypeID = (ushort)7;
		static public event Action<CS_State, Client> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public CS_State()
			: base(TypeID)
		{ }

		/// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
		public CS_State(ushort typeID, byte[] buffer, int index, int count)
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

			Write(Flip(tickCount));
			Write(Flip(clientCurrentUpdate));
			Write(Flip(clientAverageUpdate));
			Write(Flip(clientShortestUpdate));
			Write(Flip(clientLongestUpdate));
			Write(Flip(clientLastUpdate));
			Write(Flip(packetsSent));
			Write(Flip(packetsReceived));
		}

		/// <summary>
		/// Deserializes the data present in the packet contents into data fields in the class.
		/// </summary>
		public override void Deserialize()
		{	//Get the tickcount
			tickCount = Flip( _contentReader.ReadUInt16());

			clientCurrentUpdate = Flip(_contentReader.ReadInt32());
			clientAverageUpdate = Flip(_contentReader.ReadInt32());
			clientShortestUpdate = Flip(_contentReader.ReadInt32());
			clientLongestUpdate = Flip(_contentReader.ReadInt32());
			clientLastUpdate = Flip(_contentReader.ReadInt32());
			packetsSent = Flip(_contentReader.ReadUInt64());
			packetsReceived = Flip(_contentReader.ReadUInt64());
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
