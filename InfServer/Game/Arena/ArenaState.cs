using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using InfServer.Network;
using InfServer.Protocol;
using InfServer.Data;
using InfServer.Logic;

using Assets;

namespace InfServer.Game
{
    // Arena Class
    /// Represents a single arena in the server
    ///////////////////////////////////////////////////////
    public partial class Arena : IChatTarget
    {	// Member variables
        ///////////////////////////////////////////////////
        protected Dictionary<string, Team> _teams;			//The list of teams, indexed by name
        protected SortedDictionary<int, Team> _freqTeams;	//The list of teams, indexed by frequency

        protected ObjTracker<Vehicle> _vehicles;			//The vehicles belonging to the arena, indexed by id
        private List<Vehicle> _condemnedVehicles;			//Vehicles to be deleted
        private ushort _lastVehicleKey;						//The last vehicle key which was allocated

        public SortedDictionary<ushort, ItemDrop> _items;	//The items belonging to the arena, indexed by id
        public ushort _lastItemKey;							//The last item key which was allocated

        protected ObjTracker<Ball> _balls;                  //The soccer balls belonging to the arena, indexed by id

        public Dictionary<string, PlayerStats> _currentGameStats; //Our current running game stats

        ///////////////////////////////////////////////////
        // Accessors
        ///////////////////////////////////////////////////
        /// <summary>
        /// Returns a list of public teams
        /// </summary>
        public IEnumerable<Team> PublicTeams
        {
            get
            {
                return _teams.Values.Where(team => team.IsPublic);
            }
        }

        /// <summary>
        /// Returns a list of teams present in the arena
        /// </summary>
        public IEnumerable<Team> Teams
        {
            get
            {
                return _teams.Values;
            }
        }

        /// <summary>
        /// Returns a list of teams with active players
        /// </summary>
        public IEnumerable<Team> ActiveTeams
        {
            get
            {
                return _teams.Values.Where(t => t.ActivePlayerCount > 0 && !t.IsSpec);
            }
        }

        /// <summary>
        /// Returns a list of desired public teams
        /// </summary>
        public IEnumerable<Team> DesiredTeams
        {
            get
            {
                return _teams.Values.Where(t => t._id < t._server._zoneConfig.arena.desiredFrequencies);
            }
        }

        /// <summary>
        /// Returns a list of vehicles present in the arena
        /// </summary>
        public IEnumerable<Vehicle> Vehicles
        {
            get
            {
                return _vehicles;
            }
        }

        /// <summary>
        /// Returns a list of balls present in the arena
        /// </summary>
        public IEnumerable<Ball> Balls
        {
            get
            {
                return _balls;
            }
        }

        ///////////////////////////////////////////////////
        // Member Functions
        ///////////////////////////////////////////////////
        #region Init State
        /// <summary>
        /// Initializes arena details
        /// </summary>
        private void initState()
        {	//First a spectator team
            Team newTeam = new Team(this, _server);

            newTeam._isPrivate = true;
            newTeam._name = "spec";
            newTeam._id = 1000;
            _teams.Add("spec", newTeam);

            //computer team -1, shoots at nobody
            newTeam = new Team(this, _server);
            newTeam._isPrivate = true;
            newTeam._id = -1;
            _freqTeams.Add(-1, newTeam);

            //computer team 9999, shoots at everybody
            newTeam = new Team(this, _server);
            newTeam._isPrivate = true;
            newTeam._id = 9999;
            _freqTeams.Add(9999, newTeam);

            //Create all our teams, as per the zone config
            int id = 0;

            foreach (CfgInfo.TeamInfo ti in _server._zoneConfig.teams)
            {
                //Duplicate checking..
                if (_teams.ContainsKey(ti.name.ToLower()))
                {
                    //Log it and continue
                    Log.write(TLog.Warning, "Found a duplicated team name ({0}) in the .cfg.", ti.name);
                    continue;
                }

                //Populate the new class
                newTeam = new Team(this, _server);

                newTeam._name = ti.name;
                newTeam._id = (short)id;

                newTeam._info = ti;
                newTeam._relativeVehicle = ti.relativeVehicle;

                _teams.Add(ti.name.ToLower(), newTeam);

                _freqTeams.Add(id++, newTeam);
            }
        }
        #endregion

        #region State
        /// <summary>
        /// Called when a new player is entering our arena
        /// </summary>
        public void newPlayer(Player player)
        {   ///////////////////////////////////////////////
            // Prepare the player state
            ///////////////////////////////////////////////       

            //First player entering a inactive named arena? Let's flip on the lights for him
            if (_bIsNamed && !_bActive)
                _bActive = true;

            //We're entering the arena..
            player._arena = this;

            RecallPlayerStats(player);
            player.resetVars();

            player._ipAddress = player._client._ipe.Address;

            //TODO: Check rules for whether player enters in spec
            //_server._zoneConfig.arena.startInSpectator
            player._bSpectator = true;
            player._team = _teams["spec"];

            //Find his natural vehicle id and prepare the class
            Player.SkillItem baseSkill = player._skills.Values.FirstOrDefault(skill => skill.skill.DefaultVehicleId != -1);
            int baseVehicleID = (baseSkill == null) ? _server._zoneConfig.publicProfile.defaultVItemId : baseSkill.skill.DefaultVehicleId;
            Vehicle baseVehicle = new Vehicle(_server._assets.getVehicleByID(baseVehicleID), this);

            baseVehicle._bBaseVehicle = true;
            baseVehicle._id = player._id;
            baseVehicle._state = player._state;		//Player and basevehicle share same state

            player._baseVehicle = baseVehicle;

            //Run the initial events
            if (_server.IsStandalone)
                player.firstTimeEvents();

            if (player.firstTimePlayer)
            {
                Logic_Assets.RunEvent(player, _server._zoneConfig.EventInfo.firstTimeSkillSetup);
                Logic_Assets.RunEvent(player, _server._zoneConfig.EventInfo.firstTimeInvSetup);
                if (player._bIngame)
                    player.firstTimePlayer = false;
            }

            //Create our new spam filter list
            player._msgTimeStamps = new List<DateTime>();

            ///////////////////////////////////////////////
            // Send the player state
            ///////////////////////////////////////////////
            //Make sure he's receiving ingame packets
            player._client.sendReliable(new SC_SetIngame());

            //Add him to our list of players. We want to do this now so he doesn't lose 
            //info about anything happening until then.
            if (!_players.Contains(player))
                _players.Add(player);

            //Check his processes quick
            SC_Environment env = new SC_Environment();
            env.bLimitLength = false;
            player._client.sendReliable(env);

            //Lets check his level and set watchMod
            if (player.PermissionLevel >= Data.PlayerPermission.ArenaMod)
                player._watchMod = true;

            //Check if we can use him as a reliable player [check if mod]
            if (player.PermissionLevel >= Data.PlayerPermission.ArenaMod)
                player.setVar("reliable", player);

            //Send a security check for their client asset checksum
            SC_SecurityCheck cs = new SC_SecurityCheck();
            cs.key = 1015; //Key we are using
            cs.unknown = 0; // Unknown, send as 0   
            player._client.send(cs); //Send it    

            //Define the player's self object
            Helpers.Object_Players(player, player);

            //Make sure the player is aware of every player in the arena
            List<Player> audience = Players.ToList();

            //Check first for stealthed mods
            //Note: this is for any other players joining while there is a stealthed mod
            foreach (Player p in audience)
            {
                //Check their levels with the players level
                if (p != player)
                {
                    if (!p.IsStealth)
                        Helpers.Object_Players(player, p);
                    if (p.IsStealth && player.PermissionLevel >= p.PermissionLevel)
                        Helpers.Object_Players(player, p);
                }
            }

            //Load the arena's item state  
            Helpers.Object_Items(player, _items.Values);

            //Load the arena's various lio objects
            Helpers.Object_Flags(player, _flags.Values);
            Helpers.Object_LIOs(player, _switches.Values);

            //Load the vehicles in the arena
            if (_vehicles.Count > 0)
                Helpers.Object_Vehicles(player, _vehicles);

            //Load the ball state if any
            if (_balls.Count > 0)
                Helpers.Object_Ball(player, _balls);

            //Suspend his stats if it's a private arena
            if (_bIsPublic)
            {
                player.restoreStats();
                player.suspendCalled = false;
            }
            else if (!player.suspendCalled)
            {
                player.suspendStats();
                player.suspendCalled = true;
            }

            //Is this a private arena and are we the first one?
            if (!player._arena._name.StartsWith("Public", StringComparison.OrdinalIgnoreCase) && player._arena.TotalPlayerCount == 1)
            {
                //Give player required privileges
                player._arena._owner.Add(player._alias);
                if (player.PermissionLevel < Data.PlayerPermission.ArenaMod)
                {
                    player._permissionTemp = Data.PlayerPermission.ArenaMod;
                    player._watchMod = true;
                }
            }

            //Are we zone silenced or arena silenced?
            if (!this._server._playerSilenced.ContainsKey(player._ipAddress))
            {   //Since we are not in the zone list, check the arena list
                if (!this._silencedPlayers.ContainsKey(player._alias))
                    player._bSilenced = false;
                else
                    player._bSilenced = true;
            }

            //Initialize the player's state
            Helpers.Player_StateInit(player,
                delegate()
                {
                    //Check for stealthing/cloaking mods first
                    //Note: this is for the stealthed person entering
                    if (!player.IsStealth)
                        //Make sure everyone is aware of him
                        Helpers.Object_Players(audience, player);
                    else
                    { //Check their level vs people in the room
                        foreach (Player person in audience)
                            //Their level is the same or greater, allow them to see him/her
                            if (person != player && person.PermissionLevel >= player.PermissionLevel)
                                Helpers.Object_Players(person, player);
                    }

                    //Consider him loaded!
                    player.spec();
                    player.setIngame();

                    //Load the tickers
                    Helpers.Arena_Message(player, _tickers.Values);

                    //Load all the banners
                    Helpers.Social_UpdateBanner(player); //Players banner
                    Helpers.Social_ArenaBanners(player._arena.Players, player); //Inform arena of his banner
                    Helpers.Social_ArenaBanners(player, this); //Get all banners in arena
                    //Set able to receive banners
                    player._bAllowBanner = true;

                    //Trigger our event for player entering arena
                    callsync("Player.EnterArena", false, player);

                    //Temporary player message, remove this later. This is just here to get old accounts to update their information
                    player.sendMessage(-3, "[Notice] Welcome to Infantry! To get support simply use the discord link located on the top right of the infantry launcher. Enjoy your stay!");

                    //Mod notice
                    if (player.PermissionLevelLocal >= Data.PlayerPermission.ArenaMod && !player._arena.IsPrivate)
                        player.sendMessage(-3, "$[Mod Notice] To see a list of commands, type *help. To specifically get info on a command type *help <command name>");
                }
            );
        }

        /// <summary>
        /// Resets all game-specific vehicles in the arena
        /// </summary>
        public void resetVehicles()
        {	//Kill each vehicle which isn't a spectator
            List<Vehicle> vehicles = new List<Vehicle>(_vehicles);

            foreach (Vehicle veh in vehicles)
                if (veh._type.Type != VehInfo.Types.Spectator)
                    veh.destroy(true, true);
        }

        /// <summary>
        /// Resets all items in the arena
        /// </summary>
        public void resetItems()
        {	//Get rid of each item
            foreach (ItemDrop itm in _items.Values)
                itm.quantity = 0;

            Helpers.Object_ItemDrops(Players, _items.Values);
            _items.Clear();
        }

        /// <summary>
        /// Reset all active balls in the arena
        /// </summary>
        public void resetBalls()
        {   //Get rid of each ball
            foreach (Ball b in _balls.ToList())
                lostBall(b);
        }

        /// <summary>
        /// Handles the loss of a player
        /// </summary>
        public void lostPlayer(Player player)
        {
            //Sob, let him go
            _players.Remove(player);

            //Trigger our event for a player leaving a game
            if (!player.IsSpectator)
                playerLeave(player);

            //Trigger our event for player leaving an arena
            callsync("Player.LeaveArena", false, player);

            player.onLeaveArena();

            //Check owner list and appropriate powers
            if (player._arena._owner != null && player._arena._owner.Count > 0)
            {
                foreach (var p in player._arena._owner)
                {
                    if (player._alias.Equals(p))
                    {
                        player._arena._owner.Remove(p);
                        if (player._permissionTemp >= Data.PlayerPermission.ArenaMod
                            && player.PermissionLevel < Data.PlayerPermission.ArenaMod)
                        {
                            player._permissionTemp = Data.PlayerPermission.Normal;
                        }
                        break;
                    }
                }
            }

            //Do we have any players left? Don't close named arenas (We always want these displayed so players know they exist)
            if (TotalPlayerCount == 0 && !_bIsNamed)
            {
                //Nope. It's closing time.
                close();
                //Flag the arena as inactive so it's no longer polled
                _bActive = false;
            }
            else
            {
                //Notify everyone else of his departure
                if (!player.IsStealth)
                {
                    Helpers.Object_PlayerLeave(Players, player);
                }
                else
                {
                    foreach (Player person in Players.ToList())
                        if (person.PermissionLevel >= player.PermissionLevel)
                            Helpers.Object_PlayerLeave(person, player);
                }
            }
        }

        /// <summary>
        /// Handles the loss of a vehicle
        /// </summary>
        public void lostVehicle(Vehicle vehicle, bool bRemove)
        {	//Sob, let it go
            if (bRemove)
                vehicle.bCondemned = true;

            //Notify everyone else of it's destruction
            Helpers.Object_VehicleDestroy(Players, vehicle);
        }

        /// <summary>
        /// Creates and adds a new vehicle to the arena
        /// </summary>
        public Vehicle newVehicle(ushort type)
        {	//Redirect
            return newVehicle(_server._assets.getVehicleByID(type), null, null, null, null);
        }

        /// <summary>
        /// Creates and adds a new vehicle to the arena
        /// </summary>
        public Vehicle newVehicle(VehInfo type, Team team, Player creator)
        {	//Redirect
            return newVehicle(type, team, creator, null, null);
        }

        /// <summary>
        /// Creates and adds a new vehicle to the arena
        /// </summary>
        public Vehicle newVehicle(VehInfo type, Team team, Player creator, Helpers.ObjectState state)
        {	//Redirect
            return newVehicle(type, team, creator, state, null);
        }

        /// <summary>
        /// Creates and adds a new vehicle to the arena
        /// </summary>
        public Vehicle newVehicle(VehInfo type, Team team, Player creator, Helpers.ObjectState state, Action<Vehicle> setupCB)
        {
            return newVehicle(type, team, creator, state, setupCB, null);
        }

        /// <summary>
        /// Creates and adds a new vehicle to the arena
        /// </summary>
        public Vehicle newVehicle(VehInfo type, Team team, Player creator, Helpers.ObjectState state, Action<Vehicle> setupCB, Type classType)
        {	//Too many vehicles?
            if (_vehicles.Count == maxVehicles)
            {
                Log.write(TLog.Warning, "Vehicle list full.");
                return null;
            }

            //We want to continue wrapping around the vehicleid limits
            //looking for empty spots.
            ushort vk;

            for (vk = _lastVehicleKey; vk <= UInt16.MaxValue; ++vk)
            {	//If we've reached the maximum, wrap around
                if (vk == UInt16.MaxValue)
                {
                    vk = 5001;
                    continue;
                }

                //Does such a vehicle exist?
                if (_vehicles.getObjByID(vk) != null)
                    continue;

                //We have a space!
                break;
            }

            //TODO: There might be some kind of strange bug regarding re-used vehicle
            //		ids, even if you attempt to dispose of them.
            _lastVehicleKey = (ushort)(vk + 1);

            //Create our vehicle class		
            Vehicle veh;

            if (classType == null)
            {
                if (type.Type == VehInfo.Types.Computer)
                    veh = new Computer(type as VehInfo.Computer, this);
                else
                    veh = new Vehicle(type, this);
            }
            else
                veh = Activator.CreateInstance(classType, type, this) as Vehicle;

            veh._id = vk;

            veh._team = team;
            veh._creator = creator;
            veh._oldTeam = creator != null ? creator._team : null;

            veh._tickUnoccupied = veh._tickCreation = Environment.TickCount;

            if (state != null)
            {
                veh._state.positionX = state.positionX;
                veh._state.positionY = state.positionY;
                veh._state.positionZ = state.positionZ;
                veh._state.yaw = state.yaw;

                if (veh._type.Type == VehInfo.Types.Computer)
                    veh._state.fireAngle = state.yaw; //Temporary fix for computer updates rotating north by default, perm fix would be sc_vehices.cs packet fix
                if (veh._type.Type == VehInfo.Types.Dependent)
                {
                    VehInfo.Dependent dep = veh._type as VehInfo.Dependent;
                    veh._state.pitch = (byte)(dep.ChildElevationLowAngle > 0 ? dep.ChildElevationLowAngle : 0);
                }
            }

            veh.assignDefaultState();

            //Custom setup?
            if (setupCB != null)
                setupCB(veh);

            //This uses the new ID automatically
            _vehicles.Add(veh);

            //Notify everyone of the new vehicle
            Helpers.Object_Vehicles(Players, veh);

            //Handle dependent vehicles?
            int slot = 0;

            foreach (int vid in veh._type.ChildVehicles)
            {	//Nothing?
                slot++;

                if (vid <= 0)
                    continue;

                //Find the vehicle type
                VehInfo childType = _server._assets.getVehicleByID(vid);

                if (childType == null)
                {
                    Log.write(TLog.Error, "Invalid child vehicle id '{0}' for {1}.", vid, type);
                    continue;
                }

                //Create it!
                Vehicle child = newVehicle(childType, team, creator, state,
                    delegate(Vehicle c)
                    {
                        c._parent = veh;
                        c._parentSlot = slot - 1;
                    }
                );

                veh._childs.Add(child);

                //Notify everyone of the new vehicle
                Helpers.Object_Vehicles(Players, child);
            }

            //If it's not a spectator or dependent vehicle, let the arena pass it to the script
            if (type.Type != VehInfo.Types.Dependent && type.Type != VehInfo.Types.Spectator)
                handleVehicleCreation(veh, team, creator);

            return veh;
        }

        #region Vehicle Get Functions
        /// <summary>
        /// Gets all vehicles within the specified range
        /// </summary>
        public List<Vehicle> getVehiclesInRange(int posX, int posY, int range)
        {
            return _vehicles.getObjsInRange(posX, posY, range);
        }

        /// <summary>
        /// Gets all vehicles within the specified range
        /// </summary>
        public List<Vehicle> getVehiclesInRange(int posX, int posY, int range, Predicate<Vehicle> predicate)
        {
            return _vehicles.getObjsInRange(posX, posY, range, predicate);
        }

        /// <summary>
        /// Gets all vehicles within the specified box
        /// </summary>
        public List<Vehicle> getVehiclesInBox(int posX, int posY, int width, int height)
        {	//Extrapolate
            width /= 2;
            height /= 2;

            return getVehiclesInArea(posX - width, posY - height, posX + width, posY + height);
        }

        /// <summary>
        /// Gets all vehicles within the specified area
        /// </summary>
        public List<Vehicle> getVehiclesInArea(int topX, int topY, int bottomX, int bottomY)
        {
            return _vehicles.getObjsInArea(topX, topY, bottomX, bottomY);
        }

        /// <summary>
        /// Gets all vehicles within the specified area
        /// </summary>
        public List<Vehicle> getVehiclesInArea(int topX, int topY, int bottomX, int bottomY, Predicate<Vehicle> predicate)
        {
            return _vehicles.getObjsInArea(topX, topY, bottomX, bottomY, predicate);
        }


        /// <summary>
        /// Gets the amount of vehicles within the specified area
        /// </summary>
        public int getVehicleCountInArea(int topX, int topY, int bottomX, int bottomY)
        {
            return _vehicles.getObjcountInArea(topX, topY, bottomX, bottomY);
        }
        #endregion

        /// <summary>
        /// Creates an item drop at the specified location
        /// </summary>
        public ItemDrop itemSpawn(ItemInfo item, ushort quantity, short positionX, short positionY, short range, Player p)
        {
            int attempts = 0;

            while (true)
            {	//Make sure we're not doing this infinitely
                if (attempts++ > 200)
                    break;

                //Generate some random coordinates
                short pX = positionX;
                short pY = positionY;

                Helpers.randomPositionInArea(this, ref pX, ref pY, (short)range, (short)range);

                //Is it blocked?
                if (getTile(pX, pY).Blocked)
                    //Try again
                    continue;

                return itemSpawn(item, quantity, (short)pX, (short)pY, p);
            }

            return null;
        }

        /// <summary>
        /// Creates an item drop at the specified location
        /// </summary>
        public ItemDrop itemSpawn(ItemInfo item, ushort quantity, short positionX, short positionY, int relativeID, int freq, Player p)
        {
            if (item == null)
            {
                Log.write(TLog.Error, "Attempted to spawn invalid item.");
                return null;
            }

            if (quantity == 0)
            {
                Log.write(TLog.Warning, "Attempted to spawn 0 of an item.");
                return null;
            }

            //Too many items?
            if (_items.Count == maxItems)
            {
                Log.write(TLog.Warning, "Item count full.");
                return null;
            }

            if (item.itemType == ItemInfo.ItemType.Multi)
            {   //Do we need to expand?
                ItemInfo.MultiItem multi = item as ItemInfo.MultiItem;
                if (multi.ExpandRadius != 0)
                {
                    int blockedAttempts = 30;
                    ItemDrop spawn = null;
                    foreach (ItemInfo.MultiItem.Slot it in multi.slots)
                    {
                        if (it.value == 0)
                            break;

                        short pX;
                        short pY;
                        while (true)
                        {
                            pX = positionX;
                            pY = positionY;
                            Helpers.randomPositionInArea(this, _server._zoneConfig.arena.pruneDropRadius, ref pX, ref pY);
                            if (getTile(pX, pY).Blocked)
                            {
                                blockedAttempts--;
                                if (blockedAttempts <= 0)
                                    //Consider the spawn to be blocked
                                    return null;
                                continue;
                            }
                            //Consider odds of dropping
                            ItemInfo current = _server._assets.getItemByID(it.value);
                            if (current.pruneOdds != 1000)
                                if (_rand.Next(0, 1000) >= current.pruneOdds)
                                    break;

                            spawn = itemSpawn(_server._assets.getItemByID(it.value), (ushort)1, pX, pY, 0, freq, p);
                            break;
                        }
                    }
                    return spawn;
                }
            }

            //We want to continue wrapping around the vehicleid limits
            //looking for empty spots.
            ushort ik;

            for (ik = _lastItemKey; ik <= Int16.MaxValue; ++ik)
            {	//If we've reached the maximum, wrap around
                if (ik == Int16.MaxValue)
                {
                    ik = (ushort)ZoneServer.maxPlayers;
                    continue;
                }

                //Does such an item exist?
                if (_items.ContainsKey(ik))
                    continue;

                //We have a space!
                break;
            }

            _lastItemKey = ik;

            //Create our drop class		
            ItemDrop id = new ItemDrop();

            id.item = item;
            id.id = ik;
            id.quantity = (short)quantity;
            id.positionX = positionX;
            id.positionY = positionY;
            id.relativeID = (relativeID == 0 ? item.relativeID : relativeID);
            id.freq = freq;

            id.owner = p; //For bounty abuse upon pickup

            int expire = getTerrain(positionX, positionY).prizeExpire;
            id.tickExpire = (expire > 0 ? (Environment.TickCount + (expire * 1000)) : 0);

            //Add it to our list
            _items[ik] = id;

            //Notify the arena
            Helpers.Object_ItemDrop(Players, id);
            return id;
        }

        public ItemDrop itemSpawn(ItemInfo item, ushort quantity, short positionX, short positionY, Player p)
        {
            return itemSpawn(item, quantity, positionX, positionY, 0, -1, p);
        }

        /// <summary>
        /// Updates a stack of items to increase the quantity or creates item drop
        /// </summary>
        public ItemDrop itemStackSpawn(ItemInfo item, ushort quantity, short positionX, short positionY, short range, Player p)
        {
            //Too many items?
            if (_items.Count == maxItems)
            {
                Log.write(TLog.Warning, "Item count full.");
                return null;
            }
            else if (item == null)
            {
                Log.write(TLog.Error, "Attempted to spawn invalid item.");
                return null;
            }

            ItemDrop id = null;
            //Returns an ItemDrop object if there is another of the same item within the range
            id = getItemInRange(item, positionX, positionY, range);

            //If another item exist add to its quantity rather than placing another item
            if (id != null)
            {
                id.quantity += (short)quantity;
                Helpers.Object_ItemDropUpdate(Players, id.id, (ushort)id.quantity);
            }
            else
            {
                //Add new item if none nearby exists
                itemSpawn(item, quantity, positionX, positionY, p);
            }
            return id;
        }

        /// <summary>
        /// Creates a new ball and adds it to our tracking list
        /// </summary>
        public Ball newBall(short ballID)
        {
            //Maxed out?
            if (_balls.Count == Arena.maxBalls)
                return null;

            //Do we exist?
            if (_balls.getObjByID((ushort)ballID) != null)
                return null;

            //Create our ball object
            Ball ball = new Ball(ballID, this);
            if (ball != null)
            {   //Add it to the arena tracker
                _balls.Add(ball);

                return ball;
            }

            return null;
        }

        /// <summary>
        /// Handles the loss of a ball
        /// </summary>
        public void lostBall(Ball ball)
        {
            if (ball == null)
                return;

            //Let it go
            _balls.Remove(ball);

            //Send it
            Helpers.Object_BallReset(Players, ball);
        }

        /// <summary>
        /// Updates spatial data for the ball
        /// </summary>
        public void updateBall(Ball ball)
        {
            if (ball == null)
                return;

            //Do we exist?
            Ball b = _balls.getObjByID(ball._id);
            if (b == null)
            {
                Log.write(TLog.Warning, "Trying to update an invalid ball id.");
                return;
            }

            //Lets update
            _balls.updateObjState(b, b._state);

            //Update the players
            Helpers.Object_Ball(Players, ball);
        }

        /// <summary>
        /// Handles the ball action when a player dies carrying it
        /// </summary>
        public void ballResetPlayer(Player from)
        {
            //Route
            ballResetPlayer(from, null);
        }

        /// <summary>
        /// Handles the ball action when a player dies carrying it
        /// </summary>
        public void ballResetPlayer(Player from, Player killer)
        {
            if (from == null)
                return;

            Ball ball = _balls.SingleOrDefault(b => b._owner != null && b._owner == from);
            if (ball == null)
                return;

            //Make sure they arent carrying one now
            from._gotBallID = 999;

            //Are we giving it to the killer?
            if (killer != null && _server._zoneConfig.soccer.killerCatchBall)
            {
                //Give it to the killer
                killer._gotBallID = ball._id;

                ball._lastOwner = from;
                ball._owner = killer;

                ball.ballStatus = 0; //Picked up

                ball._state.positionX = killer._state.positionX;
                ball._state.positionY = killer._state.positionY;
                ball._state.positionZ = killer._state.positionZ;
                ball._state.velocityX = 0;
                ball._state.velocityY = 0;
                ball._state.velocityZ = 0;
                ball.deadBall = false;

                int now = Environment.TickCount;
                int updateTick = ((now >> 16) << 16) + (ball._state.lastUpdate & 0xFFFF);
                ball._state.lastUpdate = updateTick;
                ball._state.lastUpdateServer = now;

                ball.tickCount = (uint)now;

                //Route
                updateBall(ball);
                return;
            }

            //Just spawn it in place instead
            Ball.Spawn_Ball(ball, from._state.positionX, from._state.positionY);
        }

        /// <summary>
        /// Once a game begins, this will add them to the stat object
        /// </summary>
        public void AddArenaStat(Player p)
        {
            if (p == null)
                return;

            if (!_currentGameStats.ContainsKey(p._alias))
                _currentGameStats.Add(p._alias.ToString(), new PlayerStats());
        }

        /// <summary>
        /// If the game is still under way, it will recall the stats. If not, migrate them.
        /// </summary>
        public void RecallPlayerStats(Player p)
        {
            if (p == null)
                return;

            //If this person doesn't exist, it means the list either got cleared or 
            //its their first time in this arena so migrate their stats.
            if (!_currentGameStats.ContainsKey(p._alias))
            {
                _currentGameStats.Add(p._alias.ToString(), new PlayerStats());
                p.migrateStats();
                return;
            }
            p.StatsCurrentGame = _currentGameStats[p._alias];
        }

        /// <summary>
        /// Clears the current stats the arena saved
        /// </summary>
        public void ClearCurrentStats()
        {
            if (_currentGameStats != null)
                _currentGameStats.Clear();
        }
        #endregion
    }
}