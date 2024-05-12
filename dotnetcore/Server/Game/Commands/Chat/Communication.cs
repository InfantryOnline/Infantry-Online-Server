using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using InfServer.Protocol;
using InfServer.Game.Commands;
using Assets;
using InfServer.Logic;

namespace InfServer.Game.Commands.Chat
{
    /// <summary>
    /// Provides a series of functions for handling chat communication commands (starting with %)
    /// Please write commands in this class in alphabetical order!
    /// </summary>
    public class Communication
    {
        public static void gameTimer(Player player, Player recipient, string payload, int bong)
        {
            //Lets find it
            int Time = 0;
            foreach (Arena.TickerInfo ticker in player._arena._tickers.Values)
            {
                if (ticker.idx == player._arena.playtimeTickerIdx && ticker.timer > 0)
                {
                    Time = ticker.timer;
                    break;
                }
            }

            if (Time == 0)
            {
                player.sendMessage(-1, "Cannot find a game timer in this zone.");
                return;
            }

            Time = (Time - Environment.TickCount) / 1000;
            double minutes = Math.Floor((double)(Time / 60) % 60);
            double seconds = Math.Floor((double)(Time % 60));
            string timeLeft = string.Format("{0}:{1}", minutes.ToString(), (seconds < 10 ? "0" + seconds.ToString() : seconds.ToString()));
            player.sendMessage(0, "Time Remaining: " + timeLeft);
        }

        #region Registrar
        /// <summary>
        /// Registers all handlers
        /// </summary>
        [Commands.RegistryFunc(HandlerType.CommCommand)]
        public static IEnumerable<Commands.HandlerDescriptor> Register()
        {
            yield return new HandlerDescriptor(gameTimer, "gametimer",
                "Displays the current time left for a game.",
                "%gametimer");
        }
        #endregion
    }
}
