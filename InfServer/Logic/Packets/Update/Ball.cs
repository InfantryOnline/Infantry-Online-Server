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
        {
            //Get the ball in question..
            Ball ball = player._arena._balls.FirstOrDefault(b => b._id == pkt.ballID);

            if (ball == null)
            {
                Log.write(TLog.Warning, "Balldrop packet sent for ball that does not exist");
                return;
            }

            //Allow the player's arena to handle it
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


            //Drop the ball
            ball._state.carrier = null;
            ball._state.positionX = pkt.positionX;
            ball._state.positionY = pkt.positionY;
            ball._state.positionZ = pkt.positionZ;
            ball._state.velocityX = pkt.velocityX;
            ball._state.velocityY = pkt.velocityY;
            ball._state.velocityZ = pkt.velocityZ;

            //Route it
            ball.Route_Ball(player._arena.Players);



            //Old Stuff
            /*SC_BallState state = new SC_BallState();
            state.ballID = (ushort)pkt.ballID;
            //state.playerID = (short)pkt.bPickup;                
            state.positionX = pkt.positionX;
            state.positionY = pkt.positionY;
            state.positionZ = pkt.positionZ;
            state.velocityX = pkt.velocityX;
            state.velocityY = pkt.velocityY;
            state.velocityZ = pkt.velocityZ;
            state.playerID = pkt.playerID;
            state.unk1 = pkt.unk1;
            state.unk2 = pkt.unk2;
            state.unk3 = pkt.unk3;
            state.unk4 = pkt.unk4;
            state.unk5 = pkt.unk5;
            state.unk6 = pkt.unk6;
            state.unk7 = pkt.unk7;
            //state.something1 = pkt.something1;
            //state.something2 = pkt.something2;

            //state.bPickup = (short)0;
            //state.ballPickupID = (short)pkt.playerID;*/


            player._arena.handleEvent(delegate(Arena arena)
            {
                player._arena.handleBallDrop(player, pkt);
            });
        }

        /// <summary>
        /// Handles a goal scored request from a client
        /// </summary>
        static public void Handle_CS_GoalScored(CS_GoalScored pkt, Player player)
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


            foreach (Player p in player._arena.Players)
            {
                SC_BallState state = new SC_BallState();
                state.playerID = (short)player._id;
                state.ballID = (ushort)pkt.ballID;
                //state.bPickup = (short)1;

                p._client.send(state);
            }
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


            foreach (Player p in player._arena.Players)
            {
                SC_BallState state = new SC_BallState();
                state.playerID = (short)player._id;
                state.ballID = (ushort)pkt.ballID;
                //state.bPickup = (short)1;
                //state.unk4 = (short)pkt.unk1;
                //state.unk5 = (short)pkt.unk2;
                //state.unk6 = (short)pkt.unk3;
                //state.unk7 = (short)pkt.unk4;

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
            CS_GoalScored.Handlers += Handle_CS_GoalScored;
		}
	}
}
