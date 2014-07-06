using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Protocol;
using InfServer.Data;
using InfServer;

namespace InfServer.Logic
{	// Logic_Player Class
	/// Handles various player related functionality
	///////////////////////////////////////////////////////
	class Logic_Player
	{
		/// <summary>
		/// Handles a player update request
		/// </summary>
		static public void Handle_CS_PlayerUpdate(CS_PlayerUpdate<Zone> pkt, Zone zone)
		{	//Attempt to find the player in question
			Zone.Player player = zone.getPlayer(pkt.player.id);
			if (player == null)
			{	//Make a note
				Log.write(TLog.Warning, "Ignoring player update for #{0}, not present in zone mirror.", pkt.player.id);
				return;
			}

			using (InfantryDataContext db = zone._server.getContext())
			{	//Get the associated player entry
				Data.DB.player dbplayer = db.players.SingleOrDefault(plyr => plyr.id == player.dbid);
				if (dbplayer == null)
				{	//Make a note
					Log.write(TLog.Warning, "Ignoring player update for {0}, not present in database.", player.alias);
					return;
				}

				//Update his stats object
				Data.DB.stats stats = dbplayer.stats1;

				stats.zonestat1 = pkt.stats.zonestat1;
				stats.zonestat2 = pkt.stats.zonestat2;
				stats.zonestat3 = pkt.stats.zonestat3;
				stats.zonestat4 = pkt.stats.zonestat4;
				stats.zonestat5 = pkt.stats.zonestat5;
				stats.zonestat6 = pkt.stats.zonestat6;
				stats.zonestat7 = pkt.stats.zonestat7;
				stats.zonestat8 = pkt.stats.zonestat8;
				stats.zonestat9 = pkt.stats.zonestat9;
				stats.zonestat10 = pkt.stats.zonestat10;
				stats.zonestat11 = pkt.stats.zonestat11;
				stats.zonestat12 = pkt.stats.zonestat12;

				stats.kills = pkt.stats.kills;
				stats.deaths = pkt.stats.deaths;
				stats.killPoints = pkt.stats.killPoints;
				stats.deathPoints = pkt.stats.deathPoints;
				stats.assistPoints = pkt.stats.assistPoints;
				stats.bonusPoints = pkt.stats.bonusPoints;
				stats.vehicleKills = pkt.stats.vehicleKills;
				stats.vehicleDeaths = pkt.stats.vehicleDeaths;
				stats.playSeconds = pkt.stats.playSeconds;

				stats.cash = pkt.stats.cash;
				stats.experience = pkt.stats.experience;
				stats.experienceTotal = pkt.stats.experienceTotal;

				//Convert inventory and skills
				dbplayer.inventory = DBHelpers.inventoryToBin(pkt.stats.inventory);
				dbplayer.skills = DBHelpers.skillsToBin(pkt.stats.skills);

				//Update all changes
				db.SubmitChanges();
			}
		}

		/// <summary>
		/// Handles a player banner update
		/// </summary>
		static public void Handle_CS_PlayerBanner(CS_PlayerBanner<Zone> pkt, Zone zone)
		{	//Attempt to find the player in question
			Zone.Player player = zone.getPlayer(pkt.player.id);
			if (player == null)
			{	//Make a note
				Log.write(TLog.Warning, "Ignoring player banner update for #{0}, not present in zone mirror.", pkt.player.id);
				return;
			}

			using (InfantryDataContext db = zone._server.getContext())
			{	//Get the associated player entry
				Data.DB.player dbplayer = db.players.SingleOrDefault(plyr => plyr.id == player.dbid);
				if (dbplayer == null)
				{	//Make a note
					Log.write(TLog.Warning, "Ignoring player banner update for {0}, not present in database.", player.alias);
					return;
				}

				dbplayer.banner = pkt.banner;

				//Update all changes
				db.SubmitChanges();
			}
		}

        /// <summary>
        /// Handles a chat whisper
        /// </summary>
        static public void Handle_CS_Whisper(CS_Whisper<Zone> pkt, Zone zone)
        {
            foreach (Zone z in zone._server._zones)
            {
                if (z.hasAliasPlayer(pkt.recipient))
                {
                    SC_Whisper<Zone> reply = new SC_Whisper<Zone>();
                    reply.bong = pkt.bong;
                    reply.message = pkt.message;
                    reply.recipient = pkt.recipient;
                    reply.from = pkt.from;
                    z._client.send(reply);
                }
            }
        }

        /// <summary>
        /// Handles an arena update from a player
        /// </summary>
        static public void Handle_CS_ArenaUpdate(CS_ArenaUpdate<Zone> pkt, Zone zone)
        {
            //Attempt to find the player in question
            Zone.Player player = zone.getPlayer(pkt.player.id);
            if (player == null)
            {	//Make a note
                Log.write(TLog.Warning, "Ignoring arena update for #{0}, not present in zone mirror.", pkt.player.id);
                return;
            }

            player.arena = pkt.arena;
        }

		/// <summary>
		/// Registers all handlers
		/// </summary>
		[RegistryFunc]
		static public void Register()
		{
			CS_PlayerUpdate<Zone>.Handlers += Handle_CS_PlayerUpdate;
			CS_PlayerBanner<Zone>.Handlers += Handle_CS_PlayerBanner;
            CS_Whisper<Zone>.Handlers += Handle_CS_Whisper;
            CS_ArenaUpdate<Zone>.Handlers += Handle_CS_ArenaUpdate;
		}
	}
}
