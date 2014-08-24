using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Protocol;
using InfServer.Game;

namespace InfServer.Logic
{	// Logic_Security Class
	/// Deals with elements of the server's security
    /// 
	///////////////////////////////////////////////////////
    
	class Logic_Security
	{
        public static uint reliable;
        public static uint reliableChecksum;

        /// <summary>
		/// Triggered when the client is attempting to enter the game and sends his security reply
		/// </summary>
		static public void Handle_CS_Environment(CS_Environment pkt, Player player)
		{	//Does he have a target ready to receive the information?
			Player target = player.getVar("envReq") as Player;
            List<string> badPrograms = new List<string>();
            badPrograms.Add("cheat engine 6.2");
            badPrograms.Add("ollydbg");
            badPrograms.Add("cheat engine");
            badPrograms.Add("cheatengine");
            badPrograms.Add("ollydbg.exe");
            badPrograms.Add("wireshark");
            badPrograms.Add("wireshark.exe");
            badPrograms.Add("speederxp");
            if (target == null)
            {//It was a request by the server
                //Check the processes
                foreach (string element in pkt.processes)
                    if (badPrograms.Contains(Logic_Text.RemoveIllegalCharacters(element).ToLower()) && player._permissionStatic != Data.PlayerPermission.Sysop)
                    {//They have a cheat running or debugger, kick them out and inform mods
                        if (!player._server.IsStandalone)
                        {
                            CS_ChatQuery<Data.Database> pktquery = new CS_ChatQuery<Data.Database>();
                            pktquery.queryType = CS_ChatQuery<Data.Database>.QueryType.alert;
                            pktquery.sender = player._alias;
                            pktquery.payload = String.Format("&ALERT - Player Kicked: (Zone={0}, Arena={1}, Player={2}) Reason=Using a hack or cheat engine. Program={3}", 
                                player._server.Name, player._arena._name, player._alias, Logic_Text.RemoveIllegalCharacters(element).ToLower());
                            //Send it!
                            player._server._db.send(pktquery);
                        }
                        else
                        {
                            foreach (Player ppl in player._arena.Players.ToList())
                                if (ppl.PermissionLevelLocal >= Data.PlayerPermission.ArenaMod)
                                    ppl.sendMessage(-1, String.Format("&ALERT - Player Kicked: (Zone={0}, Arena={1}, Player={2}) Reason=Using a hack or cheat engine. Program={3}", 
                                        player._server.Name, player._arena._name, player._alias, Logic_Text.RemoveIllegalCharacters(element).ToLower()));
                        }
                        Log.write(TLog.Security, String.Format("Player Kicked: (Zone={0}, Arena={1}, Player={2}) Reason=Using a hack or cheat engine. Program={3}", 
                                player._server.Name, player._arena._name, player._alias, Logic_Text.RemoveIllegalCharacters(element).ToLower()));
                        player.disconnect();
                        return;
                    }

                //Check the windows
                 foreach (string element in pkt.windows)
                     if (badPrograms.Contains(Logic_Text.RemoveIllegalCharacters(element).ToLower()) && player._permissionStatic != Data.PlayerPermission.Sysop)
                    {//They have a cheat running or debugger, kick them out and inform mods

                        if (!player._server.IsStandalone)
                        {
                            CS_ChatQuery<Data.Database> pktquery = new CS_ChatQuery<Data.Database>();
                            pktquery.queryType = CS_ChatQuery<Data.Database>.QueryType.alert;
                            pktquery.sender = player._alias;
                            pktquery.payload = String.Format("&ALERT - Player Kicked: (Zone={0}, Arena={1}, Player={2}) Reason=Using a hack or cheat engine. Program={3}", player._server.Name, player._arena._name, player._alias, Logic_Text.RemoveIllegalCharacters(element).ToLower());
                            //Send it!
                            player._server._db.send(pktquery);
                        }
                        else
                        {
                            foreach (Player ppl in player._arena.Players.ToList())
                                if (ppl.PermissionLevelLocal >= Data.PlayerPermission.ArenaMod)
                                    ppl.sendMessage(-1, String.Format("&ALERT - Player Kicked: (Zone={0}, Arena={1}, Player={2}) Reason=Using a hack or cheat engine.", player._server.Name, player._arena._name, player._alias));
                        }

                        player.disconnect();  
                    }

                return;
            }
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
            //Don't do in private arenas until i figure out if it can fuck things up
            if (player != null && player._arena.IsPrivate)
                return;

            Player reliablePlayer = player.getVar("reliable") as Player;
            Player target = player.getVar("secReq") as Player;
        //    if (player._alias == "kon")
          //      return;
            if (target == null)
            {//Server is checking assets
                if (reliablePlayer != null && reliablePlayer == player)
                {//Lets use the mods asset checksum
                    player._server._reliableChecksum = pkt.Unk3;
                    Log.write(TLog.Security, "Reliable client checksum " + pkt.Unk3 + " set by " + player._alias);
                    player.setVar("reliable", null);
                    return;
                }
                //Not a mod
                if (pkt.Unk3 != player._server._reliableChecksum && player._server._reliableChecksum != 0 && player._permissionStatic != Data.PlayerPermission.Sysop)
                {//Mismatch
                    Log.write(TLog.Security, "Checksum mismatch: " + pkt.Unk3 + " vs " + player._server._reliableChecksum);

                    if (!player._server.IsStandalone)
                    {
                        CS_ChatQuery<Data.Database> pktquery = new CS_ChatQuery<Data.Database>();
                        pktquery.queryType = CS_ChatQuery<Data.Database>.QueryType.alert;
                        pktquery.sender = player._alias;
                        pktquery.payload = String.Format("&ALERT - Player Kicked: (Zone={0}, Arena={1}, Player={2}, Checksum={3}, Reliable={4}) Reason=Client checksum mismatch.", player._server.Name, player._arena._name, player._alias, pkt.Unk3, player._server._reliableChecksum);
                        //Send it!
                        player._server._db.send(pktquery);
                        player.disconnect();
                    }
                    else
                    {
                        foreach (Player ppl in player._arena.Players.ToList())
                            if (ppl.PermissionLevelLocal >= Data.PlayerPermission.ArenaMod)
                                ppl.sendMessage(-1, String.Format("&ALERT - Player Kicked: (Zone={0}, Arena={1}, Player={2}, Checksum={3}, Reliable={4}) Reason=Client checksum mismatch.", player._server.Name, player._arena._name, player._alias, pkt.Unk3, player._server._reliableChecksum));
                        player.disconnect();
                    }
                }
                return;
            }
            if (target == player)
            {
                reliable = pkt.Unk3;
                player._server._reliableChecksum = pkt.Unk3;
            }
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
