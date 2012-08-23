using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Protocol;
using InfServer.Game;

namespace InfServer.Logic
{	// Logic_Chat Class
	/// Deals all chat mechanisms
	///////////////////////////////////////////////////////
	class Logic_Chat
	{	/// <summary>
		/// Handles chat packets sent from the client
		/// </summary>
		static public void Handle_CS_Chat(CS_Chat pkt, Player player)
		{	//Ignore blank messages
            if (pkt.message == "")
				return;

            //Ignore messages from the silent
            if (player._bSilenced)
                return;

            //Not working correctly for whatever reason, will come back to this later...
            /*Lets do some spam checking..
            player._msgTimeStamps.Add(DateTime.Now);

            List<DateTime> old = new List<DateTime>();
            foreach (DateTime msg in player._msgTimeStamps)
            {
                TimeSpan diff = msg.Date - DateTime.Now;
                if (diff.Seconds > 5)
                    old.Add(msg); 
            }

            //Remove messages that are older than 5 seconds.
            foreach (DateTime msg in old)
                player._msgTimeStamps.Remove(msg);


            //More than 4 messages in 5 seconds?
            if (player._msgTimeStamps.Count >= 5)
            {//Warn him
                player.sendMessage(-1, "WARNING! You will be auto-silenced for spamming.");
            }

            //More than 10 messages in 5 seconds?
            if (player._msgTimeStamps.Count >= 10)
            {//Autosilence
                player.sendMessage(-1, "You are being auto-silenced for spamming");
                player._bSilenced = true;
                player._lengthOfSilence = 1;
                player._timeOfSilence = DateTime.Now;
            }*/

			//Is it a server command?
			if (pkt.message[0] == '?' && pkt.message.Length > 1)
			{	//Obtain the command and payload
				int spcIdx = pkt.message.IndexOf(' ');
				string command;
				string payload = "";

				if (spcIdx == -1)
					command = pkt.message.Substring(1);
				else
				{
					command = pkt.message.Substring(1, spcIdx - 1);
					payload = pkt.message.Substring(spcIdx + 1);
				}

				//Do we have a recipient?
				Player recipient = null;
				if (pkt.chatType == Helpers.Chat_Type.Whisper)
				{
					if ((recipient = player._server.getPlayer(pkt.recipient)) == null)
						return;
				}

				//Route it to our arena!
				player._arena.handleEvent(delegate(Arena arena)
					{
						arena.playerChatCommand(player, recipient, command, payload, pkt.bong);
					}
				);
						
				return;
			}
			else if (pkt.message[0] == '*' && pkt.message.Length > 1)
			{	//Obtain the command and payload
				int spcIdx = pkt.message.IndexOf(' ');
				string command;
				string payload = "";

				if (spcIdx == -1)
					command = pkt.message.Substring(1);
				else
				{
					command = pkt.message.Substring(1, spcIdx - 1);
					payload = pkt.message.Substring(spcIdx + 1);
				}

				//Do we have a recipient?
				Player recipient = null;
				if (pkt.chatType == Helpers.Chat_Type.Whisper)
				{
					if ((recipient = player._server.getPlayer(pkt.recipient)) == null)
						return;
				}

				//Route it to our arena!
				player._arena.handleEvent(delegate(Arena arena)
					{
						player._arena.playerModCommand(player, recipient, command, payload, pkt.bong);
					}
				);

				return;
			}

			//What sort of chat has occured?
			switch (pkt.chatType)
			{
				case Helpers.Chat_Type.Normal:
					//Send it to our arena!
					player._arena.handleEvent(delegate(Arena arena)
						{
                            pkt.bong = 0;
							player._arena.playerArenaChat(player, pkt);
						}
					);
					break;

                case Helpers.Chat_Type.Macro:
                    pkt.chatType = Helpers.Chat_Type.Normal;
                    Handle_CS_Chat(pkt, player);
                    break;

				case Helpers.Chat_Type.Team:
					//Send it to the player's team
					player._team.playerTeamChat(player, pkt);
					break;

                case Helpers.Chat_Type.PrivateChat:
                    if (!player._server.IsStandalone)
                    {
                        CS_PrivateChat<Data.Database> pchat = new CS_PrivateChat<Data.Database>();
                        pchat.chat = pkt.recipient;
                        pchat.message = pkt.message;
                        pchat.from = player._alias;
                        player._server._db.send(pchat);
                    }
                    break;

                case Helpers.Chat_Type.Whisper:
                    {	//Find our recipient
                        Player recipient = player._server.getPlayer(pkt.recipient);

                        //Are we connected to a database?
                        if (!player._server.IsStandalone)
                        {   //Yeah, lets route it through the DB so we can pm globally!
                            CS_Whisper<Data.Database> whisper = new CS_Whisper<Data.Database>();
                            whisper.bong = pkt.bong;
                            whisper.recipient = pkt.recipient;
                            whisper.message = pkt.message;
                            whisper.from = player._alias;
                            player._server._db.send(whisper);
                        }
                        else
                        {
                            //Send it to the target player
                            if (recipient != null)
                                recipient.sendPlayerChat(player, pkt);
                        }

                    }
                    break;

				case Helpers.Chat_Type.Squad:
                    //Since squads are only zone-wide, we don't need to route it to the database,
                    //instead we route it to every player in every arena in the zone
                    foreach(Arena a in player._server._arenas.Values)
                        foreach (Player p in a.Players)
                        {
                            if (p == player || p._squad != pkt.recipient || pkt.recipient == "")
                                //We don't message ourselves or anybody outside of specified squad
                                continue;
                            p.sendPlayerChat(player, pkt);
                        }
					break;
			}
		}


		/// <summary>
		/// Registers all handlers
		/// </summary>
		[Logic.RegistryFunc]
		static public void Register()
		{
			CS_Chat.Handlers += Handle_CS_Chat;
		}
	}
}
