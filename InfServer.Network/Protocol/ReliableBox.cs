using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// ReliableBox is a packet type used for storing multiple messages in one reliable
	/// </summary>
	public class ReliableBox : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public List<Client.ReliableInfo> reliables;		//The reliables we need to box
		public List<PacketBase> packets;				//The packets which we contain

		//Packet routing
		public const ushort TypeID = (ushort)0x19;
		static public event Action<ReliableBox, Client> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public ReliableBox()
			: base(TypeID)
		{
			reliables = new List<Client.ReliableInfo>();
		}

		/// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
		public ReliableBox(ushort typeID, byte[] buffer, int index, int count)
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

			//Look through all our infos
			int lastRID = -1;

			foreach (Client.ReliableInfo info in reliables)
			{	//Make sure it's in sequence
				if (lastRID != -1 && lastRID != UInt16.MaxValue &&
					info.rid != lastRID + 1)
					//An out of sync packet!
					throw new ArgumentException("Reliable boxing error! Packets in reliable queue were out of order, dropping.");

				//Make sure the packet is serialized
				PacketBase packet = info.packet;
				lastRID = info.rid;

				if (!packet._bSerialized)
				{
					packet._client = _client;
					packet._handler = _handler;

					packet.Serialize();
					packet._bSerialized = true;
				}

				//Insert the packet size
				byte[] packetData = packet.Data;

				Write((byte)packetData.Length);

				//Insert the data itself
				Write(packetData);
			}
		}

		/// <summary>
		/// Deserializes the data present in the packet contents into data fields in the class.
		/// </summary>
		public override void Deserialize()
		{	//Initialize our packet list
			packets = new List<PacketBase>();

			//While there's more packets in the buffer..
			byte[] packetData = Data;
			int idx = 0;

			while (_size - idx > 0)
			{	//Get the size of the following packet
				byte nextSize = packetData[idx];

				//Form the new packet
				ushort typeID = NetworkClient.getTypeID(packetData, idx + 1);

				//Transplant the data into a packet class
				PacketBase packet = _handler.getFactory().createPacket(_client, typeID, packetData, idx + 1, nextSize);

				packet._client = _client;
				packet._handler = _handler;
				packet.Deserialize();

				//Add it to our list!
				packets.Add(packet);

				idx += nextSize + 1;
			}
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Reliable box";
			}
		}
	}
}
