using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Protocol;
using InfServer.Game;

using Assets;

namespace InfServer.Logic
{	// Logic_Soccer Class
	/// Handles all updates related to the soccer state
	///////////////////////////////////////////////////////
	class Logic_Soccer
	{

        /// <summary>
        /// Handles a ball DROP request from a client
        /// </summary>
        static public void Handle_CS_BallDrop(CS_BallDrop pkt, Player player)
        {	//Allow the player's arena to handle it
            if (player._arena == null)
            {
                Log.write(TLog.Error, "Player {0} sent update packet with no arena.", player);
                return;
            }

            if (player.IsSpectator)
            {
                Log.write(TLog.Warning, "Player {0} attempted to activate a flag from spec.", player);
                return;
            }

            SC_BallState state = new SC_BallState();
            state.playerID = (short)pkt.bPickup;
            state.ballID = (ushort)pkt.ballID;
            state.positionX = pkt.positionX;
            state.positionY = pkt.positionY;
            state.positionZ = pkt.positionZ;
            state.velocityX = pkt.velocityX;
            state.velocityY = pkt.velocityY;
            state.velocityZ = pkt.velocityZ;
            state.unk1 = pkt.unk1;
            state.unk2 = pkt.unk2;
            state.unk3 = pkt.unk3;
            state.unk4 = pkt.unk4;
            state.unk5 = pkt.unk5;
            state.unk6 = pkt.unk6;

            state.something1 = pkt.something1;
            state.something2 = pkt.something2;
            state.unk7 = pkt.unk7;
            //state.bPickup = (short)0;
            //state.ballPickupID = (short)pkt.playerID;


            foreach (Player p in player._arena.Players)
                p._client.send(state);
           
            //player._arena.handleEvent(delegate(Arena arena)
            //{
            //    player._arena.handleBallPickup(player, pkt);
            //});


        }

        /// <summary>
        /// Handles a ball pickup request from a client
        /// </summary>
        static public void Handle_CS_BallPickup(CS_BallPickup pkt, Player player)
        {	//Allow the player's arena to handle it
            if (player._arena == null)
            {
                Log.write(TLog.Error, "Player {0} sent update packet with no arena.", player);
                return;
            }

            if (player.IsSpectator)
            {
                Log.write(TLog.Warning, "Player {0} attempted to activate a flag from spec.", player);
                return;
            }


            SC_BallState state = new SC_BallState();
            state.playerID = (short)player._id;
            state.ballID = (ushort)pkt.ballID;
            state.bPickup = (short)1;

            foreach (Player p in player._arena.Players)
            {


                p._client.send(state);
            }
            //player._arena.handleEvent(delegate(Arena arena)
            //{
            //    player._arena.handleBallPickup(player, pkt);
            //});


        }

		
		/// <summary>
		/// Registers all handlers
		/// </summary>
		[Logic.RegistryFunc]
		static public void Register()
		{
			CS_BallPickup.Handlers += Handle_CS_BallPickup;
            CS_BallDrop.Handlers += Handle_CS_BallDrop;
		}
	}
}
