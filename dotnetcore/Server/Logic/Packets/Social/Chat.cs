using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Protocol;
using InfServer.Game;
using Assets;

namespace InfServer.Logic
{	// Logic_Chat Class
	/// Deals all chat mechanisms
	///////////////////////////////////////////////////////
	class Logic_Chat
	{	/// <summary>
		/// Handles chat packets sent from the client
		/// </summary>
		static public void Handle_CS_Chat(CS_Chat pkt, Player player)
		{
            if (player == null)
            {
                Log.write(TLog.Error, "Handle_CS_Chat(): Called with null player.");
                return;
            }

            var server = player._server;

            if (server._config["server/logchat"].boolValue)
            {
                if (player._arena != null)
                {
                    using (LogAssume.Assume(player._arena._logger))
                    {
                        Log.write(TLog.Normal, $"{player._alias} ({player._client._ipe})> {pkt.message}");
                    }
                }
            }                  

            if (pkt.message == "?diag")
            {
                // Raw diagnostic output.
                var actualArena = player._server._arenas.FirstOrDefault(a => a.Value.Players.FirstOrDefault(p => p._alias == player._alias) != null);

                var arenaName = actualArena.Value != null ? actualArena.Value._name : "Not found!";
                var inArena = player._arena != null ? player._arena._name : "No Arena!";
                var ingame = player._bIngame ? "Yes" : "No!";
                var spectator = player.IsSpectator ? "Yes" : "No";

                SC_Chat rawResponse = new SC_Chat();
                rawResponse.chatType = Helpers.Chat_Type.Arena;
                rawResponse.message = $"Player Alias: {player._alias}. Assumed Arena: {inArena}. Found Arena: {arenaName}";
                rawResponse.from = "";

                player._client.send(rawResponse);

                rawResponse = new SC_Chat();
                rawResponse.chatType = Helpers.Chat_Type.Arena;
                rawResponse.message = $"Last Packet Received: {player._client._lastPacketRecv}. Last Packet Sent: {player._client._lastPacketSent}. Current Time: {Environment.TickCount}. Time Diff: {player._client._timeDiff}";
                rawResponse.from = "";

                player._client.send(rawResponse);

                rawResponse = new SC_Chat();
                rawResponse.chatType = Helpers.Chat_Type.Arena;
                rawResponse.message = $"Player Id: {player._id}. In game? {ingame}. Spectating? {spectator}";
                rawResponse.from = "";

                player._client.send(rawResponse);

                foreach (var stream in player._client._streams)
                {
                    rawResponse = new SC_Chat();
                    rawResponse.chatType = Helpers.Chat_Type.Arena;
                    rawResponse.message = $"Stream ID: {stream.streamID}. C2S: {stream.C2S_Reliable}. S2C: {stream.S2C_Reliable} / Confirmed: {stream.S2C_ReliableConfirmed}. S2C Queue: {stream.reliableQueue.Count}. C2S Queue: {stream.reliablePackets.Count}.";
                    rawResponse.from = "";

                    player._client.send(rawResponse);
                }

                return;
            }

            if (player._arena == null)
            {
                Log.write(TLog.Error, "Handle_CS_Chat(): Called with null arena.");
                return;
            }

            //Ignore blank messages
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
				if (pkt.chatType == Protocol.Helpers.Chat_Type.Whisper)
				{
					if ((recipient = player._server.getPlayer(pkt.recipient)) == null)
						return;
				}

				//Route it to our arena!
				player._arena.handleEvent(delegate(Arena arena)
					{
                        if (arena == null)
                        {
                            Log.write(TLog.Error, "Handle_CS_Chat(): Player {0} sent server chat command with no delegating arena.", player);
                            return;
                        }

						arena.playerChatCommand(player, recipient, command, payload, pkt.bong);
					}
				);
						
				return;
            } //Is it a Communication Command?
            else if (pkt.message[0] == '%' && pkt.message.Length > 1)
            {   //Obtain the command and payload
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
                if (pkt.chatType == Protocol.Helpers.Chat_Type.Whisper)
                {
                    if ((recipient = player._server.getPlayer(pkt.recipient)) == null)
                        return;
                }

                //Route it to our arena!
                player._arena.handleEvent(delegate(Arena arena)
                    {
                        if (arena == null)
                        {
                            Log.write(TLog.Error, "Handle_CS_Chat(): Player {0} sent communication chat command with no delegating arena.", player);
                            return;
                        }

                        arena.playerCommCommand(player, recipient, command, payload, pkt.bong);
                    }
                );

                return;
            } //Is it a Mod Command?
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
                if (pkt.chatType == Protocol.Helpers.Chat_Type.Whisper)
                {
                    if ((recipient = player._server.getPlayer(pkt.recipient)) == null)
                        return;
                }

                //Route it to our arena!
                player._arena.handleEvent(delegate(Arena arena)
                    {
                        if (arena == null)
                        {
                            Log.write(TLog.Error, "Handle_CS_Chat(): Player {0} sent mod chat command with no delegating arena.", player);
                            return;
                        }

                        player._arena.playerModCommand(player, recipient, command, payload, pkt.bong);
                    }
                );

                return;
            }
            else //Must be a regular chat, lets see if they are allowed first
            {
                //Ignore messages from the silent
                if (player._bSilenced)
                {
                    player.sendMessage(-1, "You can't speak.");
                    return;
                }

                //Lets do some spam checking..
                bool change = false;
                player._msgTimeStamps.Add(DateTime.Now);
                foreach (DateTime msg in player._msgTimeStamps)
                {
                    TimeSpan diff = DateTime.Now - msg;
                    if (diff.Seconds > 5)
                        change = true;
                }

                //Remove messages that are older than 5 seconds.
                //Clear player spam list, restart over
                if (player._msgTimeStamps != null && change)
                    player._msgTimeStamps = new List<DateTime>();

                //More than 5 messages in 5 seconds?
                if (player._msgTimeStamps.Count == 5)
                    //Warn him
                    player.sendMessage(-1, "WARNING! You will be auto-silenced for spamming.");

                //More than 10 messages in 5 seconds?
                if (player._msgTimeStamps.Count >= 10)
                {//Autosilence
                    int duration = 5; //5 mins
                    player.sendMessage(-1, String.Format("You are being auto-silenced for {0} minutes for spamming.", duration));
                    player._bSilenced = true;
                    player._lengthOfSilence = duration;
                    player._timeOfSilence = DateTime.Now;

                    var arenaEntry = player._arena._silencedPlayers.FirstOrDefault(sp => sp.IPAddress.Equals(player._ipAddress) || sp.Alias == player._alias);

                    if (arenaEntry == null)
                    {
                        arenaEntry = new SilencedPlayer { IPAddress = player._ipAddress, DurationMinutes = duration, SilencedAt = DateTime.Now };
                        player._arena._silencedPlayers.Add(arenaEntry);
                    }

                    return;
                }

                //For league matches
                bool Allowed = true;
                if (player._arena._isMatch && player.PermissionLevelLocal < Data.PlayerPermission.ArenaMod
                    && player.IsSpectator)
                    Allowed = false;

                if (player.PermissionLevelLocal < Data.PlayerPermission.ManagerSysop)
                {
                    bool detected;
                    pkt.message = SwearFilter.Filter(pkt.message, out detected);
                }

                //What sort of chat has occured?
                switch (pkt.chatType)
                {
                    case Protocol.Helpers.Chat_Type.Normal:
                        //For leagues, dont allow them to talk to the teams
                        if (!Allowed)
                        {
                            pkt.chatType = Protocol.Helpers.Chat_Type.Team;
                            Handle_CS_Chat(pkt, player);
                            break;
                        }

                        if ((player._arena._specQuiet || player._specQuiet) && player.PermissionLevelLocal < Data.PlayerPermission.ArenaMod && player.IsSpectator)
                        {
                            pkt.chatType = Protocol.Helpers.Chat_Type.Team;
                            Handle_CS_Chat(pkt, player);
                            break;
                        }

                        //Send it to our arena!
                        player._arena.handleEvent(delegate(Arena arena)
                            {
                                if (arena == null)
                                {
                                    Log.write(TLog.Error, "Handle_CS_Chat(): Player {0} sent chat packet with no delegating arena.", player);
                                    return;
                                }

                                pkt.bong = 0;                               
                                player._arena.playerArenaChat(player, pkt);
                            }
                        );
                        break;

                    case Protocol.Helpers.Chat_Type.Macro:
                        if (!Allowed)
                        {
                            //Arent allowed
                            pkt.chatType = Protocol.Helpers.Chat_Type.Team;
                            Handle_CS_Chat(pkt, player);
                            break;
                        }

                        if ((player._arena._specQuiet || player._specQuiet) && player.PermissionLevelLocal < Data.PlayerPermission.ArenaMod && player.IsSpectator)
                        {
                            pkt.chatType = Protocol.Helpers.Chat_Type.Team;
                            Handle_CS_Chat(pkt, player);
                            break;
                        }

                        pkt.chatType = Protocol.Helpers.Chat_Type.Normal;
                        Handle_CS_Chat(pkt, player);
                        break;

                    case Protocol.Helpers.Chat_Type.Team:
                        //Send it to the player's team
                        player._team.playerTeamChat(player, pkt);

                        break;

                    case Protocol.Helpers.Chat_Type.EnemyTeam:
                        //Send it to the players team and enemy's team
                        player._team.playerTeamChat(player, pkt);

                        if (!Allowed)
                            break;
                        if ((player._arena._specQuiet || player._specQuiet) && player.PermissionLevelLocal < Data.PlayerPermission.ArenaMod && player.IsSpectator)
                            break;

                        if (!pkt.recipient.Equals(player._alias, StringComparison.OrdinalIgnoreCase))
                        {
                            Player recipient = player._arena.getPlayerByName(pkt.recipient);
                            if (recipient != null)
                            {
                                pkt.message = String.Format("[Enemy] {0}", pkt.message);
                                recipient._team.playerTeamChat(player, pkt);
                            }
                        }
                        break;

                    case Protocol.Helpers.Chat_Type.PrivateChat:
                        if (!player._server.IsStandalone)
                        {
                            CS_PrivateChat<Data.Database> pchat = new CS_PrivateChat<Data.Database>();
                            pchat.chat = pkt.recipient;
                            pchat.message = pkt.message;
                            pchat.from = player._alias;
                            player._server._db.send(pchat);
                        }
                        break;

                    case Protocol.Helpers.Chat_Type.Whisper:
                        {	                           
                            //Find our recipient
                            Player recipient = player._server.getPlayer(pkt.recipient);

                            //For league and spec quiet toggles
                            if ((recipient != null) && !recipient.IsSpectator)
                            {
                                if (!Allowed)
                                    break;
                                if (player._arena._specQuiet || player._specQuiet)
                                {
                                    if (player.PermissionLevelLocal < Data.PlayerPermission.ArenaMod && player.IsSpectator)
                                        break;
                                }
                            }

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

                    case Protocol.Helpers.Chat_Type.Squad:
                        //Since squads are only zone-wide, we don't need to route it to the database,
                        //instead we route it to every player in every arena in the zone
                        foreach (Arena a in player._server._arenas.Values)
                            foreach (Player p in a.Players)
                            {
                                if (p == player)
                                    continue;
                                if (String.IsNullOrWhiteSpace(pkt.recipient))
                                    continue;
                                if (!p._squad.Equals(pkt.recipient, StringComparison.OrdinalIgnoreCase))
                                    continue;
                                if (!player._squad.Equals(p._squad, StringComparison.OrdinalIgnoreCase))
                                    continue;
                                p.sendPlayerChat(player, pkt);
                            }
                        break;
                }
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
