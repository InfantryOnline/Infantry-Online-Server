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

namespace InfServer.Script.GameType_SKCTF
{	// Script Class
	/// Provides the interface between the script and arena
	///////////////////////////////////////////////////////
	class Script_SKCTF : Scripts.IScript
	{	///////////////////////////////////////////////////
		// Member Variables
		///////////////////////////////////////////////////
		private Arena _arena;					        //Pointer to our arena class
		private CfgInfo _config;				        //The zone config
        private Points _points;                         //Our points

        //Timers
        private int _lastGameCheck;                     //The last time we polled the arena
        private int _tickGameStart;
        private int _tickGameStarting;
        private int _lastFlagCheck;

		//Settings
		private int _minPlayers;				        //The minimum amount of players to start a flag game
        private int _pointSmallChange;                  //The small change to # of points (ex: kills, turret kills, etc)
        private int _pointPeriodicChange;               //The change to # of points based on periodic flag rewards

        //Scores
        private Dictionary<Player, int> _healingDone;   //Keep track of healing done by players

		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Performs script initialization
		/// </summary>
		public bool init(IEventObject invoker)
		{	//Populate our variables
			_arena = invoker as Arena;
			_config = _arena._server._zoneConfig;

            _minPlayers = Int32.MaxValue;

			foreach (Arena.FlagState fs in _arena._flags.Values)
			{	//Determine the minimum number of players
				if (fs.flag.FlagData.MinPlayerCount < _minPlayers)
					_minPlayers = fs.flag.FlagData.MinPlayerCount;

				//Register our flag change events
				fs.TeamChange += onFlagChange;
			}

            if (_minPlayers == Int32.MaxValue)
                //No flags? Run blank games
                _minPlayers = 1;

			return true;
		}

		/// <summary>
		/// Allows the script to maintain itself
		/// </summary>
		public bool poll()
		{	//Should we check game state yet?
			int now = Environment.TickCount;

			if (now - _lastGameCheck <= Arena.gameCheckInterval)
				return true;
			_lastGameCheck = now;

			//Do we have enough players ingame?
			int playing = _arena.PlayerCount;

			if ((_tickGameStart == 0 || _tickGameStarting == 0) && playing < _minPlayers)
			{	//Stop the game!
				_arena.setTicker(1, 1, 0, "Not Enough Players");
				_arena.gameReset();
			}
			//Do we have enough players to start a game?
            else if (_tickGameStart == 0 && _tickGameStarting == 0 && playing >= _minPlayers)
            {	//Great! Get going
                _tickGameStarting = now;
                _arena.setTicker(1, 1, _config.flag.startDelay * 100, "Next game: ",
                    delegate()
                    {	//Trigger the game start
                        _arena.gameStart();
                    }
                );
            }
            //Maybe the game is in progress...
            else
            {   //It is!
                //The change to small points changes needs to be updated based on players in game constantly
                _pointSmallChange = (int)Math.Ceiling((double)25 / _arena.PlayersIngame.Count());
                _pointPeriodicChange = 1;

                //Let's update some points!
                int flagdelay = 1000; //1000 = 1 second
                if (now - _lastFlagCheck >= flagdelay)
                {   //It's time for a flag update

                    //Loop through every flag
                    foreach (Arena.FlagState fs in _arena._flags.Values)
                        //Add points for every flag they own
                        foreach (Team t in _arena.Teams)
                            if (t == fs.team && _points != null)
                                _points[t] += _pointPeriodicChange;

                    //Update our tick
                    _lastFlagCheck = now;
                }
            }
			return true;
		}

		#region Events
		/// <summary>
		/// Called when a flag changes team
		/// </summary>
		public void onFlagChange(Arena.FlagState flag)
		{	//Does this team now have all the flags?
            bool allFlags = true;

            if (flag.team == null || _arena._flags == null || _arena._flags.Count == 0)
                return;
			foreach (Arena.FlagState fs in _arena._flags.Values)
                if(fs.flag != null)
				    if (fs.team != flag.team)
					    allFlags = false;

            if (allFlags && _arena.DesiredTeams.Contains(flag.team))
                _arena.sendArenaMessage(flag.team._name + " controls all the flags!", 20);
            else
                _arena.sendArenaMessage(flag.team._name + " has captured " + flag.flag.GeneralData.Name + "!", 21);
		}

		/// <summary>
		/// Called when the specified team have won
		/// </summary>
		public void gameVictory(Team victors)
		{	//Game is over.
            _arena.sendArenaMessage(victors._name + " has reached " + _points[victors] + " points!", 13);

            //Clear out all tickers we use in updateTickers (1,2,3)
            for (int i = 1; i <= 3; i++)
                _arena.setTicker(0, i, 0, "");

            //Lets reward the teams and the MVP!
            int rpoints = _arena._server._zoneConfig.flag.pointReward * _arena.PlayersIngame.Count();
            int rcash = _arena._server._zoneConfig.flag.cashReward * _arena.PlayersIngame.Count();
            int rexperience = _arena._server._zoneConfig.flag.experienceReward * _arena.PlayersIngame.Count();

            //Give higher reward the more points they have
            foreach (Team t in _arena.ActiveTeams)
            {
                foreach (Player p in t.ActivePlayers)
                {   //Reward each player based on his teams performance
                    int points = _points[t];
                    double modifier = ((points / _points.MaxPoints) * 2) + 1;
                    rpoints = Convert.ToInt32(rpoints * modifier);
                    rcash = Convert.ToInt32(rcash * modifier);
                    rexperience = Convert.ToInt32(rexperience * modifier);
                    p.BonusPoints += rpoints;
                    p.Cash += rcash;
                    p.Experience += rexperience;
                    p.sendMessage(0, String.Format("Personal Reward: Points={0} Cash={1} Experience={2}",
                        rpoints, rcash, rexperience));
                    p.syncState();
                }
            }

            //TODO: Reward the MVP. Also reward the best healers.
            //Stop the game
            _arena.gameEnd();

            //Set off some fireworks using the .lio file to specify locations based on name (starts with 'firework' in name)
            foreach (LioInfo.Hide firework in _arena._server._assets.Lios.Hides.Where(h => h.GeneralData.Name.ToLower().Contains("firework")))
                Helpers.Player_RouteExplosion(_arena.Players, (short)firework.HideData.HideId, firework.GeneralData.OffsetX, firework.GeneralData.OffsetY, 0, 0);
		}

        /// <summary>
        /// Called when a teams points have been modified
        /// </summary>
        public void onPointModify(Team team, int points)
        {
            //Update the tickers
            updateTickers();

            //Check for game victory here
            if (points >= _points.MaxPoints)
                //They were the first team to reach max points!
                gameVictory(team);
        }

        public void updateTickers()
        {
            if (_points != null)
            {
                //Their teams points
                _arena.setTicker(0, 1, 0,
                    delegate(Player p)
                    {
                        //Update their ticker with current team points
                        if (!_arena.DesiredTeams.Contains(p._team) && _points != null)
                            return "";
                        return "Your Team: " + _points[p._team] + " points";
                    }
                );
                //Other teams points
                _arena.setTicker(0, 2, 0,
                    delegate(Player p)
                    {
                        //Update their ticker with every other teams points
                        List<string> otherTeams = new List<string>();
                        foreach (Team t in _arena.DesiredTeams)
                            if (t != p._team)
                                otherTeams.Add(t._name + ": " + _points[t] + " points");
                        if (otherTeams.Count == 0)
                            return "";
                        return String.Join(", ", otherTeams.ToArray());
                    }
                );
                //Point rewards
                _arena.setTicker(0, 3, 0, "Kill rewards: " + _pointSmallChange + " points");
            }
        }

        /// <summary>
        /// Called when a player sends a chat command
        /// </summary>
        [Scripts.Event("Player.ChatCommand")]
        public bool playerChatCommand(Player player, Player recipient, string command, string payload)
        {
            return true;
        }

		/// <summary>
		/// Called when a player enters the game
		/// </summary>
		[Scripts.Event("Player.Enter")]
		public void playerEnter(Player player)
		{
		}

		/// <summary>
		/// Called when a player leaves the game
		/// </summary>
		[Scripts.Event("Player.Leave")]
		public void playerLeave(Player player)
		{   //Destroy all vehicles belonging to him
            foreach (Vehicle v in _arena.Vehicles)
                if (v._type.Type == VehInfo.Types.Computer && v._creator == player)
                    //Destroy it!
                    v.destroy(true);
		}

		/// <summary>
		/// Called when the game begins
		/// </summary>
		[Scripts.Event("Game.Start")]
		public bool gameStart()
		{	//We've started!
			_tickGameStart = Environment.TickCount;
			_tickGameStarting = 0;

            //Scramble the teams!
            ScriptHelpers.scrambleTeams(_arena, 2, true);

			//Spawn our flags!
			_arena.flagSpawn();

            //Create some points and subscribe to our point modification event
            _points = new Points(_arena.ActiveTeams, 0, 1000);
            _points.PointModify += onPointModify;

            //Start keeping track of healing
            _healingDone = new Dictionary<Player, int>();

			//Let everyone know
			_arena.sendArenaMessage("Game has started! First team to " + _points.MaxPoints + " points wins!", _config.flag.resetBong);
            updateTickers();

			return true;
		}

		/// <summary>
		/// Called when the game ends
		/// </summary>
		[Scripts.Event("Game.End")]
		public bool gameEnd()
		{	//Game finished, perhaps start a new one
			_tickGameStart = 0;
			_tickGameStarting = 0;
            _healingDone = null;

			return true;
		}

        /// <summary>
        /// Called when the statistical breakdown is displayed
        /// </summary>
        [Scripts.Event("Player.Breakdown")]
        public bool playerBreakdown(Player from, bool bCurrent)
        {	//Allows additional "custom" breakdown information
            //List the best healers by sorting them according to healingdone
            from.sendMessage(0, "#Healer Breakdown");
            if (_healingDone != null && _healingDone.Count > 0)
            {
                List<KeyValuePair<Player, int>> healers = _healingDone.ToList();
                healers.Sort((a, b) => { return a.Value.CompareTo(b.Value); });
                healers.Reverse();

                int i = 1;
                foreach (KeyValuePair<Player, int> healer in healers)
                {   //Display top 3 healers in arena
                    from.sendMessage(0, String.Format("!{0} (Healed={1}): {2}",
                        ScriptHelpers.toOrdinal(i),healer.Value, healer.Key._alias));
                    if (i++ > 3)
                        break;
                }
            }

            //List teams by most points
            from.sendMessage(0, "#Team Breakdown");
            if (_points != null)
            {
                List<Team> teams = _arena.Teams.Where(t => _points[t] != 0).ToList();
                teams.OrderByDescending(t => _points[t]);

                int j = 1;
                foreach (Team t in teams)
                {
                    from.sendMessage(0, String.Format("!{0} (Points={1} Kills={2} Deaths={3}): {4}",
                        ScriptHelpers.toOrdinal(j), _points[t], t._calculatedKills, t._calculatedDeaths, t._name));
                    j++;
                }
            }

            from.sendMessage(0, "#Player Breakdown");
            int k = 1;
            foreach (Player p in _arena.PlayersIngame.OrderByDescending(player => (bCurrent ? player.StatsCurrentGame.kills : player.StatsLastGame.kills)))
            {   //Display top 3 players
                from.sendMessage(0, String.Format("!{0} (K={1} D={2}): {3}",
                    ScriptHelpers.toOrdinal(k),
                    (bCurrent ? p.StatsCurrentGame.kills : p.StatsLastGame.kills),
                    (bCurrent ? p.StatsCurrentGame.deaths : p.StatsLastGame.deaths),
                    p._alias));
                if (k++ > 3)
                    break;
            }
            //Display his score
            from.sendMessage(0, String.Format("@Personal Score: (K={0} D={1})",
                (bCurrent ? from.StatsCurrentGame.kills : from.StatsLastGame.kills),
                (bCurrent ? from.StatsCurrentGame.deaths : from.StatsLastGame.deaths)));

            //return false to avoid another breakdown from showing
            return false;
        }

		/// <summary>
		/// Called to reset the game state
		/// </summary>
		[Scripts.Event("Game.Reset")]
		public bool gameReset()
		{	//Game reset, perhaps start a new one
			_tickGameStart = 0;
			_tickGameStarting = 0;

			return true;
		}

		/// <summary>
		/// Triggered when a player requests to pick up an item
		/// </summary>
		[Scripts.Event("Player.ItemPickup")]
		public bool playerItemPickup(Player player, Arena.ItemDrop drop, ushort quantity)
		{	
			return true;
		}

		/// <summary>
		/// Triggered when a player requests to drop an item
		/// </summary>
		[Scripts.Event("Player.ItemDrop")]
		public bool playerItemDrop(Player player, ItemInfo item, ushort quantity)
		{
			return true;
		}

		/// <summary>
		/// Handles a player's portal request
		/// </summary>
		[Scripts.Event("Player.Portal")]
		public bool playerPortal(Player player, LioInfo.Portal portal)
		{           
			return true;
		}

		/// <summary>
		/// Handles a player's produce request
		/// </summary>
		[Scripts.Event("Player.Produce")]
		public bool playerProduce(Player player, Computer computer, VehInfo.Computer.ComputerProduct product)
		{
			return true;
		}

		/// <summary>
		/// Handles a player's switch request
		/// </summary>
		[Scripts.Event("Player.Switch")]
		public bool playerSwitch(Player player, LioInfo.Switch swi)
		{
			return true;
		}

		/// <summary>
		/// Handles a player's flag request
		/// </summary>
		[Scripts.Event("Player.FlagAction")]
		public bool playerFlagAction(Player player, bool bPickup, bool bInPlace, LioInfo.Flag flag)
		{
			return true;
		}

		/// <summary>
		/// Handles the spawn of a player
		/// </summary>
		[Scripts.Event("Player.Spawn")]
		public bool playerSpawn(Player player, bool bDeath)
		{
			return true;
		}

		/// <summary>
		/// Triggered when a player wants to unspec and join the game
		/// </summary>
		[Scripts.Event("Player.JoinGame")]
		public bool playerJoinGame(Player player)
		{
			return true;
		}

		/// <summary>
		/// Triggered when a player wants to spec and leave the game
		/// </summary>
		[Scripts.Event("Player.LeaveGame")]
		public bool playerLeaveGame(Player player)
		{
			return true;
		}

		/// <summary>
		/// Triggered when a player wants to enter a vehicle
		/// </summary>
		[Scripts.Event("Player.EnterVehicle")]
		public bool playerEnterVehicle(Player player, Vehicle vehicle)
		{
			return true;
		}

		/// <summary>
		/// Triggered when a player wants to leave a vehicle
		/// </summary>
		[Scripts.Event("Player.LeaveVehicle")]
		public bool playerLeaveVehicle(Player player, Vehicle vehicle)
		{
			return true;
		}

		/// <summary>
		/// Triggered when a player notifies the server of an explosion
		/// </summary>
		[Scripts.Event("Player.Explosion")]
		public bool playerExplosion(Player player, ItemInfo.Projectile weapon, short posX, short posY, short posZ)
		{
			return true;
		}

		/// <summary>
		/// Triggered when a player has died, by any means
		/// </summary>
		/// <remarks>killer may be null if it wasn't a player kill</remarks>
		[Scripts.Event("Player.Death")]
		public bool playerDeath(Player victim, Player killer, Helpers.KillType killType, CS_VehicleDeath update)
		{
            //Was it a player kill?
            if (killType == Helpers.KillType.Player)
            {   //No team killing!
                if (victim._team != killer._team)
                    //Reward the killers team!
                    if (_points != null)
                        _points[killer._team] += _pointSmallChange;
            }

            //Was it a computer kill?
            if (killType == Helpers.KillType.Computer)
            {
                //Let's find the vehicle!
                Computer cvehicle = victim._arena.Vehicles.FirstOrDefault(v => v._id == update.killerPlayerID) as Computer;
                Player vehKiller = cvehicle._creator;
                //Does it exist?
                if (cvehicle != null && vehKiller != null)
				{
                    //We'll take it from here...
                    update.type = Helpers.KillType.Player;
                    update.killerPlayerID = vehKiller._id;

                    //Don't reward for teamkills
                    if (vehKiller._team == victim._team)
                        Logic_Assets.RunEvent(vehKiller, _arena._server._zoneConfig.EventInfo.killedTeam);
                    else
                        Logic_Assets.RunEvent(vehKiller, _arena._server._zoneConfig.EventInfo.killedEnemy);

                    //Increase stats/points and notify arena of the kill!
                    if (_points != null)
                        _points[vehKiller._team] += _pointSmallChange;
                    vehKiller.Kills++;
                    victim.Deaths++;
                    Logic_Rewards.calculatePlayerKillRewards(victim, vehKiller, update);
                    return false;
				}
            }
            return true;
		}

		/// <summary>
		/// Triggered when one player has killed another
		/// </summary>
		[Scripts.Event("Player.PlayerKill")]
		public bool playerPlayerKill(Player victim, Player killer)
		{
			return true;
		}

		/// <summary>
		/// Triggered when a bot has killed a player
		/// </summary>
		[Scripts.Event("Player.BotKill")]
		public bool playerBotKill(Player victim, Bot bot)
		{
			return true;
		}

		/// <summary>
		/// Triggered when a computer vehicle has killed a player
		/// </summary>
		[Scripts.Event("Player.ComputerKill")]
		public bool playerComputerKill(Player victim, Computer computer)
		{
			return true;
		}

		/// <summary>
		/// Triggered when a player attempts to use a warp item
		/// </summary>
		[Scripts.Event("Player.WarpItem")]
		public bool playerWarpItem(Player player, ItemInfo.WarpItem item, ushort targetPlayerID, short posX, short posY)
		{                
			return true;
		}

		/// <summary>
		/// Triggered when a player attempts to use a warp item
		/// </summary>
		[Scripts.Event("Player.MakeVehicle")]
		public bool playerMakeVehicle(Player player, ItemInfo.VehicleMaker item, short posX, short posY)
		{
			return true;
		}

		/// <summary>
		/// Triggered when a player attempts to use a warp item
		/// </summary>
		[Scripts.Event("Player.MakeItem")]
		public bool playerMakeItem(Player player, ItemInfo.ItemMaker item, short posX, short posY)
		{
			return true;
		}

        /// <summary>
        /// Triggered when a player uses a repair item
        /// </summary>
        [Scripts.Event("Player.Repair")]
        public bool playerRepair(Player player, ItemInfo.RepairItem item, UInt16 targetVehicle, short posX, short posY)
        {
            int healamount = 0;
            //Let's try to credit him for the heal
            if (item.repairType == 0 || item.repairType == 2)
            {   //It's a player heal!
                if (item.repairDistance > 0)
                    //Credit him for a single heal
                    healamount = (item.repairAmount == 0) ? item.repairPercentage : item.repairAmount;
                else if (item.repairDistance < 0)
                    //Credit him for everybody he healed
                    healamount = (item.repairAmount == 0)
                        ? item.repairPercentage * _arena.getPlayersInRange(player._state.positionX, player._state.positionY, -item.repairDistance).Count
                        : item.repairAmount *_arena.getPlayersInRange(player._state.positionX, player._state.positionY, -item.repairDistance).Count;
            }

            //Keep track of it, mang
            if(_healingDone != null)
                if (_healingDone.ContainsKey(player))
                    _healingDone[player] += healamount;
                else
                    _healingDone.Add(player, healamount);
            return true;
        }

		/// <summary>
		/// Triggered when a player is buying an item from the shop
		/// </summary>
		[Scripts.Event("Shop.Buy")]
		public bool shopBuy(Player patron, ItemInfo item, int quantity)
		{
			return true;
		}

		/// <summary>
		/// Triggered when a player is selling an item to the shop
		/// </summary>
		[Scripts.Event("Shop.Sell")]
		public bool shopSell(Player patron, ItemInfo item, int quantity)
		{
			return true;
		}

        /// <summary>
        /// Triggered when a player requests a skill from skill screen (F11)
        /// </summary>
        [Scripts.Event("Shop.SkillRequest")]
        public bool shopSkillRequest(Player player, SkillInfo skill)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a player successfully purchases a skill item from skill screen (F11)
        /// </summary>
        [Scripts.Event("Shop.SkillPurchase")]
        public void shopSkillPurchase(Player player, SkillInfo skill)
        {   //Is it a class or an attribute?
            if (skill.SkillId >= 0)
            {   //It's a class change, let's look for any computer vehicles he might have owned...
                foreach (Vehicle v in _arena.Vehicles)
                    if (v._type.Type == VehInfo.Types.Computer && v._creator == player)
                        //Destroy it!
                        v.destroy(true);
            }
        }

		/// <summary>
		/// Triggered when a vehicle is created
		/// </summary>
		/// <remarks>Doesn't catch spectator or dependent vehicle creation</remarks>
		[Scripts.Event("Vehicle.Creation")]
		public bool vehicleCreation(Vehicle created, Team team, Player creator)
		{
			return true;
		}

		/// <summary>
		/// Triggered when a vehicle dies
		/// </summary>
		[Scripts.Event("Vehicle.Death")]
		public bool vehicleDeath(Vehicle dead, Player killer)
		{
			return true;
		}
		#endregion
	}
}