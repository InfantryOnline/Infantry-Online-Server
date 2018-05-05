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

        }

        #region Registrar
        /// <summary>
        /// Registers all handlers
        /// </summary>
        [Commands.RegistryFunc(HandlerType.ChatCommand)]
        public static IEnumerable<Commands.HandlerDescriptor> Register()
        {
            yield return new HandlerDescriptor(gameTimer, "gametimer",
                "Displays the current time left for a game.",
                "%gametimer");
        }
        #endregion
    }
}
