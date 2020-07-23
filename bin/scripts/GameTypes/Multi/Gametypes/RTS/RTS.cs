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
    public partial class RTS
    {	///////////////////////////////////////////////////
        // Member Variables
        ///////////////////////////////////////////////////
        private Arena _arena;					//Pointer to our arena class
        private CfgInfo _config;                //The zone config
        public Script_Multi _baseScript;
        private Random _rand;

        private int _lastGameCheck;				//The tick at which we last checked for game viability
        public string _fileName;
        public string _owner;
        private Team _ownerTeam;
        public Database _database;

        public bool _bSquadCity;
        public string _squad;
        public int _cityValue;

        private Team _titan;
        private Team _collective;

        private int _tickLastUpdate;
        private int _tickLastBotUpdate;
        private int _lastTickerUpdate;

        //Stored Data
        public Dictionary<ushort, Structure> _structures;
        public Dictionary<ushort, Unit> _units;
        public Dictionary<ushort, StoredItem> _items;
        public Dictionary<string, Attacker> _attackers;
      

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
        public RTS(Arena arena, Script_Multi baseScript)
        {
            _baseScript = baseScript;
            _arena = arena;
            _config = arena._server._zoneConfig;
            _rand = new Random();

            _arena._bIsPublic = true;

            _titan = _arena.getTeamByName("Titan Militia");
            _collective = _arena.getTeamByName("Collective Military");
            _bots = new List<Bot>();
            _units = new Dictionary<ushort, Unit>();
            _structures = new Dictionary<ushort, Structure>();
            _items = new Dictionary<ushort, StoredItem>();
            _attackers = new Dictionary<string, Attacker>();
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
            int playing = _arena.PlayerCount;


            //Time to update our structures?
            if (now - _tickLastUpdate >= 8000)
            {
                if (_structures != null)
                {
                    foreach (Structure structure in _structures.Values)
                        _database.updateStructure(structure, _fileName);

                }

                if (_units != null)
                {
                    foreach (Unit unit in _units.Values)
                        _database.updateBot(unit, _fileName);

                }

                _tickLastUpdate = now;
            }

            //Check for productions
            checkForProductions(now);

            //Maintain our Bots
            maintainBots(now);

            //Maintain any attackers
            maintainAttackers(now);

            //Maintain any tickers
            maintainTickers(now);

            return true;
        }

        public void maintainTickers(int now)
        {
            if (now - _lastTickerUpdate < 1000)
                return;

            _cityValue = calculateCityValue(_structures.Values);
            _arena.setTicker(2, 0, 0, String.Format("City Value: {0} Iron", _cityValue));

            _lastTickerUpdate = now;
        }

        /// <summary>
        /// Maintains and removes any expired attackers
        /// </summary>
        public void maintainAttackers(int now)
        {
            DateTime currentTime = DateTime.Now;
            List<string> expiredAttackers = new List<string>();

            //Has anyone's previous attack expired?
            foreach (Attacker attacker in _attackers.Values)
            {
                if (attacker._attackExpire < currentTime)
                    expiredAttackers.Add(attacker._alias);

                //Current attacker need to be specced?
                if (now >= attacker._tickExpire && attacker._tickExpire != 0)
                {
                    Player player = _arena.getPlayerByName(attacker._alias);
                    if (player == null)
                        return;

                    player.spec();
                    player.sendMessage(0, "Your attack timer has expired, You may attack this city again in 24 hours.");
                    attacker._tickExpire = 0;
                }

            }

            //Clear out any expired attackers
            foreach (string attacker in expiredAttackers)
            {
                _database.removeAttacker(attacker, _fileName);
                _attackers.Remove(attacker);
            }
        }

        public void maintainBots(int now)
        {
            if (now - _tickLastBotUpdate < 5000 && _tickLastBotUpdate != 0)
                return;

            bool bUpdated = (_bots.Count == _units.Count);


            foreach (Unit bot in _units.Values)
            {
                if (bUpdated)
                    break;

                BotLevel level = BotLevel.Normal;
                BotType type = BotType.Marine;

                switch (bot._vehicleID)
                {
                    case 152:
                        level = BotLevel.Adept;
                        type = BotType.Ripper;
                        break;
                    case 151:
                        level = BotLevel.Adept;
                        type = BotType.Marine;
                        break;
                    case 131:
                        level = BotLevel.Normal;
                        type = BotType.Marine;
                        break;
                    case 145:
                        level = BotLevel.Normal;
                        type = BotType.Ripper;
                        break;
                    case 146:
                        level = BotLevel.Elite;
                        type = BotType.EliteMarine;
                        break;
                    case 148:
                        level = BotLevel.Elite;
                        type = BotType.EliteHeavy;
                        break;


                }

                //Avoids spawning bots under computer vehicles/buildings
                if (_arena.getVehiclesInRange(bot._state.positionX, bot._state.positionY, 150).Count(veh => veh._type.Type == VehInfo.Types.Computer) > 0)
                    Helpers.randomPositionInArea(_arena, 200, ref bot._state.positionX, ref bot._state.positionY);

                Bot newUnit = newBot(_titan, type, null, null, level, bot._state);

                if (newUnit == null)
                    Log.write("[RTS] Could not spawn bot");
                else
                    bot._bot = newUnit;


            }
        }

        public void init(Player player)
        {
            _arena.gameStart();

            //Load our database
            _database = new Database(_arena, this);
            _database.loadXML();

            //Find the associated filename in our database
            _fileName = _arena._name.Substring(5, _arena._name.Length - 5).TrimStart().ToLower();

            if (!_database.tableExists(_fileName))
            {
                if (!_database.playerOwnsCity(player))
                {
                    player.sendMessage(0, "&This city/arena is unowned. Press F12 if you would like to claim it");
                }
                else
                {
                    player.sendMessage(0, "This city/arena is unowned, however you already own a city.");
                }
                return;
            }

            _owner = _database.getOwner(_fileName);
            _structures = _database.loadStructures(_fileName);
            _units = _database.loadBots(_fileName);
            _items = _database.loadItems(_fileName);
            _attackers = _database.loadAttackers(_fileName);

            foreach (Structure b in _structures.Values)
            {
                Vehicle newVeh = _arena.newVehicle(b._type, _titan, null, b._state);

                if (newVeh == null)
                    Log.write("[RTS] Could not spawn vehicle");
                else
                {
                    newVeh._state.health = b._state.health;
                    b._vehicle = newVeh;
                    b.initState(newVeh);
                }
            }

            foreach (StoredItem item in _items.Values)
            {
                item._drop = _arena.itemSpawn(AssetManager.Manager.getItemByID(item._itemID), (ushort)item._quantity, item._posX, item._posY, null);
                item._key = _fileName;
            }


            _tickLastUpdate = Environment.TickCount;
            _tickLastBotUpdate = Environment.TickCount;
        }

        #region Events

        /// <summary>
        /// Called when a player enters the arena
        /// </summary>
        public void playerEnterArena(Player player)
        {

            //First player?
            if (_arena.TotalPlayerCount == 1)
            {
                if (_arena.TotalPlayerCount == 1 && player._permissionStatic < Data.PlayerPermission.Mod && _arena._bIsPublic)
                    player._permissionTemp = Data.PlayerPermission.Normal;

                //Turn the lights on
                init(player);
            }

            if (player._alias.ToLower() == _owner)
                player.sendMessage(0, String.Format("&Welcome home, {0}. Be sure to use ?rtshelp for a list of commands available to you", player._alias));

            //Obtain the Co-Op skill..
            SkillInfo coopskillInfo = _arena._server._assets.getSkillByID(200);
            SkillInfo powerupskillInfo = _arena._server._assets.getSkillByID(201);
            SkillInfo royaleskillInfo = _arena._server._assets.getSkillByID(203);
            SkillInfo rtsMode = _arena._server._assets.getSkillByID(202);

            //Add the skill!
            if (player.findSkill(200) != null)
                player._skills.Remove(200);

            if (player.findSkill(203) != null)
                player._skills.Remove(203);

            //Add the skill!
            if (player.findSkill(201) != null)
                player._skills.Remove(201);

            //Add the skill!
            if (player.findSkill(202) == null)
                player.skillModify(rtsMode, 1);
        }

        public bool playerPortal(Player player, LioInfo.Portal portal)
        {
            if (portal.GeneralData.Name.Contains("DS Portal"))
            {
                if (player._alias.ToLower() == _owner)
                {
                    Vehicle command = _arena.Vehicles.FirstOrDefault(v => v._type.Id == 414);
                    if (command != null)
                        player.warp(Helpers.ResetFlags.ResetNone, command._state, 200, -1, 0);
                    else
                        player.warp(12260, 7460);
                }
                else
                {
                    player.warp(12260, 7460);
                }

            }
            return false;
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
        /// Handles the spawn of a player
        /// </summary>
        public bool playerSpawn(Player player, bool bDeath)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a player wants to unspec and join the game
        /// </summary>
        public bool playerJoinGame(Player player)
        {
            if (!_database.tableExists(_fileName))
            {
                if (_database.playerOwnsCity(player))
                {
                    player.sendMessage(-1, "You already own a city");
                    return false;
                }

                newCity(false, player);

                player.sendMessage(0, "&You are now the owner of this city/arena. Exit the DS and place a Command Center to get started!");
                player.sendMessage(0, "Quick Tip: Each building has a click for info option that will give a brief list of what each option in that building does");
                player.unspec(_titan);
                return true;
            }

            if (!_bSquadCity)
            {
                if (player._alias.ToLower() == _owner)
                {
                    player.unspec(_titan);
                    return true;
                }
            }
            else
            {
                if (player._squad == _squad)
                {
                    player.unspec(_titan);
                    return true;
                }
            }

            if (player._alias.ToLower() != _owner)
            {
                if (_cityValue <= 1500)
                {
                    player.sendMessage(-1, "This city is too new to attack yet! City value must be over 1500");
                    return false;
                }

                if (_database.canAttack(player._alias, _fileName))
                {
                    player.unspec(_collective);
                    newAttacker(player);
                    return true;
                }
                else
                    player.sendMessage(-1, "You cannot attack this city currently, Please try again later");

            }

            return false;
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
            return true;
        }

        /// <summary>
        /// Triggered when one player has killed another
        /// </summary>
        public bool playerPlayerKill(Player victim, Player killer)
        {
            return true;
        }


        public bool playerProduce(Player player, Computer computer, VehInfo.Computer.ComputerProduct product)
        {
            switch (computer._type.Name)
            {
                case "[RTS] Command Center":
                    return tryCommandCenterMenu(player, computer, product);
                case "[RTS] Shack":
                    return tryCollectionMenu(player, computer, product, ProductionBuilding.Shack);
                case "[RTS] House":
                    return tryCollectionMenu(player, computer, product, ProductionBuilding.House);
                case "[RTS] Villa":
                    return tryCollectionMenu(player, computer, product, ProductionBuilding.Villa);
                case "[RTS] Marine Barracks":
                    return tryBarracksMenu(player, computer, product, DefenseProduction.Marine);
                case "[RTS] Ripper Barracks":
                    return tryBarracksMenu(player, computer, product, DefenseProduction.Ripper);
                case "[RTS] Factory - Housing":
                    return tryHousingProductionMenu(player, computer, product);
                case "[RTS] Refinery - Iron":
                    return tryIronRefinery(player, computer, product);
                case "[RTS] Factory - Defense":
                    return tryDefenseMenu(player, computer, product);
                case "[RTS] Factory - Production":
                    return tryProductionMenu(player, computer, product);
                case "[RTS] Iron Mine":
                    return tryCollectionMenu(player, computer, product, ProductionBuilding.Ironmine);
                case "[RTS] Scrapyard":
                    return tryCollectionMenu(player, computer, product, ProductionBuilding.Scrapyard);
            }
            return true;
        }

        /// <summary>
        /// Triggered when a vehicle dies
        /// </summary>
        public bool vehicleDeath(Vehicle dead, Player killer)
        {
            if (!dead._bBotVehicle)
            {
                Structure structure = _structures.Values.FirstOrDefault(st => st._vehicle._id == dead._id);

                if (structure == null)
                    return true;

                //Destroy it
                structure.destroyed(dead, killer);
            }
            return true;
        }

        public bool botDeath(Bot dead, Player killer)
        {
            Unit unit = _units.Values.FirstOrDefault(st => st._bot == dead);

            if (unit == null)
                return true;

            //Destroy it
            unit.destroyed(dead, killer);

            return true;
        }

        public bool playerMakeVehicle(Player player, ItemInfo.VehicleMaker item, short posX, short posY)
        {
            int count = _arena.Vehicles.Count(veh => veh._type.Id == item.vehicleID);
            int totalCount = _structures.Count;
            bool bSuccess = false;
            VehInfo vehicle = AssetManager.Manager.getVehicleByID(item.vehicleID);
            Structure commandCenter = _structures.Values.FirstOrDefault(st => st._vehicle._type.Id == 414);

            //Max Structures?
            if (totalCount >= c_maxStructures)
            {
                player.sendMessage(-1, "You have met or exceeded the maximum amount of structures allowed. " +
                    "To build more, you must sell another building");

                player.inventoryModify(item, 1);
                return false;
            }

            //Do we have a command center even?
            if (commandCenter == null && item.vehicleID != 414)
            {
                player.sendMessage(-1, "You currently do not have a active Command Center, please build one to continue..");
                player.inventoryModify(item, 1);
                return false;
            }

            switch (item.vehicleID)
            {
                //Command Center
                case 414:
                    {
                        if (count > 0)
                        {
                            player.sendMessage(-1, "You may only have one active Command Center.");
                            player.syncState();
                            bSuccess = false;
                        }
                        else if (!isAreaOpen(posX, posY, vehicle.PhysicalRadius))
                        {
                            player.sendMessage(-1, "Cannot construct building, Cannot construct buildings that close to eachother.");
                            bSuccess = false;
                        }
                        else
                            //Building!
                            bSuccess = true;
                    }
                    break;
                //Power Station
                case 423:
                    {
                        if (!isAreaOpen(posX, posY, vehicle.PhysicalRadius))
                        {
                            player.sendMessage(-1, "Cannot construct building, Cannot construct buildings that close to eachother.");
                            bSuccess = false;
                        }
                        else
                            //Building!
                            bSuccess = true;
                    }
                    break;

                //House 
                case 424:
                    {
                        int maxCount = commandCenter._productionLevel * 4;
                        if (count == maxCount)
                        {
                            player.sendMessage(-1, "You must upgrade your command center before you may build more of this building");
                            bSuccess = false;
                        }
                        else
                            bSuccess = canBuildInArea(player, vehicle);
                    }
                    break;
                //Iron Mine
                case 427:
                    {
                        int maxCount = commandCenter._productionLevel * 3;
                        if (count == maxCount)
                        {
                            player.sendMessage(-1, "You must upgrade your command center before you may build more of this building");
                            bSuccess = false;
                        }
                        else
                            bSuccess = canBuildInArea(player, vehicle);
                    }
                    break;
                //Scrapyard 
                case 428:
                    {
                        int maxCount = commandCenter._productionLevel * 3;
                        if (count == maxCount)
                        {
                            player.sendMessage(-1, "You must upgrade your command center before you may build more of this building");
                            bSuccess = false;
                        }
                        else
                            bSuccess = canBuildInArea(player, vehicle);
                    }
                    break;
                //Villa 
                case 425:
                    {
                        int maxCount = commandCenter._productionLevel * 4;
                        if (count == maxCount)
                        {
                            player.sendMessage(-1, "You must upgrade your command center before you may build more of this building");
                            bSuccess = false;
                        }
                        else
                            bSuccess = canBuildInArea(player, vehicle);
                    }
                    break;
                //Shack
                case 415:
                    {
                        int maxCount = commandCenter._productionLevel * 4;
                        if (count == maxCount)
                        {
                            player.sendMessage(-1, "You must upgrade your command center before you may build more of this building");
                            bSuccess = false;
                        }
                        else
                            bSuccess = canBuildInArea(player, vehicle);
                    }
                    break;

                //Catch all
                default:
                    {
                        bSuccess = canBuildInArea(player, vehicle);
                    }
                    break;
            }
            //Give them back their kit if it failed.
            if (!bSuccess)
                player.inventoryModify(item, 1);

            return bSuccess;
        }

        /// <summary>
        /// Triggered when a vehicle is created
        /// </summary>
        /// <remarks>Doesn't catch spectator or dependent vehicle creation</remarks>
        public bool vehicleCreation(Vehicle created, Team team, Player creator)
        {
            if (creator != null && !created._bBotVehicle)
            newStructure(created, creator);

            return true;
        }


        public bool playerItemPickup(Player player, Arena.ItemDrop drop, ushort quantity)
        {
            StoredItem item = _items.Values.FirstOrDefault(itm => itm._drop.id == drop.id);
            if (item != null)
            {
                if (drop.quantity == 0)
                    item.remove();
                else
                {
                    item._quantity -= (short)quantity;
                    _database.updateItem(item, _fileName);
                }
            }
            return true;
        }

        public bool playerItemDrop(Player player, ItemInfo item, ushort quantity)
        {
            Arena.ItemDrop newDrop = null;

            //Droppable?
            if (!item.droppable)
                return false;

            if (player.inventoryModify(item, -quantity))
            {   //Create an item spawn
                newDrop = _arena.itemSpawn(item, quantity, player._state.positionX, player._state.positionY, 0, (int)player._team._id, player);
                newDrop.tickExpire = 0;
            }

            if (player._alias.ToLower() == _owner)
                if (newDrop != null)
                newItem(newDrop);

            return false;
        }

        #endregion

        #region Private Routines
        public void newStructure(Vehicle veh, Player player)
        {

            Structure newStruct = new Structure(this);
            newStruct._vehicle = veh;
            newStruct._state = veh._state;
            newStruct._productionLevel = 1;
            newStruct._id = (ushort)(_database.getLastStructureID(_fileName) + 1);
            newStruct._key = _fileName;
            newStruct._type = veh._type;

            switch (veh._type.Name)
            {
                case "[RTS] Shack":
                    {
                        newStruct._upgradeCost = c_baseShackUpgrade;
                        newStruct._productionQuantity = c_baseShackProduction;
                        newStruct._nextProduction = DateTime.Now.AddHours(c_shackProductionInterval);
                    }
                    break;
                case "[RTS] House":
                    {
                        newStruct._upgradeCost = c_baseHouseUpgrade;
                        newStruct._productionQuantity = c_baseHouseProduction;
                        newStruct._nextProduction = DateTime.Now.AddHours(c_houseProductionInterval);
                    }
                    break;
                case "[RTS] Villa":
                    {
                        newStruct._upgradeCost = c_basevillaUpgrade;
                        newStruct._productionQuantity = c_baseVillaProduction;
                        newStruct._nextProduction = DateTime.Now.AddHours(c_villaProductionInterval);
                    }
                    break;
                case "[RTS] Scrapyard":
                    {
                        newStruct._productionItem = AssetManager.Manager.getItemByID(2026);
                        newStruct._upgradeCost = c_baseScrapUpgrade;
                        newStruct._productionQuantity = c_baseScrapProduction;
                        newStruct._nextProduction = DateTime.Now.AddHours(c_baseScrapProductionInterval);
                    }
                    break;
                case "[RTS] Iron Mine":
                    {
                        newStruct._productionItem = AssetManager.Manager.getItemByID(2027);
                        newStruct._upgradeCost = c_baseIronMineUpgrade;
                        newStruct._productionQuantity = c_baseIronProduction;
                        newStruct._nextProduction = DateTime.Now.AddHours(c_baseIronProductionInterval);
                    }
                    break;
                case "[RTS] Command Center":
                    {
                        newStruct._upgradeCost = c_baseCCUpgrade;
                    }
                    break;
                default:
                    {
                        newStruct._productionItem = AssetManager.Manager.getItemByID(323);
                        newStruct._productionQuantity = 0;
                        newStruct._productionLevel = 1;
                    }
                    break;
            }


            newStruct.initState(veh);

             _database.addStructure(newStruct, _fileName);
            _structures.Add(newStruct._id, newStruct);
        }

        public void newUnit(BotType type, Vehicle target, Player owner, BotLevel level, Helpers.ObjectState state = null)
        {
            Unit newUnit = new Unit(this);

            Bot bot = newBot(_titan, type, target, owner, level, state);
            newUnit._vehicleID = (ushort)bot._type.Id;
            newUnit._state = state;
            newUnit._id = (ushort)(_database.getLastBotID(_fileName) + 1);
            newUnit._bot = bot;
            _database.addBot(bot, _fileName);
            _units.Add(bot._id, newUnit);
        }

        public void newItem(Arena.ItemDrop drop)
        {
            ushort id = (ushort)(_database.getLastItemID(_fileName) + 1);

            StoredItem newItem = new StoredItem(this);
            newItem._id = id;
            newItem._itemID = drop.item.id;
            newItem._quantity = drop.quantity;
            newItem._posX = drop.positionX;
            newItem._posY = drop.positionY;
            newItem._key = _fileName;
            newItem._drop = drop;

            _database.addItem(drop.item.id, drop.positionX, drop.positionY, drop.quantity, _fileName);
            _items.Add(id, newItem);
        }

        private void newAttacker(Player player)
        {
            DateTime nextAttack = DateTime.Now.AddHours(24);

            
            Attacker newAttacker = new Attacker(this);
            newAttacker._alias = player._alias;
            newAttacker._attackExpire = nextAttack;
            newAttacker._tickExpire = Environment.TickCount + (300 * 1000);

            _attackers.Add(player._alias, newAttacker);
            _database.addAttacker(nextAttack, player._alias, _fileName);

            player.sendMessage(0, "You have 5 minutes to conduct your attack, at the end of the timer, you will be auto specced and unable to attack until 24 hours have elapsed.");

            _arena.setTicker(2, 1, 300 * 100, delegate (Player p)
            {
                //Update their ticker
                if (_attackers.ContainsKey(p._alias))
                    return string.Format("Attacking time remaining: ");
                return "";
            });


        }

        private void newCity(bool squadCity, Player player)
        {
            _database.createTable(_fileName, player._alias, player._squad, squadCity);
        }

        #endregion

        #region Helpers
        public bool canBuildInArea(Player player, VehInfo vehicle)
        {
            bool bSuccess = false;

            if (!isAreaOpen(player._state.positionX, player._state.positionY, vehicle.PhysicalRadius))
            {
                player.sendMessage(-1, "Cannot construct building, Cannot construct buildings that close to eachother.");
                bSuccess = false;
            }
            else if (!powerStationInArea(player._state.positionX, player._state.positionY))
            {
                player.sendMessage(-1, "Cannot construct building, too far from a power station.");
                bSuccess = false;
            }
            else
            {
                //Building...
                bSuccess = true;
            }

            return bSuccess;
        }

        public bool isAreaOpen(short posX, short posY, int radius)
        {
            return (_arena.getVehiclesInRange(posX, posY, radius + 200).Count(veh => veh._type.Name.StartsWith("[RTS]")) == 0);
        }

        public bool powerStationInArea(short posX, short posY)
        {
            return (_arena.getVehiclesInRange(posX, posY, 500).Count(veh => veh._type.Name == "[RTS] Power Station") > 0);
        }
        #endregion

    }
}