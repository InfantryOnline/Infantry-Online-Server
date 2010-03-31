using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;


namespace InfServer.Protocol
{	/// <summary>
	/// Provides a series of functions for easily serialization of packets
	/// </summary>
	public partial class Helpers
	{	///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Serializes a login response packet
		/// </summary>
		static public void Login_Response(Client c, SC_Login.Login_Result result)
		{	//Formulate our reply!
			SC_Login reply = new SC_Login();

			reply.result = result;
			reply.serverVersion = 154;
			reply.zoneConfig = _server._config["server/zoneConfig"].Value;
			reply.popupMessage = "";

			c.sendReliable(reply);
		}

		/// <summary>
		/// Serializes a login response packet
		/// </summary>
		static public void Login_Response(Client c, SC_Login.Login_Result result, string popupMessage)
		{	//Formulate our reply!
			SC_Login reply = new SC_Login();

			reply.result = result;
			reply.serverVersion = 154;
			reply.zoneConfig = _server._config["server/zoneConfig"].Value;
			reply.popupMessage = popupMessage;

			c.sendReliable(reply);
		}
	}
}
