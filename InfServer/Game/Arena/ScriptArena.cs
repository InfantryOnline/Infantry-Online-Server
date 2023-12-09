﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using InfServer.Network;
using InfServer.Protocol;
using InfServer.Logic;
using InfServer.Scripting;
using InfServer.Bots;

using Assets;

namespace InfServer.Game
{
    // ScriptArena Class
    /// Exposes the arena methodology to scripting
    ///////////////////////////////////////////////////////
    public class ScriptArena : Arena
    {	// Member variables
        ///////////////////////////////////////////////////
        private List<Scripts.IScript> _scripts;		//The scripts we're currently supporting
        private string _scriptType;					//The type of scripts we're instancing
        private CfgInfo.StartGame _startCfg;

        ///////////////////////////////////////////////////
        // Member Functions
        ///////////////////////////////////////////////////
        /// <summary>
        /// Generic constructor
        /// </summary>
        public ScriptArena(ZoneServer server, string scriptType)
            : base(server)
        {
            _scriptType = scriptType;
        }

        /// <summary>
        /// Initializes arena details
        /// </summary>
        public override void init()
        {	//Initialize the base arena class
            base.init();

            //Initialize our breakdown settings
            _breakdownSettings = new BreakdownSettings();

            //Load the associated scripts
            if (_scriptType != null)
                _scripts = Scripts.instanceScripts(this, _scriptType);

            //Cache this just because
            _startCfg = _server._zoneConfig.startGame;

            //Run initial hides if it doesn't depend on a game running
            if (!_startCfg.initialHides)
                initialHideSpawns();
        }

        /// <summary>
        /// Reloads our scripts
        /// </summary>
        public override void reloadScript()
        {

            //Is this a registered arena name?
            string invokerType = _server._config["server/gameType"].Value;
            IList<ConfigSetting> namedArenas = _server._config["arena"].GetNamedChildren("namedArena");

            foreach (ConfigSetting named in namedArenas)
            {	//Correct arena?
                if (_name.Equals(named.Value, StringComparison.OrdinalIgnoreCase))
                {
                    invokerType = named["gameType"].Value;
                    break;
                }
            }
            //Instance our gametype
            if (!Scripting.Scripts.invokerTypeExists(invokerType))
            {
                Log.write(TLog.Error, "Unable to find gameType '{0}'", invokerType);
            }

            _scriptType = invokerType;

            //Load the associated scripts
            if (_scriptType != null)
                _scripts = Scripts.instanceScripts(this, _scriptType);
        }

        /// <summary>
        /// Allows the arena to keep it's game state up-to-date
        /// </summary>
        public override void poll()
        {	//Process the base state
            base.poll();

            //Do we have a script loaded?
            if (_scriptType == null)
                return;

            //Poll all scripts!
            foreach (Scripts.IScript script in _scripts)
                script.poll();
        }

        #region Events

        #region playerEnter
        /// <summary>
        /// Called when a player successfully enters the game
        /// Note: this updates arena player counts
        /// </summary>
        public override void playerEnter(Player player)
        {
            if (player != null)
            {
                //Update player count first
                base.playerEnter(player);

                //Pass it to the script environment
                callsync("Player.Enter", false, player);
            }
            else
                Log.write(TLog.Error, "playerEnter(): Called with null player");
        }
        #endregion

        #region playerLeave
        /// <summary>
        /// Called when a player successfully leaves the game
        /// Note: this updates arena player counts
        /// </summary>
        public override void playerLeave(Player player)
        {
            if (player != null)
            {
                //Update player count first
                base.playerLeave(player);

                //Pass it to the script environment
                callsync("Player.Leave", false, player);
            }
            else
                Log.write(TLog.Error, "playerLeave(): Called with null player");
        }
        #endregion

        #region pollQuestion
        ///<summary>
        ///Called when a poll has ended
        ///</summary>
        public override void pollQuestion(Arena arena, bool gameEnd)
        {
            //Are we cancelling this poll?
            if (gameEnd)
                //This is not a poll cancel
                arena.sendArenaMessage(String.Format("Poll Results Are In: Yes={0} No={1} - Thanks for playing!", arena._poll.yes, arena._poll.no));
            //Reset the poll
            arena._poll.start = false;
            arena._poll.no = 0;
            arena._poll.yes = 0;
            arena._poll._alias = new Dictionary<String, Arena.PollSettings.PlayerAlias>();
        }
        #endregion

        #region scrambleTeams
        /// <summary>
        /// Scrambles teams based on cfg file
        /// </summary>
        public static void scrambleTeams(Arena arena, int numTeams, bool alertArena)
        {
            List<Player> shuffledPlayers = arena.PublicPlayersInGame.OrderBy(plyr => arena._rand.Next(0, 500)).ToList();
            IEnumerable<Team> active = arena.PublicTeams.Where(t => t.ActivePlayerCount > 0).ToList();
            for (int i = 0; i < shuffledPlayers.Count; i++)
            {
                Team team = active.ElementAt(i % numTeams);
                if (shuffledPlayers[i]._team != team)
                    team.addPlayer(shuffledPlayers[i]);
            }

            //Notify players of the scramble
            if (alertArena)
                arena.sendArenaMessage("Teams have been scrambled!");
        }
        #endregion

        #region gameStart
        /// <summary>
        /// Called when the game begins
        /// </summary>
        public override void gameStart()
        {
            //We're running!
            _bGameRunning = true;
            _tickGameStarted = Environment.TickCount;
            _tickGameEnded = 0;

            //Reset the playing balls
            if (_balls.Count() > 0)
                resetBalls();

            //What else do we need to reset?
            if (_startCfg.prizeReset)
                resetItems();

            if (_startCfg.vehicleReset)
                resetVehicles();

            if (_startCfg.initialHides)
                initialHideSpawns();

            //Scramble teams if the cfg calls for it
            if (_scramble && PlayerCount > 2)
                scrambleTeams(this, _server._zoneConfig.arena.desiredFrequencies, true);

            //Clear the arena stats
            ClearCurrentStats();

            //Handle the start for all players
            string startGame = _server._zoneConfig.EventInfo.startGame;

            foreach (Player player in Players)
            {	//Reset ball handling
                player._gotBallID = 999;

                //We don't want previous stats to count
                player.clearCurrentStats();

                //Reset anything else we're told to
                if (_startCfg.clearProjectiles)
                    player.clearProjectiles();

                if (_startCfg.resetInventory && _startCfg.resetCharacter)
                {
                    player.resetInventory(false);
                    player.resetSkills(true);
                }
                else if (_startCfg.resetCharacter)
                    player.resetSkills(true);
                else if (_startCfg.resetInventory)
                    player.resetInventory(true);

                //Add them to the arena stats
                AddArenaStat(player);

                //Run the event if necessary
                if (!player.IsSpectator)
                    Logic_Assets.RunEvent(player, startGame);
            }

            //Clear the team stats
            foreach (Team t in Teams)
            {
                t._currentGameKills = 0;
                t._currentGameDeaths = 0;
            }

            //Pass it to the script environment
            callsync("Game.Start", false);
        }
        #endregion

        #region gameEnd
        /// <summary>
        /// Called when the game ends
        /// </summary>
        public override void gameEnd()
        {	//Show breakdown
            breakdown(true);

            //We've stopped
            _bGameRunning = false;
            _tickGameEnded = Environment.TickCount;

            //Execute the end game event
            string endGame = _server._zoneConfig.EventInfo.endGame;

            foreach (Player player in Players)
            {
                //Keep the player's game stats updated
                player.migrateStats();

                player.syncState();

                //Run the event if necessary
                if (!player.IsSpectator)
                    Logic_Assets.RunEvent(player, endGame);
            }

            //Reset any poll questions and display results
            if (_poll != null && _poll.start)
                pollQuestion(this, true);

            //Pass it to the script environment
            callsync("Game.End", false);
        }
        #endregion

        #region gameReset
        /// <summary>
        /// Called to reset the game state
        /// </summary>
        public override void gameReset()
        {

            if (_startCfg.prizeReset)
                resetItems();
            if (_startCfg.vehicleReset)
                resetVehicles();

            //Pass it to the script environment
            callsync("Game.Reset", false);
        }
        #endregion

        #region individualBreakdown
        /// <summary>
        /// Creates a breakdown tailored for one player
        /// </summary>
        public override void individualBreakdown(Player from, bool bCurrent)
        {
            if (from == null)
            {
                Log.write(TLog.Error, "individualBreakdown(): Called with null player.");
                return;
            }

            //WTF, Over.
            //No need to return here, just logging.
            if (from.StatsCurrentGame == null)
                Log.write(TLog.Error, "Player {0} has no stats.", from);

            //Give the script a chance to take over
            if ((bool)callsync("Player.Breakdown", false, from, bCurrent))
                return;

            //Display Team Stats?
            if (_breakdownSettings.bDisplayTeam)
            {
                from.sendMessage(0, "#Team Statistics Breakdown");

                List<Team> activeTeams = _teams.Values.Where(entry => entry.ActivePlayerCount > 0).ToList();
                List<Team> rankedTeams = activeTeams.OrderByDescending(entry => entry._currentGameKills).ToList();
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
            }

            //Do we want to display individual statistics?
            if (_breakdownSettings.bDisplayIndividual)
            {
                from.sendMessage(0, "#Individual Statistics Breakdown");

                List<Player> rankedPlayers = Players.ToList().OrderBy(player => 
                    (bCurrent ? (player.StatsCurrentGame == null ? 0 : player.StatsCurrentGame.deaths)
                        : (player.StatsLastGame == null ? 0 : player.StatsLastGame.deaths))).OrderByDescending(
                    player => (bCurrent ? (player.StatsCurrentGame == null ? 0 : player.StatsCurrentGame.kills)
                        : (player.StatsLastGame == null ? 0 : player.StatsLastGame.kills))).ToList();
                int idx = 3;	//Only display top three players

                foreach (Player p in rankedPlayers)
                {   //Do they even have stats?
                    if (bCurrent ? p.StatsCurrentGame == null : p.StatsLastGame == null)
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

                    from.sendMessage(0, string.Format(format,
                        (bCurrent ? p.StatsCurrentGame.kills : p.StatsLastGame.kills),
                        (bCurrent ? p.StatsCurrentGame.deaths : p.StatsLastGame.deaths),
                        p._alias));
                }

                //Do we have stats for them?
                if ((bCurrent && from.StatsCurrentGame != null) || (!bCurrent && from.StatsLastGame != null))
                {
                    string personalFormat = "@Personal Score: (K={0} D={1})";
                    from.sendMessage(0, string.Format(personalFormat,
                        (bCurrent ? from.StatsCurrentGame.kills : from.StatsLastGame.kills),
                        (bCurrent ? from.StatsCurrentGame.deaths : from.StatsLastGame.deaths)));
                }
            }
        }
        #endregion

        #region breakdown
        /// <summary>
        /// Called when the game needs to display end game statistics
        /// </summary>
        public override void breakdown(bool bCurrent)
        {   //Let the script add custom info
            callsync("Game.Breakdown", false);

            //Display flag victory jackpot?
            if (_flags.Count() > 0 && _server._zoneConfig.flag.useJackpot)
            {
                List<Team> flagTeams = new List<Team>();
                foreach (FlagState fs in _flags.Values)
                {
                    if (fs.bActive && fs.team != null)
                        flagTeams.Add(fs.team);
                }

                int _jackpot = (int)Math.Pow(PlayerCount, 2);
                sendArenaMessage("Victory Jackpot=" + _jackpot, _server._zoneConfig.flag.victoryBong);

                foreach (Player p in Players.ToList())
                {
                    if (p == null)
                        continue;
                    if (p.IsSpectator)
                        continue;

                    //Find the base reward jackpot
                    int personalJackpot;

                    if (flagTeams.Contains(p._team))
                        personalJackpot = _jackpot * (_server._zoneConfig.flag.winnerJackpotFixedPercent / 1000);
                    else
                        personalJackpot = _jackpot * (_server._zoneConfig.flag.loserJackpotFixedPercent / 1000);

                    //Obtain respective rewards
                    int cash = personalJackpot * (_server._zoneConfig.flag.cashReward / 1000);
                    int exp = personalJackpot * (_server._zoneConfig.flag.experienceReward / 1000);
                    int point = personalJackpot * (_server._zoneConfig.flag.pointReward / 1000);

                    p.sendMessage(0, String.Format("Your Personal Reward: Points={0} Cash={1} Experience={2}",
                        point, cash, exp));
                    p.Cash += cash;
                    p.Experience += exp;
                    p.BonusPoints += point;
                }
            }

            //Show a breakdown for each player in the arena
            foreach (Player p in Players.ToList())
            {
                if (p == null)
                    continue;

                individualBreakdown(p, bCurrent);
            }
        }
        #endregion

        #region Handlers

        #region handleBallPickup
        /// <summary>
        /// Triggered when a player requests to pick up a ball
        /// </summary>
        public override void handleBallPickup(Player from, CS_BallPickup update)
        {
            if (from == null)
            {
                Log.write(TLog.Warning, "handleBallPickup(): Called with null player.");
                return;
            }

            Ball ball = _balls.getObjByID(update.ballID);
            if (ball == null)
            {
                Log.write(TLog.Warning, "Player {0} tried picking up an invalid ball id.", from);
                return;
            }

            //Has the ball been dropped yet? 
            //NOTE: When 2 people run over the ball at the same time
            //the client sends this packet really fast trying to figure out who got it first. (you hear a click)
            //This gives it to the first person that touched the ball.
            if (ball.ballStatus != (int)Ball.BallStatus.Spawned
                && ball.ballStatus == (int)Ball.BallStatus.PickUp)
                return;

            //Is this player already carrying a ball?
            if (from._gotBallID != 999)
                return;

            //Forward to our script
            if (exists("Player.BallPickup") && !(bool)callsync("Player.BallPickup", false, from, ball))
                return;

            //Pick up the ball
            ball._lastOwner = ball._owner;
            ball._owner = from;

            from._gotBallID = ball._id;

            int now = Environment.TickCount;
            int updateTick = ((now >> 16) << 16) + (ball._state.lastUpdate & 0xFFFF);
            ball._state.lastUpdate = updateTick;
            ball._state.lastUpdateServer = now;

            ball._state.positionX = from._state.positionX;
            ball._state.positionY = from._state.positionY;
            ball._state.positionZ = from._state.positionZ;
            ball._state.velocityX = 0;
            ball._state.velocityY = 0;
            ball._state.velocityZ = 0;
            ball.tickCount = (uint)now;
            ball.ballStatus = 0;

            ball.deadBall = false;

            //Send ball coord updates to update spatial data
            _balls.updateObjState(ball, ball._state);

            //Route it
            Helpers.Object_Ball(from._arena.Players, ball);
        }
        #endregion

        #region handleBallDrop
        /// <summary>
        /// Triggered when a player requests to drop a ball
        /// </summary>
        public override void handleBallDrop(Player from, CS_BallDrop update)
        {
            if (from == null)
            {
                Log.write(TLog.Warning, "handleBallDrop(): Called with null player.");
                return;
            }

            Ball ball = _balls.getObjByID(update.ballID);
            if (ball == null)
            {
                Log.write(TLog.Warning, "Player {0} tried dropping an invalid ball id.", from);
                return;
            }

            //Forward to our script
            if (exists("Player.BallDrop") && !(bool)callsync("Player.BallDrop", false, from, ball, update))
                return;

            //Drop the ball
            ball._lastOwner = from;
            ball._owner = null;

            from._gotBallID = 999;

            int now = Environment.TickCount;
            int updateTick = ((now >> 16) << 16) + (ball._state.lastUpdate & 0xFFFF);
            ball._state.lastUpdate = updateTick;
            ball._state.lastUpdateServer = now;

            ball._state.positionX = update.positionX;
            ball._state.positionY = update.positionY;
            ball._state.positionZ = update.positionZ;
            ball._state.velocityX = update.velocityX;
            ball._state.velocityY = update.velocityY;
            ball._state.velocityZ = update.velocityZ;
            ball.tickCount = (uint)update.tickcount;
            ball.ballFriction = update.ballFriction;
            ball.ballStatus = 1;
            ball.deadBall = false;

            //Send ball coord updates to update spatial data
            _balls.updateObjState(ball, ball._state);

            //Route it
            Helpers.Object_Ball(from._arena.Players, ball);
        }
        #endregion

        #region handlePlayerGoal
        /// <summary>
        /// Triggered when a player has scored a goal
        /// </summary>
        public override void handlePlayerGoal(Player from, CS_GoalScored update)
        {
            if (from == null)
            {
                Log.write(TLog.Warning, "handlePlayerGoal(): Called with null player.");
                return;
            }

            Ball ball = _balls.getObjByID(update.ballID);
            if (ball == null)
            {
                Log.write(TLog.Warning, "Player {0} tried scoring an invalid ball id.", from);
                return;
            }

            //Forward to our script
            if (exists("Player.Goal") && !(bool)callsync("Player.Goal", false, from, ball, update))
                return;

            //Reset our variable then spawn a new ball
            from._gotBallID = 999;
            if (ball._owner != null)
                ball._owner._gotBallID = 999;
            if (ball._lastOwner != null)
                ball._lastOwner._gotBallID = 999;

            Ball.Spawn_Ball(from, ball);
        }
        #endregion

        #region handlePlayerPickup
        /// <summary>
        /// Triggered when a player requests to pick up an item
        /// </summary>
        public override void handlePlayerPickup(Player from, CS_PlayerPickup update)
        {
            //Find the itemdrop in question
            lock (_items)
            {
                ItemDrop drop;
                if (!_items.TryGetValue(update.itemID, out drop))
                    //Doesn't exist
                    return;

                //In range? 
                if (!Helpers.isInRange(_server._zoneConfig.arena.itemPickupDistance,
                                    drop.positionX, drop.positionY,
                                    from._state.positionX, from._state.positionY))
                    return;

                //Do we allow pickup?
                if (!_server._zoneConfig.level.allowUnqualifiedPickup && !Logic_Assets.SkillCheck(from, drop.item.skillLogic))
                    return;

                //Sanity checks
                if (update.quantity > drop.quantity)
                    return;

                //Forward to our script
                if (!exists("Player.ItemPickup") || (bool)callsync("Player.ItemPickup", false, from, drop, update.quantity))
                {
                    if (update.quantity == drop.quantity)
                    {	//Delete the drop
                        drop.quantity = 0;
                        _items.Remove(drop.id);
                    }
                    else
                        drop.quantity = (short)(drop.quantity - update.quantity);

                    //Add the pickup to inventory!
                    from.inventoryModify(drop.item, update.quantity);

                    //Update his bounty.
                    if (drop.owner != from) //Bug abuse fix for people dropping and picking up items to get bounty
                        from.Bounty += drop.item.prizeBountyPoints;

                    //Remove the item from player's clients
                    Helpers.Object_ItemDropUpdate(Players, update.itemID, (ushort)drop.quantity);
                }
            }
        }
        #endregion

        #region handlePlayerDrop
        /// <summary>
        /// Triggered when a player requests to drop an item
        /// </summary>
        public override void handlePlayerDrop(Player from, CS_PlayerDrop update)
        {	//Get the item into
            ItemInfo item = _server._assets.getItemByID(update.itemID);
            if (item == null)
            {
                Log.write(TLog.Warning, "Player requested to drop invalid item. {0}", from);
                return;
            }

            //Droppable?
            if (!item.droppable)
                return;

            //Is in range?
            if (!Helpers.isInRange(200,
                from._state.positionX, from._state.positionY,
                update.positionX, update.positionY))
                return;

            //For flag games, are we near a flag?
            if (_flags.Count > 0)
            {
                //Are we near the prize distance? 
                //Note: even if set as 0, prevents drop cheating by placing items on the flag
                foreach (FlagState fs in _flags.Values)
                {
                    if (Helpers.isInRange(_server._zoneConfig.flag.prizeDistance,
                        from._state.positionX, from._state.positionY, fs.posX, fs.posY))
                        return;
                }
            }

            //Forward to our script
            if (!exists("Player.ItemDrop") || (bool)callsync("Player.ItemDrop", false, from, item, update.quantity))
            {	//Update his inventory
                if (from.inventoryModify(item, -update.quantity))
                    //Create an item spawn
                    itemSpawn(item, update.quantity, update.positionX, update.positionY, 0, (int)from._team._id, from);
            }
        }
        #endregion

        #region handlePlayerPortal
        /// <summary>
        /// Handles a player's portal request
        /// </summary>
        public override void handlePlayerPortal(Player from, LioInfo.Portal portal)
        {	//Are we able to use this portal?
            if (!Logic_Assets.SkillCheck(from, portal.PortalData.SkillLogic))
                return;

            //Correct team?
            if (portal.PortalData.Frequency != -1 && portal.PortalData.Frequency != from._team._id)
                return;

            //Obtain the warp destination
            List<LioInfo.WarpField> warp = _server._assets.Lios.getWarpGroupByID(portal.PortalData.DestinationWarpGroup);
            if (warp == null)
            {	//Strange
                Log.write(TLog.Warning, "Failed portal {0}. Unconnected warpgroup #{1}", portal, portal.PortalData.DestinationWarpGroup);
                return;
            }

            //Forward to our script
            if (!exists("Player.Portal") || (bool)callsync("Player.Portal", false, from, portal))
            {
                //Is this a flag game?
                if (_flags.Count() > 0)
                {
                    List<Arena.FlagState> carried = _flags.Values.Where(flag => flag.carrier == from).ToList();
                    foreach (Arena.FlagState carry in carried)
                    {
                        int terrainNum = getTerrainID(from._state.positionX, from._state.positionY);
                        //Are we allowed on this terrain?
                        if (carry.flag.FlagData.FlagDroppableTerrains[terrainNum] == 0)
                            //Nope, reset them
                            flagResetPlayer(from);
                    }
                }

                //Do some warpage
                Logic_Lio.Warp(Helpers.ResetFlags.ResetNone, from, warp);
            }
        }
        #endregion

        #region handlePlayerProduce
        /// <summary>
        /// Handles a player's produce request
        /// </summary>
        public override void handlePlayerProduce(Player from, ushort computerVehID, ushort produceItem)
        {	//Make sure the item index is sensible
            if (produceItem > 15)
            {
                Log.write(TLog.Warning, "Player {0} attempted to produce item ({0}) > 15.", from, produceItem);
                return;
            }

            //Get the associated vehicle
            Vehicle vehicle;

            if ((vehicle = _vehicles.getObjByID(computerVehID)) == null)
            {
                Log.write(TLog.Warning, "Player {0} attempted to produce item ({0}) using invalid vehicle ({1}).", from, computerVehID, produceItem);
                return;
            }

            Computer computer = vehicle as Computer;
            if (computer == null)
            {
                Log.write(TLog.Warning, "Player {0} attempted to produce item ({0}) using non-computer vehicle ({1}).", from, computerVehID, produceItem);
                return;
            }

            //It must be in range
            if (!Helpers.isInRange(_server._zoneConfig.vehicle.computerProduceRadius,
                                    computer._state.positionX, computer._state.positionY,
                                    from._state.positionX, from._state.positionY))
                return;

            //Can't produce from dead or non-computer vehicles
            if (computer.IsDead || computer._type.Type != VehInfo.Types.Computer)
                return;

            //Vehicle looks fine, find the produce item involved
            VehInfo.Computer computerInfo = (VehInfo.Computer)computer._type;
            VehInfo.Computer.ComputerProduct product = computerInfo.Products[produceItem];

            //Quick check to make sure it isn't blank
            if (String.IsNullOrWhiteSpace(product.Title))
                return;

            //Lets check limits
            if (product.ProductToCreate < 0) //Negative are vehicles, positive are items
            {
                VehInfo vehInfo = from._server._assets.getVehicleByID(-product.ProductToCreate);
                if (vehInfo == null)
                {
                    Log.write(TLog.Error, "Produce item {0} referenced invalid vehicle id #{1}", product.Title, -product.ProductToCreate);
                    return;
                }

                if (vehInfo.Type == VehInfo.Types.Computer)
                {
                    //This is a vehicle item
                    int densityType = 0;
                    int densityAmount = 0;
                    int totalAmount = 0;
                    int totalType = 0;
                    int playerTotal = 0;
                    int totalTempAmount = 0;

                    playerTotal = from._arena.Vehicles.Where(v => v != null && v._type.Id == vehInfo.Id && v._creator != null
                        && v._creator._alias == from._alias).Count();

                    //Lets set our product as a computer
                    VehInfo.Computer vehComp = vehInfo as VehInfo.Computer;
                    if (vehComp == null)
                    {
                        Log.write(TLog.Error, "Produce item {0} AS vehinfo.computer produced invalid vehicle. ID = {1}", product.Title, vehInfo.Id);
                        return;
                    }

                    IEnumerable<Vehicle> vehs = from._arena.Vehicles;
                    foreach (Vehicle ve in vehs)
                    {
                        if (ve == null)
                            continue;

                        Computer comp = ve as Computer;
                        if (comp != null)
                        {
                            if (comp._reprogrammed && comp._team == from._team)
                                totalTempAmount++;
                            if (!comp._reprogrammed && comp._team == from._team)
                            {
                                totalAmount++;
                                if (comp._type.Name == vehComp.Name)
                                    totalType++;
                            }
                        }
                    }

                    List<Vehicle> vehicles = from._arena.getVehiclesInRange(from._state.positionX, from._state.positionY, vehComp.DensityRadius);
                    foreach (Vehicle veh in vehicles)
                    {
                        Computer comp = veh as Computer;
                        if (comp != null && !comp._reprogrammed && comp._team == from._team)
                        {
                            densityAmount++;
                            if (comp._type.Name == vehComp.Name)
                                densityType++;
                        }
                    }

                    if (vehComp.MaxTypeByPlayerRegardlessOfTeam != -1 && playerTotal >= vehComp.MaxTypeByPlayerRegardlessOfTeam)
                    {   //Exceeds the amount per player regardless of team.
                        from.sendMessage(-1, "Your team has the maximum allowed computer vehicles of this type.");
                        return;
                    }

                    if (vehComp.FrequencyMaxActive != -1 && (totalAmount >= vehComp.FrequencyMaxActive || totalTempAmount >= vehComp.FrequencyMaxActive))
                    {   //Exceeds the total amount of active computer vehicles for the team
                        from.sendMessage(-1, "Your team already has the maximum allowed computer vehicles.");
                        return;
                    }

                    if (vehComp.FrequencyMaxType != -1 && totalType >= vehComp.FrequencyMaxType)
                    {   //Exceeds the total amount of computer vehicles of this type for the team
                        from.sendMessage(-1, "Your team already has the maximum allowed computer vehicles of this type.");
                        return;
                    }

                    if (vehComp.FrequencyDensityMaxActive != -1 && densityAmount >= vehComp.FrequencyDensityMaxActive)
                    {   //Exceeds the total amount of computer vehicles for the team in the area
                        from.sendMessage(-1, "Your team already has the maximum allowed computer vehicles in the area.");
                        return;
                    }

                    if (vehComp.FrequencyDensityMaxType != -1 && densityType >= vehComp.FrequencyDensityMaxType)
                    {   //Exceeds the amount within the density radius for the specific type
                        from.sendMessage(-1, "Your team already has the maximum allowed computer vehicles of this type in the area.");
                        return;
                    }
                }
            }

            //Forward to our script
            if (!exists("Player.Produce") || (bool)callsync("Player.Produce", false, from, computer, product))
            {	//Make a produce request
                produceRequest(from, computer, product);
            }
        }
        #endregion

        #region handlePlayerSwitch
        /// <summary>
        /// Handles a player's switch request
        /// </summary>
        public override void handlePlayerSwitch(Player from, bool bOpen, LioInfo.Switch swi)
        {	//Forward to our script
            if (!exists("Player.Switch") || (bool)callsync("Player.Switch", false, from, swi))
            {	//Make a switch request
                switchRequest(false, bOpen, from, swi);
            }
        }
        #endregion

        #region handlePlayerFlag
        /// <summary>
        /// Handles a player's flag request
        /// </summary>
        public override void handlePlayerFlag(Player from, bool bPickup, bool bInPlace, LioInfo.Flag flag)
        {	//Forward to our script
            if (!exists("Player.FlagAction") || (bool)callsync("Player.FlagAction", false, from, bPickup, bInPlace, flag))
            {	//Make a flag request
                flagAction(false, bPickup, bInPlace, from, flag);
            }
        }
        #endregion

        #region handlePlayerSpawn
        /// <summary>
        /// Handles the spawn of a player
        /// </summary>
        public override void handlePlayerSpawn(Player from, bool bDeath)
        {	//Forward to our script
            if (!exists("Player.Spawn") || (bool)callsync("Player.Spawn", false, from, bDeath))
            {	//Did he die?
                if (bDeath)
                {	//Trigger the appropriate event
                    if (from._bEnemyDeath)
                        Logic_Assets.RunEvent(from, _server._zoneConfig.EventInfo.killedByEnemy);
                    else
                        Logic_Assets.RunEvent(from, _server._zoneConfig.EventInfo.killedByTeam);

                    //Reset flags to unowned state?
                    if (from._arena.getTerrain(from._state.positionX, from._state.positionY).safety
                        && !_server._zoneConfig.flag.allowSafety)
                        flagResetPlayer(from, true);

                    //Reset his bounty
                    from.Bounty = _server._zoneConfig.bounty.start;
                    //Update his client to reflect bty change
                    from.syncState();
                }
            }
        }
        #endregion

        #region handlePlayerJoin
        /// <summary>
        /// Triggered when a player wants to spec or unspec
        /// </summary>
        public override void handlePlayerJoin(Player from, bool bSpec)
        {
            if (from == null)
            {
                Log.write(TLog.Warning, "handlePlayerJoin(): Called with null player");
                return;
            }

            //Let them!
            if (bSpec)
            {	//Forward to our script
                if (!exists("Player.LeaveGame") || (bool)callsync("Player.LeaveGame", false, from))
                {	//The player has effectively left the game

                }
                from.removeBall();
                from.spec();
            }
            else
            {
                //Are we locked in spec?
                if (from._bLocked || from._arena._bLocked)
                {
                    from.sendMessage(-1, "You are locked in spectator mode.");
                    return;
                }

                //Do we have a full arena?
                if (PlayerCount >= _server._zoneConfig.arena.playingMax)
                {
                    if (!_scriptType.Equals("GameType_SoccerBrawl", StringComparison.OrdinalIgnoreCase)
                    && !_scriptType.Equals("GameType_Gravball", StringComparison.OrdinalIgnoreCase)
                    && !_scriptType.Equals("GameType_BasketBall", StringComparison.OrdinalIgnoreCase)
                    && !_scriptType.Equals("GameType_BoomBall", StringComparison.OrdinalIgnoreCase)) //Cheat fix for the queue system(Reversed this back so the queue system can work - Mizz)
                    {	//Yep, tell him why he can't get in
                        from.sendMessage(255, "Game is full.");
                        return;
                    }
                }

                //Is he able to unspec?
                //Check spectator logic and check any requirements
                foreach (Player.SkillItem skill in from._skills.Values)
                {
                    if (skill.skill.SkillId > 0 &&
                        !Logic_Assets.AllowedClassCheck(from, skill.skill, from._server._zoneConfig.arena.exitSpectatorLogic))
                    {
                        if (!string.IsNullOrWhiteSpace(_server._zoneConfig.arena.exitSpectatorMessage))
                            //Use logic message
                            from.sendMessage(-1, _server._zoneConfig.arena.exitSpectatorMessage);
                        else
                            from.sendMessage(-1, "That is not an eligible class to play.");
                        return;
                    }

                    /* Testing - this might not be needed
                    //Why was this checking attributes? Client handles all of that bizness
                    if (skill.skill.SkillId > 0 && !Logic_Assets.SkillCheck(from, skill.skill.Logic))
                    {
                        from.sendMessage(-1, "You do not have the requirements to play this class.");
                        return;
                    }*/
                }

                //Does he have a high p-loss or ping?
                Client.ConnectionStats pStats = from._client._stats;
                /* if (pStats.C2SPacketLoss > 3.50f)
                 {
                     from.sendMessage(-1, "Your packet loss is too high to enter.");
                     return;
                 }
                 */
                if (pStats.clientAverageUpdate > 600)
                {
                    from.sendMessage(-1, "Your ping is too high to enter.");
                    return;
                }

                //Forward to our script
                if (exists("Player.JoinGame") && !(bool)callsync("Player.JoinGame", false, from))
                {
                    return;
                }

                //Pick a team
                Team pick = pickAppropriateTeam(from);
                if (pick != null)
                {
                    //Great, use it
                    from.unspec(pick);
                    from._lastMovement = Environment.TickCount;
                    from._maxTimeCalled = false;
                }
                else
                    from.sendMessage(-1, "Unable to pick a team.");
            }
        }
        #endregion

        #region handlePlayerEnterVehicle
        /// <summary>
        /// Triggered when a player wants to enter a vehicle
        /// </summary>
        public override void handlePlayerEnterVehicle(Player from, bool bEnter, ushort vehicleID)
        {
            int now = Environment.TickCount;

            //Are we trying to leave our current vehicle?
            if (!bEnter)
            {	//Forward to our script
                if (!exists("Player.LeaveVehicle") || (bool)callsync("Player.LeaveVehicle", false, from, from._occupiedVehicle))
                {   //Let's leave it!
                    from._occupiedVehicle.playerLeave(true);
                    //Warp the player away from the vehicle to keep him from getting "stuck"
                    Random exitRadius = new Random();
                    from.warp(from._state.positionX + exitRadius.Next(-_server._zoneConfig.arena.vehicleExitWarpRadius, _server._zoneConfig.arena.vehicleExitWarpRadius),
                    from._state.positionY + exitRadius.Next(-_server._zoneConfig.arena.vehicleExitWarpRadius, _server._zoneConfig.arena.vehicleExitWarpRadius));
                    from._lastVehicleEntry = Environment.TickCount;
                }

                return;
            }

            //Otherwise, do we have such a vehicle?
            Vehicle entry;

            //Check warpGetInDelay
            int delay = (_server._zoneConfig.vehicle.warpGetInDelay / 60) * 1000;
            if ((now - from._lastVehicleEntry) < delay)
                return;

            if ((entry = _vehicles.getObjByID(vehicleID)) == null)
            {
                Log.write(TLog.Warning, "Player {0} attempted to enter invalid vehicle.", from);
                return;
            }

            //It must be in range
            if (!Helpers.isInRange(_server._zoneConfig.arena.vehicleGetInDistance,
                                    entry._state.positionX, entry._state.positionY,
                                    from._state.positionX, from._state.positionY))
                return;

            //Can't enter dead vehicles
            if (entry.IsDead)
                return;

            //Forward to our script
            if (!exists("Player.EnterVehicle") || (bool)callsync("Player.EnterVehicle", false, from, entry))
            {
                //Attempt to enter the vehicle!
                if (!from.enterVehicle(entry))
                {
                    Log.write(TLog.Warning, "Player {0} failed to enter vehicle ({1}).", from, vehicleID);
                    return;
                }

                //Update our last entry/exit
                from._lastVehicleEntry = Environment.TickCount;
            }
        }
        #endregion

        #region handlePlayerExplosion
        /// <summary>
        /// Triggered when a player notifies the server of an explosion
        /// </summary>
        public override void handlePlayerExplosion(Player from, CS_Explosion update)
        {	//Damage any computer vehicles (future, also bots) in the blast radius
            if (from == null)
            {
                Log.write(TLog.Error, "handlePlayerExplosion(): Called with null player.");
                return;
            }

            ItemInfo.Projectile usedWep = Helpers._server._assets.getItemByID(update.explosionID) as ItemInfo.Projectile;
            if (usedWep == null)
            {	//All things that explode should be projectiles. But just in case...
                Log.write(TLog.Warning, "Player {0} fired unsupported weapon id {0}", from, update.explosionID);
                return;
            }

            //Forward to our script
            if (!exists("Player.Explosion") || (bool)callsync("Player.Explosion", false, from, usedWep, update.positionX, update.positionY, update.positionZ))
            {	//Find the largest blast radius of damage types of this weapon
                int maxDamageRadius = Helpers.getMaxBlastRadius(usedWep);

                List<Vehicle> vechs = _vehicles.getObjsInRange(update.positionX, update.positionY, maxDamageRadius + 500);
                //Notify all vehicles in the vicinity
                foreach (Vehicle v in vechs)
                    if (!v.IsDead)
                        v.applyExplosion(from, update.positionX, update.positionY, usedWep);
            }
        }
        #endregion

        #region handlePlayerUpdate
        /// <summary>
        /// Triggered when a player has sent an update packet
        /// </summary>
        public override void handlePlayerUpdate(Player from, CS_PlayerUpdate update)
        {	//Should we ignore this?
            if (update.bIgnored)
                return;

            int now = Environment.TickCount;
            //Is it firing an item?
            if (update.itemID != 0)
            {	//Let's inspect this action a little closer
                ItemInfo info = _server._assets.getItemByID(update.itemID);
                if (info == null)
                {
                    Log.write(TLog.Warning, "Player {0} attempted to fire non-existent item.", from);
                    return;
                }

                //Does he have it?
                Player.InventoryItem ii = from.getInventory(info);
                if (ii == null)
                {	//Is it a default item?
                    if (!from.ActiveVehicle._type.InventoryItems.Any(item => item == update.itemID))
                    {
                        Log.write(TLog.Warning, "Player {0} attempted to fire unowned item '{1}'.", from, info.name);
                        return;
                    }
                }

                //And does he have the appropriate skills?
                if (!Logic_Assets.SkillCheck(from, info.skillLogic))
                {
                    Log.write(TLog.Warning, "Player {0} attempted unqualified use of item '{1}'.", from, info.name);
                    return;
                }

                //Check timings
                /*if (update.itemID != 0 && from._lastItemUse != 0 && from._lastItemUseID == update.itemID)
                {
                    if (info.itemType == ItemInfo.ItemType.Projectile)
                    {	//Is it nicely timed?
                        ItemInfo.Projectile proj = (ItemInfo.Projectile)info;
                        if (update.tickCount - from._lastItemUse < (proj.fireDelay - (proj.fireDelay / 2) + (proj.fireDelay / 4)) &&
                            update.tickCount - from._lastItemUse < (proj.fireDelayOther - (proj.fireDelayOther / 2) + (proj.fireDelayOther / 4)))
                        {
                            update.itemID = 0;
                            Log.write(TLog.Warning, "Player {0} had a suspicious reload timer.", from);
                            triggerMessage(2, 1500, from._alias + " was kicked for knobbery.");
                            from.disconnect();
                            return;
                        }
                    }
                    else if (info.itemType == ItemInfo.ItemType.MultiUse)
                    {	//Is it nicely timed?
                        ItemInfo.MultiUse multi = (ItemInfo.MultiUse)info;
                        if (update.tickCount - from._lastItemUse < (multi.fireDelay - (multi.fireDelay / 2) + (multi.fireDelay / 4)) &&
                            update.tickCount - from._lastItemUse < (multi.fireDelayOther - (multi.fireDelayOther / 2) + (multi.fireDelayOther / 4)))
                        {	//Kick the fucker
                            Log.write(TLog.Warning, "Player {0} had a suspicious reload timer.", from);
                            triggerMessage(2, 1500, from._alias + " was kicked for knobbery.");
                            from.disconnect();
                            return;
                        }
                    }
                }*/

                from._lastItemUseID = update.itemID;
                if (update.itemID != 0)
                    from._lastItemUse = update.tickCount;

                //We should be good. Check for ammo
                int ammoType;
                int ammoCount;

                if (info.getAmmoType(out ammoType, out ammoCount))
                    if (ammoType != 0 && !from.inventoryModify(false, ammoType, -ammoCount))
                        update.itemID = 0;

                //Update last movement with firing, don't want to spec people in stationary turrets;
                from._lastMovement = now;
                from._maxTimeCalled = false;
            }


            //Update the player's active equipment
            from.updateActiveEquip(update.activeEquip);

            //Update the player's state
            now = Environment.TickCount;
            int updateTick = ((now >> 16) << 16) + (update.tickCount & 0xFFFF);
            int oldPosX = from._state.positionX, oldPosY = from._state.positionY;

            from._state.energy = update.energy;

            from._state.velocityX = update.velocityX;
            from._state.velocityY = update.velocityY;
            from._state.velocityZ = update.velocityZ;

            //Update the lastmovement tick if his position state has changed
            if (from._state.positionX != update.positionX | from._state.positionY != update.positionY | from._state.positionZ != update.positionZ)
            {
                from._lastMovement = now;
                from._maxTimeCalled = false;
            }
            from._state.positionX = update.positionX;
            from._state.positionY = update.positionY;
            from._state.positionZ = update.positionZ;

            from._state.yaw = update.yaw;
            from._state.direction = (Helpers.ObjectState.Direction)update.direction;
            from._state.unk1 = update.unk1;
            from._state.pitch = update.pitch;

            from._state.lastUpdate = updateTick;
            from._state.lastUpdateServer = now;

            //If the player is inside a vehicle..
            if (from._occupiedVehicle != null)
            {
                //Update the vehicle state too..
                from._occupiedVehicle._state.health = update.health;

                from._occupiedVehicle._state.positionX = update.positionX;
                from._occupiedVehicle._state.positionY = update.positionY;
                from._occupiedVehicle._state.positionZ = update.positionZ;

                from._occupiedVehicle._state.velocityX = update.velocityX;
                from._occupiedVehicle._state.velocityY = update.velocityY;
                from._occupiedVehicle._state.velocityZ = update.velocityZ;

                from._occupiedVehicle._state.yaw = update.yaw;
                from._occupiedVehicle._state.direction = (Helpers.ObjectState.Direction)update.direction;
                from._occupiedVehicle._state.unk1 = update.unk1;
                from._occupiedVehicle._state.pitch = update.pitch;

                from._occupiedVehicle._state.lastUpdate = updateTick;

                //
                // Force an angle lock, more work may be needed here, because
                // we may need to keep track of a child's yaw as a separate value
                // from the parent so that we can add the two of them when needed.
                //
                // For now we only care about what is called an "absolute constraint".
                // This means that the child vehicle cannot rotate at all, so that it
                // _always_ faces the exactly same angle of the parent vehicle.
                //
                // Later on, we will implement variable angle constraints as well.
                //

                var child = from._occupiedVehicle;

                if (child._parent != null)
                {
                    var dep = child._type as VehInfo.Dependent;

                    if (dep != null)
                    {
                        //
                        // Force an angle lock, more work may be needed here, because
                        // we may need to keep track of a child's yaw as a separate value
                        // from the parent so that we can add the two of them when needed.
                        //
                        // For now we only care about what is called an "absolute constraint".
                        // This means that the child vehicle cannot rotate at all, so that it
                        // _always_ faces the exactly same angle of the parent vehicle.
                        //
                        // Later on, we will implement variable angle constraints as well.
                        //

                        var absoluteConstraint = dep.ChildAngleLength == 0
                            && dep.ChildAngleStart == 0
                            && dep.ChildRotateLeft == 0
                            && dep.ChildRotateRight == 0;

                        if (dep.ChildParentRelativeRotation == 1)
                        {
                            var _state = child._parent._state;

                            child._state.positionX = _state.positionX;
                            child._state.positionY = _state.positionY;
                            child._state.positionZ = _state.positionZ;

                            child._state.velocityX = _state.velocityX;
                            child._state.velocityY = _state.velocityY;
                            child._state.velocityZ = _state.velocityZ;

                            child._state.yaw = _state.yaw;
                            child._state.pitch = _state.pitch;
                        }
                    }
                }

                //Update spatial data
                _vehicles.updateObjState(from._occupiedVehicle, from._occupiedVehicle._state);


                //Propagate the state
                from._occupiedVehicle.propagateState();
            }
            else
            {
                from._state.health = update.health;
            }

            //Send player coord updates to update spatial data
            _players.updateObjState(from, from._state);
            if (!from._bSpectator)
                _playersIngame.updateObjState(from, from._state);

            //If it's a spectator, we should not route
            if (from.IsSpectator)
            {	//Are we still spectating a player?
                if (update.playerSpectating == -1 && from._spectating != null)
                {
                    from._spectating._spectators.Remove(from);
                    from._spectating = null;
                }

                return;
            }

            //Route it to all players!
            from._state.updateNumber++;

            Helpers.Update_RoutePlayer(from, update, updateTick, oldPosX, oldPosY);
        }
        #endregion

        #region handlePlayerDeath
        /// <summary>
        /// Triggered when a player has sent a death packet
        /// </summary>
        public override void handlePlayerDeath(Player from, CS_VehicleDeath update)
        {	//Store variables to pass to the event at the end
            Player killer = null;

            //Was it a player kill?
            if (update.type == Helpers.KillType.Player)
            {	//Sanity checks
                killer = _players.getObjByID((ushort)update.killerPlayerID);

                //Was it a player we can't find?
                if (update.killerPlayerID < 5001 && killer == null)
                    Log.write(TLog.Warning, "Player {0} gave invalid player killer ID.", from);
            }

            //Fall out of our vehicle and die!
            if (from._occupiedVehicle != null)
            {
                //Was it us that died?
                if (update.killedID != from._id)
                {
                    //Was it a vehicle we were in?
                    if (update.killedID == from._occupiedVehicle._id)
                    {
                        //Yes, fall out of the vehicle
                        from._occupiedVehicle.kill(killer);
                    }
                }
                else
                    from._occupiedVehicle._tickDead = Environment.TickCount;
                from._occupiedVehicle.playerLeave(true);
            }

            //Mark him as dead!
            from._bEnemyDeath = true;
            from._deathTime = Environment.TickCount;

            //Do we have any items to prune/drop?
            Dictionary<ItemInfo, int> pruneList = Logic_Assets.pruneItems(from);
            if (pruneList != null)
            {
                //Yes, drop them on the ground
                foreach (var item in pruneList)
                {
                    itemSpawn(item.Key, (ushort)item.Value, from._state.positionX,
                        from._state.positionY, (short)_server._zoneConfig.arena.pruneDropRadius, from);
                    //Now remove them from the player's inventory
                    from.inventoryModify(item.Key, -item.Value);
                }
            }

            //Innate Drop items?
            VehInfo vehicle = _server._assets.getVehicleByID(from._baseVehicle._type.Id);
            if (vehicle.DropItemId != 0)
            {
                ItemInfo item = _server._assets.getItemByID(vehicle.DropItemId);
                if (item != null && vehicle.DropItemQuantity > 0)
                    itemSpawn(item, (ushort)vehicle.DropItemQuantity, from._state.positionX, from._state.positionY, from);
            }

            //Prompt the player death event
            if (exists("Player.Death") && !(bool)callsync("Player.Death", false, from, killer, update.type, update))
                return;

            //Was it a player kill?
            if (update.type == Helpers.KillType.Player)
            {	//Sanity checks
                killer = _players.getObjByID((ushort)update.killerPlayerID);

                //Was it a player?
                if (update.killerPlayerID < 5001)
                {
                    if (killer == null)
                    {
                        Log.write(TLog.Warning, "Player {0} gave invalid killer ID.", from);
                        return;
                    }

                    //Forward to our script
                    if (!exists("Player.PlayerKill") || (bool)callsync("Player.PlayerKill", false, from, killer))
                    {	//Handle any flags
                        flagHandleDeath(from, killer);

                        //Was the killer dead? (Death nade or other)
                        if (killer.IsDead)
                            flagResetPlayer(killer);

                        //Handle any ball action
                        ballResetPlayer(from, killer);

                        //Don't reward for teamkills
                        if (from._team == killer._team)
                            Logic_Assets.RunEvent(from, _server._zoneConfig.EventInfo.killedTeam);
                        else
                            Logic_Assets.RunEvent(from, _server._zoneConfig.EventInfo.killedEnemy);

                        //Calculate rewards
                        Logic_Rewards.calculatePlayerKillRewards(from, killer, update);

                        //Update normally
                        killer.Kills++;
                        from.Deaths++;
                    }

                    return;
                }
            }

            //Reset any flags held
            flagResetPlayer(from);

            //Handle any ball action
            ballResetPlayer(from);

            //Was it a bot kill?
            if (update.type == Helpers.KillType.Player && update.killerPlayerID >= 5001)
            {	//Attempt to find the associated bot
                Bots.Bot bot = _vehicles.getObjByID((ushort)update.killerPlayerID) as Bots.Bot;

                //Note: bot can be null, for when a player is killed by the bot's projectiles after the bot is dead

                //Forward to our script
                if (!exists("Player.BotKill") || (bool)callsync("Player.BotKill", false, from, bot))
                {	//Update stats
                    //from.Deaths++;

                    //Spoof first then route only. Bots dont need rewards
                    update.type = Helpers.KillType.Computer;
                    Helpers.Player_RouteKill(Players, update, from, 0, 0, 0, 0);
                }
            }
            else
            {	//If he was killed by a computer vehicle..
                if (update.type == Helpers.KillType.Computer)
                {	//Get the related vehicle
                    Computer cvehicle = _vehicles.getObjByID((ushort)update.killerPlayerID) as Computer;
                    if (cvehicle == null)
                    {
                        Log.write(TLog.Warning, "Player {0} was killed by unidentifiable computer vehicle.", from);
                        return;
                    }

                    //Forward to our script
                    if (!exists("Player.ComputerKill") || (bool)callsync("Player.ComputerKill", false, from, cvehicle))
                    {	//Update stats
                        from.Deaths++;

                        //Route and give rewards to owner
                        Logic_Rewards.calculateTurretKillRewards(from, cvehicle, update);
                    }
                }
                else
                {	//He was killed by another phenomenon, simply
                    //route the kill packet to all players.
                    Helpers.Player_RouteKill(Players, update, from, 0, 0, 0, 0);
                }
            }
        }
        #endregion

        #region handlePlayerShop
        /// <summary>
        /// Triggered when a player attempts to use the store
        /// </summary>
        public override void handlePlayerShop(Player from, ItemInfo item, int quantity)
        {
            //Get the player's related inventory item
            Player.InventoryItem ii = from.getInventory(item);

            //Are we buying or selling?
            if (quantity > 0)
            {
                //Do we have the skills required (if we're buying)
                if (!Logic_Assets.SkillCheck(from, item.skillLogic))
                    return;

                //Buying. Are we able to?
                if (item.buyPrice == 0)
                    return;

                //Check limits
                if (item.maxAllowed != 0)
                {
                    int constraint = Math.Abs(item.maxAllowed) - ((ii == null) ? (ushort)0 : ii.quantity);
                    if (quantity > constraint)
                        return;
                }

                //Good to go, calculate the price
                int price = item.buyPrice * quantity;

                //Do we have enough?
                if (price > from.Cash)
                    return;

                //Forward to our script
                if (!exists("Shop.Buy") || (bool)callsync("Shop.Buy", false, from, item, quantity))
                {	//Perform the transaction!
                    from.Cash -= price;
                    from.inventoryModify(item, quantity);
                }
            }
            else
            {	//Sellable?
                if (item.sellPrice == -1)
                    return;
                else if (ii == null)
                    return;

                //Do we have enough items?
                if (quantity > ii.quantity)
                    return;

                //Calculate the price
                int price = item.sellPrice * quantity;

                //Forward to our script
                if (!exists("Shop.Sell") || (bool)callsync("Shop.Sell", false, from, item, -quantity))
                {	//Perform the transaction!
                    from.Cash -= price; //We use a negative because quantity is negative
                    from.inventoryModify(item, quantity);
                }
            }
        }
        #endregion

        #region handlePlayerShopSkill
        /// <summary>
        /// Triggered when a player attempts to use the skill shop
        /// </summary>
        public override void handlePlayerShopSkill(Player from, SkillInfo skill)
        {
            //Are we allowed to buy skills from spec?
            if (from._bSpectator && !_server._zoneConfig.arena.spectatorSkills)
            {
                from.sendMessage(-1, "Unable to buy skills from spec.");
                return;
            }

            //Are we able to pick these classes?
            //Only want classes, attributes is checked farther down
            if (skill.SkillId > 0 &&
                !Logic_Assets.AllowedClassCheck(from, skill, from._server._zoneConfig.arena.exitSpectatorLogic))
            {
                if (!string.IsNullOrWhiteSpace(from._server._zoneConfig.arena.exitSpectatorMessage))
                    //Use logic message
                    from.sendMessage(-1, from._server._zoneConfig.arena.exitSpectatorMessage);
                else
                    from.sendMessage(-1, "That is not an eligible class to play.");
                return;
            }

            //Are we in a vehicle that we may no longer be able to use?
            if (!from._bSpectator && !_server._zoneConfig.arena.allowSkillPurchaseInVehicle && from._occupiedVehicle != null)
            {
                from.sendMessage(-1, "Unable to buy skills while occupying a vehicle.");
                return;
            }

            //Do we have the skills required for this?
            if (!Logic_Assets.SkillCheck(from, skill.Logic))
            {
                Log.write(TLog.Warning, "Player {0} attempted to buy invalid skill '{1}'", from, skill.Name);
                return;
            }

            //Make sure it's okay with our script...
            if (!exists("Shop.SkillRequest") || (bool)callsync("Shop.SkillRequest", false, from, skill))
                //Perform the skill modify
                if (from.skillModify(skill, 1))
                    //Success! Forward to our script
                    callsync("Shop.SkillPurchase", false, from, skill);
        }
        #endregion

        #region handlePlayerWarp
        /// <summary>
        /// Triggered when a player attempts to use a warp item
        /// </summary>
        public override void handlePlayerWarp(Player player, ItemInfo.WarpItem item, ushort targetPlayerID, short posX, short posY)
        {	//Is this warp being prevented by a bot?
            Bot antiWarpBot = checkBotAntiwarp(player);
            if (antiWarpBot != null)
            {
                player.sendMessage(-1, "You are being antiwarped by a " + antiWarpBot._type.Name);
                return;
            }

            //By a computer vehicle?
            Computer antiWarpVeh = checkVehAntiWarp(player);
            if (antiWarpVeh != null)
            {
                player.sendMessage(-1, "You are being antiwarped by a " + antiWarpVeh._type.Name);
                return;
            }

            //How about a player?
            Player antiWarp = player.checkAntiWarp();
            if (antiWarp != null)
            {
                player.sendMessage(-1, String.Format("You are being antiwarped by {0}", antiWarp._alias));
                return;
            }

            //Are we currently using another item while warping?
            //ItemInfo info = _server._assets.getItemByID(item.id);
            //if (info != null && player._lastItemUse != 0 && info.id != player._lastItemUseID)
            //return;

            //What sort of warp item are we dealing with?
            switch (item.warpMode)
            {
                case ItemInfo.WarpItem.WarpMode.RandomWarp:
                    {
                        //Are we warpable?
                        if (!player.ActiveVehicle._type.IsWarpable)
                            return;

                        //Are we dead?
                        if (player.IsDead)
                            return;

                        //Forward to our script
                        if (exists("Player.WarpItem") && !(bool)callsync("Player.WarpItem", false, player, item, targetPlayerID, posX, posY))
                            return;

                        if (item.areaEffectRadius > 0)
                        {
                            foreach (Player p in getPlayersInRange(posX, posY, item.areaEffectRadius))
                            {
                                //Is he dead, warpable, or ignoring summons?
                                if (!p.IsDead && p.ActiveVehicle._type.IsWarpable 
                                    && !p._summonIgnore.Contains(player._alias) && !p._summonIgnore.Contains("*"))
                                {
                                    LvlInfo level = player._server._assets.Level;

                                    int x = level.OffsetX * 16;
                                    int y = level.OffsetY * 16;
                                    short height = player._state.positionY;
                                    short width = player._state.positionX;

                                    //Check for an available spot
                                    //This fixes warping onto physics
                                    int attempts = 0;
                                    for (; attempts < 10; attempts++)
                                    {
                                        short px = (short)x;
                                        short py = (short)y;
                                        if (!player._arena.getTile(px, py).Blocked)
                                            break;

                                        Helpers.randomPositionInArea(player._arena, ref px, ref py, width, height);
                                    }

                                    //Use our first warp!
                                    p.warp(Helpers.ResetFlags.ResetNone,
                                        (short)-1,
                                        (short)(x - width), (short)(y - height),
                                        (short)(x + width), (short)(y + height),
                                        0);
                                }
                            }
                        }
                        else
                        {
                            LvlInfo level = player._server._assets.Level;

                            int x = level.OffsetX * 16;
                            int y = level.OffsetY * 16;
                            short height = player._state.positionY;
                            short width = player._state.positionX;

                            //Check for an available spot
                            //This fixes warping onto physics
                            int attempts = 0;
                            for (; attempts < 10; attempts++)
                            {
                                short px = (short)x;
                                short py = (short)y;
                                if (!player._arena.getTile(px, py).Blocked)
                                    break;

                                Helpers.randomPositionInArea(player._arena, ref px, ref py, width, height);
                            }

                            //Use our first warp!
                            player.warp(Helpers.ResetFlags.ResetNone,
                                (short)-1,
                                (short)(x - width), (short)(y - height),
                                (short)(x + width), (short)(y + height),
                                0);
                        }
                    }
                    break;

                case ItemInfo.WarpItem.WarpMode.Lio:
                    {
                        //Are we warpable?
                        if (!player.ActiveVehicle._type.IsWarpable)
                            return;

                        //Are we dead?
                        if (player.IsDead)
                            return;

                        //Forward to our script
                        if (exists("Player.WarpItem") && !(bool)callsync("Player.WarpItem", false, player, item, targetPlayerID, posX, posY))
                            return;

                        //A simple lio warp. Get the associated warpgroup
                        List<LioInfo.WarpField> warps = _server._assets.Lios.getWarpGroupByID(item.warpGroup);
                        if (warps == null)
                        {
                            Log.write(TLog.Error, "Item: {0}. Warp group '{1}' doesn't exist.", item.name, item.warpGroup);
                            break;
                        }

                        //Set the id for abuse checking later
                        player._lastWarpItemUseID = item.id;
                        player._lastWarpItemUse = Environment.TickCount;

                        //Warp the player
                        Logic_Lio.Warp(Helpers.ResetFlags.ResetNone, player, warps);
                    }
                    break;

                case ItemInfo.WarpItem.WarpMode.WarpTeam:
                    {	//Are we warpable?
                        if (!player.ActiveVehicle._type.IsWarpable)
                            return;

                        //Find the player in question
                        Player target = _playersIngame.getObjByID(targetPlayerID);
                        if (target == null)
                            return;

                        //Can't warp to dead people
                        if (target.IsDead)
                        {
                            player.sendMessage(0xFF, "The player you are trying to warp to is dead.");
                            return;
                        }

                        //Is he on the correct team?
                        if (target._team != player._team)
                            return;

                        //Forward to our script
                        if (exists("Player.WarpItem") && !(bool)callsync("Player.WarpItem", false, player, item, targetPlayerID, posX, posY))
                            return;

                        if (item.areaEffectRadius > 0)
                        {
                            foreach (Player p in getPlayersInRange(posX, posY, item.areaEffectRadius))
                            {
                                //Is he dead, on the correct team, warpable, or ignoring summons?
                                if (!p.IsDead 
                                    && target._team == p._team 
                                    && p.ActiveVehicle._type.IsWarpable 
                                    && !p._summonIgnore.Contains(player._alias) 
                                    && !p._summonIgnore.Contains("*"))
                                    p.warp(Helpers.ResetFlags.ResetNone, target._state, (short)item.accuracyRadius, -1, 0);
                            }
                        }
                        else
                            player.warp(Helpers.ResetFlags.ResetNone, target._state, (short)item.accuracyRadius, -1, 0);

                        //Set the id for abuse checking later
                        player._lastWarpItemUseID = item.id;
                        player._lastWarpItemUse = Environment.TickCount;
                    }
                    break;

                case ItemInfo.WarpItem.WarpMode.WarpAnyone:
                    {	//Are we warpable?
                        if (!player.ActiveVehicle._type.IsWarpable)
                            return;

                        //Find the player in question						
                        Player target = _playersIngame.getObjByID(targetPlayerID);
                        if (target == null)
                            return;

                        //Can't warp to dead people
                        if (target.IsDead)
                            return;

                        //Forward to our script
                        if (exists("Player.WarpItem") && !(bool)callsync("Player.WarpItem", false, player, item, targetPlayerID, posX, posY))
                            return;

                        if (item.areaEffectRadius > 0)
                        {
                            foreach (Player p in getPlayersInRange(posX, posY, item.areaEffectRadius))
                            {
                                //Is the player in range dead or ignoring summons?
                                if (!p.IsDead && !p._summonIgnore.Contains(player._alias) && !p._summonIgnore.Contains("*"))
                                    p.warp(Helpers.ResetFlags.ResetNone, target._state, (short)item.accuracyRadius, -1, 0);
                            }
                        }
                        else
                            player.warp(Helpers.ResetFlags.ResetNone, target._state, (short)item.accuracyRadius, -1, 0);

                        //Set the id for abuse checking later
                        player._lastWarpItemUseID = item.id;
                        player._lastWarpItemUse = Environment.TickCount;
                    }
                    break;

                case ItemInfo.WarpItem.WarpMode.SummonTeam:
                    {	//Find the player in question
                        Player target = _playersIngame.getObjByID(targetPlayerID);
                        if (target == null)
                            return;

                        //Is he on the correct team?
                        if (target._team != player._team)
                            return;

                        //Is he dead?
                        if (target.IsDead)
                            return;

                        //Is he warpable?
                        if (!target.ActiveVehicle._type.IsWarpable)
                            return;

                        //Is the target player ignoring this player's summons?
                        if (target._summonIgnore.Contains(player._alias) || target._summonIgnore.Contains("*"))
                        {
                            player.sendMessage(-1, "The specified player is ignoring summons.");
                            return;
                        }

                        //Is target being AntiWarped?
                        Bot targetAntiWarpBot = checkBotAntiwarp(target);
                        if (targetAntiWarpBot != null)
                        {
                            player.sendMessage(-1, "The specified player is being antiwarped by a " + targetAntiWarpBot._type.Name);
                            return;
                        }

                        //By a computer vehicle?
                        Computer targetAntiWarpVeh = checkVehAntiWarp(target);
                        if (targetAntiWarpVeh != null)
                        {
                            player.sendMessage(-1, "The specified player is being antiwarped by a " + targetAntiWarpVeh._type.Name);
                            return;
                        }

                        //How about a player?
                        Player targetAntiWarp = target.checkAntiWarp();
                        if (targetAntiWarp != null)
                        {
                            player.sendMessage(-1, String.Format("The specified player is being antiwarped by {0}", targetAntiWarp._alias));
                            return;
                        }

                        //Forward to our script
                        if (exists("Player.WarpItem") && !(bool)callsync("Player.WarpItem", false, player, item, targetPlayerID, posX, posY))
                            return;

                        if (item.areaEffectRadius > 0)
                        {
                            foreach (Player p in getPlayersInRange(target._state.positionX, target._state.positionY, item.areaEffectRadius))
                            {
                                //Is he dead, on the correct team, warpable or ignoring summons?
                                if (!p.IsDead
                                    && target._team == p._team
                                    && p.ActiveVehicle._type.IsWarpable
                                    && !p._summonIgnore.Contains(player._alias) 
                                    && !p._summonIgnore.Contains("*"))
                                    p.warp(Helpers.ResetFlags.ResetNone, player._state, (short)item.accuracyRadius, -1, 0);
                            }
                        }
                        else
                            target.warp(Helpers.ResetFlags.ResetNone, player._state, (short)item.accuracyRadius, -1, 0);

                        //Set the id for abuse checking later
                        player._lastWarpItemUseID = item.id;
                        player._lastWarpItemUse = Environment.TickCount;
                    }
                    break;

                case ItemInfo.WarpItem.WarpMode.SummonAnyone:
                    {	//Find the player in question
                        Player target = _playersIngame.getObjByID(targetPlayerID);
                        if (target == null)
                            return;

                        //Is the target dead?
                        if (target.IsDead)
                            return;

                        //Is he warpable?
                        if (!target.ActiveVehicle._type.IsWarpable)
                            return;

                        //Is the target player ignoring this player's summons?
                        if (target._summonIgnore.Contains(player._alias) || target._summonIgnore.Contains("*"))
                        {
                            player.sendMessage(-1, "The specified player is ignoring summons.");
                            return;
                        }

                        //Forward to our script
                        if (exists("Player.WarpItem") && !(bool)callsync("Player.WarpItem", false, player, item, targetPlayerID, posX, posY))
                            return;

                        if (item.areaEffectRadius > 0)
                        {
                            foreach (Player p in getPlayersInRange(target._state.positionX, target._state.positionY, item.areaEffectRadius))
                            {
                                //Is the player dead, warpable, or ignoring summons?
                                if (!p.IsDead && p.ActiveVehicle._type.IsWarpable && !p._summonIgnore.Contains(player._alias) && !p._summonIgnore.Contains("*"))
                                    p.warp(Helpers.ResetFlags.ResetNone, player._state, (short)item.accuracyRadius, -1, 0);
                            }
                        }
                        else
                            target.warp(Helpers.ResetFlags.ResetNone, player._state, (short)item.accuracyRadius, -1, 0);

                        //Set the id for abuse checking later
                        player._lastWarpItemUseID = item.id;
                        player._lastWarpItemUse = Environment.TickCount;
                    }
                    break;

                case ItemInfo.WarpItem.WarpMode.Portal:
                    {
                        //Forward it to the script for now
                        if (exists("Player.WarpItem") && !(bool)callsync("Player.WarpItem", false, player, item, targetPlayerID, posX, posY))
                            return;

                        if (item.areaEffectRadius > 0)
                        {
                            foreach (Player p in getPlayersInRange(posX, posY, item.areaEffectRadius))
                            {
                                //Is the player dead and warpable?
                                if (!p.IsDead && p.ActiveVehicle._type.IsWarpable)
                                    p.warp(Helpers.ResetFlags.ResetNone, player._state, (short)item.accuracyRadius, -1, 0);
                            }
                        }

                        //Set the id for abuse checking later
                        player._lastWarpItemUseID = item.id;
                        player._lastWarpItemUse = DateTime.Now.AddSeconds(item.portalTime / 100).Ticks;
                    }
                    break;
            }

            //Indicate that it was successful
            SC_ItemReload rld = new SC_ItemReload();
            rld.itemID = (short)item.id;

            player._client.sendReliable(rld);

            //Trollololol
            int ammoID;
            int ammoCount;

            if (player.Cash < item.cashCost)
                return;

            if (item.getAmmoType(out ammoID, out ammoCount))
                if (ammoID != 0 && !player.inventoryModify(false, ammoID, -ammoCount))
                    return;

            player.Cash -= item.cashCost;
            player.syncInventory();
        }
        #endregion

        #region handlePlayerMakeVehicle
        /// <summary>
        /// Triggered when a player attempts to use a vehicle creator
        /// </summary>
        public override void handlePlayerMakeVehicle(Player player, ItemInfo.VehicleMaker item, short posX, short posY)
        {	//What does he expect us to make?
            VehInfo vehinfo = _server._assets.getVehicleByID(item.vehicleID);
            if (vehinfo == null)
            {
                Log.write(TLog.Warning, "VehicleMaker Item {0} corresponds to invalid vehicle.", item);
                return;
            }

            //If the vehicle is a computer
            if (vehinfo.Type == VehInfo.Types.Computer)
            {
                VehInfo.Computer newComp = vehinfo as VehInfo.Computer;
                int densityType = 0;
                int densityAmount = 0;
                int totalAmount = 0;
                int totalType = 0;
                int playerTotal = 0;

                //Holy fuck this is so much shorter!
                try
                {
                    playerTotal = player._arena.Vehicles.Where(v => v != null && v._type.Id == item.vehicleID &&
                        v._creator != null && v._creator._alias == player._alias).Count();
                }
                catch (Exception e)
                {
                    Log.write(TLog.Warning, "--some error? By player " + player._alias + " " + e);
                }

                //Continue long boring non-linq stuff... zzzzz
                if (newComp != null)
                {   //Get a list of the vehicles in the arena
                    IEnumerable<Vehicle> vehs = player._arena.Vehicles;
                    foreach (Vehicle veh in vehs)
                    {
                        Computer comp = veh as Computer;
                        if (comp != null)
                        {   //If the computer is on the same team as the one the player is trying to add increment the counter
                            if (comp._team == player._team)
                            {
                                totalAmount++;
                                if (comp._type.Name == newComp.Name)
                                    totalType++;
                            }
                        }
                    }

                    //Get a list of vehicles in the computer's density radius                 
                    List<Vehicle> vehicles = player._arena.getVehiclesInRange(player._state.positionX, player._state.positionY, newComp.DensityRadius);
                    foreach (Vehicle veh in vehicles)
                    {   //Iterate through the list checking for computers
                        Computer comp = veh as Computer;
                        if (comp != null)
                        {   //If the computer is on the same team as the one the player is trying to add increment the counter
                            if (comp._team == player._team)
                            {
                                densityAmount++;
                                if (comp._type.Name == newComp.Name)
                                {   //If the computer is of the same type and on the same team as the one the player is trying to add increment other counter
                                    densityType++;
                                }
                            }
                        }
                    }
                }

                if (playerTotal >= newComp.MaxTypeByPlayerRegardlessOfTeam && newComp.MaxTypeByPlayerRegardlessOfTeam != -1)
                {   //Exceeds the amount per player regardless of team.
                    player.sendMessage(-1, "You have the maximum allowed computer vehicles of this type");
                    return;
                }
                if (totalAmount >= newComp.FrequencyMaxActive && newComp.FrequencyMaxActive != -1)
                {   //Exceeds the total amount of computer vehicles for the team
                    player.sendMessage(-1, "Your team already has the maximum allowed computer vehicles");
                    return;
                }
                if (totalType >= newComp.FrequencyMaxType && newComp.FrequencyMaxType != -1)
                {   //Exceeds the total amount of computer vehicles of this type for the team
                    player.sendMessage(-1, "Your team already has the maximum allowed computer vehicles of this type");
                    return;
                }
                if (densityAmount >= newComp.FrequencyDensityMaxActive && newComp.FrequencyDensityMaxActive != -1)
                {   //Exceeds the total amount of computer vehicles for the team in the area
                    player.sendMessage(-1, "Your team already has the maximum allowed computer vehicles in the area");
                    return;
                }
                if (densityType >= newComp.FrequencyDensityMaxType && newComp.FrequencyDensityMaxType != -1)
                {   //Exceeds the amount within the density radius for the specific type
                    player.sendMessage(-1, "Your team already has the maximum allowed computer vehicles of this type in the area");
                    return;
                }
            }

            //Indicate that it was successful
            SC_ItemReload rld = new SC_ItemReload();
            rld.itemID = (short)item.id;

            player._client.sendReliable(rld);

            //Expensive stuff, vehicle creation
            int ammoID;
            int ammoCount;

            if (player.Cash < item.cashCost)
                return;

            if (item.getAmmoType(out ammoID, out ammoCount))
                if (ammoID != 0 && !player.inventoryModify(false, ammoID, -ammoCount))
                    return;

            player.Cash -= item.cashCost;
            player.syncInventory();

            //Forward to our script
            if (!exists("Player.MakeVehicle") || (bool)callsync("Player.MakeVehicle", false, player, item, posX, posY))
            {	//Attempt to create it 
                Vehicle vehicle = newVehicle(vehinfo, player._team, player, player._state);
            }
        }
        #endregion

        #region handlePlayerItemExpire
        /// <summary>
        /// Triggered when a player's item expires
        /// </summary>
        public override void handlePlayerItemExpire(Player player, ushort itemTypeID)
        {	//What sort of item is this?
            ItemInfo itminfo = _server._assets.getItemByID(itemTypeID);
            if (itminfo == null)
            {
                Log.write(TLog.Warning, "Player attempted to expire an invalid item type.");
                return;
            }

            //Can this item expire?
            if (itminfo.expireTimer == 0)
            {	//No!
                Log.write(TLog.Warning, "Player attempted to expire an item which can't be expired: {0}", itminfo.name);
                return;
            }
            //Remove ONE item of this type... dummies!
            player.inventoryModify(itemTypeID, -1);
        }
        #endregion

        #region handlePlayerMakeItem
        /// <summary>
        /// Triggered when a player attempts to use an item creator
        /// </summary>
        public override void handlePlayerMakeItem(Player player, ItemInfo.ItemMaker item, short posX, short posY)
        {   //What does he expect us to make?
            ItemInfo itminfo = _server._assets.getItemByID(item.itemMakerItemID);
            if (itminfo == null)
            {
                Log.write(TLog.Warning, "ItemMaker Item {0} corresponds to invalid item.", item);
                return;
            }

            //Expensive stuff, item creation
            int ammoID;
            int ammoCount;

            if (player.Cash < item.cashCost)
                return;

            if (item.getAmmoType(out ammoID, out ammoCount))
                if (ammoID != 0 && !player.inventoryModify(ammoID, -ammoCount))
                    return;

            player.Cash -= item.cashCost;

            //Forward to our script
            if (!exists("Player.MakeItem") || (bool)callsync("Player.MakeItem", false, player, item, posX, posY))
            {   //Do we create it in the inventory or arena?
                if (item.itemMakerQuantity > 0)
                    itemSpawn(itminfo, (ushort)item.itemMakerQuantity, posX, posY, 0, (int)player._team._id, player);
                else
                    player.inventoryModify(itminfo, Math.Abs(item.itemMakerQuantity));

                //Indicate that it was successful
                SC_ItemReload rld = new SC_ItemReload();
                rld.itemID = (short)item.id;

                player._client.sendReliable(rld);

                player.syncState();
            }
        }
        #endregion

        #region handlerPlayerCommCommand
        /// <summary>
        /// Triggered when a player sends a chat/communication command
        /// </summary>
        public override void handlePlayerCommCommand(Player player, Player recipient, string command, string payload)
        {
            if (!exists("Player.CommCommand") || (bool)callsync("Player.CommCommand", false, player, recipient, command, payload))
            {
            }
        }
        #endregion

        #region handlePlayerChatCommand
        /// <summary>
        /// Triggered when a player sends a chat command
        /// </summary>
        public override void handlePlayerChatCommand(Player player, Player recipient, string command, string payload)
        {
            if (!exists("Player.ChatCommand") || (bool)callsync("Player.ChatCommand", false, player, recipient, command, payload))
            {
            }
        }
        #endregion

        #region handlePlayerModCommand
        /// <summary>
        /// Triggered when a player sends a mod command
        /// </summary>
        public override void handlePlayerModCommand(Player player, Player recipient, string command, string payload)
        {
            //Do initial checks first before ok'ing it
            if (player.PermissionLevelLocal < Data.PlayerPermission.ArenaMod)
                return;

            if (player.PermissionLevel < Data.PlayerPermission.Mod)
            {
                if (recipient != null && !recipient._arena._name.Equals(player._arena._name))
                {
                    player.sendMessage(-1, "You cannot use commands from one arena to another.");
                    return;
                }
            }

            if ((bool)callsync("Player.ModCommand", false, player, recipient, command, payload))
            {
                //Only if return is true do we show mods the command
                //NOTE: DO NOT LEAVE AN EMPTY SCRIPT MOD COMMAND, IT WILL LOG IN DB
                //WITH ANYONE TYPING STUFF LIKE *HI

                //Did someone just type *
                if (String.IsNullOrEmpty(command))
                    return;

                //Command logging (ignore normal player permission commands like *help, etc)
                if (player.PermissionLevelLocal != Data.PlayerPermission.Normal)
                {   //Notify his superiors in the arena
                    string sRecipient;
                    foreach (Player p in Players)
                    {
                        if (p == player)
                            continue;

                        if ((int)player.PermissionLevelLocal <= (int)p.PermissionLevelLocal)
                        {
                            p.sendMessage(0, String.Format("&[Arena: {0}] {1}>{2} *{3} {4}",
                                player._arena._name,
                                player._alias,
                                sRecipient = (recipient != null)
                                    ? " :" + recipient._alias + ":"
                                    : String.Empty,
                                command,
                                payload));
                        }
                    }
                }

                //Log it in the history database
                if (!_server.IsStandalone)
                {
                    CS_ModCommand<Data.Database> pkt = new CS_ModCommand<Data.Database>();
                    pkt.sender = player._alias;
                    pkt.recipient = (recipient != null) ? recipient._alias : "none";
                    pkt.zone = player._server.Name;
                    pkt.arena = player._arena._name;
                    pkt.command = command + " " + payload;

                    //Send it!
                    player._server._db.send(pkt);
                }
            }
        }
        #endregion

        #region handlePlayerRepair
        /// <summary>
        /// Triggered when a player attempts to repair/heal
        /// </summary>
        public override void handlePlayerRepair(Player player, ItemInfo.RepairItem item, UInt16 targetVehicle, short posX, short posY)
        {   //Does the player have appropriate ammo?
            if (item.useAmmoID != 0 && !player.inventoryModify(false, item.useAmmoID, -item.ammoUsedPerShot))
                return;

            // Forward it to our script
            if (!exists("Player.Repair") || (bool)callsync("Player.Repair", false, player, item, targetVehicle, posX, posY))
            {	
                //New formula
                float percentage = (float)item.repairPercentage / 100;

                //For old formula, too lazy to switch out all item files.. dont judge!
                if (item.repairPercentage > 100)
                    percentage = (float)item.repairPercentage / 1000;

                int repairAmount = item.repairAmount;

                //What type of repair is it?
                switch (item.repairType)
                {
                    //Health and energy repair
                    case 0:
                    case 2:
                        {	//Is it an area or individual repair?
                            if (item.repairDistance > 0)
                            {	//Individual! Do we have a valid target?
                                Player target = _playersIngame.getObjByID(targetVehicle);
                                if (target == null)
                                {
                                    Log.write(TLog.Warning, "Player {0} attempted to use a {1} to heal a non-existent player.", player._alias, item.name);
                                    return;
                                }

                                //Is he dead?
                                if (target.IsDead)
                                    return;

                                //Is he on the correct team?
                                if (target._team != player._team)
                                    return;

                                //Is he in range?
                                if (!Helpers.isInRange(item.repairDistance, target._state, player._state))
                                    return;

                                //Repair!
                                target.heal(item, player);
                            }
                            else if (item.repairDistance < 0)
                            {	//An area heal! Get all players within this area..
                                List<Player> players = _playersIngame.getObjsInRange(player._state.positionX, player._state.positionY, -item.repairDistance);

                                //Check each player
                                foreach (Player p in players)
                                {	//Is he dead?
                                    if (p.IsDead)
                                        continue;

                                    //Is he on the correct team?
                                    if (p._team != player._team)
                                        continue;

                                    //Can we self heal?
                                    if (p == player && !item.repairSelf)
                                        continue;

                                    //Heal!
                                    p.heal(item, player);
                                }
                            }
                            else
                            {	//A self heal! Sure you can!
                                player.heal(item, player);
                            }
                        }
                        break;

                    //Vehicle repair
                    case 1:
                        {	//Is it an area or individual repair?
                            if (item.repairDistance > 0)
                            {	//Individual! Do we have a valid target?
                                Vehicle target = _vehicles.getObjByID(targetVehicle);
                                if (target == null)
                                {
                                    Log.write(TLog.Warning, "Player {0} attempted to use a {1} to repair a non-existent vehicle.", player._alias, item.name);
                                    return;
                                }

                                //Is it in range?
                                if (!Helpers.isInRange(item.repairDistance, target._state, player._state))
                                    return;

                                target.heal(player, item);
                            }
                            else if (item.repairDistance < 0)
                            {	//An area heal! Get all vehicles within this area..
                                List<Vehicle> vehicles = _vehicles.getObjsInRange(player._state.positionX, player._state.positionY, -item.repairDistance);

                                //Check each vehicle
                                foreach (Vehicle v in vehicles)
                                {	//Is it on the correct team? (temporary or not)
                                    if (v._team != player._team)
                                        continue;

                                    //Can we self heal?
                                    if (v._inhabitant == player && !item.repairSelf)
                                        continue;

                                    //Repair our main!
                                    v.heal(player, item);

                                    //Heal our childs
                                    foreach (Vehicle child in v._childs)
                                    {
                                        //Can we self heal?
                                        if (child._inhabitant == player && !item.repairSelf)
                                            continue;

                                        child.heal(player, item);
                                    }

                                }
                            }
                            else
                            {	//A self heal! Sure you can!
                                Vehicle target = _vehicles.getObjByID(targetVehicle);
                                if (target != null)
                                    target.heal(player, item);
                            }
                        }
                        break;
                }

                //Indicate that it was successful
                SC_ItemReload rld = new SC_ItemReload();
                rld.itemID = (short)item.id;

                player._client.sendReliable(rld);

                //Send an item used notification to players
                Helpers.Player_RouteItemUsed(false, Players, player, targetVehicle, (Int16)item.id, posX, posY, 0);
            }
        }
        #endregion

        #region handlePlayerControl
        /// <summary>
        /// Triggered when a player requests to take/steal ownership of a vehicle item
        /// </summary>
        public override void handlePlayerControl(Player player, ItemInfo.ControlItem item, UInt16 targetVehicle, short posX, short posY)
        {
            //Lets find the vehicle in question
            Vehicle target = _vehicles.getObjByID(targetVehicle);
            if (target == null)
            {
                Log.write(TLog.Warning, "Player {0} tried taking or stealing ownership of invalid vehicle id '{1}'.", player, targetVehicle);
                return;
            }

            //Is it a computer type?
            if (target._type.Type != VehInfo.Types.Computer)
                return;

            //Are we in the distance to control it?
            if (!Helpers.isInRange(item.controlDistance, target._state, player._state))
                return;

            VehInfo.Computer compCheck = target._type as VehInfo.Computer;
            int totalAmount = 0;
            int totalTempAmount = 0;
            int playerTotal = 0;

            if (compCheck != null)
            {
                //Lets check ownerships first
                if (target._team == player._team)
                {
                    player.sendMessage(-1, "You already have control of this.");
                    return;
                }

                //Is it unowned?
                if (target._team == null && !Logic_Assets.SkillCheck(player, compCheck.LogicTakeOwnership))
                    return;

                //Can we steal it?
                if (target._team != null && !Logic_Assets.SkillCheck(player, compCheck.LogicStealOwnership))
                    return;

                //Lets check team limits now
                playerTotal = player._arena.Vehicles.Where(v => v != null && v._type.Id == target._type.Id && v._creator != null &&
                    v._creator._alias == player._alias).Count();

                IEnumerable<Vehicle> vehs = player._arena.Vehicles;
                foreach (Vehicle veh in vehs)
                {
                    Computer comp = veh as Computer;
                    if (comp != null)
                    {
                        //If the computer is on the same team
                        if (comp._reprogrammed && comp._team == player._team)
                            totalTempAmount++;
                        if (!comp._reprogrammed && comp._team == player._team)
                            totalAmount++;
                    }
                }
            }

            if (compCheck.MaxTypeByPlayerRegardlessOfTeam != -1 && playerTotal >= compCheck.MaxTypeByPlayerRegardlessOfTeam)
            {   //Exceeds the amount per player regardless of team.
                player.sendMessage(-1, "You have the maximum allowed computer vehicles of this type.");
                return;
            }

            if (compCheck.FrequencyMaxActive != -1 && totalTempAmount >= compCheck.FrequencyMaxActive)
            {   //Exceeds the total amount of computer vehicles for the team
                player.sendMessage(-1, "Your team already has the maximum allowed computer vehicles.");
                return;
            }

            //Does player have the appropriate ammo?
            if (item.useAmmoID != 0 && !player.inventoryModify(false, item.useAmmoID, -item.ammoUsedPerShot))
                return;

            //Forward to our script
            if (!exists("Player.Control") || (bool)callsync("Player.Control", false, player, item, targetVehicle, posX, posY))
            {
                //Lets change ownership and set the control timer
                target._team = player._team;
                target._reprogrammed = true;
                target._tickControlTime = Environment.TickCount;
                target._tickControlEnd = item.controlTime;

                //Indicate that it was successful
                SC_ItemReload rld = new SC_ItemReload();
                rld.itemID = (short)item.id;

                player._client.sendReliable(rld);
                player.syncState();

                //Send an item used notification and vehicle update to players
                Helpers.Player_RouteItemUsed(false, Players, player, targetVehicle, (Int16)item.id, posX, posY, 0);
            }
        }
        #endregion

        #region handlePlayerSpectate
        /// <summary>
        /// Triggered when a player attempts to spectate another player
        /// </summary>
        public override void handlePlayerSpectate(Player player, ushort targetPlayerID)
        {	//Make sure he's in spec himself
            if (!player.IsSpectator)
                return;

            //Find the player in question						
            Player target = _playersIngame.getObjByID(targetPlayerID);
            if (target == null)
                return;

            //Can't spectate other spectators
            if (target.IsSpectator)
                return;

            //Does the zone allow spectating?
            if (!target._server._zoneConfig.arena.allowSpectating && player.PermissionLevel < Data.PlayerPermission.ArenaMod)
            {
                player.sendMessage(-1, "Zone doesn't allow spectating players.");
                return;
            }

            //Check spectator permission
            if (!target._bAllowSpectator && player.PermissionLevel < Data.PlayerPermission.ArenaMod)
            {
                player.sendMessage(-1, "Specified player isn't allowing spectators.");
                return;
            }

            //Tell him yes!
            player.spectate(target);
        }
        #endregion

        #region handleVehiclePickup
        /// <summary>
        /// Triggered when a player requests to pick up a vehicle item
        /// </summary>
        public override void handleVehiclePickup(Player from, CS_VehiclePickup update)
        {
            Vehicle ve = _vehicles.getObjByID(update.vehicleID);
            if (ve == null)
            {
                Log.write(TLog.Warning, "Player {0} tried picking up an invalid vehicle id.", from);
                return;
            }

            if (from == null)
            {
                Log.write(TLog.Warning, "handleVehiclePickup(): Called with null player.");
                return;
            }

            //Find the vehicle item in question
            lock (_vehicles)
            {
                int quantity = 1;

                VehInfo veh = _server._assets.getVehicleByID(ve._type.Id);
                if (veh == null)
                {
                    Log.write(TLog.Warning, "Player {0} tried picking up an invalid vehicle id.", from);
                    return;
                }

                if (veh.PickupItemId == -1)
                {
                    from.sendMessage(-1, "You can't pick this type up.");
                    return;
                }

                ItemInfo info = _server._assets.getItemByID(veh.PickupItemId);
                if (info == null)
                {
                    Log.write(TLog.Warning, "Vehicle pickup id {0} doesn't exist.", veh.PickupItemId);
                    return;
                }

                //In range?
                if (!Helpers.isInRange(_server._zoneConfig.arena.itemPickupDistance, ve._state.positionX, ve._state.positionY,
                                        from._state.positionX, from._state.positionY))
                    return;

                //Allowed pickup?
                if (!_server._zoneConfig.level.allowUnqualifiedPickup && !Logic_Assets.SkillCheck(from, info.skillLogic))
                    return;

                //Lets see if we have this item already and check limits
                Player.InventoryItem ii = from.getInventory(info);
                if (info.maxAllowed != 0)
                {
                    int constraint = Math.Abs(info.maxAllowed) - ((ii == null) ? (ushort)0 : ii.quantity);
                    if (quantity > constraint)
                    {
                        from.sendMessage(-1, "You already have max amount of this item type.");
                        return;
                    }
                }

                //Forward to our script
                if (!exists("Player.VehiclePickup") || (bool)callsync("Player.VehiclePickup", false, from, ve, quantity))
                {
                    if (veh.Type == VehInfo.Types.Computer)
                    {
                        if (from._team == null)
                        {
                            Log.write(TLog.Error, "handleVehiclePickup(): Player has no team.");
                            return;
                        }

                        VehInfo.Computer comp = veh as VehInfo.Computer;
                        List<Vehicle> vehicles = from._arena.getVehiclesInRange(from._state.positionX, from._state.positionY,
                                (comp.DensityRadius > 0 ? comp.DensityRadius : comp.DensityRadius + 50));
                        if (vehicles != null)
                        {
                            int found = 0;
                            //Lets check for same team/taking ownership or stealing ownership
                            foreach (Vehicle see in vehicles)
                            {
                                if (see == null || see._team == null)
                                    continue;

                                if (see._team == from._team)
                                    found++;
                                else if (see._type.Type == VehInfo.Types.Computer)
                                {
                                    VehInfo.Computer check = see._type as VehInfo.Computer;
                                    if (Logic_Assets.SkillCheck(from, check.LogicTakeOwnership))
                                        found++;
                                    if (Logic_Assets.SkillCheck(from, check.LogicStealOwnership))
                                        found++;
                                }
                            }

                            //Sanity check for hackers
                            if (quantity > found)
                            {
                                Log.write(TLog.Warning, "Player {0} tried picking up more then 1 vehicle at a time.", from._alias);
                                return;
                            }

                            //Add the pickup to inventory!
                            from.inventoryModify(info, quantity);

                            //Update his bounty.
                            from.Bounty += info.prizeBountyPoints;

                            //Destroy vehicle on the ground
                            ve.destroy(true);
                        }
                    }
                }
            }
        }
        #endregion

        #region handleVehicleCreation
        /// <summary>
        /// Triggered when a vehicle is created
        /// </summary>
        /// <remarks>Doesn't catch spectator or dependent vehicle creation</remarks>
        public override void handleVehicleCreation(Vehicle created, Team team, Player creator)
        {
            //Forward it to our script
            if (!exists("Vehicle.Creation") || (bool)callsync("Vehicle.Creation", false, created, team, creator))
            {
            }
        }
        #endregion

        #region handleVehicleDeath
        /// <summary>
        /// Triggered when a vehicle dies
        /// </summary>
        public override void handleVehicleDeath(Vehicle dead, Player killer, Player occupier)
        {	//Forward it to our script
            if (!exists("Vehicle.Death") || (bool)callsync("Vehicle.Death", false, dead, killer))
            {	//Route the death to the arena
                Helpers.Vehicle_RouteDeath(Players, killer, dead, occupier);

                //Innate Drop items?
                VehInfo vehicle = _server._assets.getVehicleByID(dead._type.Id);
                if (vehicle == null)
                {
                    Log.write(TLog.Warning, "HandleVehicleDeath: Cannot find vehicle {0}({1}).", dead._type.Name, dead._type.Id);
                    return;
                }

                if (vehicle.DropItemId != 0)
                {
                    ItemInfo item = _server._assets.getItemByID(vehicle.DropItemId);
                    if (item != null && vehicle.DropItemQuantity > 0)
                        itemSpawn(item, (ushort)vehicle.DropItemQuantity, dead._state.positionX, dead._state.positionY, null);
                }

                //Update vehicle kills and deaths stat
                if (killer != null && dead._team != killer._team)
                {
                    if (vehicle.Type == VehInfo.Types.Car)
                    {
                        VehInfo.Car type = vehicle as VehInfo.Car;
                        if (type.Mode != 2) //2 == Jetpack
                        {
                            //If this is a type 5, lets see if we really are a car by checking dependants
                            if (type.Mode == 5)
                            {
                                bool foundchild = false;
                                foreach (int childID in type.ChildVehicles)
                                {
                                    if (childID > 0)
                                    {
                                        //Found a dependant, we are a car
                                        foundchild = true;
                                        break;
                                    }
                                }
                                if (!foundchild)
                                    //Not a car
                                    return;
                            }

                            killer.vehicleKills++;
                            if (occupier != null && occupier._occupiedVehicle._type.Id == dead._type.Id)
                                occupier.vehicleDeaths++;

                            //Are we sharing sibling kills?
                            if (killer._occupiedVehicle != null && killer._occupiedVehicle._type.SiblingKillsShared > 0)
                            {
                                List<Player> sharedKills = killer._arena.getPlayersInRange(killer._state.positionX, killer._state.positionY, 50);
                                foreach (Player shared in sharedKills)
                                {
                                    if (shared == killer)
                                        continue;
                                    if (shared._occupiedVehicle != null)
                                        shared.vehicleKills++;
                                }
                            }
                        }
                    }
                }

                //Was this a warp point?
                if (dead._type.Type == VehInfo.Types.Computer)
                {
                    string name = dead._type.Name.ToLower();
                    if (name.Contains("warp point"))
                    {
                        foreach(Player p in Players)
                        {
                            if (p == null || p == killer)
                                continue;

                            if (p == dead._creator && dead._creator != null)
                                p.triggerMessage(5, 500, String.Format("{0} killed by {1} at {2}", dead._type.Name, killer._alias, dead._state.letterCoord()));
                            else
                                p.triggerMessage(5, 500, String.Format("{0} lost a {1} at {2}!", dead._team._name, dead._type.Name, dead._state.letterCoord()));
                        }
                    }
                }
            }
        }
        #endregion

        #region handleBotDeath
        /// <summary>
        /// Triggered when a bot is killed
        /// </summary>
        public override void handleBotDeath(Bot dead, Player killer, int weaponID)
        {	//Forward it to our script
            if (!exists("Bot.Death") || (bool)callsync("Bot.Death", false, dead, killer, weaponID))
            {	//Route the death to the arena
                Helpers.Vehicle_RouteDeath(Players, killer, dead, null);
                if (killer != null && dead._team != killer._team)
                {//Don't allow rewards for team kills
                    Logic_Rewards.calculateBotKillRewards(dead, killer);
                }
            }
        }
        #endregion

        #region handlePlayerDamageEvent
        /// <summary>
        /// Triggered when a player notifies the server of a damage event
        /// </summary>
        public override void handlePlayerDamageEvent(Player from, CS_DamageEvent update)
        {
            if (from == null)
            {
                Log.write(TLog.Error, "handlePlayerDamageEvent(): Called with null player.");
                return;
            }

            ItemInfo.Projectile usedWep = Helpers._server._assets.getItemByID(update.damageID) as ItemInfo.Projectile;

            if (usedWep == null)
            {
                Log.write(TLog.Error, "No weapon ({0}) found for damage event from Player {1}", update.damageID, from);
                return;
            }

            if (String.IsNullOrWhiteSpace(usedWep.damageEventString) || usedWep.damageEventString == "\"\"")
            {
                Log.write(TLog.Error, "No damage event string found for weapon: {0}", usedWep.name);
                return;
            }

            if (usedWep.damageEventRadius == 0)
                return;

            //Forward to our script and give it the option of taking over
            if (!(bool)callsync("Player.DamageEvent", false, from, usedWep, update.positionX, update.positionY, update.positionZ))
            {
                List<Vehicle> vechs = _vehicles.getObjsInRange(update.positionX, update.positionY, usedWep.damageEventRadius + 500);
                //Notify all vehicles in the vicinity
                foreach (Vehicle v in vechs)
                    if (!v.IsDead)
                        Logic_Assets.RunEvent(from, usedWep.damageEventString);
            }
        }
        #endregion

        #endregion

        #endregion
    }
}