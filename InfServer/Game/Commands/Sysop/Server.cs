using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

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
            //Power check
            if (player._developer && player.PermissionLevelLocal < Data.PlayerPermission.SMod)
            {
                player.sendMessage(-1, "Only mods or level 3 dev's and higher can use this command.");
                return;
            }

            int delay = 30;

            if (payload.Length > 0)
            {
                delay = Int32.Parse(payload);
            }

            player._arena.recycling = true;
            player._arena.setTicker(0, 0, delay * 100, "Server closing in: ", delegate()
            {
                player._server.recycle();
            });
            player._arena.sendArenaMessage(String.Format("!Server is restarting in {0} seconds. Please quit to assure stats are stored.", delay), 1);

            //For players leaving the zone, still will recycle it on its own
            if (!player._server._recycle.ContainsKey(player._server))
                player._server._recycle.Add(player._server, DateTime.Now.AddSeconds(delay));
            else
                player._server._recycle.Add(player._server, DateTime.Now.AddSeconds(delay));

            foreach (KeyValuePair<string, Arena> arena in player._server._arenas)
            {
                if (arena.Value == player._arena)
                    continue;

                arena.Value.recycling = true;
                arena.Value.sendArenaMessage(String.Format("!Server is restarting in {0} seconds. Please quit to assure stats are stored.", delay), 1);
                arena.Value.setTicker(0, 0, delay * 100, "Server closing in: ");
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
                player.sendMessage(0, "No security logs.");
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
            bool limit;
            if (String.IsNullOrEmpty(payload))
                limit = false;
            else
                limit = true;

			env.bLimitLength = limit;

			recipient.setVar("envReq", player);
			recipient._client.sendReliable(env);
		}

        /// <summary>
        /// Hack check prototype
        /// </summary>
        static public void assets(Player player, Player recipient, string payload, int bong)
        {
            if (recipient == null)
            {
                foreach (Player p in player._arena.Players)
                {
                    SC_SecurityCheck cs = new SC_SecurityCheck();
                    cs.key = 9815; //Key we are using
                    cs.unknown = 0; // Unknown, send as 0   
                    p.setVar("secReq", player); //Pass the person we need to PM the info
                    p._client.send(cs); //Send it
                }
            }
            else
            {
                SC_SecurityCheck cs = new SC_SecurityCheck();
                cs.key = 9815; //Key we are using
                cs.unknown = 0; // Unknown, send as 0
                recipient.setVar("secReq", player); //Pass the person we need to PM the info
                recipient._client.send(cs); //Send it
            }
        }

		/// <summary>
		/// Just handy for testing packet functionality
		/// </summary>
        static public void testPacket(Player player, Player recipient, string payload, int bong)
		{
            //recipient._client.destroy();
            /*
            Disconnect discon = new Disconnect();

            discon.connectionID = recipient._client._connectionID;
            discon.reason = Disconnect.DisconnectReason.DisconnectReasonOtherSideTerminated;

            recipient._client.send(discon);
            Console.WriteLine("Disconnect packet sent to {0}", recipient);
            */
            /*
            SC_TestPacket test = new SC_TestPacket();
            test.player = player;
            test.ball = player._arena._balls.SingleOrDefault(b => b._id == ((ushort)1));
            player._client.sendReliable(test);
             */

                SC_TestPacket test = new SC_TestPacket();
                //test.playerID = (short)player._id;
                test.ball = player._arena._balls.SingleOrDefault(b => b._id == (ushort)1);
                player.sendMessage(0, test.ball._id.ToString());
                player._client.sendReliable(test);
		}

        /// <summary>
        /// Displays a gif
        /// </summary>
        static public void showGif(Player player, Player recipient, string payload, int bong)
		{	//Download the gif!
		/*	
            WebClient client = new WebClient();
			Stream file = client.OpenRead(payload);
			BinaryReader br = new BinaryReader(file);
			SC_ShowGif gif = new SC_ShowGif();

			gif.gifData = br.ReadBytes(1024 * 1024);
			gif.displayTime = 50;
			gif.website = payload;

			recipient._client.sendReliable(gif, 1);
         */    
		}

        /// <summary>
        /// Returns a list of mod commands used in every zone
        /// </summary>
        static public void history(Player player, Player recipient, string payload, int bong)
        {
            int page;
            string name = "";
            string[] args = payload.Split(':');
            bool IsNumeric = Regex.IsMatch(args[0], @"^[0-9]+$");

            if (String.IsNullOrEmpty(payload))
                page = 0;
            else
            {
                //Are we just typing a page number?
                if (IsNumeric)
                {
                    try
                    {
                        page = Convert.ToInt32(payload);
                    }
                    catch
                    {
                        page = 0;
                    }
                }
                else
                {
                    //We are typing a name first
                    name = args[0].Trim();
                    page = 0;

                    if (payload.Contains(':'))
                        page = Convert.ToInt32(args[1]);
                }
            }

            CS_ChatQuery<Data.Database> pkt = new CS_ChatQuery<Data.Database>();
            pkt.sender = player._alias;
            pkt.queryType = CS_ChatQuery<Data.Database>.QueryType.history;
            if (!String.IsNullOrEmpty(name))
                pkt.payload = String.Join(":", payload, page.ToString());
            else
                pkt.payload = page.ToString();
            player._server._db.send(pkt);
        }

        /// <summary>
        /// Returns a list of admins currently powered
        /// </summary>
        static public void admins(Player player, Player recipient, string payload, int bong)
        {
            if (String.IsNullOrEmpty(payload) || payload.ToLower().Contains("list"))
            {
                //They just want to see a list of admins
                CS_ChatQuery<Data.Database> query = new CS_ChatQuery<Data.Database>();
                query.queryType = CS_ChatQuery<Data.Database>.QueryType.adminlist;
                query.sender = player._alias;
                query.payload = "list";
                player._server._db.send(query);
                return;
            }
        }

		/// <summary>
		/// Registers all handlers
		/// </summary>
		[Commands.RegistryFunc(HandlerType.ModCommand)]
		static public IEnumerable<Commands.HandlerDescriptor> Register()
		{
            yield return new HandlerDescriptor(admins, "admins",
                "Currently returns a list of powered admins",
                "*admins or *admins list",
                InfServer.Data.PlayerPermission.Sysop, false);

            yield return new HandlerDescriptor(assets, "assets",
               "secret",
               "?quit",
               InfServer.Data.PlayerPermission.Sysop, false);

            yield return new HandlerDescriptor(environment, "environment",
                "Queries environment information from a player",
                "::*environment",
                InfServer.Data.PlayerPermission.Sysop, false);

            yield return new HandlerDescriptor(history, "history",
                "Returns a list of mod commands used in every server",
                "*history [page], *history [name], or *history [name]:[page]",
                InfServer.Data.PlayerPermission.Sysop, false);

            yield return new HandlerDescriptor(log, "log",
                "Grabs exception logs for the current zone",
                "*log",
                InfServer.Data.PlayerPermission.Sysop, true);

            yield return new HandlerDescriptor(recycle, "recycle",
                "Restarts the current zone",
                "*recycle",
                InfServer.Data.PlayerPermission.Mod, true);

            yield return new HandlerDescriptor(showGif, "showgif",
                "Sends a gif to the target player",
                "::*showgif [gif url]",
                InfServer.Data.PlayerPermission.Sysop, false);

            yield return new HandlerDescriptor(testPacket, "testpacket",
				"Sends a test packet to the target player",
				"::*testpacket",
				InfServer.Data.PlayerPermission.Sysop, false);

		}
	}
}