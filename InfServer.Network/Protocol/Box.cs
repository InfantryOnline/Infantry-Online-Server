using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// BoxPacket is used to encapsulate smaller packets into one big packet
	/// in order to reduce packet overhead.
	/// </summary>
	public class BoxPacket : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public List<PacketBase> packets;		//The packets which we contain

		//Packet routing
		public const ushort TypeID = (ushort)3;
		static public event Action<BoxPacket, Client> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public BoxPacket()
			: base(TypeID)
		{
			packets = new List<PacketBase>();
		}

		/// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
		public BoxPacket(ushort typeID, byte[] buffer, int index, int count)
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
		{	//Write our info
			Write((UInt16)(TypeID << 8));

			foreach (PacketBase packet in packets)
			{	//Make sure the packet is serialized
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

				try
				{	//Transplant the data into a packet class
					PacketBase packet = _handler.getFactory().createPacket(_client, typeID, packetData, idx + 1, nextSize);

					packet._client = _client;
					packet._handler = _handler;
					packet.Deserialize();

					//Add it to our list!
					packets.Add(packet);
				}
				catch (Exception ex)
				{	//There was an error while deserializing the packet, create a dummy packet
					PacketBase packet = new PacketDummy(typeID, packetData, idx + 1, nextSize);

					packet._client = _client;
					packet._handler = _handler;
					packets.Add(packet);

					Log.write(TLog.Exception, "Exception while deserializing box packet:\r\n{0}\r\n{1}", ex, packet.DataDump);
				}

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
				string boxString = "Box packet(" + packets.Count + "):\r\n";

				foreach (PacketBase packet in packets)
					boxString += packet.Dump + "\r\n";

				boxString.Remove(boxString.Length - 2, 2);
				return boxString;
			}
		}
	}
}
