using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

using InfServer.Logic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;

using Assets;

namespace InfServer.Script.GameType_Multi
{
    public partial class Conquest
    {
        private Arena _arena;
        public Team _team1;
        public Team _team2;

        private CfgInfo _config;                //The zone config
        private int _lastTickerUpdate;
        private int _lastKillStreakUpdate;
        private bool _isScrim = false;
        public List<Arena.FlagState> _flags;
        private List<CapturePoint> _activePoints;
        private List<CapturePoint> _allPoints;
        private int _lastFlagCheck;
        private int _minPlayers = 1;
        public Script_Multi _baseScript;

        private int _flagCaptureRadius = 250;
        private int _flagCaptureTime = 5;


        public Team cqTeam1;
        public Team cqTeam2;
        private Team _winner;
        private int _totalFlags;
        private int _winnerFlags;


        #region Stat Recording
        private List<Team> activeTeams = null;
        #endregion

        #region Misc Gameplay Pointers
        private Player lastKiller;

        public Conquest(Arena arena, Script_Multi baseScript)
        {
            _baseScript = baseScript;
            _arena = arena;
            _config = arena._server._zoneConfig;

            _activePoints = new List<CapturePoint>();
            _allPoints = new List<CapturePoint>();
            _bots = new List<Bot>();

            cqTeam1 = _arena.getTeamByName("Titan Militia");
            cqTeam2 = _arena.getTeamByName("Collective Military");

        }

        public void setTeams(Team team1, Team team2, bool isScrim)
        {
            if (_team1 != team1)
            {
                _team1 = team1;
                _team2 = team2;
                SwitchTeams();
            }
        }


        public void Poll(int now)
        {

            if (now - _lastTickerUpdate >= 1000)
            {
                UpdateTickers();
                _lastTickerUpdate = now;
            }

            int playing = _arena.PlayerCount;
            if (_arena._bGameRunning && playing < _minPlayers && _arena._bIsPublic)
            {
                _baseScript.bJackpot = false;
                //Stop the game and reset voting
                _arena.gameEnd();

            }
            if (playing < _minPlayers && _arena._bIsPublic)
            {
                _baseScript._tickGameStarting = 0;
                _arena.setTicker(1, 3, 0, "Not Enough Players");
            }

            if (playing < _minPlayers && !_arena._bIsPublic && !_arena._bGameRunning)
            {
                _baseScript._tickGameStarting = 0;
                _arena.setTicker(1, 3, 0, "Private arena, Waiting for arena owner to start the game!");
            }

            //Do we have enough to start a game?
            if (!_arena._bGameRunning && _baseScript._tickGameStarting == 0 && playing >= _minPlayers && _arena._bIsPublic)
            {
                _baseScript._tickGameStarting = now;
                _arena.setTicker(1, 3, _config.deathMatch.startDelay * 100, "Next game: ",
                    delegate ()
                    {   //Trigger the game start
                        _arena.gameStart();
                    });
            }



            if (now - _lastFlagCheck >= 500 && _arena._bGameRunning)
            {
                _lastFlagCheck = now;

                if (checkForWinner(false))
                    _arena.gameEnd();



                Arena.FlagState team1Flag = _arena._flags.Values.OrderByDescending(f => f.posX).Where(f => f.team == cqTeam1).First();
                Arena.FlagState team2Flag = _arena._flags.Values.OrderBy(f => f.posX).Where(f => f.team == cqTeam2).First();
                int unowned = _arena._flags.Values.Where(f => f.team != cqTeam1 && f.team != cqTeam2).Count();

                _activePoints.Clear();

                if (unowned > 0)
                {
                    _activePoints.Add(_allPoints.FirstOrDefault(p => p._flag.team != cqTeam1 && p._flag.team != cqTeam2));
                }
                else
                {
                    _activePoints.Add(_allPoints.FirstOrDefault(p => p._flag == team1Flag));
                    _activePoints.Add(_allPoints.FirstOrDefault(p => p._flag == team2Flag));
                }

                foreach (Player p in _arena.Players)
                    Helpers.Object_Flags(p, _flags);
            }

            foreach (CapturePoint point in _activePoints)
                point.poll(now);

            if (cqTeam1.ActivePlayerCount > 0 && _arena._bGameRunning)
                pollBots(now);
        }

        public bool checkForWinner(bool bGameOver)
        {
            int team1count = _arena._flags.Values.Where(f => f.team == cqTeam1).Count();
            int team2count = _arena._flags.Values.Where(f => f.team == cqTeam2).Count();


            if (!bGameOver)
            {
                //colly?
                if (team1count == 0)
                {
                    _baseScript._winner = cqTeam2;
                    _winnerFlags = team2count;
                    return true;
                }
                //Titan?
                if (team2count == 0)
                {
                    _baseScript._winner = cqTeam1;
                    _winnerFlags = team1count;
                    return true;
                }
            }
            else
            {
                if (team1count > team2count)
                {
                    _winnerFlags = team1count;
                    _baseScript._winner = cqTeam1;
                }

                if (team2count > team1count)
                {
                    _winnerFlags = team2count;
                    _baseScript._winner = cqTeam2;
                }
                //Draw
                else
                    _winner = null;
            }

            return false;
        }
        #endregion

        #region Gametype Events
        public void gameStart()
        {
            if (_arena.ActiveTeams.Count() == 0)
                return;

            _arena.flagReset();
            _arena.flagSpawn();

            _flags = _arena._flags.Values.OrderBy(f => f.posX).ToList();
            _allPoints = new List<CapturePoint>();

            _totalFlags = _flags.Count;


            _baseScript._lastSpawn = new Dictionary<string, Helpers.ObjectState>();

            foreach (Player player in _team2.ActivePlayers)
            {
                Helpers.ObjectState lastKnown = new Helpers.ObjectState();
                lastKnown.positionX = 20224;
                lastKnown.positionY = 992;
                _baseScript._lastSpawn.Add(player._alias, lastKnown);
            }

            foreach (Player player in _team1.ActivePlayers)
            {
                Helpers.ObjectState lastKnown = new Helpers.ObjectState();
                lastKnown.positionX = 14512;
                lastKnown.positionY = 1824;
                _baseScript._lastSpawn.Add(player._alias, lastKnown);
            }


            int flagcount = 1;
            foreach (Arena.FlagState flag in _flags)
            {
                if (flagcount <= 19)
                    flag.team = cqTeam1;
                if (flagcount >= 21)
                    flag.team = cqTeam2;
                flagcount++;

                _allPoints.Add(new CapturePoint(_arena, flag, _baseScript));
            }

            foreach (Player p in _arena.Players)
                Helpers.Object_Flags(p, _flags);


            Arena.FlagState team1Flag = _flags.OrderByDescending(f => f.posX).Where(f => f.team == cqTeam1).First();
            Arena.FlagState team2Flag = _flags.OrderBy(f => f.posX).Where(f => f.team == cqTeam2).First();

            _activePoints = new List<CapturePoint>();
            _activePoints.Add(_allPoints.FirstOrDefault(p => p._flag == team1Flag));
            _activePoints.Add(_allPoints.FirstOrDefault(p => p._flag == team2Flag));
            _activePoints.Add(_allPoints.FirstOrDefault(p => p._flag.team == null));


            UpdateTickers();

            int timer = 1800 * 100;


            //Let everyone know
            _arena.sendArenaMessage("Game has started! The team with control of the most flags at the end of the game wins.");
            _arena.setTicker(1, 3, timer, "Time Left: ",
                delegate ()
                {
                    //Check for a winner before we call gameEnd
                    checkForWinner(true);
                    //Trigger game end
                    _arena.gameEnd();
                }
            );
        }

        public bool playerFlagAction(Player player, bool bPickup, bool bInPlace, LioInfo.Flag flag)
        {

            return true;
        }

        /// <summary>
        /// Called when the statistical breakdown is displayed
        /// </summary>
        public void individualBreakdown(Player from, bool bCurrent)
        {
        }

        public void gameEnd()
        {
            _arena.flagReset();
            int conquered = (_winnerFlags / _totalFlags) * 100;

            _arena.sendArenaMessage(String.Format("{0} is Victorious with {1}% Conquered", _baseScript._winner._name, conquered));
        }

        public void gameReset()
        {
            _arena.flagReset();
        }
        #endregion

        #region Player Events

        public bool playerPortal(Player player, LioInfo.Portal portal)
        {
            if (portal.GeneralData.Name.Contains("DS Portal"))
            {
                Helpers.ObjectState flagPoint;
                Helpers.ObjectState warpPoint;

                flagPoint = _baseScript.findFlagWarp(player, false);

                if (flagPoint == null)
                {
                    Log.write(TLog.Normal, String.Format("Could not find suitable flag warp for {0}", player._alias));

                    if (!_baseScript._lastSpawn.ContainsKey(player._alias))
                    {
                        player.sendMessage(-1, "Could not find suitable warp, warped to landing ship!");
                        return true;
                    }
                    else
                        warpPoint = _baseScript._lastSpawn[player._alias];
                }
                else
                {
                    warpPoint = _baseScript.findOpenWarp(player, _arena, flagPoint.positionX, 1744, _baseScript._playerWarpRadius);
                }

                if (warpPoint == null)
                {
                    Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                    player.sendMessage(-1, "Warp was blocked, please try again");
                    return false;
                }

                _baseScript.warp(player, warpPoint);

                if (_baseScript._lastSpawn.ContainsKey(player._alias))
                    _baseScript._lastSpawn[player._alias] = warpPoint;
                else
                    _baseScript._lastSpawn.Add(player._alias, warpPoint);
                return false;
            }
            return false;
        }

        /// <summary>
        /// Called when a players killstreak is updated
        /// </summary>
        /// <param name="count"></param>
        public void playerKillStreak(Player killer, int count)
        {
            switch (count)
            {
                case 6:
                    _arena.sendArenaMessage(string.Format("{0} is on fire!", killer._alias), 17);
                    break;
                case 8:
                    _arena.sendArenaMessage(string.Format("Someone kill {0}!", killer._alias), 18);
                    break;
                case 10:
                    _arena.sendArenaMessage(string.Format("{0} is dominating!", killer._alias), 19);
                    break;
                case 12:
                    _arena.sendArenaMessage(string.Format("DEATH TO {0}!", killer._alias), 30);
                    break;
            }
        }

        /// <summary>
        /// Triggered when a bot has died
        /// </summary>
        /// <param name="dead"></param>
        /// <param name="killer"></param>
        /// <returns></returns>
        public bool botDeath(Bot dead, Player killer)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a player has died to a bot
        /// </summary>
        /// <param name="victim"></param>
        /// <param name="bot"></param>
        /// <returns></returns>
        public bool playerDeathBot(Player victim, Bot bot)
        {
            return true;
        }

        public bool playerUnspec(Player player)
        {
            //Place him on the appropriate team
            pickTeam(player);
            return true;
        }


        private void pickTeam(Player player)
        {

            if (_team1.ActivePlayerCount <= _team2.ActivePlayerCount)
            {
                if (player._team != _team1)
                {
                    player.joinTeam(_team1);
                }
            }
            else
            {
                if (player._team != _team2)
                {
                    player.joinTeam(_team2);
                }
            }
        }

        public void playerSpec(Player player)
        {
        }

        public bool playerSpawn(Player player, bool death)
        {
            return true;
        }

        public void playerEnterArena(Player player)
        {

            if (Script_Multi._bPvpHappyHour)
                player.sendMessage(0, "&PvP Happy hour is currently active, Enjoy!");
            else
            {
                TimeSpan remaining = _baseScript.timeTo(Settings._pvpHappyHourStart);
                player.sendMessage(0, String.Format("&PvP Happy hour starts in {0} hours & {1} minutes", remaining.Hours, remaining.Minutes));
            }

            //Obtain the Co-Op skill..
            SkillInfo coopskillInfo = _arena._server._assets.getSkillByID(200);

            //Add the skill!
            if (player.findSkill(200) != null)
                player._skills.Remove(200);

            //Obtain the Powerup skill..
            SkillInfo powerupskillInfo = _arena._server._assets.getSkillByID(201);

            //Add the skill!
            if (player.findSkill(201) != null)
                player._skills.Remove(201);

            //Add the skill!
            if (player.findSkill(203) != null)
                player._skills.Remove(203);
            //Add the skill!
            if (player.findSkill(202) != null)
                player._skills.Remove(202);

        }

        public void playerLeaveArena(Player player)
        {

        }

        /// <summary>
        /// Triggered when a player tries to heal
        /// </summary>
        public void PlayerRepair(Player from, ItemInfo.RepairItem item)
        {
        }


        public void playerExplosion(Player player, ItemInfo.Projectile weapon, short posX, short posY, short posZ)
        {
        }

        public void playerPlayerKill(Player victim, Player killer)
        { 
        }

        public void playerDeath(Player victim, Player killer, Helpers.KillType killType, CS_VehicleDeath update)
        {
        }
        #endregion

        #region Command Handlers
        public bool playerModcommand(Player player, Player recipient, string command, string payload)
        {
            return true;
        }

        public bool playerChatCommand(Player player, Player recipient, string command, string payload)
        {
            return true;
        }
        #endregion

        #region Updation Calls

        /// <summary>
        /// Switches the teams before a game start.
        /// </summary>
        private void SwitchTeams()
        {
            foreach (var player in _arena.PlayersIngame)
            {
                //Sanity checks
                if (_team1 == null || _team2 == null)
                    break;

                if (_team1.ActivePlayerCount <= _team2.ActivePlayerCount)
                {
                    if (player._team != _team1)
                        _team1.addPlayer(player);
                }
                else
                {
                    if (player._team != _team2)
                        _team2.addPlayer(player);
                }

            }
        }



        /// <summary>
        /// Updates our tickers
        /// </summary>
        public void UpdateTickers()
        {
            if (!_arena._bGameRunning)
            { return; }

            IEnumerable<Team> active = _arena.ActiveTeams;
            if (activeTeams != null && activeTeams.Count() > 0)
            {
                active = activeTeams;
            }

            Team collie = active.Count() > 1 ? active.ElementAt(1) : _arena.getTeamByName(_config.teams[0].name);
            Team titan = active.Count() > 0 ? active.ElementAt(0) : _arena.getTeamByName(_config.teams[1].name);

            string format = string.Format("{0}={1} - {2}={3}", titan._name, titan._currentGameKills, collie._name, collie._currentGameKills);
            //We playing more events at the same time?
            if (active.Count() > 3)
            {
                Team third = active.ElementAt(2);
                Team fourth = active.ElementAt(3);
                format = string.Format("{0}={1} - {2}={3} | {4}={5} - {6}={7}", titan._name, titan._currentGameKills, collie._name, collie._currentGameKills,
                    third._name, third._currentGameKills, fourth._name, fourth._currentGameKills);
            }
            _arena.setTicker(1, 2, 0, format);

            //Personal Scores
            _arena.setTicker(2, 1, 0, delegate (Player p)
            {
                if (_baseScript.StatsCurrent(p) == null)
                    return "";
                //Update their ticker
                return string.Format("HP={0}          Personal Score: Kills={1} - Deaths={2}",
                    p._state.health,
                    _baseScript.StatsCurrent(p).kills,
                    _baseScript.StatsCurrent(p).deaths);
            });

            //1st and 2nd place
            List<Player> ranked = new List<Player>();
            foreach (Player p in _arena.Players)
            {
                if (p == null)
                    continue;

                if (_baseScript.StatsCurrent(p) == null)
                    continue;

                if (_baseScript.StatsCurrent(p).hasPlayed)
                    ranked.Add(p);
            }

            IEnumerable<Player> ranking = ranked.OrderBy(player => _baseScript.StatsCurrent(player).deaths).OrderByDescending(player => _baseScript.StatsCurrent(player).kills);
            int idx = 3; format = "";
            foreach (Player rankers in ranking)
            {
                if (idx-- == 0)
                    break;

                switch (idx)
                {
                    case 2:
                        format = string.Format("1st: {0}(K={1} D={2})", rankers._alias,
                          _baseScript.StatsCurrent(rankers).kills, _baseScript.StatsCurrent(rankers).deaths);
                        break;
                    case 1:
                        format = (format + string.Format(" 2nd: {0}(K={1} D={2})", rankers._alias,
                          _baseScript.StatsCurrent(rankers).kills, _baseScript.StatsCurrent(rankers).deaths));
                        break;
                }
            }
            if (!_arena.recycling)
                _arena.setTicker(2, 0, 0, format);
        }
    }
}
#endregion

