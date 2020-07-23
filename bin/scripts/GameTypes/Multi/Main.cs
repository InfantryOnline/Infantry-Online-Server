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
{   // Script Class
    /// Provides the interface between the script and arena
    ///////////////////////////////////////////////////////
    partial class Script_Multi : Scripts.IScript
    {   ///////////////////////////////////////////////////
        // Member Variables
        ///////////////////////////////////////////////////
        private Arena _arena;                   //Pointer to our arena class
        private CfgInfo _config;				//The zone config
        private Settings.GameTypes _gameType;
        public Loot _loot;
        public Crafting _crafting;
        private List<string> _earlyAccessList;
        private bool _isEarlyAccess;
        public bool bJackpot;
        public Dictionary<ushort, LootDrop> _privateLoot;
        public Dictionary<ushort, LootDrop> _condemnedLoot;
        public Database _database;

        //Addon Classes
        Upgrade _upgrader;


        //Poll variables
        public int _lastGameCheck;         //Tick at which we checked for game availability
        public int _tickGameStarting;      //Tick at which the game began starting (0 == not initiated)
        public int _tickGameStarted;       //Tick at which the game actually started (0 == stopped)
        public int _tickLastMinorPoll;
        public int _lastKillStreakUpdate;
        public int _lastLootMarker;

        //Misc variables
        public int _tickStartDelay;
        public int _minPlayers;            //Do we have the # of min players to start a game?
        public bool _bMiniMapsEnabled;
        public Team _winner;
        private Player lastKiller;
        public List<SupplyDrop> _supplyDrops;
        private List<Player> _fakePlayers;
        public int manualTeamSizePick;

        //Stats
        /// <summary>
        /// Current game player stats
        /// </summary>
        public Dictionary<string, Stats> _savedPlayerStats;

        /// <summary>
        /// Our gametype handlers
        /// </summary>
        private Conquest _cq;
        public Coop _coop;
        public Royale _royale;
        public RTS _rts;

        #region Member Fucntions
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
            _upgrader = new Upgrade();
            bJackpot = true;

            //Load up our gametype handlers
            _cq = new Conquest(_arena, this);
            _coop = new Coop(_arena, this);
            _royale = new Royale(_arena, this);
            _rts = new RTS(_arena, this);
            

            //Load any modules
            _loot = new Loot(_arena, this);
            _crafting = new Crafting();
            _loot.Load(_arena._server);
            _privateLoot = new Dictionary<ushort, LootDrop>();
            _condemnedLoot = new Dictionary<ushort, LootDrop>();

            //Default to Conquest
            _gameType = Settings.GameTypes.Coop;
            _minPlayers = 1;

            _isEarlyAccess = false;

            if (_arena._name.StartsWith("[Co-Op]"))
                _gameType = Settings.GameTypes.Coop;
            else if (_arena._name.StartsWith("[Royale]"))
                _gameType = Settings.GameTypes.Royale;
            else if (_arena._name.ToLower().StartsWith("[rts]"))
                _gameType = Settings.GameTypes.RTS;
            else
            {
                Team team1 = _arena.getTeamByName("Titan Militia");
                Team team2 = _arena.getTeamByName("Collective Military");
                _cq.setTeams(team1, team2, false);
            }

            _fakePlayers = new List<Player>();
            _savedPlayerStats = new Dictionary<string, Stats>();
            _lastSpawn = new Dictionary<string, Helpers.ObjectState>();
            _bMiniMapsEnabled = true;
            _supplyDrops = new List<SupplyDrop>();
            manualTeamSizePick = 0;
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
            bool bMinor = (now - _tickLastMinorPoll) >= 1000;


            if (bMinor)
                //Poll events
                pollEvents(now);


            //Keep track of expired items
            foreach (LootDrop loot in _privateLoot.Values)
                if (loot._item.tickExpire != 0 && now > loot._item.tickExpire)
                    _condemnedLoot.Add(loot._id, loot);
            //Buhbye
            foreach (LootDrop item in _condemnedLoot.Values)
                _privateLoot.Remove(item._id);
            _condemnedLoot.Clear();

            //Loot marking
            if (_privateLoot.Count > 0 && now - _lastLootMarker >= 1000)
            {
                foreach (LootDrop loot in _privateLoot.Values)
                {
                    List<Player> targets = new List<Player>();
                    targets.Add(loot._owner);

                    //If the item is still reserved, mark it
                    if (now - loot._tickCreation < Settings.c_unblockLoot)
                    Helpers.Player_RouteExplosion(targets, 1406, loot._item.positionX, loot._item.positionY, 0, 0, 0);

                    _lastLootMarker = now;
                }
            }

            //Update each player's playseconds
            if (_arena._bGameRunning && now - _tickLastMinorPoll >= 1000)
            {
                _tickLastMinorPoll = now;
                foreach (Player player in _arena.PlayersIngame)
                {
                    if (player._team._name == "Titan Militia")
                        StatsCurrent(player).titanPlaySeconds = StatsCurrent(player).titanPlaySeconds + 1;
                    if (player._team._name == "Collective Military")
                        StatsCurrent(player).collectivePlaySeconds = StatsCurrent(player).collectivePlaySeconds + 1;
                }
            }

            if (now - _lastKillStreakUpdate >= 500)
            {
                UpdateKillStreaks();
                _lastKillStreakUpdate = now;
            }

            //Pool our supply drops
            foreach (SupplyDrop drop in _supplyDrops)
                drop.poll(now);

            //Poll our current gametype!
            switch (_gameType)
            {
                case Settings.GameTypes.Conquest:
                    _cq.Poll(now);
                    break;
                case Settings.GameTypes.Coop:
                    _coop.Poll(now);
                    break;
                case Settings.GameTypes.Royale:
                    _royale.Poll(now);
                    break;
                case Settings.GameTypes.RTS:
                    _rts.Poll(now);
                    break;

                default:
                    //Do nothing
                    break;
            }

            return true;
        }
        #endregion

        #region Events

        [Scripts.Event("Player.Portal")]
        public bool playerPortal(Player player, LioInfo.Portal portal)
        {
            switch (_gameType)
            {
                case Settings.GameTypes.Conquest:
                    return _cq.playerPortal(player, portal);
                case Settings.GameTypes.Coop:
                    return _coop.playerPortal(player, portal);
                case Settings.GameTypes.Royale:
                    return _royale.playerPortal(player, portal);
                case Settings.GameTypes.RTS:
                    return _rts.playerPortal(player, portal);

                default:
                    //Do nothing
                    break;
            }
            return false;
        }

        /// <summary>
        /// Triggered when a player attempts to use a warp item
        /// </summary>
        [Scripts.Event("Player.MakeVehicle")]
        public bool playerMakeVehicle(Player player, ItemInfo.VehicleMaker item, short posX, short posY)
        {
            switch (_gameType)
            {
                case Settings.GameTypes.RTS:
                    return _rts.playerMakeVehicle(player, item, posX, posY);

                default:
                    //Do nothing
                    break;
            }
            return true;
        }

        [Scripts.Event("Player.WarpItem")]
        public bool playerWarpItem(Player player, ItemInfo.WarpItem item, ushort targetVehicle, short posX, short posY)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a player notifies the server of an explosion
        /// </summary>
        [Scripts.Event("Player.DamageEvent")]
        public bool playerDamageEvent(Player player, ItemInfo.Projectile weapon, short posX, short posY, short posZ)
        {
            
            return true;
        }

        /// <summary>
        /// Triggered when a player notifies the server of an explosion
        /// </summary>
        [Scripts.Event("Player.Explosion")]
        public bool playerExplosion(Player player, ItemInfo.Projectile weapon, short posX, short posY, short posZ)
        {
            switch (weapon.id)
            {
                //Supply Drop?
                case 1319:
                    {
                        spawnSupplyDrop(player._team, posX, posY);
                    }
                    break;
            }

            //Defer to our current gametype handler!
            switch (_gameType)
            {
                case Settings.GameTypes.Conquest:
                    _cq.playerExplosion(player, weapon, posX, posY, posZ);
                    break;
                case Settings.GameTypes.Coop:
                    _coop.playerExplosion(player, weapon, posX, posY, posZ);
                    break;
                case Settings.GameTypes.Royale:
                    //_royale.playerExplosion(player, weapon, posX, posY, posZ);
                    break;

                default:
                    //Do nothing
                    break;
            }
          
            return true;
        }

        /// <summary>
        /// Triggered when a player wants to unspec and join the game
        /// </summary>
        [Scripts.Event("Player.JoinGame")]
        public bool playerJoinGame(Player player)
        {
            if (StatsCurrent(player) == null)
                createPlayerStats(player);


            //Mark him as playing
            StatsCurrent(player).hasPlayed = true;

            bool handler = true;
            //Defer to our current gametype handler!
            switch (_gameType)
            {
                case Settings.GameTypes.Conquest:
                    handler = _cq.playerUnspec(player);
                    break;
                case Settings.GameTypes.Coop:
                    handler = _coop.playerUnspec(player);
                    break;
                case Settings.GameTypes.Royale:
                    handler = _royale.playerJoinGame(player);
                    break;
                case Settings.GameTypes.RTS:
                    handler = _rts.playerJoinGame(player);
                    break;

                default:
                    //Do nothing
                    break;
            }
            return handler;
        }

        /// <summary>
        /// Called when a player enters the game
        /// </summary>
        [Scripts.Event("Player.Enter")]
        public void playerEnter(Player player)
        {
            if (StatsCurrent(player) == null)
                createPlayerStats(player);

            //Mark him as playing
            StatsCurrent(player).hasPlayed = true;
        }

        /// <summary>
        /// Called when a player leaves the game
        /// </summary>
        [Scripts.Event("Player.Leave")]
        public bool playerLeave(Player player)
        {
            //Defer to our current gametype handler!
            switch (_gameType)
            {
                case Settings.GameTypes.Conquest:
                    _cq.playerSpec(player);
                    break;
                case Settings.GameTypes.Coop:
                    _coop.playerSpec(player);
                    break;
                case Settings.GameTypes.Royale:
                    _royale.playerLeave(player);
                    break;
                case Settings.GameTypes.RTS:
                    _rts.playerLeave(player);
                    break;


                default:
                    //Do nothing
                    break;
            }
            return false;
        }
        

        /// <summary>
        /// Called when a player enters the arena
        /// </summary>
        [Scripts.Event("Player.EnterArena")]
        public void playerEnterArena(Player player)
        {
            //Fix this later
            if (_arena.TotalPlayerCount == 1 && player._permissionStatic < Data.PlayerPermission.Mod && _arena._bIsPublic)
                player._permissionTemp = Data.PlayerPermission.Normal;

            //Read our list
            _earlyAccessList = ListReader.readListFromFile("earlyaccess.txt");


            player.sendMessage(0,
                "&NOTICE: All invididual co-op arenas have been removed. Public1 now defaults to co-op mode at the normal difficulty level. With this, " +
                "hopefully we'll see some more stability in the zone as the public arenas are able to close/restart when they hit 0 players.");


            if (_isEarlyAccess)
            {
                if (!_earlyAccessList.Contains(player._alias))
                {
                    _earlyAccessList.Add(player._alias);
                    ListReader.saveListToFile(_earlyAccessList, "earlyaccess.txt");

                    player.sendMessage(4,
                        "#You have been added to our early access list for testing the zone while it's under development. " +
                        "Once the zone is wiped and goes live, You will be given an early access bonus when you login for the first time. Thanks!");
                }
            }
            else
            {
                if (_earlyAccessList.Contains(player._alias))
                {
                    _earlyAccessList.Remove(player._alias);
                    ListReader.saveListToFile(_earlyAccessList, "earlyaccess.txt");

                    //Lucky fella
                    player.Cash += 50000;
                    player.syncState();
                    //Let him know!
                    player.sendMessage(4,
                        String.Format("#Thanks for testing! Here is your early access bonus: (Cash=50000)"));
                }
            }


            if (StatsCurrent(player) == null)
                createPlayerStats(player);

            //Defer to our current gametype handler!
            switch (_gameType)
            {
                case Settings.GameTypes.Conquest:
                    _cq.playerEnterArena(player);
                    break;
                case Settings.GameTypes.Coop:
                    _coop.playerEnterArena(player);
                    break;
                case Settings.GameTypes.Royale:
                    _royale.playerEnterArena(player);
                    break;
                case Settings.GameTypes.RTS:
                    _rts.playerEnterArena(player);
                    break;

                default:
                    //Do nothing
                    break;
            }
        }

        /// <summary>
        /// Called when a player leaves the arena
        /// </summary>
        [Scripts.Event("Player.LeaveArena")]
        public void playerLeaveArena(Player player)
        {
            //Defer to our current gametype handler!
            switch (_gameType)
            {
                case Settings.GameTypes.Conquest:
                    _cq.playerLeaveArena(player);
                    break;
                case Settings.GameTypes.Coop:
                    _coop.playerLeaveArena(player);
                    break;
                case Settings.GameTypes.Royale:
                    _royale.playerLeaveArena(player);
                    break;
                case Settings.GameTypes.RTS:
                    _rts.playerLeaveArena(player);
                    break;


                default:
                    //Do nothing
                    break;
            }
        }

        /// <summary>
        /// Triggered when a player has spawned
        /// </summary>
        [Scripts.Event("Player.Spawn")]
        public bool playerSpawn(Player player, bool death)
        {
            //Defer to our current gametype handler!
            switch (_gameType)
            {
                case Settings.GameTypes.Conquest:
                    return _cq.playerSpawn(player, death);
                case Settings.GameTypes.Coop:
                    return _coop.playerSpawn(player, death);
                case Settings.GameTypes.Royale:
                    return _royale.playerSpawn(player, death);
                case Settings.GameTypes.RTS:
                    return _rts.playerSpawn(player, death);

                default:
                    //Do nothing
                    break;
            }
            return true;
        }

        /// <summary>
        /// Called when the game begins
        /// </summary>
        [Scripts.Event("Game.Start")]
        public bool gameStart()
        {
            _tickGameStarting = 0;
            _tickGameStarted = Environment.TickCount;
            ResetKiller(null);

            if (_arena._bIsPublic)
                bJackpot = true;

            if (_arena.ActiveTeams.Count() == 0)
                return false;

            _supplyDrops.Clear();

            _arena.initialHideSpawns();

            _savedPlayerStats = new Dictionary<string, Stats>();

            foreach (Player player in _arena.Players)
                createPlayerStats(player);

            //Defer to our current gametype handler!
            switch (_gameType)
            {
                case Settings.GameTypes.Conquest:
                    _cq.gameStart();
                    break;
                case Settings.GameTypes.Coop:
                    _coop.gameStart();
                    break;
                case Settings.GameTypes.Royale:
                    _royale.gameStart();
                    break;


                default:
                    //Do nothing
                    break;
            }
            return true;
        }

        /// <summary>
        /// Called when the game ends
        /// </summary>
        [Scripts.Event("Game.End")]
        public bool gameEnd()
        {   //Game finished, perhaps start a new one
            _tickGameStarted = 0;
            _tickGameStarting = 0;

            //Clean up any bots
            //foreach (Bot bot in _coop._bots)
                //bot.destroy(false);

            //foreach (Bot bot in _cq._bots)
                //bot.destroy(false);

            //Defer to our current gametype handler!
            switch (_gameType)
            {
                case Settings.GameTypes.Conquest:
                    _cq.gameEnd();
                    break;
                case Settings.GameTypes.Coop:
                    _coop.gameEnd();
                    break;
                case Settings.GameTypes.Royale:
                    _royale.gameEnd();
                    break;


                default:
                    //Do nothing
                    break;
            }

            //Are we counting this game for rewards?
            if (!bJackpot)
                return true;

            //Calculate our jacpot
            Rewards.Jackpot jackpot = Rewards.calculateJackpot(_arena.Players, _winner, _gameType, this, false);
            _arena.sendArenaMessage(String.Format("&Jackpot: {0}", jackpot.totalJackPot));

            List<Rewards.Jackpot.Reward> rankers = new List<Rewards.Jackpot.Reward>();

            foreach (Rewards.Jackpot.Reward reward in jackpot._playerRewards.OrderByDescending(r => r.MVP))
            {
                if (reward.Score == 0)
                    continue;

                rankers.Add(reward);
            }

            int idx = 3;
            foreach (Rewards.Jackpot.Reward reward in rankers)
            {
                if (reward.player == null) continue;

                if (idx-- <= 0)
                    break;

                string placeWord = "&3rd";
                string format = " (MVP Percentage={0}%): {1}";
                switch (idx)
                {
                    case 2:
                        placeWord = "&1st";
                        break;
                    case 1:
                        placeWord = "&2nd";
                        break;
                }

                _arena.sendArenaMessage(string.Format(placeWord + format, Math.Round(reward.MVP * 100, 2), reward.player._alias));
            }

            idx = 1;
            foreach (Rewards.Jackpot.Reward reward in rankers)
            {
                if (reward.player == null) continue;





                reward.cash = Rewards.addCash(reward.player, reward.cash, _gameType);
                reward.player.sendMessage(0, String.Format("Your personal Jackpot: (MVP={0}% Rank={1} Score={2}) Rewards: (Cash={3} Experience={4} Points={5})",
                Math.Round(reward.MVP * 100, 2), idx, reward.Score, reward.cash, reward.experience, reward.points));
                reward.player.Experience += reward.experience;
                reward.player.BonusPoints += reward.points;

                //Adjust depending on difficulty for coop
                if (_gameType == Settings.GameTypes.Coop && _winner == _coop._team)
                {
                    int bonusCash = 0;
                    int bonusExp = 0;
                    int bonusPoints = 0;
                    double mod = Rewards.calculateDiffMod(reward.player.Points, _coop._botDifficulty + _coop._botDifficultyPlayerModifier);
                    bonusCash = Convert.ToInt32(((jackpot.totalJackPot * Settings.c_difficulty_CashMultiplier) * mod));
                    bonusExp = Convert.ToInt32(((jackpot.totalJackPot * Settings.c_difficulty_ExpMultiplier) * mod));
                    bonusPoints = Convert.ToInt32(((jackpot.totalJackPot * Settings.c_difficulty_PtsMultiplier) * mod));

                    reward.player.sendMessage(0, String.Format("Victory difficulty Bonus: (Cash={0} Experience={1} Points={2})", bonusCash, bonusExp, bonusPoints));
                    reward.player.Cash += bonusCash;
                    reward.player.Experience += bonusExp;
                    reward.player.BonusPoints += bonusPoints;
                    reward.player.syncState();
                }

                idx++;
            }
            return true;
        }

        /// <summary>
        /// Called when the statistical breakdown is displayed
        /// </summary>
        [Scripts.Event("Game.Breakdown")]
        public bool gameBreakdown()
        {	//Allows additional "custom" breakdown information

            //Always return true;
            return true;
        }

        /// <summary>
        /// Called to reset the game state
        /// </summary>
        [Scripts.Event("Game.Reset")]
        public bool gameReset()
        {
            //Defer to our current gametype handler!
            switch (_gameType)
            {
                case Settings.GameTypes.Conquest:
                    _cq.gameReset();
                    break;
                case Settings.GameTypes.Coop:
                    _coop.gameReset();
                    break;
                case Settings.GameTypes.Royale:
                    _royale.gameReset();
                    break;

                default:
                    //Do nothing
                    break;
            }
            return true;
        }

        /// <summary>
        /// Triggered when a player requests to drop an item
        /// </summary>
        [Scripts.Event("Player.ItemDrop")]
        public bool playerItemDrop(Player player, ItemInfo item, ushort quantity)
        {
            //Defer to our current gametype handler!
            switch (_gameType)
            {
                case Settings.GameTypes.Conquest:
                    break;
                case Settings.GameTypes.Coop:
                    break;
                case Settings.GameTypes.RTS:
                    return _rts.playerItemDrop(player, item, quantity);

                default:
                    //Do nothing
                    break;
            }
            return true;
        }

        /// <summary>
        /// Triggered when a player requests to pick up an item
        /// </summary>
        [Scripts.Event("Player.ItemPickup")]
        public bool playerItemPickup(Player player, Arena.ItemDrop drop, ushort quantity)
        {
            int now = Environment.TickCount;


            //Private loot?
            if (_privateLoot.ContainsKey(drop.id))
            {
                LootDrop loot = _privateLoot[drop.id];
                if (drop.owner != player && now - loot._tickCreation < Settings.c_unblockLoot)
                {
                    player.sendMessage(0, "You can't pick up another players loot unless it was dropped by a player");
                    return false;
                }
                else
                    _privateLoot.Remove(drop.id);
            }


            if (quantity == drop.quantity)
            {   //Delete the drop
                drop.quantity = 0;
                _arena._items.Remove(drop.id);
            }
            else
                drop.quantity = (short)(drop.quantity - quantity);

            //Add the pickup to inventory!
            player.inventoryModify(drop.item, quantity);

            //Update his bounty.
            if (drop.owner != player) //Bug abuse fix for people dropping and picking up items to get bounty
                player.Bounty += drop.item.prizeBountyPoints;

            if (_privateLoot.ContainsKey(drop.id))
                _privateLoot.Remove(drop.id);

            //Remove the item from player's clients
            Helpers.Object_ItemDropUpdate(_arena.Players, drop.id, (ushort)drop.quantity);

            if (drop.item.name.EndsWith("(Loot)"))
            {
                ItemInfo.MultiItem multi = (ItemInfo.MultiItem)drop.item;

                //Ammo stuffs
                player.inventoryModify(multi.slots[1].value, 150);

            }

            //Defer to our current gametype handler!
            switch (_gameType)
            {
                case Settings.GameTypes.Conquest:
                    break;
                case Settings.GameTypes.Coop:
                    break;
                case Settings.GameTypes.RTS:
                    _rts.playerItemPickup(player, drop, quantity);
                    break;

                default:
                    //Do nothing
                    break;
            }

            return false;
        }

        /// <summary>
        /// Handles a player's flag request
        /// </summary>
        [Scripts.Event("Player.FlagAction")]
        public bool playerFlagAction(Player player, bool bPickup, bool bInPlace, LioInfo.Flag flag)
        {
            //Defer to our current gametype handler!
            switch (_gameType)
            {
                case Settings.GameTypes.Conquest:
                    return _cq.playerFlagAction(player, bPickup, bInPlace, flag);
                case Settings.GameTypes.Coop:
                    return _coop.playerFlagAction(player, bPickup, bInPlace, flag);
                default:
                    //Do nothing
                    break;
            }
            return true;
        }

        /// <summary>
        /// Called when the statistical breakdown is displayed
        /// </summary>
        [Scripts.Event("Player.Breakdown")]
        public bool individualBreakdown(Player from, bool bCurrent)
        {
            //Defer to our current gametype handler!
            switch (_gameType)
            {
                case Settings.GameTypes.Conquest:
                    _cq.individualBreakdown(from, bCurrent);
                    break;
                case Settings.GameTypes.Coop:
                    _coop.individualBreakdown(from, bCurrent);
                    break;
                case Settings.GameTypes.Royale:
                    _royale.playerBreakdown(from, bCurrent);
                    break;

                default:
                    //Do nothing
                    break;
            }

            from.sendMessage(0, "#Individual Statistics Breakdown");
            int idx = 3;        //Only display the top 3 players
            List<Player> rankers = new List<Player>();
            foreach (Player p in _arena.Players.ToList())
            {
                if (p == null)
                    continue;
                if (StatsCurrent(p) != null && StatsCurrent(p).hasPlayed)
                    rankers.Add(p);
            }

            if (rankers.Count > 0)
            {
                var rankedPlayerGroups = rankers.Select(player => new
                {
                    Alias = player._alias,
                    Kills = StatsCurrent(player).kills,
                    Deaths = StatsCurrent(player).deaths
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
                        from.sendMessage(0, string.Format(placeWord + format, group.First().Kills,
                            group.First().Deaths, string.Join(", ", group.Select(g => g.Alias))));
                }

                IEnumerable<Player> specialPlayers = rankers.OrderByDescending(player => StatsCurrent(player).deaths);
                int topDeaths = (specialPlayers.First() != null ? StatsCurrent(specialPlayers.First()).deaths : 0), deaths = 0;
                if (topDeaths > 0)
                {
                    from.sendMessage(0, "Most Deaths");
                    int i = 0;
                    List<string> mostDeaths = new List<string>();
                    foreach (Player p in specialPlayers)
                    {
                        if (p == null)
                            continue;

                        if (p.getStats() != null)
                        {
                            deaths = p.getStats().deaths;
                            if (deaths == topDeaths)
                            {
                                if (i++ >= 1)
                                    mostDeaths.Add(p._alias);
                                else
                                    mostDeaths.Add(string.Format("(D={0}): {1}", deaths, p._alias));
                            }
                        }
                    }
                    if (mostDeaths.Count > 0)
                    {
                        string s = string.Join(", ", mostDeaths.ToArray());
                        from.sendMessage(0, s);
                    }
                }

                IEnumerable<Player> Healed = rankers.Where(player => StatsCurrent(player).potentialHealthHealed > 0);
                if (Healed.Count() > 0)
                {
                    IEnumerable<Player> mostHealed = Healed.OrderByDescending(player => StatsCurrent(player).potentialHealthHealed);
                    idx = 3;
                    from.sendMessage(0, "&Most HP Healed");
                    foreach (Player p in mostHealed)
                    {
                        if (p == null) continue;
                        if (StatsCurrent(p) != null)
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
                            from.sendMessage(0, string.Format(placeWord + format, StatsCurrent(p).potentialHealthHealed, p._alias));
                        }
                    }
                }
            }

            //Are they on the list?
            if (StatsCurrent(from) != null)
            {
                string personalFormat = "!Personal Score: (K={0} D={1})";
                from.sendMessage(0, string.Format(personalFormat,
                    StatsCurrent(from).kills,
                    StatsCurrent(from).deaths));
            }
            //If not, give them the generic one
            else
            {
                string personalFormat = "!Personal Score: (K=0 D=0)";
                from.sendMessage(0, personalFormat);
            }

            return true;
        }

        /// <summary>
        /// Handles a player's produce request
        /// </summary>
        [Scripts.Event("Player.Produce")]
        public bool playerProduce(Player player, Computer computer, VehInfo.Computer.ComputerProduct product)
        {
            switch (computer._type.Name)
            {
                case "Supply Drop":
                    {
                        if (product.Title == "Open Supplies")
                        {
                            SupplyDrop drop = _supplyDrops.First(s => s._computer == computer);

                            if (drop == null)
                                return true;

                            drop.open(player);
                        }
                    }
                    break;
                case "Blacksmith":
                    {
                        string itemName;

                        if (product.Title.StartsWith("Build"))
                        {
                            itemName = product.Title.Substring(5, product.Title.Length - 5).Trim();

                            return _crafting.playerCraftItem(player, itemName);
                        }
                    }
                    break;
                case "Engineer [Fortification]":
                    return tryEngineerFort(player, computer, product);
                case "Engineer [Vehicles]":
                    return tryEngineerVehicle(player, computer, product);
                case "Iron Refinery":
                    return tryIronRefinery(player, computer, product);
                case "Helmet Trader":
                    return tryHelmetTrade(player, computer, product);
            }

            switch (_gameType)
            {
                case Settings.GameTypes.RTS:
                    _rts.playerProduce(player, computer, product);
                    break;
                default:
                    //Do nothing
                    break;
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
            //Reset stored bounty
            //victim.ZoneStat1 = 0;
           
            //Defer to our current gametype handler!
            switch (_gameType)
            {
                case Settings.GameTypes.Conquest:
                    _cq.playerDeath(victim, killer, killType, update);
                    break;
                case Settings.GameTypes.Coop:
                    victim.ZoneStat1 = 0;
                    _coop.playerDeath(victim, killer, killType, update);
                    break;
                case Settings.GameTypes.Royale:
                    _royale.playerDeath(victim, killer, killType, update);
                    break;
                case Settings.GameTypes.RTS:
                    _rts.playerDeath(victim, killer, killType, update);
                    break;
                default:
                    //Do nothing
                    break;
            }
            return true;
        }

        /// <summary>
        /// Triggered when one player has killed another
        /// </summary>
        [Scripts.Event("Player.PlayerKill")]
        public bool playerPlayerKill(Player victim, Player killer)
        {
            //Don't reward for teamkills
            if (victim._team == killer._team)
                Logic_Assets.RunEvent(victim, _arena._server._zoneConfig.EventInfo.killedTeam);
            else
            {
                Logic_Assets.RunEvent(victim, _arena._server._zoneConfig.EventInfo.killedEnemy);
                //Calculate rewards
                Rewards.calculatePlayerKillRewards(victim, killer, _gameType);
            }

            //Update stats
            killer.Kills++;
            victim.Deaths++;
            //killer.ZoneStat1 = killer.Bounty;

            //Update our kill streak
            UpdateKiller(killer);
            UpdateDeath(victim, killer);


            StatsCurrent(killer).kills++;
            StatsCurrent(victim).deaths++;

            long wepTick = StatsCurrent(killer).lastUsedWepTick;
            if (wepTick != -1)
                UpdateWeaponKill(killer);

            if (killer != null && victim != null && victim._bounty >= 1200)
                _arena.sendArenaMessage(string.Format("{0} has ended {1}'s bounty.", killer._alias, victim._alias), 8);


            //Check for players in the share radius
            List<Player> playersInRadius = _arena.getPlayersInRange(victim._state.positionX, victim._state.positionY, 450, false)
                .Where(p => p._team != victim._team && p != victim).ToList();

            //Killer is always added...
            if (!playersInRadius.Contains(killer))
                playersInRadius.Add(killer);

            //Try some lootin
            _loot.tryLootDrop(playersInRadius, victim, victim._state.positionX, victim._state.positionY);

            //Now defer to our current gametype handler!
            switch (_gameType)
            {
                case Settings.GameTypes.Conquest:
                    _cq.playerPlayerKill(victim, killer);
                    break;
                case Settings.GameTypes.Coop:
                    killer.ZoneStat1 = killer.Bounty;
                    _coop.playerPlayerKill(victim, killer);
                    break;
                case Settings.GameTypes.Royale:
                    _royale.playerPlayerKill(victim, killer);
                    break;
                case Settings.GameTypes.RTS:
                    _rts.playerPlayerKill(victim, killer);
                    break;

                default:
                    //Do nothing
                    break;
            }
            return false;
        }

        /// <summary>
        /// Triggered when a bot has killed a player
        /// </summary>
        [Scripts.Event("Player.BotKill")]
        public bool playerBotKill(Player victim, Bot bot)
        {
            UpdateDeath(victim, null);
            StatsCurrent(victim).deaths++;
            //Update our base zone stats
            victim.Deaths++;
            //victim.ZoneStat1 = 0;

            //Now defer to our current gametype handler!
            switch (_gameType)
            {
                case Settings.GameTypes.Conquest:
                    _cq.playerDeathBot(victim, bot);
                    break;
                case Settings.GameTypes.Coop:
                    victim.ZoneStat1 = 0;
                    _coop.playerDeathBot(victim, bot);
                    break;
                case Settings.GameTypes.RTS:
                   // _rts.playerDeathBot(victim, bot);
                    break;

                default:
                    //Do nothing
                    break;
            }
            return true;
        }


        /// <summary>
        /// Triggered when a vehicle dies
        /// </summary>
        [Scripts.Event("Bot.Death")]
        public bool botDeath(Bot dead, Player killer, int weaponID)
        {

            if (killer != null)
            {
                Helpers.Vehicle_RouteDeath(_arena.Players, killer, dead, null);
                if (killer != null && dead._team != killer._team)
                {//Don't allow rewards for team kills
                    Rewards.calculateBotKillRewards(dead, killer, _gameType);
                }

                killer.Kills++;
                UpdateKiller(killer);
                //killer.ZoneStat1 = killer.Bounty;

                StatsCurrent(killer).kills++;
                long wepTick = StatsCurrent(killer).lastUsedWepTick;
                if (wepTick != -1)
                    UpdateWeaponKill(killer);

                //Now defer to our current gametype handler!
                switch (_gameType)
                {
                    case Settings.GameTypes.Conquest:
                        _cq.botDeath(dead, killer);
                        break;
                    case Settings.GameTypes.Coop:
                        killer.ZoneStat1 = killer.Bounty;
                        _coop.botDeath(dead, killer);
                        break;
                    case Settings.GameTypes.RTS:
                        _rts.botDeath(dead, killer);
                        break;
                        

                    default:
                        //Do nothing
                        break;
                }
            }
            else
                Helpers.Vehicle_RouteDeath(_arena.Players, null, dead, null);

            //Check for players in the share radius
            List<Player> playersInRadius = _arena.getPlayersInRange(dead._state.positionX, dead._state.positionY, 600, false);

            //Killer is always added...
            if (!playersInRadius.Contains(killer))
                playersInRadius.Add(killer);

            //Try some lootin
            _loot.tryLootDrop(playersInRadius, dead, dead._state.positionX, dead._state.positionY);

            return false;
        }

        /// <summary>
        /// Triggered when a player is buying an item from the shop
        /// </summary>
        [Scripts.Event("Shop.Buy")]
        public bool shopBuy(Player patron, ItemInfo item, int quantity)
        {
            if (item.name.StartsWith("Upgrade"))
            {
                string upgradeItem = item.name.Split(':')[1].TrimStart();

                if (patron._inventory.Values.Count(itm => itm.item.name.Contains(upgradeItem)) == 0)
                {
                    patron.sendMessage(-1, "You're not allowed to upgrade items you don't own");
                    return false;
                }

                ItemInfo currentItem = patron._inventory.Values.First(itm => itm.item.name.Contains(upgradeItem)).item;
                ItemInfo newItem = _upgrader.tryItemUpgrade(patron, currentItem.name);

                //Success?
                if (newItem != null)
                {
                    patron.inventoryModify(currentItem, -1);
                    patron.inventoryModify(newItem, 1);
                }
                return true;
            }
            return true;
        }

        /// <summary>
        /// Triggered when a player requests to buy a skill
        /// </summary>
        [Scripts.Event("Shop.SkillRequest")]
        public bool PlayerShopSkillRequest(Player from, SkillInfo skill)
        {

            if (!_arena._bIsPublic)
            {
            }
            //Defer to our current gametype handler!

            if ((skill.SkillId >= 0) && (skill.SkillId < 100)) // they trying to pick  class
                from.resetSkills();

            switch (_gameType)
            {
                case Settings.GameTypes.Conquest:
                    // _cq.PlayerRepair(from, item);
                    break;

                default:
                    //Do nothing
                    break;
            }

            return true;
        }

        /// <summary>
        /// Triggers when a repair item is used
        /// </summary>
        [Scripts.Event("Player.Repair")]
        public bool playerPlayerRepair(Player player, ItemInfo.RepairItem item, UInt16 target, short posX, short posY)
        {
            //Healing points
            if (item.repairType == 0 && item.repairDistance < 0)
            {   //Get all players near
                List<Player> players = _arena.getPlayersInRange(player._state.positionX, player._state.positionY, -item.repairDistance);
                int totalHealth = 0;
                foreach (Player p in players)
                {
                    if (p == null || p == player || p._state.health >= 100 || p._state.health <= 0)
                        continue;
                    totalHealth += (p._baseVehicle._type.Hitpoints - p._state.health);
                }
                StatsCurrent(player).potentialHealthHealed += totalHealth;
            }

            //Defer to our current gametype handler!
            switch (_gameType)
            {
                case Settings.GameTypes.Conquest:
                    _cq.PlayerRepair(player, item);
                    break;
                case Settings.GameTypes.Coop:
                    _coop.PlayerRepair(player, item);
                    break;
                case Settings.GameTypes.Royale:
                    _royale.PlayerRepair(player, item);
                    break;
                case Settings.GameTypes.RTS:
                    _rts.PlayerRepair(player, item);
                    break;

                default:
                    //Do nothing
                    break;
            }
            return true;
        }

        /// <summary>
        /// Triggered when a vehicle dies
        /// </summary>
        [Scripts.Event("Vehicle.Death")]
        public bool vehicleDeath(Vehicle dead, Player killer)
        {
            switch (_gameType)
            {
                case Settings.GameTypes.Conquest:
                    break;
                case Settings.GameTypes.Coop:
                    break;
                case Settings.GameTypes.Royale:
                    _royale.vehicleDeath(dead, killer);
                    break;
                case Settings.GameTypes.RTS:
                    _rts.vehicleDeath(dead, killer);
                    break;

                default:
                    //Do nothing
                    break;
            }
            return true;
        }

        /// <summary>
        /// Triggered when a vehicle is created
        /// </summary>
        /// <remarks>Doesn't catch spectator or dependent vehicle creation</remarks>
        [Scripts.Event("Vehicle.Creation")]
        public bool vehicleCreation(Vehicle created, Team team, Player creator)
        {
            switch (_gameType)
            {
                case Settings.GameTypes.Conquest:
                    break;
                case Settings.GameTypes.Coop:
                    break;
                case Settings.GameTypes.Royale:
                    break;
                case Settings.GameTypes.RTS:
                    _rts.vehicleCreation(created, team, creator);
                    break;

                default:
                    //Do nothing
                    break;
            }
            return true;
        }

        /// <summary>
        /// Triggered only when a special communication command is created here that isn't a server command.
        /// </summary>
        [Scripts.Event("Player.CommCommand")]
        public bool playerCommCommand(Player player, Player recipient, string command, string payload)
        {
                return true;
        }

        /// <summary>
        /// Triggered only when a special chat command is created here that isn't a server command.
        /// </summary>
        [Scripts.Event("Player.ChatCommand")]
        public bool playerChatCommand(Player player, Player recipient, string command, string payload)
        {
            switch (_gameType)
            {
                case Settings.GameTypes.Conquest:
                    return _cq.playerChatCommand(player, recipient, command, payload);
                case Settings.GameTypes.Coop:
                    return _coop.playerChatCommand(player, recipient, command, payload);
                case Settings.GameTypes.Royale:
                    return _royale.playerChatCommand(player, recipient, command, payload);
                case Settings.GameTypes.RTS:
                    return _rts.playerChatCommand(player, recipient, command, payload);
            }
            return true;
        }

        /// <summary>
        /// Triggered only when a special mod command created here that isn't a server command.
        /// </summary>
        [Scripts.Event("Player.ModCommand")]
        public bool playerModcommand(Player player, Player recipient, string command, string payload)
        {

            if (command.Equals("bounty"))
            {
                if (recipient == null)
                {
                    player.sendMessage(-1, "Command must be sent in PM");
                    return false;
                }

                if (String.IsNullOrEmpty(payload))
                {
                    player.sendMessage(-1, "Must specify an amount");
                    return false;
                }

                int amount;
                if (Int32.TryParse(payload, out amount))
                {
                    recipient.Bounty = amount;
                    recipient.syncState();
                    return true;
                }
                else
                {
                    player.sendMessage(-1, "Invalid amount specified");
                    return false;
                }

            }
            if (command.Equals("gametype"))
            {
                if (String.IsNullOrEmpty(payload))
                    return false;

                if (_arena._name.StartsWith("[Co-Op]"))
                    return false;

                if (payload.ToLower().Equals("conquest"))
                {
                    _gameType = Settings.GameTypes.Conquest;
                    _arena.gameEnd();
                    return false;
                }
                if (payload.ToLower().Equals("royale"))
                {
                    _gameType = Settings.GameTypes.Royale;
                    _arena.gameEnd();
                    return false;
                }
            }

            if (command.Equals("addbot"))
            {
                if (String.IsNullOrEmpty(payload))
                {
                    return false;
                }
                if (payload.Equals("exolight"))
                {
                    _coop.spawnExoLight(_coop._botTeam);
                    return false;
                }
                if (payload.Equals("exoheavy"))
                {
                    _coop.spawnExoHeavy(_coop._botTeam);
                    return false;
                }
            }

            if (command.Equals("bots"))
            {
                if (String.IsNullOrEmpty(payload))
                {
                    player.sendMessage(0, String.Format("Bots are {0}", ((_cq.spawnBots) ? "enabled" : "disabled")));
                    return false;
                }

                if (payload.Equals("off"))
                {
                    _cq.spawnBots = false;
                    _arena.sendArenaMessage("Bots have been disabled for this arena");
                    return false;
                }

                if (payload.Equals("on"))
                {
                    _cq.spawnBots = true;
                    _arena.sendArenaMessage("Bots have been enabled for this arena");
                    return false;
                }
            }

            if (command.Equals("minimaps"))
            {
                if (String.IsNullOrEmpty(payload))
                {
                    player.sendMessage(0, String.Format("Minimaps are {0}", ((_bMiniMapsEnabled) ? "enabled" : "disabled")));
                    return false;
                }

                if (payload.Equals("off"))
                {
                    _bMiniMapsEnabled = false;

                    if (_arena._bGameRunning)
                        _arena.gameEnd();

                    Team team1 = _arena.getTeamByName("Titan Militia");
                    Team team2 = _arena.getTeamByName("Collective Military");
                    _cq.setTeams(team1, team2, false);

                    _arena.sendArenaMessage("Minimaps have been disabled for this arena");
                    return false;
                }

                if (payload.Equals("on"))
                {
                    _bMiniMapsEnabled = true;
                    _arena.sendArenaMessage("Minimaps have been enabled for this arena");
                    return false;
                }
            }

            if (command.Equals("map"))
            {
                if (String.IsNullOrEmpty(payload))
                {
                    player.sendMessage(0, "Payload cannot be empty. Ex: *map redblue");
                    return false;
                }

                if (payload.Equals("cazzo"))
                {
                    Team team1 = _arena.getTeamByName("Titan Militia");
                    Team team2 = _arena.getTeamByName("Collective Military");
                    _cq.setTeams(team1, team2, false);
                    _arena.gameStart();
                }

                if (payload.Equals("redblue"))
                {
                    Team team1 = _arena.getTeamByName("Red");
                    Team team2 = _arena.getTeamByName("Blue");
                    _cq.setTeams(team1, team2, false);
                    _arena.gameStart();
                }

                if (payload.Equals("greenyellow"))
                {
                    Team team1 = _arena.getTeamByName("Green");
                    Team team2 = _arena.getTeamByName("Yellow");
                    _cq.setTeams(team1, team2, false);
                    _arena.gameStart();
                }
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
                    if (!String.IsNullOrWhiteSpace(payload))
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
                        recipient.sendMessage(0, String.Format("You have been powered to level {0}. Use *help to familiarize with the commands and please read all rules.", level));
                        player.sendMessage(0, String.Format("You have promoted {0} to level {1}.", recipient._alias, level));
                    }
                    else
                    {
                        recipient._developer = true;
                        recipient._permissionStatic = Data.PlayerPermission.ArenaMod;
                        recipient.sendMessage(0, String.Format("You have been powered to level {0}. Use *help to familiarize with the commands and please read all rules.", level));
                        player.sendMessage(0, String.Format("You have promoted {0} to level {1}.", recipient._alias, level));
                    }

                    //Lets send it to the database
                    //Send it to the db
                    CS_ModQuery<Data.Database> query = new CS_ModQuery<Data.Database>();
                    query.queryType = CS_ModQuery<Data.Database>.QueryType.mod;
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
                    if (String.IsNullOrEmpty(payload))
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
                            player.sendMessage(-1, String.Format("Syntax: *poweradd alias:level(optional) OR :alias:*poweradd level(optional) possible levels are 1-{0}", ((int)player.PermissionLevelLocal).ToString()));
                            player.sendMessage(0, "Note: there can be only 1 admin level.");
                            return false;
                        }
                        payload = param[0];
                    }
                    player.sendMessage(0, String.Format("You have promoted {0} to level {1}.", payload, level));
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
                        recipient.sendMessage(0, String.Format("You have been powered to level {0}. Use *help to familiarize with the commands and please read all rules.", level));
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
                    if (!String.IsNullOrWhiteSpace(payload))
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
                        recipient.sendMessage(0, String.Format("You have been demoted to level {0}.", level));
                        player.sendMessage(0, String.Format("You have demoted {0} to level {1}.", recipient._alias, level));
                    }
                    else
                    {
                        recipient._developer = false;
                        recipient._permissionStatic = Data.PlayerPermission.Normal;
                        recipient.sendMessage(0, String.Format("You have been demoted to level {0}.", level));
                        player.sendMessage(0, String.Format("You have demoted {0} to level {1}.", recipient._alias, level));
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
                    if (String.IsNullOrEmpty(payload))
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
                            player.sendMessage(-1, String.Format("Syntax: *powerremove alias:level(optional) OR :alias:*powerremove level(optional) possible levels are 0-{0}", ((int)player.PermissionLevelLocal).ToString()));
                            return false;
                        }
                        payload = param[0];
                    }
                    player.sendMessage(0, String.Format("You have demoted {0} to level {1}.", payload, level));
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
                        recipient.sendMessage(0, String.Format("You have been depowered to level {0}.", level));
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


            switch (_gameType)
            {
                case Settings.GameTypes.Conquest:
                    return _cq.playerModcommand(player, recipient, command, payload);
                case Settings.GameTypes.Coop:
                    return _coop.playerModcommand(player, recipient, command, payload);
                case Settings.GameTypes.Royale:
                    return _royale.playerModcommand(player, recipient, command, payload);
                case Settings.GameTypes.RTS:
                    return _rts.playerModcommand(player, recipient, command, payload);
            }

            return false;
        }

        #endregion

        #region Killstreaks
        /// <summary>
        /// Updates our players kill streak timer
        /// </summary>
        private void UpdateKillStreaks()
        {
            foreach (Player p in _arena.PlayersIngame)
            {
                if (StatsCurrent(p) == null)
                    continue;

                if (StatsCurrent(p).lastUsedWepTick == -1)
                    continue;

                if (Environment.TickCount - StatsCurrent(p).lastUsedWepTick <= 0)
                    ResetWeaponTicker(p);
            }
        }

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
            if (StatsCurrent(target) == null)
                return;

            StatsCurrent(target).lastUsedWep = null;
            StatsCurrent(target).lastUsedWepKillCount = 0;
            StatsCurrent(target).lastUsedWepTick = -1;
        }

        /// <summary>
        /// Updates the killer and their counter
        /// </summary>
        public void UpdateKiller(Player killer)
        {
            if (StatsCurrent(killer) == null)
                return;

            StatsCurrent(killer).lastKillerCount++;

            //Should we be giving any rewards etc?
            switch (_gameType)
            {
                case Settings.GameTypes.Conquest:
                    _cq.playerKillStreak(killer, StatsCurrent(killer).lastKillerCount);
                    break;
                case Settings.GameTypes.Coop:
                    {
                    _coop.playerKillStreak(killer, StatsCurrent(killer).lastKillerCount);
                    }
                    break;

            }
           
            //Is this first blood?
            if (lastKiller == null)
            {
                //It is, lets make the sound
                _arena.sendArenaMessage(string.Format("{0} has drawn first blood.", killer._alias), 9);
            }
            lastKiller = killer;
        }

        /// <summary>
        /// Updates the victim's kill streaks
        /// </summary>
        public void UpdateDeath(Player victim, Player killer)
        {
            if (StatsCurrent(victim) == null)
                return;


            if (StatsCurrent(victim).lastKillerCount >= 6)
            {
                _arena.sendArenaMessage(string.Format("{0}", killer != null ? killer._alias + " has ended " + victim._alias + "'s kill streak." :
                    victim._alias + "'s kill streak has ended."), 7);
            }
            StatsCurrent(victim).lastKillerCount = 0;

        }

        /// <summary>
        /// Updates the last fired weapon and the ticker
        /// </summary>
        public void UpdateWeapon(Player from, ItemInfo.Projectile usedWep)
        {
            if (StatsCurrent(from) == null)
                return;

            StatsCurrent(from).lastUsedWep = usedWep;
            //500 = Alive time for the schrapnel after main weap explosion
            StatsCurrent(from).lastUsedWepTick = DateTime.Now.AddTicks(500).Ticks;
        }

        /// <summary>
        /// Updates the last weapon kill counter
        /// </summary>
        public void UpdateWeaponKill(Player from)
        {
            if (StatsCurrent(from) == null)
                return;

            StatsCurrent(from).lastUsedWepKillCount++;
            ItemInfo.Projectile lastUsedWep = StatsCurrent(from).lastUsedWep;
            if (lastUsedWep == null)
                return;

            if (lastUsedWep.name.Contains("Combat Knife"))
                _arena.sendArenaMessage(string.Format("{0} is throwing out the knives.", from._alias), 6);

            switch (StatsCurrent(from).lastUsedWepKillCount)
            {
                case 2:
                    _arena.sendArenaMessage(string.Format("{0} just got a double {1} kill.", from._alias, lastUsedWep.name), 13);
                    break;
                case 3:
                    _arena.sendArenaMessage(string.Format("{0} just got a triple {1} kill.", from._alias, lastUsedWep.name), 14);
                    break;
                case 4:
                    _arena.sendArenaMessage(string.Format("A 4 {0} kill by {0}?!?", lastUsedWep.name, from._alias), 15);
                    break;
                case 5:
                    _arena.sendArenaMessage(string.Format("Unbelievable! {0} with the 5 {1} kill?", from._alias, lastUsedWep.name), 16);
                    break;
            }
        }

        #endregion

        #region Custom Calls
        public void AllowPrivateTeams(bool bAllow, int maxSize)
        {
            _arena._server._zoneConfig.arena.allowManualTeamSwitch = bAllow;
            _arena._server._zoneConfig.arena.allowPrivateFrequencies = bAllow;
            _arena._server._zoneConfig.arena.maxPerFrequency = maxSize;
            _arena.sendArenaMessage(String.Format("Private Teams/Team switching is now {0}", ((bAllow) ? "enabled" : "disabled")));
        }


    public void spawnSupplyDrop(Team team, short posX, short posY)
        {
            VehInfo supplyVehicle = AssetManager.Manager.getVehicleByID(405);
            Helpers.ObjectState objState = new Helpers.ObjectState();

            objState.positionX = posX;
            objState.positionY = posY;

            Computer newVehicle = _arena.newVehicle(supplyVehicle, team, null, objState) as Computer;
            SupplyDrop newDrop = new SupplyDrop(team, newVehicle, posX, posY);
            _supplyDrops.Add(newDrop);

            team.sendArenaMessage(String.Format("&Supplies have been dropped at {0}.", newVehicle._state.letterCoord()), 4);
        }

        private Player findSquadWarp(IEnumerable<Player> squadmates, Player player)
        {
            Player warpTo = null;

            List<Player> potentialTargets = new List<Player>();

            //Start shuffling through looking for potential targets
            foreach (Player squadmate in squadmates)
            {
                int enemycount = 0;
                double distance = 0;

                //Any enemies?
                enemycount = _arena.getPlayersInRange
                    (squadmate._state.positionX, squadmate._state.positionY, 625).Where
                    (p => p._team != squadmate._team && !p.IsDead).Count();

                //Decent distance away? Don't want to warp ourselves super sort distances
                distance = Helpers.distanceTo(squadmate._state, player._state);

                //Is he a match!?
                if (distance >= 2000 && enemycount == 0)
                {
                    potentialTargets.Add(squadmate);
                }
            }

            //Randomize!
            if (potentialTargets.Count > 0)
            {
                warpTo = potentialTargets[new Random().Next(0, potentialTargets.Count)];
            }


            return warpTo;
        }

        private bool warpToSquad(Player player)
        {
            //Public arena?
            if (!_arena._bIsPublic)
            {
                player.sendMessage(-1, "Only allowed in public arenas!");
                return false;
            }

            //No squad no laundry
            if (player._squad == "")
            {
                player.sendMessage(-1, "You don't have a squad to warp to! Join or create a squad.");
                return false;
            }

            //Lets find some squaddies
            IEnumerable<Player> squadmates = player._team.ActivePlayers.Where(p => p._squad == player._squad && p.IsDead == false && p != player);

            //No squadmates online on his team?
            if (squadmates.Count() == 0)
            {
                player.sendMessage(-1, "You don't have any squadmates online on your team!");
                return false;
            }

            Player warpTo = findSquadWarp(squadmates, player);

            //Can we find an appropriate target?
            if (warpTo == null)
            {
                player.sendMessage(-1, "Your squadmates are in battle or dead! Try again soon");
                return false;
            }

            //Warp him!
            player.warp(warpTo);
            warpTo.sendMessage(0, String.Format("!{0} has joined you in battle.", player._alias));

            return true;
        }
        #endregion

        #region Player Stats
        public void createPlayerStats(Player player)
        {
            //Create some new stuff
            Stats temp = new Stats();
            temp.teamname = player._team._name;
            temp.alias = player._alias;
            temp.points = 0;
            temp.assistPoints = 0;
            temp.bonusPoints = 0;
            temp.killPoints = 0;
            temp.titanPlaySeconds = 0;
            temp.collectivePlaySeconds = 0;
            temp.kills = 0;
            temp.deaths = 0;
            temp.player = player;

            temp.lastKillerCount = 0;
            temp.lastUsedWep = null;
            temp.lastUsedWepKillCount = 0;
            temp.lastUsedWepTick = -1;
            temp.potentialHealthHealed = 0;
            temp.onPlayingField = false;
            temp.hasPlayed = player.IsSpectator ? false : true;

            if (!_savedPlayerStats.ContainsKey(player._alias))
                _savedPlayerStats.Add(player._alias, temp);
        }

        public Stats StatsCurrent(Player player)
        {
            if (_savedPlayerStats.ContainsKey(player._alias))
                return _savedPlayerStats[player._alias];
            else
                return null;
        }
    }
    #endregion
}