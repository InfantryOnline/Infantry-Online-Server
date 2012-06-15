using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using InfServer.Protocol;
using InfServer.Game;

namespace InfServer.Logic
{	// Logic_Login Class
	/// Deals with the login process
	///////////////////////////////////////////////////////
	class Logic_Login
	{	
		/// <summary>
		/// Handles the login request packet sent by the client
		/// </summary>
		static public void Handle_CS_Login(CS_Login pkt, Client<Player> client)
		{	//Let's escalate our client's status to player!
			ZoneServer server = (client._handler as ZoneServer);
			Player newPlayer = server.newPlayer(client, pkt.Username);

            if (server._config["server/permitMode"].boolValue)
            {
                if (!Logic_Permit.checkPermit(newPlayer._alias) && newPlayer.PermissionLevel == 0)
                    Helpers.Login_Response(client, SC_Login.Login_Result.Failed, "Zone is in permission only mode.");
            }

            String alias = newPlayer._alias;

            //Check alias for illegal characters, may need overhauling.
            if (alias.Length == 0)
                Helpers.Login_Response(client, SC_Login.Login_Result.Failed, "Alias cannot be blank.");
            if (!char.IsLetterOrDigit(alias, 0) ||
                char.IsWhiteSpace(alias, 0) ||
                char.IsWhiteSpace(alias, alias.Length - 1))
            {   //Boot him..
                Helpers.Login_Response(client, SC_Login.Login_Result.Failed, "Alias contains illegal characters, Cannot start or end with a space.");
            }

			//If it failed for some reason, present a failure message
			if (newPlayer == null)
			{
				Helpers.Login_Response(client, SC_Login.Login_Result.Failed, "Unknown login failure.");
				return;
			}

			//Are we in standalone mode?
			if (server.IsStandalone)
			{	//Always first time setup in standalone mode
				newPlayer.assignFirstTimeStats(false);

				//Success! Let him in.
				Helpers.Login_Response(client, SC_Login.Login_Result.Success, "This server is currently in stand-alone mode. Your character's scores and stats will not be saved.");
			}
			else
			{	//Defer the request to the database server
				CS_PlayerLogin<Data.Database> plogin = new CS_PlayerLogin<Data.Database>();

				plogin.bCreateAlias = pkt.bCreateAlias;

				plogin.player = newPlayer.toInstance();
				plogin.alias = pkt.Username;
				plogin.ticketid = pkt.TicketID;
				plogin.UID1 = pkt.UID1;
				plogin.UID2 = pkt.UID2;
                plogin.UID3 = pkt.UID3;
				plogin.UID4 = pkt.NICInfo;
                plogin.ipaddress = pkt._client._ipe.Address.ToString().Trim();

				server._db.send(plogin);
			}
		}

		/// <summary>
		/// Triggered when the client is ready to download the game state
		/// </summary>
		static public void Handle_CS_Ready(CS_Ready pkt, Player player)
		{
			using (LogAssume.Assume(player._server._logger))
			{	//Fake patch server packet
				SC_PatchInfo pinfo = new SC_PatchInfo();

				pinfo.Unk1 = false;
				pinfo.patchServer = "patch.station.sony.com";
				pinfo.patchPort = 0x1b58;
				pinfo.patchXml = "patch/infantry/Zone7130.xml";

				player._client.sendReliable(pinfo);

				//Send him the asset list	
				SC_AssetInfo assets = new SC_AssetInfo(Helpers._server._assets.getAssetList());

                //Optional updates?
                if (player.PermissionLevel == Data.PlayerPermission.Sysop)
                    assets.bOptionalUpdate = true;
                else
                    assets.bOptionalUpdate = false;

				player._client.sendReliable(assets);

				//We can now consider him past the login process
				player._bLoggedIn = true;
			}
		}

		/// <summary>
		/// Registers all handlers
		/// </summary>
		[Logic.RegistryFunc]
		static public void Register()
		{
			CS_Login.Handlers += Handle_CS_Login;
			CS_Ready.Handlers += Handle_CS_Ready;
		}
	}
}
