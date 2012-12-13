using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;


using InfServer;
using InfServer.Network;
using InfServer.Protocol;

namespace InfClient.Protocol
{
    public partial class Login
    {
        /// <summary>
        /// Handles the initial packet sent by the server
        /// </summary>
        static public void Handle_SC_Initial(SC_Initial pkt, Client client)
        {
                CS_State csi = new CS_State();


                csi.tickCount = (ushort)Environment.TickCount;
                csi.packetsSent = client._packetsSent;
                csi.packetsReceived = client._packetsReceived;

                client.send(csi);
        }

        /// <summary>
        /// Handles the servers's state packet
        /// </summary>
        static public void Handle_SC_State(SC_State pkt, Client client)
        {	//Consider the connection started now, time to send our login info..

            InfClient c = ((client as Client<InfClient>)._obj);
            c._syncStart.Set();

            CS_Login login = new CS_Login();

            //Lets put some bogus stuff together...
            login.bCreateAlias = false;
            login.UID1 = 99999;
            login.UID2 = 99999;
            login.UID3 = 99999;
            login.NICInfo = 4;
            login.SysopPass = "";
            login.Username = "nwapslleh";
            login.Version = (ushort)154;
            login.TicketID = "3bd5c3e9-8bea-43b8-946e-40263f8a039a";

            //Send it!
            client.send(login);
        }


        /// <summary>
        /// Handles the server's login reply
        /// </summary>
        static public void Handle_SC_Login(SC_Login pkt, Client client)
        {
            //Log the response!
            Log.write("(Result={0}) -  (Config={1}) - (Message={2})", pkt.result, pkt.zoneConfig, pkt.popupMessage);

            //No sense in being connected anymore
            if (pkt.result == SC_Login.Login_Result.Failed)
            {
                Disconnect discon = new Disconnect();
                discon.connectionID = client._connectionID;
                discon.reason = Disconnect.DisconnectReason.DisconnectReasonApplication;
                client.send(discon);
                return;
            }
            
            //Must have been a success, lets let the server know we're ready.
            client.send(new CS_Ready());
        }

        /// <summary>
        /// Handles the server's PatchInfo reply
        /// </summary>
        static public void Handle_SC_PatchInfo(SC_PatchInfo pkt, Client client)
        {
            Log.write("(PatchServer={0}:{1}) (Xml={2})", pkt.patchServer, pkt.patchPort, pkt.patchXml); 
        }

        /// <summary>
        /// Handles the server's AssetInfo reply
        /// </summary>
        static public void Handle_SC_AssetInfo(SC_AssetInfo pkt, Client client)
        {
            //Send a ArenaJoin reply.
            CS_ArenaJoin join = new CS_ArenaJoin();
            join.EXEChecksum = 1;
            join.Unk1 = false;
            join.Unk2 = 1;
            join.Unk3 = 2;
            join.AssetChecksum = 3;
            join.ArenaName = "";
            client.send(join);
        }

        /// <summary>
        /// Handles the server's SetIngame reply
        /// </summary>
        static public void Handle_SC_SetIngame(SC_SetIngame pkt, Client client)
        {
            Log.write("We're in!");
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
            SC_PatchInfo.Handlers += Handle_SC_PatchInfo;
            SC_AssetInfo.Handlers += Handle_SC_AssetInfo;
            SC_SetIngame.Handlers += Handle_SC_SetIngame;
        }
    }
}
