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
        /// Handles the initial packet sent by the server
        /// </summary>
        static public void Handle_SC_Initial(SC_Initial pkt, Client client)
        {   CS_State csi = new CS_State();


            csi.tickCount = (ushort)Environment.TickCount;
            csi.packetsSent = client._packetsSent;
            csi.packetsReceived = client._packetsReceived;

            _conn._client.send(csi);
        }

        /// <summary>
        /// Handles the servers's state packet
        /// </summary>
        static public void Handle_SC_State(SC_State pkt, Client client)
        {	//Consider the connection started now, time to send our login info..
            CS_Login login = new CS_Login();
            
            //Lets put some bogus stuff together...
            login.bCreateAlias = false;
            login.UID1 = 1;
            login.UID2 = 2;
            login.UID3 = 3;
            login.NICInfo = 4;
            login.SysopPass = "";
            login.Username = "Spawn";
            login.Version = (ushort)154;
            login.TicketID = "2d9df516-2578-45a1-9a9b-957edbc1c028";

            //Send it!
            _conn._client.send(login);
        }


        /// <summary>
        /// Handles the server's login reply
        /// </summary>
        static public void Handle_SC_Login(SC_Login pkt, Client client)
        {
            //Log the response!
            Log.write("(Result={0}) -  (Config={1})", pkt.result, pkt.zoneConfig);
        }



        /// <summary>
        /// Registers all handlers
        /// </summary>
        [InfServer.Logic.RegistryFunc]
        static public void Register()
        {
            SC_Initial.Handlers += Handle_SC_Initial;
            SC_State.Handlers += Handle_SC_State;
            SC_Login.Handlers += Handle_SC_Login;
        }



        /// <summary>
        /// Destroys our client properly.
        /// </summary>
        public void destroy()
        {
        }
    }
}