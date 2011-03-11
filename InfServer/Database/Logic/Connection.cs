using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Protocol;
using InfServer.Data;

namespace InfServer.Logic
{	// Logic_Connection Class
	/// Deals with connection-related client protocol
	///////////////////////////////////////////////////////
	class Logic_Connection
	{	/// <summary>
		/// Handles the initial packet sent by the server
		/// </summary>
		static public void Handle_SC_Initial(SC_Initial pkt, Client client)
		{	//We will be sending out state packet only once, as the sync doesn't
			//really matter on the Zone -> Database connection
			CS_State csi = new CS_State();

			csi.tickCount = (ushort)Environment.TickCount;
			csi.packetsSent = client._stats.C2S_packetsSent;
			csi.packetsReceived = client._stats.C2S_packetsRecv;

			client.send(csi);
		}

		/// <summary>
		/// Handles the servers's state packet
		/// </summary>
		static public void Handle_SC_State(SC_State pkt, Client client)
		{	//Consider the connection started now, time to send our login info..
			Database db = ((client as Client<Database>)._obj);
			db._syncStart.Set();

			//Prepare our login packet
			CS_Auth<Database> auth = new CS_Auth<Database>();

			auth.zoneID = db._config["zoneid"].intValue;
			auth.password = db._config["password"].Value;

			client.sendReliable(auth);
		}

		/// <summary>
		/// Handles the servers's auth reply
		/// </summary>
		static public void Handle_SC_Auth(SC_Auth<Database> pkt, Database db)
		{	//If it's a failure..
			if (pkt.result != SC_Auth<Database>.LoginResult.Success)
			{
				switch (pkt.result)
				{
					case SC_Auth<Database>.LoginResult.Failure:
						//General failure..
						if (pkt.message != "")
							Log.write(TLog.Error, "Failure to connect to database: {0}", pkt.message);
						else
							Log.write(TLog.Error, "Unknown login failure.");
						break;

					case SC_Auth<Database>.LoginResult.BadCredentials:
						Log.write(TLog.Error, "Invalid login credentials.");
						break;

					default:
						Log.write(TLog.Error, "Unknown login result.");
						break;
				}

				db._bLoginSuccess = false;
			}
			else
			{	//Display additional info if present
				Log.write(TLog.Normal, "Connected to database server.");
				if (pkt.message != "")
					Log.write(TLog.Normal, "Login info: {0}", pkt.message);

				db._server._name = "TODO: Send server name";
				db._bLoginSuccess = true;
			}

			//Signal our completion
			db._syncStart.Set();
		}

		/// <summary>
		/// Registers all handlers
		/// </summary>
		[Logic.RegistryFunc]
		static public void Register()
		{
			SC_Initial.Handlers += Handle_SC_Initial;
			SC_State.Handlers += Handle_SC_State;
			SC_Auth<Database>.Handlers += Handle_SC_Auth;
		}
	}
}
