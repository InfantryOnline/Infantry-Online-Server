using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Protocol;
using InfServer.Data;
using InfServer.Game;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using DBComm.Enums;

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
		{
            //Attempt to find the player in question
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
                player.bannerMode = (BannerMode)pkt.bannermode;
            }
            else
            {
                //First time loading!
                player.assignFirstTimeStats(true);
            }

			//Let him in! Set his alias, squad and permissions
			Helpers.Login_Response(player._client, SC_Login.Login_Result.Success, pkt.loginMessage);
            player._permissionStatic = pkt.permission;
            player._developer = pkt.developer;
            player._admin = pkt.admin;
            player._bIsStealth = pkt.stealth;
            player._alias = pkt.alias;
            player._squad = pkt.squad;
            player._squadID = pkt.squadID;

            player._bDBLoaded = true;

            if (pkt.silencedDurationMinutes > 0)
            {
                var silenceDateTime = DateTimeOffset.FromUnixTimeMilliseconds(pkt.silencedAtUnixMilliseconds).LocalDateTime;

                if (player._ipAddress == null)
                {
                    Log.write(TLog.Warning, "IP Address is null in CS_Handle_PlayerLogin.");
                }
                else
                {
                    var silencedPlayer = new SilencedPlayer
                    {
                        Alias = player._alias,
                        IPAddress = player._ipAddress,
                        DurationMinutes = (int)pkt.silencedDurationMinutes,
                        SilencedAt = silenceDateTime
                    };

                    db._server._playerSilenced.Add(silencedPlayer);

                    player._bSilenced = true;

                    player._timeOfSilence = silenceDateTime;
                    player._lengthOfSilence = (int)pkt.silencedDurationMinutes;
                }   
            }

            //
            // In case of re-connect (db server is down, then back up)
            // this ensures that the the latest state database has for
            // stats/inventory/skills will be applied and sent to the
            // client.
            //
            player.syncState();
        }

        /// <summary>
        /// Handles re-routing of whisper messages to the appropriate player
        /// </summary>
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

        static public void Handle_SC_DisconnectPlayer(SC_DisconnectPlayer<Database> pkt, Database db)
        {
            var player = db._server.getPlayer(pkt.alias);

            if (player != null)
            {
                player.disconnect();
            }
        }

        static public void Handle_SC_Silence(SC_Silence<Database> pkt, Database db)
        {
            var player = db._server.getPlayer(pkt.alias);

            // Only apply to player if they are in the zone, otherwise it doesn't matter.

            if (player == null)
            {
                return;
            }

            var existingSilencedPlayer = db._server._playerSilenced.FirstOrDefault(p => p.Alias == pkt.alias);

            //
            // Player wasn't silenced to begin with? Might be an error.
            //
            if (existingSilencedPlayer == null && pkt.minutes <= 0)
            {
                return;
            }

            // Get local time from the unix timestamp.

            var silencedAt = DateTimeOffset.FromUnixTimeMilliseconds(pkt.silencedAtUnixMs).LocalDateTime;

            if (existingSilencedPlayer != null)
            {
                existingSilencedPlayer.DurationMinutes += (int)pkt.minutes;
            }
            else
            {
                existingSilencedPlayer = new SilencedPlayer
                {
                    Alias = player._alias,
                    IPAddress = player._ipAddress,
                    SilencedAt = silencedAt,
                    DurationMinutes = (int)pkt.minutes,
                };

                db._server._playerSilenced.Add(existingSilencedPlayer);
            }

            if (pkt.minutes <= 0 || existingSilencedPlayer.DurationMinutes <= 0)
            {
                player._bSilenced = false;
                player._lengthOfSilence = 0;
                db._server._playerSilenced.Remove(existingSilencedPlayer);
            }
            else
            {
                player._timeOfSilence = silencedAt;
                player._lengthOfSilence = (int)pkt.minutes;
                player._bSilenced = true;
            }

            //
            // TODO: Maybe we want to send out a message to the player saying they are no longer silenced.
            //
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
            SC_DisconnectPlayer<Database>.Handlers += Handle_SC_DisconnectPlayer;
            SC_Silence<Database>.Handlers += Handle_SC_Silence;
		}
	}
}
