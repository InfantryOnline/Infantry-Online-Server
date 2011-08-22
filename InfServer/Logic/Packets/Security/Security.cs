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
			string acc = "";
			int idx = 0;
			target.sendMessage(0, "&Processes:");

			foreach (string element in pkt.processes)
			{
				acc += element;

				if ((idx++ % 5) == 4)
				{
					target.sendMessage(0, acc);
					acc = "";
				}
				else
					acc += ", ";
			}

			acc = "";
			idx = 0;
			target.sendMessage(0, "&Windows:");

			foreach (string element in pkt.windows)
			{
				acc += element;

				if ((idx++ % 3) == 2)
				{
					target.sendMessage(0, acc);
					acc = "";
				}
				else
					acc += ", ";
			}
		}

		/// <summary>
		/// Registers all handlers
		/// </summary>
		[Logic.RegistryFunc]
		static public void Register()
		{
			CS_Environment.Handlers += Handle_CS_Environment;
		}
	}
}
