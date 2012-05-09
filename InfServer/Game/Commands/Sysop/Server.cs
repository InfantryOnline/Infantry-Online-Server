using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Web;
using System.IO;
using System.Timers;

using Assets;

using InfServer.Bots;
using InfServer.Protocol;

namespace InfServer.Game.Commands.Mod
{
	/// <summary>
	/// Provides a series of functions for handling mod commands
	/// </summary>
	public class Server
	{
		/// <summary>
		/// Restarts the server
		/// </summary>
        static public void recycle(Player player, Player recipient, string payload, int bong)
        {
            int delay = 30;

            if (payload.Length > 0)
            {
                delay = Int32.Parse(payload);
            }

            player._arena.setTicker(0, 0, delay * 100, "Server closing in: ", player._server.recycle);
            player._arena.sendArenaMessage(String.Format("!Server is restarting in {0} seconds. Please quit to assure stats are stored.", delay), 1);


            foreach (KeyValuePair<string, Arena> arena in player._server._arenas)
            {
                if (arena.Value == player._arena)
                    continue;

                arena.Value.sendArenaMessage(String.Format("!Server is restarting in {0} seconds. Please quit to assure stats are stored.", delay), 1);
                arena.Value.setTicker(0, 0, delay * 100, "Server closing in:");
            }

            
        }

        /// <summary>
        /// Grabs logs
        /// </summary>
        static public void log(Player player, Player recipient, string payload, int bong)
        {
            List<string> logs = Log.readLog();

            if (logs == null)
                return;

            //Do we even have any?
            if (logs.Count() == 0)
            {
                player.sendMessage(0, "No exception logs.");
                return;
            }

            //Send em!
            foreach (string log in logs)
            {
                player.sendMessage(0, String.Format("!{0}", log));
            }
        }


        


		/// <summary>
		/// Queries environment information from a player
		/// </summary>
        static public void environment(Player player, Player recipient, string payload, int bong)
		{	//Send him an environment packet!
			SC_Environment env = new SC_Environment();

			env.bLimitLength = false;

			recipient.setVar("envReq", player);
			recipient._client.sendReliable(env);
		}

		/// <summary>
		/// Just handy for testing packet functionality
		/// </summary>
        static public void testPacket(Player player, Player recipient, string payload, int bong)
		{
			SC_Test test = new SC_Test();
            test.bCrown = true;
            test.players = new List<short>();

            foreach (Player p in player._arena.Players)
            {
                test.players.Add((short)p._id);
            }

            foreach (Player p in player._arena.Players)
                p._client.sendReliable(test);
		}

		/// <summary>
		/// Displays a gif
		/// </summary>
        static public void showGif(Player player, Player recipient, string payload, int bong)
		{	//Download the gif!
			WebClient client = new WebClient();
			Stream file = client.OpenRead(payload);
			BinaryReader br = new BinaryReader(file);
			SC_ShowGif gif = new SC_ShowGif();

			gif.gifData = br.ReadBytes(1024 * 1024);
			gif.displayTime = 50;
			gif.website = payload;

			recipient._client.sendReliable(gif, 1);
		}

		/// <summary>
		/// Registers all handlers
		/// </summary>
		[Commands.RegistryFunc(HandlerType.ModCommand)]
		static public IEnumerable<Commands.HandlerDescriptor> Register()
		{
			yield return new HandlerDescriptor(recycle, "recycle",
				"Restarts the current zone",
				"*recycle",
				InfServer.Data.PlayerPermission.Sysop);

            yield return new HandlerDescriptor(log, "log",
                "Grabs exception logs for the current zone",
                "*log",
                InfServer.Data.PlayerPermission.Sysop);

			yield return new HandlerDescriptor(environment, "environment",
				"Queries environment information from a player",
				"::*environment",
				InfServer.Data.PlayerPermission.Sysop);

			yield return new HandlerDescriptor(testPacket, "testpacket",
				"Sends a test packet to the target player",
				"::*testpacket",
				InfServer.Data.PlayerPermission.Sysop);

			yield return new HandlerDescriptor(showGif, "showgif",
				"Sends a gif to the target player",
				"::*showgif [gif url]",
				InfServer.Data.PlayerPermission.Sysop);
		}
	}
}