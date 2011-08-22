using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

namespace InfServer.Protocol
{	/// <summary>
	/// CS_Environment contains a client's environmental data
	/// </summary>
	public class CS_Environment : PacketBase
	{	// Member Variables
		///////////////////////////////////////////////////
		public List<string> processes;
		public List<string> windows;

		//Packet routing
		public const ushort TypeID = (ushort)Helpers.PacketIDs.C2S.Environment;
		static public event Action<CS_Environment, Player> Handlers;


		///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Creates an instance of the dummy packet used to debug communication or 
		/// to represent unknown packets.
		/// </summary>
		/// <param name="typeID">The type of the received packet.</param>
		/// <param name="buffer">The received data.</param>
		public CS_Environment(ushort typeID, byte[] buffer, int index, int count)
			: base(typeID, buffer, index, count)
		{
		}

		/// <summary>
		/// Routes a new packet to various relevant handlers
		/// </summary>
		public override void Route()
		{	//Call all handlers!
			if (Handlers != null)
				Handlers(this, ((Client<Player>)_client)._obj);
		}

		/// <summary>
		/// Deserializes the data present in the packet contents into data fields in the class.
		/// </summary>
		public override void Deserialize()
		{
			_contentReader.ReadInt16();

			//Read in the main string
			processes = new List<string>();
			windows = new List<string>();
			string env = ReadNullString((int)_content.Length);
			int idx = 0;

			while ((idx = env.IndexOf('"', idx)) != -1)
			{	//Find the end
				int endIdx = env.IndexOf("\",", idx + 1);
				if (endIdx == -1)
					break;

				//A process or window?
				string payload = env.Substring(idx + 3, endIdx - idx - 3);

				if (env[idx + 1] == 'P')
					processes.Add(payload);
				else
					windows.Add(payload);

				idx = endIdx + 2;
			}
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public override string Dump
		{
			get
			{
				return "Framelapse update";
			}
		}
	}
}
