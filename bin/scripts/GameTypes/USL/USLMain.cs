using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using InfServer.Logic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;

using Assets;

namespace InfServer.Script.GameType_USL
{   // Script Class
    /// Provides the interface between the script and arena
    ///////////////////////////////////////////////////////
    class Script_USL : Scripts.IScript
    {   ///////////////////////////////////////////////////
        // Member Variables
        ///////////////////////////////////////////////////
        #region Vars
        //Pointers
        private Arena _arena;
        private CfgInfo _config;
        private GamePlay _gamePlay;

        //Poll variables
        private int _lastGameCheck;         //Tick at which we checked for game availability
        private int _tickGameStarting;      //Tick at which the game began starting (0 == not initiated)
        private int _tickGameStarted;       //Tick at which the game actually started (0 == stopped)
        private int _lastTickerUpdate;      //Tick at which the scoreboard was last updated
        private int _tickVotingStarted;     //Tick at which players started voting
        private int _lastKillStreakUpdate;  //Tick at which a players kill streak started

        //Misc variables
        private int _tickStartDelay;
        private int _minPlayers;            //Do we have the # of min players to start a game?
        private Dictionary<string, List<int>> _eventVoting;
        private int _votingTime;            //Our voting countdown timer
        private bool _votingEnded = false;
        public event Action EventOff;       //Turns off all events when *event off is called
        #endregion

        #region Game Functions
        ///////////////////////////////////////////////////
        // Member Functions
        ///////////////////////////////////////////////////
        /// <summary>
        /// Performs script initialization
        /// </summary>
        public bool init(IEventObject invoker)
        {   //Populate our variables
            _arena = invoker as Arena;
            _config = _arena._server._zoneConfig;

            _arena.playtimeTickerIdx = 3; //Sets the global index for our ticker
            _gamePlay = new GamePlay(_arena);
            _gamePlay.Initiate();
            _minPlayers = _config.deathMatch.minimumPlayers;
            _eventVoting = new Dictionary<string, List<int>>();
            _votingTime = _gamePlay.VotingTime;

            //Allow voting here?
            if (!_arena._bIsPublic)
            {
                _gamePlay.Voting = false;
                _gamePlay.Events = false;
                _gamePlay.SpawnEvent = false;
            }

            return true;
        }

        /// <summary>
        /// Allows the script to maintain itself
        /// </summary>
        public bool poll()
        {   //Should we check gamestate yet?
            int now = Environment.TickCount;
            if (now - _lastGameCheck <= Arena.gameCheckInterval)
                return true;
            _lastGameCheck = now;

            //Do we have enough players?
            int playing = _arena.PlayerCount;
            if (_arena._bGameRunning && playing < _minPlayers)
            {
                //Stop the game and reset voting
                _arena.gameEnd();
                _eventVoting.Clear();
            }

            if (playing < _minPlayers)
            {
                _tickGameStarting = 0;
                _votingEnded = false;
                _arena.setTicker(1, 3, 0, "Not Enough Players");
            }

            //Update our gamePlay
            _gamePlay.Poll(now);

            //Do we have enough to start a game?
            if (!_arena._bGameRunning && _tickGameStarting == 0 && playing >= _minPlayers)
            {   //Great! Get GOING!
                _tickGameStarting = now;
                Vote();

                //If this isnt a league overtime match, lets start a regular game
                //otherwise wait till a ref starts the match using *startgame
                if (_gamePlay._gameType != Settings.GameTypes.LEAGUEOVERTIME)
                {
                    _arena.setTicker(1, 3, _config.deathMatch.startDelay * 100, "Next game: ",
                        delegate ()
                        {   //Trigger the game start
                            _arena.gameStart();
                        }
                    );
                }
            }

            //Has voting started?
            if (_tickVotingStarted > 0)
            {
                if (now - _tickVotingStarted >= 1000)
                {
                    _tickVotingStarted = now;
                    _votingTime--;

                    if (_eventVoting.Count > 0)
                    {
                        var voteByName = _eventVoting.Select(x => String.Format("{0}({1})", x.Key, x.Value.Count));
                        _arena.setTicker(0, 0, 0,
                            delegate (Player p)
                            {
                                return "Event Votes: " + String.Join(", ", voteByName) + " | Voting Time Left: " + _votingTime;
                            }
                        );
                    }
                }

                //Lets reset and complete
                if (_votingTime <= 0)
                {
                    _tickVotingStarted = 0;
                    _votingTime = _gamePlay.VotingTime;
                    CompleteVoting();
                }
            }

            return true;
        }

        /// <summary>
        /// Called when the game begins
        /// </summary>
        [Scripts.Event("Game.Start")]
        public bool gameStart()
        {   //We've started!
            _tickGameStarting = 0;
            _tickGameStarted = Environment.TickCount;

            //Was an event turned off?
            if (EventOff != null)
            {
                EventOff();
                EventOff -= EventOff;
            }

            return _gamePlay.GameStart();
        }

        /// <summary>
        /// Called when the game ends
        /// </summary>
        [Scripts.Event("Game.End")]
        public bool gameEnd()
        {   //Game finished, perhaps start a new one?
            _arena.sendArenaMessage("Game Over!");

            _tickGameStarted = 0;
            _tickGameStarting = 0;
            _votingEnded = false;

            return _gamePlay.GameEnd();
        }

        /// <summary>
        /// Called to reset the game state
        /// </summary>
        [Scripts.Event("Game.Reset")]
        public bool gameReset()
        {	//Game reset, perhaps start a new one
            _tickGameStarted = 0;
            _tickGameStarting = 0;
            _gamePlay.victoryTeam = null;

            return true;
        }

        /// <summary>
        /// Called when the statistical breakdown is displayed
        /// </summary>
        [Scripts.Event("Player.Breakdown")]
        public bool individualBreakdown(Player from, bool bCurrent)
        {
            _gamePlay.individualBreakdown(from, bCurrent);
            return true;
        }
        #endregion

        #region Player Events
        /// <summary>
        /// Triggered when an explosion happens from a projectile a player fired
        /// </summary>
        [Scripts.Event("Player.Explosion")]
        public bool playerExplosion(Player from, ItemInfo.Projectile usedWep, short posX, short posY, short posZ)
        {
            _gamePlay.playerPlayerExplosion(from, usedWep);
            return true;
        }

        /// <summary>
        /// Triggers when a repair item is used
        /// </summary>
        [Scripts.Event("Player.Repair")]
        public bool playerPlayerRepair(Player player, ItemInfo.RepairItem item, UInt16 target, short posX, short posY)
        {
            _gamePlay.PlayerRepair(player, item);
            return true;
        }

        /// <summary>
        /// Triggered when one player has killed another
        /// </summary>
        [Scripts.Event("Player.PlayerKill")]
        public bool playerPlayerKill(Player victim, Player killer)
        {
            _gamePlay.playerPlayerKill(victim, killer);
            return true;
        }

        /// <summary>
        /// Triggered when a player has died, by any means
        /// </summary>
        /// <remarks>killer may be null if it wasn't a player kill</remarks>
        [Scripts.Event("Player.Death")]
        public bool playerDeath(Player victim, Player killer, Helpers.KillType killType, CS_VehicleDeath update)
        {
            _gamePlay.playerDeath(victim, killer, killType, update);
            return true;
        }

        /// <summary>
        /// Triggered when a player has spawned
        /// </summary>
        [Scripts.Event("Player.Spawn")]
        public bool playerSpawn(Player player, bool death)
        {
            _gamePlay.playerSpawn(player, death, _tickGameStarted > 0 ? true : false);
            return true;
        }

        /// <summary>
        /// Triggered when a player dies to a bot
        /// </summary>
        [Scripts.Event("Player.BotKill")]
        public bool botKill(Player victim, Bot killer)
        {
            if (_tickGameStarted > 0)
                _gamePlay.botKill(victim, killer);
            return true;
        }

        /// <summary>
        /// Triggered when a bot dies to a player
        /// </summary>
        [Scripts.Event("Bot.Death")]
        public bool botDeath(Bot victim, Player killer, int weaponID)
        {
            if (_tickGameStarted > 0)
                _gamePlay.botDeath(victim, killer, weaponID);
            return true;
        }

        /// <summary>
        /// Called when the player successfully joins the game
        /// </summary>
        [Scripts.Event("Player.Enter")]
        public void playerEnter(Player player)
        {
            _gamePlay.playerEnter(player);
        }

        /// <summary>
        /// Triggered when a player wants to unspec and join the game
        /// </summary>
        [Scripts.Event("Player.JoinGame")]
        public bool playerJoinGame(Player player)
        {
            return _gamePlay.playerJoinGame(player);
        }

        /// <summary>
        /// Called when a player enters the arena
        /// </summary>
        [Scripts.Event("Player.EnterArena")]
        public void playerEnterArena(Player player)
        {
            _gamePlay.playerEnterArena(player);
        }

        /// <summary>
        /// Called when the player successfully leaves the game
        /// </summary>
        [Scripts.Event("Player.Leave")]
        public void playerLeave(Player player)
        {
            _gamePlay.playerLeave(player, _tickGameStarted > 0 ? true : false);
        }

        /// <summary>
        /// Called when a player leaves the arena
        /// </summary>
        [Scripts.Event("Player.LeaveArena")]
        public void playerLeaveArena(Player player)
        {
            _gamePlay.playerLeaveArena(player, _tickGameStarted > 0 ? true : false);
        }

        /// <summary>
        /// Called when someone tries to pick up an item
        /// </summary>
        [Scripts.Event("Player.ItemPickup")]
        public bool playerItemPickup(Player player, Arena.ItemDrop drop, ushort quantity)
        {
            return _gamePlay.playerItemPickup(player, drop, quantity);
        }

        /// <summary>
        /// Called when a player successfully changes their class
        /// </summary>
        [Scripts.Event("Shop.SkillPurchase")]
        public void playerSkillPurchase(Player player, SkillInfo skill)
        {
            _gamePlay.playerSkillPurchase(player, skill);
        }

        /// <summary>
        /// Called when a player uses a chat command
        /// </summary>
        [Scripts.Event("Player.ChatCommand")]
        public bool playerChatCommand(Player player, Player recipient, string command, string payload)
        {
            command = command.Trim().ToLower();
            if (command == "elo")
            {
                EloRating rating = new EloRating(1500.0d, 1500.0d, 37, 33);
                player.sendMessage(0, String.Format("{0},{1}", rating.FinalResult1, rating.FinalResult2));
            }

            if (command == "event")
            {
                if (!AllowEvents)
                {
                    player.sendMessage(-1, "This arena is locked from doing events.");
                    return false;
                }

                var names = _gamePlay.CurrentEventTypes;
                if (names.Count == 0)
                {
                    player.sendMessage(-1, "There are no events currently available.");
                    return false;
                }

                var eventName = names.FirstOrDefault(x => string.Equals(x, payload, StringComparison.OrdinalIgnoreCase));
                var options = string.Join(", ", names);

                if (string.IsNullOrWhiteSpace(payload) || string.IsNullOrWhiteSpace(eventName))
                {
                    //If an event is active, show what it is
                    if (_gamePlay.Events)
                        player.sendMessage(0, string.Format("Current active event - {0}", Enum.GetName(typeof(Settings.EventTypes), _gamePlay._eventType)));

                    player.sendMessage(-1, string.Format("Syntax: ?event <event name> - Options are {0}.", options));
                    return false;
                }

                // If game is running, no voting allowed.
                if (_arena._bGameRunning)
                {
                    player.sendMessage(0, "Please wait until the current match is over before starting a vote.");
                    return false;
                }

                //Dont allow spectators to vote when voting has started
                if (player.IsSpectator && _tickVotingStarted > 0)
                {
                    player.sendMessage(-1, "Spectators are not allowed to vote.");
                    return false;
                }

                if (_votingEnded)
                {
                    player.sendMessage(-1, "Voting has already ended. You must wait till the next game to vote.");
                    return false;
                }

                // Votes are starting!
                if (_eventVoting.Keys.Count == 0)
                {
                    _tickVotingStarted = Environment.TickCount;
                    _votingTime = _gamePlay.VotingTime;
                    player._arena.sendArenaMessage(string.Format("Event voting has started, use ?event <event name> to vote - Options are {0}.", options));
                }

                List<string> removeKeys = new List<string>();
                //Did this person already vote?
                foreach (var kvp in _eventVoting)
                {
                    var playerIds = kvp.Value;
                    if (playerIds.Contains(player._id))
                    {
                        playerIds.Remove(player._id);
                    }

                    //Is this key now empty?
                    if (kvp.Value.Count == 0)
                        removeKeys.Add(kvp.Key);
                }

                //Remove any keys that have no votes
                foreach (var key in removeKeys)
                    _eventVoting.Remove(key);

                //This name in the list?
                if (!_eventVoting.ContainsKey(eventName))
                    _eventVoting.Add(eventName, new List<int>());

                //Add them
                _eventVoting[eventName].Add(player._id);
                player.sendMessage(0, string.Format("You have voted for event {0}.", eventName));
            }

            return true;
        }

        /// <summary>
        /// Called when a player sends a mod command
        /// </summary>
        [Scripts.Event("Player.ModCommand")]
        public bool playerModCommand(Player player, Player recipient, string command, string payload)
        {
            command = (command.ToLower());
            if (command.Equals("squadfind") && player.PermissionLevelLocal >= Data.PlayerPermission.Mod)
            {
                if (string.IsNullOrWhiteSpace(payload) && recipient == null)
                {
                    player.sendMessage(-1, "Sytax: *squadfind alias OR ::*squadfind");
                    return false;
                }

                if (_gamePlay.ActiveSquads == null)
                {
                    player.sendMessage(-1, "Cannot search for the player; the active squad list does not exist.");
                    return false;
                }

                if (recipient != null && _gamePlay.ActiveSquads.ContainsKey(recipient._alias.ToLower()))
                {
                    player.sendMessage(0, string.Format("Player: {0} - Squad: {1}", recipient._alias, _gamePlay.ActiveSquads[recipient._alias.ToLower()]));
                    return true;
                }

                else if (_gamePlay.ActiveSquads.ContainsKey(payload.ToLower()))
                {
                    player.sendMessage(0, string.Format("Player: {0} - Squad: {1}", payload, _gamePlay.ActiveSquads[payload.ToLower()]));
                    return true;
                }

                else
                {
                    player.sendMessage(0, "That player doesn't seem to be on a usl squad.");
                    return true;
                }
            }

            if (command.Equals("mvp") && player.PermissionLevelLocal >= Data.PlayerPermission.Mod)
            {
                if (string.IsNullOrWhiteSpace(payload) && recipient == null)
                {
                    player.sendMessage(-1, "Syntax: *mvp alias OR ::*mvp");
                    return false;
                }


                if (!_gamePlay.AwardMVP)
                {
                    player.sendMessage(-1, "Cannot award yet till the end of a match.");
                    return false;
                }

                Player target = recipient != null ? recipient : _arena.getPlayerByName(payload);
                _arena.sendArenaMessage("MVP award goes to......... ");
                _arena.sendArenaMessage(target != null ? target._alias : payload);

                if (target != null)
                {
                    target.ZoneStat3 += 1;
                    _arena._server._db.updatePlayer(target);
                }

                if (!string.IsNullOrEmpty(_gamePlay.GetFileName))
                {
                    StreamWriter fs = Logic_File.OpenStatFile(_gamePlay.GetFileName, string.Format("Season {0}", _gamePlay.LeagueSeason.ToString()));
                    fs.WriteLine("Referee: {0}", player._alias);
                    fs.WriteLine();
                    fs.WriteLine("MVP: {0}", target != null ? target._alias : payload);
                    fs.Close();
                }

                _gamePlay.AwardMVP = false;
                return true;
            }

            if (command.Equals("setscore"))
            {
                if (string.IsNullOrEmpty(payload))
                {
                    player.sendMessage(-1, "Syntax: *setscore 1,2  (In order by teamname per scoreboard)");
                    return false;
                }

                if (!payload.Contains(','))
                {
                    player.sendMessage(-1, "Error in syntax, missing comma seperation.");
                    return false;
                }

                string[] args = payload.Split(',');
                if (!Helpers.IsNumeric(args[0]) || !Helpers.IsNumeric(args[1]))
                {
                    player.sendMessage(-1, "Value is not numeric.");
                    return false;
                }

                IEnumerable<Team> activeTeams = _arena.ActiveTeams;
                if (activeTeams.ElementAt(0) != null)
                    int.TryParse(args[0].Trim(), out activeTeams.ElementAt(0)._currentGameKills);
                if (activeTeams.ElementAt(1) != null)
                    int.TryParse(args[1].Trim(), out activeTeams.ElementAt(1)._currentGameKills);

                //Immediately notify the change
                _gamePlay.UpdateTickers();

                return true;
            }

            if (command.Equals("poweradd"))
            {
                if (player.PermissionLevelLocal < Data.PlayerPermission.SMod)
                {
                    player.sendMessage(-1, "Nice try.");
                    return false;
                }

                int level = (int)Data.PlayerPermission.ArenaMod;
                //Pm'd?
                if (recipient != null)
                {
                    //Check for a possible level
                    if (!string.IsNullOrWhiteSpace(payload))
                    {
                        try
                        {
                            level = Convert.ToInt16(payload);
                        }
                        catch
                        {
                            player.sendMessage(-1, "Invalid level. Level must be either 1 or 2.");
                            return false;
                        }

                        if (level < 1 || level > (int)player.PermissionLevelLocal
                            || level == (int)Data.PlayerPermission.SMod)
                        {
                            player.sendMessage(-1, ":alias:*poweradd level(optional), :alias:*poweradd level (Defaults to 1)");
                            player.sendMessage(0, "Note: there can only be 1 admin level.");
                            return false;
                        }

                        switch (level)
                        {
                            case 1:
                                recipient._permissionStatic = Data.PlayerPermission.ArenaMod;
                                break;
                            case 2:
                                recipient._permissionStatic = Data.PlayerPermission.Mod;
                                break;
                        }
                        recipient._developer = true;
                        recipient.sendMessage(0, string.Format("You have been powered to level {0}. Use *help to familiarize with the commands and please read all rules.", level));
                        player.sendMessage(0, string.Format("You have promoted {0} to level {1}.", recipient._alias, level));
                    }
                    else
                    {
                        recipient._developer = true;
                        recipient._permissionStatic = Data.PlayerPermission.ArenaMod;
                        recipient.sendMessage(0, string.Format("You have been powered to level {0}. Use *help to familiarize with the commands and please read all rules.", level));
                        player.sendMessage(0, string.Format("You have promoted {0} to level {1}.", recipient._alias, level));
                    }

                    //Lets send it to the database
                    //Send it to the db
                    CS_ModQuery<Data.Database> query = new CS_ModQuery<Data.Database>();
                    query.queryType = CS_ModQuery<Data.Database>.QueryType.dev;
                    query.sender = player._alias;
                    query.query = recipient._alias;
                    query.level = level;
                    //Send it!
                    player._server._db.send(query);
                    return true;
                }
                else
                {
                    //We arent
                    //Get name and possible level
                    short number;
                    if (string.IsNullOrEmpty(payload))
                    {
                        player.sendMessage(-1, "Syntax: *poweradd alias:level(optional) Note: if using a level, put : before it otherwise defaults to arena mod");
                        player.sendMessage(0, "Note: there can only be 1 admin.");
                        return false;
                    }
                    if (payload.Contains(':'))
                    {
                        string[] param = payload.Split(':');
                        try
                        {
                            number = Convert.ToInt16(param[1]);
                            if (number >= 0)
                                level = number;
                        }
                        catch
                        {
                            player.sendMessage(-1, "That is not a valid level. Possible powering levels are 1 or 2.");
                            return false;
                        }
                        if (level < 1 || level > (int)player.PermissionLevelLocal
                            || level == (int)Data.PlayerPermission.SMod)
                        {
                            player.sendMessage(-1, string.Format("Syntax: *poweradd alias:level(optional) OR :alias:*poweradd level(optional) possible levels are 1-{0}", ((int)player.PermissionLevelLocal).ToString()));
                            player.sendMessage(0, "Note: there can be only 1 admin level.");
                            return false;
                        }
                        payload = param[0];
                    }
                    player.sendMessage(0, string.Format("You have promoted {0} to level {1}.", payload, level));
                    if ((recipient = player._server.getPlayer(payload)) != null)
                    { //They are playing, lets update them
                        switch (level)
                        {
                            case 1:
                                recipient._permissionStatic = Data.PlayerPermission.ArenaMod;
                                break;
                            case 2:
                                recipient._permissionStatic = Data.PlayerPermission.Mod;
                                break;
                        }
                        recipient._developer = true;
                        recipient.sendMessage(0, string.Format("You have been powered to level {0}. Use *help to familiarize with the commands and please read all rules.", level));
                    }

                    //Lets send it off
                    CS_ModQuery<Data.Database> query = new CS_ModQuery<Data.Database>();
                    query.queryType = CS_ModQuery<Data.Database>.QueryType.dev;
                    query.sender = player._alias;
                    query.query = payload;
                    query.level = level;
                    //Send it!
                    player._server._db.send(query);
                    return true;
                }
            }

            if (command.Equals("powerremove"))
            {
                if (player.PermissionLevelLocal < Data.PlayerPermission.SMod)
                {
                    player.sendMessage(-1, "Nice try.");
                    return false;
                }

                int level = (int)Data.PlayerPermission.Normal;
                //Pm'd?
                if (recipient != null)
                {
                    //Check for a possible level
                    if (!string.IsNullOrWhiteSpace(payload))
                    {
                        try
                        {
                            level = Convert.ToInt16(payload);
                        }
                        catch
                        {
                            player.sendMessage(-1, "Invalid level. Levels must be between 0 and 2.");
                            return false;
                        }

                        if (level < 0 || level > (int)player.PermissionLevelLocal
                            || level == (int)Data.PlayerPermission.SMod)
                        {
                            player.sendMessage(-1, ":alias:*powerremove level(optional), :alias:*powerremove level (Defaults to 0)");
                            return false;
                        }

                        switch (level)
                        {
                            case 0:
                                recipient._permissionStatic = Data.PlayerPermission.Normal;
                                recipient._developer = false;
                                break;
                            case 1:
                                recipient._permissionStatic = Data.PlayerPermission.ArenaMod;
                                break;
                            case 2:
                                recipient._permissionStatic = Data.PlayerPermission.Mod;
                                break;
                        }
                        recipient.sendMessage(0, string.Format("You have been demoted to level {0}.", level));
                        player.sendMessage(0, string.Format("You have demoted {0} to level {1}.", recipient._alias, level));
                    }
                    else
                    {
                        recipient._developer = false;
                        recipient._permissionStatic = Data.PlayerPermission.Normal;
                        recipient.sendMessage(0, string.Format("You have been demoted to level {0}.", level));
                        player.sendMessage(0, string.Format("You have demoted {0} to level {1}.", recipient._alias, level));
                    }

                    //Lets send it to the database
                    //Send it to the db
                    CS_ModQuery<Data.Database> query = new CS_ModQuery<Data.Database>();
                    query.queryType = CS_ModQuery<Data.Database>.QueryType.dev;
                    query.sender = player._alias;
                    query.query = recipient._alias;
                    query.level = level;
                    //Send it!
                    player._server._db.send(query);
                    return true;
                }
                else
                {
                    //We arent
                    //Get name and possible level
                    short number;
                    if (string.IsNullOrEmpty(payload))
                    {
                        player.sendMessage(-1, "Syntax: *powerremove alias:level(optional) Note: if using a level, put : before it otherwise defaults to arena mod");
                        return false;
                    }
                    if (payload.Contains(':'))
                    {
                        string[] param = payload.Split(':');
                        try
                        {
                            number = Convert.ToInt16(param[1]);
                            if (number >= 0)
                                level = number;
                        }
                        catch
                        {
                            player.sendMessage(-1, "That is not a valid level. Possible depowering levels are between 0 and 2.");
                            return false;
                        }
                        if (level < 0 || level > (int)player.PermissionLevelLocal
                            || level == (int)Data.PlayerPermission.SMod)
                        {
                            player.sendMessage(-1, string.Format("Syntax: *powerremove alias:level(optional) OR :alias:*powerremove level(optional) possible levels are 0-{0}", ((int)player.PermissionLevelLocal).ToString()));
                            return false;
                        }
                        payload = param[0];
                    }
                    player.sendMessage(0, string.Format("You have demoted {0} to level {1}.", payload, level));
                    if ((recipient = player._server.getPlayer(payload)) != null)
                    { //They are playing, lets update them
                        switch (level)
                        {
                            case 0:
                                recipient._permissionStatic = Data.PlayerPermission.Normal;
                                recipient._developer = false;
                                break;
                            case 1:
                                recipient._permissionStatic = Data.PlayerPermission.ArenaMod;
                                break;
                            case 2:
                                recipient._permissionStatic = Data.PlayerPermission.Mod;
                                break;
                        }
                        recipient.sendMessage(0, string.Format("You have been depowered to level {0}.", level));
                    }

                    //Lets send it off
                    CS_ModQuery<Data.Database> query = new CS_ModQuery<Data.Database>();
                    query.queryType = CS_ModQuery<Data.Database>.QueryType.dev;
                    query.sender = player._alias;
                    query.query = payload;
                    query.level = level;
                    //Send it!
                    player._server._db.send(query);
                    return true;
                }
            }

            if (command.Equals("event"))
            {
                var names = _gamePlay.CurrentEventTypes;
                if (names.Count == 0)
                {
                    player.sendMessage(-1, "There are no events currently available.");
                    return false;
                }

                if (string.IsNullOrEmpty(payload))
                {
                    //If an event is active, show what it is
                    if (_gamePlay.Events)
                        player.sendMessage(0, string.Format("Current active event - {0}", Enum.GetName(typeof(Settings.EventTypes), _gamePlay._eventType)));
                    string options = string.Join(", ", names);
                    player.sendMessage(-1, string.Format("Syntax: *event <event name> - Options are {0}.", options));
                    player.sendMessage(0, "Use *event off to stop events and return to normal gameplay.");
                    return false;
                }

                if (payload.Equals("off"))
                {
                    EventOff += OnEventOff;
                    _arena.sendArenaMessage("All Events will be turned off at the end of this game.");
                    return true;
                }

                if (!names.Contains(payload, StringComparer.OrdinalIgnoreCase))
                {
                    player.sendMessage(-1, "That is not a valid option.");
                    string options = string.Join(", ", names);
                    player.sendMessage(0, string.Format("Syntax: *event <event name> - Options are {0} (use *event off to stop the event)", options));
                    return false;
                }

                Settings.EventTypes eType;
                foreach (string s in names)
                {
                    if (s.Equals(payload, StringComparison.OrdinalIgnoreCase))
                        if (Enum.TryParse(s, out eType))
                        {
                            _gamePlay._eventType = eType;
                            _arena.sendArenaMessage(string.Format("Event {0} is now ON!", s));

                            _gamePlay.Events = true;
                            if (EventOff != null)
                            {
                                //Still active, lets reset
                                EventOff -= EventOff;
                            }
                            return true;
                        }
                }
            }

            if (command.Equals("spawnevent"))
            {
                var names = Enum.GetNames(typeof(Settings.SpawnEventTypes));
                if (string.IsNullOrEmpty(payload))
                {
                    //If an event is active, show what it is
                    if (_gamePlay.SpawnEvent)
                        player.sendMessage(0, String.Format("Current active event - {0}", Enum.GetName(typeof(Settings.SpawnEventTypes), _gamePlay._spawnEventType)));
                    string options = string.Join(", ", names);
                    player.sendMessage(-1, string.Format("Syntax: *spawnevent <event name> - Options are {0}.", options));
                    player.sendMessage(0, "If you want to set or disable a halfway point for 30k's, use *spawnevent timer");
                    player.sendMessage(0, "Use *spawnevent off to stop events and return to normal gameplay.");
                    return false;
                }

                if (payload.Equals("off"))
                {
                    _gamePlay.SpawnTimer = 0;
                    _gamePlay.SpawnEvent = false;
                    _arena.sendArenaMessage("Spawned Events are now turned off.");
                    return true;
                }

                if ((!player._developer && player.PermissionLevel < Data.PlayerPermission.Mod)
                    || (player._developer && player.PermissionLevelLocal < Data.PlayerPermission.SMod))
                {
                    player.sendMessage(-1, "Only Mods/Zone Admins can set the 30k event.");
                    return false;
                }

                if (payload.Equals("timer"))
                {   //We even activated?
                    if (!_gamePlay.SpawnEvent)
                    {
                        player.sendMessage(-1, "Spawn Events are not activated yet.");
                        return false;
                    }

                    //If this hasnt been activated, lets turn it on
                    if (_gamePlay.SpawnTimer == 0)
                    {
                        Random rand = new Random();
                        int midpoint = ((_config.deathMatch.timer / 60) / 2); //Deathmatch is in seconds, need to convert to minutes then find halfway point
                        _gamePlay.SpawnTimer = rand.Next(midpoint - 1, midpoint + 1); //Lets randomize mid point
                        player.sendMessage(0, "Midpoint timer has been activated.");
                        return true;
                    }
                    //It has, turn it off
                    _gamePlay.SpawnTimer = 0;
                    player.sendMessage(0, "Midpoint timer has been deactivated.");
                    return true;
                }

                if (!names.Contains(payload, StringComparer.OrdinalIgnoreCase))
                {
                    player.sendMessage(-1, "That is not a valid option.");
                    string options = string.Join(", ", names);
                    player.sendMessage(0, string.Format("Syntax: *spawnevent <event name> - Options are {0} (use *spawnevent off to stop the event)", options));
                    return false;
                }

                Settings.SpawnEventTypes eType;
                foreach (string s in names)
                {
                    if (s.Equals(payload, StringComparison.OrdinalIgnoreCase))
                        if (Enum.TryParse(s, out eType))
                        {
                            _gamePlay._spawnEventType = eType;
                            _arena.sendArenaMessage(string.Format("SpawnEvent {0} has been turned ON!", s));

                            _gamePlay.SpawnEvent = true;
                            return true;
                        }
                }
            }
            return false;
        }
        #endregion

        #region Private Calls
        /// <summary>
        /// Are events and voting allowed?
        /// </summary>
        private bool AllowEvents
        {
            get
            {
                if (_gamePlay._gameType == Settings.GameTypes.LEAGUEMATCH ||
                    _gamePlay._gameType == Settings.GameTypes.LEAGUEOVERTIME ||
                    !_gamePlay.Voting)
                    return false;

                return true;
            }
        }
        /// <summary>
        /// Starts our vote processing if allowed
        /// </summary>
        private void Vote()
        {
            //If this is a league match, or voting is turned off, dont allow voting
            if (!AllowEvents)
                return;

            var names = _gamePlay.CurrentEventTypes;
            if (names.Count == 0)
                return;

            var options = string.Join(", ", names);
            if (_tickVotingStarted == 0)
            {
                _tickVotingStarted = Environment.TickCount;
                _arena.sendArenaMessage(string.Format("Event voting has started, use ?event <event name> to vote - Options are {0}.", options));
            }
        }

        /// <summary>
        /// Completes our voting process and shows the results
        /// </summary>
        private void CompleteVoting()
        {   //Dont switch if no one voted
            if (_eventVoting.Count == 0)
                return;

            //Do we have enough players voting? 15%
            int inGameCount = _arena.PlayersIngame.Count();
            int votes = 0;
            foreach (KeyValuePair<string, List<int>> voting in _eventVoting)
            {
                votes += voting.Value.Count;
            }

            if (votes < Math.Round(inGameCount * 0.15f))
            {
                _arena.sendArenaMessage("Not enough players voted. Continuing same event.");
                return;
            }

            var winningEvent = _eventVoting.OrderByDescending(x => x.Value).FirstOrDefault();
            _arena.sendArenaMessage(string.Format("Voting over! Winning event is {0} with {1} vote(s). Switching teams...", winningEvent.Key, winningEvent.Value.Count));

            _gamePlay._eventType = (Settings.EventTypes)Enum.Parse(typeof(Settings.EventTypes), winningEvent.Key);

            _gamePlay._gameType = Settings.GameTypes.EVENT;
            _gamePlay.Events = true;
            _gamePlay.Voting = true;
            _votingEnded = true;
            _eventVoting.Clear();

            SwitchTeams();
        }

        /// <summary>
        /// Fires when event off is used and game start is called
        /// </summary>
        private void OnEventOff()
        {
            _gamePlay.Events = false;
            _gamePlay.SpawnEvent = false;
            _gamePlay.SpawnTimer = 0;
            _tickVotingStarted = 0;

            //If this is an arena match, just return
            if (_arena._isMatch || _gamePlay._gameType == Settings.GameTypes.LEAGUEOVERTIME)
                return;

            Team titan = _arena.getTeamByName(_config.teams[0].name);
            Team collie = _arena.getTeamByName(_config.teams[1].name);
            List<Player> shuffledPlayers = _arena.PlayersIngame.OrderBy(plyr => _arena._rand.Next(0, 500)).ToList();
            for (int i = 0; i < shuffledPlayers.Count; i++)
            {
                Team team = (i % 2) > 0 ? collie : titan;
                if (shuffledPlayers[i]._team != team)
                    team.addPlayer(shuffledPlayers[i]);
            }
            _gamePlay._gameType = Settings.GameTypes.TDM;
        }

        /// <summary>
        /// Switches the teams before a game start.
        /// </summary>
        private void SwitchTeams()
        {
            foreach (var player in _arena.PlayersIngame)
            {
                switch (_gamePlay._eventType)
                {
                    case Settings.EventTypes.RedBlue:
                        Team red = _arena.getTeamByName("Red");
                        Team blue = _arena.getTeamByName("Blue");

                        //Sanity checks
                        if (red == null || blue == null)
                            break;

                        if (red.ActivePlayerCount <= blue.ActivePlayerCount)
                        {
                            if (player._team != red)
                                red.addPlayer(player);
                        }
                        else
                        {
                            if (player._team != blue)
                                blue.addPlayer(player);
                        }
                        break;

                    case Settings.EventTypes.GreenYellow:
                        Team green = _arena.getTeamByName("Green");
                        Team yellow = _arena.getTeamByName("Yellow");

                        //Sanity checks
                        if (green == null || yellow == null)
                            break;

                        if (green.ActivePlayerCount <= yellow.ActivePlayerCount)
                        {
                            if (player._team != green)
                                green.addPlayer(player);
                        }
                        else
                        {
                            if (player._team != yellow)
                                yellow.addPlayer(player);
                        }
                        break;

                    case Settings.EventTypes.WhiteBlack:
                        Team white = _arena.getTeamByName("White");
                        Team black = _arena.getTeamByName("Black");

                        //Sanity checks
                        if (white == null || black == null)
                            break;

                        if (white.ActivePlayerCount <= black.ActivePlayerCount)
                        {
                            if (player._team != white)
                                white.addPlayer(player);
                        }
                        else
                        {
                            if (player._team != black)
                                black.addPlayer(player);
                        }
                        break;

                    case Settings.EventTypes.PinkPurple:
                        Team pink = _arena.getTeamByName("Pink");
                        Team purple = _arena.getTeamByName("Purple");

                        //Sanity checks
                        if (pink == null || purple == null)
                            break;

                        if (pink.ActivePlayerCount <= purple.ActivePlayerCount)
                        {
                            if (player._team != pink)
                                pink.addPlayer(player);
                        }
                        else
                        {
                            if (player._team != purple)
                                purple.addPlayer(player);
                        }
                        break;

                    case Settings.EventTypes.GoldSilver:
                        Team gold = _arena.getTeamByName("Gold");
                        Team silver = _arena.getTeamByName("Silver");

                        //Sanity checks
                        if (gold == null || silver == null)
                            break;

                        if (gold.ActivePlayerCount <= silver.ActivePlayerCount)
                        {
                            if (player._team != gold)
                                gold.addPlayer(player);
                        }
                        else
                        {
                            if (player._team != silver)
                                silver.addPlayer(player);
                        }
                        break;

                    case Settings.EventTypes.BronzeDiamond:
                        Team bronze = _arena.getTeamByName("Bronze");
                        Team diamond = _arena.getTeamByName("Diamond");

                        //Sanity checks
                        if (bronze == null || diamond == null)
                            break;

                        if (bronze.ActivePlayerCount <= diamond.ActivePlayerCount)
                        {
                            if (player._team != bronze)
                                bronze.addPlayer(player);
                        }
                        else
                        {
                            if (player._team != diamond)
                                diamond.addPlayer(player);
                        }
                        break;

                    case Settings.EventTypes.OrangeGray:
                        Team orange = _arena.getTeamByName("Orange");
                        Team gray = _arena.getTeamByName("Gray");

                        //Sanity checks
                        if (orange == null || gray == null)
                            break;

                        if (orange.ActivePlayerCount <= gray.ActivePlayerCount)
                        {
                            if (player._team != orange)
                                orange.addPlayer(player);
                        }
                        else
                        {
                            if (player._team != gray)
                                gray.addPlayer(player);
                        }
                        break;
                }
            }
        }

        #endregion
    }
}