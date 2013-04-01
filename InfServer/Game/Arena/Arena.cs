using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using InfServer.Network;
using InfServer.Protocol;

using Assets;

namespace InfServer.Game
{
	// Arena Class
	/// Represents a single arena in the server
	///////////////////////////////////////////////////////
	public abstract partial class Arena : CustomObject, IChatTarget, IEventObject		
	{	// Member variables
		///////////////////////////////////////////////////
		public LogClient _logger;						//The logger we use for this arena!
		public volatile bool _bActive;					//Is the arena functioning, or condemned?
		public bool _bIsPublic;							//Is this a public arena?

		public ZoneServer _server;						//The server we belong to
		public Bots.Pathfinder _pathfinder;				//The pathfinding object used for this arena

		protected ObjTracker<Player> _players;			//The list of players in this arena
		protected ObjTracker<Player> _playersIngame;	//The list of players currently ingame

        public List<string> _owner;                     //The owner's name of this arena - we use a list because we can grant players
        public Dictionary<Arena, Player> _notAllowed;   //Banned list for owned arenas

        public List<Ball> _balls;

		public string _name;							//The name of this arena

		public Random _rand;							//Our random seed

		public int _tickLastMinorPoll;					//The time at which we performed a last minor poll update
        public int _tickLastDatabaseSync;               //The time at which we last synced all our players with the database

		public Dictionary<int, TickerInfo> _tickers;	//The tickers!
		public bool _bGameRunning;						//Is the game running?
        public bool recycling = false;                  //Are we recycling an active zone?
        public int _tickGameStarted;					//The tick at which our game started
		public int _tickGameEnded;						//The tick at which our game ended
        public bool _bLocked;
        public bool _specQuiet;                         //Arena spec chat only on/off
        public bool _watchMod;                          //Viewing of mod commands on/off
        public bool _isMatch;                           //For leagues
        public bool _saveStats = true;
        public BreakdownSettings _breakdownSettings;
        private int _bountyTick;                        //Last time AutoBounty ticked
        public List<ItemDrop> _condemnedItems;

        public PollSettings _poll;						//For our poll command

		public int _levelWidth;
		public int _levelHeight;
		public LvlInfo.Tile[] _tiles;					//The terrain tiles in the arena, can be updated to reflect switches, etc
        public LvlInfo.Tile[] _originaltiles;           //The original tiles in the arena to revert to when switched

		public Commands.Registrar _commandRegistrar;	//Our chat/mod command registrar

		private List<DelayedAction> _delayedActionList;	//The delayed actions waiting to be executed

		public BlockingCollection<Action<Arena>> _events;//Arena events waiting to be processed

		//Events
		public event Action<Arena> Close;				//Called when an arena runs out of players and is closed

		//Settings
		static public int maxVehicles;					//The maximum amount of vehicles we can have active
		static public int maxItems;						//The maximum amount of items we can have laying about
		
		static public int gameCheckInterval;			//The frequency at which we check basic game state

		static public int routeRange;					//The range at which all update packets are routed
		static public int routeWeaponRange;				//The range at which all weapon update packets are routed
		static public int routeRadarRange;				//The range at which packets are routed, but slower
		static public int routeRadarRangeFactor;		//The factor at which the packets are routed
		static public int routeRadarRangeFar;			//The range at which packets are routed, but even slower
		static public int routeRadarRangeFarFactor;		//The factor at which the far packets are routed

        public delegate void LogMessage(string message);

		#region EventObject
		/// <summary>
		/// The event logger, if exists, for this class
		/// </summary>
		public EventHandlers events
		{
			get;
			set;
		}

		#region ThreadedObject
		/// <summary>
		/// The event logger, if exists, for this class
		/// </summary>
		public LogClient _eventLogger
		{
			get;
			set;
		}

		/// <summary>
		/// The sync object for this class
		/// </summary>
		public object _sync
		{
			get;
			set;
		}
		#endregion

		/// <summary>
		/// Initializes events for the event object
		/// </summary>
		public void eventInit(bool bParseEvents)
		{
			EventObjects.eventInit(this, bParseEvents);
		}

		/// <summary>
		/// Triggers an event
		/// </summary>
		public void trigger(string name, params object[] args)
		{
			EventObjects.trigger(this, name, true, args);
		}

		/// <summary>
		/// Calls a singlecast event, returning a value
		/// </summary>
		public object call(string name, params object[] args)
		{
			return EventObjects.callsync(this, name, true, args);
		}

		/// <summary>
		/// Calls a singlecast event, returning a value
		/// </summary>
		public object callsync(string name, bool bSync, params object[] args)
		{
			return EventObjects.callsync(this, name, bSync, args);
		}

		/// <summary>
		/// Determines if a event type exists
		/// </summary>
		public bool exists(string name)
		{	//Does the event exist?
			HandlerList list;
			return events.TryGetValue(name, out list);
		}

		/// <summary>
		/// Flushes the handlerlist - removing all handlers
		/// </summary>
		public void flushEvents()
		{	//Kill all the handlers!
			using (DdMonitor.Lock(_sync))
				events.Clear();
		}
		#endregion

		#region Accessors
		///////////////////////////////////////////////////
		// Accessors
		///////////////////////////////////////////////////
		/// <summary>
		/// Returns the amount of players that are actually ingame
		/// </summary>
		public int PlayerCount
		{
			get
			{
				return _playersIngame.Count;
			}
		}

		/// <summary>
		/// Returns a list of the active players in the arena
		/// </summary>
		/// 
		public IEnumerable<Player> PlayersIngame
		{
			get
			{
				return _playersIngame;
			}
		}

        public IEnumerable<Player> PublicPlayersInGame
        {
            get
            {
                List<Player> pubPlayers = new List<Player>();
                foreach (Player plyr in _playersIngame)
                {
                    if (!plyr._team._isPrivate)
                        pubPlayers.Add(plyr);
                }
                return pubPlayers;
            }
        }

		/// <summary>
		/// Returns the total amount of players that are in the arena
		/// </summary>
		public int TotalPlayerCount
		{
			get
			{
				return _players.Count;
			}
		}

		/// <summary>
		/// Returns a list of the players in the arena
		/// </summary>
		/// 
		public IEnumerable<Player> Players
		{
			get
			{
				return _players;
			}
		}

		/// <summary>
		/// Is this arena invisible to normal players?
		/// </summary>
		public bool IsPrivate
		{
			get
			{
				return _name[0] == '#';
			}
		}

		/// <summary>
		/// Gets the tile at the specified location
		/// </summary>
		/// <remarks>The position given should be in map ticks.</remarks>
		public LvlInfo.Tile getTile(int x, int y)
		{
			x /= 16;
			y /= 16;

			return _tiles[(y * _levelWidth) + x];
		}

		/// <summary>
		/// Gets the tile at the specified location
		/// </summary>
		/// <remarks>The position given should be in map ticks.</remarks>
		public CfgInfo.Terrain getTerrain(int x, int y)
		{	//Get the terrain type of the tile
			x /= 16;
			y /= 16;

			LvlInfo.Tile tile = _tiles[(y * _levelWidth) + x];

			//Find the associated terrain type
			return _server._zoneConfig.terrains[_server._assets.Level.TerrainLookup[tile.TerrainLookup]];
		}

        /// <summary>
        /// Gets the terrain ID at a specified location
        /// </summary>
        /// <remarks>The position given should be in map ticks.</remarks>
        public int getTerrainID(int x, int y)
        {
            return _server._assets.Level.TerrainLookup[_tiles[((y/16) * _levelWidth) + (x/16)].TerrainLookup];
        }
		#endregion Accessors

		///////////////////////////////////////////////////
		// Member Classes
		///////////////////////////////////////////////////
		#region Member Classes
		/// <summary>
		/// Our configurable Breakdown Class.
		/// </summary>
		public class BreakdownSettings
		{ 
            //Generics
			public bool bDisplayTeam = true;
			public bool bDisplayIndividual = true;
            //Gametype Specific
            public bool bDisplayFlagMVP = false;
		} 

		/// <summary>
		/// Represents a dropped item
		/// </summary>
		public class ItemDrop
		{
			public ushort id;			//The ID of the item pile
			public ItemInfo item;		//The type of type
			
			public short quantity;		//The amount in the pile
			public short positionX;		//The location of the pile
			public short positionY;		//
			public int relativeID;      //Relative ID of the item or the hide
			public int tickExpire;      //Tick the item expires on
			public int freq = -1;       //Owning frequency

            public Player owner;        //To prevent bounty abuse
		}

		/// <summary>
		/// Represents a delayed action
		/// </summary>
		public class DelayedAction
		{
			public Func<object, bool> action;	//Action to execute
			public object state;				//State to pass to the function

			public int tickExecute;				//When we should execute it
			public int tickDelay;				//The original tick delay before execution
		}

		/// <summary>
		/// Represents the state of a ticker
		/// </summary>
		public class TickerInfo
		{
			public string message;
			public int timer;
			public int idx;
			public byte colour;

			public Action expireCallback;
			public Func<Player, String> customTicker;

			public TickerInfo(string _message, int _timer, int _idx, byte _colour, Action _callback, Func<Player, String> _customTicker)
			{
				message = _message;
				colour = _colour;
				expireCallback = _callback;
				customTicker = _customTicker;
				idx = _idx;

				//The timer has to be relative, so calculate
				timer = Environment.TickCount + (_timer * 10);
			}

			public void onExpire()
			{
				if (expireCallback != null)
					expireCallback();
			}
		}

        /// <summary>
        /// Our configurable PollQuestion Class.
        /// </summary>
        public class PollSettings
        {
            //Generics
            public bool start = false;
            public int yes = 0;
            public int no = 0;

            public Dictionary<String, PlayerAlias> _alias;
            public class PlayerAlias
            {
                public string name;
            }

            public PollResults _results;
            public class PollResults
            {
                public int amount = 0;
            }
        }

        /// <summary>
        /// Our configurable banned arena class.
        /// </summary>
        public class notAllowed
        {
            //Generics
            public string arena_name;
            public string reason;
        }
		#endregion

		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Generic constructor
		/// </summary>
		public Arena(ZoneServer server)
		{	//Initialize the event object
			eventInit(true);

			//Populate variables
			_sync = new object();
			_server = server;
			_pathfinder = _server._pathfinder;

			_rand = new Random();

			_tickers = new Dictionary<int, TickerInfo>();

			_players = new ObjTracker<Player>();
			_playersIngame = new ObjTracker<Player>();

			_teams = new Dictionary<string, Team>();
			_freqTeams = new SortedDictionary<int, Team>();
            _condemnedItems = new List<ItemDrop>();
			_condemnedVehicles = new List<Vehicle>();
			_vehicles = new ObjTracker<Vehicle>();
			_lastVehicleKey = (ushort)5001;						//The vehicle IDs must start at 5001, everything before
																//is assumed to be a player base vehicle.
			_items = new SortedDictionary<ushort, ItemDrop>();
			_lastItemKey = 0;

			_bots = new ObjTracker<InfServer.Bots.Bot>();
			_condemnedBots = new List<Bots.Bot>();

			_delayedActionList = new List<DelayedAction>();

			_events = new BlockingCollection<Action<Arena>>();

            _balls = new List<Ball>();
           
            _owner = new List<string>();

			//Instance our tiles array
			LvlInfo lvl = server._assets.Level;
			_tiles = new LvlInfo.Tile[lvl.Tiles.Length];
            _originaltiles = new LvlInfo.Tile[lvl.Tiles.Length];
			_levelWidth = lvl.Width;
			_levelHeight = lvl.Height;

			Array.Copy(lvl.Tiles, _tiles, lvl.Height * lvl.Width);
            Array.Copy(lvl.Tiles, _originaltiles, lvl.Height * lvl.Width);

			//Initialize our command registrar
			_commandRegistrar = new InfServer.Game.Commands.Registrar();
			_commandRegistrar.register();
		}

		#region State
		/// <summary>
		/// Initializes arena details
		/// </summary>
		public virtual void init()
		{	//Initialize our subsections
			initState();
			initLio();

            //Initialize our breakdown settings
            _breakdownSettings = new BreakdownSettings();            
		}

		/// <summary>
		/// Adds an event to be processed
		/// </summary>
		public void handleEvent(Action<Arena> action)
		{
			_events.Add(action);
		}

		/// <summary>
		/// Allows the arena to keep it's game state up-to-date
		/// </summary>
		public virtual void poll()
		{	//Make sure we're synced
			using (DdMonitor.Lock(_sync))
			{	//Process any waiting events
				for (int i =_events.Count; i > 0; --i)
					_events.Take()(this);

				//Are we due a minor poll update?
				int now = Environment.TickCount;
				bool bMinor = (now - _tickLastMinorPoll) > 1000;
				if (bMinor)
					_tickLastMinorPoll = now;

                bool bDBSync = (now - _tickLastDatabaseSync) > 60000; //Every 60 seconds
                if (bDBSync)
                    _tickLastDatabaseSync = now;

                //Keep our itemdrops in line
                foreach (var itm in _items.Values)
                {
                    if (itm.tickExpire != 0 && now > itm.tickExpire)
                        _condemnedItems.Add(itm);
                }

                //Remove expired items
                foreach (ItemDrop item in _condemnedItems)
                {
                    //Update the players of the status..
                    Helpers.Object_ItemDropUpdate(Players, item.id, 0);
                    _items.Remove(item.id);
                }
                _condemnedItems.Clear();

                CfgInfo.Terrain t;
                bool tickTerrainBty = now - _bountyTick > 30000; //run every 30 seconds
                if (tickTerrainBty)
                    _bountyTick = now;              

				foreach (Player player in PlayersIngame.ToList())
				{	//Is he awaiting a respawn?
					if (player._deathTime != 0 && now - player._deathTime > _server._zoneConfig.timing.enterDelay * 10)
					{	//So spawn him!
						player._deathTime = 0;
                        //Reset lastMovement to prevent being specced too early for inactivity
                        player._lastMovement = 0;
						handlePlayerSpawn(player, true);
					}

                    //Check inactivity
                    if ((_server._zoneConfig.arena.inactivityTimeout > 0) &&
                        (now - player._lastMovement) > (_server._zoneConfig.arena.inactivityTimeout * 1000) &&
                        player._lastMovement != 0)
                    {
                        player.spec();
                        player.sendMessage(-1, "You have been sent to spectator mode for being inactive.");
                    }
                     
                    //Check maxTimeAllowed inactivity
                    if (player._arena.getTerrain(player._state.positionX, player._state.positionY).maxTimeAllowed > 0)
                    {
                        int maxTime = (player._arena.getTerrain(player._state.positionX, player._state.positionY).maxTimeAllowed * 1000);
                        
                        //Send message at half of max time
                        if ((now - player._lastMovement) >= ((maxTime / 2) - 10) && (now - player._lastMovement) <= ((maxTime / 2) + 10) && player._lastMovement != 0)
                        {
                            string format = "WARNING! Staying in this location for another {0} seconds will send you to spectator mode.";
                            player.sendMessage(-1, String.Format(format, ((maxTime / 2) / 1000)));
                        }
                        //Send to spectator due to inactivity
                        if ((now - player._lastMovement) > maxTime && player._lastMovement != 0)
                        {
                            player.spec();                            
                            player.sendMessage(-1, "You have been sent to spectator mode due to inactivity.");
                        }
                    }
                    

                    //Update play seconds
					if (bMinor)
						player.PlaySeconds++;

					if (tickTerrainBty)
					{
						t = getTerrain(player._state.positionX, player._state.positionY);
                        
                        if (t.bountyAutoRate < 1 || player._bounty >= t.bountyAutoMax)
							continue;
						// bountyAutoRate: 33 = 33% chance to give a bty.. 400 = give 4 bty
						player.Bounty += (t.bountyAutoRate < 100 ? (_rand.Next(100) < t.bountyAutoRate ? 1 : 0) : (int)Math.Floor((double)t.bountyAutoRate / 100.0));
					}

                    //Check tick diff every 10 secs
                    if ((now - player._state.lastUpdate) > 10000)
                    {
                        Client.ConnectionStats pStats = player._client._stats;

                        //Check their lag
                        //Will spec a player for client to server lag/force lagging/or high ping
                        if (pStats.C2SPacketLoss > 3.00f)
                        {
                            player.spec();
                            player.sendMessage(-1, "You have been sent to spectator mode for high packet loss.");
                        }

                        if (pStats.clientAverageUpdate > 600)
                        {
                            player.spec();
                            player.sendMessage(-1, "You have been sent to spectator mode for high ping.");
                        }
                    }
				}

                //Do we have any players that need to be unsilenced?
                foreach (Player p in _players.ToList())
                {
                    if (!p._bSilenced)
                        continue;

                    //Check the timediff.
                    TimeSpan diff = DateTime.Now - p._timeOfSilence;

                    //Okay, unsilence him.
                    if (diff.Minutes >= p._lengthOfSilence)
                    {
                        p._bSilenced = false;
                        p._lengthOfSilence = 0;
                        p.sendMessage(-1, "You may speak now.");

                        if (p._server._playerSilenced.ContainsKey(p._alias))
                            p._server._playerSilenced.Remove(p._alias);
                    }
                }

				if (bMinor)
				{	//Keep our tickers in line
					foreach (TickerInfo ticker in _tickers.Values.ToList())
					{	//If it's timed out
						if (ticker.timer != -1 && ticker.timer < now)
						{	//Ticker has expired
							ticker.timer = -1;
							ticker.onExpire();
						}
					}
				}

				//Keep car vehicles in line
				foreach (Vehicle vehicle in _vehicles.ToList())
				{	//We don't need to bother maintaining bot vehicles
					if (vehicle.bCondemned)
						_condemnedVehicles.Add(vehicle);

					if (vehicle._bBotVehicle)
						continue;

					//What sort of vehicle is it?
					switch (vehicle._type.Type)
					{
						case VehInfo.Types.Car:
							{	//Get our information
								VehInfo.Car carInfo = vehicle._type as VehInfo.Car;
								if (carInfo == null)
									continue;

								//Check for expiration timers
								if (carInfo.RemoveGlobalTimer != 0 && vehicle._tickCreation != 0 &&
									(now - vehicle._tickCreation) > (carInfo.RemoveGlobalTimer * 1000))
									vehicle.destroy(true);
								else if (carInfo.RemoveDeadTimer != 0 && vehicle._tickDead != 0 &&
									(now - vehicle._tickDead) > (carInfo.RemoveDeadTimer * 1000))
									vehicle.destroy(true);
								else if (carInfo.RemoveUnoccupiedTimer != 0 && vehicle._tickUnoccupied != 0 &&
									(now - vehicle._tickUnoccupied) > (carInfo.RemoveUnoccupiedTimer * 1000))
									vehicle.destroy(true);
							}
							break;

                        case VehInfo.Types.Computer:
                            {
                                //Check our control timer and update if necessary
                                if (vehicle._tickControlEnd != 0 && vehicle._tickControlTime != 0 &&
                                    (now - vehicle._tickControlTime) > (vehicle._tickControlEnd * 1000))
                                {
                                    //Reset till the next ownership change
                                    if (vehicle._team.ActivePlayerCount > 0)
                                        vehicle._owner = vehicle._team;
                                    else
                                        vehicle._owner = null;
                                    vehicle._tickControlEnd = 0;
                                    vehicle._tickControlTime = 0;
                                }
                            }
                            break;
					}
				}

				foreach (Vehicle vehicle in _condemnedVehicles)
					_vehicles.Remove(vehicle);
				_condemnedVehicles.Clear();

				//Take care of our delayed actions
				List<DelayedAction> executedActions = null;

				foreach (DelayedAction delayed in _delayedActionList)
				{	//Is it due to be executed?
					if (now < delayed.tickExecute)
						continue;

					//Queue it for execution
					if (executedActions == null)
						executedActions = new List<DelayedAction>();
					executedActions.Add(delayed);
				}

				//We have to execute them outside of the actionlist loop to make sure
				//it won't be modified due to removing or adding new actions.
				if (executedActions != null)
				{
					foreach (DelayedAction delayed in executedActions)
					{
						if (!delayed.action(delayed.state))
							_delayedActionList.Remove(delayed);
						else
							delayed.tickExecute = now + delayed.tickDelay;
					}
				}

                //Is it time to sync to database?
                if (bDBSync && !_server.IsStandalone && _bIsPublic && _saveStats)
                    foreach (Player p in Players.ToList())
                        if (p._bDBLoaded)
                            //Update him!
                            p._server._db.updatePlayer(p);

				//Look after our lio objects
				pollLio();

				// Aim and fire turrets!
				pollComputers();

				//Handle the bots!
				pollBots();
			}
		}

		/// <summary>
		/// Cleans up the arena and removes it from the zone server list
		/// </summary>
		public virtual void close()
		{	//Call our close event
			if (Close != null)
				Close(this);
		}
		#endregion

		#region Players
		public List<Player> getPlayersAndSpecInRange(int posX, int posY, int range)
		{
			return _players.getObjsInRange(posX, posY, range);
		}

		public List<Player> getPlayersInRange(int posX, int posY, int range)
		{
			return _playersIngame.getObjsInRange(posX, posY, range);
		}

		public List<Player> getPlayersInBox(int posX, int posY, int width, int height)
		{	
			//Extrapolate
			width /= 2;
			height /= 2;
			return getPlayersInArea(posX - width, posY - height, posX + width, posY + height);
		}

		public List<Player> getPlayersInArea(int topX, int topY, int bottomX, int bottomY)
		{
			return _playersIngame.getObjsInArea(topX, topY, bottomX, bottomY);
		}

		public int getPlayerCountInArea(int topX, int topY, int bottomX, int bottomY)
		{
			return _playersIngame.getObjcountInArea(topX, topY, bottomX, bottomY);
		}

		#endregion

		#region Accessors
		/// <summary>
		/// Obtains a team by name
		/// </summary>
		public Team getTeamByName(string name)
		{	//Attempt to find it
			Team team;

			if (_teams.TryGetValue(name.ToLower(), out team))
                //Check _teams
				return team;
            foreach (Team arenaTeam in Teams)
                //Check Teams
                if (arenaTeam._name.ToLower() == name.ToLower())
                    return arenaTeam;
            //Still nothing? team doesn't exist
			return null;
		}

        public Team getTeamByID(int id)
        {
            Team team;

            if (_freqTeams.TryGetValue(id, out team))
                return team;
            foreach (Team arenaTeam in Teams)
                if (arenaTeam._id == id)
                    return arenaTeam;
            return null;
        }

        public void createTeam(Team team)
        {
            _teams.Add(team._name.ToLower(), team);
        }

		/// <summary>
		/// Gets a player of the specified name
		/// </summary>
		public Player getPlayerByName(string name)
		{	//Attempt to find him
			foreach (Player player in _players)
				if (player._alias.Equals(name, StringComparison.OrdinalIgnoreCase))
					return player;

			return null;
		}

        /// <summary>
        /// Gets a player of the specified id
        /// </summary>
        public Player getPlayerById(uint id)
        {	//Attempt to find him
            foreach (Player player in _players)
                if (player._id == id)
                    return player;

            return null;
        }

		/// <summary>
		/// Determines whether the player should be able to see this arena
		/// </summary>
		public bool isVisibleToPlayer(Player player)
		{	//If we're private..
			if (IsPrivate)
			{	//Does the player have enough permission?
				return (player.PermissionLevel >= Data.PlayerPermission.Mod);
			}

			return true;
		}

        /// <summary>
        /// Determines whether the player is the owner of this arena
        /// </summary>
        public bool IsOwner(Player player)
        {
            if (_owner != null && _owner.Count > 0)
            {
                foreach (var p in _owner)
                    if (player._alias.Equals(p))
                        return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether the player is granted in this arena
        /// </summary>
        public bool IsGranted(Player player)
        {
            if (IsPrivate)
                if (IsOwner(player) || player.PermissionLevel >= Data.PlayerPermission.ArenaMod)
                    return true;
            return false;
        }

		/// <summary>
		/// Gets the tile at the specified location
		/// </summary>
		/// <remarks>The position given should be in map ticks.</remarks>
		public bool getUnblockedTileInRadius(ref short x, ref short y, int innerRadius, int outerRadius)
		{
			return getUnblockedTileInRadius(ref x, ref y, innerRadius, outerRadius, 0);
		}

		/// <summary>
		/// Gets the tile at the specified location
		/// </summary>
		/// <remarks>The position given should be in map ticks.</remarks>
		public bool getUnblockedTileInRadius(ref short x, ref short y, int innerRadius, int outerRadius, int unblockedRadius)
		{	//Create a list of legible tiles
			List<int> legible = new List<int>();

			//Turn all coordinates into tile coordinates
			x /= 16;
			y /= 16;

			innerRadius /= 16;
			outerRadius /= 16;
			unblockedRadius /= 16;
			innerRadius++;
			outerRadius++;
			unblockedRadius++;

			int yStep1 = Math.Max(0, y - outerRadius);
			int yStep2 = Math.Max(0, y - innerRadius);
			int yStep3 = Math.Min(_levelHeight, y + innerRadius);
			int yStep4 = Math.Min(_levelHeight, y + outerRadius);
			int xStep1 = Math.Max(0, x - outerRadius);
			int xStep2 = Math.Max(0, x - innerRadius);
			int xStep3 = Math.Min(_levelWidth, x + innerRadius);
			int xStep4 = Math.Min(_levelWidth, x + outerRadius);

			//Top section
			for (int j = yStep1; j < yStep2; ++j)
			{
				for (int k = xStep1; k < xStep4; ++k)
					if (!_tiles[(j * _levelWidth) + k].Blocked)
						legible.Add((j * _levelWidth) + k);
			}

			//Mid section
			for (int j = yStep2; j < yStep3; ++j)
			{
				for (int k = xStep1; k < xStep2; ++k)
				{	//Not blocked?
					if (!_tiles[(j * _levelWidth) + k].Blocked)
						legible.Add((j * _levelWidth) + k);
				}
				for (int k = xStep3; k < xStep4; ++k)
				{	//Not blocked?
					if (!_tiles[(j * _levelWidth) + k].Blocked)
						legible.Add((j * _levelWidth) + k);
				}
			}

			//Bottom section
			for (int j = yStep3; j < yStep4; ++j)
			{
				for (int k = xStep1; k < xStep4; ++k)
					if (!_tiles[(j * _levelWidth) + k].Blocked)
						legible.Add((j * _levelWidth) + k);
			}

			if (legible.Count == 0)
				return false;

			//Should we perform a second pass to find tiles which have the appropriate amount of enclosing space?
			if (unblockedRadius > 0)
			{
				List<int> secondPass = new List<int>();

				foreach (int idx in legible)
				{	//Check around the point
					int j1 = Math.Max(0, (idx % _levelWidth) - unblockedRadius);
					int j2 = Math.Min(_levelWidth, (idx % _levelWidth) + unblockedRadius);
					int k = Math.Max(0, (idx / _levelWidth) - unblockedRadius);
					int k2 = Math.Min(_levelHeight, (idx / _levelWidth) + unblockedRadius);
					bool bBlocked = false;

					for (; k < k2 && !bBlocked; ++k)
						for (int j = j1; j < j2; ++j)
						{
							if (_tiles[(k * _levelWidth) + j].Blocked)
							{
								bBlocked = true;
								break;
							}
						}

					if (!bBlocked)
						secondPass.Add(idx);
				}

				if (secondPass.Count == 0)
					return false;

				legible = secondPass;
			}

			//Choose a random location from the list
			int chosen = legible[_rand.Next(legible.Count)];

			x = (short)((chosen % _levelWidth) * 16);
			y = (short)((chosen / _levelWidth) * 16);

			return true;
		}

		/// <summary>
		/// Returns the amount of items total in the specified area
		/// </summary>
		public int getItemCountInRange(ItemInfo item, short x, short y, int range)
		{
			int objArea = 0;

			foreach (ItemDrop drop in _items.Values)
			{	//Is it of the same time?
				if (item != null && drop.item != item)
					continue;

				//In the given area?
				if (x + range < drop.positionX)
					continue;
				if (x - range > drop.positionX)
					continue;
				if (y + range < drop.positionY)
					continue;
				if (y - range > drop.positionY)
					continue;

				objArea += drop.quantity;
			}

			return objArea;
		}

		/// <summary>
		/// Returns the amount of item drops total in the specified area
		/// </summary>
		public int getItemDropCountInArea(ItemInfo item, ref short x, ref short y, int range)
		{
			int objArea = 0;

			foreach (ItemDrop drop in _items.Values)
			{	//Is it of the same time?
				if (item != null && drop.item != item)
					continue;

				//In the given area?
				if (x + range < drop.positionX)
					continue;
				if (x - range > drop.positionX)
					continue;
				if (y + range < drop.positionY)
					continue;
				if (y - range > drop.positionY)
					continue;

				objArea++;
			}

			return objArea;
		}
		#endregion

        /// <summary>
        /// Returns a specific item in the specified area
        /// </summary>
        public ItemDrop getItemInRange(ItemInfo item, short x, short y, int range)
        {
            ItemDrop returnDrop = null;

            int bestX = -1;
            int bestY = -1;
            
            foreach (ItemDrop drop in _items.Values)
            {	//Is it the same item?
                if (item != null && drop.item != item)
                    continue;
                
                //In the given area?
                if ((x + range) > drop.positionX)
                {
                    if ((x - range) < drop.positionX)
                    {
                        if ((y + range) > drop.positionY)
                        {
                            if ((y - range) < drop.positionY)
                            {                                
                                //No other items found in range
                                if (bestX == -1 && bestY == -1)
                                {
                                    bestX = drop.positionX;
                                    bestY = drop.positionY;
                                    returnDrop = drop;                                    
                                }
                                //Compare against other items in range to see which is closer to the location
                                if ((Math.Abs(bestX - x) + Math.Abs(bestY - y)) > (Math.Abs(drop.positionX - x) + Math.Abs(drop.positionY - y)))
                                {
                                    bestX = drop.positionX;
                                    bestY = drop.positionY;
                                    returnDrop = drop;                                   
                                }
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    continue;
                }              
            }

            return returnDrop;
        }

		#region Delayed Actions
		/// <summary>
		/// Registers a delayed action to be executed at a later date
		/// </summary>
		/// <remarks>The action function given should return false to never execute again,
		/// or true to execute again after the next [millisecondDelay] milliseconds</remarks>
		/// <param name="millisecondDelay">The delay in milliseconds before the action function is called</param>
		/// <param name="action">The function to call after the delay elapses</param>
		/// <param name="state">The argument to pass to the action function when called</param>
		public void addDelayedAction(int millisecondDelay, Func<object, bool> action, object state)
		{	//Create our delayed action structure
			DelayedAction delayed = new DelayedAction();
			
			delayed.action = action;
			delayed.state = state;

			delayed.tickExecute = Environment.TickCount + millisecondDelay;
			delayed.tickDelay = millisecondDelay;

			using (DdMonitor.Lock(_sync))
				_delayedActionList.Add(delayed);
		}
		#endregion

		#region Events
		/// <summary>
		/// Called when a player enters the game
		/// </summary>
		public virtual void playerEnter(Player player)
		{	//The player has joined the game! Add him
			_playersIngame.Add(player);

			//Stop spectating
			if (player._spectating != null)
			{
				player._spectating._spectators.Remove(player);
				player._spectating = null;
			}
		}

		/// <summary>
		/// Called when a player leaves the game
		/// </summary>
		public virtual void playerLeave(Player player)
		{	//He's left, remove him
			_playersIngame.Remove(player);
		}

        /// <summary>
        /// Called when a person starts a poll question
        /// </summary>
        public virtual void pollQuestion(Arena arena, bool start)
        {
        }

		/// <summary>
		/// Called when the game begins
		/// </summary>
		public virtual void gameStart() 
		{
		}

		/// <summary>
		/// Called when the game ends
		/// </summary>
		public virtual void gameEnd()
		{
		}

		/// <summary>
		/// Creates a breakdown tailored for one player
		/// </summary>
		public virtual void individualBreakdown(Player from, bool bCurrent)
		{
		}

        /// <summary>
		/// Called when the game needs to display end game statistics
		/// </summary>
        public virtual void breakdown(bool bCurrent)
        {
        }

        public virtual void handlePlayerChatCommand(Player player, Player recipient, string command, string payload)
        {
        }

		/// <summary>
		/// Determines which team is appropriate for the player to be playing on
		/// </summary>
		public virtual Team pickAppropriateTeam(Player player)
		{	//Find an appropriate team for the player to join
			List<Team> publicTeams = _teams.Values.Where(team => team.IsPublic).ToList();
			Team pick = null;

			if (_server._zoneConfig.arena.forceEvenTeams)
			{	//We just want one for each team
				int playerCount = 0;

				for (int i = 0; i < _server._zoneConfig.arena.desiredFrequencies; ++i)
				{	//Do we have more active players than the last?
					Team team = publicTeams[i];
					int activePlayers = team.ActivePlayerCount;
					int maxPlayers = team._info.maxPlayers;

					if ((pick == null && maxPlayers != -1) ||
						(playerCount > activePlayers &&
							(maxPlayers == 0 || playerCount <= maxPlayers)))
					{
						pick = team;
						playerCount = activePlayers;
					}
				}

				if (pick == null)
					return null;
			}
			else
			{	//Spread them out until we hit our desired number of frequencies
				int playerCount = int.MaxValue;
				int desiredFreqs = _server._zoneConfig.arena.desiredFrequencies;
				int idx = 0;

				while (desiredFreqs > 0 && publicTeams.Count > idx)
				{	//Valid team?
					Team team = publicTeams[idx++];
					int maxPlayers = team._info.maxPlayers;

					if (maxPlayers == -1)
						continue;

					//Do we have less active players than the last?
					int activePlayers = team.ActivePlayerCount;

					if (activePlayers < playerCount &&
						(maxPlayers == 0 || activePlayers + 1 <= maxPlayers))
					{
						pick = team;
						playerCount = activePlayers;

						if (activePlayers == 0)
							break;
					}

					if (activePlayers > 0)
						desiredFreqs--;
				}

				if (pick == null)
				{	//Desired frequencies are all full, go to our extra teams!
					playerCount = -1;
					desiredFreqs = _server._zoneConfig.arena.frequencyMax;
					idx = 0;

					while (desiredFreqs > 0 && publicTeams.Count > idx)
					{	//Valid team?
						Team team = publicTeams[idx++];
						int maxPlayers = team._info.maxPlayers;

						if (maxPlayers == -1)
							continue;

						//Do we have more active players than the last?
						int activePlayers = team.ActivePlayerCount;

						if (activePlayers > playerCount &&
							(maxPlayers == 0 || activePlayers + 1 <= maxPlayers))
						{
							pick = team;
							playerCount = activePlayers;
						}

						if (activePlayers > 0)
							desiredFreqs--;
					}
				}
			}

			return pick;
		}

		/// <summary>
		/// Called to reset the game state
		/// </summary>
		public virtual void gameReset()
		{	}

        #endregion
	}
}