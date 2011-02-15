using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Protocol;
using InfServer.Data;

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

				stats.altstat1 = pkt.stats.altstat1;
				stats.altstat2 = pkt.stats.altstat2;
				stats.altstat3 = pkt.stats.altstat3;
				stats.altstat4 = pkt.stats.altstat4;
				stats.altstat5 = pkt.stats.altstat5;
				stats.altstat6 = pkt.stats.altstat6;
				stats.altstat7 = pkt.stats.altstat7;
				stats.altstat8 = pkt.stats.altstat8;

				stats.points = pkt.stats.points;
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
		/// Registers all handlers
		/// </summary>
		[RegistryFunc]
		static public void Register()
		{
			CS_PlayerUpdate<Zone>.Handlers += Handle_CS_PlayerUpdate;
		}
	}
}
