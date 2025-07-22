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
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
                //Make everyone aware
                Ball.Spawn_Ball(player, ball);
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
            if (string.IsNullOrEmpty(payload))
                askedBallId = 0;
            else
            {
                ushort requestBallID = Convert.ToUInt16(payload);
                if (requestBallID > Arena.maxBalls || requestBallID < 0) //5 is max on the field (0 id = 1 ball, 3 id = 4 balls)
                {
                    player.sendMessage(-1, "Invalid Ball ID");
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

            //Update data and route
            ball._arena.updateBall(ball);
        }

        /// <summary>
        /// Removes a ball from the arena
        /// </summary>
        static public void removeball(Player player, Player recipient, string payload, int bong)
        {
            ushort askedBallId; // Id of the requested getball
            if (string.IsNullOrEmpty(payload))
                askedBallId = 0;
            else
            {
                ushort requestBallID = Convert.ToUInt16(payload);
                if (requestBallID > Arena.maxBalls || requestBallID < 0)
                {
                    player.sendMessage(-1, "Invalid Ball ID");
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
            if (string.IsNullOrEmpty(payload))
            {
                player.sendMessage(-1, "Syntax: *allow alias OR *allow list to see who's allowed");
                player.sendMessage(0, "Notes: make sure you are in the arena you want and type the alias correctly(Case doesn't matter)");
                return;
            }

            if (player._arena._bIsPublic)
            {
                player.sendMessage(-1, "Only non-public arena's are allowed to be locked.");
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
                whisper.message = (string.Format("You have been given permission to enter {0}.", player._arena._name));
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
            if (string.IsNullOrEmpty(payload))
                player.sendMessage(-1, "Message can not be empty.");
            else
            {
                //Snap color code before sending

                Regex reg = new Regex(@"^(~|!|@|#|\$|%|\^|&|\*)", RegexOptions.IgnoreCase);
                Match match = reg.Match(payload);

                if (match.Success)
                {
                    payload = string.Format("{0}[Arena] {1}", match.Groups[0].Value, payload.Remove(0, 1));
                }
                else
                {
                    payload = string.Format("[Arena] {0}", payload);
                }

                if (player.PermissionLevelLocal < Data.PlayerPermission.ManagerSysop)
                {
                    bool detected;
                    payload = SwearFilter.Filter(payload, out detected);
                }

                player._arena.sendArenaMessage(payload, bong);
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
                if (player._arena.IsGranted(player))
                {
                    player.sendMessage(-1, "Only mods/refs are allowed to lock non-private arenas.");
                    return;
                }
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
            player._arena.sendArenaMessage(string.Format("Arena is now {0}.", player._arena._aLocked ? "locked" : "unlocked"));
        }

        static public void maxpop(Player player, Player recipient, string payload, int bong)
        {
            if (!player._arena.IsPrivate)
            {
                player.sendMessage(-1, "This command is only usable in private arenas.");
                return;
            }

            if (string.IsNullOrWhiteSpace(payload))
            {
                player.sendMessage(-1, $"Max pop for current arena is {player._arena._maxPrivatePop}");
            }
            else
            {
                int maxVal;

                if (!Int32.TryParse(payload, out maxVal) || maxVal < 0)
                {
                    player.sendMessage(-1, "Malformed value. Please specify a positive integer.");
                    return;
                }

                if (maxVal == 0)
                {
                    var arenaConfig = player._arena._server._zoneConfig.arena;
                    maxVal = arenaConfig.maxPrivatePlayers != 0 ? arenaConfig.maxPrivatePlayers : arenaConfig.maxPlayers;
                }

                player._arena._maxPrivatePop = maxVal;
                player.sendMessage(-1, $"Max pop for current arena updated to {maxVal}");
            }
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
                    player._admin = true;
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

            if (player._arena._name.StartsWith("Arena", StringComparison.OrdinalIgnoreCase) && level < (int)Data.PlayerPermission.Mod)
            {
                player.sendMessage(-1, "You can only use it in non-public arena's.");
                return;
            }

            if (!player._arena._name.StartsWith("Arena", StringComparison.OrdinalIgnoreCase) && !player._arena.IsPrivate)
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

            if (player._arena._name.StartsWith("Arena", StringComparison.OrdinalIgnoreCase) && level < (int)Data.PlayerPermission.Mod)
            {
                player.sendMessage(-1, "You can only use it in non-public arena's.");
                return;
            }

            if (!player._arena._name.StartsWith("Arena", StringComparison.OrdinalIgnoreCase) && !player._arena.IsPrivate)
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
        /// Spawns a flag to a specified position
        /// </summary>
        static public void flagSpawn(Player player, Player recipient, string payload, int bong)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                player.sendMessage(-1, "What flag(s) do you want to spawn?");
                player.sendMessage(0, "Syntax: *flagspawn <all or specific flag> : <location or default>");
                player.sendMessage(0, "NOTE: flags can be looked up by name or lio id.");
                return;
            }

            if (!payload.Contains(':'))
            {
                player.sendMessage(-1, "Error in command, missing a colon seperation between flag and location.");
                return;
            }

            string[] split = payload.Split(':');
            if (string.IsNullOrWhiteSpace(split[0]))
            {
                player.sendMessage(-1, "You must provide either a flag name or id first.");
                return;
            }

            if (string.IsNullOrWhiteSpace(split[1]))
            {
                player.sendMessage(-1, "Flag location cannot be blank. Please choose either default, exact coords or sector location.");
                return;
            }

            Arena.FlagState fs = null;
            int id = -1;
            bool allSpawn = split[0].Equals("all");

            if (!allSpawn)
            {
                //Are they using an id?
                if (int.TryParse(split[0], out id))
                {   //Yep
                    fs = player._arena.getFlag(id);
                    if (fs == null)
                    {
                        player.sendMessage(-1, "Cannot find that specific flag id.");
                        return;
                    }
                }
                else
                {
                    //Nope, must be a name
                    fs = player._arena.getFlag(split[0].Trim());
                    if (fs == null)
                    {
                        player.sendMessage(-1, "Cannot find that specific flag.");
                        return;
                    }
                }
            }

            //Spawning at default location?
            string coords = split[1].ToLower();
            if (string.Equals(coords, "default"))
            {
                if (!allSpawn)
                {
                    if (!fs.bActive)
                    {
                        player.sendMessage(-1, "Cannot spawn that specific flag because it is not active.");
                        return;
                    }
                    player._arena.flagSpawn(fs);
                    player.sendMessage(0, string.Format("Flag {0} has been spawned at its default location.", fs.flag.GeneralData.Name));
                }
                else
                {
                    player._arena.flagSpawn();
                    player.sendMessage(0, "All active flags have been spawned at its default location.");
                }
                return;
            }

            //Are we dealing with coords or exacts?
            int x = 0, y = 0;
            if (coords[0] >= 'a' && coords[0] <= 'z')
            {   //Coords
                x = (((int)coords[0]) - ((int)'a')) * 16 * 80;
                try
                {
                    y = Convert.ToInt32(coords.Substring(1)) * 16 * 80; //Bypass the letter to get numbers
                }
                catch
                {
                    player.sendMessage(-1, "That is not a valid coord.");
                    return;
                }

                //We want to spawn it in the coords center
                x += 40 * 16;
                y -= 40 * 16;

                if (player._arena.getTile(x, y).Blocked)
                {
                    player.sendMessage(-1, "Cannot spawn flag on that location.");
                    return;
                }

                if (!allSpawn)
                {
                    if (!fs.bActive)
                    {
                        player.sendMessage(-1, "Cannot spawn that specific flag because it is not active.");
                        return;
                    }
                    fs.posX = (short)x;
                    fs.posY = (short)y;
                    Helpers.Object_Flags(player._arena.Players, fs);

                    player.sendMessage(0, string.Format("Flag {0} has been spawned at {1}.", fs.flag.GeneralData.Name, coords));
                }
                else
                {
                    foreach (Arena.FlagState f in player._arena._flags.Values)
                    {
                        f.posX = (short)x;
                        f.posY = (short)y;
                    }
                    Helpers.Object_Flags(player._arena.Players, player._arena._flags.Values);
                    player.sendMessage(0, string.Format("All active flags have been spawned at {0}.", coords));
                }
                return;
            }

            //Exacts
            if (!coords.Contains(','))
            {
                player.sendMessage(-1, "Error in location, no comma used between exact coords.");
                return;
            }

            string[] exacts = coords.Split(',');
            try
            {
                x = Convert.ToInt32(exacts[0]) * 16;
                y = Convert.ToInt32(exacts[1]) * 16;
            }
            catch
            {
                player.sendMessage(-1, "That is not a valid exact coord.");
                return;
            }

            if (player._arena.getTile(x, y).Blocked)
            {
                player.sendMessage(-1, "Cannot spawn flag on that location.");
                return;
            }

            if (!allSpawn)
            {
                fs.posX = (short)x;
                fs.posY = (short)y;
                fs.bActive = true;
                Helpers.Object_Flags(player._arena.Players, fs);

                player.sendMessage(0, string.Format("Flag {0} has been spawned at {1}.", fs.flag.GeneralData.Name, coords));
            }
            else
            {
                foreach (Arena.FlagState f in player._arena._flags.Values)
                {
                    f.posX = (short)x;
                    f.posY = (short)y;
                    f.bActive = true;
                }
                Helpers.Object_Flags(player._arena.Players, player._arena._flags.Values);
                player.sendMessage(0, string.Format("All flags have been spawned at {0}.", coords));
            }
        }

        /// <summary>
        /// Finds a player currently logged in on possible aliases
        /// </summary>
        static public void find(Player player, Player recipient, string payload, int bong)
        {
            if (string.IsNullOrEmpty(payload))
            {
                player.sendMessage(-1, "Syntax: *find [alias]");
                return;
            }

            if (player._server.IsStandalone)
                player.sendMessage(0, "Server is in Stand-Alone Mode. Command will only work zone wide.");

            //Send it
            if (!player._server.IsStandalone)
            {
                CS_ModQuery<Data.Database> find = new CS_ModQuery<Data.Database>();
                find.queryType = CS_ModQuery<Data.Database>.QueryType.find;
                find.sender = player._alias;
                find.query = payload;

                player._server._db.send(find);
            }
            else
            {
                Player target;
                //Check to see if that player is in the zone
                if ((target = player._server.getPlayer(payload)) != null)
                {
                    System.Net.IPEndPoint ip = target._client._ipe;
                    player.sendMessage(0, "&Search Results:");
                    if (ip == null)
                    {
                        player.sendMessage(0, string.Format("*Found: {0} [Zone: {1} - Arena: {2}]", target._alias, target._server != null ? target._server.Name : "Unknown", !string.IsNullOrWhiteSpace(target._arena._name) ? target._arena._name : "Unknown Arena"));
                        return;
                    }
                    System.Net.IPEndPoint targetIP;
                    foreach (Arena a in player._server._arenas.Values)
                        foreach (Player p in a.Players)
                        {
                            targetIP = p._client._ipe;
                            if (targetIP != null && targetIP.Address.Equals(ip.Address))
                                player.sendMessage(0, string.Format("*Found: {0} [Zone: {1} - Arena: {2}]", p._alias, p._server != null ? p._server.Name : "Unknown", a._name));
                        }
                }
                else
                    player.sendMessage(-1, "Cannot find the specified alias.");
            }
        }

        /// <summary>
        /// Finds an item within the zone
        /// </summary>
        static public void findItem(Player player, Player recipient, string payload, int bong)
        {
            if (string.IsNullOrEmpty(payload))
            {
                player.sendMessage(-1, "Syntax: *finditem [itemID or item Name]");
                return;
            }

            if (Helpers.IsNumeric(payload))
            {
                int Itemid = Convert.ToInt32(payload);

                //Obtain the item indicated
                Assets.ItemInfo item = player._server._assets.getItemByID(Convert.ToInt32(Itemid));
                if (item == null)
                {
                    player.sendMessage(-1, "That item doesn't exist.");
                    return;
                }

                player.sendMessage(0, string.Format("[{0}] {1}", item.id, item.name));
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
                        player.sendMessage(0, string.Format("[{0}] {1}", item.id, item.name));
                        count++;
                    }
                }
                if (count == 0)
                    player.sendMessage(-1, "That item doesn't exist.");
            }
        }

        /// <summary>
        /// Finds a skill within the zone
        /// </summary>
        static public void findSkill(Player player, Player recipient, string payload, int bong)
        {
            if (string.IsNullOrEmpty(payload))
            {
                player.sendMessage(-1, "Syntax: *findskill [skill ID or skill Name]");
                return;
            }

            //Our handy string/int checker
            bool IsNumeric = Regex.IsMatch(payload, @"^[-0-9]+$");

            SkillInfo skill;
            //Asking for a skill id?
            if (!IsNumeric)
            {
                payload = payload.Trim().ToLower();

                //Nope, lets try finding the exact match first
                skill = player._server._assets.getSkillByName(payload);
                if (skill != null)
                {
                    player.sendMessage(0, string.Format("[{0}] {1}", skill.SkillId, skill.Name));
                    return;
                }

                //Lets try a semi match
                List<Assets.SkillInfo> skills = player._server._assets.getSkillInfos;
                if (skills == null)
                {
                    player.sendMessage(-1, "That skill doesn't exist.");
                    return;
                }

                int count = 0;
                foreach (Assets.SkillInfo sk in skills)
                {
                    if (sk.Name.Contains(payload))
                    {
                        player.sendMessage(0, string.Format("[{0}] {1}", sk.SkillId, sk.Name));
                        count++;
                    }
                }

                if (count == 0)
                {
                    player.sendMessage(-1, "That skill doesn't exist.");
                    return;
                }

                return;
            }
            else
                skill = player._server._assets.getSkillByID(Int32.Parse(payload.Trim()));

            if (skill == null)
            {
                player.sendMessage(-1, "That skill doesn't exist.");
                return;
            }

            player.sendMessage(0, string.Format("[{0}] {1}", skill.SkillId, skill.Name));
        }

        /// <summary>
        /// Sends a global message to every zone connected to current database
        /// </summary>
        static public void global(Player player, Player recipient, string payload, int bong)
        {   //Sanity checks
            if (string.IsNullOrEmpty(payload))
            {
                player.sendMessage(-1, "And say what?");
                return;
            }

            if (player._server.IsStandalone)
            {
                player.sendMessage(-1, "Server is currently in stand-alone mode.");
                return;
            }

            //Snap the color code before sending
            Regex reg = new Regex(@"^(~|!|@|#|\$|%|\^|&|\*)", RegexOptions.IgnoreCase);
            Match match = reg.Match(payload);

            if (match.Success)
            {
                payload = string.Format("{0}[Global] {1}", match.Groups[0].Value, payload.Remove(0, 1));
            }
            else
            {
                payload = string.Format("[Global] {0}", payload);
            }

            CS_ChatQuery<Data.Database> pkt = new CS_ChatQuery<Data.Database>();
            pkt.queryType = CS_ChatQuery<Data.Database>.QueryType.global;
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
            if (player._arena._bIsPublic)
            {
                player.sendMessage(-1, "You cannot grant in public arena's.");
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
                    player._arena.sendArenaMessage(string.Format("{0} is no longer granted.", recipient._alias));
                    recipient.sendMessage(0, "You have been removed from arena privileges.");
                    return;
                }
                player._arena._owner.Add(recipient._alias);
                recipient._permissionTemp = Data.PlayerPermission.ArenaMod;
                player._arena.sendArenaMessage(string.Format("{0} is now granted.", recipient._alias));
                recipient.sendMessage(0, "You are now granted arena privileges.");
            }
            else
            {
                if (string.IsNullOrEmpty(payload))
                {
                    player.sendMessage(-1, "Syntax: *grant alias");
                    return;
                }

                if ((recipient = player._arena.getPlayerByName(payload.ToLower())) == null)
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
                    player._arena.sendArenaMessage(string.Format("{0} is no longer granted.", recipient._alias));
                    recipient.sendMessage(0, "You have been removed from arena privileges.");
                    return;
                }
                player._arena._owner.Add(recipient._alias);
                recipient._permissionTemp = Data.PlayerPermission.ArenaMod;
                recipient.sendMessage(0, "You are now granted arena privileges.");
            }

            player.sendMessage(0, "You have given " + recipient._alias + " arena privileges.");
        }

        /// <summary>
        /// Grants a player private arena privileges
        /// </summary>
        static public void moderator(Player player, Player recipient, string payload, int bong)
        {
            //Sanity checks
            if (player._arena._bIsPublic)
            {
                player.sendMessage(-1, "You cannot grant moderator in public arena's.");
                return;
            }

            if (!player._arena.IsGranted(player) && player.PermissionLevelLocal < Data.PlayerPermission.Mod)
            {
                player.sendMessage(-1, "You are not granted moderator in this arena.");
                return;
            }

            if (recipient != null)
            {
                if (recipient.PermissionLevel >= Data.PlayerPermission.Mod)
                {
                    player.sendMessage(-1, "That person doesn't need grant.");
                    return;
                }

                if (player._arena.IsGranted(recipient))
                {
                    if (recipient.PermissionLevel >= Data.PlayerPermission.Mod)
                    {
                        player.sendMessage(-1, "You cannot remove this players power.");
                        return;
                    }

                    if (player._arena._owner.Contains(recipient._alias))
                        player._arena._owner.Remove(recipient._alias);

                    recipient._permissionTemp = Data.PlayerPermission.Normal;
                    player._arena.sendArenaMessage(string.Format("{0} is no longer granted moderator.", recipient._alias));
                    recipient.sendMessage(0, "You have been removed from arena privileges.");
                    return;
                }
                player._arena._owner.Add(recipient._alias);
                recipient._permissionTemp = Data.PlayerPermission.Mod;
                player._arena.sendArenaMessage(string.Format("{0} is now granted moderator.", recipient._alias));
                recipient.sendMessage(0, "You are now granted arena privileges.");
            }
            else
            {
                if (string.IsNullOrEmpty(payload))
                {
                    player.sendMessage(-1, "Syntax: *moderator alias");
                    return;
                }

                if ((recipient = player._arena.getPlayerByName(payload.ToLower())) == null)
                {
                    player.sendMessage(-1, "That player doesn't exist.");
                    return;
                }

                if (recipient.PermissionLevel >= Data.PlayerPermission.Mod)
                {
                    player.sendMessage(-1, "That person doesn't need grant.");
                    return;
                }

                if (player._arena.IsGranted(recipient))
                {
                    if (recipient.PermissionLevel >= Data.PlayerPermission.Mod)
                    {
                        player.sendMessage(-1, "You cannot remove this players power.");
                        return;
                    }

                    if (player._arena._owner.Contains(recipient._alias))
                        player._arena._owner.Remove(recipient._alias);

                    recipient._permissionTemp = Data.PlayerPermission.Normal;
                    player._arena.sendArenaMessage(string.Format("{0} is no longer granted.", recipient._alias));
                    recipient.sendMessage(0, "You have been removed from arena privileges.");
                    return;
                }
                player._arena._owner.Add(recipient._alias);
                recipient._permissionTemp = Data.PlayerPermission.Mod;
                recipient.sendMessage(0, "You are now granted arena privileges.");
            }

            player.sendMessage(0, "You have given " + recipient._alias + " arena moderator privileges.");
        }

        /// <summary>
        /// Gives the user help information on a given command
        /// </summary>
        static public void help(Player player, Player recipient, string payload, int bong)
        {
            if (string.IsNullOrEmpty(payload))
            {	//List all mod commands
                player.sendMessage(0, "&Commands available to you:");

                SortedList<string, int> commands = new SortedList<string, int>();
                int playerPermission = (int)player.PermissionLevelLocal;
                string[] powers = { "Basic", "Granted Player", "Mod", "Super Mod", "Manager/Sysop", "Head Mod/Admin" };
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
                        player.sendMessage(0, string.Format("!{0}", powers[level]));
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
                        player.sendMessage(0, string.Format("!{0}", powers[level]));
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
            if (string.IsNullOrEmpty(payload))
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
            if (string.IsNullOrEmpty(payload))
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
                whisper.message = (string.Format("You have been given permission to enter {0}.", player._server.Name));
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
                    player._arena.sendArenaMessage(string.Format("{0}'s Poll has been cancelled.", player._alias));
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
                if (string.IsNullOrEmpty(payload))
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
                player._arena.setTicker(1, index, timer * 100, result.ElementAt(0), delegate () { player._arena.pollQuestion(player._arena, true); });
                player._arena.sendArenaMessage(string.Format("&A Poll has been started by {0}." + " Topic: {1}", (player.IsStealth ? "Unknown" : player._alias), result.ElementAt(0)));
                player._arena.sendArenaMessage("&Type ?poll yes or ?poll no to participate");
            }
            else
            {
                player._arena.setTicker(1, 4, timer, payload); //Game ending will stop and display results
                player._arena.sendArenaMessage(string.Format("&A Poll has been started by {0}." + " Topic: {1}", (player.IsStealth ? "Unknown" : player._alias), payload));
            }

            player._arena._poll.start = true;
        }

        /// <summary>
        /// Spawns an item on the ground or in a player's inventory
        /// </summary>
        static public void prize(Player player, Player recipient, string payload, int bong)
        {	//Sanity checks
            if (string.IsNullOrEmpty(payload))
            {
                player.sendMessage(-1, "Syntax: *prize item:amount or ::*prize item:amount");
                return;
            }

            //Get players mod level first
            int level;
            if (player.PermissionLevelLocal == Data.PlayerPermission.GrantedPlayer)
                level = (int)player.PermissionLevelLocal;
            else
                level = (int)player.PermissionLevel;

            if (player._arena._name.StartsWith("Arena", StringComparison.OrdinalIgnoreCase) && level < (int)Data.PlayerPermission.Mod)
            {
                player.sendMessage(-1, "You can only use it in non-public arena's.");
                return;
            }

            if (!player._arena._allowprize)
            {
                player.sendMessage(-1, "This arena does not allow *prize to be used.");
                return;
            }
            /*
            if (!player._arena._name.StartsWith("Arena", StringComparison.OrdinalIgnoreCase) && !player._arena.IsPrivate)
                if (level < (int)Data.PlayerPermission.Mod)
                {
                    player.sendMessage(-1, "You can only use it in private arena's.");
                    return;
                }
            */
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

            //Check if player is a granted player and compare item selected to list of allowed items.
            if (level == (int)Data.PlayerPermission.GrantedPlayer)
            {
                if (item.buyPrice == 0)
                {
                    if (player._arena.Prizelist && !player._arena._prizeItems.Contains(item.id))
                    {
                        player.sendMessage(-1, "You do not have permission to prize this item. It is unpurchasable in store and not on the permitted list.");
                        return;
                    }
                    else
                    {
                        if (!player._arena.Prizelist)
                        {
                            player.sendMessage(-1, "Prize item list does not exist, please contact admin.");
                            return;
                        }
                    }
                }
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
            if (!string.IsNullOrEmpty(payload))
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
                player.sendMessage(0, "&Permission Level: " + (int)player.PermissionLevel + (player._developer ? "(Dev)" : player._permissionTemp == Data.PlayerPermission.ArenaMod ? "(Granted)" : ""));
            else if (player.PermissionLevel >= target.PermissionLevel)
                player.sendMessage(0, "&Permission Level: " + (int)target.PermissionLevel + (target._developer ? "(Dev)" : player._permissionTemp == Data.PlayerPermission.ArenaMod ? "(Granted)" : ""));
            player.sendMessage(0, "Items");
            foreach (KeyValuePair<int, Player.InventoryItem> itm in target._inventory)
                player.sendMessage(0, string.Format("~{0}={1}", itm.Value.item.name, itm.Value.quantity));
            player.sendMessage(0, "Skills");
            foreach (KeyValuePair<int, Player.SkillItem> skill in target._skills)
                player.sendMessage(0, string.Format("~{0}={1}", skill.Value.skill.Name, skill.Value.quantity));
            player.sendMessage(0, "Profile");
            player.sendMessage(0, string.Format("~Cash={0} Experience={1} Points={2}", target.Cash, target.Experience, target.Points));
            player.sendMessage(0, "Base Vehicle");
            player.sendMessage(0, string.Format("~VehicleID={0} Health={1} Energy={2}", target._baseVehicle._id, target._baseVehicle._state.health, target._baseVehicle._state.energy));
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

        static public void silenced(Player player, Player recipient, string payload, int bong)
        {
            var players = player._arena._server.SilencedPlayers
                .DistinctBy(sp => sp.IPAddress)
                .ToList();

            if (players.Count == 0)
            {
                player.sendMessage(0, "No players silenced in this zone.");
                return;
            }

            player.sendMessage(0, string.Join(", ", players.Select(p => $"{p.Alias} ({p.DurationMinutes} mins)")));
        }

        /// <summary>
        /// Toggles a players ability to use the messaging system entirely across a single zone
        /// </summary>
        static public void silence(Player player, Player recipient, string payload, int bong)
        {
            //Sanity checks
            if (recipient == null)
            {
                player.sendMessage(-1, "Syntax: :alias:*silence");
                return;
            }

            if (player != recipient && (int)player.PermissionLevelLocal < (int)recipient.PermissionLevel)
            {
                player.sendMessage(-1, "Nice try.");
                return;
            }

            if (string.IsNullOrEmpty(payload))
            {
                player.sendMessage(-1, "Syntax: :alias:*silence timeinminutes");
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

            //Are we changing the time?
            if (recipient._bSilenced && minutes > 0)
            {
                var serverEntry = recipient._arena._server.SilencedPlayers.FirstOrDefault(sp => sp.IPAddress.Equals(recipient._ipAddress) || sp.Alias.ToLower() == recipient._alias.ToLower());

                if (serverEntry != null)
                {
                    serverEntry.DurationMinutes = minutes;
                }
                else
                {
                    var sp = new SilencedPlayer { IPAddress = recipient._ipAddress, Alias = recipient._alias, DurationMinutes = minutes, SilencedAt = DateTime.Now };

                    recipient._arena._server.SilencedPlayers.Add(sp);
                }

                recipient._lengthOfSilence = minutes;
                recipient._timeOfSilence = DateTime.Now;

                player.sendMessage(0, recipient._alias + " has been zone silenced for " + minutes + " minutes.");
                return;
            }

            //Toggle his ability to speak
            recipient._bSilenced = !recipient._bSilenced;

            //Notify some people
            if (recipient._bSilenced)
            {
                var serverEntry = recipient._arena._server.SilencedPlayers.FirstOrDefault(sp => sp.IPAddress.Equals(recipient._ipAddress) || sp.Alias.ToLower() == recipient._alias.ToLower());

                if (serverEntry != null)
                {
                    serverEntry.DurationMinutes = minutes;
                }
                else
                {
                    var sp = new SilencedPlayer { IPAddress = recipient._ipAddress, Alias = recipient._alias, DurationMinutes = minutes, SilencedAt = DateTime.Now };

                    recipient._arena._server.SilencedPlayers.Add(sp);
                }

                recipient._lengthOfSilence = minutes;
                recipient._timeOfSilence = DateTime.Now;

                player.sendMessage(0, recipient._alias + " has been zone silenced.");
            }
            else
            {
                recipient._lengthOfSilence = 0;

                var serverEntry = recipient._arena._server.SilencedPlayers.FirstOrDefault(sp => sp.IPAddress.Equals(recipient._ipAddress) || sp.Alias.ToLower() == recipient._alias.ToLower());

                if (serverEntry != null)
                {
                    recipient._arena._server.SilencedPlayers.Remove(serverEntry);
                }

                player.sendMessage(0, recipient._alias + " has been unsilenced.");
            }
        }

        /// <summary>
        /// Gives a player a certain skill/attribute
        /// </summary>
        static public void skill(Player player, Player recipient, string payload, int bong)
        {
            //Sanity checks
            if (string.IsNullOrEmpty(payload) || recipient == null)
            {
                player.sendMessage(-1, "Syntax: :player:*skill id:amount");
                return;
            }

            int level;
            if (player.PermissionLevelLocal == Data.PlayerPermission.ArenaMod)
                level = (int)player.PermissionLevelLocal;
            else
                level = (int)player.PermissionLevel;
            /*
            if (!player._arena._name.StartsWith("Arena", StringComparison.OrdinalIgnoreCase)
                && !player._arena.IsPrivate)
                if (level < (int)Data.PlayerPermission.Mod)
                {
                    player.sendMessage(-1, "You can only use it in private arena's.");
                    return;
                }
            */
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
            if (!string.IsNullOrEmpty(payload))
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
            if (string.IsNullOrEmpty(payload))
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
            if (payload == "all" || (string.IsNullOrEmpty(payload) && recipient == null))
            {
                player._arena._bLocked = !player._arena._bLocked;
                player._arena.sendArenaMessage("Spec lock has been toggled" + (player._arena._bLocked ? " ON!" : " OFF!"));
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

        static public void allowspec(Player player, Player recipient, string payload, int bong)
        {
            player._arena._allowSpec = !player._arena._allowSpec;
            player.sendMessage(0, $"Force allow-spec is now toggled " + (player._arena._allowSpec ? "ON" : "OFF"));
        }

        /// <summary>
        /// Toggles stealth mode for mods, will prevent players from seeing them on lists
        /// </summary>
        static public void stealth(Player player, Player recipient, string payload, int bong)
        {
            if (player._developer || player._permissionTemp >= Data.PlayerPermission.GrantedPlayer)
            {
                player.sendMessage(-1, "You are not authorized to use this command.");
                return;
            }

            List<Player> audience = player._arena.Players.ToList();

            if (!player.IsStealth)
            {
                player._bIsStealth = true;

                foreach (Player person in audience)
                {
                    if (person == player)
                    {
                        continue;
                    }

                    //Their level is greater, allow them to see him/her
                    if (player.PermissionLevel > person.PermissionLevel)
                    {
                        Helpers.Object_PlayerLeave(person, player);
                        //Add here to spoof leave chat
                    }
                }
            }
            else
            {
                player._bIsStealth = false;

                foreach (Player person in audience)
                {
                    if (person != player && person.PermissionLevel < player.PermissionLevel)
                    {
                        Helpers.Object_Players(person, player);
                    }
                }
            }

            CS_Stealth<Data.Database> stealthReq = new CS_Stealth<Data.Database>();
            stealthReq.sender = player._alias;
            stealthReq.stealth = player.IsStealth;

            player._server._db.send(stealthReq);
        }

        /// <summary>
        /// Substitutes one player for another
        /// </summary>
        static public void substitute(Player player, Player recipient, string payload, int bong)
        {
            if (string.IsNullOrEmpty(payload))
            {
                player.sendMessage(-1, "Syntax: either *sub aliasComingOut:aliasGoingIn or :aliasComingOut:*sub aliasGoingIn");
                return;
            }

            if (player._arena._name.StartsWith("Arena", StringComparison.OrdinalIgnoreCase))
            {
                player.sendMessage(-1, "This command can only be used in non-public arenas.");
                return;
            }

            Team team1;
            Team team2;

            //First check if we are pm'ing someone
            if (recipient != null)
            {
                if (recipient.IsSpectator)
                {
                    player.sendMessage(-1, "The player you private messaged is already in spec.");
                    return;
                }

                Player target;
                if ((target = player._arena.getPlayerByName(payload)) == null)
                {
                    player.sendMessage(-1, "That player isn't here.");
                    return;
                }

                team1 = recipient._team;
                team2 = target._team;

                //Is he/she on a spectator team?
                if (target.IsSpectator)
                {   //Unspec them
                    target.unspec(team1);

                    //Was it the same team?
                    if (team1 == team2)
                    {   //Spec him/her then add him/her to the same team
                        recipient.spec();
                        team2.addPlayer(recipient);
                    }
                    else
                    {
                        recipient.spec();
                    }
                }
                else
                {   //Change team
                    team1.addPlayer(target);
                    team2.addPlayer(recipient);
                }

                string formatted = string.Format("{0} has been substituted for {1}.", target._alias, recipient._alias);
                player._arena.sendArenaMessage(formatted, bong);
                return;
            }

            //We aren't
            if (!payload.Contains(':'))
            {
                player.sendMessage(-1, "Syntax: *sub aliasComingOut:aliasGoingIn");
                return;
            }

            string[] param = payload.Split(':');
            Player comingOut, goingIn;
            if ((comingOut = player._arena.getPlayerByName(param[0])) == null)
            {
                player.sendMessage(-1, "That alias coming out doesn't exist in the arena.");
                return;
            }

            if ((goingIn = player._arena.getPlayerByName(param[1])) == null)
            {
                player.sendMessage(-1, "That alias going in doesn't exist in the arena.");
                return;
            }

            team1 = comingOut._team;
            team2 = goingIn._team;

            //Is he/she on a spectator team?
            if (goingIn.IsSpectator)
            {   //Unspec him/her
                goingIn.unspec(team1);

                //Was it the same team?
                if (team1 == team2)
                {
                    comingOut.spec();
                    team2.addPlayer(comingOut);
                }
                else
                {
                    comingOut.spec();
                }
            }
            else
            {   //Change team
                team1.addPlayer(goingIn);
                team2.addPlayer(comingOut);
            }

            string format = string.Format("{0} has been substituted for {1}.", goingIn._alias, comingOut._alias);
            player._arena.sendArenaMessage(format, bong);
        }

        /// <summary>
        /// Summons the specified player to yourself
        /// </summary>
        static public void summon(Player player, Player recipient, string payload, int bong)
        {	//Sanity checks
            if (recipient == null && string.IsNullOrWhiteSpace(payload))
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
                if (split[0].Equals("team") && (split.Count() == 1 || string.IsNullOrWhiteSpace(split[1])))
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
        /// Switches players to the opposite side
        /// </summary>
        static public void switchSides(Player player, Player recipient, string payload, int bong)
        {
            //Public arena?
            if (player._arena._bIsPublic && player.PermissionLevel < Data.PlayerPermission.Mod)
            {
                player.sendMessage(-1, "This command cannot be used in public arena's.");
                return;
            }

            //Do we have any teams to switch with?
            if (player._arena.ActiveTeams.Count() <= 1)
            {
                player.sendMessage(-1, "Cannot switch sides, there needs to be 2 or more active teams to switch with.");
                return;
            }

            //Do we have multiple teams?
            if (player._arena.ActiveTeams.Count() > 2)
            {
                if (recipient == null && string.IsNullOrWhiteSpace(payload))
                {
                    player.sendMessage(-1, "Which team do you want to switch with?");
                    player.sendMessage(0, "Either type the exact team name or pm the player to switch sides with.");
                    return;
                }
            }

            //Did we pm someone?
            if (recipient != null)
            {
                //Are we trying to switch sides with the spectator team?
                if (recipient._team.IsSpec)
                {
                    player.sendMessage(-1, "Cannot switch sides with the spectators.");
                    return;
                }

                //Is it a public team?
                if (!recipient._team.IsPublic)
                {
                    player.sendMessage(-1, "You can only switch sides with public teams.");
                    return;
                }

                //Is it the same team?
                if (recipient._team == player._team)
                {
                    player.sendMessage(-1, "Thats the team you are currently on.");
                    return;
                }

                //Get each team first
                Team teamA = player._team;
                Team teamB = recipient._team;

                //Get a list of each side first before switching
                List<Player> sideA = player._team.AllPlayers.ToList();
                List<Player> sideB = recipient._team.AllPlayers.ToList();
                int count = 0;

                //Check for a league zone
                if (teamA._name.Contains("- T"))
                {
                    string nameA = teamA._name.Replace("- T", "- C");
                    string nameB = teamB._name.Replace("- C", "- T");
                    teamA = player._arena.getTeamByName(nameA);
                    teamB = player._arena.getTeamByName(nameB);
                }
                else if (teamA._name.Contains("- C"))
                {
                    string nameA = teamA._name.Replace("- C", "- T");
                    string nameB = teamB._name.Replace("- T", "- C");
                    teamA = player._arena.getTeamByName(nameA);
                    teamB = player._arena.getTeamByName(nameB);
                }

                foreach (Player p in sideA)
                {
                    //Team maxed out?
                    if (teamA.MaxPlayers > 0 && count == teamA.MaxPlayers)
                    {
                        player.sendMessage(0, "The team is maxed out, you cannot switch anyone else.");
                        return;
                    }

                    //Add
                    teamA.addPlayer(p);

                    //Was the player just spectating?
                    if (p.IsSpectator)
                        //He was, dont count them
                        continue;

                    ++count;
                }
                count = 0;
                foreach (Player plyr in sideB)
                {
                    //Team maxed out?
                    if (teamB.MaxPlayers > 0 && count == teamB.MaxPlayers)
                    {
                        player.sendMessage(0, "The team is maxed out, you cannot switch anyone else.");
                        return;
                    }

                    //Add
                    teamB.addPlayer(plyr);

                    //Was the player just spectating?
                    if (plyr.IsSpectator)
                        //They were, dont count them
                        continue;

                    ++count;
                }
            }
            else //No recipient
            {
                //Lets set the defaults
                Team teamA = player._team;
                Team teamB = null;

                if (string.IsNullOrWhiteSpace(payload))
                {
                    //Lets assume the active teams want to switch sides
                    if (player._arena.ActiveTeams.Count() == 2)
                    {
                        teamB = player._arena.ActiveTeams.SingleOrDefault(t => t != player._team);
                    }
                    else //Ask them to choose instead
                    {
                        player.sendMessage(-1, "Which team do you want to switch with?");
                        player.sendMessage(0, "Either type the exact team name or pm the player to switch sides with.");
                        player.sendMessage(0, "Syntax: *switch <teamname> OR :player:*switch");
                        return;
                    }
                }
                else //They used a team name
                {
                    teamB = player._arena.getTeamByName(payload);
                }

                //Does the team exist?
                if (teamB == null)
                {
                    player.sendMessage(-1, "That team doesn't exist.");
                    return;
                }

                //Spectator team?
                if (teamB.IsSpec)
                {
                    player.sendMessage(-1, "Cannot switch sides with the spectators.");
                    return;
                }

                //Private team?
                if (!teamB.IsPublic)
                {
                    player.sendMessage(-1, "You can only switch sides with public teams.");
                    return;
                }

                //Same team?
                if (teamB == player._team)
                {
                    player.sendMessage(-1, "Thats the team you are currently on.");
                    return;
                }

                //Get a list of all the players on each
                List<Player> sideA = player._team.AllPlayers.ToList();
                List<Player> sideB = teamB.AllPlayers.ToList();
                int count = 0;
                //Check for a league zone
                if (teamA._name.Contains("- T"))
                {
                    string nameA = teamA._name.Replace("- T", "- C");
                    string nameB = teamB._name.Replace("- C", "- T");
                    teamA = player._arena.getTeamByName(nameA);
                    teamB = player._arena.getTeamByName(nameB);
                }
                else if (teamA._name.Contains("- C"))
                {
                    string nameA = teamA._name.Replace("- C", "- T");
                    string nameB = teamB._name.Replace("- T", "- C");
                    teamA = player._arena.getTeamByName(nameA);
                    teamB = player._arena.getTeamByName(nameB);
                }

                foreach (Player p in sideA)
                {
                    //Is the team maxed out?
                    if (teamA.MaxPlayers > 0 && count == teamA.MaxPlayers)
                    {
                        player.sendMessage(0, "The team is maxed out, you cannot switch anyone else.");
                        return;
                    }

                    //Add
                    teamA.addPlayer(p);

                    //Was the player just spectating?
                    if (p.IsSpectator)
                        //They were, dont count them
                        continue;

                    ++count;
                }
                count = 0;
                foreach (Player plyr in sideB)
                {
                    //Team maxed out?
                    if (teamB.MaxPlayers > 0 && count == teamB.MaxPlayers)
                    {
                        player.sendMessage(0, "The team is maxed out, you cannot switch anyone else.");
                        return;
                    }

                    //Add
                    teamB.addPlayer(plyr);

                    //Was the player just spectating?
                    if (plyr.IsSpectator)
                        //They were, dont count them
                        continue;

                    ++count;
                }
            }
        }

        /// <summary>
        /// Puts another player, or yourself, on a specified team
        /// </summary>
        static public void team(Player player, Player recipient, string payload, int bong)
        {	//Sanity checks
            if (string.IsNullOrEmpty(payload))
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
            if (string.IsNullOrEmpty(payload))
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
            if (string.IsNullOrEmpty(payload) || !payload.Contains(','))
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
            if (string.IsNullOrEmpty(payload))
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
                    {   //Split the payload up if it contains a colon
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
                        player._arena.setTicker(1, player._arena.playtimeTickerIdx, minutes * 6000 + seconds * 100, "Time Remaining: ", delegate ()
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
            if (!string.IsNullOrEmpty(payload))
            {	//Find the team
                Team newTeam = player._arena.getTeamByName(payload);

                string temp = payload.ToLower();
                if (temp.Equals("spec") || temp.Equals("spectator") || newTeam == null)
                {
                    player.sendMessage(-1, "Invalid team specified");
                    return;
                }

                //Is he on a spectator team?
                if (target.IsSpectator)
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
                if (string.IsNullOrWhiteSpace(payload))
                    //Simply warp to the recipient
                    player.warp(recipient);
                else
                {
                    //Did we type it correctly?
                    if (payload.Length < 2)
                    {
                        player.sendMessage(-1, "That is not a valid coord.");
                        return;
                    }

                    //Are we dealing with coords or exacts?
                    payload = payload.ToLower();
                    if (payload[0] >= 'a' && payload[0] <= 'z')
                    {   //Coords
                        int x = (((int)payload[0]) - ((int)'a')) * 16 * 80;
                        int y = 0;
                        try
                        {
                            y = Convert.ToInt32(payload.Substring(1)) * 16 * 80;
                        }
                        catch
                        {
                            player.sendMessage(-1, "That is not a valid coord.");
                            return;
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

                        string[] exacts = payload.Split(',');
                        int x = 0;
                        int y = 0;
                        try
                        {
                            x = Convert.ToInt32(exacts[0]) * 16;
                            y = Convert.ToInt32(exacts[1]) * 16;
                        }
                        catch
                        {
                            player.sendMessage(-1, "That is not a valid exact coordinate.");
                        }
                        recipient.warp(x, y);
                    }
                }
            }
            else
            {	//We must have a payload for this
                if (string.IsNullOrWhiteSpace(payload))
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

                //Did we type it correctly?
                if (payload.Length < 2)
                {
                    player.sendMessage(-1, "That is not a valid coord.");
                    return;
                }

                //Are we dealing with coords or exacts?
                payload = payload.ToLower();
                if (payload[0] >= 'a' && payload[0] <= 'z')
                {   //Coords
                    int x = (((int)payload[0]) - ((int)'a')) * 16 * 80;
                    int y = 0;
                    try
                    {
                        y = Convert.ToInt32(payload.Substring(1)) * 16 * 80;
                    }
                    catch
                    {
                        player.sendMessage(-1, "That is not a valid coord.");
                        return;
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

                    string[] exacts = payload.Split(',');
                    int x = Convert.ToInt32(exacts[0]) * 16;
                    int y;

                    //Are we trying to summon a team?
                    if (exacts[1].Contains(' '))
                    {
                        //Lets get our first number only
                        //This is a coord, any other number after could be a potential
                        //team name.
                        string Y = "";
                        foreach (char c in exacts[1])
                        {
                            if (c == ' ')
                                break;
                            Y += c;
                        }
                        y = Convert.ToInt32(Y) * 16;

                        string syntax = (exacts[1].Substring(exacts[1].IndexOf(' '))).Trim();
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
                        y = Convert.ToInt32(exacts[1]) * 16;

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
            if (string.IsNullOrWhiteSpace(payload) || !payload.ToLower().Contains("yes"))
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
                recipient.setDefaultSkill();

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
                            p.setDefaultSkill();
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
                    recipient.setDefaultSkill();
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
            if (string.IsNullOrEmpty(payload))
                player.sendMessage(-1, "Message can not be empty.");
            else
            {
                //Snap color code before sending
                Regex reg = new Regex(@"^(~|!|@|#|\$|%|\^|&|\*)", RegexOptions.IgnoreCase);
                Match match = reg.Match(payload);

                if (match.Success)
                {
                    payload = string.Format("{0}[Zone] {1}", match.Groups[0].Value, payload.Remove(0, 1));
                }
                else
                {
                    payload = string.Format("[Zone] {0}", payload);
                }

                var arenas = player._server._arenas.Values.Where(a => a != null);

                foreach (var arena in arenas)
                {
                    arena.sendArenaMessage(payload, bong);
                }
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

            if (recipient == null && string.IsNullOrEmpty(payload))
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

                if (string.IsNullOrEmpty(payload))
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
                    recipient.sendMessage(0, string.Format("You are being banned for {0} minutes.", minutes));
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
                    if (!string.IsNullOrEmpty(param[1]))
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
            if ((recipient = player._server.getPlayer(alias)) != null)
            {
                if (recipient.PermissionLevel >= player.PermissionLevel && !player._admin)
                {
                    player.sendMessage(-1, "You cannot ban someone equal or higher than you.");
                    return;
                }
                recipient.disconnect();
            }

            player.sendMessage(0, string.Format("You are banning {0} for {1} minutes.", alias, minutes));

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

        static public void banremove(Player player, Player recipient, string payload, int bong)
        {
            if (player._server.IsStandalone)
            {
                player.sendMessage(-1, "Server is currently in stand-alone mode.");
                return;
            }

            var alias = recipient != null ? recipient._alias : payload;

            if (String.IsNullOrWhiteSpace(alias))
            {
                player.sendMessage(-1, "Coommand used incorrectly, please specify target alias.");
                return;
            }

            var cmd = new CS_Unban<Data.Database>();

            cmd.banType = CS_Unban<Data.Database>.BanType.account;
            cmd.alias = alias;
            cmd.sender = player._alias;

            player._server._db.send(cmd);
        }

        /// <summary>
        /// Shows a ban list for a player
        /// </summary>
        static public void banlist(Player player, Player recipient, string payload, int bong)
        {
            //Sanity check
            if (player._server.IsStandalone)
            {
                player.sendMessage(-1, "Server is currently in stand-alone mode.");
                return;
            }

            if (recipient == null && string.IsNullOrEmpty(payload))
            {
                player.sendMessage(-1, "Syntax: either :alias:*banlist or *banlist alias");
                return;
            }

            //Relay it to the database
            CS_ChatQuery<Data.Database> ban = new CS_ChatQuery<Data.Database>();
            ban.queryType = CS_ChatQuery<Data.Database>.QueryType.ban;
            ban.sender = player._alias;
            ban.payload = recipient != null ? recipient._alias : payload;

            player._server._db.send(ban);
        }

        /// <summary>
        /// Bans a player from a specific zone
        /// </summary>
        static public void zoneban(Player player, Player recipient, string payload, int bong)
        {
            //Sanity check
            if (player._server.IsStandalone)
            {
                player.sendMessage(-1, "Server is currently in stand-alone mode.");
                return;
            }

            if (recipient == null && string.IsNullOrEmpty(payload))
            {
                player.sendMessage(-1, "Syntax: either ::*zoneban time:reason(optional) or *zoneban alias time:reason(optional)");
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

                if (string.IsNullOrEmpty(payload))
                {
                    player.sendMessage(-1, "Syntax: ::*zoneban time:reason(Optional)");
                    return;
                }

                alias = recipient._alias.ToString();

                if (payload.Contains(':'))
                {
                    //Lets snag both the time and reason here
                    param = payload.Split(':');

                    if (!Int32.TryParse(param[0], out number))
                    {
                        player.sendMessage(-1, "Syntax: ::*zoneban time:reason(optional) Note: if using a reason, use : between time and reason");
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
                    if (!string.IsNullOrEmpty(param[1]))
                        reason = param[1];
                }
                else //Just check for a time
                {
                    if (!Int32.TryParse(payload, out number))
                    {
                        player.sendMessage(-1, "That is not a valid time. Syntax ::*zoneban time:reason(Optional)");
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
                    recipient.sendMessage(0, string.Format("You are being banned for {0} minutes.", minutes));
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
                        player.sendMessage(-1, "Bad Syntax, use *zoneban \"alias\" time:reason(optional)");
                        return;
                    }
                }
                else
                {
                    player.sendMessage(-1, "Syntax: *zoneban \"alias\" time:reason(Optional) Note: Use quotations around the alias");
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
                    if (!string.IsNullOrEmpty(param[1]))
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
            if ((recipient = player._server.getPlayer(alias)) != null)
            {
                if (recipient.PermissionLevel >= player.PermissionLevel && !player._admin)
                {
                    player.sendMessage(-1, "You cannot ban someone equal or higher than you.");
                    return;
                }
                recipient.disconnect();
            }

            player.sendMessage(0, string.Format("You are banning {0} for {1} minutes.", alias, minutes));

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
        /// Removes a zone ban for the player.
        /// </summary>
        static public void zonebanremove(Player player, Player recipient, string payload, int bong)
        {
            if (player._server.IsStandalone)
            {
                player.sendMessage(-1, "Server is currently in stand-alone mode.");
                return;
            }

            var alias = recipient != null ? recipient._alias : payload;

            if (String.IsNullOrWhiteSpace(alias))
            {
                player.sendMessage(-1, "Coommand used incorrectly, please specify target alias.");
                return;
            }

            var cmd = new CS_Unban<Data.Database>();

            cmd.banType = CS_Unban<Data.Database>.BanType.zone;
            cmd.alias = alias;
            cmd.sender = player._alias;

            player._server._db.send(cmd);
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
            if (!string.IsNullOrEmpty(payload) && payload.Equals("all", StringComparison.CurrentCultureIgnoreCase))
            {
                foreach (Player p in player._arena.Players.ToList())
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
        /// Bans or just kicks the player from a specific arena - if granted players, privately owned only
        /// </summary>
        static public void arenaban(Player player, Player recipient, string payload, int bong)
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

            if (level < (int)Data.PlayerPermission.Mod && player._arena._name.StartsWith("Arena", StringComparison.OrdinalIgnoreCase))
            {
                player.sendMessage(-1, "You can only use it in non-public arena's.");
                return;
            }

            if (rLevel >= level && !player._admin)
            {
                player.sendMessage(-1, "No.");
                return;
            }

            if (recipient != null && string.IsNullOrEmpty(payload))
            {
                //Assume they just want to kick them out of the arena temporarily
                recipient.sendMessage(-1, "You have been kicked out of the arena.");
                recipient.disconnect();
                player.sendMessage(0, string.Format("You have kicked player {0} from the arena.", recipient._alias));
                return;
            }

            if (recipient == null && string.IsNullOrEmpty(payload))
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
            recipient.sendMessage(-1, string.Format("You have been kicked from the arena{0}", (minutes > 0) ? string.Format(" for {0} minutes.", minutes) : "."));
            recipient._arena._blockedList.Add(recipient._alias, DateTime.Now.AddMinutes(minutes));
            recipient.disconnect();

            player.sendMessage(0, string.Format("You have kicked player {0}{1}", recipient._alias, (minutes > 0) ? string.Format(" for {0} minutes.", minutes) : "."));
        }

        static public void arenabanremove(Player player, Player recipient, string payload, int bong)
        {
            if (player._server.IsStandalone)
            {
                player.sendMessage(0, "Server is currently in stand-alone mode.");
                return;
            }

            var alias = recipient != null ? recipient._alias : payload.Trim();

            if (!player._arena._blockedList.ContainsKey(alias))
            {
                player.sendMessage(-1, $"Alias {alias} isn't on the kick list.");
                return;
            }

            player._arena._blockedList.Remove(alias);
            player.sendMessage(0, $"Alias {alias} has been removed from the kick list.");
        }



        static public void addswear(Player player, Player recipient, string payload, int bong)
        {

        }
        static public void removeswear(Player player, Player recipient, string payload, int bong)
        {

        }

        static public void allowprivate(Player player, Player recipient, string payload, int bong)
        {
            if (player._arena._allowPrivate)
                player._arena._allowPrivate = false;
            else
                player._arena._allowPrivate = true;

            player._arena.sendArenaMessage("Private teams are now" + (player._arena._allowPrivate ? " allowed!" : " not allowed!"), 0);
        }

        static public void maxprivfreq(Player player, Player recipient, string payload, int bong)
        {
            if (player._arena._maxPerPrivateTeam != int.Parse(payload))
            {
                player._arena._maxPerPrivateTeam = int.Parse(payload);
            }
            player._arena.sendArenaMessage("Private teams max players is now set to " + player._arena._maxPerPrivateTeam, 0);
        }

        static public void maxperfreq(Player player, Player recipient, string payload, int bong)
        {
            if (player._arena._maxPerteam != int.Parse(payload))
            {
                player._arena._maxPerteam = int.Parse(payload);
            }
            player._arena.sendArenaMessage("Public teams max players is now set to " + player._arena._maxPerteam, 0);
        }

        static public void allowprize(Player player, Player recipient, string payload, int bong)
        {
            if (player._arena._allowprize)
                player._arena._allowprize = false;
            else
                player._arena._allowprize = true;

            player._arena.sendArenaMessage("Prizing of items is now" + (player._arena._allowprize ? " allowed!" : " not allowed!"), 0);
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
                InfServer.Data.PlayerPermission.GrantedPlayer, true);

            yield return new HandlerDescriptor(addball, "addball",
                "Adds a ball to the arena.",
                "*addball",
               InfServer.Data.PlayerPermission.GrantedPlayer, true);

            yield return new HandlerDescriptor(allow, "allow",
                "Lists, Adds or Removes a player from a private arena list.",
                "*allow list OR *allow alias",
                InfServer.Data.PlayerPermission.Mod, true);

            yield return new HandlerDescriptor(arena, "arena",
                "Send a arena-wide system message.",
                "*arena message",
               InfServer.Data.PlayerPermission.GrantedPlayer, true);

            yield return new HandlerDescriptor(arenalock, "arenalock",
                "Locks or unlocks an arena from outsiders. Note: will kick out any non powered players in spec and only mods or admins can join/stay.",
                "*arenalock",
                InfServer.Data.PlayerPermission.Mod, true);

            yield return new HandlerDescriptor(maxpop, "maxpop",
                "Gets or sets the maximum player count (excluding mods/admins) in the arena. Does not kick existing players out. 0 value resets.",
                "*maxpop [optional: value or 0]",
                Data.PlayerPermission.Mod, true);

            yield return new HandlerDescriptor(auth, "auth",
                "Log in during Stand-Alone Mode",
                "*auth password");

            yield return new HandlerDescriptor(banlist, "banlist",
                "Shows a list of bans a player has",
                "*banlist alias or :alias:*banlist",
                InfServer.Data.PlayerPermission.Mod, false);

            yield return new HandlerDescriptor(cash, "cash",
                "Prizes specified amount of cash to target player",
                "*cash [amount] or ::*cash [amount]",
                InfServer.Data.PlayerPermission.GrantedPlayer, true);

            yield return new HandlerDescriptor(dc, "dc",
                "Kicks a player from the server",
                "*dc reason (optional) or dc all",
                InfServer.Data.PlayerPermission.Mod, true);

            yield return new HandlerDescriptor(experience, "experience",
                "Prizes specified amount of experience to target player",
                "*experience [amount] or ::*experience [amount]",
                InfServer.Data.PlayerPermission.GrantedPlayer, true);

            yield return new HandlerDescriptor(flagSpawn, "flagspawn",
                "Spawns all or a specific flag at either default location, exact, or coordinate",
                "*flagspawn <all|id|name>:<default|coord|exacts>",
                InfServer.Data.PlayerPermission.GrantedPlayer, true);

            yield return new HandlerDescriptor(find, "find",
                "Searches for an alias and returns if they are possibly on another name",
                "*find [alias]",
                InfServer.Data.PlayerPermission.Mod, true);

            yield return new HandlerDescriptor(findItem, "finditem",
                "Finds an item within our zone",
                "*finditem [itemID or item name]",
                InfServer.Data.PlayerPermission.GrantedPlayer, true);

            yield return new HandlerDescriptor(getball, "getball",
                "Gets a ball.",
                "*getball (gets ball ID 0) or *getball # (gets a specific ball ID)",
                InfServer.Data.PlayerPermission.GrantedPlayer, true);

            yield return new HandlerDescriptor(global, "global",
               "Sends a global message to every zone connected to current database",
               "*global [message]",
               InfServer.Data.PlayerPermission.Mod, false);

            yield return new HandlerDescriptor(warp, "goto",
                "Warps you to a specified player, coordinate or exact coordinate. Alternatively, you can warp other players to coordinates or exacts.",
                ":alias:*goto or *goto [alias] or *goto A4 (optional team name) or *goto 123,123 (optional team name)",
                InfServer.Data.PlayerPermission.GrantedPlayer, true);

            yield return new HandlerDescriptor(grant, "grant",
                "Gives arena privileges to a player",
                ":player:*grant or *grant [alias]",
                InfServer.Data.PlayerPermission.GrantedPlayer, true);

            yield return new HandlerDescriptor(moderator, "moderator",
                "Gives moderator arena privileges to a player",
                ":player:*moderator or *moderator [alias]",
                InfServer.Data.PlayerPermission.Mod, true);

            yield return new HandlerDescriptor(helpcall, "helpcall",
                "helpcall shows all helpcalls from all zones",
                "*helpcall  OR *helpcall page - to show a specific page",
                InfServer.Data.PlayerPermission.Mod, false);

            yield return new HandlerDescriptor(arenaban, "arenaban",
                "Bans a player from an arena",
                ":player:*arenaban minutes(Optional) or *arenaban alias minutes(optional)",
                InfServer.Data.PlayerPermission.Mod, true);

            yield return new HandlerDescriptor(arenabanremove, "arenabanremove",
                "Removes an active kick for the given alias.",
                ":player:*arenabanremove or *arenabanremove alias",
                InfServer.Data.PlayerPermission.Mod, true);

            yield return new HandlerDescriptor(zoneban, "zoneban",
                "Blocks a player from a specific zone - Note: make sure you are in the zone you want to ban from",
                "*zoneban alias minutes:reason(optional) or :player:*zoneban minutes:reason(optional)",
               InfServer.Data.PlayerPermission.Mod, true);

            yield return new HandlerDescriptor(zonebanremove, "zonebanremove",
                "Blocks a player from a specific zone - Note: make sure you are in the zone you want to ban from",
                "*zonebanremove alias or :player:*zonebanremove",
               InfServer.Data.PlayerPermission.Mod, true);

            yield return new HandlerDescriptor(ban, "ban",
                "Bans a player from all zones",
                "*ban alias minutes:reason(optional) or :player:*ban minutes:reason(optional)",
                InfServer.Data.PlayerPermission.SuperMod, false);

            yield return new HandlerDescriptor(banremove, "banremove",
                "Removes a ban from the given alias.",
                "*banremove alias or :player:*banremove",
                InfServer.Data.PlayerPermission.SuperMod, false);

            yield return new HandlerDescriptor(speclock, "lock",
                "*lock will toggle arena lock on or off, using lock in a pm will lock and spec or unlock a player in spec",
                "*lock OR :target:*lock",
                InfServer.Data.PlayerPermission.GrantedPlayer, true);

            yield return new HandlerDescriptor(permit, "permit",
                "Permits target player to enter, leave, or read an allowed list of a permission-only zone.",
                "permit [alias] (Type it again to remove them) *permit list",
               InfServer.Data.PlayerPermission.Mod, true);

            yield return new HandlerDescriptor(poll, "poll",
                "Starts a poll for a question asked, *poll cancel to stop one, *poll end to end one",
                "*poll question(Optional: *poll question:timer)",
                InfServer.Data.PlayerPermission.GrantedPlayer, true);

            yield return new HandlerDescriptor(prize, "prize",
                "Spawns an item on the ground or in a player's inventory",
                "*prize item:amount or ::*prize item:amount",
                InfServer.Data.PlayerPermission.GrantedPlayer, true);

            yield return new HandlerDescriptor(profile, "profile",
                "Displays a player's inventory.",
                "/*profile or :player:*profile or *profile",
                InfServer.Data.PlayerPermission.GrantedPlayer, true);

            yield return new HandlerDescriptor(removeball, "removeball",
                "Removes a ball from the arena.",
                "*removeball (by itself, removes ballID 0), *removeball # (removes that ball id)",
                InfServer.Data.PlayerPermission.GrantedPlayer, true);

            yield return new HandlerDescriptor(scramble, "scramble",
                "Scrambles an arena, use on/off to set the arena to scramble automatically.",
                "::*scramble [on/off(optional)]",
                InfServer.Data.PlayerPermission.GrantedPlayer, true);

            yield return new HandlerDescriptor(silenced, "silenced",
               "Returns list of silenced players",
               "*silenced",
               InfServer.Data.PlayerPermission.Mod, true);

            yield return new HandlerDescriptor(silence, "silence",
               "Toggles a players ability to use the messaging system entirely across the zone",
               ":alias:*silence",
               InfServer.Data.PlayerPermission.Mod, true);

            yield return new HandlerDescriptor(skill, "skill",
                "Gives or sets a skill to a player",
                "::*skill id:amount",
                InfServer.Data.PlayerPermission.GrantedPlayer, true);

            yield return new HandlerDescriptor(spec, "spec",
                "Puts a player into spectator mode, optionally on a specified team.",
                "*spec or ::*spec or ::*spec [team]",
                InfServer.Data.PlayerPermission.GrantedPlayer, true);

            yield return new HandlerDescriptor(spectate, "spectate",
                "Forces a player or the whole arena to spectate the specified player.",
                "Syntax: ::*spectate [player] or *spectate [player]",
                InfServer.Data.PlayerPermission.ManagerSysop, false);

            yield return new HandlerDescriptor(specquiet, "specquiet",
                "Toggles chatting to spectator only.",
                "Syntax: :alias:*specquiet (for a specific player) or *specquiet (toggles the whole arena)",
                InfServer.Data.PlayerPermission.GrantedPlayer, true);

            yield return new HandlerDescriptor(allowspec, "allowspec",
                "Toggles the ability for players to disable being spectated. Will not clear player preference of those who already set it, but it will ignore the preference while it is toggled on.",
                "Syntax: *allowspec",
                InfServer.Data.PlayerPermission.Mod, true);

            yield return new HandlerDescriptor(stealth, "stealth",
                "Toggles stealth mode, mods become invisible to arena's",
                "*stealth",
                InfServer.Data.PlayerPermission.Mod, false);

            yield return new HandlerDescriptor(substitute, "sub",
                "Substitutes one player for another.",
                "*sub aliasComingOut:aliasGoingIn or :aliasComingOut:*sub aliasGoingIn",
                InfServer.Data.PlayerPermission.GrantedPlayer, true);

            yield return new HandlerDescriptor(summon, "summon",
                "Summons a specified player to your location, or all players to your location.",
                "::*summon, *summon alias, *summon team [teamname], or *summon all",
                InfServer.Data.PlayerPermission.GrantedPlayer, true);

            yield return new HandlerDescriptor(switchSides, "switch",
                "Switches sides with another team.",
                "*switch, ::*switch or *switch [teamname]",
                InfServer.Data.PlayerPermission.GrantedPlayer, true);

            yield return new HandlerDescriptor(team, "team",
                "Puts another player, or yourself, on a specified team",
                "*team [teamname] or ::*team [teamname]",
                InfServer.Data.PlayerPermission.GrantedPlayer, true);

            yield return new HandlerDescriptor(teamname, "teamname",
                "Renames public team names in the arena",
                "*teamname [teamname1,teamname2,...]",
                InfServer.Data.PlayerPermission.GrantedPlayer, true);

            yield return new HandlerDescriptor(ticker, "ticker",
               "Sets a ticker at the specified index with color/timer/message",
               "*ticker [message],[index],[color=optional],[timer=optional]",
               InfServer.Data.PlayerPermission.GrantedPlayer, true);

            yield return new HandlerDescriptor(timer, "timer",
                "Sets a ticker to display a timer",
                "Syntax: *timer xx or *timer xx:xx",
                InfServer.Data.PlayerPermission.GrantedPlayer, true);

            yield return new HandlerDescriptor(unspec, "unspec",
                "Takes a player out of spectator mode and puts him on the specified team.",
                "*unspec [team] or ::*unspec [team] or ::*unspec ..",
                InfServer.Data.PlayerPermission.GrantedPlayer, true);

            yield return new HandlerDescriptor(warp, "warp",
                "Warps you to a specified player, coordinate or exact coordinate. Alternatively, you can warp other players to coordinates or exacts.",
                "::*warp, *warp alias, *warp A4 (optional team name) or *warp 123,123 (optional team name)",
                InfServer.Data.PlayerPermission.GrantedPlayer, true);

            yield return new HandlerDescriptor(watchmod, "watchmod",
                "Toggles viewing mod commands on or off",
                "*watchmod",
                InfServer.Data.PlayerPermission.GrantedPlayer, true);

            yield return new HandlerDescriptor(wipe, "wipe",
                "Wipes a character (or all) within the current zone",
                "*wipe all yes, *wipe [alias] yes, :alias:*wipe yes",
                InfServer.Data.PlayerPermission.ManagerSysop, false);

            yield return new HandlerDescriptor(zone, "zone",
                "Send a zone-wide system message.",
                "*zone message",
               InfServer.Data.PlayerPermission.Mod, true);

            yield return new HandlerDescriptor(allowprivate, "allowprivate",
                "Toggles if arena allows private teams.",
                "*allowprivate",
               InfServer.Data.PlayerPermission.Mod, true);

            yield return new HandlerDescriptor(maxprivfreq, "maxprivfreq",
                "Set the maximum amount of players per private teams.",
                "*maxprivfreq number",
               InfServer.Data.PlayerPermission.Mod, true);

            yield return new HandlerDescriptor(maxperfreq, "maxperfreq",
                "Set the maximum amount of players per public team.",
                "*maxperfreq number",
               InfServer.Data.PlayerPermission.Mod, true);

            yield return new HandlerDescriptor(allowprize, "allowprize",
                "Toggles if arena allows prize.",
                "*allowprize",
               InfServer.Data.PlayerPermission.Mod, true);
        }
    }
}
