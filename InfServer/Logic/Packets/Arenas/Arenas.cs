using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using InfServer.Protocol;
using InfServer.Game;

namespace InfServer.Logic
{	// Logic_Arenas Class
	/// Deals with players entering, leaving and requesting arenas
	///////////////////////////////////////////////////////
	class Logic_Arenas
	{
		/// <summary>
		/// Triggered when the client is attempting to join an arena, complete with security credentials
		/// </summary>
		static public void Handle_CS_ArenaJoin(CS_ArenaJoin pkt, Player player)
		{	//If the player isn't logged in, ignore
			if (!player._bLoggedIn)
			{	//Log and abort
				Log.write(TLog.Warning, "Player {0} tried to send security update while not logged in.", player);
				player.disconnect();
				return;
			}

			//If he's in an arena, get him out of it
			if (player._arena != null)
				player.leftArena();

			//Does he have a specific arena to join?
			Arena match = null;

			if (pkt.ArenaName != "" && pkt.ArenaName != "-2")
				match = player._server.playerJoinArena(player, pkt.ArenaName);

			if (match == null)
				//We need to find our player an arena to inhabit..
				match = player._server.allocatePlayer(player);

			//If we're unable to find an arena, abort
			if (match == null)
			{
				Log.write(TLog.Warning, "Unable to allocate player '{0}' an arena.", player._alias);
				player.disconnect();
				return;
			}

			//Add him to the arena
			match.newPlayer(player);

            //TODO: Compare to the server's checksum instead.
            if (player._assetCS == 0)
            {
                player._assetCS = pkt.AssetChecksum;
            }
            else
            {
                //What the tomfoolery is goin' here?
                if (player._assetCS != pkt.AssetChecksum)
                {//Kick the fucker

                    //Alert any moderators first.
                    foreach (Player p in player._arena.Players)
                    {
                        if (p.PermissionLevelLocal > Data.PlayerPermission.Normal)
                            p.sendMessage(0, String.Format("![Security] Player {0} kicked. Checksum mismatch - (Original={1} New={2})", player._alias, player._assetCS, pkt.AssetChecksum));
                    }

                    //Log it
                    Log.write(TLog.Warning, "[Security] Player {0} kicked. Checksum mismatch - (Original={1} New={2})", player._alias, player._assetCS, pkt.AssetChecksum);

                    //Bye!
                    player.disconnect();
                }
            }
		}

		/// <summary>
		/// Registers all handlers
		/// </summary>
		[Logic.RegistryFunc]
		static public void Register()
		{
			CS_ArenaJoin.Handlers += Handle_CS_ArenaJoin;
		}
	}
}
