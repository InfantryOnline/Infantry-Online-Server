using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;


using InfServer;
using InfServer.Network;
using InfServer.Protocol;

namespace InfClient
{
    public partial class InfClient : IClient
    {
        public new LogClient _logger;
        public static ClientConn<InfClient> _conn;		//Our UDP connection client
        public static IPEndPoint endpoint;

        public ManualResetEvent _syncStart;		//Used for blocking connect attempts
        public bool _bLoginSuccess;				//Were we able to successfully login?

        /// <summary>
        /// Constructor
        /// </summary>
        public InfClient()
        {
            _conn = new ClientConn<InfClient>(new S2CPacketFactory<InfClient>(), this);
            _syncStart = new ManualResetEvent(false);

            //Log packets for now..
            _conn._bLogPackets = false;

            _logger = Log.createClient("Client");
            _conn._logger = _logger;
        }

        //Place holder atm.
        public bool init()
        {
            Log.write("Client initializing..");
            return true;
        }


        /// <summary>
        /// Called when making a connection to a zoneserver
        /// </summary>
        public void connect(IPEndPoint sPoint)
        {
            Log.write("Connecting to {0}", sPoint);

            //Start our connection
            _conn.begin(sPoint);

            //Send our initial packet
            CS_Initial init = new CS_Initial();

            //Generate a connection ID.
            _conn._client._connectionID = init.connectionID = new Random().Next();
            init.CRCLength = 2;
            init.udpMaxPacket = 496;

            //Send our init!
            _conn._client.send(init);

        }

        /// <summary>
        /// Disconnects our current session with the database server
        /// </summary>
        public void disconnect()
        {
            _conn._client.destroy();
        }

        /// <summary>
        /// Destroys our client properly.
        /// </summary>
        public void destroy()
        {

        }
    }
}