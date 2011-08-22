using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;

namespace InfServer.Protocol
{	/// <summary>
	/// Reliable is a packet type used for exchanging reliable messages
	/// </summary>
	public class Reliable : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public ushort rNumber;			//The number which identifies this message
		public PacketBase packet;		//The packet message we're carrying

		public int streamID;			//Which data stream is this? (Maximum of 4)

		//Packet routing
		public const ushort TypeID = (ushort)9;
		static public event Action<Reliable, Client> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		public Reliable()
			: base(TypeID)
		{}

		/// <summary>
		/// Used to easily create a reliable packet
		/// </summary>
		public Reliable(PacketBase _packet, int _rNumber, int _streamID)
			: base(TypeID)
		{
			packet = _packet;
			rNumber = (ushort)_rNumber;
			streamID = _streamID;
		}

		/// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
		public Reliable(ushort typeID, byte[] buffer, int index, int count, int sID)
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
		{	//Packet ID
			Write((UInt16)((TypeID + streamID) << 8));
			
			//Insert our number
			Write(Flip(rNumber));

			//Make sure our packet is serialized
			packet.MakeSerialized(_client, _handler);

			//Write our packet contents
			Write(packet.Data);
		}

		/// <summary>
		/// Deserializes the data present in the packet contents into data fields in the class.
		/// </summary>
		public override void Deserialize()
		{	//Obtain our message number
			rNumber = Flip(_contentReader.ReadUInt16());

			//Obtain our message
			byte[] data = Data;
			ushort typeID = NetworkClient.getTypeID(Data, 2);

			try
			{
				packet = _handler.getFactory().createPacket(_client, typeID, data, 2, data.Length - 2);

				packet._client = _client;
				packet._handler = _handler;
				packet.Deserialize();
			}
			catch (Exception ex)
			{	//There was an error while deserializing the packet, create a dummy packet
				packet = new PacketDummy(typeID, data, 2, data.Length - 2);
				packet._client = _client;
				packet._handler = _handler;

				Log.write(TLog.Exception, "Exception while deserializing reliable packet:\r\n{0}\r\n{1}", ex, packet.DataDump);
			}
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return String.Format("R[{0}]: {1}", rNumber, packet.Dump);
			}
		}

		/// <summary>
		/// Returns a dump of the packet's data
		/// </summary>
		public override string DataDump
		{
			get
			{
				return packet.DataDump;
			}
		}
	}
}
