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
                    CS_PrivateChat<Data.Database> pchat = new CS_PrivateChat<Data.Database>();
                    pchat.chat = pkt.recipient;
                    pchat.message = pkt.message;
                    pchat.from = player._alias;
                    player._server._db.send(pchat);
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
					string squad = player._squad;
					//Look up the squad that the player is currently in
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
