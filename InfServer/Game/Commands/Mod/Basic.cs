using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Data.SqlClient;

using Assets;
using InfServer.Game;
using InfServer.Bots;
using InfServer.Protocol;
using InfServer.Logic;

namespace InfServer.Game.Commands.Mod
{
    /// <summary>
    /// Provides a series of functions for handling mod commands
    /// </summary>
    public class Basic
    {
        static public void addball(Player player, Player recipient, string payload, int bong)
        {
            //Get the ball's default spawn
            int wID = player._server._zoneConfig.warpGroup.ballEntry;
            LioInfo.WarpField spawnLoc = player._server._assets.Lios.getWarpGroupByID(wID).FirstOrDefault();

            //Does it exist?
            if (spawnLoc == null)
            {
                Log.write(TLog.Warning, "Attempt to spawn a ball with no associated warpgroup");
                return;
            }

            //Grab a new ballID
            int ballID = player._arena._balls.Count + 1;

            //Lets create a new ball!
            Ball newBall = new Ball((short)ballID, player._arena);

            //Initialize its ballstate
            newBall._state = new Ball.BallState();

            //Assign default state
            newBall._state.positionX = spawnLoc.GeneralData.OffsetX;
            newBall._state.positionY = spawnLoc.GeneralData.OffsetY;
            newBall._state.positionZ = 0;

            //Store it.
            player._arena._balls.Add(newBall);


            //Make each player aware of the ball
            newBall.Route_Ball(player._arena.Players);
            player._arena.sendArenaMessage("Ball Added");
        }

        /// <summary>
        /// Sends a arena/system message
        /// </summary>
        static public void arena(Player player, Player recipient, string payload, int bong)
        {
            if (payload == "")
                player.sendMessage(-1, "Message can not be empty");
            else
                player._arena.sendArenaMessage(payload, bong);
        }

        /// <summary>
        /// Log the player in. Stand-Alone Mode only.
        /// </summary>
        static public void authenticate(Player player, Player recipient, string payload, int bong)
        {
            if (player._server.IsStandalone && player._server._config["server/managerPassword"].Value.Length > 0)
            {
                if (payload.Trim() == player._server._config["server/managerPassword"].Value)
                {
                    player._permissionTemp = Data.PlayerPermission.ArenaMod;
                    player.sendMessage(1, "You have been granted");
                }
            }
        }

        /// <summary>
        /// Prizes target player cash.
        /// </summary>
        static public void cash(Player player, Player recipient, string payload, int bong)
        {	//Find our target
            Player target = (recipient == null) ? player : recipient;

            //Convert our string to an int and check for illegal characters.
            int cashVal;
            if (!Int32.TryParse(payload, out cashVal))
            {   //Uh oh
                player.sendMessage(-1, "Payload can only contain numbers (0-9)");
            }
            else
            {	//Give our target player some cash!
                target.Cash += cashVal;

                //Alert the player
                player.sendMessage(-3, "Target has been prized the specified amount of cash.");

                //Sync and clean up
                target.syncState();
            }
        }

        /// <summary>
        /// Prizes target player experience.
        /// </summary>
        static public void experience(Player player, Player recipient, string payload, int bong)
        {	//Find our target
            Player target = (recipient == null) ? player : recipient;

            //Convert our string to an int and check for illegal characters.
            int expVal;
            if (!Int32.TryParse(payload, out expVal))
            {   //Uh oh
                player.sendMessage(-1, "Payload can only contain numbers (0-9)");
            }
            else
            {   //Give our target player some experience!
                target.Experience += expVal;

                //Alert the player
                player.sendMessage(-3, "Target has been prized the specified amount of experience.");

                //Sync and clean up
                target.syncState();
            }
        }

        /// <summary>
        /// Sends a global message to every zone connected to current database
        /// </summary>
        static public void global(Player player, Player recipient, string payload, int bong)
        {   //Sanity checks
            if (payload == "" || player._server.IsStandalone)
                return;

            CS_Query<Data.Database> pkt = new CS_Query<Data.Database>();
            pkt.queryType = CS_Query<Data.Database>.QueryType.global;
            pkt.sender = player._alias;
            pkt.payload = payload;

            player._server._db.send(pkt);
        }

        /// <summary>
        /// Gives the user help information on a given command
        /// </summary>
        static public void help(Player player, Player recipient, string payload, int bong)
        {
            if (payload == "")
            {	//List all mod commands
                player.sendMessage(0, "&Commands available to you:");

                int playerPermission = (int)player.PermissionLevelLocal;

                string[] powers = { "Basic", "ArenaMod", "Mod", "SMod", "Head Mod", "Server Operator", "Developer" };
                int level;

                //New help list, sorted by level
                if (player.PermissionLevelLocal != Data.PlayerPermission.Developer)
                {
                    for (level = 0; level < 7; level++)
                    {
                        player.sendMessage(0, String.Format("!{0}", powers[level]));
                        foreach (HandlerDescriptor cmd in player._arena._commandRegistrar._modCommands.Values)
                            if (playerPermission >= (int)cmd.permissionLevel && level == (int)cmd.permissionLevel)
                                player.sendMessage(0, "**" + cmd.handlerCommand);
                    }
                }
                else
                {
                    foreach (HandlerDescriptor cmd in player._arena._commandRegistrar._modCommands.Values)
                        if (cmd.isDevCommand)
                            player.sendMessage(0, "**" + cmd.handlerCommand);
                }

                return;
            }

            //Attempt to find the command's handler
            HandlerDescriptor handler;

            if (!player._arena._commandRegistrar._modCommands.TryGetValue(payload.ToLower(), out handler))
            {
                player.sendMessage(-1, "Unable to find the specified command.");
                return;
            }

            //Do we have permission to view this?
            if ((int)player.PermissionLevelLocal < (int)handler.permissionLevel)
            {
                player.sendMessage(-1, "Unable to find the specified command.");
                return;
            }

            //Display help information
            player.sendMessage(0, "&*" + handler.handlerCommand + ": " + handler.commandDescription);
            player.sendMessage(0, "*Usage: " + handler.usage);
        }

        /// <summary>
        /// Permits a player in a permission-only zone
        /// </summary>
        static public void permit(Player player, Player recipient, string payload, int bong)
        {
            if (payload == "")
            {
                player.sendMessage(-1, "Syntax: permit name");
                return;
            }

            //Does he want a list?
            if (payload.ToLower() == "list")
            {
                player.sendMessage(0, Logic.Logic_Permit.listPermit());
                return;
            }
            //Are we adding or removing?
            if (Logic.Logic_Permit.checkPermit(payload))
            {   //Remove
                player.sendMessage(-3, "Player removed from permission list");
                Logic.Logic_Permit.removePermit(payload);
            }

            else
            {   //Adding
                player.sendMessage(-3, "Player added to permission list");
                Logic.Logic_Permit.addPermit(payload);

                //Let the person know
                SC_Whisper<Data.Database> whisper = new SC_Whisper<Data.Database>();
                whisper.bong = 1;
                whisper.recipient = payload.ToString();
                whisper.message = (String.Format("You have been given permission to enter {0}.", player._server.Name));
                whisper.from = player._alias;

                player._server._db.send(whisper);
            }
        }

        ///<summary>
        /// Starts an arena poll for any question asked
        ///</summary>
        public static void poll(Player player, Player recipient, string payload, int bong)
		{
			//Check for a running poll
			if (player._arena._poll != null && player._arena._poll.start)
			{
                if (payload == "cancel")
                {
                    player._arena.pollQuestion(player._arena, false);
                    return;
                }
				player.sendMessage(-1, "There is a poll in progress, you must wait till its finished to start a new one.");
				return;
			}
			else
			{
				if (payload == "")
				{
					player.sendMessage(-1, "Syntax: *poll <Question>(Optional <time> - Note: to use time seperate the question with : I.E ?poll Like me?:60");
					return;
				}
			}
            player._arena._poll = new Arena.PollSettings();
            int timer = 0;
            if (payload.Contains(':'))
            {
                string[] result = payload.Split(':');
                timer = Convert.ToInt32(result.ElementAt(1));
                player._arena.setTicker(1, 4, timer * 100, result.ElementAt(0), delegate() { player._arena.pollQuestion(player._arena, true); });
                player._arena.sendArenaMessage(String.Format("!A Poll has been started by {0}." + " Topic: {1}", (player.IsStealth ? "Unknown" : player._alias), result.ElementAt(0)));
            }
            else
            {
                player._arena.setTicker(1, 4, timer, payload); //Game ending will stop and display results
                player._arena.sendArenaMessage(String.Format("!A Poll has been started by {0}." + " Topic: {1}", (player.IsStealth ? "Unknown" : player._alias), payload));
            }

			player._arena._poll.start = true;
		}

        /// <summary>
        /// Spawns an item on the ground or in a player's inventory
        /// </summary>
        static public void prize(Player player, Player recipient, string payload, int bong)
        {	//Sanity checks
            if (payload == "")
            {
                player.sendMessage(-1, "Syntax: *prize item:amount or ::*prize item:amount");
                return;
            }

            //Determine the item and quantitiy
            string[] args = payload.Split(':');

            //Our handy string/int checkar
            bool IsNumeric = Regex.IsMatch(args[0], @"^\d+$");

            ItemInfo item;

            //Is he asking for an ITEMID?
            if (!IsNumeric)
            {   //Nope, pass as string.
                item = player._server._assets.getItemByName(args[0].Trim());
            }
            else { item = player._server._assets.getItemByID(Int32.Parse(args[0].Trim())); }

            int quantity = (args.Length == 1) ? 1 : Convert.ToInt32(args[1].Trim());

            if (item == null)
            {
                player.sendMessage(-1, "Unable to find the item specified.");
                return;
            }

            //Is it targetted?
            if (recipient == null)
            {	//We are to spawn it on the ground
                player._arena.itemSpawn(item, (ushort)quantity, player._state.positionX, player._state.positionY);
            }
            else
            {	//Modify the recipient inventory
                recipient.inventoryModify(item, quantity);
            }
        }

        /// <summary>
        /// Spits out information about a player's zone profile
        /// </summary>
        static public void profile(Player player, Player recipient, string payload, int bong)
        {
            Player target = (recipient == null) ? player : recipient;
            player.sendMessage(-3, "&Player Profile Information");
            player.sendMessage(0, "*" + target._alias);
            player.sendMessage(0, "Items");
            foreach (KeyValuePair<int, Player.InventoryItem> itm in target._inventory)
                player.sendMessage(0, String.Format("~{0}={1}", itm.Value.item.name, itm.Value.quantity));
            player.sendMessage(0, "Skills");
            foreach (KeyValuePair<int, Player.SkillItem> skill in target._skills)
                player.sendMessage(0, String.Format("~{0}={1}", skill.Value.skill.Name, skill.Value.quantity));
            player.sendMessage(0, "Profile");
            player.sendMessage(0, String.Format("~Cash={0} Experience={1} Points={2}", target.Cash, target.Experience, target.Points));
            player.sendMessage(0, "Base Vehicle");
            player.sendMessage(0, String.Format("~VehicleID={0} Health={1} Energy={2}", target._baseVehicle._id, target._baseVehicle._state.health, target._baseVehicle._state.energy));
        }

        /// <summary>
        /// Scrambles all players across all teams in an arena
        /// </summary>
        /// <param name="on/off">Sets the script for the arena</param>
        public static void scramble(Player player, Player recipient, string payload, int bong)
        {
            Random _rand = new Random();

            List<Player> shuffledPlayers = player._arena.PublicPlayersInGame.OrderBy(plyr => _rand.Next(0, 500)).ToList();
            int numTeams = 0;

            if (player._arena.ActiveTeams.Count() > 1)
            {
                //Set script based on user input
                if (payload.ToLower() == "on")
                {
                    player._arena._server._zoneConfig.arena.scrambleTeams = 1;
                    player.sendMessage(0, "Scramble toggled ON.");
                }
                else if (payload.ToLower() == "off")
                {
                    player._arena._server._zoneConfig.arena.scrambleTeams = 0;
                    player.sendMessage(0, "Scramble toggled OFF.");
                }
                else
                {
                    //Now scramble teams
                    numTeams = player._arena.ActiveTeams.Count();
                    for (int i = 0; i < shuffledPlayers.Count; i++)
                        player._arena.PublicTeams.ElementAt(i % numTeams).addPlayer(shuffledPlayers[i]);

                    //Notify players of the scramble
                    player._arena.sendArenaMessage("Teams have been scrambled!");
                }
            }
        }

        /// <summary>
        /// Toggles a players ability to use the messaging system entirely
        /// </summary>
        static public void shutup(Player player, Player recipient, string payload, int bong)
        {
            //Sanity checks
            if (recipient == null)
            {
                player.sendMessage(-1, "Syntax: ::*shutup");
                return;
            }

            if (player != recipient && (int)player.PermissionLevel < (int)recipient.PermissionLevel)
                return;

            if (payload == "")
            {
                player.sendMessage(-1, "Syntax: ::*shutup timeinminutes");
                return;
            }

            //Convert our payload into a numerical value
            int minutes = 0;
            try
            {
                minutes = Convert.ToInt32(payload);
            }
            catch (OverflowException e)
            {
                Log.write(TLog.Warning, e.ToString());
            }

            //Toggle his ability to speak
            recipient._bSilenced = !recipient._bSilenced;

            //Notify some people
            if (recipient._bSilenced)
            {
                recipient._timeOfSilence = DateTime.Now;
                recipient._lengthOfSilence = minutes;
                player.sendMessage(0, recipient._alias + " has been silenced");
            }
            else
            {
                recipient._lengthOfSilence = 0;
                player.sendMessage(0, recipient._alias + " has been unsilenced");
            }
        }

        /// <summary>
        /// Puts a player into spectator mode
        /// </summary>
        static public void spec(Player player, Player recipient, string payload, int bong)
        {	//Shove him in spec!
            Player target = (recipient == null) ? player : recipient;
            if (recipient != null && player != recipient && (int)player.PermissionLevel <= (int)recipient.PermissionLevel)
                return;
            if (payload.ToLower() == "all")
            {
                foreach (Player p in player._arena.PlayersIngame.ToList())
                {
                    p.spec();
                }
            }

            //Do we have a target team?
            if (payload != "")
            {	//Find the team
                Team newTeam = player._arena.getTeamByName(payload);
                if (newTeam == null)
                {
                    player.sendMessage(-1, "The specified team doesn't exist.");
                    return;
                }

                target.spec(newTeam);
            }
            else
                target.spec();
        }

        /// <summary>
        /// Forces a player to spectate another
        /// </summary>
        static public void spectate(Player player, Player recipient, string payload, int bong)
        {	//Sanity checks
            if (payload == "")
            {
                player.sendMessage(-1, "Syntax: ::*spectate [player] or *spectate [player]");
                return;
            }

            if (recipient != null && !recipient.IsSpectator)
            {
                player.sendMessage(-1, "Player isn't in spec.");
                return;
            }

            //Find the player he's talking about
            Player target = player._arena.getPlayerByName(payload);

            if (target == null)
            {
                player.sendMessage(-1, "Cannot find player '" + payload + "'.");
                return;
            }

            if (target.IsSpectator)
            {
                player.sendMessage(-1, "Target isn't in the game.");
                return;
            }

            //Let the games begin!
            if (recipient != null)
                recipient.spectate(target);
            else
            {
                foreach (Player p in player._arena.Players)
                    if (p.IsSpectator)
                        p.spectate(target);
            }
        }

        /// <summary>
        /// Locks a player in spec
        /// </summary>
        static public void speclock(Player player, Player recipient, string payload, int bong)
        {
            //Lock Entire Arena
            if (payload == "all")
            {
                player._arena._bLocked = !player._arena._bLocked;
                player._arena.sendArenaMessage("Arena lock has been toggled");

                //Spec them all.
                if (player._arena._bLocked)
                {
                    foreach (Player p in player._arena.Players)
                        p.spec();
                }

                return;
            }

            //Sanity checks
            if (recipient == null)
            {
                player.sendMessage(-1, "Syntax: ::*lock");
                return;
            }

            if (player != recipient && (int)player.PermissionLevel < (int)recipient.PermissionLevel)
                return;

            //Toggle his locked status
            recipient._bLocked = !recipient._bLocked;

            //Notify some people and send him to spec
            if (recipient._bLocked)
            {
                recipient.spec();
                player.sendMessage(0, recipient._alias + " has been locked in spec");
            }
            else
                player.sendMessage(0, recipient._alias + " has been been unlocked from spec");
        }

        /// <summary>
        /// Toggles stealth mode for mods, will prevent players from seeing them on lists
        /// </summary>
        static public void stealth(Player player, Player recipient, string payload, int bong)
        {
            if (player.PermissionLevel == Data.PlayerPermission.Developer)
            {
                player.sendMessage(-1, "You are not authorized to use this command.");
                return;
            }

            List<Player> audience = player._arena.Players.ToList();
            if (!player.IsStealth)
            {
                player.sendMessage(0, "Stealth mode is now 'ON'. You will no longer be seen in arena lists.");
                player._bIsStealth = true;
                foreach (Player person in audience)
                    //Their level is the same or greater, allow them to see him/her
                    if (person != player && person.PermissionLevel >= player.PermissionLevel)
                        Helpers.Object_PlayerLeave(person, player);
                return;
            }

            player.sendMessage(0, "Stealth mode is now 'OFF'.");
            player._bIsStealth = false;
            foreach (Player person in audience)
                if (person != player && person.PermissionLevel >= player.PermissionLevel)
                    Helpers.Object_Players(person, player);
        }

        /// <summary>
        /// Summons the specified player to yourself
        /// </summary>
        static public void summon(Player player, Player recipient, string payload, int bong)
        {	//Sanity checks
            if (recipient == null)
            {
                player.sendMessage(-1, "Syntax: ::*summon");
                return;
            }

            //Simply warp the recipient
            recipient.warp(player);
        }

        /// <summary>
        /// Puts another player, or yourself, on a specified team
        /// </summary>
        static public void team(Player player, Player recipient, string payload, int bong)
        {	//Sanity checks
            if (payload == "")
            {
                player.sendMessage(-1, "Syntax: *team [teamname] or ::*team [teamname]");
                return;
            }

            //Attempt to find the specified team
            Team newTeam = player._arena.getTeamByName(payload);
            if (newTeam == null)
            {
                player.sendMessage(-1, "The specified team doesn't exist.");
                return;
            }

            //Alter his team!
            Player target = (recipient == null) ? player : recipient;

            newTeam.addPlayer(target);
        }

        /// <summary>
        /// Renames public team names
        /// </summary>
        static public void teamname(Player player, Player recipient, string payload, int bong)
        {
            if (payload == "")
            {
                player.sendMessage(-1, "Syntax: *teamname [teamname1,teamname2,...]");
            }

            List<string> teamNames = new List<string>();
            List<Team> teams = new List<Team>(player._arena.Teams);

            if (payload.IndexOf(",") != -1)
                teamNames = new List<string>(payload.Split(','));
            else
                teamNames.Add(payload);

            //Spec everybody in the arena and rename team names
            foreach (Player unspecced in player._arena.PlayersIngame.ToArray())
                unspecced.spec();

            for (int i = 0; i < teamNames.Count; i++)
            {
                if (teams[i + 1]._name == null)
                {
                    Team newTeam = new Team(player._arena, player._server);
                    teams.Add(newTeam);
                }
                //Add 1 to avoid renaming spec team   
                teams[i + 1]._name = teamNames[i];
            }
        }

        /// <summary>
        /// Sets a ticker at the specified index with color/timer/messageSyntax: *ticker [message],[index],[color=optional],[timer=optional]
        /// </summary>
        static public void ticker(Player player, Player recipient, string payload, int bong)
        {
            if (payload == "" || !payload.Contains(','))
            {
                player.sendMessage(-1, "Syntax: *ticker [message],[index],[color=optional],[timer=optional]");
            }
            string tMessage;
            int tIndex;
            int tColor;
            int tTimer;

            string[] parameters = payload.Split(',');
            tMessage = parameters[0];
            tIndex = Convert.ToInt32(parameters[1]);
            if (parameters.Count() >= 3)
                tColor = Convert.ToInt16(parameters[2]);
            else
                tColor = 0;
            if (parameters.Count() >= 4)
                tTimer = Convert.ToInt32(parameters[3]);
            else
                tTimer = 0;

            player._arena.setTicker((byte)tColor, tIndex, tTimer * 100, tMessage);
        }

        /// <summary>
        /// Sets a ticker to display a timer
        /// </summary>
        static public void timer(Player player, Player recipient, string payload, int bong)
        {
            if (payload == "")
            {   //Clear current timer if payload is empty
                player._arena.setTicker(1, 1, 0, "");
            }
            else
            {
                try
                {
                    int minutes = 0;
                    int seconds = 0;
                    if (payload.Contains(":"))
                    {   //Split the payload up if it contains a semi-colon
                        char[] splitArr = { ':' };
                        string[] items = payload.Split(splitArr, StringSplitOptions.RemoveEmptyEntries);

                        if (items.Count() > 1)
                        {   //Format: *timer xx:xx
                            minutes = Convert.ToInt32(items[0].Trim());
                            seconds = Convert.ToInt32(items[1].Trim());
                        }
                        else
                        {
                            if (payload.StartsWith(":"))
                            {   //Format: *timer :xx
                                seconds = Convert.ToInt32(items[0].Trim());
                            }
                            else
                            {   //Format: *timer xx:
                                minutes = Convert.ToInt32(items[0].Trim());
                            }
                        }
                    }
                    else
                    {   //Format: *timer xx                   
                        minutes = Convert.ToInt32(payload.Trim());
                    }

                    if (minutes > 0 || seconds > 0)
                    {   //Timer works on increments of 10ms, excludes negative timers
                        player._arena.setTicker(1, 1, minutes * 6000 + seconds * 100, "Time Remaining: ");
                    }
                    else
                    {
                        player._arena.setTicker(1, 1, 0, "");
                    }
                }
                catch
                {
                    player.sendMessage(-1, "Invalid timer amount.  Format: *timer xx or *timer xx:xx");
                }
            }
        }

        /// <summary>
        /// Gets a player in spectator mode, and puts him out onto a team
        /// </summary>
        static public void unspec(Player player, Player recipient, string payload, int bong)
        {	//Find our target
            Player target = (recipient == null) ? player : recipient;

            //Do we have a target team?
            if (payload != "")
            {	//Find the team
                Team newTeam = player._arena.getTeamByName(payload);
                if (newTeam == null)
                {
                    player.sendMessage(-1, "The specified team doesn't exist.");
                    return;
                }

                if (player.IsSpectator)
                {   //Unspec him
                    target.unspec(newTeam);
                }
                else
                {   //Change team
                    newTeam.addPlayer(target);
                }
            }
            else
            {
                //Join next available team
                if (target._arena.pickAppropriateTeam(target) != null)
                {   //Great, use it
                    target.unspec(target._arena.pickAppropriateTeam(target));
                }
                else
                {
                    player.sendMessage(-1, "Unable to unspec on that team");
                }
            }
        }

        /// <summary>
        /// Warps the player to a specified location or player
        /// </summary>
        static public void warp(Player player, Player recipient, string payload, int bong)
        {	//Do we have a target?
            if (recipient != null)
            {	//Do we have a destination?
                if (payload == "")
                    //Simply warp to the recipient
                    player.warp(recipient);
                else
                {	//Are we dealing with coords or exacts?
                    payload = payload.ToLower();

                    if (payload[0] >= 'a' && payload[0] <= 'z')
                    {
                        int x = (((int)payload[0]) - ((int)'a')) * 16 * 80;
                        int y = Convert.ToInt32(payload.Substring(1)) * 16 * 80;

                        //We want to spawn in the coord center
                        x += 40 * 16;
                        y -= 40 * 16;

                        recipient.warp(x, y);
                    }
                    else
                    {
                        string[] coords = payload.Split(',');
                        int x = Convert.ToInt32(coords[0]) * 16;
                        int y = Convert.ToInt32(coords[1]) * 16;

                        recipient.warp(x, y);
                    }
                }
            }
            else
            {	//We must have a payload for this
                if (payload == "")
                {
                    player.sendMessage(-1, "Syntax: ::*warp or *warp A4 or *warp 123,123");
                    return;
                }

                //Are we dealing with coords or exacts?
                payload = payload.ToLower();

                if (payload[0] >= 'a' && payload[0] <= 'z')
                {
                    int x = (((int)payload[0]) - ((int)'a')) * 16 * 80;
                    int y = Convert.ToInt32(payload.Substring(1)) * 16 * 80;

                    //We want to spawn in the coord center
                    x += 40 * 16;
                    y -= 40 * 16;

                    player.warp(x, y);
                }
                else
                {
                    string[] coords = payload.Split(',');
                    int x = Convert.ToInt32(coords[0]) * 16;
                    int y = Convert.ToInt32(coords[1]) * 16;

                    player.warp(x, y);
                }
            }
        }

        /// <summary>
        /// Wipes a character
        /// </summary>
        public static void wipe(Player player, Player recipient, string payload, int bong)
        {   //Sanity checks
            if (recipient == null)
            {
                player.sendMessage(-1, "Syntax: ::*wipe");
                return;
            }

            if (!recipient.IsSpectator)
            {
                player.sendMessage(-1, "That character must be in spec.");
                return;
            }

            //Wiping all stats/inv etc
            recipient.assignFirstTimeStats(true);
            player.syncInventory();
            player.syncState();
            Logic_Assets.RunEvent(player, player._server._zoneConfig.EventInfo.firstTimeSkillSetup);
            Logic_Assets.RunEvent(player, player._server._zoneConfig.EventInfo.firstTimeInvSetup);

            player.sendMessage(0, "His/her character has been wiped.");
        }

        /// <summary>
        /// Removes a player from the server
        /// </summary>
        static public void kill(Player player, Player recipient, string payload, int bong)
        {
            //Kill all?
            if (payload != null && payload.Equals("all", StringComparison.CurrentCultureIgnoreCase))
                foreach (Player p in player._arena.Players)
                    p.destroy();
            else
                if (recipient != null && (int)player.PermissionLevel >= (int)recipient.PermissionLevel)
                    //Destroy him!
                    recipient.destroy();

                else
                    player.sendMessage(-1, "Syntax: ::*kill reason (optional) or *kill all");
        }

        /// <summary>
        /// Removes a player from the entire server
        /// </summary>
        static public void gkill(Player player, Player recipient, string payload, int bong)
        {
            SqlConnection db;
            string pAccount = "";
            string alias = "";
            string reason = "None given";
            int minutes = 0;

            if (payload == "" && recipient == null)
            {
                player.sendMessage(-1, "Syntax: Either PM the person with *gkill time:reason(Optional) or *gkill alias time:reason(Optional)");
                return;
            }

            string[] parameters;
            string[] param;
            //Check to see if we are pm'ing someone with gkill
            if (recipient != null)
            {
                if ((int)player.PermissionLevel <= (int)recipient.PermissionLevel)
                    return;

                if (payload == "")
                {
                    player.sendMessage(-1, "Syntax: ::*gkill time:reason(Optional)  Note: Time is in minutes and must be less than 1,000,000.");
                    return;
                }

                alias = recipient._alias.ToString();

                //Let the person know
                if (minutes > 0)
                    recipient.sendMessage(0, String.Format("You are being banned for {0} minutes.", minutes));

                //KILL HIM!!
                recipient.destroy();
            }
            else //Using an alias instead
            {
                param = payload.Split(' ');
                alias = param[0].ToString();
            }

            //Must be a timed ban?
            if (payload.Contains(':'))
            {
                parameters = payload.Split(':');
                try
                {
                    minutes = Convert.ToInt32(parameters[0]);
                }
                catch
                {
                    if (minutes > Int32.MaxValue)
                        minutes = Int32.MaxValue - 1;
                }

                //Check if there is a reason
                if (parameters.Count() >= 1)
                    reason = parameters[1];
            }

            //For no reason given and no seperator
            if (!payload.Contains(':'))
            {
                parameters = payload.Split(' ');
                try
                {
                    if (recipient != null)
                        minutes = Convert.ToInt32(parameters[0]);
                    else
                        minutes = Convert.ToInt32(parameters[1]);
                }
                catch
                {
                    if (minutes > Int32.MaxValue)
                    {
                        Log.write("OverflowException reached using gkill, setting to max value.");
                        minutes = Int32.MaxValue - 1;
                    }
                    else
                        minutes = 30; //30 mins
                }
            }

            player.sendMessage(0, String.Format("You are banning {0} for {1} minutes.", alias, minutes));

            //Check if they are already banned
            db = new SqlConnection("Server=INFANTRY\\SQLEXPRESS;Database=Data;Trusted_Connection=True;");
            db.Open();
            //Get their Account ID
            var playerID = new SqlCommand("SELECT * FROM alias WHERE name='" + alias + "'", db);
            try
            {
                using (var reader = playerID.ExecuteReader())
                {
                    while (reader.Read())
                        pAccount = reader["account"].ToString();
                }
            }
            catch
            {
            }

            if (pAccount != "")
            {
                //They already exist, delete current entry and move on to adding them
                using (SqlCommand Command = new SqlCommand("DELETE FROM bans WHERE account=" + pAccount, db))
                    Command.ExecuteNonQuery();
            }

            db.Close();

            //Relay it to the database             
            CS_Ban<Data.Database> newBan = new CS_Ban<Data.Database>();
            newBan.alias = alias;
            if (recipient != null)
            {
                newBan.UID1 = recipient._UID1;
                newBan.UID2 = recipient._UID2;
                newBan.UID3 = recipient._UID3;
            }
            newBan.time = minutes;
            newBan.sender = player._alias;
            newBan.reason = reason;

            player._server._db.send(newBan);
        }

        /// <summary>
        /// Registers all handlers
        /// </summary>
        [Commands.RegistryFunc(HandlerType.ModCommand)]
        static public IEnumerable<Commands.HandlerDescriptor> Register()
        {
            yield return new HandlerDescriptor(help, "help",
                "Gives the user help information on a given command.",
                "*help [commandName]");

            yield return new HandlerDescriptor(addball, "addball",
                "Adds a ball to the arena.",
                "*addball",
               InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(arena, "arena",
                "Send a arena-wide system message.",
                "*arena message",
               InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(authenticate, "auth",
                "Log in during Stand-Alone Mode",
                "*auth password");

            yield return new HandlerDescriptor(cash, "cash",
                "Prizes specified amount of cash to target player",
                "*cash [amount] or ::*cash [amount]",
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(experience, "experience",
                "Prizes specified amount of experience to target player",
                "*experience [amount] or ::*experience [amount]",
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(gkill, "gkill",
                "Globally bans a player from the entire server",
                "::*gkill minutes:reason",
                InfServer.Data.PlayerPermission.SMod, false);

            yield return new HandlerDescriptor(global, "global",
               "Sends a global message to every zone connected to current database",
               "*global [message]",
               InfServer.Data.PlayerPermission.SMod, false);

            yield return new HandlerDescriptor(kill, "kill",
               "Removes the target player from the server",
               "::*kill reason or *kill all",
               InfServer.Data.PlayerPermission.SMod, false);

            yield return new HandlerDescriptor(speclock, "lock",
                "Locks the target player into spec",
                "::*lock",
                InfServer.Data.PlayerPermission.ArenaMod, false);

            yield return new HandlerDescriptor(permit, "permit",
                "Permits target player to enter a permission-only zone.",
                "*permit alias",
               InfServer.Data.PlayerPermission.Mod, true);

            yield return new HandlerDescriptor(poll, "poll",
                "Starts a poll for a question asked. *poll cancel to stop one",
                "*poll question(Optional: *poll question:timer)",
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(prize, "prize",
                "Spawns an item on the ground or in a player's inventory",
                "*prize item:amount or ::*prize item:amount",
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(profile, "profile",
                "Displays a player's inventory.",
                "/*profile or :player:*profile or *profile",
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(scramble, "scramble",
                "Scrambles an arena, use on/off to set the arena to scramble automatically.",
                "::*scramble [on/off(optional)]",
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(shutup, "shutup",
               "Toggles a players ability to use the messaging system entirely",
               "::*shutup",
               InfServer.Data.PlayerPermission.ArenaMod, false);

            yield return new HandlerDescriptor(spec, "spec",
                "Puts a player into spectator mode, optionally on a specified team.",
                "*spec or ::*spec or ::*spec [team]",
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(spectate, "spectate",
                "Forces a player or the whole arena to spectate the specified player.",
                "Syntax: ::*spectate [player] or *spectate [player]",
                InfServer.Data.PlayerPermission.Sysop, false);

            yield return new HandlerDescriptor(stealth, "stealth",
                "Toggles stealth mode, mods become invisible to arena's",
                "*stealth",
                InfServer.Data.PlayerPermission.Mod, false);

            yield return new HandlerDescriptor(summon, "summon",
                "Summons a specified player to your location.",
                "::*summon",
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(team, "team",
                "Puts another player, or yourself, on a specified team",
                "*team [teamname] or ::*team [teamname]",
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(teamname, "teamname",
                "Renames public team names in the arena",
                "*teamname [teamname1,teamname2,...]",
                InfServer.Data.PlayerPermission.ArenaMod, false);

            yield return new HandlerDescriptor(ticker, "ticker",
               "Sets a ticker at the specified index with color/timer/message",
               "*ticker [message],[index],[color=optional],[timer=optional]",
               InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(timer, "timer",
                "Sets a ticker to display a timer",
                "Syntax: *timer xx or *timer xx:xx",
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(warp, "warp",
                "Warps you to a specified player, coordinate or exact coordinate. Alternatively, you can warp other players to coordinates or exacts.",
                "::*warp or *warp A4 or *warp 123,123",
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(unspec, "unspec",
                "Takes a player out of spectator mode and puts him on the specified team.",
                "*unspec [team] or ::*unspec [team] or ::*unspec ..",
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(wipe, "wipe",
                "Wipes a character within the current zone",
                "::*wipe",
                InfServer.Data.PlayerPermission.Sysop, false);
        }
    }
}