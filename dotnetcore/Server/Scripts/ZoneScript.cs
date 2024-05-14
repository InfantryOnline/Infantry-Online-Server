using InfServer.Game;
using InfServer.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static InfServer.Game.Arena;

namespace InfServer.Scripting
{
    /// <summary>
    /// Determines whether or not the core arena script will execute after our custom script event handler.
    /// </summary>
    public enum ScriptEventExecutionResult
    {
        /// <summary>
        /// Prevent the default core arena script execution.
        /// </summary>
        PreventDefault,

        /// <summary>
        /// Core arena script will execute after our event handler runs.
        /// </summary>
        ContinueWithDefault
    }

    /// <summary>
    /// Base class for all zone scripts. This is the main driver class and one that you need to implement
    /// for your zone scripts.
    /// </summary>
    /// <remarks>
    /// 
    /// </remarks>
    public abstract class ZoneScript
    {
        public ScriptArena Arena { get; private set; }

        public ZoneScript(ScriptArena arena)
        {
            Arena = arena;
        }

        //
        // System Events.
        //

        /// <summary>
        /// Called upon the arena being initialized.
        /// </summary>
        public abstract void Init();

        /// <summary>
        /// Called by the arena once per server tick to update any script logic.
        /// </summary>
        public abstract void Poll();

        /// <summary>
        /// Optional. Called when the arena (or server) is being shut down.
        /// </summary>
        public void Shutdown() { }


        //
        // Player <-> Arena Events.
        //

        /// <summary>
        /// Optional. Invoked when player joins the arena.
        /// </summary>
        /// <param name="player"></param>
        public void PlayerEnterArena(Player player) { }

        /// <summary>
        /// Optional. Invoked when player leaves the arena.
        /// </summary>
        /// <param name="player"></param>
        public void PlayerLeaveArena(Player player) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="player"></param>
        public void PlayerEnter(Player player) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="player"></param>
        public void PlayerLeave(Player player) { }

        /// <summary>
        /// Optional. Invoked when player unspectates and is placed into the game to play.
        /// </summary>
        /// <param name="player"></param>
        public void PlayerJoinGame(Player player) { }

        /// <summary>
        /// Optional. Invoked when player spectates and is taken out of the game.
        /// </summary>
        /// <param name="player"></param>
        public void PlayerLeaveGame(Player player) { }


        //
        // Game Events.
        //

        public void GameStart() { }

        public void GameEnd() { }

        public void GameReset() { }

        public void StartLeagueMatch() { }

        public void StopLeagueMatch() { }


        //
        // Pickup / Drop Events.
        //

        public void PlayerBallPickup(Player from, Ball ball) { }

        public void PlayerBallDrop(Player from, Ball ball) { }

        public void PlayerItemPickup(Player from, ItemDrop drop, ushort quantity) { }

        public void PlayerItemDrop(Player from, ItemDrop drop, ushort quantity) { }


    }
}
