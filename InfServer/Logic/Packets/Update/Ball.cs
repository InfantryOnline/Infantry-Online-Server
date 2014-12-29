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
                Log.write(TLog.Warning, "Player {0} attempted to score a goal from spec.", player);
                return;
            }

            // Arena handles it, so they can keep track of the score
            player._arena.handleEvent(delegate(Arena arena)
            {
                player._arena.handlePlayerGoal(player, pkt);
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

            if (player.IsDead)
            {
                Log.write(TLog.Warning, "Player {0} attempted to pick up a ball while dead.", player);
                return;
            }

            //Let the arena handle it too
            player._arena.handleEvent(delegate(Arena arena)
            {
                player._arena.handleBallPickup(player, pkt);
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
