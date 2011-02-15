using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using InfServer.Network;
using InfServer.Protocol;
using InfServer.Data;

namespace InfServer
{
	// DBServer Class
	/// Represents the database server state
	///////////////////////////////////////////////////////
	public partial class DBServer : Server
	{	// Member variables
		///////////////////////////////////////////////////
		public ConfigSetting _config;			//Our server config
		public new LogClient _logger;			//Our zone server log

		private string _connectionString;		//The connectionstring to our database


		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Generic constructor
		/// </summary>
		public DBServer()
			: base(new C2SPacketFactory<Zone>(), new Client<Zone>(false))
		{
			_config = ConfigSetting.Blank;
		}

		/// <summary>
		/// Allows the server to preload all assets.
		/// </summary>
		public bool init()
		{	//Load our server config
			Log.write(TLog.Normal, "Loading Server Configuration");
			_config = new Xmlconfig("server.xml", false).Settings;

			//Load protocol config settings
			Client.udpMaxSize = _config["protocol/udpMaxSize"].intValue;
			Client.crcLength = _config["protocol/crcLength"].intValue;
			if (Client.crcLength > 4)
			{
				Log.write(TLog.Error, "Invalid protocol/crcLength, must be less than 4.");
				return false;
			}
			
			Client.reliableJuggle = _config["protocol/reliableJuggle"].intValue;
			Client.reliableGrace = _config["protocol/reliableGrace"].intValue;
			Client.connectionTimeout = _config["protocol/connectionTimeout"].intValue;

			//Attempt to connect to our database
			_connectionString = _config["database/connectionString"].Value;

			//Does the database exist?
			using (InfantryDataContext db = getContext())
			{
				if (!db.DatabaseExists())
				{	//Create a new one
					Log.write(TLog.Warning, "Database layout doesn't exist, creating..");

					db.CreateDatabase();
				}
			}

			//We're good!
			Log.write("Connected to database.");
			return true;
		}

		/// <summary>
		/// Begins all server processes, and starts accepting clients.
		/// </summary>
		public void begin()
		{	//Start up the network
			_logger = Log.createClient("DBServer");
			base._logger = Log.createClient("Network");

			IPEndPoint listenPoint = new IPEndPoint(
				IPAddress.Parse(_config["bindIP"].Value), _config["bindPort"].intValue);
			base.begin(listenPoint);
			
			while (true)
				Thread.Sleep(10);
		}

		/// <summary>
		/// Creates a new data context to connect to the database
		/// </summary>
		public InfantryDataContext getContext()
		{
			return new InfantryDataContext(_connectionString);
		}
	}
}
