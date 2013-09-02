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
            //Grab a new ballID
            int ballID = player._arena._balls.Count; // We don't add 1, as the first ballID is 0, so the count is always the correct next ID (1 ball = ballID of 0, = next ballID of 1)            

            if (ballID > 4)
            {
                Log.write(TLog.Warning, "Attempt to add a 7th ball, too many!!!!");
                player.sendMessage(-1, "All the balls are currently in the game.");
                return;
            }
            //Lets create a new ball!
            Ball newBall = new Ball((short)ballID, player._arena);

            //Initialize its ballstate
            newBall._state = new Ball.BallState();

            //Assign default state
            newBall._state.positionX = 2817;
            newBall._state.positionY = 1600;
            newBall._state.positionZ = 5;
            newBall._state.unk2 = -1;
            //newBall._state.carrier = 1;


            //Store it.
            //player._arena._balls.Add(newBall);


            //Make each player aware of the ball
            //newBall.Route_Ball(player._arena.Players);
            //player._arena.sendArenaMessage("Ball Added");
        }
        static public void getball(Player player, Player recipient, string payload, int bong)
        {
            ushort askedBallId; // Id of the requested getball
            if (payload == "")
            {
                askedBallId = 0;
            }
            else
            {
                ushort requestBallID = Convert.ToUInt16(payload);
                if (requestBallID > 4 || requestBallID < 0)
                {
                    Log.write(TLog.Warning, "Invalid getball Ball ID");
                    player.sendMessage(-1, "Invalid getball Ball ID");
                    return;
                }
                askedBallId = requestBallID;
            }
            //Get the ball in question..
            Ball ball = recipient._arena._balls.FirstOrDefault(b => b._id == askedBallId);
            foreach (Player p in player._arena.Players)
            {
                p._gotBallID = 999;
            }
            if (ball == null)
            {
                Log.write(TLog.Warning, "Ball does not exist.");
                player.sendMessage(-1, "Ball does not exist.");
                return;
            }
            recipient._gotBallID = ball._id;
            //Assign the ball to the player
            ball._state.carrier = recipient;
            //Assign default state
            ball._state.positionX = recipient._state.positionX;
            ball._state.positionY = recipient._state.positionY;
            ball._state.positionZ = recipient._state.positionZ;
            ball._state.velocityX = 0;
            ball._state.velocityY = 0;
            ball._state.velocityZ = 0;
            ball._state.unk2 = -1;
            //newBall._state.carrier = 1;


            //Store it.
            //player._arena._balls.Add(newBall);

            //Make each player aware of the ball
            ball.Route_Ball(recipient._arena.Players);

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
            if (!player._server.IsStandalone)
            {
                player.sendMessage(-1, "Command is only available in stand-alone mode.");
                return;
            }

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

            //Get players mod level first
            int level;
            if (player.PermissionLevelLocal == Data.PlayerPermission.ArenaMod)
                level = (int)player.PermissionLevelLocal;
            else
                level = (int)player.PermissionLevel;

            if (player._arena._name.StartsWith("Public", StringComparison.OrdinalIgnoreCase) && level < (int)Data.PlayerPermission.Mod)
            {
                player.sendMessage(-1, "You can only use it in non-public arena's.");
                return;
            }

            if (!player._arena._name.StartsWith("Public", StringComparison.OrdinalIgnoreCase) && !player._arena.IsPrivate)
                if (level < (int)Data.PlayerPermission.Mod)
                {
                    player.sendMessage(-1, "You can only use it in private arena's.");
                    return;
                }

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

            //Get players mod level first
            int level;
            if (player.PermissionLevelLocal == Data.PlayerPermission.ArenaMod)
                level = (int)player.PermissionLevelLocal;
            else
                level = (int)player.PermissionLevel;

            if (player._arena._name.StartsWith("Public", StringComparison.OrdinalIgnoreCase) && level < (int)Data.PlayerPermission.Mod)
            {
                player.sendMessage(-1, "You can only use it in non-public arena's.");
                return;
            }

            if (!player._arena._name.StartsWith("Public", StringComparison.OrdinalIgnoreCase) && !player._arena.IsPrivate)
                if (level < (int)Data.PlayerPermission.Mod)
                {
                    player.sendMessage(-1, "You can only use it in private arena's.");
                    return;
                }

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
            if (payload == "")
            {
                player.sendMessage(-1, "And say what?");
                return;
            }
            
            if (player._server.IsStandalone)
            {
                player.sendMessage(-1, "Server is currently in stand-alone mode.");
                return;
            }

            CS_Query<Data.Database> pkt = new CS_Query<Data.Database>();
            pkt.queryType = CS_Query<Data.Database>.QueryType.global;
            pkt.sender = player._alias;
            pkt.payload = payload;

            player._server._db.send(pkt);
        }

        /// <summary>
        /// Grants a player private arena privileges
        /// </summary>
        static public void grant(Player player, Player recipient, string payload, int bong)
        {
            //Sanity checks
            if (!player._arena.IsPrivate)
            {
                player.sendMessage(-1, "This is not a private arena.");
                return;
            }

            if (!player._arena.IsGranted(player))
            {
                player.sendMessage(-1, "You are not granted in this arena.");
                return;
            }

            if (recipient != null)
            {
                if (recipient.PermissionLevel > Data.PlayerPermission.ArenaMod)
                {
                    player.sendMessage(-1, "That person doesn't need grant.");
                    return;
                }

                if (player._arena.IsGranted(recipient))
                {
                    player.sendMessage(-1, "He/she is already granted.");
                    return;
                }
                player._arena._owner.Add(recipient._alias);
                recipient._permissionTemp = Data.PlayerPermission.ArenaMod;
                recipient.sendMessage(0, "You are now granted arena privileges.");
            }
            else
            {
                if (payload == "")
                {
                    player.sendMessage(-1, "Syntax: *grant alias");
                    return;
                }

                if ((recipient = player._arena.getPlayerByName(payload.ToLower() )) == null)
                {
                    player.sendMessage(-1, "That player doesn't exist.");
                    return;
                }

                if (recipient.PermissionLevel > Data.PlayerPermission.ArenaMod)
                {
                    player.sendMessage(-1, "That person doesn't need grant.");
                    return;
                }

                if (player._arena.IsGranted(recipient))
                {
                    player.sendMessage(-1, "He/she is already granted.");
                    return;
                }
                player._arena._owner.Add(recipient._alias);
                recipient._permissionTemp = Data.PlayerPermission.ArenaMod;
                recipient.sendMessage(0, "You are now granted arena privileges.");
            }

            player.sendMessage(0, "You have given " + recipient._alias.ToString() + " arena privileges.");
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

                string[] powers = { "Basic", "ArenaMod", "Mod", "SMod", "Manager", "Head Mod/Sysop", "Developer" };
                int level;

                //New help list, sorted by level
                if (player.PermissionLevelLocal != Data.PlayerPermission.Developer)
                {
                    for (level = 0; level <= (int)player.PermissionLevelLocal; level++)
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
        /// Returns a list of help calls used in every server
        /// </summary>
        static public void helpcall(Player player, Player recipient, string payload, int bong)
        {
            int page;
            if (payload == "")
                page = 0;
            else
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
            CS_Query<Data.Database> pkt = new CS_Query<Data.Database>();
            pkt.queryType = CS_Query<Data.Database>.QueryType.helpcall;
            pkt.sender = player._alias;
            pkt.payload = page.ToString();
            //Send it!
            player._server._db.send(pkt);
        }

        /// <summary>
        /// Permits a player in a permission-only zone
        /// </summary>
        static public void permit(Player player, Player recipient, string payload, int bong)
        {
            if (payload == "")
            {
                player.sendMessage(-1, "Syntax: *permit name");
                return;
            }

            //Does he want a list?
            if (payload.ToLower() == "list")
            {
                player.sendMessage(0, Logic.Logic_Permit.listPermit());
                return;
            }

            //Are we adding or removing?
            bool found = false; //For lowered aliases
            if (Logic.Logic_Permit.checkPermit(payload))
            {   //Remove
                player.sendMessage(-3, "Player removed from permission list");
                Logic.Logic_Permit.removePermit(payload);
                found = true;
            }
            else if (!found && Logic.Logic_Permit.checkPermit(payload.ToLower()))
            {
                //Remove
                player.sendMessage(-3, "Player removed from permission list");
                Logic.Logic_Permit.removePermit(payload.ToLower());
            }
            else
            {   //Adding
                player.sendMessage(-3, "Player added to permission list");
                Logic.Logic_Permit.addPermit(payload);

                //Let the person know
                CS_Whisper<Data.Database> whisper = new CS_Whisper<Data.Database>();
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
                    player._arena.sendArenaMessage(String.Format("{0}'s Poll has been cancelled.", player._alias));
                    return;
                }
                else if (payload == "end" || payload == "off")
                {
                    player._arena.pollQuestion(player._arena, true);
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
                player._arena.sendArenaMessage("!Type ?poll yes or ?poll no to participate");
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

            //Get players mod level first
            int level;
            if (player.PermissionLevelLocal == Data.PlayerPermission.ArenaMod)
                level = (int)player.PermissionLevelLocal;
            else
                level = (int)player.PermissionLevel;

            if (player._arena._name.StartsWith("Public", StringComparison.OrdinalIgnoreCase) && level < (int)Data.PlayerPermission.Mod)
            {
                player.sendMessage(-1, "You can only use it in non-public arena's.");
                return;
            }

            if (!player._arena._name.StartsWith("Public", StringComparison.OrdinalIgnoreCase) && !player._arena.IsPrivate)
                if (level < (int)Data.PlayerPermission.Mod)
                {
                    player.sendMessage(-1, "You can only use it in private arena's.");
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
                player._arena.itemSpawn(item, (ushort)quantity, player._state.positionX, player._state.positionY, player);
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
            if (payload != "")
            {
                if ((target = player._arena.getPlayerByName(payload)) == null)
                    target = player;
            }

            player.sendMessage(-3, "&Player Profile Information");
            player.sendMessage(0, "*" + target._alias);
            if (target == player)
                player.sendMessage(0, "&Permission Level: " + (int)player.PermissionLevel);
            else if (player.PermissionLevel >= target.PermissionLevel)
                player.sendMessage(0, "&Permission Level: " + (int)target.PermissionLevel);
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
                player.sendMessage(-1, "Syntax: :alias:*shutup");
                return;
            }

            if (player != recipient && (int)player.PermissionLevel < (int)recipient.PermissionLevel)
                return;

            if (payload == "")
            {
                player.sendMessage(-1, "Syntax: :alias:*shutup timeinminutes");
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

            //Are we adding on more time?
            if (recipient._bSilenced && minutes > 0)
            {
                int min = 0;
                DateTime time = DateTime.Now;
                if (recipient._server._playerSilenced.ContainsKey(recipient._alias))
                {
                    if (recipient._server._playerSilenced[recipient._alias].Keys.Count > 0)
                    {
                        //Lets update his/her time
                        foreach (int length in recipient._server._playerSilenced[recipient._alias].Keys)
                            min = length;
                        foreach (DateTime timer in recipient._server._playerSilenced[recipient._alias].Values)
                            time = timer;

                        recipient._server._playerSilenced.Remove(recipient._alias);
                        //Make a new one
                        min = min + minutes;
                        time = time.AddMinutes(minutes);
                        recipient._server._playerSilenced.Add(recipient._alias, new Dictionary<int, DateTime>());
                        recipient._server._playerSilenced[recipient._alias].Add(min, time);

                        player.sendMessage(0, recipient._alias + " has been silenced for an additional " + minutes + " minutes.");
                        return;
                    }
                    else
                        recipient._server._playerSilenced[recipient._alias].Add(min + minutes, time);
                }
                else
                {
                    //No list found for player, adding it
                    recipient._server._playerSilenced.Add(recipient._alias, new Dictionary<int, DateTime>());
                    recipient._server._playerSilenced[recipient._alias].Add(min + minutes, time);
                }
                recipient._lengthOfSilence = min + minutes;
                player.sendMessage(0, recipient._alias + " has been silenced for an additional " + minutes + " minutes.");
                return;
            }

            //Toggle his ability to speak
            recipient._bSilenced = !recipient._bSilenced;

            //Notify some people
            if (recipient._bSilenced)
            {
                recipient._timeOfSilence = DateTime.Now;
                recipient._lengthOfSilence = minutes;
                //Lets add him to the zone silencer so the fucker cant avoid it by re-entering the zone
                if (recipient._server._playerSilenced.ContainsKey(recipient._alias))
                    recipient._server._playerSilenced[recipient._alias].Add(recipient._lengthOfSilence, DateTime.Now);
                else
                {
                    recipient._server._playerSilenced.Add(recipient._alias, new Dictionary<int, DateTime>());
                    recipient._server._playerSilenced[recipient._alias].Add(recipient._lengthOfSilence, DateTime.Now);
                }
                player.sendMessage(0, recipient._alias + " has been silenced.");
            }
            else
            {
                recipient._lengthOfSilence = 0;
                if (recipient._server._playerSilenced.ContainsKey(recipient._alias))
                    recipient._server._playerSilenced.Remove(recipient._alias);
                player.sendMessage(0, recipient._alias + " has been unsilenced.");
            }
        }

        /// <summary>
        /// Puts a player into spectator mode
        /// </summary>
        static public void spec(Player player, Player recipient, string payload, int bong)
        {	//Shove him in spec!
            Player target = (recipient == null) ? player : recipient;
            //if (recipient != null && player != recipient && (int)player.PermissionLevel <= (int)recipient.PermissionLevel)
                //return;
            if (payload.ToLower() == "all")
            {
                foreach (Player p in player._arena.PlayersIngame.ToList())
                    p.spec();
                return;
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
            if (payload == "all" || (payload == "" && recipient == null))
            {
                player._arena._bLocked = !player._arena._bLocked;
                player._arena.sendArenaMessage("Arena lock has been toggled" + (player._arena._bLocked ? " ON!" : " OFF!"));

                return;
            }

            //Sanity checks
            if (recipient == null)
            {
                player.sendMessage(-1, "Syntax: ::*lock or *lock");
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
                player.sendMessage(0, recipient._alias + " has been locked in spec.");
            }
            else
                player.sendMessage(0, recipient._alias + " has been been unlocked from spec.");
        }

        /// <summary>
        /// Toggles chatting to spec only
        /// </summary>
        static public void specquiet(Player player, Player recipient, string payload, int bong)
        {
            //Lets see if we are using it on a specific player
            if (recipient != null)
            {
                if (recipient._specQuiet)
                    recipient._specQuiet = false;
                else
                    recipient._specQuiet = true;
                player.sendMessage(0, "You have turned spec quiet" + (recipient._specQuiet ? " on " : " off ") + "for " + recipient._alias);
                return;
            }

            if (player._arena._specQuiet)
                player._arena._specQuiet = false;
            else
                player._arena._specQuiet = true;

            player._arena.sendArenaMessage("Spec Quiet is now" + (player._arena._specQuiet ? " ON!" : " OFF!"), 0);
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
                    if (person != player && player.PermissionLevel >= person.PermissionLevel)
                        Helpers.Object_PlayerLeave(person, player);
                return;
            }

            player.sendMessage(0, "Stealth mode is now 'OFF'.");
            player._bIsStealth = false;
            foreach (Player person in audience)
                if (person != player && person.PermissionLevel < player.PermissionLevel)
                    Helpers.Object_Players(person, player);
        }

        /// <summary>
        /// Summons the specified player to yourself
        /// </summary>
        static public void summon(Player player, Player recipient, string payload, int bong)
        {	//Sanity checks
            if (recipient == null && payload != "all")
            {
                player.sendMessage(-1, "Syntax: ::*summon or *summon all");
                return;
            }

            if (recipient != null)
            {
                //Simply warp the recipient
                recipient.warp(player);
            }

            if (payload == "all")
            {
                //summon everybody
                foreach (Player p in player._arena.PlayersIngame)
                    p.warp(player);
            }
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

            string temp = payload.ToLower();
            if (temp.Equals("spec") || temp.Equals("spectator"))
            {
                player.sendMessage(-1, "You can't use this team name.");
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

                string temp = payload.ToLower();
                if (temp.Equals("spec") || temp.Equals("spectator") || newTeam == null)
                {
                    player.sendMessage(-1, "Invalid team specified");
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

                if ((recipient = player._arena.getPlayerByName(payload)) != null)
                {
                    //Just warping to the player
                    player.warp(recipient);
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
        /// Toggles viewing of the mod commands on or off
        /// </summary>
        public static void watchmod(Player p, Player recipient, string payload, int bong)
        {
            p._arena._watchMod = !p._arena._watchMod;
            p.sendMessage(0, "You will" + (p._arena._watchMod ? " now see " : " no longer see ") + "mod commands.");
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
            recipient.syncInventory();
            recipient.syncState();
            Logic_Assets.RunEvent(recipient, recipient._server._zoneConfig.EventInfo.firstTimeSkillSetup);
            Logic_Assets.RunEvent(recipient, recipient._server._zoneConfig.EventInfo.firstTimeInvSetup);

            player.sendMessage(0, "His/her character has been wiped.");
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////Below are Banning Commands/////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////
/*
        /// <summary>
        /// Bans a player from a specific zone
        /// </summary>
        static public void ban(Player player, Player recipient, string payload, int bong)
        {
            //Sanity check
            if (player._server.IsStandalone)
            {
                player.sendMessage(-1, "Server is currently in stand-alone mode.");
                return;
            }

            if (recipient == null && payload == "")
            {
                player.sendMessage(-1, "Syntax: either ::*ban time:reason(optional) or *ban alias time:reason(optional)");
                return;
            }

            string[] param;
            string reason = "None given";
            string alias;
            int minutes = 0;
            int number;

            //Check to see if we are pm'ing someone
            if (recipient != null)
            {
                if ((int)player.PermissionLevel <= (int)recipient.PermissionLevel)
                {
                    player.sendMessage(-1, "You can't ban someone equal or higher than you.");
                    return;
                }

                if (payload == "")
                {
                    player.sendMessage(-1, "Syntax: ::*ban time:reason(Optional)");
                    return;
                }

                alias = recipient._alias.ToString();

                //Let the person know
                if (minutes > 0)
                    recipient.sendMessage(0, String.Format("You are being banned for {0} minutes.", minutes));
            }
            else //Using an alias instead
            {
                param = payload.Split(' ');
                if (!Int32.TryParse(param.ElementAt(0), out number))
                    alias = param[0].ToString();
                else
                {
                    player.sendMessage(-1, "Syntax: *ban alias time:reason(Optional)");
                    return;
                }
            }

            //Must be a timed ban?
            if (payload.Contains(':'))
            {
                param = payload.Split(':');
                string[] pick = param.ElementAt(0).Split(' ');
                try
                {
                    if (recipient != null)
                        minutes = Convert.ToInt32(param.ElementAt(0));
                    else
                        minutes = Convert.ToInt32(pick.ElementAt(1));
                }
                catch
                {
                    player.sendMessage(-1, "That is not a valid time.");
                    return;
                }

                //Check if there is a reason
                if (param[1] != null && param[1] != "")
                    reason = param[1];
            }
            else
            {
                param = payload.Split(' ');
                if (recipient == null)
                {
                    if (!Int32.TryParse(param.ElementAt(1), out number) || param[1] == "" || param[1] == null)
                    {
                        player.sendMessage(-1, "Syntax: *ban alias time:reason(optional)");
                        return;
                    }
                    try
                    {
                        minutes = Convert.ToInt32(param[1]);
                    }
                    catch (OverflowException)
                    {
                        player.sendMessage(-1, "That is not a valid time.");
                    }
                }
                else
                {
                    if (!Int32.TryParse(payload, out number))
                    {
                        player.sendMessage(-1, "Syntax: ::*ban time:reason(optional)");
                        return;
                    }
                    try
                    {
                        minutes = Convert.ToInt32(payload);
                    }
                    catch (OverflowException)
                    {
                        player.sendMessage(-1, "That is not a valid time.");
                    }
                }
            }

            //KILL HIM!! Note: putting it here because the person never see's the message before being dc'd
            if ((recipient = player._arena.getPlayerByName(alias)) != null)
                recipient.disconnect();

            player.sendMessage(0, String.Format("You are banning {0} for {1} minutes.", alias, minutes));

            //Relay it to the database
            CS_Ban<Data.Database> newBan = new CS_Ban<Data.Database>();
            newBan.banType = CS_Ban<Data.Database>.BanType.account;
            newBan.alias = alias;
            if (recipient != null)
            {
                newBan.UID1 = recipient._UID1;
                newBan.UID2 = recipient._UID2;
                newBan.UID3 = recipient._UID3;
            }
            newBan.time = minutes;
            newBan.sender = player._alias;
            newBan.reason = reason.ToString();

            player._server._db.send(newBan);
        }
*/
        /// <summary>
        /// Bans a player from a specific zone
        /// </summary>
        static public void ban(Player player, Player recipient, string payload, int bong)
        {
            //Sanity check
            if (player._server.IsStandalone)
            {
                player.sendMessage(-1, "Server is currently in stand-alone mode.");
                return;
            }

            if (recipient == null && payload == "")
            {
                player.sendMessage(-1, "Syntax: either ::*ban time:reason(optional) or *ban alias time:reason(optional)");
                return;
            }

            string[] param;
            string reason = "None given";
            string alias;
            int minutes = 0;
            int number;

            //Check to see if we are pm'ing someone
            if (recipient != null)
            {
                if ((int)player.PermissionLevel <= (int)recipient.PermissionLevel)
                {
                    player.sendMessage(-1, "You can't ban someone equal or higher than you.");
                    return;
                }

                if (payload == "")
                {
                    player.sendMessage(-1, "Syntax: ::*ban time:reason(Optional)");
                    return;
                }

                alias = recipient._alias.ToString();

                if (payload.Contains(':'))
                {
                    //Lets snag both the time and reason here
                    param = payload.Split(':');

                    if (!Int32.TryParse(param[0], out number))
                    {
                        player.sendMessage(-1, "Syntax: ::*ban time:reason(optional) Note: if using a reason, use : between time and reason");
                        return;
                    }

                    try
                    {
                        minutes = Convert.ToInt32(number);
                    }
                    catch (OverflowException)
                    {
                        player.sendMessage(-1, "That is not a valid time.");
                        return;
                    }

                    //Was a reason used?
                    if (param[1] != null || param[1] != "")
                        reason = param[1];
                }
                else //Just check for a time
                {
                    if (!Int32.TryParse(payload, out number))
                    {
                        player.sendMessage(-1, "That is not a valid time. Syntax ::*ban time:reason(Optional)");
                        return;
                    }

                    try
                    {
                        minutes = Convert.ToInt32(number);
                    }
                    catch (OverflowException)
                    {
                        player.sendMessage(-1, "That is not a valid time.");
                        return;
                    }
                }

                //Let the person know
                if (minutes > 0)
                    recipient.sendMessage(0, String.Format("You are being banned for {0} minutes.", minutes));
            }
            else //Using an alias instead
            {
                int listStart = -1, listEnd = -1;
                if (payload.Contains('"'))
                {
                    listStart = payload.IndexOf('"');
                    listEnd = payload.IndexOf('"', listStart + 1);
                    if (listStart != -1 && listEnd != -1)
                        alias = payload.Substring(listStart + 1, (listEnd - listStart) - 1);
                    else
                    {
                        player.sendMessage(-1, "Bad Syntax, use *ban \"alias\" time:reason(optional)");
                        return;
                    }
                }
                else
                {
                    player.sendMessage(-1, "Syntax: *ban \"alias\" time:reason(Optional) Note: Use quotations around the alias");
                    return;
                }

                //Must be a timed ban with a reason?
                if (payload.Contains(':'))
                {
                    param = payload.Split(':');
                    string[] pick = param.ElementAt(0).Split(' ');
                    try
                    {
                        minutes = Convert.ToInt32(pick.ElementAt(1)); //Without element1, it displays everything before ':'
                    }
                    catch
                    {
                        player.sendMessage(-1, "That is not a valid time.");
                        return;
                    }

                    //Check if there is a reason
                    if (param[1] != null && param[1] != "")
                        reason = param[1];
                }
                else //Just a timed ban
                {
                    string time;
                    listStart = listEnd;
                    if (listStart != -1)
                    {
                        time = payload.Substring(listStart + 1);
                        if (time.Contains(' '))
                            time = payload.Substring(listStart + 2);
                    }
                    else
                    {
                        player.sendMessage(-1, "Bad Syntax, use *ban \"alias\" time:reason(optional)");
                        return;
                    }

                    try
                    {
                        minutes = Convert.ToInt32(time);
                    }
                    catch
                    {
                        player.sendMessage(-1, "That is not a valid time.");
                        return;
                    }
                }
            }

            //KILL HIM!! Note: putting it here because the person never see's the message before being dc'd
            if ((recipient = player._arena.getPlayerByName(alias)) != null)
                recipient.disconnect();

            player.sendMessage(0, String.Format("You are banning {0} for {1} minutes.", alias, minutes));

            //Relay it to the database
            CS_Ban<Data.Database> newBan = new CS_Ban<Data.Database>();
            newBan.banType = CS_Ban<Data.Database>.BanType.account;
            newBan.alias = alias;
            if (recipient != null)
            {
                newBan.UID1 = recipient._UID1;
                newBan.UID2 = recipient._UID2;
                newBan.UID3 = recipient._UID3;
            }
            newBan.time = minutes;
            newBan.sender = player._alias;
            newBan.reason = reason.ToString();

            player._server._db.send(newBan);
        }

        /// <summary>
        /// Bans a player from a specific zone
        /// </summary>
        static public void block(Player player, Player recipient, string payload, int bong)
        {
            //Sanity check
            if (player._server.IsStandalone)
            {
                player.sendMessage(-1, "Server is currently in stand-alone mode.");
                return;
            }

            if (recipient == null && payload == "")
            {
                player.sendMessage(-1, "Syntax: either ::*block time:reason(optional) or *block alias time:reason(optional)");
                player.sendMessage(-1, "Note: make sure you are in the zone you want to ban from");
                return;
            }

            string[] param;
            string reason = "None given";
            string alias;
            int minutes = 0;
            int number;

            //Check to see if we are pm'ing someone
            if (recipient != null)
            {
                if ((int)player.PermissionLevel <= (int)recipient.PermissionLevel)
                {
                    player.sendMessage(-1, "You can't ban someone equal or higher than you.");
                    return;
                }

                if (payload == "")
                {
                    player.sendMessage(-1, "Syntax: ::*block time:reason(Optional)");
                    return;
                }

                alias = recipient._alias.ToString();

                if (payload.Contains(':'))
                {
                    //Lets snag both the time and reason here
                    param = payload.Split(':');

                    if (!Int32.TryParse(param[0], out number))
                    {
                        player.sendMessage(-1, "Syntax: ::*block time:reason(optional) Note: if using a reason, use : between time and reason");
                        return;
                    }

                    try
                    {
                        minutes = Convert.ToInt32(number);
                    }
                    catch (OverflowException)
                    {
                        player.sendMessage(-1, "That is not a valid time.");
                        return;
                    }

                    //Was a reason used?
                    if (param[1] != null || param[1] != "")
                        reason = param[1];
                }
                else //Just check for a time
                {
                    if (!Int32.TryParse(payload, out number))
                    {
                        player.sendMessage(-1, "That is not a valid time. Syntax ::*block time:reason(Optional)");
                        return;
                    }

                    try
                    {
                        minutes = Convert.ToInt32(number);
                    }
                    catch (OverflowException)
                    {
                        player.sendMessage(-1, "That is not a valid time.");
                        return;
                    }
                }

                //Let the person know
                if (minutes > 0)
                    recipient.sendMessage(0, String.Format("You are being banned for {0} minutes.", minutes));
            }
            else //Using an alias instead
            {
                int listStart = -1, listEnd = -1;
                if (payload.Contains('"'))
                {
                    listStart = payload.IndexOf('"');
                    listEnd = payload.IndexOf('"', listStart + 1);
                    if (listStart != -1 && listEnd != -1)
                        alias = payload.Substring(listStart + 1, (listEnd - listStart) - 1);
                    else
                    {
                        player.sendMessage(-1, "Bad Syntax, use *block \"alias\" time:reason(optional)");
                        return;
                    }
                }
                else
                {
                    player.sendMessage(-1, "Syntax: *block \"alias\" time:reason(Optional) Note: Use quotations around the alias");
                    return;
                }

                //Must be a timed ban with a reason?
                if (payload.Contains(':'))
                {
                    param = payload.Split(':');
                    string[] pick = param.ElementAt(0).Split(' ');
                    try
                    {
                        minutes = Convert.ToInt32(pick.ElementAt(1)); //Without element1, it displays everything before ':'
                    }
                    catch
                    {
                        player.sendMessage(-1, "That is not a valid time.");
                        return;
                    }

                    //Check if there is a reason
                    if (param[1] != null && param[1] != "")
                        reason = param[1];
                }
                else //Just a timed ban
                {
                    string time;
                    listStart = listEnd;
                    if (listStart != -1)
                    {
                        time = payload.Substring(listStart + 1);
                        if (time.Contains(' '))
                            time = payload.Substring(listStart + 2);
                    }
                    else
                    {
                        player.sendMessage(-1, "Bad Syntax, use *block \"alias\" time:reason(optional)");
                        return;
                    }

                    try
                    {
                        minutes = Convert.ToInt32(time);
                    }
                    catch
                    {
                        player.sendMessage(-1, "That is not a valid time.");
                        return;
                    }
                }
            }

            //KILL HIM!! Note: putting it here because the person never see's the message before being dc'd
            if ((recipient = player._arena.getPlayerByName(alias)) != null)
                recipient.disconnect();

            player.sendMessage(0, String.Format("You are banning {0} for {1} minutes.", alias, minutes));

            //Relay it to the database
            CS_Ban<Data.Database> newBan = new CS_Ban<Data.Database>();
            newBan.banType = CS_Ban<Data.Database>.BanType.zone;
            newBan.alias = alias;
            if (recipient != null)
            {
                newBan.UID1 = recipient._UID1;
                newBan.UID2 = recipient._UID2;
                newBan.UID3 = recipient._UID3;
            }
            newBan.time = minutes;
            newBan.sender = player._alias;
            newBan.reason = reason.ToString();

            player._server._db.send(newBan);
        }

        /// <summary>
        /// Force dc's a player from the server
        /// </summary>
        static public void dc(Player player, Player recipient, string payload, int bong)
        {
            //Check if they are granted and in a private arena
            if (player.PermissionLevelLocal == Data.PlayerPermission.ArenaMod && !player._arena.IsPrivate)
            {
                player.sendMessage(-1, "Nice try.");
                return;
            }

            //Kill all?
            if (payload != null && payload.Equals("all", StringComparison.CurrentCultureIgnoreCase))
            {
                foreach (Player p in player._arena.Players)
                    if (p != player)
                        p.disconnect();
            }
            else
            {
                if (recipient != null && (int)player.PermissionLevel >= (int)recipient.PermissionLevel)
                    //Destroy him!
                    recipient.disconnect();
                else
                    player.sendMessage(-1, "Syntax: ::*kill reason (optional) or *kill all");
            }
        }

        /// <summary>
        /// Bans a player from a specific zone
        /// </summary>
        static public void ipban(Player player, Player recipient, string payload, int bong)
        {
            //Sanity check
            if (player._server.IsStandalone)
            {
                player.sendMessage(-1, "Server is currently in stand-alone mode.");
                return;
            }

            if (recipient == null && payload == "")
            {
                player.sendMessage(-1, "Syntax: either ::*ipban time:reason(optional) or *ipban alias time:reason(optional)");
                return;
            }

            string[] param;
            string reason = "None given";
            string alias;
            int minutes = 0;
            int number;

            //Check to see if we are pm'ing someone
            if (recipient != null)
            {
                if ((int)player.PermissionLevel <= (int)recipient.PermissionLevel)
                {
                    player.sendMessage(-1, "You can't ban someone equal or higher than you.");
                    return;
                }

                if (payload == "")
                {
                    player.sendMessage(-1, "Syntax: ::*ipban time:reason(Optional)");
                    return;
                }

                alias = recipient._alias.ToString();

                if (payload.Contains(':'))
                {
                    //Lets snag both the time and reason here
                    param = payload.Split(':');

                    if (!Int32.TryParse(param[0], out number))
                    {
                        player.sendMessage(-1, "Syntax: ::*ipban time:reason(optional) Note: if using a reason, use : between time and reason");
                        return;
                    }

                    try
                    {
                        minutes = Convert.ToInt32(number);
                    }
                    catch (OverflowException)
                    {
                        player.sendMessage(-1, "That is not a valid time.");
                        return;
                    }

                    //Was a reason used?
                    if (param[1] != null || param[1] != "")
                        reason = param[1];
                }
                else //Just check for a time
                {
                    if (!Int32.TryParse(payload, out number))
                    {
                        player.sendMessage(-1, "That is not a valid time. Syntax ::*ipban time:reason(Optional)");
                        return;
                    }

                    try
                    {
                        minutes = Convert.ToInt32(number);
                    }
                    catch (OverflowException)
                    {
                        player.sendMessage(-1, "That is not a valid time.");
                        return;
                    }
                }

                //Let the person know
                if (minutes > 0)
                    recipient.sendMessage(0, String.Format("You are being banned for {0} minutes.", minutes));
            }
            else //Using an alias instead
            {
                int listStart = -1, listEnd = -1;
                if (payload.Contains('"'))
                {
                    listStart = payload.IndexOf('"');
                    listEnd = payload.IndexOf('"', listStart + 1);
                    if (listStart != -1 && listEnd != -1)
                        alias = payload.Substring(listStart + 1, (listEnd - listStart) - 1);
                    else
                    {
                        player.sendMessage(-1, "Bad Syntax, use *ipban \"alias\" time:reason(optional)");
                        return;
                    }
                }
                else
                {
                    player.sendMessage(-1, "Syntax: *ipban \"alias\" time:reason(Optional) Note: Use quotations around the alias");
                    return;
                }

                //Must be a timed ban with a reason?
                if (payload.Contains(':'))
                {
                    param = payload.Split(':');
                    string[] pick = param.ElementAt(0).Split(' ');
                    try
                    {
                        minutes = Convert.ToInt32(pick.ElementAt(1)); //Without element1, it displays everything before ':'
                    }
                    catch
                    {
                        player.sendMessage(-1, "That is not a valid time.");
                        return;
                    }

                    //Check if there is a reason
                    if (param[1] != null && param[1] != "")
                        reason = param[1];
                }
                else //Just a timed ban
                {
                    string time;
                    listStart = listEnd;
                    if (listStart != -1)
                    {
                        time = payload.Substring(listStart + 1);
                        if (time.Contains(' '))
                            time = payload.Substring(listStart + 2);
                    }
                    else
                    {
                        player.sendMessage(-1, "Bad Syntax, use *ipban \"alias\" time:reason(optional)");
                        return;
                    }

                    try
                    {
                        minutes = Convert.ToInt32(time);
                    }
                    catch
                    {
                        player.sendMessage(-1, "That is not a valid time.");
                        return;
                    }
                }
            }

            //KILL HIM!! Note: putting it here because the person never see's the message before being dc'd
            if ((recipient = player._arena.getPlayerByName(alias)) != null)
                recipient.disconnect();

            player.sendMessage(0, String.Format("You are banning {0} for {1} minutes.", alias, minutes));

            //Relay it to the database
            CS_Ban<Data.Database> newBan = new CS_Ban<Data.Database>();
            newBan.banType = CS_Ban<Data.Database>.BanType.ip;
            newBan.alias = alias;
            if (recipient != null)
            {
                newBan.UID1 = recipient._UID1;
                newBan.UID2 = recipient._UID2;
                newBan.UID3 = recipient._UID3;
            }
            newBan.time = minutes;
            newBan.sender = player._alias;
            newBan.reason = reason.ToString();

            player._server._db.send(newBan);
        }

        /// <summary>
        /// Removes a player from the entire server
        /// </summary>
        static public void gkill(Player player, Player recipient, string payload, int bong)
        {
            //SqlConnection db;
            //string pAccount = "";
            if (player._server.IsStandalone)
            {
                player.sendMessage(-1, "Server is currently in stand-alone mode.");
                return;
            }

            if (payload == "" && recipient == null)
            {
                player.sendMessage(-1, "Syntax: Either PM the person with *gkill time:reason(Optional) or *gkill alias time:reason(Optional)");
                return;
            }

            string[] param;
            string reason = "None given";
            string alias;
            int minutes = 0;
            int number;

            //Check to see if we are pm'ing someone
            if (recipient != null)
            {
                if ((int)player.PermissionLevel <= (int)recipient.PermissionLevel)
                {
                    player.sendMessage(-1, "You can't ban someone equal or higher than you.");
                    return;
                }

                if (payload == "")
                {
                    player.sendMessage(-1, "Syntax: ::*gkill time:reason(Optional)");
                    return;
                }

                alias = recipient._alias.ToString();

                if (payload.Contains(':'))
                {
                    //Lets snag both the time and reason here
                    param = payload.Split(':');

                    if (!Int32.TryParse(param[0], out number))
                    {
                        player.sendMessage(-1, "Syntax: ::*gkill time:reason(optional) Note: if using a reason, use : between time and reason");
                        return;
                    }

                    try
                    {
                        minutes = Convert.ToInt32(number);
                    }
                    catch (OverflowException)
                    {
                        player.sendMessage(-1, "That is not a valid time.");
                        return;
                    }

                    //Was a reason used?
                    if (param[1] != null || param[1] != "")
                        reason = param[1];
                }
                else //Just check for a time
                {
                    if (!Int32.TryParse(payload, out number))
                    {
                        player.sendMessage(-1, "That is not a valid time. Syntax ::*gkill time:reason(Optional)");
                        return;
                    }

                    try
                    {
                        minutes = Convert.ToInt32(number);
                    }
                    catch (OverflowException)
                    {
                        player.sendMessage(-1, "That is not a valid time.");
                        return;
                    }
                }

                //Let the person know
                if (minutes > 0)
                    recipient.sendMessage(0, String.Format("You are being banned for {0} minutes.", minutes));
            }
            else //Using an alias instead
            {
                int listStart = -1, listEnd = -1;
                if (payload.Contains('"'))
                {
                    listStart = payload.IndexOf('"');
                    listEnd = payload.IndexOf('"', listStart + 1);
                    if (listStart != -1 && listEnd != -1)
                        alias = payload.Substring(listStart + 1, (listEnd - listStart) - 1);
                    else
                    {
                        player.sendMessage(-1, "Bad Syntax, use *gkill \"alias\" time:reason(optional)");
                        return;
                    }
                }
                else
                {
                    player.sendMessage(-1, "Syntax: *gkill \"alias\" time:reason(Optional) Note: Use quotations around the alias");
                    return;
                }

                //Must be a timed ban with a reason?
                if (payload.Contains(':'))
                {
                    param = payload.Split(':');
                    string[] pick = param.ElementAt(0).Split(' ');
                    try
                    {
                        minutes = Convert.ToInt32(pick.ElementAt(1)); //Without element1, it displays everything before ':'
                    }
                    catch
                    {
                        player.sendMessage(-1, "That is not a valid time.");
                        return;
                    }

                    //Check if there is a reason
                    if (param[1] != null && param[1] != "")
                        reason = param[1];
                }
                else //Just a timed ban
                {
                    string time;
                    listStart = listEnd;
                    if (listStart != -1)
                    {
                        time = payload.Substring(listStart + 1);
                        if (time.Contains(' '))
                            time = payload.Substring(listStart + 2);
                    }
                    else
                    {
                        player.sendMessage(-1, "Bad Syntax, use *gkill \"alias\" time:reason(optional)");
                        return;
                    }

                    try
                    {
                        minutes = Convert.ToInt32(time);
                    }
                    catch
                    {
                        player.sendMessage(-1, "That is not a valid time.");
                        return;
                    }
                }
            }

            //KILL HIM!! Note: putting it here because the person never see's the message before being dc'd
            if ((recipient = player._arena.getPlayerByName(alias)) != null)
                recipient.disconnect();

            player.sendMessage(0, String.Format("You are banning {0} for {1} minutes.", alias, minutes));

            //Relay it to the database
            CS_Ban<Data.Database> newBan = new CS_Ban<Data.Database>();
            newBan.banType = CS_Ban<Data.Database>.BanType.global;
            newBan.alias = alias;
            if (recipient != null)
            {
                newBan.UID1 = recipient._UID1;
                newBan.UID2 = recipient._UID2;
                newBan.UID3 = recipient._UID3;
            }
            newBan.time = minutes;
            newBan.sender = player._alias;
            newBan.reason = reason.ToString();

            player._server._db.send(newBan);
        }

            //Check if they are already banned
            /*
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
            */

        /// <summary>
        /// Bans or just kicks the player from a specific arena - if granted players, privately owned only
        /// </summary>
        static public void kick(Player player, Player recipient, string payload, int bong)
        {
            //Sanity check
            if (player._server.IsStandalone)
            {
                player.sendMessage(0, "Server is currently in stand-alone mode.");
                return;
            }

            //Get players mod level first
            int level;
            if (player.PermissionLevelLocal == Data.PlayerPermission.ArenaMod)
                level = (int)player.PermissionLevelLocal;
            else
                level = (int)player.PermissionLevel;

            int rLevel = (int)recipient.PermissionLevel;

            //Check if they are granted and in a private arena
            if (level == (int)Data.PlayerPermission.ArenaMod && !player._arena.IsPrivate)
            {
                player.sendMessage(-1, "Nice try.");
                return;
            }

            if (level == (int)Data.PlayerPermission.Developer && player._arena._name.StartsWith("Public", StringComparison.OrdinalIgnoreCase))
            {
                player.sendMessage(-1, "You can only use it in non-public arena's.");
                return;
            }

            if (((rLevel == (int)Data.PlayerPermission.Developer) && (level < (int)Data.PlayerPermission.Manager)) || level <= rLevel)
            {
                player.sendMessage(-1, "No.");
                return;
            }

            if (recipient != null && payload == "")
            {
                //Assume they just want to kick them out of the arena temporarily
                recipient.sendMessage(-1, "You have been kicked out of the arena.");
                recipient.disconnect();
                player.sendMessage(0, String.Format("You have kicked player {0} from the arena.", recipient._alias.ToString()));
                return;
            }

            if (recipient == null && payload == "")
            {
                player.sendMessage(-1, "Syntax: ::*kick time(Optional) OR *kick alias time(Optional)");
                return;
            }

            string alias;
            int minutes = 0;
            string[] param = payload.Split(' ');

            //Lets grab the players name
            if (recipient != null)
            {
                alias = recipient._alias.ToString();
                //Lets get the time too
                try
                {
                    minutes = Convert.ToInt32(payload);
                }
                catch (OverflowException) //We are only catching overflow, null can be 0, we can kick without a time limit 
                {
                    player.sendMessage(-1, "That is not a valid time.");
                    return;
                }
            }
            else
            {
                alias = param.ElementAt(0);

                //Lets check to see if they just used a timer without a name
                if ((recipient = player._arena.getPlayerByName(alias)) == null)
                {
                    player.sendMessage(-1, "That player isn't here.");
                    return;
                }
                alias = recipient._alias.ToString();

                //Lets check for time now
                try
                {
                    minutes = Convert.ToInt32(param.ElementAt(1).Split(' ').ElementAt(1));
                }
                catch (OverflowException) //We are only catching overflow, null = 0 which we can kick without a time limit 
                {
                    player.sendMessage(-1, "That is not a valid time.");
                    return;
                }
            }
            //Remove his privileges
            if (player._arena._owner != null && player._arena._owner.Count > 0)
            {
                foreach (var p in player._arena._owner)
                    if (recipient._alias.Equals(p))
                    {
                        player._arena._owner.Remove(p);
                        break;
                    }
            }
            recipient.sendMessage(-1, String.Format("You have been kicked from the arena{0}", (minutes > 0) ? String.Format(" for {0} minutes.", minutes) : "."));
            recipient._server._arenaBans[recipient._arena._name].Add(recipient._alias, DateTime.Now.AddMinutes(minutes));
            recipient.disconnect();

            player.sendMessage(0, String.Format("You have kicked player {0}{1}", recipient._alias, (minutes > 0) ? String.Format(" for {0} minutes.", minutes) : "."));
        }

        /// <summary>
        /// Registers all handlers
        /// </summary>
        [Commands.RegistryFunc(HandlerType.ModCommand)]
        static public IEnumerable<Commands.HandlerDescriptor> Register()
        {
            yield return new HandlerDescriptor(help, "help",
                "Gives the user help information on a given command.",
                "*help [commandName]",
                InfServer.Data.PlayerPermission.ArenaMod,true);

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

            yield return new HandlerDescriptor(ban, "ban",
                "Bans a player from all zones",
                "*ban alias minutes:reason(optional) or :player:*ban minutes:reason(optional)",
                InfServer.Data.PlayerPermission.SMod, false);

            yield return new HandlerDescriptor(block, "block",
                "Blocks a player from a specific zone - Note: make sure you are in the zone you want to ban from",
                "*block alias minutes:reason(optional) or :player:*block minutes:reason(optional)",
                InfServer.Data.PlayerPermission.Mod, true);

            yield return new HandlerDescriptor(cash, "cash",
                "Prizes specified amount of cash to target player",
                "*cash [amount] or ::*cash [amount]",
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(dc, "dc",
                "Kicks a player from the server",
                "*dc reason (optional) or dc all",
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(experience, "experience",
                "Prizes specified amount of experience to target player",
                "*experience [amount] or ::*experience [amount]",
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(getball, "getball",
                "Gets a ball.","*getball",
                InfServer.Data.PlayerPermission.ArenaMod, true);           

            yield return new HandlerDescriptor(gkill, "gkill",
                "Globally bans a player from the entire server",
                ":player:*gkill minutes:reason(Optional) or *gkill alias minutes:reason(Optional)",
                InfServer.Data.PlayerPermission.Sysop, false);

            yield return new HandlerDescriptor(global, "global",
               "Sends a global message to every zone connected to current database",
               "*global [message]",
               InfServer.Data.PlayerPermission.Mod, false);

            yield return new HandlerDescriptor(warp, "goto",
                "Warps you to a specified player, coordinate or exact coordinate. Alternatively, you can warp other players to coordinates or exacts.",
                ":alias:*goto or *goto alias or *goto A4 or *goto 123,123",
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(grant, "grant",
                "Gives arena privileges to a player",
                ":player:*grant or *grant alias",
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(ipban, "ipban",
                "IPBans a player from all zones",
                "*ipban alias minutes:reason(optional) or :player:*ipban minutes:reason(optional)",
                InfServer.Data.PlayerPermission.SMod, false);

            yield return new HandlerDescriptor(helpcall, "helpcall",
                "helpcall shows all helpcalls from all zones",
                "*helpcall  OR *helpcall page - to show a specific page",
                InfServer.Data.PlayerPermission.Mod, false);

            yield return new HandlerDescriptor(kick, "kick",
                "Kicks and can also ban a player from an arena",
                ":player:*kick minutes(Optional) or *kick alias minutes(optional)",
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(block, "kill",
                "Blocks a player from a specific zone - Note: make sure you are in the zone you want to ban from",
                "*kill alias minutes:reason(optional) or :player:*kill minutes:reason(optional)",
               InfServer.Data.PlayerPermission.Mod, true);

            yield return new HandlerDescriptor(speclock, "lock",
                "*lock will toggle arena lock on or off, using lock in a pm will lock and spec or unlock a player in spec",
                "*lock OR :target:*lock",
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(permit, "permit",
                "Permits target player to enter a permission-only zone.",
                "*permit alias",
               InfServer.Data.PlayerPermission.Mod, true);

            yield return new HandlerDescriptor(poll, "poll",
                "Starts a poll for a question asked, *poll cancel to stop one, *poll end to end one",
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
               ":alias:*shutup",
               InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(spec, "spec",
                "Puts a player into spectator mode, optionally on a specified team.",
                "*spec or ::*spec or ::*spec [team]",
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(spectate, "spectate",
                "Forces a player or the whole arena to spectate the specified player.",
                "Syntax: ::*spectate [player] or *spectate [player]",
                InfServer.Data.PlayerPermission.Sysop, false);

            yield return new HandlerDescriptor(specquiet, "specquiet",
                "Toggles chatting to spectator only.",
                "Syntax: :alias:*specquiet (for a specific player) or *specquiet (toggles the whole arena)",
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(stealth, "stealth",
                "Toggles stealth mode, mods become invisible to arena's",
                "*stealth",
                InfServer.Data.PlayerPermission.Mod, false);

            yield return new HandlerDescriptor(summon, "summon",
                "Summons a specified player to your location, or all players to your location.",
                "::*summon or *summon al",
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(team, "team",
                "Puts another player, or yourself, on a specified team",
                "*team [teamname] or ::*team [teamname]",
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(teamname, "teamname",
                "Renames public team names in the arena",
                "*teamname [teamname1,teamname2,...]",
                InfServer.Data.PlayerPermission.ArenaMod, true);

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

            yield return new HandlerDescriptor(watchmod, "watchmod",
                "Toggles viewing mod commands on or off",
                "*watchmod",
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(unspec, "unspec",
                "Takes a player out of spectator mode and puts him on the specified team.",
                "*unspec [team] or ::*unspec [team] or ::*unspec ..",
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(wipe, "wipe",
                "Wipes a character within the current zone",
                "::*wipe",
                InfServer.Data.PlayerPermission.Mod, false);
        }
    }
}