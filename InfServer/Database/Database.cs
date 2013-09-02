using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;

using InfServer.Network;
using InfServer.Protocol;
using InfServer.Game;


namespace InfServer.Data
{
	// Database Class
	/// Used to maintain a connection with the database server
	///////////////////////////////////////////////////////u
	public class Database : IClient
	{	// Member variables
		///////////////////////////////////////////////////
		private LogClient _logger;				//Our log client for database related activities
		public ZoneServer _server;				//Our associated zone server

		private ClientConn<Database> _conn;		//Our UDP connection client

		public ManualResetEvent _syncStart;		//Used for blocking connect attempts
		public ConfigSetting _config;			//Our database-specific settings
		public bool _bLoginSuccess;				//Were we able to successfully login?

		public bool bActive
		{
			get
			{
				return _bLoginSuccess;
			}
		}


		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Generic constructor
		/// </summary>
		public Database(ZoneServer server, ConfigSetting config, LogClient logger)
		{
			_server = server;
			_config = config;

			_conn = new ClientConn<Database>(new S2CPacketFactory<Database>(), this);
			_syncStart = new ManualResetEvent(false);

            _logger = logger;
			_conn._logger = logger;
		}

		#region Connection
		/// <summary>
		/// Attempts to create and initialize a connection to the DB server
		/// </summary>
		public bool connect(IPEndPoint dbPoint, bool bBlock)
		{	//Assume the worst
			_syncStart.Reset();
			_bLoginSuccess = false;
            _server._lastDBAttempt = Environment.TickCount;

            using (LogAssume.Assume(_logger))
            {
                Log.write("Connecting to database server..");

                //Start our connection
                _conn.begin(dbPoint);

                //Send our initial packet
                CS_Initial init = new CS_Initial();

                _conn._client._connectionID = init.connectionID = new Random().Next();
                init.CRCLength = Client.crcLength;
                init.udpMaxPacket = Client.udpMaxSize;

                _conn._client.send(init);

                _syncStart.WaitOne(10000);

                //Reset our event
                _syncStart.Reset();


                //Were we successful?
                return _bLoginSuccess;
            }
		}

		/// <summary>
		/// Sends a reliable packet to the client
		/// </summary>
		public void send(PacketBase packet)
		{	//Relay
			send(packet, null);
		}

		/// <summary>
		/// Sends a reliable packet to the client
		/// </summary>
		public void send(PacketBase packet, Action completionCallback)
		{	//Defer to our client!
			_conn._client.sendReliable(packet, completionCallback);
		}

		/// <summary>
		/// Disconnects our current session with the database server
		/// </summary>
		public void disconnect()
		{
			_conn._client.destroy();
		}

		/// <summary>
		/// Allows our client object to be destroyed properly
		/// </summary>
		public void destroy()
		{

		}

		/// <summary>
		/// Retrieves the client connection stats
		/// </summary>
		public Client.ConnectionStats getStats()
		{
			return _conn._client._stats;
		}
		#endregion

		#region Updating
		/// <summary>
		/// Declares that a player has left the server
		/// </summary>
		public void lostPlayer(Player player)
		{	//Notify the database!
			CS_PlayerLeave<Database> pkt = new CS_PlayerLeave<Database>();

			pkt.player = player.toInstance();
			pkt.alias = player._alias;

			send(pkt);
		}

		/// <summary>
		/// Submits a player update to the database
		/// </summary>
		public void updatePlayer(Player player)
		{	//Obtain a player stats object
			PlayerStats stats = player.getStats();
			if (stats == null)
				return;

            if (_server.IsStandalone)
                return;

			//Create an update packet
			CS_PlayerUpdate<Database> upd = new CS_PlayerUpdate<Database>();

			upd.player = player.toInstance();
			upd.stats = stats;

			//All good!
			send(upd);

            //Lets check the timers for other stat updates
            DateTime now = DateTime.Now;
            DateTime specDate = DateTime.Today.AddDays(1);

            //Note: Time.Today uses a format 00/00/0000 time 12:00:00am
            if (now >= (specDate.AddMinutes(-2)))
            {
                CS_StatsUpdate<Data.Database> stat = new CS_StatsUpdate<Data.Database>();
                stat.player = player.toInstance();
                stat.scoreType = CS_StatsUpdate<Data.Database>.ScoreType.ScoreDaily;
                stat.stats = stats;
                stat.date = now;
                send(stat);
            }

            DayOfWeek day = DayOfWeek.Sunday;
            if (now.DayOfWeek == day && now >= (specDate.AddMinutes(-2)))
            {
                CS_StatsUpdate<Data.Database> stat = new CS_StatsUpdate<Data.Database>();
                stat.player = player.toInstance();
                stat.scoreType = CS_StatsUpdate<Data.Database>.ScoreType.ScoreWeekly;
                stat.stats = stats;
                stat.date = now;
                send(stat);
            }

            DateTime month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            month = month.AddMonths(1);
            if (now < month && now >= (specDate.AddMinutes(-2)))
            {
                CS_StatsUpdate<Data.Database> stat = new CS_StatsUpdate<Data.Database>();
                stat.player = player.toInstance();
                stat.scoreType = CS_StatsUpdate<Data.Database>.ScoreType.ScoreMonthly;
                stat.stats = stats;
                stat.date = now;
                send(stat);
            }

            DateTime year = new DateTime(DateTime.Today.Year, 1, 1);
            year = year.AddYears(1);
            if (now < year && now >= (specDate.AddMinutes(-2)))
            {
                CS_StatsUpdate<Data.Database> stat = new CS_StatsUpdate<Data.Database>();
                stat.player = player.toInstance();
                stat.scoreType = CS_StatsUpdate<Data.Database>.ScoreType.ScoreYearly;
                stat.stats = stats;
                stat.date = now;
                send(stat);
            }
		}

        public void reportMatch(long winner, long loser, CS_SquadMatch<Database>.SquadStats wStats, CS_SquadMatch<Database>.SquadStats lStats)
        {
            if (_server.IsStandalone)
                return;

            //Create an update packet
            CS_SquadMatch<Database> upd = new CS_SquadMatch<Database>();
            upd.wStats = wStats;
            upd.lStats = wStats;
            upd.loser = loser;
            upd.winner = winner;


            //All good!
            send(upd);
        }
		#endregion
	}
}
