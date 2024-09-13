using System;
using System.Collections.Generic;
using System.Linq;

using InfServer.Game;
using InfServer.Scripting;
using InfServer.Protocol;

using Assets;

namespace InfServer.Script.GameType_CTF
{
    using MapFlagEntry = Tuple<string, int, int>; // <Flag ID, Tile X, Tile Y>

    /// <summary>
    /// Proxies the Player object to provide CTF-oriented stats.
    /// 
    /// This list of stats maps over to the config file `Name0` through `Name6`
    /// list of stats.
    /// </summary>
    /// 
    /// <remarks>
    /// The player object is proxied because the stats are contained in variables
    /// named `ZoneStat1` through `ZoneStat7`. We want better names that match
    /// what the actual stat is, so we will hide it behind this proxy.
    /// 
    /// Note that we only create one instance for this proxy and then we reassign
    /// the player whenever we want to update; otherwise we'd be doing needless
    /// allocations for a stat update.
    /// 
    /// Ensure after you're  done updating the stat that you set player to null,
    /// that will help to guard any accidental writes.
    /// </remarks>
    class CTFPlayerStatsProxy
    {
        public Player player {get;set;}

        /// <summary>
        /// Gets or sets the number of games this player has won.
        /// </summary>
        public int GamesWon
        {
            get { return player.ZoneStat1; }
            set { player.ZoneStat1 = value; }
        }

        /// <summary>
        /// Gets or esets the number of games this player has lost.
        /// </summary>
        public int GamesLost
        {
            get { return player.ZoneStat2; }
            set { player.ZoneStat2 = value; }
        }

        /// <summary>
        /// Time in seconds that the player has carried at least one flag for.
        /// </summary>
        public int CarryTimeSeconds
        {
            get { return player.ZoneStat3; }
            set { player.ZoneStat3 = value; }
        }

        /// <summary>
        /// Cumulative time in seconds that the player has carried flags.
        /// </summary>
        public int CarryTimeSecondsPlus
        {
            get { return player.ZoneStat4; }
            set { player.ZoneStat4 = value; }
        }

        /// <summary>
        /// Number of times that a player has captured a flag - from actual pickup/killing a carrier and picking their flag up.
        /// </summary>
        public int Captures
        {
            get { return player.ZoneStat5; }
            set { player.ZoneStat5 = value; }
        }

        /// <summary>
        /// Amount of times a flag carrier gets a kill.
        /// </summary>
        public int CarryKills
        {
            get { return player.ZoneStat6; }
            set { player.ZoneStat6 = value; }
        }

        /// <summary>
        /// Amount of times a player kills a flag carrier.
        /// </summary>
        public int CarrierKills
        {
            get { return player.ZoneStat7; }
            set { player.ZoneStat7 = value; }
        }
    }

    /// <summary>
    /// Models a single CTF map (i.e. playable area) with specific teams and flag coordinates.
    /// </summary>
    /// <remarks>
    /// Consider doing this properly and loading it from a file you lazy bums.
    /// </remarks>
    class CTFMap
    {
        public string MapName { get; set; }

        /// <summary>
        /// If set to true, the coordinates of the flags are randomized and the given positions are ignored and only the Flag ID
        /// is used.
        /// </summary>
        public bool RandomizeFlagLocations = false;

        public List<string> TeamNames = new List<string>();

        /// <summary>
        /// List of flags for this map. coordinates must be multiplied by 16 as per the actual in-game coordinates (coord specified * 16).
        /// </summary>
        public List<MapFlagEntry> Flags = new List<MapFlagEntry>();
    }

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
        private int lastStatsWriteMs;

        private int minPlayers;
        private int preGamePeriod;

        private Team winningTeam;
        private int winningTeamTick;
        private int winningTeamNotify;
        private int victoryHoldTime;
        private bool gameWon;

        private GameState gameState;
        private CTFMode flagMode;

        // Create only one so that we aren't doing needless allocations all the time.
        private CTFPlayerStatsProxy ctfPlayerProxy = new CTFPlayerStatsProxy();

        private bool isOVD = false;
        private Team notPlaying;
        private Team playing;
        private Team spec;
        private List<Arena.FlagState> _flags;

        private Dictionary<string, Base> bases;

        private List<CTFMap> availableMaps = new List<CTFMap>();

        private CTFMap currentMap = null;

        private class Base
        {
            public Base(short posX, short posY, short fposX, short fposY)
            {
                x = (short)(posX * 16);
                y = (short)(posY * 16);

                flagX = (short)(fposX * 16);
                flagY = (short)(fposY * 16);

            }
            public short x;
            public short y;
            public short flagX;
            public short flagY;
        }

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

        private void InitializeMaps()
        {
            // Initialize our hardcoded maps. Note that we should really move these into a json file eventually.
            // NOTE: This is to be called _after_ `_flags` is initialized because it depends on whether the
            // arena is OVD or not.

            availableMaps.Clear();

            CTFMap def = new CTFMap();
            def.MapName = "default";
            def.TeamNames.Add(CFG.teams[0].name);
            def.TeamNames.Add(CFG.teams[1].name);

            // For default, we are interested in the original flag placement; so we will extract
            // those. Note that if this code does not work, we will probably have to investigate
            // at what point in time we need to query the flags to get the position.

            // Add dummy flags based on however many flags the arena actually has, because we will be
            // randomizing their positions anyway.
            def.RandomizeFlagLocations = true;

            foreach (var fs in _flags)
            {
                var flagName = fs.flag.GeneralData.Name.ToLower().Trim('\"');

                def.Flags.Add(new MapFlagEntry(flagName, 202, 118));
            }

            availableMaps.Add(def);

            CTFMap full = new CTFMap();
            full.MapName = "full";
            full.TeamNames.Add("Titan Militia");
            full.TeamNames.Add("Collective");
			full.Flags.Add(new MapFlagEntry("Hill201", 54, 32));
            full.Flags.Add(new MapFlagEntry("Bridge1", 202, 120));
            full.Flags.Add(new MapFlagEntry("Bridge2", 202, 202));
            full.Flags.Add(new MapFlagEntry("Bridge3", 202, 286));
            full.Flags.Add(new MapFlagEntry("Hill86", 316, 338));
            full.Flags.Add(new MapFlagEntry("sdFlag", 0, 0));

            availableMaps.Add(full);

            CTFMap bravo = new CTFMap();
            bravo.MapName = "bravo";
            bravo.TeamNames.Add(CFG.teams[0].name); // these _should_ be the default two teams.
            bravo.TeamNames.Add(CFG.teams[1].name);
            bravo.Flags.Add(new MapFlagEntry("flag1", 500, 1333));
            bravo.Flags.Add(new MapFlagEntry("flag2", 500, 1213));
            bravo.Flags.Add(new MapFlagEntry("flag3", 744, 1564));
            bravo.Flags.Add(new MapFlagEntry("flag4", 864, 1564));

            availableMaps.Add(bravo);

            currentMap = full; // Set full map as the ... default ... event.
        }

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

            _flags = new List<Arena.FlagState>();
            minPlayers = 2;
            victoryHoldTime = CFG.flag.victoryHoldTime;
            preGamePeriod = CFG.flag.startDelay;

            killStreaks = new Dictionary<string, PlayerStreak>();
            explosives = new Dictionary<string, int>();

            bases = new Dictionary<string, Base>();

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

            if (arena._name.ToLower().Contains("ovd|ctfdl"))
            {
                foreach (Arena.FlagState fs in arena._flags.Values)
                {
                    if (fs.flag.FlagData.MinPlayerCount == 200)
                        _flags.Add(fs);     
                }

                playing = new Team(arena, arena._server);
                playing._name = "Playing";
                playing._id = (short)arena.Teams.Count();
                playing._password = "";
                playing._owner = null;
                playing._isPrivate = true;
                arena.createTeam(playing);

                bases["A7"] = new Base(21, 474, 9, 483);
                bases["D7"] = new Base(279, 504, 267, 491);
                bases["F8"] = new Base(412, 575, 390, 563);
                bases["F4"] = new Base(422, 271, 413, 263);
                bases["A5"] = new Base(57, 359, 37, 349);

                isOVD = true;
            }
            else
            {
                foreach (Arena.FlagState fs in arena._flags.Values)
                {
                    if (fs.flag.FlagData.MinPlayerCount == 0)
                        _flags.Add(fs);
                }
            }

            InitializeMaps();

            return true;
        }

        /*//////////////////////////////////////////////////
        // Class change announcement logic
        *///////////////////////////////////////////////////

        // Dictionaries to track the last skill name, last announcement time, and to track if a player has ever been on a non-SPEC team for each player
        private Dictionary<Player, string> playerLastSkillNames = new Dictionary<Player, string>();
        private Dictionary<Player, DateTime> lastAnnouncementTimes = new Dictionary<Player, DateTime>();
        Dictionary<Player, bool> playerHasPlayed = new Dictionary<Player, bool>();

        // Grace period for announcements (in seconds)
        private const int AnnouncementGracePeriod = 30;

        // Helper method to retrieve the primary skill name from the player's skills dictionary
        private string GetPrimarySkillName(Player player)
        {
            // Assuming the primary skill is the first entry or has a specific key
            if (player._skills != null && player._skills.Count > 0)
            {
                // Get the first skill from the dictionary
                foreach (var skillItem in player._skills.Values)
                {
                    return skillItem.skill.Name; // Access the Name property from SkillInfo
                }
            }
            return "Unknown"; // Default to "Unknown" if no skill is found
        }

        /// <summary>
        /// CTF Script poll called by our arena
        /// </summary>
        public bool poll()
        {
            // Loop through each player in the arena
            foreach (Player player in arena.Players)
            {
                // We need to get the current skill name from the player's skills dictionary
                string currentSkillName = GetPrimarySkillName(player);

                // Get the player's current team name
                string currentTeamName = player._team != null ? player._team._name : "SPEC";

                // Check if the player's skill name has changed
                if (!playerLastSkillNames.ContainsKey(player) || playerLastSkillNames[player] != currentSkillName)
                {
                    // Check if the player has ever been on a non-SPEC team
                    if (!playerHasPlayed.ContainsKey(player))
                    {
                        playerHasPlayed[player] = false; // Default to false if not tracked yet
                    }

                    // If the player is currently on a non-SPEC team, mark them as having played
                    if (currentTeamName != "spec")
                    {
                        playerHasPlayed[player] = true; // Mark the player as having played
                    }

                    // Only announce class changes for players who have ever been on a non-SPEC team
                    if (playerHasPlayed[player])
                    {
                        // Update the dictionary with the new skill name
                        playerLastSkillNames[player] = currentSkillName;

                        // Check if the player is within the grace period for announcements
                        bool withinGracePeriod = lastAnnouncementTimes.ContainsKey(player) &&
                                                (DateTime.Now - lastAnnouncementTimes[player]).TotalSeconds < AnnouncementGracePeriod;

                        // Announce the player's skill change if it is Infiltrator and not within the grace period
                        if (currentSkillName == "Infiltrator" && !withinGracePeriod)
                        {
                            // Make the actual announcement
                            arena.sendArenaMessage("CLASS SWAP------ " + currentTeamName + "------" + currentSkillName + "------" + player._alias + ".", 14);

                            // Update the last announcement time for the player
                            lastAnnouncementTimes[player] = DateTime.Now;
                        }
                    }
                }
            }
            
            /*//////////////////////////////////////////////////
            // Game state management
            /*//////////////////////////////////////////////////
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
        
        private bool SpawnMapPlayers()
        {
            if (currentMap != null)
            {
                var teamA = arena.getTeamByName(currentMap.TeamNames[0]);
                var teamB = arena.getTeamByName(currentMap.TeamNames[1]);

                // This will more or less swizzle/splice the teams evenly
                // but we should really come up with a nice random method.

                foreach(var player in arena.PlayersIngame)
                {
                    if (teamA.ActivePlayerCount < teamB.ActivePlayerCount)
                    {
                        teamA.addPlayer(player);
                    }
                    else
                    {
                        teamB.addPlayer(player);
                    }
                }
            }

            // Scramble the teams.
            ScriptArena.scrambleTeams(arena, arena._server._zoneConfig.arena.desiredFrequencies, true);

            return true;
        }
        private bool SpawnMapFlags()
        {
            foreach(var flag in currentMap.Flags)
            {
                var fs = arena.getFlag(flag.Item1);

                if (fs == null)
                {
                    return false;
                }

                // Check if the flag is "sdFlag" and set it inactive
                if (flag.Item1.Equals("sdFlag", StringComparison.OrdinalIgnoreCase))
                {
                    fs.bActive = false;  // Make sure sdFlag is inactive by default
                    fs.posX = 0;         // You can position it off the map or at a default position
                    fs.posY = 0;
                    continue;            // Skip the rest of the logic for sdFlag
                }

                bool bActive = true;

                if (currentMap.RandomizeFlagLocations)
                {
                    bActive = RandomizeFlagLocation(fs);
                }
                else
                {
                    fs.posX = (short)(flag.Item2 * 16);
                    fs.posY = (short)(flag.Item3 * 16);
                }
                
                fs.bActive = bActive;
                fs.team = null;
                fs.carrier = null;

                Helpers.Object_Flags(arena.Players, fs);
            }

            return true;
        }

        //Spawn first flag
        private bool SpawnMapFlag()
        {
            var flag = currentMap.Flags[0];
            var fs = arena.getFlag(flag.Item1);
                

            if (fs == null)
            {
                return false;
            }

            bool bActive = true;

            if (currentMap.RandomizeFlagLocations)
            {
                bActive = RandomizeFlagLocation(fs);
            }
            else
            {
                fs.posX = (short)(flag.Item2 * 16);
                fs.posY = (short)(flag.Item3 * 16);
            }
               
            fs.bActive = bActive;
            fs.team = null;
            fs.carrier = null;

            Helpers.Object_Flags(arena.Players, fs);
            
            return true;
        }

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
                    delegate ()
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

                int tick = (int)Math.Ceiling((winningTeamTick - now) / 1000.0f);
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

            int countdown = winningTeamTick > 0 ? (int)Math.Ceiling((winningTeamTick - now) / 1000.0f) : 0;
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
            UpdateFlagCarryStats(now);
        }

        /// <summary>
        /// Called when the game begins
        /// </summary>
        [Scripts.Event("Game.Start")]
        public bool StartGame()
        {
            //Reset Flags
            arena.flagReset();

            SpawnMapFlags();

            if (!isOVD)
            {
                // Let's not disturb the teams that OvD uses.
                SpawnMapPlayers();
            }
            
            HealAll();

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
        /// Attempts to spawn a given flag; returns true if successful, false if no suitable location found.
        /// </summary>
        public bool RandomizeFlagLocation(Arena.FlagState fs)
        {   //Set offsets
            int levelX = arena._server._assets.Level.OffsetX * 16;
            int levelY = arena._server._assets.Level.OffsetY * 16;

            //Give it some valid coordinates
            int attempts = 0;
            do
            {   //Make sure we're not doing this infinitely
                if (attempts++ > 200)
                {
                    Log.write(TLog.Error, "Unable to satisfy flag spawn for '{0}'.", fs.flag);
                    return false;
                }

                fs.posX = (short)(fs.flag.GeneralData.OffsetX - levelX);
                fs.posY = (short)(fs.flag.GeneralData.OffsetY - levelY);
                fs.oldPosX = fs.posX;
                fs.oldPosY = fs.posY;

                //Taken from Math.cs
                //For random flag spawn if applicable
                int lowerX = fs.posX - ((short)fs.flag.GeneralData.Width / 2);
                int higherX = fs.posX + ((short)fs.flag.GeneralData.Width / 2);
                int lowerY = fs.posY - ((short)fs.flag.GeneralData.Height / 2);
                int higherY = fs.posY + ((short)fs.flag.GeneralData.Height / 2);

                //Clamp within the map coordinates
                int mapWidth = (arena._server._assets.Level.Width - 1) * 16;
                int mapHeight = (arena._server._assets.Level.Height - 1) * 16;

                lowerX = Math.Min(Math.Max(0, lowerX), mapWidth);
                higherX = Math.Min(Math.Max(0, higherX), mapWidth);
                lowerY = Math.Min(Math.Max(0, lowerY), mapHeight);
                higherY = Math.Min(Math.Max(0, higherY), mapHeight);

                //Randomly generate some coordinates!
                int tmpPosX = ((short)arena._rand.Next(lowerX, higherX));
                int tmpPosY = ((short)arena._rand.Next(lowerY, higherY));

                //Check for allowable terrain drops
                int terrainID = arena.getTerrainID(tmpPosX, tmpPosY);
                for (int terrain = 0; terrain < 15; terrain++)
                {
                    if (terrainID == terrain && fs.flag.FlagData.FlagDroppableTerrains[terrain] == 1)
                    {
                        fs.posX = (short)tmpPosX;
                        fs.posY = (short)tmpPosY;
                        fs.oldPosX = fs.posX;
                        fs.oldPosY = fs.posY;
                        break;
                    }
                }

                //Check the terrain settings
                if (arena.getTerrain(fs.posX, fs.posY).flagTimerSpeed == 0)
                    continue;

            }
            while (arena.getTile(fs.posX, fs.posY).Blocked);

            return true;
        }

        /// <summary>
        /// Called when the game ends
        /// </summary>
        [Scripts.Event("Game.End")]
        public bool EndGame()
        {
            if (!isOVD)
            {
                if (winningTeam == null)
                {
                    arena.sendArenaMessage("There was no winner.");
                }
                else
                {
                    UpdateGameEndFlagStats();

                    arena.sendArenaMessage(winningTeam._name + " has won the game!");
                    winningTeam = null;
                }
            }
            else
            {
                arena.sendArenaMessage("&Game has ended, Host may either *reset to spec all, or *restart for a rematch", 3);
            }

            gameState = GameState.PostGame;
            arena.flagReset();

            return true;
        }

        /// <summary>
        /// Called to reset the game state
        /// </summary>
        [Scripts.Event("Game.Reset")]
        public bool gameReset()
        {
            gameState = GameState.PostGame;
            arena.flagReset();
            ResetKiller(null);
            killStreaks.Clear();


            if (isOVD)
            {
                arena.sendArenaMessage("& ----=[ Please type ?team spec or ?playing to ready up for the next game! ]=----", 30);

                //Spec all in game players
                foreach (Player p in arena.Players)
                    p.spec("np");
            }

            return true;
        }


        #endregion

        #region Player Events

        /// <summary>
        /// Called when a player requests to pick up/drop the flag.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="bPickup"></param>
        /// <param name="bSuccess"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        [Scripts.Event("Player.FlagAction")]
        public bool playerFlagAction(Player from, bool bPickup, bool bSuccess, LioInfo.Flag flag)
        {
            if (bPickup && bSuccess)
            {
                ctfPlayerProxy.player = from;
                ctfPlayerProxy.Captures++;
                ctfPlayerProxy.player = null;
            }

            return true;
        }

        /// <summary>
        /// Called when a player sends a chat command
        /// </summary>
        [Scripts.Event("Player.ChatCommand")]
        public bool playerChatCommand(Player player, Player recipient, string command, string payload)
        {
            switch (command.ToLower())
            {
                case "playing":
                    player.spec("spec");
                    break;
                case "np":
                    {
                        player.spec();
                    }
                    break;
            }

            return true;
        }


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
            {
                return true;
            }

            UpdateKiller(killer);

            if (killStreaks.ContainsKey(victim._alias))
            {
                long wepTick = killStreaks[victim._alias].lastUsedWepTick;

                if (wepTick != -1)
                {
                    UpdateWeaponKill(killer);
                }
            }

            // TODO: Remove these unnecessary null checks - killer/victim must be defined objects here.
            if (killer != null && victim != null && victim._bounty >= 300)
            {
                arena.sendArenaMessage(String.Format("{0} has ended {1}'s bounty.", killer._alias, victim._alias), 5);
            }

            bool bVictimCarrier = arena._flags.Values.Any(fs => fs.carrier == victim);
            bool bKillerCarrier = arena._flags.Values.Any(fs => fs.carrier == killer);

            if (bVictimCarrier)
            {
                ctfPlayerProxy.player = killer;
                ctfPlayerProxy.CarrierKills++;
                ctfPlayerProxy.player = null;
            }

            if (bKillerCarrier)
            {
                ctfPlayerProxy.player = killer;
                ctfPlayerProxy.CarryKills++;
                ctfPlayerProxy.player = null;
            }

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
            {
                return true;
            }

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
            if (isOVD)
            {
                player.sendMessage(3, "&Welcome to Offense vs Defense. Please type ?playing if you wish to play!");

                if (player.PermissionLevel > 0 || player._permissionTemp > 0)
                {
                    player.sendMessage(0, "#If you are hosting OvDs, please use *endgame to spec all. This will automatically trigger the playing/not playing scripting");
                }
            }
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
/*
            if (!isOVD)
            {
                // obtain spawn coordinates from the current map.
                var teamA = arena.getTeamByName(currentMap.TeamNames[0]);
                var teamB = arena.getTeamByName(currentMap.TeamNames[1]);

                if (teamA.ActivePlayerCount < teamB.ActivePlayerCount)
                {
                    player.unspec(teamA);
                }
                else
                {
                    player.unspec(teamB);
                }

                player._lastMovement = Environment.TickCount;
                player._maxTimeCalled = false;

                return false;
            }
*/
            return true;
        }

        /// <summary>
        /// Called when a player sends a mod command
        /// </summary>
        [Scripts.Event("Player.ModCommand")]
        public bool playerModCommand(Player player, Player recipient, string command, string payload)
        {
            command = (command.ToLower());

            if (command.Equals("setup"))
            {
                if (player.PermissionLevelLocal < Data.PlayerPermission.ArenaMod)
                    return false;

                if (!bases.ContainsKey(payload.ToUpper()))
                {
                    player.sendMessage(-1, "That base is not recognized, Options are: ");
                    foreach (string key in bases.Keys)
                        player.sendMessage(0, key);

                    return true;
                }

                Base defense = bases[payload.ToUpper()];

                arena.itemSpawn(arena._server._assets.getItemByID(2005), 150, defense.x, defense.y, 100, null);
                arena.itemSpawn(arena._server._assets.getItemByID(2009), 150, defense.x, defense.y, 100, null);
                //arena.itemSpawn(arena._server._assets.getItemByID(23), 2, defense.x, defense.y, 100, null);
                //arena.itemSpawn(arena._server._assets.getItemByID(10), 2, defense.x, defense.y, 100, null);
                //arena.itemSpawn(arena._server._assets.getItemByID(11), 1, defense.x, defense.y, 100, null);
                //arena.itemSpawn(arena._server._assets.getItemByID(9), 1, defense.x, defense.y, 100, null);
                Arena.FlagState flag = arena.getFlag("Bridge3");

                flag.posX = defense.flagX;
                flag.posY = defense.flagY;

                Helpers.Object_Flags(arena.Players, flag);
                arena.sendArenaMessage(String.Format("&Minerals, flag, and auto-kits dropped at {0}", payload.ToUpper()));
                return true;
            }

            if (command.Equals("sd"))
            {
                if (player.PermissionLevelLocal < Data.PlayerPermission.ArenaMod)
                    return false;
                
                arena.flagReset();
                //SpawnMapFlag();
                //Arena.FlagState flag = arena.getFlag("Hill201");
                // Get the "sdFlag" and activate it
                Arena.FlagState flag = arena.getFlag("sdFlag");

                if (flag == null)
                {
                    player.sendMessage(-1, "Sudden death flag not found.");
                    return false;
                }

                // Set the flag to active
                flag.bActive = true;

                int sdrr = Environment.TickCount % 5; //randomizes location of flag based on TickCount - sudden death random respawn
                switch (sdrr)
                {
                    case 0:
                        flag.posX = (short)(202 * 16);
                        flag.posY = (short)(120 * 16);
                        break;
                    case 1:
                        flag.posX = (short)(202 * 16);
                        flag.posY = (short)(202 * 16);
                        break;
                    case 2:
                        flag.posX = (short)(202 * 16);
                        flag.posY = (short)(286 * 16);
                        break;
                    case 3:
                        flag.posX = (short)(595 * 16);
                        flag.posY = (short)(125 * 16);
                        break;
                    case 4:
                        flag.posX = (short)(718 * 16);
                        flag.posY = (short)(218 * 16);
                        break;
                }

                Helpers.Object_Flags(arena.Players, flag);

				arena.sendArenaMessage("!+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+", 30);
                arena.sendArenaMessage("&|  S U D D E N   D E A T H  |");
				arena.sendArenaMessage("!+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+");
                arena.sendArenaMessage("&|  S U D D E N   D E A T H  |");
				arena.sendArenaMessage("!+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+");
                arena.sendArenaMessage("&|  S U D D E N   D E A T H  |");
                arena.sendArenaMessage("!+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+", 17);


                
                return true;
            }


            if (command.Equals("healall"))
            {
                if (player.PermissionLevelLocal < Data.PlayerPermission.ArenaMod)
                {
                    return false;
                }

                HealAll();
                return true;
            }

            if (command == "map")
            {
                var mapNames = string.Join(", ", availableMaps.Select(x => x.MapName));

                if (string.IsNullOrWhiteSpace(payload))
                {
                    player.sendMessage(-1, "Available map options are: " + mapNames);
                }
                else
                {
                    var requestedEvent = availableMaps.FirstOrDefault(x => x.MapName == payload);

                    if (requestedEvent != null)
                    {
                        player.sendMessage(-1, "Switching to map " + requestedEvent.MapName);
                        currentMap = requestedEvent;

                        SpawnMapPlayers();
                        SpawnMapFlags();
                    }
                    else
                    {
                        player.sendMessage(-1, "Map with that name not found. Available options are: " + mapNames);
                    }
                }
                
                return true;
            }

            return false;
        }

        #endregion

        #region Updaters

        private void UpdateGameEndFlagStats()
        {
            foreach(var player in arena.PlayersIngame)
            {
                if (player._team == winningTeam)
                {
                    ctfPlayerProxy.player = player;
                    ctfPlayerProxy.GamesWon++;
                    ctfPlayerProxy.player = null;
                }
                else
                {
                    ctfPlayerProxy.player = player;
                    ctfPlayerProxy.GamesLost++;
                    ctfPlayerProxy.player = null;
                }
            }
        }

        /// <summary>
        /// Updates the stats for flag carriers. Not that this is supposed to be
        /// executed once per second.
        /// </summary>
        private void UpdateFlagCarryStats(int nowMs)
        {
            if (nowMs - lastStatsWriteMs < 1000) {
                return;
            }

            lastStatsWriteMs = nowMs;

            var dict = new Dictionary<Player, int>();

            foreach (Arena.FlagState fs in arena._flags.Values)
            {
                if (fs.carrier != null)
                {
                    if (!dict.ContainsKey(fs.carrier))
                    {
                        dict.Add(fs.carrier, 0);
                    }

                    dict[fs.carrier]++;
                }
            }

            foreach(var d in dict)
            {
                ctfPlayerProxy.player = d.Key;
                ctfPlayerProxy.CarryTimeSeconds++; // 1 second.
                ctfPlayerProxy.CarryTimeSecondsPlus += d.Value; // 1 second * number of flags.
                ctfPlayerProxy.player = null;
            }
        }

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

            arena.setTicker(2, 3, 0, delegate (Player p)
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

        private void HealAll()
        {
            foreach (Player p in arena.PlayersIngame)
            {
                p.inventoryModify(104, 1);
            }
            
            arena.sendArenaMessage("&All players have been healed");
        }

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

public static class ArenaExtensions
{
    /// <summary>
    /// Spawns the given item randomly in the specified area
    /// </summary>
    public static void spawnItemInArea(this Arena arena, ItemInfo item, ushort quantity, short x, short y, short radius)
    {       //Sanity
        if (quantity <= 0)
            return;

        int blockedAttempts = 30;

        short pX;
        short pY;
        while (true)
        {
            pX = x;
            pY = y;
            Helpers.randomPositionInArea(arena, radius, ref pX, ref pY);
            if (arena.getTile(pX, pY).Blocked)
            {
                blockedAttempts--;
                if (blockedAttempts <= 0)
                    //Consider the spawn to be blocked
                    return;
                continue;
            }
            arena.itemSpawn(item, quantity, pX, pY, null);
            break;
        }
    }
}
