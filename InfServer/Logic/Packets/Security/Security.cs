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
        public static uint reliable;
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
            {
                if (element.Contains("firefox|oracle|iexplore"))
                    continue;
                target.sendMessage(0, "*" + Logic_Text.RemoveIllegalCharacters(element));
            }
		}

        /// <summary>
		/// Triggered when the client has responsed to a security request
		/// </summary>
        static public void Handle_CS_Security(CS_SecurityCheck pkt, Player player)
        {
            
            Player target = player.getVar("secReq") as Player;
            if (target == null)
                return;

            if (target == player)
                reliable = pkt.Unk3;
            player.setVar("secReq", null);
            
            
            //Check pkt.Unk3 against reliable source here
            if (pkt.Unk3 != reliable && reliable != 0)
            {//Message any mod that used this command
                target.sendMessage(0, "@**Mismatch for player: " + player._alias.ToString());
                target.sendMessage(0, "@**Expected: " + reliable + "       Received: " + pkt.Unk3);
                //player.disconnect();
                //Write and inform mods here
            }           
            else
                target.sendMessage(0, "Player " + player._alias.ToString() + " :: " + pkt.Unk3); //In-Memory Asset checksum
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
