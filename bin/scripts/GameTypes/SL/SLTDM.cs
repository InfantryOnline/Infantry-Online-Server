using System;
using System.Linq;
using System.Collections.Generic;

using InfServer.Game;
using InfServer.Scripting;

using Assets;

namespace InfServer.Script.GameType_SLTDM
{	/// Script Class
    /// Provides the interface between the script and arena
    ///////////////////////////////////////////////////////
    public class Script_SLTDM : Scripts.IScript
    {
        #region Member Variables
        ///////////////////////////////////////////////////
        // Member Variables
        ///////////////////////////////////////////////////
        private Arena arena;				//Pointer to our arena class
        private CfgInfo CFG;				//The zone config
        private int LastGameCheck;          //The tick at which the gamestate was checked
        private int PreGamePeriod;          //How long the pre game period is between games
        private int DeathmatchTimer;

        private Team WinningTeam;
        private int WinningTeamTick;        //The tick at which the team grabbed all the flags
        private int WinningTeamNotify;
        private int VictoryHoldTime;        //How long till the game ends
        private int MinPlayers;             //Mininimum amount of players to start a game
        private int MinDeaths;              //How many deaths must occur before you are specced out
        private int DefaultMinDeaths;       //Revert back to this after a league match
        private bool GameWon;               //Called when someone has captured all flags

        private GameState GameStates;
        private LeagueState LeagueStates;
        private FlagStatus FlagMode;

        private bool LeagueEvent;

        /// <summary>
        /// Our current in game recordings
        /// </summary>
        private Dictionary<string, PlayerStats> CurrentPlayerStats;
        public class PlayerStats
        {
            public int Kills;
            public int Deaths;
            public bool SubbedIn;
            public bool HasPlayed;
        }
        #endregion

        #region Game Functions
        ///////////////////////////////////////////////////
        // Game Functions
        ///////////////////////////////////////////////////
        public bool init(IEventObject invoker)
        {
            arena = invoker as Arena;
            CFG = arena._server._zoneConfig;
            arena.playtimeTickerIdx = 3; //Sets the global ticker index used for changing the timer

            MinPlayers = CFG.deathMatch.minimumPlayers;
            MinDeaths = 3; //Change this if we want to run a higher death count
            DefaultMinDeaths = MinDeaths;
            VictoryHoldTime = CFG.flag.victoryHoldTime;
            PreGamePeriod = CFG.deathMatch.startDelay;
            DeathmatchTimer = CFG.deathMatch.timer;

            CurrentPlayerStats = new Dictionary<string, PlayerStats>();

            foreach (Arena.FlagState fs in arena._flags.Values)
            {   //Register our flag games
                fs.TeamChange += OnFlagChange;
            }

            GameStates = GameState.Init;
            LeagueStates = LeagueState.None;
            return true;
        }

        public bool poll()
        {
            int now = Environment.TickCount;

            if (now - LastGameCheck < Arena.gameCheckInterval)
                return true;
            LastGameCheck = now;

            if (GameStates == GameState.Init)
            {
                if (arena.PlayersIngame.Count() < MinPlayers)
                {
                    GameStates = GameState.NotEnoughPlayers;
                }
            }

            switch (GameStates)
            {
                case GameState.NotEnoughPlayers:
                    arena.setTicker(1, 3, 0, "Not Enough Players");
                    GameStates = GameState.Init;
                    break;
                case GameState.Transitioning:
                    //Do nothing while we wait
                    break;
                case GameState.ActiveGame:
                    Poll(now);
                    break;
                case GameState.Init:
                    Initialize();
                    break;
                case GameState.PreGame:
                    PreGame();
                    break;
                case GameState.PostGame:
                    GameStates = GameState.Init;
                    break;
            }

            return true;
        }

        /// <summary>
        /// Called when a flag changes team
        /// </summary>
        public void OnFlagChange(Arena.FlagState flag)
        {   //Does this team have all the flags?
            Team VictoryTeam = flag.team;

            foreach (Arena.FlagState fs in arena._flags.Values)
            {
                if (fs.bActive && fs.team != VictoryTeam)
                {
                    VictoryTeam = null;
                    break;
                }
            }

            if (!GameWon)
            {
                if (VictoryTeam != null)
                {
                    WinningTeamTick = (Environment.TickCount + (VictoryHoldTime * 10));
                    WinningTeam = VictoryTeam;
                    WinningTeamNotify = 0;
                    FlagMode = FlagStatus.XSeconds;
                }
                else
                {
                    if (WinningTeam != null)
                    {
                        WinningTeam = null;
                        WinningTeamTick = 0;
                        FlagMode = FlagStatus.Aborted;
                    }
                }
            }
        }

        #endregion

        #region Script Functions
        ///////////////////////////////////////////////////
        // Script Functions
        ///////////////////////////////////////////////////
        private void Initialize()
        {
            WinningTeamNotify = 0;
            WinningTeamTick = 0;
            WinningTeam = null;
            GameWon = false;

            GameStates = GameState.PreGame;
        }

        private void PreGame()
        {
            GameStates = GameState.Transitioning;

            if (isLeagueMatch)
            {
                leaguePreGame();
            }
            else
            {   //Normal gamePlay
                //Sit here until timer runs out
                arena.setTicker(1, 3, PreGamePeriod * 100, "Next game: ",
                        delegate()
                        {  //Trigger the game start
                            arena.gameStart();
                        }
                );
            }
        }

        private void Reset()
        {
            //Clear any tickers that might be active first
            arena.setTicker(4, 0, 0, "");
            arena.setTicker(1, 3, 0, "");

            //Clear saved stats
            CurrentPlayerStats.Clear();

            //Reset
            GameStates = GameState.Init;
        }

        private void CheckWinner(int now)
        {
            //See if someone is winning
            if (WinningTeam != null)
            {
                //Has XSeconds been called yet?
                if (FlagMode == FlagStatus.XSeconds)
                { return; }

                int tick = ((WinningTeamTick - now) / 1000);
                switch (tick)
                {
                    case 10:
                        FlagMode = FlagStatus.TenSeconds;
                        break;
                    case 30:
                        FlagMode = FlagStatus.ThirtySeconds;
                        break;
                    case 60:
                        FlagMode = FlagStatus.SixtySeconds;
                        break;
                    default:
                        if (tick <= 0)
                        {
                            FlagMode = FlagStatus.GameDone;
                        }
                        break;
                }
            }
        }

        private void SetNotifyBypass(int countdown)
        {   //If XSeconds matches one of these, it will bypass that call
            //so there is no duplicated Victory message
            switch (countdown)
            {
                case 10:
                    WinningTeamNotify = 1;
                    break;
                case 30:
                    WinningTeamNotify = 2;
                    break;
                case 60:
                    WinningTeamNotify = 3;
                    break;
            }
        }

        private void Poll(int now)
        {
            //See if we have enough players to keep playing
            if (arena.PlayersIngame.Count() < MinPlayers)
            {   //We don't, lets start over from the beginning
                Reset();
            }
            else
            {
                CheckWinner(now);
            }

            int countdown = WinningTeamTick > 0 ? ((WinningTeamTick - now) / 1000) : 0;
            switch (FlagMode)
            {
                case FlagStatus.Aborted:
                    arena.setTicker(4, 0, 0, "");
                    arena.sendArenaMessage("Victory has been aborted.", CFG.flag.victoryAbortedBong);
                    FlagMode = FlagStatus.None;
                    break;
                case FlagStatus.TenSeconds:
                    //10 second win timer
                    if (WinningTeamNotify == 1) //Been notified already?
                    { break; }
                    WinningTeamNotify = 1;
                    arena.sendArenaMessage(string.Format("Victory for {0} in {1} seconds!", WinningTeam._name, countdown), CFG.flag.victoryWarningBong);
                    FlagMode = FlagStatus.None;
                    break;
                case FlagStatus.ThirtySeconds:
                    //30 second win timer
                    if (WinningTeamNotify == 2) //Been notified already?
                    { break; }
                    WinningTeamNotify = 2;
                    arena.sendArenaMessage(string.Format("Victory for {0} in {1} seconds!", WinningTeam._name, countdown), CFG.flag.victoryWarningBong);
                    FlagMode = FlagStatus.None;
                    break;
                case FlagStatus.SixtySeconds:
                    //60 second win timer
                    if (WinningTeamNotify == 3) //Been notified already?
                    { break; }
                    WinningTeamNotify = 3;
                    arena.sendArenaMessage(string.Format("Victory for {0} in {1} seconds!", WinningTeam._name, countdown), CFG.flag.victoryWarningBong);
                    FlagMode = FlagStatus.None;
                    break;
                case FlagStatus.XSeconds:
                    //Initial win timer upon capturing
                    SetNotifyBypass(countdown); //Checks to see if xSeconds matches any other timers
                    arena.setTicker(4, 0, CFG.flag.victoryHoldTime, "Victory in ");
                    arena.sendArenaMessage(string.Format("Victory for {0} in {1} seconds!", WinningTeam._name, countdown), CFG.flag.victoryWarningBong);
                    FlagMode = FlagStatus.None;
                    break;
                case FlagStatus.GameDone:
                    //Game is done
                    GameWon = true;
                    arena.gameEnd();
                    break;
            }

            UpdateTickers();
        }

        private void UpdateTickers()
        {
            //Team scores
            List<Team> ActiveTeams = arena.Teams.Where(entry => entry.ActivePlayerCount > 0).ToList();
            Team titan = ActiveTeams.Count() > 0 ? ActiveTeams.ElementAt(0) : arena.getTeamByName(CFG.teams[0].name);
            Team collie = ActiveTeams.Count() > 1 ? ActiveTeams.ElementAt(1) : arena.getTeamByName(CFG.teams[1].name);

            string format = string.Format("{0}={1} - {2}={3}", titan._name, titan._currentGameKills, collie._name, collie._currentGameKills);
            arena.setTicker(1, 2, 0, format);

            //Personal scores
            arena.setTicker(2, 1, 0, delegate(Player p)
            {
                //Update their ticker
                return string.Format("HP={0}          Personal Score: Kills={1} - Deaths={2}",
                        p._state.health,
                        (p.StatsCurrentGame == null ? 0 : p.StatsCurrentGame.kills),
                        (p.StatsCurrentGame == null ? 0 : p.StatsCurrentGame.deaths));
            });

            //1st and 2nd place
            List<Player> ranking = arena.Players.OrderBy(player => (player.StatsCurrentGame == null ? 0 : player.StatsCurrentGame.deaths)).OrderByDescending(
                player => (player.StatsCurrentGame == null ? 0 : player.StatsCurrentGame.kills)).ToList();
            int idx = 3; format = "";
            foreach (Player rankers in ranking)
            {
                if (idx-- == 0)
                    break;

                switch (idx)
                {
                    case 2:
                        format = string.Format("1st: {0}(K={1} D={2})", rankers._alias,
                          rankers.StatsCurrentGame.kills, rankers.StatsCurrentGame.deaths);
                        break;
                    case 1:
                        format = (format + string.Format(" 2nd: {0}(K={1} D={2})", rankers._alias,
                          rankers.StatsCurrentGame.kills, rankers.StatsCurrentGame.deaths));
                        break;
                }
            }
            if (!arena.recycling && WinningTeam == null)
                arena.setTicker(2, 0, 0, format);
        }
        #endregion

        #region Script Events
        ///////////////////////////////////////////////////
        // Script Functions
        ///////////////////////////////////////////////////
        /// <summary>
        /// Called when the game begins
        /// </summary>
        [Scripts.Event("Game.Start")]
        public bool gameStart()
        {
            //Reset flags
            arena.flagReset();
            arena.flagSpawn();

            GameStates = GameState.ActiveGame;
            FlagMode = FlagStatus.None;

            if (isLeagueMatch)
            {
                leagueStart();
            }
            else
            {   //Normal gameplay
                arena.sendArenaMessage("Game has started!");
                arena.setTicker(1, 3, DeathmatchTimer * 100, "Time Left: ",
                    delegate()
                    {   //Trigger game end
                        arena.gameEnd();
                    }
                );
            }
            return true;
        }

        /// <summary>
        /// Called when the game ends
        /// </summary>
        [Scripts.Event("Game.End")]
        public bool gameEnd()
        {
            //Reset flags
            _arena.flagReset();

            GameStates = GameState.PostGame;

            //Game finished
            arena.sendArenaMessage("Game Over!");

            if (WinningTeam != null)
            {
                arena.sendArenaMessage(WinningTeam._name + " has won the game!");
            }

            if (isLeagueMatch)
            {
                leagueEnd();
            }

            WinningTeam = null;

            return true;
        }

        /// <summary>
        /// Called when the statistical breakdown is displayed
        /// </summary>
        [Scripts.Event("Player.Breakdown")]
        public bool breakdown(Player from, bool bCurrent)
        {
            from.sendMessage(0, "#Team Statistics Breakdown");
            IEnumerable<Team> activeTeams = arena.Teams.Where(entry => entry.ActivePlayerCount > 0);
            IEnumerable<Team> rankedTeams = activeTeams.OrderByDescending(entry => entry._currentGameKills);
            int idx = 3;	//Only display top three teams
            foreach (Team t in rankedTeams)
            {
                if (idx-- == 0)
                    break;

                string format = "!3rd (K={0} D={1}): {2}";
                switch (idx)
                {
                    case 2:
                        format = "!1st (K={0} D={1}): {2}";
                        break;
                    case 1:
                        format = "!2nd (K={0} D={1}): {2}";
                        break;
                }

                from.sendMessage(0, string.Format(format,
                    t._currentGameKills, t._currentGameDeaths,
                    t._name));
            }

            from.sendMessage(0, "#Individual Statistics Breakdown");
            idx = 3;        //Only display top three players
            List<Player> plist = new List<Player>();
            foreach (Player p in arena.Players.ToList())
            {
                if (p.StatsCurrentGame == null)
                    continue;
                if (p.StatsCurrentGame.kills > 0 || p.StatsCurrentGame.deaths > 0)
                    plist.Add(p);
            }

            if (plist.Count > 0)
            {
                var ranking = plist.Select(player => new
                {
                    Alias = player._alias,
                    Kills = player.StatsCurrentGame.kills,
                    Deaths = player.StatsCurrentGame.deaths
                })
                .GroupBy(p => p.Kills)
                .OrderByDescending(k => k.Key)
                .Take(idx)
                .Select(g => g.OrderBy(pl => pl.Deaths));

                foreach (var alias in ranking)
                {
                    if (idx <= 0)
                        break;

                    string placeword = "";
                    string format = " (K={0} D={1}): {2}";
                    switch (idx)
                    {
                        case 3:
                            placeword = "!1st";
                            break;
                        case 2:
                            placeword = "!2nd";
                            break;
                        case 1:
                            placeword = "!3rd";
                            break;
                    }

                    idx -= alias.Count();
                    if (alias.First() != null)
                    {
                        from.sendMessage(0, string.Format(placeword + format, alias.First().Kills, alias.First().Deaths,
                            string.Join(", ", alias.Select(g => g.Alias))));
                    }
                }

                IEnumerable<Player> specialPlayers = plist.OrderByDescending(player => player.StatsCurrentGame.deaths);
                int topDeaths = (specialPlayers.First() != null ? specialPlayers.First().StatsCurrentGame.deaths : 0), deaths = 0;
                if (topDeaths > 0)
                {
                    from.sendMessage(0, "Most Deaths");
                    int i = 0;
                    List<string> mostDeaths = new List<string>();
                    foreach (Player p in specialPlayers)
                    {
                        deaths = p.StatsCurrentGame.deaths;
                        if (deaths == topDeaths)
                        {
                            if (i++ >= 1)
                                mostDeaths.Add(p._alias);
                            else
                                mostDeaths.Add(string.Format("(D={0}): {1}", deaths, p._alias));
                        }
                    }
                    if (mostDeaths.Count > 0)
                    {
                        string s = string.Join(", ", mostDeaths.ToArray());
                        from.sendMessage(0, s);
                    }
                }
            }

            if (from.StatsCurrentGame != null)
            {
                string personalFormat = "!Personal Score: (K={0} D={1})";
                from.sendMessage(0, string.Format(personalFormat,
                    from.StatsCurrentGame.kills,
                    from.StatsCurrentGame.deaths));
            }
            return true;
        }

        /// <summary>
        /// Called when a player enters the arena
        /// </summary>
        [Scripts.Event("Player.EnterArena")]
        public void playerEnterArena(Player player)
        {
            if (!CurrentPlayerStats.ContainsKey(player._alias))
            {
                PlayerStats temp = new PlayerStats();
                temp.Deaths = 0;
                temp.Kills = 0;
                temp.HasPlayed = false;
                temp.SubbedIn = false;
                CurrentPlayerStats.Add(player._alias, temp);
            }

            //Announce the command
            if (player.PermissionLevelLocal > Data.PlayerPermission.Normal)
            {
                player.sendMessage(0, "NOTE: If you would like to squad battle like it was league match, use *leaguematch to set that type of game play.");
            }
        }

        /// <summary>
        /// Called when a player enters the game
        /// </summary>
        [Scripts.Event("Player.Enter")]
        public void playerEnter(Player player)
        {
            if (GameStates != GameState.ActiveGame)
            { return; }

            if (!isLeagueMatch)
            { return; }

            if (CurrentPlayerStats.ContainsKey(player._alias))
            {
                CurrentPlayerStats[player._alias].HasPlayed = true;

                //Since the game has already started, give them 1 life
                CurrentPlayerStats[player._alias].SubbedIn = true;
            }

            player._bAllowBanner = false;
        }

        /// <summary>
        /// Triggered when a player wants to unspec and join the game
        /// </summary>
        [Scripts.Event("Player.JoinGame")]
        public bool playerJoinGame(Player player)
        {
            if (GameStates != GameState.ActiveGame)
            { return true; }

            if (!isLeagueMatch)
            { return true; }

            if (CurrentPlayerStats.ContainsKey(player._alias))
            {
                CurrentPlayerStats[player._alias].HasPlayed = true;

                //Since the game has already started, give them 1 life
                CurrentPlayerStats[player._alias].SubbedIn = true;
            }

            player._bAllowBanner = false;

            return true;
        }

        /// <summary>
        /// Triggered when one player has killed another
        /// </summary>
        [Scripts.Event("Player.PlayerKill")]
        public bool playerPlayerKill(Player victim, Player killer)
        {
            if (!isLeagueMatch)
            { return true; }

            if (CurrentPlayerStats.ContainsKey(killer._alias))
            { CurrentPlayerStats[killer._alias].Kills++; }

            if (CurrentPlayerStats.ContainsKey(victim._alias))
            { CurrentPlayerStats[victim._alias].Deaths++; }

            return true;
        }

        /// <summary>
        /// Triggered when a player has spawned
        /// </summary>
        [Scripts.Event("Player.Spawn")]
        public bool playerSpawn(Player player, bool death)
        {
            if (GameStates != GameState.ActiveGame)
            { return true; }

            //We only want to trigger end game when the last team member died out
            if (death)
            {
                if ((player.StatsCurrentGame != null && player.StatsCurrentGame.deaths >= MinDeaths)
                    || (CurrentPlayerStats.ContainsKey(player._alias) && CurrentPlayerStats[player._alias].SubbedIn))
                {
                    player.spec();
                    arena.sendArenaMessage(string.Format("{0} has died out.", player._alias));

                    if (arena.ActiveTeams.Count() < 2)
                    {
                        if (arena.ActiveTeams.Count() == 1)
                        {
                            //Reset the victory timer since they died out before flag timer went off
                            arena.setTicker(4, 0, 0, "");
                            WinningTeam = arena.ActiveTeams.ElementAt(0);
                        }

                        GameWon = true;
                        arena.gameEnd();
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Called when a player sends an unregistered mod command
        /// </summary>
        [Scripts.Event("Player.ModCommand")]
        public bool playerModCommand(Player player, Player recipient, string command, string payload)
        {
            command = (command.ToLower());
            if (command.Equals("leaguematch") && player.PermissionLevelLocal > Data.PlayerPermission.Normal)
            {
                if (GameStates == GameState.ActiveGame)
                {
                    player.sendMessage(-1, "This command can only used before a game has started or after a game has ended.");
                    return false;
                }

                LeagueEvent = !LeagueEvent;
                player.sendMessage(0, string.Format("League event has been turned {0}", LeagueEvent ? "ON!" : "OFF!"));
                return true;
            }

            return false;
        }

        #endregion

        #region League Functions
        ///////////////////////////////////////////////////
        // League Functions
        ///////////////////////////////////////////////////
        private void leaguePreGame()
        {
            arena.setTicker(1, 3, 0, "Awaiting on the referee to use *startgame");
        }

        private void leagueStart()
        {
            //Clear saved stats
            CurrentPlayerStats.Clear();

            //Set their stats and make sure they cannot get banner spammed
            foreach (Player p in arena.PlayersIngame)
            {
                if (!CurrentPlayerStats.ContainsKey(p._alias))
                {
                    PlayerStats temp = new PlayerStats();
                    temp.Deaths = 0;
                    temp.Kills = 0;
                    temp.HasPlayed = false;
                    temp.SubbedIn = false;
                    CurrentPlayerStats.Add(p._alias, temp);
                }
                p._bAllowBanner = false;
            }

            //Lets start our timer
            switch (LeagueStates)
            {
                case LeagueState.None:
                    //This is the start of a new league match, lets set it
                    LeagueStates = LeagueState.Match;
                    break;
                case LeagueState.Match:
                    arena.sendArenaMessage("Game has started!");
                    arena.setTicker(1, 3, DeathmatchTimer * 100, "Time Left: ",
                        delegate()
                        {   //Trigger game end
                            arena.gameEnd();
                        }
                    );
                    break;
                case LeagueState.OverTime:
                    //Set the deaths since its OT
                    MinDeaths = 1;
                    arena.sendArenaMessage("Game has started!");
                    arena.setTicker(1, 3, DeathmatchTimer * 100, "Time Left: ",
                        delegate()
                        {   //Trigger game end
                            arena.gameEnd();
                        }
                    );
                    break;
                case LeagueState.DoubleOT:
                    //Double OT and after only have 15 min games
                    arena.sendArenaMessage("Game has started!");
                    arena.setTicker(1, 3, (DeathmatchTimer / 2) * 100, "Time Left: ",
                        delegate()
                        {   //Trigger game end
                            arena.gameEnd();
                        }
                    );
                    break;
            }
        }

        private void leagueEnd()
        {
            switch (LeagueStates)
            {
                case LeagueState.None:
                    //Do nothing
                    break;
                case LeagueState.Match:
                    if (WinningTeam == null)
                    {
                        //Since this was a match already, we are going into OT boys!
                        LeagueStates = LeagueState.OverTime;
                        arena.sendArenaMessage("Game is going into Over Time!");
                    }
                    break;
                case LeagueState.OverTime:
                    if (WinningTeam == null)
                    {
                        //Since this was a match already, we are going into Double OT boys!
                        LeagueStates = LeagueState.DoubleOT;
                        arena.sendArenaMessage("Game is going into Double Over Time!");
                    }
                    break;
                case LeagueState.DoubleOT:
                    if (WinningTeam == null)
                    {
                        arena.sendArenaMessage("Game is continuing Double Over Time!");
                    }
                    break;
            }

            //Since there is a winner, reset the death count and league status back to default
            if (WinningTeam != null)
            {
                MinDeaths = DefaultMinDeaths;
                LeagueStates = LeagueState.None;
                arena._isMatch = false;
                LeagueEvent = false;

                arena.sendArenaMessage("This concludes our league match. League event has now been turned OFF!");
            }
        }

        private bool isLeagueMatch
        {
            get
            {
                if (arena._isMatch)
                { return true; }

                if (LeagueEvent)
                { return true; }

                return false;
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

        private enum LeagueState
        {
            None,
            Match,
            OverTime,
            DoubleOT,
        }

        private enum FlagStatus
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