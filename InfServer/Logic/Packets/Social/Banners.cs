using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Protocol;
using InfServer.Game;

namespace InfServer.Logic
{	// Logic_Banners Class
	/// Deals with all banner packets
	///////////////////////////////////////////////////////
	class Logic_Banners
	{	/// <summary>
		/// Handles set banner packets sent from the client
		/// </summary>
		static public void Handle_CS_SetBanner(CS_SetBanner pkt, Player player)
		{	//Set the player's new banner data
			player._bannerData = pkt.bannerData;

			//Inform everyone in the arena
			if (player._arena != null)
				Helpers.Social_ArenaBanners(player._arena.Players, player);

			//Shall we send it to the database?
			if (!player._arena._server.IsStandalone)
			{	//Defer the request to the database server
				CS_PlayerBanner<Data.Database> plogin = new CS_PlayerBanner<Data.Database>();

				plogin.player = player.toInstance();
				plogin.banner = player._bannerData;

				player._arena._server._db.send(plogin);
			}
		}

        /// <summary>
        /// Handles sendingTo banner requests from the client
        /// </summary>
        static public void Handle_CS_SendBanner(CS_SendBanner pkt, Player player)
        {
            Player target = null;

            //Lets find the player in question
            if ((target = player._arena.getPlayerById(pkt.playerID)) == null)
            {
                Log.write(TLog.Warning, "Sending banner request without a proper player ID from player {0}", player._alias);
                return;
            }

            if (target == player)
            {
                player.sendMessage(-1, "&You tried sending yourself a banner.");
                return;
            }

            if (pkt.bannerData == null)
                return;

            if (!target._bAllowBanner)
            {
                player.sendMessage(-1, "&Target is not allowing banners.");
                return;
            }

            SC_SendBanner banner = new SC_SendBanner();
            banner.playerID = (short)player._id;
            banner.bannerData = pkt.bannerData;

            //Send it
            target._client.sendReliable(banner);
        }

		/// <summary>
		/// Registers all handlers
		/// </summary>
		[Logic.RegistryFunc]
		static public void Register()
		{
			CS_SetBanner.Handlers += Handle_CS_SetBanner;
            CS_SendBanner.Handlers += Handle_CS_SendBanner;
		}
	}
}
