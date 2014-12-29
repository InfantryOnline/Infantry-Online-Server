using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
        /// <summary>
        /// Adds a soccerball on the field
        /// </summary>
        static public void addball(Player player, Player recipient, string payload, int bong)
        {
            if (player._arena.Balls.Count() >= Arena.maxBalls)
            {
                player.sendMessage(-1, "All the balls are currently in the game.");
                return;
            }

            //Lets create a new ball!
            int ballID = player._arena.Balls.Count(); //We use count because ball id's start at 0 so count would be always + 1
            Ball ball = player._arena.newBall((short)ballID);
            if (ball != null)
            {
                //Send it
                Ball.Spawn_Ball(null, ball);

                player._arena.sendArenaMessage("Ball Added.", player._arena._server._zoneConfig.soccer.ballAddedBong);
            }
        }

        /// <summary>
        /// Grabs a ball on the field and gives it to the player
        /// </summary>
        static public void getball(Player player, Player recipient, string payload, int bong)
        {
            Player target = recipient != null ? recipient : player;
            if (target.IsSpectator || target._team.IsSpec)
            {
                player.sendMessage(-1, "You cannot use getball on a spectator.");
                return;
            }

            ushort askedBallId; // Id of the requested getball
            if (String.IsNullOrEmpty(payload))
                askedBallId = 0;
            else
            {
                ushort requestBallID = Convert.ToUInt16(payload);
                if (requestBallID > Arena.maxBalls || requestBallID < 0) //5 is max on the field (0 id = 1 ball, 3 id = 4 balls)
                {
                    player.sendMessage(-1, "Invalid getball Ball ID");
                    return;
                }
                askedBallId = requestBallID;
            }

            //Get the ball in question..
            Ball ball = player._arena.Balls.SingleOrDefault(b => b._id == askedBallId);
            if (ball == null)
            {
                player.sendMessage(-1, "Ball does not exist.");
                return;
            }

            if (ball._lastOwner != null && (ushort)ball._lastOwner._gotBallID == ball._id)
                ball._lastOwner._gotBallID = 999;

            //Assign the ball to the player
            ball._lastOwner = ball._owner != null ? ball._owner : null;
            ball._owner = target;

            target._gotBallID = ball._id;

            //Assign default state
            ball._state.positionX = target._state.positionX;
            ball._state.positionY = target._state.positionY;
            ball._state.positionZ = target._state.positionZ;
            ball._state.velocityX = 0;
            ball._state.velocityY = 0;
            ball._state.velocityZ = 0;
            ball.ballStatus = 0;
            ball.deadBall = false;

            //Update data
            ball._arena.UpdateBall(ball);

            //Make each player aware of the ball
            Ball.Route_Ball(player._arena.Players, ball);
        }

        /// <summary>
        /// Removes a ball from the arena
        /// </summary>
        static public void removeball(Player player, Player recipient, string payload, int bong)
        {
            ushort askedBallId; // Id of the requested getball
            if (String.IsNullOrEmpty(payload))
                askedBallId = 0;
            else
            {
                ushort requestBallID = Convert.ToUInt16(payload);
                if (requestBallID > Arena.maxBalls || requestBallID < 0)
                {
                    player.sendMessage(-1, "Invalid getball Ball ID");
                    return;
                }
                askedBallId = requestBallID;
            }

            //Get the ball in question..
            Ball ball = player._arena.Balls.SingleOrDefault(b => b._id == askedBallId);
            if (ball == null)
            {
                player.sendMessage(-1, "Ball does not exist.");
                return;
            }

            //Update handlers
            if (ball._lastOwner != null && (ushort)ball._lastOwner._gotBallID == ball._id)
                ball._lastOwner._gotBallID = 999;

            if (ball._owner != null && (ushort)ball._owner._gotBallID == ball._id)
                ball._owner._gotBallID = 999;

            ball._owner = null;
            ball._lastOwner = null;

            //Update clients
            Ball.Remove_Ball(ball);

            //Let players know
            player._arena.sendArenaMessage("Ball removed.");
        }

        /// <summary>
        /// Allows a player to join a private arena
        /// </summary>
        static public void allow(Player player, Player recipient, string payload, int bong)
        {
            if (String.IsNullOrEmpty(payload))
            {
                player.sendMessage(-1, "Syntax: *allow alias OR *allow list to see who's allowed");
                player.sendMessage(0, "Notes: make sure you are in the arena you want and type the alias correctly(Case doesn't matter)");
                return;
            }

            if (player._arena._bIsPublic)
            {
                player.sendMessage(-1, "Command is not allowed for public named arenas.");
                return;
            }

            if (!player._arena.IsPrivate && player._developer)
            {
                player.sendMessage(-1, "Only mods are allowed to lock non-private arenas.");
                return;
            }

            string toLower = payload.ToLower();
            //Does he want a list?
            if (toLower == "list")
            {
                player.sendMessage(0, "Current List of Allowed Players:");
                if (player._arena._bAllowed.Count > 0)
                {
                    List<string> ordered = player._arena._bAllowed.ToList();
                    ordered.Sort();
                    foreach (string str in ordered)
                        player.sendMessage(0, str);
                }
                else
                    player.sendMessage(0, "Empty.");
                return;
            }

            //Are we adding or removing?
            if (player._arena._bAllowed.Count == 0)
            {
                //List is empty, just add the player
                player._arena._bAllowed.Add(toLower);
                player.sendMessage(0, "Player " + payload + " added.");
                if (!player._arena._aLocked)
                    player.sendMessage(-1, "Note: Arena lock(locks arena from outsiders) is not on. To turn it on type *arenalock.");
                return;
            }

            if (player._arena._bAllowed.Contains(toLower))
            {   //Remove
                player._arena._bAllowed.Remove(toLower);
                player.sendMessage(-3, "Player removed from the permission list.");
            }
            else
            {   //Adding
                player.sendMessage(-3, "Player added to permission list");
                player._arena._bAllowed.Add(toLower);

                //Let the person know
                CS_Whisper<Data.Database> whisper = new CS_Whisper<Data.Database>();
                whisper.bong = 1;
                whisper.recipient = payload;
                whisper.message = (String.Format("You have been given permission to enter {0}.", player._arena._name));
                whisper.from = player._alias;

                player._server._db.send(whisper);
            }

            if (!player._arena._aLocked)
                player.sendMessage(-1, "Note: Arena lock is not on. To turn it on type *arenalock.");
        }

        /// <summary>
        /// Sends a arena/system message
        /// </summary>
        static public void arena(Player player, Player recipient, string payload, int bong)
        {
            if (String.IsNullOrEmpty(payload))
                player.sendMessage(-1, "Message can not be empty.");
            else
            {
                string format = String.Format("{0} - {1}", payload, player._alias);

                //Snap color code before sending
                Regex reg = new Regex(@"^(~|!|@|#|\$|%|\^|&|\*)", RegexOptions.IgnoreCase);
                Match match = reg.Match(payload);
                if (match.Success)
                    format = String.Format("{0}[Arena] {1}", match.Groups[0].Value, format.Remove(0, 1));
                else
                    format = String.Format("[Arena] {0}", format);
                //Send it
                player._arena.sendArenaMessage(format, bong);
            }
        }

        /// <summary>
        /// Locks an arena from outsiders joining it
        /// </summary>
        static public void arenalock(Player player, Player recipient, string payload, int bong)
        {
            if (player._arena._bIsPublic)
            {
                player.sendMessage(-1, "Command is not allowed for public named arenas.");
                return;
            }

            if (!player._arena.IsPrivate && player._developer)
            {
                player.sendMessage(-1, "Only mods are allowed to lock non-private arenas.");
                return;
            }

            player._arena._aLocked = !player._arena._aLocked;

            Arena arena = player._arena;
            if (arena._aLocked)
            {
                foreach (Player p in player._arena.Players.ToList())
                {
                    if (p == null || p == player)
                        continue;
                    if (!p.IsSpectator)
                        continue;
                    if (p._developer || p._admin || arena.IsGranted(p))
                        continue;
                    p.sendMessage(-1, "Disconnecting due to arena being locked.");
                    p.disconnect();
                }
            }
            player._arena.sendArenaMessage(String.Format("Arena is now {0}.", player._arena._aLocked ? "locked" : "unlocked"));
        }

        /// <summary>
        /// Log the player in. Stand-Alone Mode only.
        /// </summary>
        static public void auth(Player player, Player recipient, string payload, int bong)
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
                    player._permissionTemp = Data.PlayerPermission.Sysop;
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
        /// Finds an item within the zone
        /// </summary>
        static public void findItem(Player player, Player recipient, string payload, int bong)
        {
            if (String.IsNullOrEmpty(payload))
            {
                player.sendMessage(-1, "Syntax: *finditem [itemID or item Name]");
                return;
            }

            if (Helpers.IsNumeric(payload))
            {
                int Itemid = Convert.ToInt32(payload);

                //Obtain the vehicle indicated
                Assets.ItemInfo item = player._server._assets.getItemByID(Convert.ToInt32(Itemid));
                if (item == null)
                {
                    player.sendMessage(-1, "That item doesn't exist.");
                    return;
                }

                player.sendMessage(0, String.Format("[{0}] {1}", item.id, item.name));
            }
            else
            {
                List<Assets.ItemInfo> items = player._server._assets.getItems;
                if (items == null)
                {
                    player.sendMessage(-1, "That item doesn't exist.");
                    return;
                }

                int count = 0;
                payload = payload.ToLower();
                foreach (Assets.ItemInfo item in items)
                {
                    if (item.name.ToLower().Contains(payload))
                    {
                        player.sendMessage(0, String.Format("[{0}] {1}", item.id, item.name));
                        count++;
                    }
                }
                if (count == 0)
                    player.sendMessage(-1, "That item doesn't exist.");
            }
        }

        /// <summary>
        /// Sends a global message to every zone connected to current database
        /// </summary>
        static public void global(Player player, Player recipient, string payload, int bong)
        {   //Sanity checks
            if (String.IsNullOrEmpty(payload))
            {
                player.sendMessage(-1, "And say what?");
                return;
            }
            
            if (player._server.IsStandalone)
            {
                player.sendMessage(-1, "Server is currently in stand-alone mode.");
                return;
            }

            string format = String.Format("{0} - {1}", payload, player._alias);
            //Snap the color code before sending
            Regex reg = new Regex(@"^(~|!|@|#|\$|%|\^|&|\*)", RegexOptions.IgnoreCase);
            Match match = reg.Match(payload);
            if (match.Success)
                format = String.Format("{0}[Global] {1}", match.Groups[0].Value, format.Remove(0, 1));
            else
                format = String.Format("[Global] {0}", format);

            CS_ChatQuery<Data.Database> pkt = new CS_ChatQuery<Data.Database>();
            pkt.queryType = CS_ChatQuery<Data.Database>.QueryType.global;
            pkt.sender = player._alias;
            pkt.payload = format;

            player._server._db.send(pkt);
        }

        /// <summary>
        /// Grants a player private arena privileges
        /// </summary>
        static public void grant(Player player, Player recipient, string payload, int bong)
        {
            //Sanity checks
            if (!player._arena.IsPrivate && player.PermissionLevel < Data.PlayerPermission.Mod)
            {
                player.sendMessage(-1, "This is not a private arena.");
                return;
            }

            if (!player._arena.IsGranted(player) && player.PermissionLevelLocal < Data.PlayerPermission.ArenaMod)
            {
                player.sendMessage(-1, "You are not granted in this arena.");
                return;
            }

            if (recipient != null)
            {
                if (recipient.PermissionLevel >= Data.PlayerPermission.ArenaMod)
                {
                    player.sendMessage(-1, "That person doesn't need grant.");
                    return;
                }

                if (player._arena.IsGranted(recipient))
                {
                    if (recipient.PermissionLevel >= Data.PlayerPermission.ArenaMod)
                    {
                        player.sendMessage(-1, "You cannot remove this players power.");
                        return;
                    }

                    if (player._arena._owner.Contains(recipient._alias))
                        player._arena._owner.Remove(recipient._alias);

                    recipient._permissionTemp = Data.PlayerPermission.Normal;
                    recipient.sendMessage(0, "You have been removed from arena privileges.");
                    player.sendMessage(0, "You have removed " + recipient._alias + "'s arena privileges.");
                    return;
                }
                player._arena._owner.Add(recipient._alias);
                recipient._permissionTemp = Data.PlayerPermission.ArenaMod;
                recipient.sendMessage(0, "You are now granted arena privileges.");
            }
            else
            {
                if (String.IsNullOrEmpty(payload))
                {
                    player.sendMessage(-1, "Syntax: *grant alias");
                    return;
                }

                if ((recipient = player._arena.getPlayerByName(payload.ToLower() )) == null)
                {
                    player.sendMessage(-1, "That player doesn't exist.");
                    return;
                }

                if (recipient.PermissionLevel >= Data.PlayerPermission.ArenaMod)
                {
                    player.sendMessage(-1, "That person doesn't need grant.");
                    return;
                }

                if (player._arena.IsGranted(recipient))
                {
                    if (recipient.PermissionLevel >= Data.PlayerPermission.ArenaMod)
                    {
                        player.sendMessage(-1, "You cannot remove this players power.");
                        return;
                    }

                    if (player._arena._owner.Contains(recipient._alias))
                        player._arena._owner.Remove(recipient._alias);

                    recipient._permissionTemp = Data.PlayerPermission.Normal;
                    recipient.sendMessage(0, "You have been removed from arena privileges.");
                    player.sendMessage(0, "You have removed " + recipient._alias + "'s arena privileges.");
                    return;
                }
                player._arena._owner.Add(recipient._alias);
                recipient._permissionTemp = Data.PlayerPermission.ArenaMod;
                recipient.sendMessage(0, "You are now granted arena privileges.");
            }

            player.sendMessage(0, "You have given " + recipient._alias + " arena privileges.");
        }

        /// <summary>
        /// Gives the user help information on a given command
        /// </summary>
        static public void help(Player player, Player recipient, string payload, int bong)
        {
            if (String.IsNullOrEmpty(payload))
            {	//List all mod commands
                player.sendMessage(0, "&Commands available to you:");

                SortedList<string, int> commands = new SortedList<string, int>();
                int playerPermission = (int)player.PermissionLevelLocal;
                string[] powers = { "Basic", "ArenaMod", "Mod", "SMod", "Manager", "Head Mod/Sysop" };
                int level;

                //New help list, sorted by level
                if (player._developer || player._permissionTemp == Data.PlayerPermission.ArenaMod)
                {
                    //First set list
                    foreach (HandlerDescriptor cmd in player._arena._commandRegistrar._modCommands.Values)
                        if (cmd.isDevCommand)
                            commands.Add(cmd.handlerCommand, (int)cmd.permissionLevel);

                    //Now sort by level
                    for (level = 0; level <= (int)player.PermissionLevelLocal; level++)
                    {
                        player.sendMessage(0, String.Format("!{0}", powers[level]));
                        foreach (KeyValuePair<string, int> cmds in commands)
                            if (playerPermission >= cmds.Value && level == cmds.Value)
                                player.sendMessage(0, "**" + cmds.Key);
                    }
                }
                else
                {
                    //First set list
                    foreach (HandlerDescriptor cmd in player._arena._commandRegistrar._modCommands.Values)
                        commands.Add(cmd.handlerCommand, (int)cmd.permissionLevel);

                    //Now sort by level
                    for (level = 0; level <= (int)player.PermissionLevelLocal; level++)
                    {
                        player.sendMessage(0, String.Format("!{0}", powers[level]));
                        foreach (KeyValuePair<string, int> cmds in commands)
                            if (playerPermission >= cmds.Value && level == cmds.Value)
                                player.sendMessage(0, "**" + cmds.Key);
                    }
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
            if (String.IsNullOrEmpty(payload))
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
            CS_ChatQuery<Data.Database> pkt = new CS_ChatQuery<Data.Database>();
            pkt.queryType = CS_ChatQuery<Data.Database>.QueryType.helpcall;
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
            if (String.IsNullOrEmpty(payload))
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
				if (String.IsNullOrEmpty(payload))
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
                int index = player._arena.playtimeTickerIdx > 1 ? 1 : 2;
                player._arena.setTicker(1, index, timer * 100, result.ElementAt(0), delegate() { player._arena.pollQuestion(player._arena, true); });
                player._arena.sendArenaMessage(String.Format("&A Poll has been started by {0}." + " Topic: {1}", (player.IsStealth ? "Unknown" : player._alias), result.ElementAt(0)));
                player._arena.sendArenaMessage("&Type ?poll yes or ?poll no to participate");
            }
            else
            {
                player._arena.setTicker(1, 4, timer, payload); //Game ending will stop and display results
                player._arena.sendArenaMessage(String.Format("&A Poll has been started by {0}." + " Topic: {1}", (player.IsStealth ? "Unknown" : player._alias), payload));
            }

			player._arena._poll.start = true;
		}

        /// <summary>
        /// Spawns an item on the ground or in a player's inventory
        /// </summary>
        static public void prize(Player player, Player recipient, string payload, int bong)
        {	//Sanity checks
            if (String.IsNullOrEmpty(payload))
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

            //Determine the item and quantity
            string[] args = payload.Split(':');

            //Our handy string/int checker
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
            if (!String.IsNullOrEmpty(payload))
            {
                if ((target = player._arena.getPlayerByName(payload)) == null)
                {
                    player.sendMessage(-1, "That player isn't here.");
                    return;
                }
            }

            player.sendMessage(-3, "&Player Profile Information");
            player.sendMessage(0, "*" + target._alias);
            if (target == player)
                player.sendMessage(0, "&Permission Level: " + (int)player.PermissionLevel + (player._developer ? "(Dev)" : ""));
            else if (player.PermissionLevel >= target.PermissionLevel)
                player.sendMessage(0, "&Permission Level: " + (int)target.PermissionLevel + (target._developer ? "(Dev)" : ""));
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
                    player._arena._scramble = true;
                    player.sendMessage(0, "Scramble toggled ON.");
                }
                else if (payload.ToLower() == "off")
                {
                    player._arena._scramble = false;
                    player.sendMessage(0, "Scramble toggled OFF.");
                }
                else
                {
                    //Now scramble teams
                    numTeams = player._arena.ActiveTeams.Count();
                    IEnumerable<Team> active = player._arena.PublicTeams.Where(t => t.ActivePlayerCount > 0).ToList();
                    for (int i = 0; i < shuffledPlayers.Count; i++)
                    {
                        Team team = active.ElementAt(i % numTeams);
                        if (shuffledPlayers[i]._team != team)
                            team.addPlayer(shuffledPlayers[i]);
                    }

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

            if (player != recipient && (int)player.PermissionLevelLocal < (int)recipient.PermissionLevel)
            {
                player.sendMessage(-1, "Nice try.");
                return;
            }

            if (String.IsNullOrEmpty(payload))
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
        /// Gives a player a certain skill/attribute
        /// </summary>
        static public void skill(Player player, Player recipient, string payload, int bong)
        {
            //Sanity checks
            if (String.IsNullOrEmpty(payload) || recipient == null)
            {
                player.sendMessage(-1, "Syntax: :player:*skill id:amount");
                return;
            }

            int level;
            if (player.PermissionLevelLocal == Data.PlayerPermission.ArenaMod)
                level = (int)player.PermissionLevelLocal;
            else
                level = (int)player.PermissionLevel;

            if (!player._arena._name.StartsWith("Public", StringComparison.OrdinalIgnoreCase)
                && !player._arena.IsPrivate)
                if (level < (int)Data.PlayerPermission.Mod)
                {
                    player.sendMessage(-1, "You can only use it in private arena's.");
                    return;
                }

            if (!payload.Contains(':'))
            {
                player.sendMessage(-1, "Syntax error: :player:*skill id:amount");
                return;
            }

            //Determine the id and quantity
            string[] args = payload.Split(':');

            //Our handy string/int checker
            bool IsNumeric = Regex.IsMatch(args[0], @"^[-0-9]+$");

            SkillInfo skill;
            //Asking for a skill id?
            if (!IsNumeric)
                //Nope
                skill = player._server._assets.getSkillByName(args[0].Trim());
            else
                skill = player._server._assets.getSkillByID(Int32.Parse(args[0].Trim()));

            int quantity = (args.Length == 1) ? 1 : Convert.ToInt32(args[1].Trim());

            if (skill == null)
            {
                player.sendMessage(-1, "Unable to find specified skill.");
                return;
            }

            Player.SkillItem sk;
            int adjust = 0;
            if (recipient._skills.TryGetValue(skill.SkillId, out sk))
                adjust = player._server._zoneConfig.rpg.attributeBaseCost + skill.Price;

            recipient.skillModify(skill, quantity);
            //Give back their exp
            if (quantity < 0)
                recipient.Experience += (-adjust * quantity);
            else
                recipient.Experience -= (adjust * quantity);

            player.sendMessage(0, "The command has been completed.");
        }

        /// <summary>
        /// Spawns a flag to a specified position
        /// </summary>
        static public void spawnFlag(Player player, Player recipient, string payload, int bong)
        {
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
            if (!String.IsNullOrEmpty(payload))
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
            if (String.IsNullOrEmpty(payload))
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
            if (payload == "all" || (String.IsNullOrEmpty(payload) && recipient == null))
            {
                player._arena._bLocked = !player._arena._bLocked;
                player._arena.sendArenaMessage("Spec lock has been toggled" + (player._arena._bLocked ? " ON!" : " OFF!"));
                return;
            }

            //Just incase someone meant private locking
            if (payload.Equals("arena", StringComparison.OrdinalIgnoreCase))
            {   //Pass it along
                arenalock(player, recipient, payload, bong);
                return;
            }

            //Sanity checks
            if (recipient == null)
            {
                player.sendMessage(-1, "Syntax: ::*lock or *lock");
                return;
            }

            if (player != recipient && (int)player.PermissionLevelLocal < (int)recipient.PermissionLevel)
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
            if (player._developer || player._permissionTemp >= Data.PlayerPermission.ArenaMod)
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
                {
                    if (person == player)
                        continue;
                    //Their level is greater, allow them to see him/her
                    if (player.PermissionLevel > person.PermissionLevel)
                    {
                        Helpers.Object_PlayerLeave(person, player);
                        //Add here to spoof leave chat
                    }
                }
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
            if (recipient == null && String.IsNullOrWhiteSpace(payload))
            {
                player.sendMessage(-1, "Syntax: ::*summon, OR *summon all, OR *summon alias, OR *summon team teamname");
                return;
            }

            if (recipient != null)
            {
                //Simply warp the recipient
                recipient.warp(player);
                return;
            }

            if (payload.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                //summon everybody
                foreach (Player p in player._arena.PlayersIngame.ToList())
                    if (p != player)
                        p.warp(player);
                return;
            }

            if (payload.Contains("team"))
            {
                string[] split = payload.Split(' ');
                string teamname = "";
                if (split[0].Equals("team") && (split.Count() == 1 || String.IsNullOrWhiteSpace(split[1])))
                {
                    player.sendMessage(-1, "Syntax: *summon team teamname");
                    return;
                }

                //Strip the word team out
                teamname = payload.Remove(payload.IndexOf("team", StringComparison.OrdinalIgnoreCase), 5); //5 = team + a space
                Team team = player._arena.getTeamByName(teamname);
                if (team == null)
                {
                    player.sendMessage(-1, "That team doesn't exist.");
                    return;
                }

                if (team._name.Equals("spec") || team._name.Equals("spectator"))
                {
                    player.sendMessage(-1, "Cannot summon players on a spectating team.");
                    return;
                }

                foreach (Player p in team.ActivePlayers.ToList())
                    if (p != player)
                        p.warp(player);
                return;
            }

            if ((recipient = player._arena.getPlayerByName(payload)) != null)
            {
                //Simply summon the player
                recipient.warp(player);
            }
        }

        /// <summary>
        /// Puts another player, or yourself, on a specified team
        /// </summary>
        static public void team(Player player, Player recipient, string payload, int bong)
        {	//Sanity checks
            if (String.IsNullOrEmpty(payload))
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
            if (String.IsNullOrEmpty(payload))
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
                //Remove spaces for dumb mods that use *teamname 1, 2
                teams[i + 1]._name = (teamNames[i].StartsWith(" ") ? teamNames[i].Substring(1) : teamNames[i]);
            }
        }

        /// <summary>
        /// Sets a ticker at the specified index with color/timer/messageSyntax: *ticker [message],[index],[color=optional],[timer=optional]
        /// </summary>
        static public void ticker(Player player, Player recipient, string payload, int bong)
        {
            if (String.IsNullOrEmpty(payload) || !payload.Contains(','))
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
            if (String.IsNullOrEmpty(payload))
            {   //Clear current timer if payload is empty
                player._arena.setTicker(1, player._arena.playtimeTickerIdx, 0, "");
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
                        player._arena.setTicker(1, player._arena.playtimeTickerIdx, minutes * 6000 + seconds * 100, "Time Remaining: ", delegate()
                        { player._arena.gameEnd(); });
                    }
                    else
                    {
                        player._arena.setTicker(1, player._arena.playtimeTickerIdx, 0, "");
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
            if (!String.IsNullOrEmpty(payload))
            {	//Find the team
                Team newTeam = player._arena.getTeamByName(payload);

                string temp = payload.ToLower();
                if (temp.Equals("spec") || temp.Equals("spectator") || newTeam == null)
                {
                    player.sendMessage(-1, "Invalid team specified");
                    return;
                }

                if (target.IsSpectator)
                {   //Is he on a spectator team?
                    //if (!target._team.IsSpec)
                        //newTeam.addPlayer(target);
                    //else
                        //Unspec him
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
                if (String.IsNullOrWhiteSpace(payload))
                    //Simply warp to the recipient
                    player.warp(recipient);
                else
                {	//Are we dealing with coords or exacts?
                    payload = payload.ToLower();

                    if (payload[0] >= 'a' && payload[0] <= 'z')
                    {
                        int x = (((int)payload[0]) - ((int)'a')) * 16 * 80;
                        int y = Convert.ToInt32(payload.Substring(1)) * 16 * 80;
                        try
                        {   //Incase there are double digits
                            y = Convert.ToInt32(payload.Substring(1, 2)) * 16 * 80;
                        }
                        catch
                        {
                        }

                        //We want to spawn in the coord center
                        x += 40 * 16;
                        y -= 40 * 16;

                        recipient.warp(x, y);
                    }
                    else
                    {
                        if (!payload.Contains(","))
                        {
                            player.sendMessage(-1, "Error: coordinates must be split using an apostrophy(,).");
                            return;
                        }

                        string[] coords = payload.Split(',');
                        int x = Convert.ToInt32(coords[0]) * 16;
                        int y = Convert.ToInt32(coords[1]) * 16;

                        recipient.warp(x, y);
                    }
                }
            }
            else
            {	//We must have a payload for this
                if (String.IsNullOrWhiteSpace(payload))
                {
                    player.sendMessage(-1, "Syntax: ::*warp or *warp alias, *warp A4 (optional teamname) or *warp 123,123 (optional teamname)");
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
                    try
                    {   //Incase there are double digits
                        y = Convert.ToInt32(payload.Substring(1, 2)) * 16 * 80;
                    }
                    catch
                    {
                    }

                    //We want to spawn in the coord center
                    x += 40 * 16;
                    y -= 40 * 16;

                    //Are we using a team name as well?
                    if (payload.Contains(' '))
                    {
                        string syntax = (payload.Substring(payload.IndexOf(' '))).Trim();
                        if (payload.Length > 0)
                        {
                            Team team;
                            if ((team = player._arena.getTeamByName(syntax)) != null)
                            {
                                if (team._name.Equals("spec") || team._name.Equals("spectator"))
                                {
                                    player.sendMessage(-1, "Cannot summon players on a spectating team.");
                                    return;
                                }
                                foreach (Player p in team.ActivePlayers.ToList())
                                    p.warp(x, y);
                                return;
                            }
                            else
                            {
                                player.sendMessage(-1, "That team doesn't exist.");
                                return;
                            }
                        }
                        player.warp(x, y);
                    }
                    else
                        player.warp(x, y);
                }
                else
                {
                    if (!payload.Contains(","))
                    {
                        player.sendMessage(-1, "Error: coordinates must be split using an apostrophy(,).");
                        return;
                    }

                    string[] coords = payload.Split(',');
                    int x = Convert.ToInt32(coords[0]) * 16;
                    int y;
                    
                    //Are we trying to summon a team?
                    if (coords[1].Contains(' '))
                    {
                        //Lets get our first number only
                        //This is a coord, any other number after could be a potential
                        //team name.
                        string Y = "";
                        foreach (char c in coords[1])
                        {
                            if (c == ' ')
                                break;
                            Y += c;
                        }
                        y = Convert.ToInt32(Y) * 16;

                        string syntax = (coords[1].Substring(coords[1].IndexOf(' '))).Trim();
                        if (syntax.Length > 0)
                        {
                            Team team;
                            if ((team = player._arena.getTeamByName(syntax)) != null)
                            {
                                if (team._name.Equals("spec") || team._name.Equals("spectator"))
                                {
                                    player.sendMessage(-1, "Cannot summon players on a spectating team.");
                                    return;
                                }
                                foreach (Player p in team.ActivePlayers.ToList())
                                    p.warp(x, y);
                                return;
                            }
                            else
                            {
                                player.sendMessage(-1, "That team doesn't exist.");
                                return;
                            }
                        }
                    }
                    else
                        y = Convert.ToInt32(coords[1]) * 16;

                    player.warp(x, y);
                }
            }
        }

        /// <summary>
        /// Toggles viewing of the mod commands on or off
        /// </summary>
        public static void watchmod(Player p, Player recipient, string payload, int bong)
        {
            p._watchMod = !p._watchMod;
            p.sendMessage(0, "You will" + (p._watchMod ? " now see " : " no longer see ") + "mod commands.");
        }

        /// <summary>
        /// Wipes a character
        /// </summary>
        public static void wipe(Player player, Player recipient, string payload, int bong)
        {   //Sanity checks
            if (String.IsNullOrWhiteSpace(payload) || !payload.ToLower().Contains("yes"))
            {
                player.sendMessage(-1, "Syntax: *wipe alias yes, :alias:*wipe yes, or *wipe all yes (Note: wipe all wipes anyone that played this zone)");
                return;
            }

            //Are we high enough?
            if (player._developer && player.PermissionLevelLocal < Data.PlayerPermission.SMod)
            {
                player.sendMessage(-1, "Only mods or level 3 dev's and higher can use this command.");
                return;
            }

            //Is this a pm?
            if (recipient != null)
            {
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
                return;
            }

            //Check level requirement
            if (player.PermissionLevel < Data.PlayerPermission.Sysop)
            {
                player.sendMessage(-1, "Sorry, you are not high enough.");
                return;
            }

            string[] argument = payload.Split(' ');
            if (argument[0].ToLower().Equals("all"))
            {
                //For active players, reset them
                foreach (KeyValuePair<string, Arena> a in player._server._arenas)
                {
                    if (a.Value.TotalPlayerCount > 0)
                    {
                        a.Value.sendArenaMessage("Zone reset has been called, wiping all stats.", 2);
                        foreach (Player p in a.Value.Players)
                        {
                            if (!p.IsSpectator)
                                p.spec();

                            //Wiping all stats/inv etc
                            p.assignFirstTimeStats(true);
                            p.syncInventory();
                            p.syncState();
                            Logic_Assets.RunEvent(p, p._server._zoneConfig.EventInfo.firstTimeSkillSetup);
                            Logic_Assets.RunEvent(p, p._server._zoneConfig.EventInfo.firstTimeInvSetup);
                        }
                    }
                }

                //Send it to the db
                CS_ChatQuery<Data.Database> query = new CS_ChatQuery<Data.Database>();
                query.queryType = CS_ChatQuery<Data.Database>.QueryType.wipe;
                query.payload = argument[0].ToLower();
                query.sender = player._alias;
                player._server._db.send(query);
                return;
            }
            else
            {
                //Nope, check for aliases
                if (argument[0].ToLower().Equals("yes"))
                {
                    //Error, didnt type an alias
                    player.sendMessage(-1, "Syntax: *wipe <alias> yes, :alias:*wipe yes, or *wipe all yes");
                    return;
                }

                if ((recipient = player._server.getPlayer(argument[0])) != null)
                {
                    if (!recipient.IsSpectator)
                        recipient.spec();
                    //Wiping all stats/inv etc
                    recipient.assignFirstTimeStats(true);
                    recipient.syncInventory();
                    recipient.syncState();
                    Logic_Assets.RunEvent(recipient, recipient._server._zoneConfig.EventInfo.firstTimeSkillSetup);
                    Logic_Assets.RunEvent(recipient, recipient._server._zoneConfig.EventInfo.firstTimeInvSetup);
                    recipient.sendMessage(0, "Your stats have been wiped.");
                }

                //Send it to the db
                CS_ChatQuery<Data.Database> query = new CS_ChatQuery<Data.Database>();
                query.queryType = CS_ChatQuery<Data.Database>.QueryType.wipe;
                query.payload = argument[0];
                query.sender = player._alias;
                player._server._db.send(query);

                return;
            }
        }

        /// <summary>
        /// Sends a arena/system message to all arena's in the zone
        /// </summary>
        static public void zone(Player player, Player recipient, string payload, int bong)
        {
            if (String.IsNullOrEmpty(payload))
                player.sendMessage(-1, "Message can not be empty.");
            else
            {
                string format = String.Format("{0} - {1}", payload, player._alias);
                //Snap color code before sending
                Regex reg = new Regex(@"^(~|!|@|#|\$|%|\^|&|\*)", RegexOptions.IgnoreCase);
                Match match = reg.Match(payload);
                if (match.Success)
                    format = String.Format("{0}[Zone] {1}", match.Groups[0].Value, format.Remove(0, 1));
                else
                    format = String.Format("[Zone] {0}", format);
                //Send it to all
                foreach(Arena arena in player._server._arenas.Values)
                    if (arena != null)
                        arena.sendArenaMessage(format, bong);
            }
        }
 
        ////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////Below are Banning Commands/////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Bans a player from all zones
        /// </summary>
        static public void ban(Player player, Player recipient, string payload, int bong)
        {
            //Sanity check
            if (player._server.IsStandalone)
            {
                player.sendMessage(-1, "Server is currently in stand-alone mode.");
                return;
            }

            if (player._developer)
            {
                player.sendMessage(-1, "Nice try.");
                return;
            }

            if (recipient == null && String.IsNullOrEmpty(payload))
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
                if (recipient.PermissionLevel >= player.PermissionLevel && !player._admin)
                {
                    player.sendMessage(-1, "You can't ban someone equal or higher than you.");
                    return;
                }

                if (String.IsNullOrEmpty(payload))
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
                    if (!String.IsNullOrEmpty(param[1]))
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
            {
                if (recipient.PermissionLevel >= player.PermissionLevel && !player._admin)
                {
                    player.sendMessage(-1, "You cannot ban someone equal or higher than you.");
                    return;
                }
                recipient.disconnect();
            }

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

            if (recipient == null && String.IsNullOrEmpty(payload))
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
                if (recipient.PermissionLevel >= player.PermissionLevel && !player._admin)
                {
                    player.sendMessage(-1, "You can't ban someone equal or higher than you.");
                    return;
                }

                if (String.IsNullOrEmpty(payload))
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
                    if (!String.IsNullOrEmpty(param[1]))
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
                    if (!String.IsNullOrEmpty(param[1]))
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
            {
                if (recipient.PermissionLevel >= player.PermissionLevel && !player._admin)
                {
                    player.sendMessage(-1, "You cannot ban someone equal or higher than you.");
                    return;
                }
                recipient.disconnect();
            }

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
            if (!String.IsNullOrEmpty(payload) && payload.Equals("all", StringComparison.CurrentCultureIgnoreCase))
            {
                foreach (Player p in player._arena.Players)
                    if (p != player)
                        p.disconnect();
            }
            else
            {
                if (recipient != null && player.PermissionLevel >= recipient.PermissionLevel)
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

            if (recipient == null && String.IsNullOrEmpty(payload))
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
                if (recipient.PermissionLevel >= player.PermissionLevel && !player._admin)
                {
                    player.sendMessage(-1, "You can't ban someone equal or higher than you.");
                    return;
                }

                if (String.IsNullOrEmpty(payload))
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
                    if (!String.IsNullOrEmpty(param[1]))
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
                    if (!String.IsNullOrEmpty(param[1]))
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
            {
                if (recipient.PermissionLevel >= player.PermissionLevel && !player._admin)
                {
                    player.sendMessage(-1, "You cannot ban someone equal or higher than you.");
                    return;
                }
                recipient.disconnect();
            }

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
            if (player._server.IsStandalone)
            {
                player.sendMessage(-1, "Server is currently in stand-alone mode.");
                return;
            }

            if (player._developer)
            {
                player.sendMessage(-1, "Nice try.");
                return;
            }

            if (String.IsNullOrEmpty(payload) && recipient == null)
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
                if (recipient.PermissionLevel >= player.PermissionLevel && !player._admin)
                {
                    player.sendMessage(-1, "You cannot ban someone equal or higher than you.");
                    return;
                }

                if (String.IsNullOrEmpty(payload))
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
                    if (!String.IsNullOrEmpty(param[1]))
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
                    if (!String.IsNullOrEmpty(param[1]))
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
            {
                if (recipient.PermissionLevel >= player.PermissionLevel && !player._admin)
                {
                    player.sendMessage(-1, "You cannot ban someone equal or higher than you.");
                    return;
                }
                recipient.disconnect();
            }

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

            if (level < (int)Data.PlayerPermission.Mod && player._arena._name.StartsWith("Public", StringComparison.OrdinalIgnoreCase))
            {
                player.sendMessage(-1, "You can only use it in non-public arena's.");
                return;
            }

            if (rLevel >= level && !player._admin)
            {
                player.sendMessage(-1, "No.");
                return;
            }

            if (recipient != null && String.IsNullOrEmpty(payload))
            {
                //Assume they just want to kick them out of the arena temporarily
                recipient.sendMessage(-1, "You have been kicked out of the arena.");
                recipient.disconnect();
                player.sendMessage(0, String.Format("You have kicked player {0} from the arena.", recipient._alias));
                return;
            }

            if (recipient == null && String.IsNullOrEmpty(payload))
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
                alias = recipient._alias;
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
                alias = recipient._alias;

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
            recipient._arena._blockedList.Add(recipient._alias, DateTime.Now.AddMinutes(minutes));
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
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(addball, "addball",
                "Adds a ball to the arena.",
                "*addball",
               InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(allow, "allow",
                "Lists, Adds or Removes a player from a private arena list.",
                "*allow list OR *allow alias",
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(arena, "arena",
                "Send a arena-wide system message.",
                "*arena message",
               InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(arenalock, "arenalock",
                "Locks or unlocks an arena from outsiders. Note: will kick out any non powered players in spec and only mods or admins can join/stay.",
                "*arenalock",
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(auth, "auth",
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

            yield return new HandlerDescriptor(findItem, "finditem",
                "Finds an item within our zone",
                "*finditem [itemID or item name]",
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(getball, "getball",
                "Gets a ball.", "*getball (gets ball ID 0) or *getball # (gets a specific ball ID)",
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
                ":alias:*goto or *goto alias or *goto A4 (optional team name) or *goto 123,123 (optional team name)",
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

            yield return new HandlerDescriptor(removeball, "removeball",
                "Removes a ball from the arena.",
                "*removeball (by itself, removes ballID 0), *removeball # (removes that ball id)",
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(scramble, "scramble",
                "Scrambles an arena, use on/off to set the arena to scramble automatically.",
                "::*scramble [on/off(optional)]",
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(shutup, "shutup",
               "Toggles a players ability to use the messaging system entirely",
               ":alias:*shutup",
               InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(skill, "skill",
                "Gives or sets a skill to a player",
                "::*skill id:amount",
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
                "::*summon, *summon alias, *summon team teamname, or *summon all",
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

            yield return new HandlerDescriptor(unspec, "unspec",
                "Takes a player out of spectator mode and puts him on the specified team.",
                "*unspec [team] or ::*unspec [team] or ::*unspec ..",
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(warp, "warp",
                "Warps you to a specified player, coordinate or exact coordinate. Alternatively, you can warp other players to coordinates or exacts.",
                "::*warp, *warp alias, *warp A4 (optional team name) or *warp 123,123 (optional team name)",
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(watchmod, "watchmod",
                "Toggles viewing mod commands on or off",
                "*watchmod",
                InfServer.Data.PlayerPermission.ArenaMod, true);

            yield return new HandlerDescriptor(wipe, "wipe",
                "Wipes a character (or all) within the current zone",
                "*wipe all yes, *wipe <alias> yes, :alias:*wipe yes",
                InfServer.Data.PlayerPermission.Mod, true);

            yield return new HandlerDescriptor(zone, "zone",
                "Send a zone-wide system message.",
                "*zone message",
               InfServer.Data.PlayerPermission.ArenaMod, true);
        }
    }
}