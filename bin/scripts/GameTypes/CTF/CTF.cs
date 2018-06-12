using System;
using System.Collections.Generic;
using System.Linq;

using InfServer.Game;
using InfServer.Scripting;
using InfServer.Protocol;

using Assets;

namespace InfServer.Script.GameType_CTF
{
    //////////////////////////////////////////////////////
    // Script class
    // Provides the interface between the script and arena
    //////////////////////////////////////////////////////
    class Script_CTF : Scripts.IScript
    {
        #region Member Variables
        //////////////////////////////////////////////////
        // Member Variables
        //////////////////////////////////////////////////
        private Arena arena;
        private CfgInfo CFG;
        private int lastGameCheck;

        private int minPlayers;
        private int preGamePeriod;

        private Team winningTeam;
        private int winningTeamTick;
        private int winningTeamNotify;
        private int victoryHoldTime;
        private bool gameWon;

        private GameState gameState;
        private CTFMode flagMode;

        /// <summary>
        /// Stores our player streak information
        /// </summary>
        private class PlayerStreak
        {
            public ItemInfo.Projectile lastUsedWeap { get; set; }
            public int lastUsedWepKillCount { get; set; }
            public long lastUsedWepTick { get; set; }
            public int lastKillerCount { get; set; }
        }

        private Dictionary<string, PlayerStreak> killStreaks;
        private Player lastKiller;

        private Dictionary<string, int> explosives;
        private string[] explosiveList = { "Frag Grenade", "WP Grenade", "EMP Grenade", "Kuchler RG 249", "Maklov RG 2", "Titan Arms RG 2mv", "AP Mine",
                                        "Plasma Mine", "Grapeshot Mine", "RPG", "Micro Missle Launcher", "Recoilless Rifle", "Kuchler PC v2",
                                        "Maklov XVI PC2000" };
        //Note: these corrispond with the weapons above in order
        private int[] explosiveAliveTimes = { 250, 250, 250, 500, 500, 500, 500, 100, 250, 500, 500, 500, 450, 450 };

        #endregion

        #region Game Functions
        //////////////////////////////////////////////////
        // Game Functions
        //////////////////////////////////////////////////
        /// <summary>
        /// Performs script initialization
        /// </summary>
        public bool init(IEventObject invoker)
        {
            arena = invoker as Arena;
            CFG = arena._server._zoneConfig;

            minPlayers = 2;
            victoryHoldTime = CFG.flag.victoryHoldTime;
            preGamePeriod = CFG.flag.startDelay;

            killStreaks = new Dictionary<string, PlayerStreak>();
            explosives = new Dictionary<string, int>();

            for (int i = 0; i < explosiveList.Length; i++)
            {
                explosives.Add(explosiveList[i], explosiveAliveTimes[i]);
            }

            foreach (Arena.FlagState fs in arena._flags.Values)
            {	//Determine the minimum number of players
                if (fs.flag.FlagData.MinPlayerCount < minPlayers)
                { minPlayers = fs.flag.FlagData.MinPlayerCount; }

                //Register our flag change events
                fs.TeamChange += OnFlagChange;
            }

            gameState = GameState.Init;
            return true;
        }

        /// <summary>
        /// CTF Script poll called by our arena
        /// </summary>
        public bool poll()
        {
            int now = Environment.TickCount;

            if (now - lastGameCheck < Arena.gameCheckInterval)
                return true;
            lastGameCheck = now;

            if (gameState == GameState.Init)
            {
                if (arena.PlayersIngame.Count() < minPlayers)
                {
                    gameState = GameState.NotEnoughPlayers;
                }
            }

            switch (gameState)
            {
                case GameState.NotEnoughPlayers:
                    arena.setTicker(1, 3, 0, "Not Enough Players");
                    gameState = GameState.Init;
                    break;
                case GameState.Transitioning:
                    //Do nothing while we wait
                    break;
                case GameState.ActiveGame:
                    PollCTF(now);
                    break;
                case GameState.Init:
                    Initialize();
                    break;
                case GameState.PreGame:
                    PreGame();
                    break;
                case GameState.PostGame:
                    gameState = GameState.Init;
                    break;
            }
            return true;
        }

        private void OnFlagChange(Arena.FlagState flag)
        {
            Team victory = flag.team;

            //Does this team now have all the flags?
            foreach (Arena.FlagState fs in arena._flags.Values)
            {
                if (fs.bActive && fs.team != flag.team)
                {   //Not all flags are captured yet
                    victory = null;
                    break;
                }
            }

            if (!gameWon)
            {   //All flags captured?
                if (victory != null)
                {   //Yep
                    winningTeamTick = (Environment.TickCount + (victoryHoldTime * 10));
                    winningTeamNotify = 0;
                    winningTeam = victory;
                    flagMode = CTFMode.XSeconds;
                }
                else
                {   //Aborted?
                    if (winningTeam != null)
                    {   //Yep
                        winningTeam = null;
                        winningTeamTick = 0;
                        flagMode = CTFMode.Aborted;
                    }
                }
            }
        }

        #endregion

        #region Script Functions
        ///////////////////////////////////////////////////
        // Script Functions
        ///////////////////////////////////////////////////
        /// <summary>
        /// Resets all variables and initializes a new game state
        /// </summary>
        private void Initialize()
        {
            winningTeamNotify = 0;
            winningTeamTick = 0;
            winningTeam = null;
            gameWon = false;

            //We are officially initialized, pregame it.
            gameState = GameState.PreGame;
        }

        /// <summary>
        /// Our waiting period between games
        /// </summary>
        private void PreGame()
        {
            gameState = GameState.Transitioning;

            //Sit here until timer runs out
            arena.setTicker(1, 3, preGamePeriod * 100, "Next game: ",
                    delegate()
                    {	//Trigger the game start
                        arena.gameStart();
                    }
            );
        }

        /// <summary>
        /// Resets our tickers and gamestate
        /// </summary>
        private void Reset()
        {
            //Clear any tickers that might be still active
            if (gameState == GameState.Transitioning)
            {
                arena.setTicker(4, 3, 0, ""); //Next game
            }
            arena.setTicker(4, 1, 0, ""); //Victory in x:x

            //Reset
            gameState = GameState.Init;
        }

        /// <summary>
        /// Did someone win yet? If so, set the announcement
        /// </summary>
        private void CheckWinner(int now)
        {
            //See if someone is winning
            if (winningTeam != null)
            {
                //Has XSeconds been called yet?
                if (flagMode == CTFMode.XSeconds)
                { return; }

                int tick = ((winningTeamTick - now) / 1000);
                switch (tick)
                {
                    case 10:
                        flagMode = CTFMode.TenSeconds;
                        break;
                    case 30:
                        flagMode = CTFMode.ThirtySeconds;
                        break;
                    case 60:
                        flagMode = CTFMode.SixtySeconds;
                        break;
                    default:
                        if (tick <= 0)
                        {
                            flagMode = CTFMode.GameDone;
                        }
                        break;
                }
            }
        }

        private void SetNotifyBypass(int countdown)
        {   //If XSeconds matches one of these, it will bypass that call
            //so there is no duplicate Victory message
            switch (countdown)
            {
                case 10:
                    winningTeamNotify = 1;
                    break;
                case 30:
                    winningTeamNotify = 2;
                    break;
                case 60:
                    winningTeamNotify = 3;
                    break;
            }
        }

        /// <summary>
        /// Poll the flag state while checking for a winner
        /// </summary>
        private void PollCTF(int now)
        {
            //See if we have enough players to keep playing
            if (arena.PlayersIngame.Count() < minPlayers)
            {
                Reset();
            }
            else
            {
                CheckWinner(now);
            }

            int countdown = winningTeamTick > 0 ? ((winningTeamTick - now) / 1000) : 0;
            switch (flagMode)
            {
                case CTFMode.Aborted:
                    arena.setTicker(4, 1, 0, "");
                    arena.sendArenaMessage("Victory has been aborted.", CFG.flag.victoryAbortedBong);
                    flagMode = CTFMode.None;
                    break;
                case CTFMode.TenSeconds:
                    //10 second win timer
                    if (winningTeamNotify == 1) //Been notified already?
                    { break; }
                    winningTeamNotify = 1;
                    arena.sendArenaMessage(string.Format("Victory for {0} in {1} seconds!", winningTeam._name, countdown), CFG.flag.victoryWarningBong);
                    flagMode = CTFMode.None;
                    break;
                case CTFMode.ThirtySeconds:
                    //30 second win timer
                    if (winningTeamNotify == 2) //Been notified already?
                    { break; }
                    winningTeamNotify = 2;
                    arena.sendArenaMessage(string.Format("Victory for {0} in {1} seconds!", winningTeam._name, countdown), CFG.flag.victoryWarningBong);
                    flagMode = CTFMode.None;
                    break;
                case CTFMode.SixtySeconds:
                    //60 second win timer
                    if (winningTeamNotify == 3) //Been notified already?
                    { break; }
                    winningTeamNotify = 3;
                    arena.sendArenaMessage(string.Format("Victory for {0} in {1} seconds!", winningTeam._name, countdown), CFG.flag.victoryWarningBong);
                    flagMode = CTFMode.None;
                    break;
                case CTFMode.XSeconds:
                    //Initial win timer upon capturing
                    SetNotifyBypass(countdown); //Checks to see if xSeconds matches any other timers
                    arena.setTicker(4, 1, CFG.flag.victoryHoldTime, "Victory in ");
                    arena.sendArenaMessage(string.Format("Victory for {0} in {1} seconds!", winningTeam._name, countdown), CFG.flag.victoryWarningBong);
                    flagMode = CTFMode.None;
                    break;
                case CTFMode.GameDone:
                    //Game is done
                    gameWon = true;
                    arena.gameEnd();
                    break;
            }

            UpdateCTFTickers();
            UpdateKillStreaks();
        }

        /// <summary>
        /// Called when the game begins
        /// </summary>
        [Scripts.Event("Game.Start")]
        public bool StartGame()
        {
            gameState = GameState.ActiveGame;
            flagMode = CTFMode.None;

            ResetKiller(null);
            killStreaks.Clear();

            foreach (Player p in arena.Players)
            {
                PlayerStreak temp = new PlayerStreak();
                temp.lastKillerCount = 0;
                temp.lastUsedWeap = null;
                temp.lastUsedWepKillCount = 0;
                temp.lastUsedWepTick = -1;
                killStreaks.Add(p._alias, temp);
            }

            //Let everyone know
            arena.sendArenaMessage("Game has started!", CFG.flag.resetBong);

            return true;
        }

        /// <summary>
        /// Called when the game ends
        /// </summary>
        [Scripts.Event("Game.End")]
        public bool EndGame()
        {
            gameState = GameState.PostGame;

            if (winningTeam == null)
            {
                arena.sendArenaMessage("There was no winner.");
            }
            else
            {
                arena.sendArenaMessage(winningTeam._name + " has won the game!");
                winningTeam = null;
            }

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
            if (gameState != GameState.ActiveGame)
            { return true; }

            if (killStreaks.ContainsKey(from._alias))
            {
                if (explosives.ContainsKey(usedWep.name))
                    UpdateWeapon(from, usedWep, explosives[usedWep.name]);
            }
            return true;
        }

        /// <summary>
        /// Triggered when one player has killed another
        /// </summary>
        [Scripts.Event("Player.PlayerKill")]
        public bool playerPlayerKill(Player victim, Player killer)
        {
            if (gameState != GameState.ActiveGame)
            { return true; }

            //Update our kill streak
            UpdateKiller(killer);

            if (killStreaks.ContainsKey(victim._alias))
            {
                long wepTick = killStreaks[victim._alias].lastUsedWepTick;
                if (wepTick != -1)
                    UpdateWeaponKill(killer);
            }
            if (killer != null && victim != null && victim._bounty >= 300)
                arena.sendArenaMessage(String.Format("{0} has ended {1}'s bounty.", killer._alias, victim._alias), 5);

            return true;
        }

        /// <summary>
        /// Triggered when a player has died, by any means
        /// </summary>
        /// <remarks>killer may be null if it wasn't a player kill</remarks>
        [Scripts.Event("Player.Death")]
        public bool playerDeath(Player victim, Player killer, Helpers.KillType killType, CS_VehicleDeath update)
        {
            if (gameState != GameState.ActiveGame)
            { return true; }

            //Update our kill counter
            UpdateDeath(victim, killer);
            return true;
        }

        /// <summary>
        /// Called when the player successfully joins the game
        /// </summary>
        [Scripts.Event("Player.Enter")]
        public void playerEnter(Player player)
        {
            //Add them to the list if its not in it
            if (!killStreaks.ContainsKey(player._alias))
            {
                PlayerStreak temp = new PlayerStreak();
                temp.lastKillerCount = 0;
                temp.lastUsedWeap = null;
                temp.lastUsedWepKillCount = 0;
                temp.lastUsedWepTick = -1;
                killStreaks.Add(player._alias, temp);
            }
        }

        /// <summary>
        /// Called when a player enters the arena
        /// </summary>
        [Scripts.Event("Player.EnterArena")]
        public void playerEnterArena(Player player)
        {
            //Add them to the list if its not in it
            if (!killStreaks.ContainsKey(player._alias))
            {
                PlayerStreak temp = new PlayerStreak();
                temp.lastKillerCount = 0;
                temp.lastUsedWeap = null;
                temp.lastUsedWepKillCount = 0;
                temp.lastUsedWepTick = -1;
                killStreaks.Add(player._alias, temp);
            }
        }

        /// <summary>
        /// Triggered when a player wants to unspec and join the game
        /// </summary>
        [Scripts.Event("Player.JoinGame")]
        public bool playerJoinGame(Player player)
        {
            //Add them to the list if its not in it
            if (!killStreaks.ContainsKey(player._alias))
            {
                PlayerStreak temp = new PlayerStreak();
                temp.lastKillerCount = 0;
                temp.lastUsedWeap = null;
                temp.lastUsedWepKillCount = 0;
                temp.lastUsedWepTick = -1;
                killStreaks.Add(player._alias, temp);
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
                    Int16 number;
                    if (string.IsNullOrEmpty(payload))
                    {
                        player.sendMessage(-1, "*poweradd alias:level(optional) Note: if using a level, put : before it otherwise defaults to arena mod");
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
                            player.sendMessage(-1, string.Format("*poweradd alias:level(optional) OR :alias:*poweradd level(optional) possible levels are 1-{0}", ((int)player.PermissionLevelLocal).ToString()));
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
                    Int16 number;
                    if (string.IsNullOrEmpty(payload))
                    {
                        player.sendMessage(-1, "*powerremove alias:level(optional) Note: if using a level, put : before it otherwise defaults to arena mod");
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
                            player.sendMessage(-1, string.Format("*powerremove alias:level(optional) OR :alias:*powerremove level(optional) possible levels are 0-{0}", ((int)player.PermissionLevelLocal).ToString()));
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
            return false;
        }

        #endregion

        #region Updaters
        private void UpdateCTFTickers()
        {
            List<Player> rankedPlayers = arena.Players.ToList().OrderBy(player => (player.StatsCurrentGame == null ? 0 : player.StatsCurrentGame.deaths)).OrderByDescending(
                player => (player.StatsCurrentGame == null ? 0 : player.StatsCurrentGame.kills)).ToList();
            int idx = 3;
            string format = "";
            foreach (Player p in rankedPlayers)
            {
                if (p.StatsCurrentGame == null)
                { continue; }
                if (idx-- == 0)
                {
                    break;
                }

                switch (idx)
                {
                    case 2:
                        format = string.Format("!1st: {0}(K={1} D={2}) ", p._alias, p.StatsCurrentGame.kills, p.StatsCurrentGame.deaths);
                        break;
                    case 1:
                        format = (format + string.Format("!2nd: {0}(K={1} D={2})", p._alias, p.StatsCurrentGame.kills, p.StatsCurrentGame.deaths));
                        break;
                }
            }
            if (!string.IsNullOrWhiteSpace(format))
            { arena.setTicker(1, 2, 0, format); }

            arena.setTicker(2, 3, 0, delegate(Player p)
            {
                if (p.StatsCurrentGame == null)
                {
                    return "Personal Score: Kills=0 - Deaths=0";
                }
                return string.Format("Personal Score: Kills={0} - Deaths={1}", p.StatsCurrentGame.kills, p.StatsCurrentGame.deaths);
            });
        }

        /// <summary>
        /// Updates our players kill streak timer
        /// </summary>
        private void UpdateKillStreaks()
        {
            foreach (KeyValuePair<string, PlayerStreak> p in killStreaks)
            {
                if (p.Value.lastUsedWepTick == -1)
                    continue;

                if (Environment.TickCount - p.Value.lastUsedWepTick <= 0)
                    ResetWeaponTicker(p.Key);
            }
        }

        /// <summary>
        /// Updates the last killer
        /// </summary>
        private void ResetKiller(Player killer)
        {
            lastKiller = killer;
        }

        /// <summary>
        /// Resets the weapon ticker to default (Time Expired)
        /// </summary>
        private void ResetWeaponTicker(string targetAlias)
        {
            if (killStreaks.ContainsKey(targetAlias))
            {
                killStreaks[targetAlias].lastUsedWeap = null;
                killStreaks[targetAlias].lastUsedWepKillCount = 0;
                killStreaks[targetAlias].lastUsedWepTick = -1;
            }
        }

        /// <summary>
        /// Updates the killer and their kill counter
        /// </summary>
        private void UpdateKiller(Player killer)
        {
            if (killStreaks.ContainsKey(killer._alias))
            {
                killStreaks[killer._alias].lastKillerCount++;
                switch (killStreaks[killer._alias].lastKillerCount)
                {
                    case 6:
                        arena.sendArenaMessage(string.Format("{0} is on fire!", killer._alias), 8);
                        break;
                    case 8:
                        arena.sendArenaMessage(string.Format("Someone kill {0}!", killer._alias), 9);
                        break;
                }
            }
            //Is this first blood?
            if (lastKiller == null)
            {
                //It is, lets make the sound
                arena.sendArenaMessage(string.Format("{0} has drawn first blood.", killer._alias), 7);
            }
            lastKiller = killer;
        }

        /// <summary>
        /// Updates the victim's kill streak and notifies the public
        /// </summary>
        private void UpdateDeath(Player victim, Player killer)
        {
            if (killStreaks.ContainsKey(victim._alias))
            {
                if (killStreaks[victim._alias].lastKillerCount >= 6)
                {
                    arena.sendArenaMessage(string.Format("{0}", killer != null ? killer._alias + " has ended " + victim._alias + "'s kill streak." :
                        victim._alias + "'s kill streak has ended."), 6);
                }
                killStreaks[victim._alias].lastKillerCount = 0;
            }
        }

        /// <summary>
        /// Updates the last fired weapon and its ticker
        /// </summary>
        private void UpdateWeapon(Player from, ItemInfo.Projectile usedWep, int aliveTime)
        {
            if (killStreaks.ContainsKey(from._alias))
            {
                killStreaks[from._alias].lastUsedWeap = usedWep;
                killStreaks[from._alias].lastUsedWepTick = DateTime.Now.AddTicks(aliveTime).Ticks;
            }
        }

        /// <summary>
        /// Updates the last weapon used and kill count then announcing it to the public
        /// </summary>
        private void UpdateWeaponKill(Player from)
        {
            if (killStreaks.ContainsKey(from._alias))
            {
                if (killStreaks[from._alias].lastUsedWeap == null)
                    return;

                killStreaks[from._alias].lastUsedWepKillCount++;
                ItemInfo.Projectile lastUsedWep = killStreaks[from._alias].lastUsedWeap;
                switch (killStreaks[from._alias].lastUsedWepKillCount)
                {
                    case 2:
                        arena.sendArenaMessage(string.Format("{0} just got a double {1} kill.", from._alias, lastUsedWep.name), 17);
                        break;
                    case 3:
                        arena.sendArenaMessage(string.Format("{0} just got a triple {1} kill!", from._alias, lastUsedWep.name), 18);
                        break;
                    case 4:
                        arena.sendArenaMessage(string.Format("A 4 {0} kill by {0}?!?", lastUsedWep.name, from._alias), 19);
                        break;
                    case 5:
                        arena.sendArenaMessage(string.Format("Unbelievable! {0} with the 5 {1} kill?", from._alias, lastUsedWep.name), 20);
                        break;
                }
            }
        }
        #endregion

        private enum GameState
        {
            Init,
            PreGame,
            ActiveGame,
            PostGame,
            NotEnoughPlayers,
            Transitioning,
        }

        private enum CTFMode
        {
            None,
            Aborted,
            TenSeconds,
            ThirtySeconds,
            SixtySeconds,
            XSeconds,
            GameDone,
        }

    }
}