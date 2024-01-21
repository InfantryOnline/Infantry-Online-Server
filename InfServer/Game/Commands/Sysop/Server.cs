﻿using System;
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

            if (!String.IsNullOrWhiteSpace(payload))
            {
                delay = Int32.Parse(payload);
            }

            foreach (KeyValuePair<string, Arena> arena in player._server._arenas)
            {
                arena.Value.recycling = true;
                arena.Value.sendArenaMessage(String.Format("!Server is restarting in {0} seconds. Please quit to assure stats are stored.", delay), 1);
                arena.Value.setTicker(0, 0, delay * 100, "Server closing in: ");
            }

            player._server._recycling = true;
            player._server._recycleAttempt = (delay * 1000) + Environment.TickCount;
        }

        /// <summary>
        /// Recompiles any active scripts
        /// </summary>
        static public void reloadscripts(Player player, Player recipient, string payload, int bong)
        {
            //Power check
            if (player._developer && player.PermissionLevelLocal < Data.PlayerPermission.SMod)
            {
                player.sendMessage(-1, "Only mods or level 3 dev's and higher can use this command.");
                return;
            }

            //Tell him we're doing our best!
            player.sendMessage(0, "Attempting to reload scripts...");

            //Lets do some thangs
            if (!player._server.reloadScripts())
                player.sendMessage(-1, "Error reloading scripts, please check logs for more info");
            //ruh roh
            else
                player.sendMessage(0, "Scripts have been reloaded successfully");
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
        {   //Send him an environment packet!
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
                    cs.key = 1125; //Key we are using
                    cs.unknown = 0; // Unknown, send as 0   
                    p.setVar("secReq", player); //Pass the person we need to PM the info
                    p._client.send(cs); //Send it
                }
            }
            else
            {
                SC_SecurityCheck cs = new SC_SecurityCheck();
                cs.key = 1125; //Key we are using
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
            

            SC_TestPacket test = new SC_TestPacket();
            test.player = player;
            test.ball = player._arena._balls.SingleOrDefault(b => b._id == (ushort)0);
            player._client.sendReliable(test);
             */

            SC_TestPacket test = new SC_TestPacket();
            test.player = player;
            player._client.send(test);


            /*
            SC_RegQuery reg = new SC_RegQuery();
            reg.unk = 2;
            reg.location = "HKEY_CURRENT_USER\\SOFTWARE\\7-Zip\\Path";
            player._client.sendReliable(reg);
             */
        }

        /// <summary>
        /// Displays a gif
        /// </summary>
        static public void showGif(Player player, Player recipient, string payload, int bong)
        {   //Download the gif!
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
            int page = 0;
            string name = string.Empty;
            string[] args = payload.Split(':');
            bool pageIsFirst = Regex.IsMatch(args[0], @"^[0-9]+$");

            if (!String.IsNullOrWhiteSpace(payload))
            {
                if (pageIsFirst)
                {
                    try
                    {
                        page = Convert.ToInt32(args[0]);
                    }
                    catch
                    {
                        page = 0; //Convert doesn't do negative numbers
                    }
                }
                else
                {
                    //We are typing a name first
                    name = args[0].Trim();

                    if (payload.Contains(':'))
                    {
                        try
                        {
                            page = Convert.ToInt32(args[1]);
                        }
                        catch
                        {
                            page = 0; //Convert doesn't do negative numbers
                        }
                    }
                }
            }

            // convert 1-indexed human entries to 0-indexed
            if (page > 0)
            {
                page--;
            }

            CS_ChatQuery<Data.Database> pkt = new CS_ChatQuery<Data.Database>();
            pkt.sender = player._alias;
            pkt.queryType = CS_ChatQuery<Data.Database>.QueryType.history;
            pkt.payload = String.Join(":", name, page.ToString());
            player._server._db.send(pkt);
        }

        /// <summary>
        /// Calls a shutdown of the current zone server
        /// </summary>
        static public void shutdown(Player player, Player recipient, string payload, int bong)
        {
            if (!player._admin)
            {
                player.sendMessage(-1, "Only admins can use this command.");
                return;
            }

            player.sendMessage(0, "Shutting down...");
            Log.write("Shutdown called by {0}.", player._alias);

            //Shut it down
            player._server.shutdown();
            return;
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
                InfServer.Data.PlayerPermission.HeadModAdmin, false);

            yield return new HandlerDescriptor(assets, "assets",
               "secret",
               "?quit",
               InfServer.Data.PlayerPermission.HeadModAdmin, false);

            yield return new HandlerDescriptor(environment, "environment",
                "Queries environment information from a player",
                "::*environment",
                InfServer.Data.PlayerPermission.HeadModAdmin, false);

            yield return new HandlerDescriptor(history, "history",
                "Returns a list of mod commands used in every server",
                "*history [page], *history [name], or *history [name]:[page]",
                InfServer.Data.PlayerPermission.Mod, false);

            yield return new HandlerDescriptor(log, "log",
                "Grabs exception logs for the current zone",
                "*log",
                InfServer.Data.PlayerPermission.Sysop, true);

            yield return new HandlerDescriptor(recycle, "recycle",
                "Restarts the current zone",
                "*recycle",
                InfServer.Data.PlayerPermission.Mod, true);

            yield return new HandlerDescriptor(reloadscripts, "reloadscripts",
                "Reloads the specified scripts for all arenas in the zoneserver",
                "*reloadscripts",
                InfServer.Data.PlayerPermission.Mod, true);

            yield return new HandlerDescriptor(showGif, "showgif",
                "Sends a gif to the target player",
                "::*showgif [gif url]",
                InfServer.Data.PlayerPermission.ManagerSysop, false);

            yield return new HandlerDescriptor(shutdown, "shutdown",
                "Shut downs the current zone server",
                "*shutdown",
                InfServer.Data.PlayerPermission.ManagerSysop, false);

            yield return new HandlerDescriptor(testPacket, "testpacket",
                "Sends a test packet to the target player or just sends a packet",
                "::*testpacket, *testpacket",
                InfServer.Data.PlayerPermission.ManagerSysop, false);

        }
    }
}