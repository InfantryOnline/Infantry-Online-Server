using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using InfServer.Logic;
using InfServer.Game;
using InfServer.Protocol;
using InfServer.Bots;

using Assets;

namespace InfServer.Script.GameType_USL
{
    public class GamePlay : Settings
    {
        public GameTypes _gameType = GameTypes.NULL;
        public EventTypes _eventType;
        public SpawnEventTypes _spawnEventType;
        private Arena _arena;
        private CfgInfo _config;

        #region Stat Recording
        private string FileName;
        private Team lastTeam1, lastTeam2;  //Records the previous team before overtime starts(We get team stats from this since ot is not recorded)
        public Team victoryTeam = null;
        DateTime startTime;

        /// <summary>
        /// Current game player stats
        /// </summary>
        private Dictionary<string, PlayerStat> _savedPlayerStats;
        /// <summary>
        /// Only used when overtime is about to be initiated
        /// </summary>
        private Dictionary<string, PlayerStat> _lastSavedStats;

        /// <summary>
        /// Stores our player information
        /// </summary>
        private class PlayerStat
        {
            public Player player { get; set; }
            public string teamname { get; set; }
            public string alias { get; set; }
            public string squad { get; set; }
            public long points { get; set; }
            public int kills { get; set; }
            public int deaths { get; set; }
            public int killPoints { get; set; }
            public int assistPoints { get; set; }
            public int bonusPoints { get; set; }
            public int playSeconds { get; set; }
            public bool hasPlayed { get; set; }
            public string classType { get; set; }

            //Kill stats
            public ItemInfo.Projectile lastUsedWep { get; set; }
            public int lastUsedWepKillCount { get; set; }
            public long lastUsedWepTick { get; set; }
            public int lastKillerCount { get; set; }

            //Medic stats
            public int potentialHealthHealed { get; set; }
        }
        #endregion

        #region Misc Gameplay Pointers
        private Team BountyA, BountyB;
        private Player lastKiller;
        
        /// <summary>
        /// Resets the last killer object
        /// </summary>
        public void ResetKiller(Player killer)
        {
            lastKiller = killer;
        }

        /// <summary>
        /// Resets the weapon ticker to default (Time Expired)
        /// </summary>
        public void ResetWeaponTicker(Player target)
        {
            if (_savedPlayerStats.ContainsKey(target._alias))
            {
                _savedPlayerStats[target._alias].lastUsedWep = null;
                _savedPlayerStats[target._alias].lastUsedWepKillCount = 0;
                _savedPlayerStats[target._alias].lastUsedWepTick = -1;
            }
        }

        /// <summary>
        /// Updates the killer and their counter
        /// </summary>
        public void UpdateKiller(Player killer)
        {
            if (_savedPlayerStats.ContainsKey(killer._alias))
            {
                _savedPlayerStats[killer._alias].lastKillerCount++;
                switch(_savedPlayerStats[killer._alias].lastKillerCount)
                {
                    case 6:
                        _arena.sendArenaMessage(String.Format("{0} is on fire!", killer._alias), 17);
                        break;
                    case 8:
                        _arena.sendArenaMessage(String.Format("Someone kill {0}!", killer._alias), 18);
                        break;
                    case 10:
                        _arena.sendArenaMessage(String.Format("{0} is dominating!", killer._alias), 19);
                        break;
                    case 12:
                        _arena.sendArenaMessage(String.Format("DEATH TO {0}!", killer._alias), 30);
                        break;
                }
            }

            //Is this first blood?
            if (lastKiller == null)
            {
                //It is, lets make the sound
                _arena.sendArenaMessage(String.Format("{0} has drawn first blood.", killer._alias), 9);
            }
            lastKiller = killer;
        }

        /// <summary>
        /// Updates the victim's kill streaks
        /// </summary>
        public void UpdateDeath(Player victim, Player killer)
        {
            if (_savedPlayerStats.ContainsKey(victim._alias))
            {
                if (_savedPlayerStats[victim._alias].lastKillerCount >= 6)
                {
                    _arena.sendArenaMessage(String.Format("{0}", killer != null ? killer._alias + " has ended " + victim._alias + "'s kill streak." :
                        victim._alias + "'s kill streak has ended."), 7);
                }
                _savedPlayerStats[victim._alias].lastKillerCount = 0;
                //_savedPlayerStats[victim._alias].lastUsedWep = null;
                //_savedPlayerStats[victim._alias].lastUsedWepKillCount = 0;
                //_savedPlayerStats[victim._alias].lastUsedWepTick = -1;
            }
        }

        /// <summary>
        /// Updates the last fired weapon and the ticker
        /// </summary>
        public void UpdateWeapon(Player from, ItemInfo.Projectile usedWep)
        {
            if (_savedPlayerStats.ContainsKey(from._alias))
            {
                _savedPlayerStats[from._alias].lastUsedWep = usedWep;
                //500 = Alive time for the schrapnel after main weap explosion
                _savedPlayerStats[from._alias].lastUsedWepTick = DateTime.Now.AddTicks(500).Ticks;
            }
        }

        /// <summary>
        /// Updates the last weapon kill counter
        /// </summary>
        public void UpdateWeaponKill(Player from)
        {
            if (_savedPlayerStats.ContainsKey(from._alias))
            {
                if (_savedPlayerStats[from._alias].lastUsedWep == null)
                    return;
                _savedPlayerStats[from._alias].lastUsedWepKillCount++;
                ItemInfo.Projectile lastUsedWep = _savedPlayerStats[from._alias].lastUsedWep;
                if (lastUsedWep.name.Contains("Combat Knife"))
                    _arena.sendArenaMessage(String.Format("{0} is throwing out the knives.", from._alias), 6);

                switch (_savedPlayerStats[from._alias].lastUsedWepKillCount)
                {
                    case 2:
                        _arena.sendArenaMessage(String.Format("{0} just got a double {1} kill.", from._alias, lastUsedWep.name), 13);
                        break;
                    case 3:
                        _arena.sendArenaMessage(String.Format("{0} just got a triple {1} kill.", from._alias, lastUsedWep.name), 14);
                        break;
                    case 4:
                        _arena.sendArenaMessage(String.Format("A 4 {0} kill by {0}?!?", lastUsedWep.name, from._alias), 15);
                        break;
                    case 5:
                        _arena.sendArenaMessage(String.Format("Unbelievable! {0} with the 5 {1} kill?", from._alias, lastUsedWep.name), 16);
                        break;
                }
            }
        }

        /// <summary>
        /// Gets our current filename
        /// </summary>
        public string GetFileName
        {
            get { return FileName; }
        }

        /// <summary>
        /// Gets an available list of game play types
        /// </summary>
        private List<string> GetEventTypes()
        {
            Team check1 = null, check2 = null;
            List<string> types = new List<string>();
            foreach (Enum e in Enum.GetValues(typeof(EventTypes)))
            {
                switch ((EventTypes)e)
                {
                    case EventTypes.RedBlue:
                        check1 = _arena.getTeamByName("Red");
                        check2 = _arena.getTeamByName("Blue");
                        if (GetEventTypes(check1, check2) == true)
                            types.Add(Enum.GetName(typeof(EventTypes), e));
                        break;
                    case EventTypes.WhiteBlack:
                        check1 = _arena.getTeamByName("White");
                        check2 = _arena.getTeamByName("Black");
                        if (GetEventTypes(check1, check2) == true)
                            types.Add(Enum.GetName(typeof(EventTypes), e));
                        break;
                    case EventTypes.PinkPurple:
                        check1 = _arena.getTeamByName("Pink");
                        check2 = _arena.getTeamByName("Purple");
                        if (GetEventTypes(check1, check2) == true)
                            types.Add(Enum.GetName(typeof(EventTypes), e));
                        break;
                    case EventTypes.OrangeGray:
                        check1 = _arena.getTeamByName("Orange");
                        check2 = _arena.getTeamByName("Gray");
                        if (GetEventTypes(check1, check2) == true)
                            types.Add(Enum.GetName(typeof(EventTypes), e));
                        break;
                    case EventTypes.GreenYellow:
                        check1 = _arena.getTeamByName("Green");
                        check2 = _arena.getTeamByName("Yellow");
                        if (GetEventTypes(check1, check2) == true)
                            types.Add(Enum.GetName(typeof(EventTypes), e));
                        break;
                    case EventTypes.GoldSilver:
                        check1 = _arena.getTeamByName("Gold");
                        check2 = _arena.getTeamByName("Silver");
                        if (GetEventTypes(check1, check2) == true)
                            types.Add(Enum.GetName(typeof(EventTypes), e));
                        break;
                    case EventTypes.BronzeDiamond:
                        check1 = _arena.getTeamByName("Bronze");
                        check2 = _arena.getTeamByName("Diamond");
                        if (GetEventTypes(check1, check2) == true)
                            types.Add(Enum.GetName(typeof(EventTypes), e));
                        break;
                }
            }

            return types;
        }

        private bool GetEventTypes(Team check1, Team check2)
        {
            if (check1 == null || check2 == null)
                return false;

            CfgInfo teamInfo = _arena._server._zoneConfig;
            //Crap way of doing this, need to get the warp parameter id
            int i = 0, team1 = 0, team2 = 0;
            foreach (CfgInfo.TeamInfo info in teamInfo.teams)
            {
                if (info == check1._info)
                    team1 = i;

                if (info == check2._info)
                    team2 = i;
                i++;
            }

            //Find our warp points
            IEnumerable<LioInfo.WarpField> wGroup = _arena._server._assets.Lios.getWarpGroupByID(1); //Cfg exit spectator and other events
            LioInfo.WarpField warp1 = wGroup.FirstOrDefault(w => w.WarpFieldData.WarpModeParameter == team1);
            LioInfo.WarpField warp2 = wGroup.FirstOrDefault(w => w.WarpFieldData.WarpModeParameter == team2);
            int levelX = _arena._server._assets.Level.OffsetX * 16;
            int levelY = _arena._server._assets.Level.OffsetY * 16;
            if (warp1 == null || warp2 == null)
                return false;

            //Check first warp then second
            if (warp1.GeneralData.OffsetX / 16 - levelX > _arena._server._assets.Level.Width ||
                warp1.GeneralData.OffsetY / 16 - levelY > _arena._server._assets.Level.Height)
                return false;

            if (warp2.GeneralData.OffsetX / 16 - levelX > _arena._server._assets.Level.Width ||
                warp2.GeneralData.OffsetY / 16 - levelY > _arena._server._assets.Level.Height)
                return false;

            return true;
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Generic Constructor which sets our arena
        /// </summary>
        public GamePlay(Arena arena)
        { _arena = arena; }

        /// <summary>
        /// Initiates our game play object and sets any related pointers
        /// </summary>
        public void Initiate()
        {
            _savedPlayerStats = new Dictionary<string, PlayerStat>();
            _config = _arena._server._zoneConfig;

            //Lets see what season we are in (Match related)
            string season = Logic_File.GetSeasonDirectory();
            if (!Int32.TryParse(season.Substring(season.Length - 1, 1), out LeagueSeason))
                LeagueSeason = 1;
            //Lets generate our list of available events
            CurrentEventTypes = GetEventTypes();
        }
        #endregion

        #region Game Functions
        /// <summary>
        /// Starts our game play type
        /// </summary>
        public bool GameStart()
        {
            if (_arena.ActiveTeams.Count() == 0)
                return false;

            bool isMatch = _arena._isMatch;
            ResetKiller(null);

            _savedPlayerStats.Clear();
            foreach (Player p in _arena.Players)
            {
                PlayerStat temp = new PlayerStat();
                temp.teamname = p._team._name;
                temp.alias = p._alias;
                temp.points = 0;
                temp.assistPoints = 0;
                temp.bonusPoints = 0;
                temp.killPoints = 0;
                temp.playSeconds = 0;
                temp.squad = p._squad;
                temp.kills = 0;
                temp.deaths = 0;
                temp.player = p;
                temp.hasPlayed = p.IsSpectator ? false : true;
                if (!p.IsSpectator)
                {
                    if (p._baseVehicle != null)
                        temp.classType = p._baseVehicle._type.Name;
                }

                temp.lastKillerCount = 0;
                temp.lastUsedWep = null;
                temp.lastUsedWepKillCount = 0;
                temp.lastUsedWepTick = -1;
                temp.potentialHealthHealed = 0;
                _savedPlayerStats.Add(p._alias, temp);
            }

            if (isMatch)
                //Lets match setup
                MatchSetup();

            //Doing any special events?
            if (SpawnEvent)
                SpawnEventItem();

            //Let everyone know
            _arena.sendArenaMessage("Game has started!");
            _arena.setTicker(1, 3, _config.deathMatch.timer * 100, "Time Left: ",
                delegate()
                {   //Trigger game end
                    _arena.gameEnd();
                }
            );

            UpdateTickers();
            return true;
        }

        /// <summary>
        /// Stops our game play
        /// </summary>
        public bool GameEnd()
        {
            switch (_gameType)
            {
                case GameTypes.LEAGUEMATCH:
                    //Make sure to get an accurate playing time
                    foreach (KeyValuePair<string, PlayerStat> pair in _savedPlayerStats)
                    {   //Did they play or exist?
                        if (!pair.Value.hasPlayed || pair.Value.player == null)
                            continue;
                        //Stats are migrated first due to game.end being called before our script callsync
                        pair.Value.playSeconds = pair.Value.player.StatsLastGame.playSeconds;
                    }

                    //Show the match results
                    MatchEnd();
                    break;

                case GameTypes.LEAGUEOVERTIME:
                    //Increase our count
                    OvertimeCount++;

                    //Show results
                    MatchEnd();
                    break;

                default:
                    //Dont need to do anything else
                    break;
            }
            return true;
        }
        #endregion

        #region Player Events
        /// <summary>
        /// Called when the statistical breakdown is displayed
        /// </summary>
        public void individualBreakdown(Player from, bool bCurrent)
        {	//Allows additional "custom" breakdown information
            from.sendMessage(0, "#Team Statistics Breakdown");

            IEnumerable<Team> activeTeams = _arena.Teams.OrderByDescending(entry => entry._currentGameKills).ToList();
            int idx = 3;	//Only display top three teams
            foreach (Team t in activeTeams)
            {
                if (t == null)
                    continue;

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

                from.sendMessage(0, String.Format(format,
                    t._currentGameKills, t._currentGameDeaths,
                    t._name));
            }

            from.sendMessage(0, "#Individual Statistics Breakdown");
            idx = 3;        //Only display the top 3 players
            List<Player> rankers = new List<Player>();
            foreach (Player p in _arena.Players.ToList())
            {
                if (p == null)
                    continue;
                if (_savedPlayerStats.ContainsKey(p._alias) && _savedPlayerStats[p._alias].hasPlayed)
                    rankers.Add(p);
            }

            if (rankers.Count > 0)
            {
                var rankedPlayerGroups = rankers.Select(player => new
                {
                    Alias = player._alias,
                    Kills = _savedPlayerStats[player._alias].kills,
                    Deaths = _savedPlayerStats[player._alias].deaths
                })
                .GroupBy(pl => pl.Kills)
                .OrderByDescending(k => k.Key)
                .Take(idx)
                .Select(g => g.OrderBy(plyr => plyr.Deaths));

                foreach (var group in rankedPlayerGroups)
                {
                    if (idx <= 0)
                        break;

                    string placeWord = "";
                    string format = " (K={0} D={1}): {2}";
                    switch (idx)
                    {
                        case 3:
                            placeWord = "!1st";
                            break;
                        case 2:
                            placeWord = "!2nd";
                            break;
                        case 1:
                            placeWord = "!3rd";
                            break;
                    }

                    idx -= group.Count();
                    if (group.First() != null)
                        from.sendMessage(0, String.Format(placeWord + format, group.First().Kills,
                            group.First().Deaths, String.Join(", ", group.Select(g => g.Alias))));
                }

                IEnumerable<Player> specialPlayers = rankers.OrderByDescending(player => _savedPlayerStats[player._alias].deaths);
                int topDeaths = (specialPlayers.First() != null ? _savedPlayerStats[specialPlayers.First()._alias].deaths : 0), deaths = 0;
                if (topDeaths > 0)
                {
                    from.sendMessage(0, "Most Deaths");
                    int i = 0;
                    List<string> mostDeaths = new List<string>();
                    foreach (Player p in specialPlayers)
                    {
                        if (p == null)
                            continue;

                        if (_savedPlayerStats[p._alias] != null)
                        {
                            deaths = _savedPlayerStats[p._alias].deaths;
                            if (deaths == topDeaths)
                            {
                                if (i++ >= 1)
                                    mostDeaths.Add(p._alias);
                                else
                                    mostDeaths.Add(String.Format("(D={0}): {1}", deaths, p._alias));
                            }
                        }
                    }
                    if (mostDeaths.Count > 0)
                    {
                        string s = String.Join(", ", mostDeaths.ToArray());
                        from.sendMessage(0, s);
                    }
                }

                IEnumerable<Player> Healed = rankers.Where(player => _savedPlayerStats[player._alias].potentialHealthHealed > 0);
                if (Healed.Count() > 0)
                {
                    IEnumerable<Player> mostHealed = Healed.OrderByDescending(player => _savedPlayerStats[player._alias].potentialHealthHealed);
                    idx = 3;
                    from.sendMessage(0, "&Most HP Healed");
                    foreach(Player p in mostHealed)
                    {
                        if (p == null) continue;
                        if (_savedPlayerStats[p._alias] != null)
                        {
                            if (idx-- <= 0)
                                break;

                            string placeWord = "&3rd";
                            string format = " (HP Total={0}): {1}";
                            switch (idx)
                            {
                                case 2:
                                    placeWord = "&1st";
                                    break;
                                case 1:
                                    placeWord = "&2nd";
                                    break;
                            }
                            from.sendMessage(0, String.Format(placeWord + format, _savedPlayerStats[p._alias].potentialHealthHealed, p._alias));
                        }
                    }
                }
            }

            if (_savedPlayerStats[from._alias] != null)
            {
                string personalFormat = "!Personal Score: (K={0} D={1})";
                from.sendMessage(0, String.Format(personalFormat,
                    _savedPlayerStats[from._alias].kills,
                    _savedPlayerStats[from._alias].deaths));
            }
        }

        /// <summary>
        /// Triggered when a player caused a weapon explosion
        /// </summary>
        public void playerPlayerExplosion(Player from, ItemInfo.Projectile usedWep)
        {
            if (usedWep.name.Contains("LAW") || usedWep.name.Contains("Hand Grenade")
                || usedWep.name.Contains("Grenade Launcher") || usedWep.name.Contains("Incendiary Grenade")
                || usedWep.name.Contains("Demo Charge") || usedWep.name.Contains("Combat Knife"))
                UpdateWeapon(from, usedWep);
        }

        /// <summary>
        /// Triggered when a player tries to heal
        /// </summary>
        public void PlayerRepair(Player from, ItemInfo.RepairItem item)
        {
            if (!_savedPlayerStats.ContainsKey(from._alias))
                return;
            if (item.repairType == 0 && item.repairDistance < 0)
            {   //Get all players near
                List<Player> players = _arena.getPlayersInRange(from._state.positionX, from._state.positionY, -item.repairDistance);
                int totalHealth = 0;
                foreach (Player p in players)
                {
                    if (p == null || p == from || p._state.health >= 100 || p._state.health <= 0)
                        continue;
                    totalHealth += (p._baseVehicle._type.Hitpoints - p._state.health);
                }
                _savedPlayerStats[from._alias].potentialHealthHealed += totalHealth;

                if (_gameType == GameTypes.LEAGUEMATCH)
                    from.ZoneStat4 += totalHealth;
            }
        }

        /// <summary>
        /// Triggered when one player has killed another
        /// </summary>
        public void playerPlayerKill(Player victim, Player killer)
        {   //Update our kill streak
            UpdateKiller(killer);

            if (_savedPlayerStats.ContainsKey(killer._alias))
            {
                _savedPlayerStats[killer._alias].kills++;
                long wepTick = _savedPlayerStats[killer._alias].lastUsedWepTick;
                if (wepTick != -1)
                    UpdateWeaponKill(killer);
            }
            if (_savedPlayerStats.ContainsKey(victim._alias))
                _savedPlayerStats[victim._alias].deaths++;

            if (killer != null && victim != null && victim._bounty >= 300)
                _arena.sendArenaMessage(String.Format("{0} has ended {1}'s bounty.", killer._alias, victim._alias), 8);
        }

        /// <summary>
        /// Triggered when a player has died, by any means
        /// </summary>
        /// <remarks>killer may be null if it wasn't a player kill</remarks>
        public void playerDeath(Player victim, Player killer, Helpers.KillType killType, CS_VehicleDeath update)
        {   //Update our kill streak
            UpdateDeath(victim, killer);

            //Spawn event even activated?
            if (!SpawnEvent)
                return;

            switch ((SpawnEventTypes)_spawnEventType)
            {
                case SpawnEventTypes.SOLOTHIRTYK:
                    break;

                case SpawnEventTypes.TEAMTHIRTYK:
                    if (victim != null && victim._bounty >= 30000)
                    {
                        //Lets reset the teams
                        if (BountyA != null)
                            BountyA = null;
                        else if (BountyB != null)
                            BountyB = null;
                    }
                    break;
            }
        }

        /// <summary>
        /// Triggered when a player has spawned
        /// </summary>
        public void playerSpawn(Player player, bool death, bool gameStarted)
        {
            //We only want to trigger end game when the last team member died out
            if (gameStarted && death)
            {
                switch (_gameType)
                {
                    case GameTypes.LEAGUEOVERTIME:
                        player.spec();
                        _arena.sendArenaMessage(String.Format("{0} has died out.", player._alias));

                        //Last to die?
                        if (_arena.ActiveTeams.Count() <= 1)
                            _arena.gameEnd();
                        break;
                }
            }
        }

        /// <summary>
        /// Triggered when a player dies to a bot
        /// </summary>
        public void botKill(Player victim, Bot killer)
        {
            if (_savedPlayerStats.ContainsKey(victim._alias))
                _savedPlayerStats[victim._alias].deaths++;
        }

        /// <summary>
        /// Triggered when a bot dies to a player
        /// </summary>
        public void botDeath(Bot victim, Player killer, int weaponID)
        {
            if (killer != null && _savedPlayerStats.ContainsKey(killer._alias)
                && killer._team != victim._team)
                _savedPlayerStats[killer._alias].kills++;
        }

        /// <summary>
        /// Called when the player successfully joins the game
        /// </summary>
        public void playerEnter(Player player)
        {
            //Add them to the list if not in it
            if (!_savedPlayerStats.ContainsKey(player._alias))
            {
                PlayerStat temp = new PlayerStat();
                temp.squad = player._squad;
                temp.assistPoints = 0;
                temp.bonusPoints = 0;
                temp.killPoints = 0;
                temp.points = 0;
                temp.playSeconds = 0;
                temp.alias = player._alias;
                temp.deaths = 0;
                temp.kills = 0;
                temp.player = player;
                _savedPlayerStats.Add(player._alias, temp);
            }
            _savedPlayerStats[player._alias].teamname = player._team._name;
            _savedPlayerStats[player._alias].hasPlayed = player.IsSpectator ? false : true;
            if (player._baseVehicle != null)
                _savedPlayerStats[player._alias].classType = player._baseVehicle._type.Name;

            if (_gameType == GameTypes.LEAGUEMATCH || _gameType == GameTypes.LEAGUEOVERTIME)
            {
                if (!player.IsSpectator)
                    //Lets make sure to turn banner spamming off
                    player._bAllowBanner = false;
                if (!player._bAllowSpectator)
                    //Make sure they are speccable
                    player._bAllowSpectator = true;
            }
        }

        /// <summary>
        /// Triggered when a player wants to unspec and join the game
        /// </summary>
        public bool playerJoinGame(Player player)
        {
            //Add them to the list if not in it
            if (!_savedPlayerStats.ContainsKey(player._alias))
            {
                PlayerStat temp = new PlayerStat();
                temp.alias = player._alias;
                temp.squad = player._squad;
                temp.assistPoints = 0;
                temp.bonusPoints = 0;
                temp.killPoints = 0;
                temp.points = 0;
                temp.playSeconds = 0;
                temp.deaths = 0;
                temp.kills = 0;
                temp.player = player;
                _savedPlayerStats.Add(player._alias, temp);
            }
            _savedPlayerStats[player._alias].hasPlayed = true;
            if (player._baseVehicle != null)
                _savedPlayerStats[player._alias].classType = player._baseVehicle._type.Name;

            if (_gameType == GameTypes.LEAGUEMATCH || _gameType == GameTypes.LEAGUEOVERTIME)
            {
                if (!player.IsSpectator)
                    //Lets make sure to turn banner spamming off
                    player._bAllowBanner = false;
                if (!player._bAllowSpectator)
                    //Make sure they are speccable
                    player._bAllowSpectator = true;
            }

            //Are we doing an event?
            if (Events)
            {   //Which event are we doing?
                switch ((EventTypes)_eventType)
                {
                    case EventTypes.GreenYellow:
                        //Lets get team stuff
                        Team green = _arena.getTeamByName("Green");
                        Team yellow = _arena.getTeamByName("Yellow");

                        //First do sanity checks
                        if (green == null || yellow == null)
                            break;

                        //Are they the first or the same player count?
                        if (green.ActivePlayerCount <= yellow.ActivePlayerCount)
                            player.unspec(green);
                        else
                            player.unspec(yellow);
                        player._lastMovement = Environment.TickCount;
                        player._maxTimeCalled = false;
                        //Returning false so the server doesnt repick us
                        return false;

                    case EventTypes.RedBlue:
                        //Lets get team stuff
                        Team red = _arena.getTeamByName("Red");
                        Team blue = _arena.getTeamByName("Blue");

                        //First do sanity checks
                        if (red == null || blue == null)
                            break;

                        //Are they the first or the same player count?
                        if (red.ActivePlayerCount <= blue.ActivePlayerCount)
                            player.unspec(red);
                        else
                            player.unspec(blue);
                        player._lastMovement = Environment.TickCount;
                        player._maxTimeCalled = false;
                        //We are returning false so server wont repick us
                        return false;

                    case EventTypes.WhiteBlack:
                        //Lets get team stuff
                        Team white = _arena.getTeamByName("White");
                        Team black = _arena.getTeamByName("Black");

                        //First do sanity checks
                        if (white == null || black == null)
                            break;

                        //Are they the first or the same player count?
                        if (white.ActivePlayerCount <= black.ActivePlayerCount)
                            player.unspec(white);
                        else
                            player.unspec(black);
                        player._lastMovement = Environment.TickCount;
                        player._maxTimeCalled = false;
                        //We are returning false so server wont repick us
                        return false;

                    case EventTypes.PinkPurple:
                        //Lets get team stuff
                        Team pink = _arena.getTeamByName("Pink");
                        Team purple = _arena.getTeamByName("Purple");

                        //First do sanity checks
                        if (pink == null || purple == null)
                            break;

                        //Are they the first or the same player count?
                        if (pink.ActivePlayerCount <= purple.ActivePlayerCount)
                            player.unspec(pink);
                        else
                            player.unspec(purple);
                        player._lastMovement = Environment.TickCount;
                        player._maxTimeCalled = false;
                        //We are returning false so server wont repick us
                        return false;

                    case EventTypes.GoldSilver:
                        //Lets get team stuff
                        Team gold = _arena.getTeamByName("Gold");
                        Team silver = _arena.getTeamByName("Silver");

                        //First do sanity checks
                        if (gold == null || silver == null)
                            break;

                        //Are they the first or the same player count?
                        if (gold.ActivePlayerCount <= silver.ActivePlayerCount)
                            player.unspec(gold);
                        else
                            player.unspec(silver);
                        player._lastMovement = Environment.TickCount;
                        player._maxTimeCalled = false;
                        //We are returning false so server wont repick us
                        return false;

                    case EventTypes.BronzeDiamond:
                        //Lets get team stuff
                        Team diamond = _arena.getTeamByName("Diamond");
                        Team bronze = _arena.getTeamByName("Bronze");

                        //First do sanity checks
                        if (diamond == null || bronze == null)
                            break;

                        //Are they the first or the same player count?
                        if (diamond.ActivePlayerCount <= bronze.ActivePlayerCount)
                            player.unspec(diamond);
                        else
                            player.unspec(bronze);
                        player._lastMovement = Environment.TickCount;
                        player._maxTimeCalled = false;
                        //We are returning false so server wont repick us
                        return false;

                    case EventTypes.OrangeGray:
                        //Lets get team stuff
                        Team orange = _arena.getTeamByName("Orange");
                        Team gray = _arena.getTeamByName("Gray");

                        //First do sanity checks
                        if (orange == null || gray == null)
                            break;

                        //Are they the first or the same player count?
                        if (orange.ActivePlayerCount <= gray.ActivePlayerCount)
                            player.unspec(orange);
                        else
                            player.unspec(gray);
                        player._lastMovement = Environment.TickCount;
                        player._maxTimeCalled = false;
                        //We are returning false so server wont repick us
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Called when a player enters the arena
        /// </summary>
        public void playerEnterArena(Player player)
        {
            //Lets reset our current game
            if (_savedPlayerStats.ContainsKey(player._alias))
            {
                PlayerStat stat = _savedPlayerStats[player._alias];
                player.StatsCurrentGame.assistPoints = stat.assistPoints;
                player.StatsCurrentGame.bonusPoints = stat.bonusPoints;
                player.StatsCurrentGame.killPoints = stat.killPoints;
                player.StatsCurrentGame.deaths = stat.deaths;
                player.StatsCurrentGame.kills = stat.kills;
                player.StatsCurrentGame.playSeconds = stat.playSeconds;
            }
            else
            {
                PlayerStat temp = new PlayerStat();
                temp.alias = player._alias;
                temp.assistPoints = 0;
                temp.killPoints = 0;
                temp.bonusPoints = 0;
                temp.points = 0;
                temp.playSeconds = 0;
                temp.squad = player._squad;
                temp.deaths = 0;
                temp.kills = 0;
                temp.player = player;
                temp.hasPlayed = false;
                _savedPlayerStats.Add(player._alias, temp);
            }
        }

        /// <summary>
        /// Called when the player successfully leaves the game
        /// </summary>
        public void playerLeave(Player player, bool gameStarted)
        {
            if (_gameType == GameTypes.LEAGUEMATCH)
            {
                if (_savedPlayerStats.ContainsKey(player._alias) && _savedPlayerStats[player._alias].hasPlayed)
                {
                    _savedPlayerStats[player._alias].playSeconds = gameStarted ? player.StatsCurrentGame.playSeconds : player.StatsLastGame != null ? player.StatsLastGame.playSeconds : 0;
                    _savedPlayerStats[player._alias].points = gameStarted ? player.StatsCurrentGame.Points : player.StatsLastGame != null ? player.StatsLastGame.Points : 0;
                    _savedPlayerStats[player._alias].assistPoints = gameStarted ? player.StatsCurrentGame.assistPoints : player.StatsLastGame != null ? player.StatsLastGame.assistPoints : 0;
                    _savedPlayerStats[player._alias].killPoints = gameStarted ? player.StatsCurrentGame.killPoints : player.StatsLastGame != null ? player.StatsLastGame.killPoints : 0;
                    _savedPlayerStats[player._alias].bonusPoints = gameStarted ? player.StatsCurrentGame.bonusPoints : player.StatsLastGame != null ? player.StatsLastGame.bonusPoints : 0;
                    if (player._baseVehicle != null)
                        _savedPlayerStats[player._alias].classType = player._baseVehicle._type.Name;
                }
            }

            if (player.Bounty >= 30000 && player._team == BountyA)
                BountyA = null;
            if (player.Bounty >= 30000 && player._team == BountyB)
                BountyB = null;
        }

        /// <summary>
        /// Called when a player leaves the arena
        /// </summary>
        public void playerLeaveArena(Player player, bool gameStarted)
        {
            if (_gameType == GameTypes.LEAGUEMATCH)
            {
                if (_savedPlayerStats.ContainsKey(player._alias) && _savedPlayerStats[player._alias].hasPlayed)
                {
                    _savedPlayerStats[player._alias].playSeconds = gameStarted ? player.StatsCurrentGame.playSeconds : player.StatsLastGame != null ? player.StatsLastGame.playSeconds : 0;
                    _savedPlayerStats[player._alias].points = gameStarted ? player.StatsCurrentGame.Points : player.StatsLastGame != null ? player.StatsLastGame.Points : 0;
                    _savedPlayerStats[player._alias].assistPoints = gameStarted ? player.StatsCurrentGame.assistPoints : player.StatsLastGame != null ? player.StatsLastGame.assistPoints : 0;
                    _savedPlayerStats[player._alias].killPoints = gameStarted ? player.StatsCurrentGame.killPoints : player.StatsLastGame != null ? player.StatsLastGame.killPoints : 0;
                    _savedPlayerStats[player._alias].bonusPoints = gameStarted ? player.StatsCurrentGame.bonusPoints : player.StatsLastGame != null ? player.StatsLastGame.bonusPoints : 0;
                    if (player._baseVehicle != null)
                        _savedPlayerStats[player._alias].classType = player._baseVehicle._type.Name;
                }
            }

            if (player.Bounty >= 30000 && player._team == BountyA)
                BountyA = null;
            if (player.Bounty >= 30000 && player._team == BountyB)
                BountyB = null;
        }

        /// <summary>
        /// Called when someone tries to pick up an item
        /// </summary>
        public bool playerItemPickup(Player player, Arena.ItemDrop drop, ushort quantity)
        {
            //Are we 30k eventing?
            if (SpawnEvent && _spawnEventType == SpawnEventTypes.TEAMTHIRTYK)
            {
                if (drop.item.name.Equals("Bty"))
                {
                    //Is this player on the same team as a current bountier?
                    if (BountyA == player._team || BountyB == player._team)
                        return false;

                    //Lets set the bounty team
                    if (BountyA == null)
                        BountyA = player._team;
                    else
                        BountyB = player._team;
                }
            }
            return true;
        }

        /// <summary>
        /// Called when a player successfully changes their class
        /// </summary>
        public void playerSkillPurchase(Player player, SkillInfo skill)
        {
            if (_gameType == GameTypes.LEAGUEMATCH)
                if (_savedPlayerStats.ContainsKey(player._alias) && player._baseVehicle != null)
                    _savedPlayerStats[player._alias].classType = player._baseVehicle._type.Name;
        }
        #endregion

        #region Updation Calls
        /// <summary>
        /// Updates our players kill streak timer
        /// </summary>
        public void UpdateKillStreaks()
        {
            foreach(PlayerStat p in _savedPlayerStats.Values)
            {
                if (p.lastUsedWepTick == -1)
                    continue;

                if (Environment.TickCount - p.lastUsedWepTick <= 0)
                    ResetWeaponTicker(p.player);
            }
        }

        /// <summary>
        /// Updates our tickers
        /// </summary>
        public void UpdateTickers()
        {
            //Team scores
            IEnumerable<Team> activeTeams = _arena.Teams.Where(entry => entry.ActivePlayerCount > 0);
            Team titan = activeTeams.ElementAt(0) != null ? activeTeams.ElementAt(0) : _arena.getTeamByName(_config.teams[0].name);
            Team collie = activeTeams.Count() > 1 ? activeTeams.ElementAt(1) : _arena.getTeamByName(_config.teams[1].name);
            string format = String.Format("{0}={1} - {2}={3}", titan._name, titan._currentGameKills, collie._name, collie._currentGameKills);
            //We playing more events at the same time?
            if (activeTeams.Count() > 3)
            {
                Team third = activeTeams.ElementAt(2);
                Team fourth = activeTeams.ElementAt(3);
                format = String.Format("{0}={1} - {2}={3} | {4}={5} - {6}={7}", titan._name, titan._currentGameKills, collie._name, collie._currentGameKills,
                    third._name, third._currentGameKills, fourth._name, fourth._currentGameKills);
            }
            _arena.setTicker(1, 2, 0, format);

            //Personal Scores
            _arena.setTicker(2, 1, 0, delegate(Player p)
            {
                //Update their ticker
                if (_savedPlayerStats.ContainsKey(p._alias))
                    return String.Format("HP={0}          Personal Score: Kills={1} - Deaths={2}",
                        p._state.health,
                        _savedPlayerStats[p._alias].kills,
                        _savedPlayerStats[p._alias].deaths);
                return "";
            });

            //1st and 2nd place
            List<Player> ranked = new List<Player>();
            foreach (Player p in _arena.Players)
            {
                if (p == null)
                    continue;
                if (_savedPlayerStats.ContainsKey(p._alias) && _savedPlayerStats[p._alias].hasPlayed)
                    ranked.Add(p);
            }

            IEnumerable<Player> ranking = ranked.OrderBy(player => _savedPlayerStats[player._alias].deaths).OrderByDescending(player => _savedPlayerStats[player._alias].kills);
            int idx = 3; format = "";
            foreach (Player rankers in ranking)
            {
                if (idx-- == 0)
                    break;

                switch (idx)
                {
                    case 2:
                        format = String.Format("1st: {0}(K={1} D={2})", rankers._alias,
                          _savedPlayerStats[rankers._alias].kills, _savedPlayerStats[rankers._alias].deaths);
                        break;
                    case 1:
                        format = (format + String.Format(" 2nd: {0}(K={1} D={2})", rankers._alias,
                          _savedPlayerStats[rankers._alias].kills, _savedPlayerStats[rankers._alias].deaths));
                        break;
                }
            }
            if (!_arena.recycling)
                _arena.setTicker(2, 0, 0, format);
        }
        #endregion

        #region Private Calls
        /// <summary>
        /// Gets the arena ready for our match
        /// </summary>
        private void MatchSetup()
        {
            //Reset variables
            Voting = false;
            Events = false;
            SpawnEvent = false;
            BountyA = null;
            BountyB = null;
            foreach (Player p in _arena.Players)
            {
                //Make sure no one has grant
                if (_arena.IsOwner(p))
                {
                    _arena._owner.Remove(p._alias);
                    //Reset any temp powers they might have
                    if (!p._developer && !p._admin && p._permissionTemp > Data.PlayerPermission.Normal)
                        p._permissionTemp = Data.PlayerPermission.Normal;
                }

                //Turn off banners for in game players (can cause the player lag by spamming them)
                if (!p.IsSpectator)
                    p._bAllowBanner = false;
            }

            //Record our start time if this is our initial start time
            if (_gameType != GameTypes.LEAGUEOVERTIME)
            {
                startTime = DateTime.Now.ToLocalTime();
                _gameType = GameTypes.LEAGUEMATCH;
            }
        }

        /// <summary>
        /// Ends our gameplay and shows the results
        /// </summary>
        private void MatchEnd()
        {
            List<Team> activeTeams = _arena.ActiveTeams.OrderByDescending(entry => entry._currentGameKills).ToList();
            Team team2 = activeTeams.Count > 1 ? activeTeams.ElementAt(1) : null;
            Team team1 = activeTeams.Count > 0 ? activeTeams.ElementAt(0) : null;

            //Spec all in game players
            foreach (Player p in _arena.PlayersIngame.ToList())
                p.spec();

            switch (activeTeams.Count)
            {
                case 0: //No teams, just return
                    return;
                case 1:
                    //Only one team? Just show whatever results we have
                    int idx = 2;
                    foreach (Team t in activeTeams)
                    {
                        if (idx-- == 0)
                            break;

                        string format = "!2nd (K={0} D={1}): {2}";
                        switch (idx)
                        {
                            case 1:
                                format = "!1st (K={0} D={1}): {2}";
                                break;
                        }
                        _arena.sendArenaMessage(String.Format(format, t._currentGameKills, t._currentGameDeaths, t._name));
                    }
                    break;
                case 2:
                    if (team1._currentGameKills > team2._currentGameKills)
                    {
                        victoryTeam = team1;
                        _arena.sendArenaMessage(String.Format("{0} has won with a {1}-{2} victory!", team1._name, team1._currentGameKills, team2._currentGameKills));
                        foreach (Player p in _arena.Players)
                        {
                            if (p._squad.Contains(team1._name))
                                p.ZoneStat1++;
                            if (p._squad.Contains(team2._name))
                                p.ZoneStat2++;
                        }
                    }
                    else if (team2._currentGameKills > team1._currentGameKills)
                    {
                        victoryTeam = team2;
                        _arena.sendArenaMessage(String.Format("{0} has won with a {1}-{2} victory!", team2._name, team2._currentGameKills, team1._currentGameKills));
                        foreach (Player p in _arena.Players)
                        {
                            if (p._squad.Contains(team2._name))
                                p.ZoneStat1++;
                            if (p._squad.Contains(team1._name))
                                p.ZoneStat2++;
                        }
                    }
                    else
                    {
                        _arena.sendArenaMessage(GetOT());

                        //We in overtime?
                        if (_gameType == GameTypes.LEAGUEMATCH)
                        {   //Nope! Lets migrate stats, we migrate only once because ot is not recorded
                            lastTeam1 = new Team(_arena, _arena._server);
                            lastTeam1._name = team1._name;
                            lastTeam1._currentGameKills = team1._currentGameKills;
                            lastTeam1._currentGameDeaths = team1._currentGameDeaths;

                            lastTeam2 = new Team(_arena, _arena._server);
                            lastTeam2._name = team2._name;
                            lastTeam2._currentGameKills = team2._currentGameKills;
                            lastTeam2._currentGameDeaths = team2._currentGameDeaths;
                            _lastSavedStats = new Dictionary<string, PlayerStat>();
                            foreach (KeyValuePair<string, PlayerStat> pair in _savedPlayerStats)
                            {
                                if (pair.Value.hasPlayed)
                                    _lastSavedStats.Add(pair.Key, pair.Value);
                            }
                        }
                        _gameType = GameTypes.LEAGUEOVERTIME;

                        _arena.gameReset();
                        return; //Since this is ot, we want to return.. not continue
                    }
                    //Save to a file
                    ExportStats(team1, team2);

                    _arena._isMatch = false;
                    _gameType = GameTypes.NULL;
                    OvertimeCount = 0;
                    AwardMVP = true;
                    _arena.gameReset();
                    break;
            }
        }

        /// <summary>
        /// Spawns anything related to our current event
        /// </summary>
        private void SpawnEventItem()
        {
            //Are we even running events?
            if (!SpawnEvent)
                return;

            //Lets find the item first
            Assets.ItemInfo item = _arena._server._assets.getItemByName("Bty");
            if (item == null)
                return;

            //Find the spawn spot
            string eventName;
            if (_gameType == GameTypes.TDM)
                eventName = String.Format("{0}{1}", Enum.GetName(typeof(GameTypes), _gameType), "30k");
            else if (Events)
                eventName = String.Format("{0}{1}", Enum.GetName(typeof(EventTypes), _eventType), "30k");
            else
                return;

            List<LioInfo.Hide> hides = _arena._server._assets.Lios.Hides.Where(s => s.GeneralData.Name.Contains(eventName)).ToList();
            if (hides == null)
                return;

            switch ((SpawnEventTypes)_spawnEventType)
            {
                case SpawnEventTypes.SOLOTHIRTYK:
                    {
                        //Find solo spawn points
                        eventName = eventName + "Solo";
                        LioInfo.Hide hide = hides.FirstOrDefault(h => h.GeneralData.Name.Contains(eventName));
                        if (hide == null)
                            return;
                        //Check to see if anyone still has a bty before spawning
                        foreach (Player p in _arena.PlayersIngame)
                        {
                            if (p == null)
                                continue;
                            //Found someone, dont need to spawn it
                            if (p._bounty >= 30000)
                                return;
                        }

                        //Lets see if its already spawned
                        Arena.ItemDrop drop;
                        if (_arena._items.TryGetValue((ushort)item.id, out drop))
                        {
                            if (drop.quantity >= 1)
                                return;
                        }

                        //Spawn it
                        _arena.itemSpawn(item, (ushort)1, hide.GeneralData.OffsetX, hide.GeneralData.OffsetY, null);
                    }
                    break;

                case SpawnEventTypes.TEAMTHIRTYK:
                    {
                        //Find team spawn points
                        eventName = eventName + "Team";
                        LioInfo.Hide hideA = hides.FirstOrDefault(h => h.GeneralData.Name.Contains(eventName + "A"));
                        LioInfo.Hide hideB = hides.FirstOrDefault(h => h.GeneralData.Name.Contains(eventName + "B"));
                        if (hideA == null || hideB == null)
                            return;
                        //Check to see if both teams still have a bty before spawning
                        int count = 0;
                        foreach (Player p in _arena.PlayersIngame)
                        {
                            if (p == null)
                                continue;
                            //Found someone
                            if ((p._bounty >= 30000) && (p._team == BountyA || p._team == BountyB))
                            {
                                //Both teams have a btyer, dont spawn
                                if (count == 2)
                                    return;

                                ++count;
                            }
                        }

                        //Lets see if its already spawned
                        Arena.ItemDrop drop;
                        if (_arena._items.TryGetValue((ushort)item.id, out drop))
                        {
                            if ((count == 1 && drop.quantity >= 1)
                                || (count == 0 && drop.quantity >= 2))
                                return;
                        }

                        //Spawn it
                        if (BountyA == null)
                            _arena.itemSpawn(item, (ushort)1, hideA.GeneralData.OffsetX, hideA.GeneralData.OffsetY, null);
                        if (BountyB == null)
                            _arena.itemSpawn(item, (ushort)1, hideB.GeneralData.OffsetX, hideB.GeneralData.OffsetY, null);
                    }
                    break;
            }
        }

        /// <summary>
        /// Saves all league stats to a file and website
        /// </summary>
        private void ExportStats(Team team1, Team team2)
        {   //Sanity checks
            if (_gameType == GameTypes.LEAGUEOVERTIME)
            {
                if (_lastSavedStats.Count < 1)
                    return;

                string OT = "OT";
                if (OvertimeCount > 0)
                {
                    switch (OvertimeCount)
                    {
                        case 1:
                            OT = "D-OT";
                            break;
                        case 2:
                            OT = "T-OT";
                            break;
                        case 3:
                            OT = "Q-OT";
                            break;
                        default:
                            OT = "OT++";
                            break;
                    }
                }
                //Make the file with current date and filename
                string name1 = lastTeam1._name.Trim(' ');
                string name2 = lastTeam2._name.Trim(' ');
                string filename = String.Format("{0}vs{1} {2}", name1, name2, startTime.ToLocalTime().ToString());
                FileName = filename;
                StreamWriter fs = Logic_File.CreateStatFile(filename, String.Format("Season {0}", LeagueSeason.ToString()));

                fs.WriteLine();
                fs.WriteLine(String.Format("Team Name = {0}, Kills = {1}, Deaths = {2}, Win = {3}, In OT? = {4}",
                    lastTeam1._name, lastTeam1._currentGameKills, lastTeam1._currentGameDeaths, lastTeam1._name.Equals(victoryTeam._name) ? "Yes" : "No", OT));
                fs.WriteLine("--------------------------------------------------------------------");
                foreach (KeyValuePair<string, PlayerStat> p in _lastSavedStats)
                {
                    if (String.IsNullOrWhiteSpace(p.Key))
                        continue;

                    if (!p.Value.hasPlayed)
                        continue;

                    if (p.Value.teamname.Equals(team2._name))
                        continue;

                    bool yes = true; //We were an nt
                    if (team1._name.Contains(p.Value.squad))
                        yes = false; //We arent an nt
                    fs.WriteLine(String.Format("Name = {0}, NT? = {1}, Kills = {2}, Deaths = {3} PlaySeconds = {4}, Class = {5}",
                        p.Value.alias,
                        yes == false ? "No" : "Yes",
                        p.Value.kills,
                        p.Value.deaths,
                        p.Value.playSeconds,
                        p.Value.classType));
                }

                fs.WriteLine("---------------------------Medics-----------------------------------");
                foreach(KeyValuePair<string, PlayerStat> p in _lastSavedStats)
                {
                    if (String.IsNullOrWhiteSpace(p.Key))
                        continue;
                    if (!p.Value.hasPlayed)
                        continue;
                    if (p.Value.teamname.Equals(team2._name))
                        continue;
                    if (p.Value.potentialHealthHealed <= 0)
                        continue;
                    fs.WriteLine(String.Format("Name = {0}, Health Healed = {1}", p.Value.alias, p.Value.potentialHealthHealed));
                }
                fs.WriteLine("--------------------------------------------------------------------");
                fs.WriteLine();

                fs.WriteLine(String.Format("Team Name = {0}, Kills = {1}, Deaths = {2}, Win = {3}, In OT? = {4}",
                    lastTeam2._name, lastTeam2._currentGameKills, lastTeam2._currentGameDeaths, lastTeam2._name.Equals(victoryTeam._name) ? "Yes" : "No", OT));
                fs.WriteLine("--------------------------------------------------------------------");
                foreach (KeyValuePair<string, PlayerStat> p in _lastSavedStats)
                {
                    if (String.IsNullOrWhiteSpace(p.Key))
                        continue;

                    if (!p.Value.hasPlayed)
                        continue;

                    if (p.Value.teamname.Equals(team1._name))
                        continue;

                    bool yes = true; //We were an nt
                    if (team2._name.Contains(p.Value.squad))
                        yes = false; //We arent an nt
                    fs.WriteLine(String.Format("Name = {0}, NT? = {1}, Kills = {2}, Deaths = {3}, PlaySeconds = {4}, Class = {5}",
                        p.Value.alias,
                        yes == false ? "No" : "Yes",
                        p.Value.kills,
                        p.Value.deaths,
                        p.Value.playSeconds,
                        p.Value.classType));
                }

                fs.WriteLine("---------------------------Medics-----------------------------------");
                foreach (KeyValuePair<string, PlayerStat> p in _lastSavedStats)
                {
                    if (String.IsNullOrWhiteSpace(p.Key))
                        continue;
                    if (!p.Value.hasPlayed)
                        continue;
                    if (p.Value.teamname.Equals(team1._name))
                        continue;
                    if (p.Value.potentialHealthHealed <= 0)
                        continue;
                    fs.WriteLine(String.Format("Name = {0}, Health Healed = {1}", p.Value.alias, p.Value.potentialHealthHealed));
                }
                fs.WriteLine("--------------------------------------------------------------------");

                //Now set the format as per the export file function in the client
                foreach (KeyValuePair<string, PlayerStat> p in _lastSavedStats)
                {
                    if (String.IsNullOrWhiteSpace(p.Key))
                        continue;

                    if (!p.Value.hasPlayed)
                        continue;

                    fs.WriteLine(String.Format("{0},{1},{2},{3},{4},0,{5},0,{6},0,0,0,0,{7},{8}",
                        p.Value.alias,
                        p.Value.squad,
                        p.Value.points,
                        p.Value.kills,
                        p.Value.deaths,
                        p.Value.assistPoints,
                        p.Value.playSeconds,
                        p.Value.classType,
                        (p.Value.potentialHealthHealed <= 0 ? 0 : p.Value.potentialHealthHealed)));
                }
                fs.WriteLine("--------------------------------------------------------------------");

                //Close it
                fs.Close();

                //Report it
                _arena.sendArenaMessage("Stats have been backed up to a file. Please stay till refs are done recording.", 0);
            }
            else
            {
                //Make the file with current date and filename
                string name1 = team1._name.Trim(' ');
                string name2 = team2._name.Trim(' ');
                string filename = String.Format("{0}vs{1} {2}", name1, name2, startTime.ToLocalTime().ToString());
                FileName = filename;
                StreamWriter fs = Logic_File.CreateStatFile(filename, String.Format("Season {0}", LeagueSeason.ToString()));

                fs.WriteLine();
                fs.WriteLine(String.Format("Team Name = {0}, Kills = {1}, Deaths = {2}, Win = {3}, In OT? = No",
                    team1._name, team1._currentGameKills, team1._currentGameDeaths, team1 == victoryTeam ? "Yes" : "No"));
                fs.WriteLine("--------------------------------------------------------------------");
                foreach (KeyValuePair<string, PlayerStat> p in _savedPlayerStats)
                {
                    if (String.IsNullOrWhiteSpace(p.Key))
                        continue;

                    if (!p.Value.hasPlayed)
                        continue;

                    if (p.Value.teamname.Equals(team2._name))
                        continue;

                    bool yes = true; //We were an nt
                    if (team1._name.Contains(p.Value.squad))
                        yes = false; //We arent an nt
                    fs.WriteLine(String.Format("Name = {0}, NT? = {1}, Kills = {2}, Deaths = {3}, PlaySeconds = {4}, Class = {5}",
                        p.Value.alias,
                        yes == false ? "No" : "Yes",
                        p.Value.kills,
                        p.Value.deaths,
                        p.Value.playSeconds,
                        p.Value.classType));
                }

                fs.WriteLine("---------------------------Medics-----------------------------------");
                foreach (KeyValuePair<string, PlayerStat> p in _savedPlayerStats)
                {
                    if (String.IsNullOrWhiteSpace(p.Key))
                        continue;
                    if (!p.Value.hasPlayed)
                        continue;
                    if (p.Value.teamname.Equals(team2._name))
                        continue;
                    if (p.Value.potentialHealthHealed <= 0)
                        continue;
                    fs.WriteLine(String.Format("Name = {0}, Health Healed = {1}", p.Value.alias, p.Value.potentialHealthHealed));
                }
                fs.WriteLine("--------------------------------------------------------------------");
                fs.WriteLine();

                fs.WriteLine(String.Format("Team Name = {0}, Kills = {1}, Deaths = {2}, Win = {3}, In OT? = No",
                    team2._name, team2._currentGameKills, team2._currentGameDeaths, team2 == victoryTeam ? "Yes" : "No"));
                fs.WriteLine("--------------------------------------------------------------------");
                foreach (KeyValuePair<string, PlayerStat> p in _savedPlayerStats)
                {
                    if (String.IsNullOrWhiteSpace(p.Key))
                        continue;

                    if (!p.Value.hasPlayed)
                        continue;

                    if (p.Value.teamname.Equals(team1._name))
                        continue;

                    bool yes = true; //We were an nt
                    if (team2._name.Contains(p.Value.squad))
                        yes = false; //We arent an nt
                    fs.WriteLine(String.Format("Name = {0}, NT? = {1}, Kills = {2}, Deaths = {3}, PlaySeconds = {4}, Class = {5}",
                        p.Value.alias,
                        yes == false ? "No" : "Yes",
                        p.Value.kills,
                        p.Value.deaths,
                        p.Value.playSeconds,
                        p.Value.classType));
                }

                fs.WriteLine("---------------------------Medics-----------------------------------");
                foreach (KeyValuePair<string, PlayerStat> p in _savedPlayerStats)
                {
                    if (String.IsNullOrWhiteSpace(p.Key))
                        continue;
                    if (!p.Value.hasPlayed)
                        continue;
                    if (p.Value.teamname.Equals(team1._name))
                        continue;
                    if (p.Value.potentialHealthHealed <= 0)
                        continue;
                    fs.WriteLine(String.Format("Name = {0}, Health Healed = {1}", p.Value.alias, p.Value.potentialHealthHealed));
                }
                fs.WriteLine("--------------------------------------------------------------------");

                //Now set the format as per the export file function in the client
                foreach (KeyValuePair<string, PlayerStat> p in _savedPlayerStats)
                {
                    if (String.IsNullOrWhiteSpace(p.Key))
                        continue;

                    if (!p.Value.hasPlayed)
                        continue;

                    fs.WriteLine(String.Format("{0},{1},{2},{3},{4},0,{5},0,{6},0,0,0,0,{7},{8}",
                        p.Value.alias,
                        p.Value.squad,
                        p.Value.points,
                        p.Value.kills,
                        p.Value.deaths,
                        p.Value.assistPoints,
                        p.Value.playSeconds,
                        p.Value.classType,
                        (p.Value.potentialHealthHealed <= 0 ? 0 : p.Value.potentialHealthHealed)));
                }
                fs.WriteLine("--------------------------------------------------------------------");

                //Close it
                fs.Close();

                //Report it
                _arena.sendArenaMessage("Stats have been backed up to a file. Please stay till refs are done recording.", 0);
            }
        }
        #endregion
    }
}
