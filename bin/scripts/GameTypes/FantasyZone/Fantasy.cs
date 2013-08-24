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

namespace InfServer.Script.GameType_Fantasy
{	// Script Class
    /// Provides the interface between the script and arena
    ///////////////////////////////////////////////////////
    class Script_Fantasy : Scripts.IScript
    {	///////////////////////////////////////////////////
        // Member Variables
        ///////////////////////////////////////////////////
        private Arena _arena;					//Pointer to our arena class
        private CfgInfo _config;				//The zone config

        private List<BookInfo> _books;
        private List<RecipeInfo> _recipes;
        //   private List<Chest> _chests;
        //   private Dictionary<Player, Helper.oState> _oStates;

        private int _jackpot;					//The game's jackpot so far

        private Team _victoryTeam;				//The team currently winning!
        private int _tickVictoryStart;			//The tick at which the victory countdown began
        private int _tickNextVictoryNotice;		//The tick at which we will next indicate imminent victory
        private int _victoryNotice;				//The number of victory notices we've done
        private int _tickLastBotSpawn;
        private int _nestID = 400;
        private int _botSpawnRate = 8000;
        private int _lastGameCheck;				//The tick at which we last checked for game viability
        private int _tickGameStarting;			//The tick at which the game began starting (0 == not initiated)
        private int _tickGameStart;				//The tick at which the game started (0 == stopped)

        //Settings
        private int _minPlayers;				//The minimum amount of players

        private bool _gameWon = false;

        //Bot spawns
        public List<fantasyBot> _spawns = new List<fantasyBot>();
        public class fantasyBot
        {
            //General settings
            public int ID;                  //Our bot ID
            public int vehID;               //Our vehicle ID
            public int spawnID;             //Vehicle ID of where we spawn
            public int count;               //Current amount of this bot
            public int max;                 //Max amount of this bot for assigned spawn
            public int multiple;            //Multiple of bots to spawn at one time
            public int ticksBetweenSpawn;   //How many ticks to wait in between spawns

            //Targetting settings
            public bool atkBots;    //Do we attack bots
            public bool atkVeh;     //Do we attack computer vehicles?
            public bool atkPlayer;  //Do we attack players?
            public bool atkCloak;   //Do we attack cloakers?
            public bool patrol;     //Do we patrol?
            //Spawn point settings
            public bool relyOnSpawn;//Do we cease to exist when our spawn point is missing

            //Distances
            public int defenseRadius;       //Distance from player until we attack them
            public int distanceFromHome;    //Distance from home we have to be to ignore targets and run back [if not fighting]
            public int patrolRadius;        //Radius to patrol around home
            public int lockInTime;          //Amount of time we spend focused on one target
            public int attackRadius;        //Distance from home that we keep engaging units
            //      public bool patrolWaypoints;       //Whether or not to use waypoints for patrol


            //Weapon settings
            public int shortRange;
            public int mediumRange;
            public int longRange;

            //Utility settings

            //Constructor
            public fantasyBot(int id)
            {
                //General settings
                ID = id;
            }

            //Weapon Systems
            public List<BotWeapon> _weapons = new List<BotWeapon>();
            public class BotWeapon
            {
                public int ID;                 //ID of entry
                public int weaponID;           //Weapon ID

                public int preferredRange;  //Preferred range for this weapon [Bot will attempt to stay within this distance of enemy when using this weapon]

                public int shortChance;        //Chances to actually fire this within each range
                public int midChance;          //Use 500 for 50%
                public int longChance;
                public int allChance;

                //Constructor
                public BotWeapon(int id)
                {
                    ID = id;
                }
            }

            //Waypoint settings
            public List<Waypoints> _waypoints = new List<Waypoints>();
            public class Waypoints
            {
                public int ID;          //ID of entry
                public int posX;        //X coordinate
                public int posY;        //Y coordinate

                //Constructor
                public Waypoints(int id)
                {
                    ID = id;
                }
            }

        }
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

            //oState
            //_oStates = new Dictionary<Player, Helper.oState>();

            //Load up some data
            string books = @".\Assets\Data\books.csv";
            string recipes = @".\Assets\Data\recipes.csv";

            //Books
            if (File.Exists(books))
                _books = BookInfo.Load(Path.GetFullPath(books));
            else
                Log.write("Could not locate books at {0}", Path.GetFullPath(books));
            //Recipes
            if (File.Exists(recipes))
                _recipes = RecipeInfo.Load(Path.GetFullPath(recipes));
            else
                Log.write("Could not locate recipes at {0}", Path.GetFullPath(recipes));

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

            using (StreamReader sr = new StreamReader("./assets/FantasyBots.txt"))
            {

                String bots = sr.ReadToEnd();
                var values = bots.Split(',');

                int i = 0, j = 0;
                while (i < (values.Count() - 1))
                {
                    if (values[i] == "!")
                        i++;

                    _spawns.Add(new fantasyBot(j));
                    _spawns[j].ID = j;
                    //Basics
                    _spawns[j].vehID = Int32.Parse(values[i++]);
                    _spawns[j].spawnID = Int32.Parse(values[i++]);
                    _spawns[j].count = Int32.Parse(values[i++]);
                    _spawns[j].max = Int32.Parse(values[i++]);
                    _spawns[j].multiple = Int32.Parse(values[i++]);
                    _spawns[j].ticksBetweenSpawn = Int32.Parse(values[i++]);
                    //Bools
                    _spawns[j].atkBots = Boolean.Parse(values[i++]);
                    _spawns[j].atkVeh = Boolean.Parse(values[i++]);
                    _spawns[j].atkPlayer = Boolean.Parse(values[i++]);
                    _spawns[j].atkCloak = Boolean.Parse(values[i++]);
                    _spawns[j].patrol = Boolean.Parse(values[i++]);

                    //Distances
                    _spawns[j].defenseRadius = Int32.Parse(values[i++]);
                    _spawns[j].distanceFromHome = Int32.Parse(values[i++]);
                    _spawns[j].patrolRadius = Int32.Parse(values[i++]);
                    _spawns[j].lockInTime = Int32.Parse(values[i++]);
                    _spawns[j].attackRadius = Int32.Parse(values[i++]);
                    int totalWeapons = Int32.Parse(values[i]);
                    i++;
                    int k = 0;
                    Log.write("Adding bot weapons -- total:" + totalWeapons);
                    while (k < totalWeapons)
                    {
                        _spawns[j]._weapons.Add(new fantasyBot.BotWeapon(k));
                        Log.write("Added bot weapon - " + k);
                        _spawns[j]._weapons[k].weaponID = Int32.Parse(values[i++]);
                        Log.write("Added bot weapon - " + _spawns[j]._weapons[k].weaponID);
                        _spawns[j]._weapons[k].allChance = Int32.Parse(values[i++]);
                        _spawns[j]._weapons[k].shortChance = Int32.Parse(values[i++]);
                        _spawns[j]._weapons[k].midChance = Int32.Parse(values[i++]);
                        _spawns[j]._weapons[k].longChance = Int32.Parse(values[i++]);
                        _spawns[j]._weapons[k].preferredRange = Int32.Parse(values[i++]);
                        k++;
                    }
                    _spawns[j].shortRange = Int32.Parse(values[i++]);
                    _spawns[j].mediumRange = Int32.Parse(values[i++]);
                    _spawns[j].longRange = Int32.Parse(values[i++]);

                    //Waypoint Settings
                    int totalWaypoints = Int32.Parse(values[i]);
                    i++;
                    k = 0;
                    while (k < totalWaypoints)
                    {
                        _spawns[j]._waypoints.Add(new fantasyBot.Waypoints(k));
                        _spawns[j]._waypoints[k].ID = k;
                        _spawns[j]._waypoints[k].posX = Int32.Parse(values[i++]);
                        _spawns[j]._waypoints[k].posY = Int32.Parse(values[i]);
                        i++;
                        k++;
                    }
                    i++;
                    j++;
                }
            }


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

            //Find the nest and spawn a spider at it or whatever
            if (now - _tickLastBotSpawn > _botSpawnRate)
            {
                _tickLastBotSpawn = now;

                foreach (Vehicle v in _arena.Vehicles.ToList())
                    foreach (var p in _spawns)
                        if (p.spawnID == v._type.Id)
                        {
                            //First check to see if we have enough room to spawn this one
                            p.count = 0;
                            foreach (Vehicle existing in _arena.getVehiclesInRange(v._state.positionX, v._state.positionY, 200))
                                if (existing._type.Id == p.vehID)
                                    p.count++;

                            if (p.count < p.max)
                            {
                                int amountToSpawn = 1 * p.multiple;
                                for (int i = 0; i < amountToSpawn; i++)
                                    _arena.newBot(typeof(RangeMinion), (ushort)p.vehID, _arena.Teams.ElementAt(7), null, v._state, this, p.ID);
                            }
                        }
            }

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

            //Is anybody experiencing a victory?
            if (_tickVictoryStart != 0)
            {	//Have they won yet?
                if (now - _tickVictoryStart > (_config.flag.victoryHoldTime * 10))
                {
                    //Yes! Trigger game victory
                    _gameWon = true; // game won
                    gameVictory(_victoryTeam);

                }
                else
                {	//Do we have a victory notice to give?
                    if (_tickNextVictoryNotice != 0 && now > _tickNextVictoryNotice)
                    {	//Yes! Let's give it
                        int countdown = (_config.flag.victoryHoldTime / 100) - ((now - _tickVictoryStart) / 1000);
                        _arena.sendArenaMessage(String.Format("Victory for {0} in {1} seconds!",
                            _victoryTeam._name, countdown), _config.flag.victoryWarningBong);

                        //Plan the next notice
                        _tickNextVictoryNotice = _tickVictoryStart;
                        _victoryNotice++;

                        if (_victoryNotice == 1 && countdown >= 30)
                            //Default 2/3 time
                            _tickNextVictoryNotice += (_config.flag.victoryHoldTime / 3) * 10;
                        else if (_victoryNotice == 2 || (_victoryNotice == 1 && countdown >= 20))
                            //10 second marker
                            _tickNextVictoryNotice += (_config.flag.victoryHoldTime * 10) - 10000;
                        else
                            _tickNextVictoryNotice = 0;
                    }
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
            Team victoryTeam = flag.team;


            foreach (Arena.FlagState fs in _arena._flags.Values)
                if (fs.bActive && fs.team != victoryTeam)
                    victoryTeam = null;

            if (victoryTeam != null)
            {	//Yes! Victory for them!
                _arena.setTicker(1, 1, _config.flag.victoryHoldTime, "Victory in ");
                _tickNextVictoryNotice = _tickVictoryStart = Environment.TickCount;
                _victoryTeam = victoryTeam;
            }
            else
            {	//Aborted?
                if (_victoryTeam != null && !_gameWon)
                {
                    _tickVictoryStart = 0;
                    _tickNextVictoryNotice = 0;
                    _victoryTeam = null;

                    _arena.sendArenaMessage("Victory has been aborted.", _config.flag.victoryAbortedBong);
                    _arena.setTicker(1, 1, 0, "");
                }
            }
        }

        /// <summary>
        /// Called when the specified team have won
        /// </summary>
        public void gameVictory(Team victors)
        {	//Let everyone know
            if (_config.flag.useJackpot)
                _jackpot = (int)Math.Pow(_arena.PlayerCount, 2);
            _arena.sendArenaMessage(String.Format("Victory={0} Jackpot={1}", victors._name, _jackpot), _config.flag.victoryBong);

            //TODO: Move this calculation to breakdown() in ScriptArena?
            //Calculate the jackpot for each player
            foreach (Player p in _arena.Players)
            {	//Spectating? Psh.
                if (p.IsSpectator)
                    continue;
                //Find the base reward
                int personalJackpot;

                if (p._team == victors)
                    personalJackpot = _jackpot * (_config.flag.winnerJackpotFixedPercent / 1000);
                else
                    personalJackpot = _jackpot * (_config.flag.loserJackpotFixedPercent / 1000);

                //Obtain the respective rewards
                int cashReward = personalJackpot * (_config.flag.cashReward / 1000);
                int experienceReward = personalJackpot * (_config.flag.experienceReward / 1000);
                int pointReward = personalJackpot * (_config.flag.pointReward / 1000);

                p.sendMessage(0, String.Format("Your Personal Reward: Points={0} Cash={1} Experience={2}", pointReward, cashReward, experienceReward));

                p.Cash += cashReward;
                p.Experience += experienceReward;
                p.BonusPoints += pointReward;
            }

            //Stop the game
            _arena.gameEnd();
        }

        /// <summary>
        /// Called when a player sends a chat command
        /// </summary>
        [Scripts.Event("Player.ChatCommand")]
        public bool playerChatCommand(Player player, Player recipient, string command, string payload)
        {
            if (command.ToLower() == "recipes")
            {
                foreach (RecipeInfo recipe in _recipes)
                {
                    player.sendMessage(-1, String.Format("{0}--{1} : {2}", recipe.id, recipe.name, recipe.description));

                    foreach (var item in recipe.ingredients)
                    {
                        ItemInfo ingredient = _arena._server._assets.getItemByID(item.Key);
                        if (ingredient != null)
                        {
                            player.sendMessage(-1, String.Format("&Ingredient = {0} | Quantity = {1}", ingredient.name, item.Value));
                        }
                    }

                    ItemInfo result = _arena._server._assets.getItemByID(recipe.result);
                    if (result != null)
                    {
                        player.sendMessage(-1, String.Format("Result = {0} : Quantity = {1}", result.name, recipe.resultQuantity));
                    }
                }
            }
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
        {
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
            // ScriptHelpers.scrambleTeams(_arena, 2, true);

            //Spawn our flags!
            _arena.flagSpawn();

            //Let everyone know
            _arena.sendArenaMessage("Game has started!", _config.flag.resetBong);
            _gameWon = false;

            foreach (var s in _spawns)
                foreach (var w in s._waypoints)
                {
                    VehInfo wp = _arena._server._assets.getVehicleByID(Convert.ToInt32(402));
                    Helpers.ObjectState waypointState = new Protocol.Helpers.ObjectState();
                    waypointState.positionX = (short)w.posX;
                    waypointState.positionY = (short)w.posY;
                    waypointState.positionZ = 0;
                    waypointState.yaw = 0;
                    _arena.newVehicle(
                                    wp,
                                    _arena.Teams.ElementAt(7), null,
                                   waypointState);
                }

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
            _tickVictoryStart = 0;
            _tickNextVictoryNotice = 0;
            _victoryTeam = null;
            _gameWon = false;

            return true;
        }

        /// <summary>
        /// Called when the statistical breakdown is displayed
        /// </summary>
        [Scripts.Event("Game.Breakdown")]
        public bool breakdown()
        {	//Allows additional "custom" breakdown information


            //Always return true;
            return true;
        }

        /// <summary>
        /// Called to reset the game state
        /// </summary>
        [Scripts.Event("Game.Reset")]
        public bool gameReset()
        {	//Game reset, perhaps start a new one
            _tickGameStart = 0;
            _tickGameStarting = 0;
            _tickVictoryStart = 0;
            _tickNextVictoryNotice = 0;

            _gameWon = false;

            _victoryTeam = null;

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
            if (_arena.getTerrainID(player._state.positionX, player._state.positionY) == 10)
            {

                if (player.inventoryModify(item, -quantity))
                {
                    //We want to continue wrapping around the vehicleid limits
                    //looking for empty spots.
                    ushort ik;

                    for (ik = _arena._lastItemKey; ik <= Int16.MaxValue; ++ik)
                    {	//If we've reached the maximum, wrap around
                        if (ik == Int16.MaxValue)
                        {
                            ik = (ushort)ZoneServer.maxPlayers;
                            continue;
                        }

                        //Does such an item exist?
                        if (_arena._items.ContainsKey(ik))
                            continue;

                        //We have a space!
                        break;
                    }

                    _arena._lastItemKey = ik;
                    //Create our drop class		
                    Arena.ItemDrop id = new Arena.ItemDrop();

                    id.item = item;
                    id.id = ik;
                    id.quantity = (short)quantity;
                    id.positionX = player._state.positionX;
                    id.positionY = player._state.positionY;
                    id.relativeID = (0 == 0 ? item.relativeID : 0);
                    id.freq = player._team._id;

                    id.owner = player; //For bounty abuse upon pickup

                    int expire = _arena.getTerrain(player._state.positionX, player._state.positionY).prizeExpire;
                    id.tickExpire = (expire > 0 ? (Environment.TickCount + (expire * 1000)) : 0);

                    //Add it to our list
                    _arena._items[ik] = id;

                    //Notify JUST the player
                    Helpers.Object_ItemDrop(player, id);
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// Handles a player's portal request
        /// </summary>
        [Scripts.Event("Player.Portal")]
        public bool playerPortal(Player player, LioInfo.Portal portal)
        {
            List<Arena.FlagState> carried = _arena._flags.Values.Where(flag => flag.carrier == player).ToList();

            foreach (Arena.FlagState carry in carried)
            {   //If the terrain number is 0-15

                int terrainNum = player._arena.getTerrainID(player._state.positionX, player._state.positionY);
                if (terrainNum >= 0 && terrainNum <= 15)
                {   //Check the FlagDroppableTerrains for that specific terrain id

                    if (carry.flag.FlagData.FlagDroppableTerrains[terrainNum] == 0)
                        _arena.flagResetPlayer(player);
                }
            }

            return true;
        }

        /// <summary>
        /// Handles a player's produce request
        /// </summary>
        [Scripts.Event("Player.Produce")]
        public bool playerProduce(Player player, Computer computer, VehInfo.Computer.ComputerProduct product)
        {
            #region Crafting
            //Crafting Vendor?
            if (computer._type.Name.StartsWith("[Crafting]"))
            {
                if (product.Title.ToLower() == "craft")
                {
                    List<Arena.ItemDrop> items = _arena.getItemsInRange(computer._state.positionX, computer._state.positionY, 200).Where(i => i.owner == player).ToList();
                    Dictionary<int, int> items1 = new Dictionary<int, int>();

                    //Have they dropped any?
                    if (items.Count == 0)
                    {
                        player.triggerMessage(2, 500, String.Format("{0}> Sorry, I don't know that recipe", computer._type.Name));
                        foreach (Arena.ItemDrop itm in items)
                        {
                            //Update the players of the status..
                            Helpers.Object_ItemDropUpdate(_arena.Players, itm.id, 0);
                            _arena._items.Remove(itm.id);
                        }
                        return true;
                    }

                    //Convert to a simplier dictionary
                    foreach (Arena.ItemDrop itm in items)
                    {
                        if (!items1.Keys.Contains(itm.item.id))
                        {
                            items1.Add(itm.item.id, itm.quantity);
                        }
                    }

                    //Lets narrow our results down a bit..
                    List<RecipeInfo> recipes = _recipes.Where(r => r.ingredients.Count == items.Count).ToList();
                    if (recipes.Count == 0)
                    {
                        player.triggerMessage(2, 500, String.Format("{0}> Sorry, I don't know that recipe", computer._type.Name));
                        foreach (Arena.ItemDrop itm in items)
                        {
                            //Update the players of the status..
                            Helpers.Object_ItemDropUpdate(_arena.Players, itm.id, 0);
                            _arena._items.Remove(itm.id);
                        }
                        return true;
                    }

                    //K, narrowed it down, now lets do some extensive comparisons
                    RecipeInfo recipe = null;
                    foreach (RecipeInfo r in recipes)
                    {
                        bool equal = false;
                        if (r.ingredients.Count == items1.Count) // Require equal count.
                        {
                            equal = true;
                            foreach (var pair in r.ingredients)
                            {
                                int value;
                                if (items1.TryGetValue(pair.Key, out value))
                                {
                                    // Require value be equal.
                                    if (value != pair.Value)
                                    {
                                        equal = false;
                                        break;
                                    }
                                }
                                else
                                {
                                    // Require key be present.
                                    equal = false;
                                    break;
                                }

                            }
                        }
                        if (equal == true)
                        {
                            recipe = r;
                        }
                    }

                    //
                    if (recipe == null)
                    {
                        player.triggerMessage(2, 500, String.Format("{0}> Sorry, I don't know that recipe", computer._type.Name));

                        foreach (Arena.ItemDrop itm in items)
                        {
                            //Update the players of the status..
                            Helpers.Object_ItemDropUpdate(_arena.Players, itm.id, 0);
                            _arena._items.Remove(itm.id);
                        }

                        return false;
                    }

                    ItemInfo results = _arena._server._assets.getItemByID(recipe.result);

                    if (results != null)
                    {
                        player.inventoryModify(true, recipe.result, recipe.resultQuantity);
                        player.triggerMessage(2, 800, String.Format("You have crafted {0}", results.name));
                    }

                    foreach (Arena.ItemDrop itm in items)
                    {
                        //Update the players of the status..
                        Helpers.Object_ItemDropUpdate(_arena.Players, itm.id, 0);
                        _arena._items.Remove(itm.id);
                    }



                }
            }
            #endregion

            #region Chest
            if (computer._type.Name == "Chest")
            {
                List<Arena.ItemDrop> items = _arena.getItemsInRange(computer._state.positionX, computer._state.positionY, 200).Where(i => i.owner == player).ToList();

                //Are we closing?
                if (product.Title == "Close")
                {
                    //Ignore if they haven't dropped any items or if the chest was empty.
                    if (items.Count == 0)
                    {
                        return false;
                    }
                }

                //Opening?
                if (product.Title == "Open")
                {

                }

            }
            #endregion
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
            #region Stone Skin
            if (vehicle._type.Id == 121)
                return false;
            #endregion
            return true;
        }

        /// <summary>
        /// Triggered when a player notifies the server of an explosion
        /// </summary>
        [Scripts.Event("Player.Explosion")]
        public bool playerExplosion(Player player, ItemInfo.Projectile weapon, short posX, short posY, short posZ)
        {
            //Teleport
            if (weapon.id == 1127 || weapon.id == 1130 || weapon.id == 1131 || weapon.id == 1137)
            {   //Warp the player to the location
                player.warp(posX, posY);
            }
            switch (weapon.id)
            {
                //Chained Lightning
                case 1290:
                    {
                        IEnumerable<Player> players = _arena.getPlayersInRange(posX, posY, 600).Where(p => p != player);
                        ItemInfo.Projectile bolt = _arena._server._assets.getItemByID(1291) as ItemInfo.Projectile;
                        if (players.Count() > 0)
                        {
                            foreach (Player p in players)
                            {
                                byte yaw = Helpers.computeLeadFireAngle(player._state, p._state, bolt.muzzleVelocity / 1000);
                                Helpers.Player_RouteExplosion(_arena.Players, 1290, posX, posY, posZ, yaw, player._id);

                            }
                        }
                    }
                    break;
                //Teleport
                case 1130:
                    {
                        {   //Warp the player to the location
                            player.warp(posX, posY);
                        }
                    }
                    break;
                //Stone Skin
                case 1283:
                    {
                        Vehicle skin = _arena.newVehicle(121);
                        skin._state.positionX = player._state.positionX;
                        skin._state.positionY = player._state.positionY;
                        skin._state.yaw = player._state.yaw;
                        skin.playerEnter(player);
                    }
                    break;
            }


            #region Books
            //Trying to read a book?
            BookInfo book = _books.FirstOrDefault(b => b.itmID == weapon.id);
            if (book != null)
            {
                //Trigger the book's text
                player.triggerMessage(2, 1500, book.openText);
            }
            #endregion

            return true;
        }

        /// <summary>
        /// Triggered when a player has died, by any means
        /// </summary>
        /// <remarks>killer may be null if it wasn't a player kill</remarks>
        [Scripts.Event("Player.Death")]
        public bool playerDeath(Player victim, Player killer, Helpers.KillType killType, CS_VehicleDeath update)
        {
            #region Stone Skin
            if (victim._occupiedVehicle != null)
            {
                if (victim._occupiedVehicle._type.Id == 121)
                    victim._occupiedVehicle.destroy(false, true);
            }
            #endregion
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
            #region Stone Skin
            if (dead._type.Id == 121)
            {
                dead.destroy(true, true);
                return false;
            }
            #endregion

            return true;
        }
        #endregion
    }
}