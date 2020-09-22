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
    public partial class Coop
    {
        private Arena _arena;
        public Team _team;
        public Team _botTeam;
        private CfgInfo _config;                //The zone config
        private int _lastTickerUpdate;

        public List<Arena.FlagState> _flags;
        private List<CapturePoint> _activePoints;
        private List<CapturePoint> _allPoints;
        private int _lastFlagCheck;
        public Script_Multi _baseScript;

        private int _flagCaptureRadius = 250;
        private int _flagCaptureTime = 5;
        private int _flagsCaptured;
        private int _totalFlags;
        private int _minPlayers = 1;

        public int _botDifficulty;   // 1-10 are valid entries, controls percentage of veteran spawns.
        public int _botDifficultyPlayerModifier;   // Used to increase difficulty of arena when over 6 players.

        #region Stat Recording
        private List<Team> activeTeams = null;
        #endregion

        #region Misc Gameplay Pointers

        public Coop(Arena arena, Script_Multi baseScript)
        {
            _baseScript = baseScript;
            _arena = arena;
            _config = arena._server._zoneConfig;

            _activePoints = new List<CapturePoint>();
            _allPoints = new List<CapturePoint>();

            _bots = new List<Bot>();
            _condemnedBots = new List<Bot>();
            spawnBots = true;
            _medBotFollowTargets = new Dictionary<ushort, Player>();
            _medBotHealTargets = new Dictionary<ushort, Player>();

            _team = _arena.getTeamByName("Titan Militia");
            _botTeam = _arena.getTeamByName("Collective Military");

            if (_arena._name.StartsWith("[Co-Op]"))
            {   
                _botDifficulty = 1;
                if (_arena._name.EndsWith("Easy"))
                {
                    _botDifficulty = 0;
                }

                if (_arena._name.EndsWith("Normal"))
                {
                    _botDifficulty = 1;
                }

                if (_arena._name.EndsWith("Hard"))
                {
                    _botDifficulty = 3;
                }

                if (_arena._name.EndsWith("Expert"))
                {
                    _botDifficulty = 6;
                }
                if (_arena._name.EndsWith("Master"))
                {
                    _botDifficulty = 9;
                }
                if (_arena._name.EndsWith("Elite"))
                {
                    _botDifficulty = 12;
                }
                if (_arena._name.EndsWith("Insane"))
                {
                    _botDifficulty = 15;
                }
                if (_arena._name.EndsWith("Hell"))
                {
                    _botDifficulty = 35;
                }
            }
        }

        public void Poll(int now)
        {

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

            if (now - _lastTickerUpdate >= 1000)
            {
                UpdateTickers();
                _lastTickerUpdate = now;
            }

            if (now - _lastFlagCheck >= 500 && _arena._bGameRunning)
            {
                _lastFlagCheck = now;

                int team1count = _arena._flags.Values.Where(f => f.team == _team).Count();
                int team2count = _arena._flags.Values.Where(f => f.team == _botTeam).Count();

                _flagsCaptured = team1count;

                //Has anyone won?
                if (team1count == 0 || team2count == 0)
                {
                    _baseScript._winner = _team;
                    _arena.gameEnd();
                    return;
                }

                Arena.FlagState team1Flag = _arena._flags.Values.OrderByDescending(f => f.posX).Where(f => f.team == _team).First();
                Arena.FlagState team2Flag = _arena._flags.Values.OrderBy(f => f.posX).Where(f => f.team == _botTeam).First();

                int unowned = _arena._flags.Values.Where(f => f.team != _team && f.team != _botTeam).Count();

                _activePoints.Clear();

                if (unowned > 0)
                {
                    _activePoints.Add(_allPoints.FirstOrDefault(p => p._flag.team != _team && p._flag.team != _botTeam));
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

            pollBots(now);
        }
        #endregion

        #region Gametype Events
        public void gameStart()
        {
            if (_arena.ActiveTeams.Count() == 0)
                return;

            _arena.flagReset();
            _arena.flagSpawn();

            _firstRushWave = false;
            _secondRushWave = false;
            _thirdRushWave = false;
            _firstBoss = false;
            _secondBoss = false;

            _firstLightExoWave = false;
            _secondLightExoWave = false;
            _thirdLightExoWave = false;

            _firstHeavyExoWave = false;
            _secondHeavyExoWave = false;
            _thirdHeavyExoWave = false;

            _firstDifficultyWave = false;
            _secondDifficultyWave = false;
            _thirdDifficultyWave = false;
            _fourthDifficultyWave = false;
            _fifthDifficultyWave = false;

            _sixthDifficultyWave = false;
            _seventhDifficultyWave = false;
            _eighthDifficultyWave = false;
            _ninthDifficultyWave = false;
            _tenthDifficultyWave = false;
            _eleventhDifficultyWave = false;
            _twelvthDifficultyWave = false;
            _thirteenthDifficultyWave = false;
            _fourteenthDifficultyWave = false;
            _fifthteenthDifficultyWave = false;

            _lastSupplyDrop = Environment.TickCount;
            _lastHPChange = Environment.TickCount;
            hpMultiplier = 0.25;

            _flags = _arena._flags.Values.OrderBy(f => f.posX).ToList();
            _allPoints = new List<CapturePoint>();
            _baseScript._lastSpawn = new Dictionary<string, Helpers.ObjectState>();

            _totalFlags = _flags.Count;


            int flagcount = 1;
            foreach (Arena.FlagState flag in _flags)
            {
                if (flagcount <= 1)
                    flag.team = _team;
                if (flagcount >= 2)
                    flag.team = _botTeam;
                flagcount++;
                CapturePoint point = new CapturePoint(_arena, flag, _baseScript);
                point.Captured += delegate (Arena.FlagState capturedFlag)
                {
                    int playercount = _team.ActivePlayerCount;
                    int max = Convert.ToInt32(playercount * 2.5);
                    spawnRandomWave(_botTeam, max);
                };

                _allPoints.Add(point);
            }

            foreach (Player p in _arena.Players)
                Helpers.Object_Flags(p, _flags);


            Arena.FlagState team1Flag = _flags.OrderByDescending(f => f.posX).Where(f => f.team == _team).First();
            Arena.FlagState team2Flag = _flags.OrderBy(f => f.posX).Where(f => f.team == _botTeam).First();

            _activePoints = new List<CapturePoint>();
            _activePoints.Add(_allPoints.FirstOrDefault(p => p._flag == team1Flag));
            _activePoints.Add(_allPoints.FirstOrDefault(p => p._flag == team2Flag));
            _activePoints.Add(_allPoints.FirstOrDefault(p => p._flag.team == null));

            UpdateTickers();

            int timer = 1800 * 100;


            //Let everyone know
            _arena.sendArenaMessage("Game has started! Good luck Titans.");
            _arena.setTicker(1, 3, timer, "Time Left: ",
                delegate ()
                {   //Trigger game end
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

            foreach (Bot bot in _bots)
                _condemnedBots.Add(bot);

            foreach (Bot bot in _condemnedBots)
                bot.destroy(false);

            _condemnedBots.Clear();
            _bots.Clear();

            int conquered = (_flagsCaptured / _totalFlags) * 100;

            if (conquered == 100)
                _baseScript._winner = _team;
            else
            {
                _baseScript._winner = _botTeam;
                _arena.sendArenaMessage(String.Format("The Enemy is victorious. Better luck next time Soldiers!", _baseScript._winner._name, conquered));
                return;
            }

            _arena.sendArenaMessage(String.Format("{0} is Victorious, Good work Soldiers", _baseScript._winner._name, conquered));
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

                flagPoint = _baseScript.findFlagWarp(player, true);

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

        public void playerKillStreak(Player killer, int count)
        {
            switch (count)
            {
                case 10:
                    _arena.sendArenaMessage(string.Format("{0} is on fire!", killer._alias), 17);
                    break;
                case 30:
                    {
                        _arena.sendArenaMessage(string.Format("Someone kill {0}!", killer._alias), 18);
                        killer.sendMessage(0, "You've been awarded a Pavelow for your efforts, Check your inventory and call it in!");
                        killer.inventoryModify(1325, 1);
                    }
                    break;
                case 50:
                    _arena.sendArenaMessage(string.Format("{0} is dominating!", killer._alias), 19);
                    break;
                case 60:
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
            player.joinTeam(_team);
            return true;
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
            player.sendMessage(0, String.Format("Welcome to Cooperative mode, {0}", player._alias));

            if (Script_Multi._bCoopHappyHour)
                player.sendMessage(0, "&Co-Op Happy hour is currently active, Enjoy!");
            else
            {
                TimeSpan remaining = _baseScript.timeTo(Settings._coopHappyHourStart);
                player.sendMessage(0, String.Format("&Co-Op Happy hour starts in {0} hours & {1} minutes", remaining.Hours, remaining.Minutes));
            }

            //Obtain the Co-Op skill..
            SkillInfo coopskillInfo = _arena._server._assets.getSkillByID(200);



            //Add the skill!
            if (player.findSkill(200) == null)
            player.skillModify(coopskillInfo, 1);

            //Add the skill!
            if (player.findSkill(203) != null)
                player._skills.Remove(203);
            //Add the skill!
            if (player.findSkill(202) != null)
                player._skills.Remove(202);

            if (_botDifficulty <= 6)
            {
                player.sendMessage(2, String.Format("Powerups are enabled for this difficulty, {0}", player._alias));

                //Obtain the Co-Op skill..
                SkillInfo powerupskillInfo = _arena._server._assets.getSkillByID(201);

                //Add the skill!
                if (player.findSkill(201) == null)
                    player.skillModify(powerupskillInfo, 1);

            }
            else
            {
                player.sendMessage(2, String.Format("Powerups are disabled for this difficulty, {0}", player._alias));
                //Obtain the Powerup skill..
                SkillInfo powerupskillInfo2 = _arena._server._assets.getSkillByID(201);

                //Add the skill!
                if (player.findSkill(201) != null)
                    player._skills.Remove(201);
            }
            
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
            switch (weapon.id)
            {
                //Supply Drop?
                case 1325:
                    {
                        Helpers.ObjectState spawn = new Helpers.ObjectState();
                        spawn.positionX = posX;
                        spawn.positionY = posY;

                        Vehicle marker = _arena.newVehicle(AssetManager.Manager.getVehicleByID(406), _team, null, spawn);

                        if (marker != null)
                        {
                            _arena.sendArenaMessage(String.Format("&Air Support inbound to your coordinates ({0}), Sit tight Soldiers!",
                                Helpers.posToLetterCoord(marker._state.positionX, marker._state.positionY)), 4);
                            spawnGunship(player._team, marker, player);
                        }
                    }
                    break;

            }
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
        /// Updates our tickers
        /// </summary>
        public void UpdateTickers()
        {
            if (!_arena._bGameRunning)
            { return; }

            IEnumerable<Team> active = _arena.ActiveTeams;

            Team collie = active.Count() > 1 ? active.ElementAt(1) : _arena.getTeamByName(_config.teams[0].name);
            Team titan = active.Count() > 0 ? active.ElementAt(0) : _arena.getTeamByName(_config.teams[1].name);

            Rewards.Jackpot jackpot = Rewards.calculateJackpot(_arena.Players, null, Settings.GameTypes.Conquest, _baseScript, true);


              string format = string.Format("");
              //_arena.setTicker(1, 2, 0, format);
 

            //Personal Scores
            _arena.setTicker(2, 1, 0, delegate (Player p)
            {
                if (_baseScript.StatsCurrent(p) == null)
                    return "";

                Rewards.Jackpot.Reward reward = jackpot._playerRewards.FirstOrDefault(r => r.player == p);

                if (reward == null)
                   return "";

            //Update their ticker
            return string.Format("Personal Score: Kills={0} - Deaths={1} - MVP={2}%",
                _baseScript.StatsCurrent(p).kills,
                _baseScript.StatsCurrent(p).deaths,
                 Math.Round(reward.MVP * 100, 1));
            });


            //1st and 2nd place
            List<Rewards.Jackpot.Reward> ranked = new List<Rewards.Jackpot.Reward>();
            foreach (Rewards.Jackpot.Reward reward in jackpot._playerRewards.OrderByDescending(r => r.MVP))
            {
                if (reward.Score == 0)
                    continue;

                ranked.Add(reward);
            }

            int idx = 3; format = "";
            foreach (Rewards.Jackpot.Reward ranker in ranked)
            {
                if (idx-- == 0)
                    break;

                switch (idx)
                {
                    case 2:
                        format = string.Format("1st: {0}(K={1} D={2} MVP={3}%)", ranker.player._alias,
                          _baseScript.StatsCurrent(ranker.player).kills, _baseScript.StatsCurrent(ranker.player).deaths, Math.Round(ranker.MVP * 100, 1));
                        break;
                    case 1:
                        format = (format + string.Format(" 2nd: {0}(K={1} D={2} MVP={3}%)", ranker.player._alias,
                          _baseScript.StatsCurrent(ranker.player).kills, _baseScript.StatsCurrent(ranker.player).deaths, Math.Round(ranker.MVP * 100, 1)));
                        break;
                }
            }
            if (!_arena.recycling)
                _arena.setTicker(2, 0, 0, format);
        }
    }
}
#endregion