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
{ 	// Script Class
    /// Provides the interface between the script and arena
    ///////////////////////////////////////////////////////
    public class Royale
    {	///////////////////////////////////////////////////
        // Member Variables
        ///////////////////////////////////////////////////
        private Arena _arena;					//Pointer to our arena class
        private CfgInfo _config;                //The zone config
        public Script_Multi _baseScript;
        private Random _rand;


        private Team _victoryTeam;				//The team currently winning!
        private int _tickGameLastTickerUpdate;  //The tick at which the ticker was last updated
        private int _lastGameCheck;				//The tick at which we last checked for game viability
        private int _tickGameStarting;			//The tick at which the game began starting (0 == not initiated)
        public int _tickGameStart;				//The tick at which the game started (0 == stopped)
        private int _tickLastShrink;
        private int _tickLastReadyCheck;
        private int _tickLastPlayingCheck;

        private List<Team> _activeTeams;
        private Dictionary<string, bool> _playersReady;

        //Settings
        public bool bClassic;
        public bool bGameLocked;
        public bool bAllPlayersReady;
        public bool bSquadRoyale;

        private int playing = 0;
        private int _minPlayers = 2;				//The minimum amount of players
        private int _gameCount = 0;
        private int _playersPerTeam = 1;
        private ItemInfo.RepairItem oobEffect = AssetManager.Manager.getItemByID(172) as ItemInfo.RepairItem;
        private int manualTeamSizePick;

        private Boundary _storm;
        private Team _red;
        private Team _blue;
        private SkillInfo _classSkill = AssetManager.Manager.getSkillByID(38);

        public class Position
        {
            public short positionX;
            public short positionY;
        }

        ///////////////////////////////////////////////////
        // Member Functions
        ///////////////////////////////////////////////////
        /// <summary>
        /// Performs script initialization
        /// </summary>
        public Royale(Arena arena, Script_Multi baseScript)
        {
            _baseScript = baseScript;
            _arena = arena;
            _config = arena._server._zoneConfig;

            _storm = new Boundary(_arena, 0, 0, 0, 0);
            _red = _arena.getTeamByName("Red");
            _blue = _arena.getTeamByName("Blue");
            _rand = new Random();
            _activeTeams = new List<Team>();
            _playersReady = new Dictionary<string, bool>();
            _tickLastReadyCheck = 0;
            _tickLastPlayingCheck = 0;
            _tickGameStarting = 0; //test

            playing = 0;

            manualTeamSizePick = 0;
            bClassic = false; // Turn on looting.
            bGameLocked = false;
            bAllPlayersReady = false;
            bSquadRoyale = false;

            if (_arena._name.StartsWith("[Royale] Squad"))
            {
                bSquadRoyale = true;
                _baseScript.AllowPrivateTeams(true, 8);
            }
        }

        /// <summary>
        /// Allows the script to maintain itself
        /// </summary>
        public bool Poll(int now)
        {	//Should we check game state yet?
            // List<Player> crowns = _activeCrowns;

            

            if (now - _lastGameCheck <= Arena.gameCheckInterval)
                return true;


            _lastGameCheck = now;

            //Do we have enough players ingame?
            

            if (!bSquadRoyale)
                playing = _arena.PlayerCount;
            else if (bSquadRoyale)
            {
                
                if (_tickLastPlayingCheck == 0)
                {
                    _tickLastPlayingCheck = now;
                }
                    
                else if (now - _tickLastPlayingCheck > 1000)
                {
                    playing = 0;
                    foreach (Player player in _arena.Players)
                    {
                        if (player._team._name == "spec" || player._team._name == "Red" || player._team._name == "Blue")
                        {
                        }
                        else
                            playing++;
                    }
                    int privateTeamCount = 0;
                    foreach (Team team in _arena.Teams)
                    {
                        if (team.AllPlayers.Count() > 0 && team._isPrivate)
                        {
                            privateTeamCount++;
                        }
                    }

                    if (privateTeamCount <= 1)
                    {
                        playing = 0;
                    }
                    _tickLastPlayingCheck = now;
                }  
            }
            if (_arena._bGameRunning)
            {

                _storm.Poll(now);

                //Check win conditions
                checkForwinner(now);
            }

            //Update our tickers
            if (_baseScript._tickGameStarted > 0 && now - _arena._tickGameStarted > 2000)
            {
                if (now - _tickGameLastTickerUpdate > 1000)
                {
                    updateTickers();
                    _tickGameLastTickerUpdate = now;
                }
            }

            if (_arena._bGameRunning && playing < _minPlayers && _arena._bIsPublic && _victoryTeam == null)
            {
                _baseScript.bJackpot = false;
                //Stop the game and reset voting
                _arena.gameEnd();

            }
            if (playing < _minPlayers && _arena._bIsPublic)
            {
                _baseScript._tickGameStarting = 0;
                _arena.setTicker(1, 3, 0, "Not Enough Players");
                bGameLocked = false;
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
                gameSetup();
                //Put 15 second locked timer in

                _arena.setTicker(1, 3, 20 * 100, "Time until entrance into the tournament is locked: ",
                    delegate ()
                    {   //Trigger the game start
                        bGameLocked = true;

                        if (!bSquadRoyale)
                        {
                            _baseScript.AllowPrivateTeams(false, 1);
                            //Scramble it up
                            _arena.scrambleTeams(_arena.PlayersIngame.Where(p => !p._team._isPrivate),
                                    _arena.Teams.Where(team => team.IsPublic && team._name != "spec"
                                    && team._name.StartsWith("Public")).ToList(), _playersPerTeam);
                        }
                        else
                        {

                        }

                        _arena.setTicker(1, 3, 15 * 100, "Time until tournament starts: ",

                        delegate ()
                        {   //Trigger the game start
                        });
                    });
            }
            if (_baseScript._tickGameStarting > 0 && now - _baseScript._tickGameStarting > 15000)
            {
                if (_tickLastReadyCheck == 0)
                {
                    _tickLastReadyCheck = now;
                }
                else if (now - _tickLastReadyCheck > 2000)
                {
                    if (checkForAllPlayersReady())
                    {
                        _arena.gameStart();
                        if (bSquadRoyale)
                            _baseScript.AllowPrivateTeams(false, 1);
                    }
                    else if (now - _baseScript._tickGameStarting > 35000)
                    {
                        _arena.gameStart();
                        if (bSquadRoyale)
                            _baseScript.AllowPrivateTeams(false, 1);
                    }
                    _tickLastReadyCheck = now;
                }                  
                   // _arena.sendArenaMessage(String.Format("Test Before: {0}", checkForAllPlayersReady()));
            }

                return true;
        }

        public void gameSetup()
        {

            if (bSquadRoyale)
            {
                /*
                int maxPrivateTeamSize = 0;
                foreach (Player player in _arena.PlayersIngame)
                {
                    if (player._team._name == "Red" || player._team._name == "Blue")
                        pickTeam(player);
                }

                foreach (Team team in _arena.Teams)
                {
                    if (team._isPrivate)
                    {
                        if (team.AllPlayers.Count() > maxPrivateTeamSize)
                            maxPrivateTeamSize = team.AllPlayers.Count();
                    }
                }

                foreach (Team team in _arena.ActiveTeams.Where(tm => tm._isPrivate))
                {
                    List<Player> playersRemoved = new List<Player>();

                    //If they are within our parameter, ignore.
                    if (team.ActivePlayerCount <= _playersPerTeam)
                        continue;

                    int numToRemove = team.ActivePlayerCount - _playersPerTeam;

                    for (int i = 0; i < numToRemove; i++)
                    {
                        Player rndPlayer = team.ActivePlayers.PickRandom();
                        if (rndPlayer == null)
                            continue;

                        rndPlayer.spec();
                        team.addPlayer(rndPlayer);
                        rndPlayer.sendMessage(0, "You've randomly been moved to spec to keep teams even.");
                    }
                }
                */
                foreach (Player player in _arena.Players)
                {
                   // _arena.sendArenaMessage(" " + player._alias + " ");
                    if (player._team._name == "Red" || player._team._name == "Blue")
                    {
                        //pickTeam(player);
                    }
                    else if (player.IsSpectator && player._team._name != "spec")
                    {
                        Team oldTeam = player._team;
                        //player._bSpectator = false;
                        //player.joinTeam(player._team);
                        //player.unspec(player._team);
                        pickOutTeam(player);
                        oldTeam.addPlayer(player);
                    }

                }

                _arena.sendArenaMessage("$A new Squad Royale game is starting in 35 seconds or less. Players please choose your team in the next 20 seconds. At the end of these 20 seconds, private team selection will also be locked.");
                _arena.sendArenaMessage("&---Game will start early by having all players 'Ready Up' by walking over a Dropship Portal.");
                _playersPerTeam = 8;
                _baseScript.AllowPrivateTeams(true, _playersPerTeam);
                return;
            }

            _arena.sendArenaMessage("$A new game is starting in 35 seconds or less. Please unspec in the next 20 seconds to play in the next tournament. At the end of this 20 seconds, private team selection will also be locked.");

            foreach (Player player in _arena.PlayersIngame)
            {
                if (player._team._name == "Red" || player._team._name == "Blue")
                    pickTeam(player);             
            }

            List<int> teamSizePickList = new List<int>();
            int playersToConsider = _arena.PlayerCount; // later adjust this for private teams.

            if (playersToConsider <= 7)
            {
                teamSizePickList.Add(1);
            }
            if ((playersToConsider >= 4) && (playersToConsider <= 16))
            {
                teamSizePickList.Add(2);
            }
            if (playersToConsider % 2 == 0) //If can split team evenly ,add that team size.
                teamSizePickList.Add(playersToConsider / 2);

            if (((playersToConsider % 3 == 0) && (playersToConsider != 3)) || ((playersToConsider >= 11) && (playersToConsider <= 30)))
            {
                teamSizePickList.Add(3);
            }
            if (((playersToConsider % 4 == 0) && (playersToConsider != 4)) || ((playersToConsider >= 22) && (playersToConsider <= 40)))
            {
                teamSizePickList.Add(4);
            }
            if (((playersToConsider % 5 == 0) && (playersToConsider != 5)) || ((playersToConsider >= 28) && (playersToConsider <= 50)))
            {
                teamSizePickList.Add(5);
            }
            if (((playersToConsider % 6 == 0) && (playersToConsider != 6)) || ((playersToConsider >= 34) && (playersToConsider <= 60)))
            {
                teamSizePickList.Add(6);
            }
            if (((playersToConsider % 7 == 0) && (playersToConsider != 7)) || ((playersToConsider >= 40) && (playersToConsider <= 70)))
            {
                teamSizePickList.Add(7);
            }

            Random randTeamSizePick = new Random();

            int teamSizePick = teamSizePickList[randTeamSizePick.Next(teamSizePickList.Count)];
            _playersPerTeam = teamSizePick;

            //Even out any private teams that are OVER our current limit
            foreach (Team team in _arena.ActiveTeams.Where(tm => tm._isPrivate))
            {
                List<Player> playersRemoved = new List<Player>();

                //If they are within our parameter, ignore.
                if (team.ActivePlayerCount <= _playersPerTeam)
                    continue;

                int numToRemove = team.ActivePlayerCount - _playersPerTeam;

                for (int i = 0; i < numToRemove; i++)
                {
                    Player rndPlayer = team.ActivePlayers.PickRandom();
                    if (rndPlayer == null)
                        continue;

                    pickTeam(rndPlayer);
                    rndPlayer.sendMessage(0, "You've randomly been moved to a public team to keep teams even.");
                }
            }
            
            _arena.sendArenaMessage(String.Format("&--Max team size for this game: {0}", _playersPerTeam));
            _arena.sendArenaMessage("&---Game will start early by having all players 'Ready Up' by walking over a Dropship Portal.");

            _baseScript.AllowPrivateTeams(true, _playersPerTeam);
        }

        public void checkForwinner(int now)
        {
            List<Team> removes = _activeTeams.Where(t => t.ActivePlayerCount == 0).ToList();

            foreach (Team team in removes)
                if (_activeTeams.Contains(team))
                    _activeTeams.Remove(team);

            List<Team> removes2 = new List<Team>();

            foreach (Team team in _activeTeams)
            {
                int teamAlivePlayerCount = team.ActivePlayerCount;
                foreach (Player player in team.ActivePlayers)
                {
                    if (player.IsDead)
                    {
                        teamAlivePlayerCount--;
                    }

                }
                if (teamAlivePlayerCount == 0)
                    removes2.Add(team);
            }

            foreach (Team team in removes2)
                if (_activeTeams.Contains(team))
                    _activeTeams.Remove(team);

            int teamCount = _activeTeams.Count;
            if (teamCount == 1)
            {
                _victoryTeam = _activeTeams.First();
                _baseScript._winner = _victoryTeam;

                if (_victoryTeam == null)
                    _arena.sendArenaMessage("There was no winner");
                else
                    _arena.sendArenaMessage(String.Format("{0} has won!", _victoryTeam._name));


                _baseScript.bJackpot = false;

                int gameLength = 0;
                int fixedJackpot = 2000;
                gameLength = (now - _tickGameStart) / 1000;

                double jackpot = _arena.TotalPlayerCount * 500;
                jackpot += (gameLength * 50);
                jackpot += fixedJackpot;

                int bonusCash = 0;
                int bonusExp = 0;
                int bonusPoints = 0;

                foreach (Player player in _arena.Players)
                {
                    if (_baseScript.StatsCurrent(player).hasPlayed)
                    {
                        if (_victoryTeam != null)
                        {
                            if (player._team._name == _victoryTeam._name)
                            {
                                bonusCash = Convert.ToInt32(jackpot * 0.5);
                                bonusExp = Convert.ToInt32(jackpot * 0.5);
                                bonusPoints = Convert.ToInt32(jackpot * 1);
                            }
                            else
                            {
                                bonusCash = Convert.ToInt32((jackpot * 0.5) / 4);
                                bonusExp = Convert.ToInt32((jackpot * 0.5) / 4);
                                bonusPoints = Convert.ToInt32((jackpot * 1) / 4);
                            }
                        }
                        else
                        {
                            bonusCash = Convert.ToInt32((jackpot * 0.5) / 4);
                            bonusExp = Convert.ToInt32((jackpot * 0.5) / 4);
                            bonusPoints = Convert.ToInt32((jackpot * 1) / 4);
                        }

                        player.sendMessage(0, String.Format("Tournament Bonus: (Points={2} Cash={0} Experience={1} )", bonusCash, bonusExp, bonusPoints));

                        if (Script_Multi._bPvpHappyHour)
                        {

                            player.sendMessage(0, String.Format("Additional PvP Happy Hour Bonus: (Cash={0})", bonusCash));
                            bonusCash += bonusCash;
                        }

                        player.Cash += bonusCash;
                        player.Experience += bonusExp;
                        player.BonusPoints += bonusPoints;
                        player.syncState();
                    }
                    


                }
                _arena.gameEnd();
            }
            if (teamCount == 0)  //What about a tie?
            {
                _victoryTeam = null;
                _baseScript._winner = null;

                if (_victoryTeam == null)
                    _arena.sendArenaMessage("There was no winner");

                _baseScript.bJackpot = false;

                int gameLength = 0;
                int fixedJackpot = 2000;
                gameLength = (now - _tickGameStart) / 1000;

                double jackpot = _arena.TotalPlayerCount * 500;
                jackpot += (gameLength * 50);
                jackpot += fixedJackpot;

                int bonusCash = 0;
                int bonusExp = 0;
                int bonusPoints = 0;

                foreach (Player player in _arena.Players)
                {
                    if (_baseScript.StatsCurrent(player).hasPlayed)
                    {
                        if (_victoryTeam != null)
                        {
                            if (player._team._name == _victoryTeam._name)
                            {
                                bonusCash = Convert.ToInt32(jackpot * 0.5);
                                bonusExp = Convert.ToInt32(jackpot * 0.5);
                                bonusPoints = Convert.ToInt32(jackpot * 1);
                            }
                            else
                            {
                                bonusCash = Convert.ToInt32((jackpot * 0.5) / 4);
                                bonusExp = Convert.ToInt32((jackpot * 0.5) / 4);
                                bonusPoints = Convert.ToInt32((jackpot * 1) / 4);
                            }
                        }
                        else
                        {
                            bonusCash = Convert.ToInt32((jackpot * 0.5) / 4);
                            bonusExp = Convert.ToInt32((jackpot * 0.5) / 4);
                            bonusPoints = Convert.ToInt32((jackpot * 1) / 4);
                        }

                        

                        player.sendMessage(0, String.Format("Tournament Bonus: (Points={2} Cash={0} Experience={1} )", bonusCash, bonusExp, bonusPoints));
                        if (Script_Multi._bPvpHappyHour)
                        {
             
                            player.sendMessage(0, String.Format("Additional PvP Happy Hour Bonus: (Cash={0})", bonusCash));
                            bonusCash += bonusCash;
                        }
                        player.Cash += bonusCash;
                        player.Experience += bonusExp;
                        player.BonusPoints += bonusPoints;
                        player.syncState();
                    }



                }
                _arena.gameEnd();
            }
        }

        public void squadRoyaleFinishSetup()
        {
            List<Team> squadRoyalePotentialTeams = new List<Team>();
            
            foreach (Team team in _arena.Teams)
            {
                if (team._name != "Red" || team._name != "Blue" || team._name != "spec")
                {
                    squadRoyalePotentialTeams.Add(team);
                }
            }

            //squadRoyalePotentialTeams = squadRoyalePotentialTeams.OrderByDescending(x => x.AllPlayers.Count);
            //squadRoyalePotentialTeams = _arena.t.Where(team => team._name != "Red" && team._name != "Blue" && !team.isSpec).OrderByDescending(x => x.AllPlayers.Count);
        }

        public bool checkForAllPlayersReady()
        {
            foreach (Player player in _arena.PlayersIngame)
            {
                if (player._team._name == "Red" || player._team._name == "Blue" || player._team._name == "spec")
                    continue;

                if (_playersReady.ContainsKey(player._alias))
                {
                    if (_playersReady[player._alias])
                    {
                        continue;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    _playersReady[player._alias] = false;
                    return false;
                }

            }
            _playersReady.Clear();
            return true;
        }

        #region Events

        /// <summary>
        /// Called when a player enters the arena
        /// </summary>
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
            if (player.findSkill(202) != null)
                player._skills.Remove(202);

            //Add the New Player Skill if Points under 200k , remove if over
            if (player.Points > 1000000)
            {
                if (player.findSkill(253) != null)
                    player._skills.Remove(253);
            }
            else if (player.findSkill(253) == null)
            {
                //Obtain the New Player Skill..
                SkillInfo newPlayerSkill = _arena._server._assets.getSkillByID(253);
                //Add the skill!
                player.skillModify(newPlayerSkill, 1);
            }

            //Add the Weekly Class Test Skill
            if (player.findSkill(254) == null)
            {
                //Obtain the Weekly Class Test Skill..
                SkillInfo weeklyClassSkill = _arena._server._assets.getSkillByID(254);
                //Add the skill!
                player.skillModify(weeklyClassSkill, 1);
            }
            player.syncState();


        }

        /// <summary>
        /// Called when a player enters the game
        /// </summary>
        public void playerEnter(Player player)
        {

        }

        /// <summary>
        /// Called when a player leaves the game
        /// </summary>
        public void playerLeave(Player player)
        {
        }

        /// <summary>
        /// Triggered when a player tries to heal
        /// </summary>
        public void PlayerRepair(Player from, ItemInfo.RepairItem item)
        {
        }


        public void playerLeaveArena(Player player)
        {
        }

        /// <summary>
        /// Called when the game begins
        /// </summary>
        public bool gameStart()
        {	//We've started!
            _tickGameStart = Environment.TickCount;
            _tickGameStarting = 0;
            _tickLastReadyCheck = 0;
            _tickLastShrink = Environment.TickCount;
            _victoryTeam = null;
            _activeTeams.Clear();

            List<ItemInfo> removes = AssetManager.Manager.getItems.Where(itm => itm.name.EndsWith("(BR)")).ToList();

            foreach (Player player in _arena.Players)
            {
                foreach (ItemInfo item in removes)
                    if (player.getInventoryAmount(item.id) > 0)
                        player.removeAllItemFromInventory(item.id);
            }

                //Let everyone know
                _arena.sendArenaMessage("Game has started!", 1);

            List<short> spawnPoints = new List<short>();
            short maxLeft = 1920;
            short maxRight = 30832;
            short center = 16488;
            short separation = 3000; //previously 1500
            short current = 0;

            int teamCount = _arena.ActiveTeams.Count(team => team._name != "Red" || team._name != "Blue");

            //Adjust separation depending on # of teams (Separation at 20 teams would be ~1000)
            separation -= (short)(teamCount * 90);

            short requiredSpace = (short)(teamCount * separation);

            while (true)
            {
                current = (short)_rand.Next(maxLeft, maxRight);

                if (current + requiredSpace > maxRight)
                    continue;

                break;
            }
            
            foreach (Team team in _arena.ActiveTeams)
            {
                if (team._name == "Red" || team._name == "Blue") // Skip Red vs Blue for spawning
                    continue;

                //Find a good location to spawn
                short pX = current, pY = 1744;

                //Spawn each player around this point
                foreach (Player player in team.ActivePlayers)
                {
                    if (bClassic)
                    {
                        player.resetSkills();
                        player.skillModify(true, _classSkill, 1);
                    }
                    player.warp(pX, pY);
                    player.inventoryModify(203, 2);

                    if (player.Bounty < 1000)
                    player.Bounty = 1000;
                }

                spawnPoints.Add(current);
                _activeTeams.Add(team);

                current += separation;
            }

            //Start up the storm!
            _storm = new Boundary(_arena, 104, 3624, (short)(spawnPoints.Last() + 800), (short)(spawnPoints.First() - 800));
            _arena.setTicker(1, 3, 60 * 100, "Play area shrinking in: ");

            //Clear flags
            _arena.flagReset();

            //Hide some loot boxes
            if (bClassic)
            hideLootBoxes(_playersPerTeam, spawnPoints);
            return true;
        }

        /// <summary>
        /// Updates our tickers
        /// </summary>
        public void updateTickers()
        {
        }

        /// <summary>
        /// Called when the game ends
        /// </summary>
        public bool gameEnd()
        {	//Game finished, perhaps start a new one
            _gameCount = _gameCount + 1;
            _arena.sendArenaMessage("Game Over");
            _tickGameStart = 0;
            _tickGameStarting = 0;
            _tickLastReadyCheck = 0;
            _victoryTeam = null;

            bGameLocked = false;
            if (bSquadRoyale) //changed
                _baseScript.AllowPrivateTeams(true, 8); 

            List<ItemInfo> removes = AssetManager.Manager.getItems.Where(itm => itm.name.EndsWith("(BR)")).ToList();

            foreach (Player player in _arena.Players)
            {
                if (!player.IsDead && !player.IsSpectator)
                    player.warp(1924*16, 347*16);

                foreach (ItemInfo item in removes)
                    if (player.getInventoryAmount(item.id) > 0)
                        player.removeAllItemFromInventory(item.id);

                if ((player._team._name == "Red" || player._team._name == "Blue") && !bSquadRoyale)
                {
                    if (_arena.PlayerCount >= _minPlayers)
                    {
                        pickTeam(player);
                        continue;
                    }
                    
                }

                if (player._team._name == "spec")
                    continue;

                if (!player.IsSpectator)
                    continue;

                /*if (_arena.PlayerCount >= _minPlayers) changed
                    player.unspec(player._team);    
*/
                    //player.joinTeam(player._team); 
                
            }

            return true;
        }

        /// <summary>
        /// Called when the statistical breakdown is displayed
        /// </summary>
        public bool playerBreakdown(Player from, bool bCurrent)
        {	//Show some statistics!
            return false;
        }

        /// <summary>
        /// Called when the statistical breakdown is displayed
        /// </summary>
        public bool breakdown()
        {	//Allows additional "custom" breakdown information

            //Always return true;
            return false;
        }

        /// <summary>
        /// Called to reset the game state
        /// </summary>
        public bool gameReset()
        {	//Game reset, perhaps start a new one
            _tickGameStart = 0;
            _tickGameStarting = 0;
            bGameLocked = false;
            _victoryTeam = null;
            return true;
        }

        public bool playerPortal(Player player, LioInfo.Portal portal)
        {
            if (portal.GeneralData.Name.Contains("DS Portal"))
            {
                if ((!_arena._bGameRunning) && (bGameLocked))
                {
                    _playersReady[player._alias] = true;

                    player.sendMessage(0, "You have marked yourself ready for the next game! Game will start early when all other players ready up.");
                }

            }
            return false;
        }

        /// <summary>
        /// Handles the spawn of a player
        /// </summary>
        public bool playerSpawn(Player player, bool bDeath)
        {
            if (player._team == _red || player._team == _blue)
                return true;

            Team oldTeam = player._team;
            if (_arena._bGameRunning && bDeath)
            {
                if (bSquadRoyale)
                {
                    //player.spec("spec");
                    //pickOutTeam(player);
                    if (oldTeam == null)
                        _arena.createTeam(oldTeam);
                    player.spec(oldTeam);
                    
                    //if (oldTeam != null)
                    //oldTeam.addPlayer(player);
                    //_arena.sendArenaMessage("Spawn Debug 2.");
                }                                
                else
                {
                    player.spec("spec");
                    pickOutTeam(player);
                }
                
                if (oldTeam.ActivePlayerCount == 0) // changed
                    _activeTeams.Remove(oldTeam);

            }
            return true;
        }

        /// <summary>
        /// Triggered when a player wants to unspec and join the game
        /// </summary>
        public bool playerJoinGame(Player player)
        {
            if ((_arena._bGameRunning) || (bGameLocked)) // Probably can just gamelocked
            {
                pickOutTeam(player);
                player.sendMessage(0, "A Royale Tournament is currently going on. You will be automatically entered in the next one. Feel free to play Red vs Blue until then.");
            }
              
            else if (_baseScript._tickGameStarting == 0 && _arena.PlayerCount < _minPlayers)
            {
                pickOutTeam(player);
                //player.sendMessage(0, "A Royale Tournament will start when there are 2 or more players. Feel free to play Red vs Blue until then.");
            }
            else if (bSquadRoyale)
                pickOutTeam(player);
            else
                pickTeam(player);


            if (player.getInventoryAmount(188) == 0) // Check for Royale Armor and add it.
                player.inventoryModify(188, 1);

            return true;
        }

        public void pickOutTeam(Player player)
        {
            if (_red.ActivePlayerCount <= _blue.ActivePlayerCount)
            {
                if (player._team != _red)
                {
                    player.unspec(_red);
                    //player.joinTeam(_red);
                }
            }
            else
            {
                if (player._team != _blue)
                {
                    player.unspec(_blue);
                    //player.joinTeam(_blue);
                }
            }
        }

        public void pickTeam(Player player)
        {
            List<Team> potentialTeams = _arena.ActiveTeams.Where(t => t._name.StartsWith("Public") && t.ActivePlayerCount < _playersPerTeam).ToList();

            //Put him on a new Public Team
            if (potentialTeams.Count == 0)
            {
                Team newTeam = _arena.PublicTeams.First(t => t._name.StartsWith("Public") && t.ActivePlayerCount == 0);
                newTeam.addPlayer(player);
            }
            else
                potentialTeams.First().addPlayer(player);
        }

        /// <summary>
        /// Triggered when a player wants to spec and leave the game
        /// </summary>
        public bool playerLeaveGame(Player player)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a player has died, by any means
        /// </summary>
        /// <remarks>killer may be null if it wasn't a player kill</remarks>
        public bool playerDeath(Player victim, Player killer, Helpers.KillType killType, CS_VehicleDeath update)
        {

            if (victim._team == _red || victim._team == _blue)
                return true;

            if (_arena._bGameRunning)
            {
                if (killer == null)
                    _arena.sendArenaMessage(String.Format("{0} has been knocked out of the Tournament by the storm.", victim._alias));
                else if (killer._alias == victim._alias)
                    _arena.sendArenaMessage(String.Format("{0} has been knocked out of the Tournament by the storm.", victim._alias));
                else
                    _arena.sendArenaMessage(String.Format("{0} has been knocked out of the Tournament by {1}.", victim._alias, killer._alias));

                victim.sendMessage(0, "You've been knocked out of the tournament, You may continue to play Red/Blue until the next game!");
            }

            return true;
        }

        /// <summary>
        /// Triggered when one player has killed another
        /// </summary>
        public bool playerPlayerKill(Player victim, Player killer)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a vehicle dies
        /// </summary>
        public bool vehicleDeath(Vehicle dead, Player killer)
        {
            if (dead._type.Name == "Loot Box")
            {
                List<ItemInfo> potentialItems = AssetManager.Manager.getItems.Where(itm => itm.name.EndsWith("(Loot)")).ToList();

                if (potentialItems.Count == 0)
                    return true;

                int idx = _rand.Next(potentialItems.Count);

                ItemInfo drop = potentialItems[idx];

                if (drop == null)
                    return true;

                _arena.itemSpawn(drop, 1, dead._state.positionX, dead._state.positionY, null);
            }

            return true;
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

        #region Private Routines
        private void hideLootBoxes(int count, List<short> spawnPoints)
        {
            VehInfo box = AssetManager.Manager.getVehicleByID(413);

            foreach (short posX in spawnPoints)
            {
                short posY = 1744;

                for (int i = 0; i < count; i++)
                {
                    //Generate some random coordinates
                    short pX = 0;
                    short pY = 0;
                    int attempts = 0;
                    for (; attempts < 30; attempts++)
                    {
                        pX = posX;
                        pY = posY;

                        Helpers.randomPositionInArea(_arena, 2500, ref pX, ref pY);

                        //Is it blocked?
                        if (_arena.getTile(pX, pY).Blocked)
                            //Try again
                            continue;

                        Helpers.ObjectState newState = new Helpers.ObjectState();
                        newState.positionX = pX;
                        newState.positionY = pY;
                        _arena.newVehicle(box, _arena.getTeamByName("spec"), null, newState);
                        break;
                    }
                }
            }
                
        }
        #endregion
    }
}