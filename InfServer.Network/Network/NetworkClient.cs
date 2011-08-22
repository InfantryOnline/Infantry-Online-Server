using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace InfServer.Network
{	// NetworkClient Class
	/// Used to represent a connection to a single client
	///////////////////////////////////////////////////////
	public abstract class NetworkClient
	{	// Member variables
		///////////////////////////////////////////////////
		public IPacketHandler _handler;					//The packet handler we belong to
		public bool _bDestroyed;						//Is the connection in use anymore?

		//Credentials
		public Int64 _clientID;							//The ID of the client (IP | (Source Port << 32))
		public IPEndPoint _ipe;							//The client destination

		//Connection stats
		public int _lastPacketRecv;						//The time at which we last received a packet from this client

		public event Action<NetworkClient> Destruct;	//Called when the client is being destroyed and connection severed


		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Allows the client to take care of anything it needs to
		/// </summary>
		public virtual void poll()
		{ }

		/// <summary>
		/// Ceases interaction with this client, removes it from the server
		/// </summary>
		public virtual void destroy()
		{	//No longer active
			_bDestroyed = true;

			//Allow handlers to trigger
			if (Destruct != null)
				Destruct(this);
		}

		/// <summary>
		/// Sends a given packet to the client
		/// </summary>
		public virtual void send(PacketBase packet)
		{	//First, allow the packet to serialize
			packet.MakeSerialized(this, _handler);

			//Start sending!
			_handler.sendPacket(packet, packet.Data, _ipe);
		}

		/// <summary>
		/// Gets the typeid of a given packet
		/// </summary>
		static public ushort getTypeID(byte[] packet, int offset)
		{	//System packet?
			if (packet[offset] == 0)
				return (ushort)packet[offset + 1];

			return (ushort)packet[offset];
		}

		/// <summary>
		/// Allows handling of packets before they're dispatched
		/// </summary>
		public virtual bool predispatchCheck(PacketBase packet)
		{ return true; }

		/// <summary>
		/// Checks the integrity of a given packet
		/// </summary>
		public abstract bool checkPacket(byte[] data, ref int offset, ref int count);

		/// <summary>
		/// Creates a new network client class of the same type.
		/// </summary>
		public abstract NetworkClient newInstance();

		/// <summary>
		/// Provides a summary of the client.
		/// </summary>
		public override string ToString()
		{
			return _ipe.ToString();
		}
	}
}
