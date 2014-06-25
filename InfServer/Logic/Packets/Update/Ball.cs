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
        /// Handles a ball drop request from a client
        /// </summary>
        static public void Handle_CS_BallDrop(CS_BallDrop pkt, Player player)
        {   //Allow the arena to handle it
            if (player._arena == null)
            {
                Log.write(TLog.Error, "Player {0} sent update packet with no arena.", player);
                return;
            }

            if (player.IsSpectator)
            {
                Log.write(TLog.Warning, "Player {0} attempted to drop a ball from spec.", player);
                return;
            }

            //Get the ball in question..
            Ball ball = player._arena._balls.FirstOrDefault(b => b._id == pkt.ballID);
            if (ball == null)
            {
                Log.write(TLog.Warning, "Balldrop packet sent for ball that does not exist");
                return;
            }

            if (ball._state.inProgress == 1)
            {
                Log.write(TLog.Warning, "Current ball action in progress");
                return;
            }

            //if (ball._state.carrier._id > 0)
            // {
            //     Log.write("Tried to pickup a ball already in someone else hands!");
            //     return;
            // }
            /*
            string format = String.Format("DROP - boolPickup {0} ballID {1} velX {2} velY {3} velZ {4} posX {5} posY {6} posZ {7} pID {8}", pkt.bPickup, pkt.ballID, pkt.velocityX,
                    pkt.velocityY, pkt.velocityZ, pkt.positionX, pkt.positionY, pkt.positionZ, pkt.playerID);
            string mat = String.Format("unk1 {0} unk2 {1} unk3 {2} unk4 {3} unk5 {4} unk6 {5} unk7 {6}", pkt.unk1, pkt.unk2, pkt.unk3, pkt.unk4, pkt.unk5, pkt.unk6, pkt.unk7);
            player.sendMessage(0, String.Format("{0} {1}", format, mat));
            */

            //Drop the ball
            ball._state.carrier = player;
            ball._state.positionX = pkt.positionX;
            ball._state.positionY = pkt.positionY;
            ball._state.positionZ = pkt.positionZ;
            ball._state.velocityX = pkt.velocityX;
            ball._state.velocityY = pkt.velocityY;
            ball._state.velocityZ = pkt.velocityZ;
            ball._state.unk1 = pkt.unk1;
            ball._state.ballStatus = pkt.unk2;
            ball._state.unk3 = pkt.unk3;
            ball._state.unk4 = pkt.unk4;
            ball._state.unk5 = pkt.unk5;
            ball._state.unk6 = pkt.unk6;
            ball._state.unk7 = pkt.unk7;
            player._gotBallID = 999;
            ball._owner = null;
            ball._lastOwner = player;
            //Route it
            ball.Route_Ball(player._arena.Players);

            player._arena.handleEvent(delegate(Arena arena)
            {  
                player._arena.handleBallDrop(player, ball);
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
                Log.write(TLog.Warning, "Player {0} attempted to score a goal from spec.", player);
                return;
            }

            //Get the ball in question..
            Ball ball = player._arena._balls.FirstOrDefault(b => b._id == pkt.ballID);
            if (ball == null)
            {
                Log.write(TLog.Warning, "GoalScored packet sent with ball that does not exist");
                return;
            }

            if (ball._state.inProgress == 1)
            {
                Log.write(TLog.Warning, "Current ball action in progress");
                return;
            }

        //    player.sendMessage(0, String.Format("SCORE - ballID {0} unk1 {1} unk2 {2} unk3 {3} unk4 {4} posX {5} posY {6}", pkt.ballID, pkt.unk1, pkt.unk2, pkt.unk3, pkt.unk4, pkt.positionX, pkt.positionY));
            player._gotBallID = 999;

            // Arena handles it, so they can keep track of the score
            player._arena.handleEvent(delegate(Arena arena)
            {
                player._arena.handlePlayerGoal(player, ball);
            });
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
                Log.write(TLog.Warning, "Player {0} attempted to pick up a ball from spec.", player);
                return;
            }

            //Get the ball in question..
            Ball ball = player._arena._balls.FirstOrDefault(b => b._id == pkt.ballID);
            if (ball == null)
            {
                Log.write(TLog.Warning, "Ballpickup packet sent for ball that does not exist");
                return;
            }

            if (ball._state.inProgress == 1)
            {
                Log.write(TLog.Warning, "Current ball action in progress");
                return;
            }

            // Code here meant to check the player doesnt have a ball. Might work fine after i fix bugs
            // if (player._gotBallID != 999)
            // {
            //     Log.write(TLog.Warning, "Player {0} already has a ball.", player);
            //     return;
            // }

            //string format = String.Format("PICKUP - ballID {0} test {1} unk1 {2} unk2 {3} unk3 {4} unk4 {5}", pkt.ballID, pkt.test, pkt.unk1, pkt.unk2, pkt.unk3, pkt.unk4);
         //   player.sendMessage(0, format);

            //Pick up the ball
            ball._state.inProgress = 1;
            ball._state.carrier = player;
            ball._state.positionX = player._state.positionX;
            ball._state.positionY = player._state.positionY;
            ball._state.positionZ = player._state.positionZ;
            ball._state.velocityX = 0;
            ball._state.velocityY = 0;
            ball._state.velocityZ = 0;
            player._gotBallID = pkt.ballID;
            ball._state.ballStatus = 0;
            ball._owner = player;
            ball.Route_Ball(player._arena.Players);
            ball.deadBall = false;
            ball._state.inProgress = 0;

            //Let the arena handle it too
            player._arena.handleEvent(delegate(Arena arena)
            {
                player._arena.handleBallPickup(player, ball);
            });
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
