using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Protocol;
using InfServer.Data;
using InfServer.Game;

namespace InfServer.Logic
{	// Logic_Player Class
	/// Deals with player specific database packets
	///////////////////////////////////////////////////////
	class Logic_Player
	{	
		/// <summary>
		/// Handles the servers's player login reply
		/// </summary>
		static public void Handle_SC_PlayerLogin(SC_PlayerLogin<Database> pkt, Database db)
		{	//Attempt to find the player in question
			Player player = db._server.getPlayer(pkt.player);
			if (player == null)
			{
				Log.write(TLog.Warning, "Received login reply for unknown player instance.");
				return;
			}
			
			//Failure?
			if (!pkt.bSuccess)
			{	//Is it a create alias prompt?
				if (pkt.bNewAlias)
					Helpers.Login_Response(player._client, SC_Login.Login_Result.CreateAlias);
				else
					//Notify with the login message if present
					Helpers.Login_Response(player._client, SC_Login.Login_Result.Failed, pkt.loginMessage);
				return;
			}

			//Do we want to load stats?
            if (!pkt.bFirstTimeSetup)
            {	//Assign the player stats
                player.assignStats(pkt.stats);
                player._bannerData = pkt.banner;
            }
            else
            {
                //First time loading!
                player.assignFirstTimeStats(true);
                Log.write(TLog.Warning, "SC_Playerlogin called, assiging first time stats.");
            }

			//Let him in! Set his alias, squad and permissions
			Helpers.Login_Response(player._client, SC_Login.Login_Result.Success, pkt.loginMessage);
            player._permissionStatic = pkt.permission;
            player._alias = pkt.alias;
            player._squad = pkt.squad;
            player._squadID = pkt.squadID;
        }

        static public void Handle_SC_Whisper(SC_Whisper<Database> pkt, Database db)
        {
            Player recipient = db._server.getPlayer(pkt.recipient);
            Player from = db._server.getPlayer(pkt.from);
            
            if (recipient != null)
                recipient.sendPlayerChat(from, pkt);
        }

        /// <summary>
        /// Handles re-routing of db related messages.
        /// </summary>
        static public void Handle_DB_Chat(SC_Chat<Database> pkt, Database db)
        {
            if (pkt.recipient == "*")
            {   //Route it to everybody
                List<Arena> targetarenas = db._server._arenas.Values.ToList();
                List<Player> targets = new List<Player>();
                foreach (Arena a in targetarenas)
                    foreach (Player pl in a.Players)
                        targets.Add(pl);
                Helpers.Social_ArenaChat(targets, pkt.message, 0);
                return;
            }

            //Route it to a single player
            Player p = db._server.getPlayer(pkt.recipient);
            if (p == null)
                return;
            //Route it.
            Helpers.Social_ArenaChat(p, pkt.message, 0);
        }

        /// <summary>
        /// Handles re-routing of a zonelist message to the appropriate player
        /// </summary>
        static public void Handle_SC_ZoneList(SC_Zones<Database> pkt, Database db)
        {
            Player recipient = db._server.getPlayer(pkt.requestee);
            if (recipient == null)
                return;

            //Give him his list of zones!
            SC_ZoneList zl = new SC_ZoneList(pkt.zoneList, recipient);
            recipient._client.sendReliable(zl, 1);
        }

		/// <summary>
		/// Registers all handlers
		/// </summary>
		[Logic.RegistryFunc]
		static public void Register()
		{
			SC_PlayerLogin<Database>.Handlers += Handle_SC_PlayerLogin;
            SC_Whisper<Database>.Handlers += Handle_SC_Whisper;
            SC_Chat<Database>.Handlers += Handle_DB_Chat;
            SC_Zones<Database>.Handlers += Handle_SC_ZoneList;
		}
	}
}
