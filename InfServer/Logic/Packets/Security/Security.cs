using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Protocol;
using InfServer.Game;

namespace InfServer.Logic
{	// Logic_Security Class
	/// Deals with elements of the server's security
	///////////////////////////////////////////////////////
	class Logic_Security
	{
		/// <summary>
		/// Triggered when the client is attempting to enter the game and sends his security reply
		/// </summary>
		static public void Handle_CS_Environment(CS_Environment pkt, Player player)
		{	//Does he have a target ready to receive the information?
			Player target = player.getVar("envReq") as Player;
			if (target == null)
				return;
			player.setVar("envReq", null);

			//Display to him the results
			target.sendMessage(0, "&Processes:");

			foreach (string element in pkt.processes)
                target.sendMessage(0, "*" + Logic_Text.RemoveIllegalCharacters(element));

			target.sendMessage(0, "&Windows:");

            foreach (string element in pkt.windows)
                target.sendMessage(0, "*" + Logic_Text.RemoveIllegalCharacters(element));
		}

        /// <summary>
		/// Triggered when the client has responsed to a security request
		/// </summary>
        static public void Handle_CS_Security(CS_SecurityCheck pkt, Player player)
        {
            Log.write("Security dump: " + pkt.DataDump);
        }

		/// <summary>
		/// Registers all handlers
		/// </summary>
		[Logic.RegistryFunc]
		static public void Register()
		{
			CS_Environment.Handlers += Handle_CS_Environment;
            CS_SecurityCheck.Handlers += Handle_CS_Security;
		}
	}
}
