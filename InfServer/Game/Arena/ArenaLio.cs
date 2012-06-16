using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using InfServer.Network;
using InfServer.Protocol;
using InfServer.Logic;

using Assets;

namespace InfServer.Game
{
	// Arena Class
	/// Represents a single arena in the server
	///////////////////////////////////////////////////////
	public partial class Arena
	{	// Member variables
		///////////////////////////////////////////////////
		public Dictionary<int, SwitchState> _switches;
		public Dictionary<int, FlagState> _flags;
        public int _lastFlagReward;
		public List<HideState> _hides;
        public TurretGroupMan _turretGroups;

		///////////////////////////////////////////////////
		// Member Classes
		///////////////////////////////////////////////////
		#region Member Classes
		/// <summary>
		/// Represents the state of a hide object
		/// </summary>
		public class HideState
		{
			public LioInfo.Hide Hide;				//Our hide information

			public int _tickLastAttempt;			//The time we last attempted to hide
			public int _tickLastSuccessAttempt;		//The last time we successfully attempted to hide (0 == N/A)
		}

		/// <summary>
		/// Represents the state of a lio switch item
		/// </summary>
		public class SwitchState
		{
			public LioInfo.Switch Switch;	//Our switch information

			public bool bOpen;				//Is the switch open?
			public int lastOperation;		//The time at which the switch was last triggered
			public int closeDelay;			//Autoclose delay
		}

		/// <summary>
		/// Represents the state of a lio flag
		/// </summary>
		public class FlagState
		{
			public bool bActive;			//Is the flag currently in use?

			public LioInfo.Flag flag;		//Our flag definition

			private Team _team;				//The team the flag currently belongs to
			public Team oldTeam;			//The team the flag belonged to before pickup
			public Player carrier;			//The carrier, if any

			public short posX;				//Our position
			public short posY;

			public int lastOperation;		//The time at which the flag was last triggered

			//We want to inform when a flag changes team
			public event Action<FlagState> TeamChange;

			public Team team
			{
				get
				{
					return _team;
				}

				set
				{	//As long as it's not the same team..
					if (_team == value)
						return;

					_team = value;
					if (TeamChange != null)
						TeamChange(this);
				}
			}
		}

        /// <summary>
        /// Represents a relative object others can spawn from
        /// </summary>
        public class RelativeObj
        {
            public short posX;              //
            public short posY;              //
            public int freq;                //Owning team
            public LioInfo.WarpField warp;  //Associated warp
            public RelativeObj(short _posX, short _posY, int _freq)
            {
                posX = _posX;
                posY = _posY;
                freq = _freq;
            }
        }

        public class TurretGroupMan
        {
            public Dictionary <int, List<Tuple<LioInfo.Hide, Computer>>> turrets;
            public TurretGroupMan()
            {
                turrets = new Dictionary<int, List<Tuple<LioInfo.Hide, Computer>>> { };
            }
            
            //Add a vehicle when it spawns
            public void Add(LioInfo.Hide hide, Computer veh)
            {
                if (!turrets.ContainsKey(hide.HideData.HideTurretGroup))
                    turrets[hide.HideData.HideTurretGroup] = new List<Tuple<LioInfo.Hide, Computer>> { };
                turrets[hide.HideData.HideTurretGroup].Add(Tuple.Create(hide, veh));
            }

            //Switch all turrets in a group
            public bool Switch(int turretGroup, Player player, bool bOpen)
            {
                if (!turrets.ContainsKey(turretGroup))
                    return false;

                bool anySwitched = false;
                foreach (var turret in turrets[turretGroup])
                {
                    if (bOpen && turret.Item1.HideData.TurretInverseState == 0 )
                    {   //Normal acting turret
                        if (turret.Item1.HideData.TurretSwitchedFrequency == -2)
                        {   //Set to player's freq
                            Team newTeam = player._team;
                            turret.Item2._team = newTeam;
                            anySwitched = true;
                        }
                        else
                        {   //freq -1 .. 9999
                            Team newTeam = player._arena.getTeamByID(turret.Item1.HideData.TurretSwitchedFrequency);
                            if (newTeam != null)
                            {
                                turret.Item2._team = newTeam;
                                anySwitched = true;
                            }
                        }
                    }
                    else if (!bOpen || (turret.Item1.HideData.TurretInverseState == 1 && bOpen))
                    {   //The turret should do the opposite, switch is flipped and the turret goes off
                        Team newTeam = player._arena.getTeamByID(turret.Item1.HideData.AssignFrequency);
                        if (newTeam != null)
                        {
                            turret.Item2._team = newTeam;
                            anySwitched = true;
                        }
                    }
                }
                return anySwitched;
            }

            public void Switch(int[] turretGroups, Player player, bool bOpen)
            {
                foreach (int tg in turretGroups)
                {
                    Switch(tg, player, bOpen);
                }
            }

            //Remove a turret because it died
            public void Remove(Computer veh)
            {
                Tuple<LioInfo.Hide, Computer> res = null;
                int goodGravy = 0;
                foreach (var tg in turrets)
                {
                    res = tg.Value.Find(t => t.Item2 == veh);
                    if (res != null)
                    {
                        goodGravy = tg.Key;
                        break;
                    }
                }
                if (goodGravy != 0)
                    turrets[goodGravy].Remove(res);
            }
        }
		#endregion


		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Initializes arena details
		/// </summary>
		private void initLio()
		{	//Initialize our list of switch states
			IEnumerable<LioInfo.Switch> switches = _server._assets.Lios.Switches;
			_switches = new Dictionary<int, SwitchState>();

			foreach (LioInfo.Switch swi in switches)
			{	//Create a switch state for this switch
				SwitchState ss = new SwitchState();

				ss.bOpen = false;
				ss.lastOperation = 0;
				ss.closeDelay = swi.SwitchData.AutoCloseDelay * 10;

				ss.Switch = swi;

				_switches[swi.GeneralData.Id] = ss;
			}

			//Initialize our list of flag states
			IEnumerable<LioInfo.Flag> flags = _server._assets.Lios.Flags;
			_flags = new Dictionary<int, FlagState>();

			foreach (LioInfo.Flag flag in flags)
			{	//Create a state for this flag
				FlagState fs = new FlagState();

				fs.flag = flag;

				_flags[flag.GeneralData.Id] = fs;
			}

			//Initialize our list of hide states
			_hides = new List<HideState>();
            _turretGroups = new TurretGroupMan();
			foreach (LioInfo.Hide hide in _server._assets.Lios.Hides)
			{	//Create our state
				HideState hs = new HideState();

				hs.Hide = hide;

				_hides.Add(hs);
			}
		}

		/// <summary>
		/// Keeps the lio state up-to-date
		/// </summary>
		private void pollLio()
		{	//Look after our switches
			int tickUpdate = Environment.TickCount;
            int flagDelay = _server._zoneConfig.flag.periodicRewardDelay * 1000;

            if (flagDelay != 0 && (tickUpdate - _lastFlagReward) > flagDelay)
            {

                foreach (Team rewardees in ActiveTeams)
                {
                    int cashReward = 0;
                    int expReward = 0;
                    int pointReward = 0;

                    foreach (FlagState fs in _flags.Values)
                    {
                       
                        if (fs.team != rewardees)
                            continue;

                        LioInfo.Flag.FlagSettings flagSettings = fs.flag.FlagData;
                        //Periodic reward?
                        if (flagSettings.PeriodicCashReward == 0 && flagSettings.PeriodicExperienceReward == 0 && flagSettings.PeriodicPointsReward == 00)
                            continue;

                        //Cash
                        if (flagSettings.PeriodicCashReward < 0)
                            cashReward += (Math.Abs(flagSettings.PeriodicCashReward) * _playersIngame.Count()) / 1000;
                        else
                            cashReward += Math.Abs(flagSettings.PeriodicCashReward);
                        //Experience
                        if (flagSettings.PeriodicExperienceReward < 0)
                            expReward += (Math.Abs(flagSettings.PeriodicExperienceReward) * _playersIngame.Count()) / 1000;
                        else
                            expReward += Math.Abs(flagSettings.PeriodicExperienceReward);
                        //Points
                        if (flagSettings.PeriodicPointsReward < 0)
                            pointReward += (Math.Abs(flagSettings.PeriodicPointsReward) * _playersIngame.Count()) / 1000;
                        else
                            pointReward += Math.Abs(flagSettings.PeriodicPointsReward);

                    }

                    if (cashReward == 0 && expReward == 0 && pointReward == 0)
                        continue;

                    //Format the message
                    string format = String.Format
                        ("&Reward (Cash={0} Experience={1} Points={2}) Next reward in {3} seconds.",
                        cashReward, expReward, pointReward, _server._zoneConfig.flag.periodicRewardDelay);

                    //Send it.
                    rewardees.sendArenaMessage(format, _server._zoneConfig.flag.periodicBong);

                    //Reward each player on the rewardees team
                    foreach (Player player in rewardees.ActivePlayers)
                    {
                        player.Cash += cashReward;
                        player.Experience += expReward;
                        player.syncState();
                    }
                }

                _lastFlagReward = Environment.TickCount;
            }

			foreach (SwitchState ss in _switches.Values)
			{	//Does it require autoclosing?
				if (ss.bOpen && ss.closeDelay != 0 &&
					(tickUpdate - ss.lastOperation > ss.closeDelay))
				{	//Set and update!
					ss.bOpen = false;
					Helpers.Object_LIOs(Players, ss);
                    if (ss.Switch.SwitchData.Switch == 0)
                        //Update map tiles
                        updateDoors();
                    else
                        _turretGroups.Switch(ss.Switch.SwitchData.SwitchLioId, null, false);
				}
			}

			//Look after our hides if the game is running
			if (_bGameRunning || !_server._zoneConfig.startGame.initialHides)
			{
				foreach (HideState hs in _hides)
				{	//Time to reattempt?
					if (hs._tickLastSuccessAttempt != 0 && tickUpdate - hs._tickLastSuccessAttempt > (hs.Hide.HideData.SucceedDelay * 100))
						//Yes! Do it
						hideSpawn(hs, 1, false);
					else if (tickUpdate - hs._tickLastAttempt > (hs.Hide.HideData.AttemptDelay * 100))
						hideSpawn(hs, 1, false);
				}
			}
		}

        /// <summary>
        /// Updates the map tiles based on doors. TODO: Include linked doors, will only updated doors specified by Switch
        /// </summary>
        private void updateDoors()
        {
            foreach (SwitchState ss in _switches.Values)
            {
                if (ss.Switch.SwitchData.Switch == 1)
                    //This is a turret switch, not a door.
                    continue;

                foreach (int doorid in ss.Switch.SwitchData.SwitchLioId)
                {   //Update map level info with whether or not door is open or closed
                    if (doorid == 0) //Is a door specified?
                        continue;

                    //The door that's being switched
                    LioInfo.Door door = _server._assets.Lios.Doors.FirstOrDefault(d => d.GeneralData.Id == doorid);

                    //???
                    if (door == null)
                        continue;

                    bool isBlocked = !ss.bOpen; //Is the door closed?

                    if (door.DoorData.InverseState == 1)//Is the state inversed?
                        isBlocked = !isBlocked;

                    for (int y = 0; y < door.DoorData.PhysicsHeight; y++)
                    {
                        int posy = (door.GeneralData.OffsetY / 16) - _server._assets.Level.OffsetY + door.DoorData.RelativePhysicsTileY + y;
                        for (int x = 0; x < door.DoorData.PhysicsWidth; x++)
                        {
                            int posx = (door.GeneralData.OffsetX / 16) - _server._assets.Level.OffsetX + door.DoorData.RelativePhysicsTileX + x;
                            int t = posy * _levelWidth + posx;
                            //are we removing tiles or adding tiles?
                            if (isBlocked)
                                //Recreate the tile
                                _tiles[t].PhysicsVision = _originaltiles[t].PhysicsVision;
                            else
                                //Clear the tile
                                _tiles[t].PhysicsVision = (byte)0x00;
                        }
                    }
                }
            }
        }

		/// <summary>
		/// Performs all the initial hide spawns
		/// </summary>
		public void initialHideSpawns()
		{	//For each hide..
			foreach (HideState hs in _hides)
			{	//Spawn it the requested amount of times
				if (hs.Hide.HideData.InitialCount != 0)
					hideSpawn(hs, hs.Hide.HideData.InitialCount, true);
			}
		}

        //I need to build a list of relative IDs
        //This list reflects computers, flags, and the active vehicle of players (and bots by extension)
        //and items laying around in the arena
        public List<RelativeObj> findRelativeID(int huntFreq, int relID, Player requestingPlayer)
        {
            List<RelativeObj> possibilities = new List<RelativeObj> { };
            try
            {
                possibilities.AddRange(from v in Vehicles
                                       where (v.relativeID == relID &&
                                               (v._type.Type == VehInfo.Types.Computer)
                                           || (v._type.Type == VehInfo.Types.Car && v._inhabitant != null && v._inhabitant.ActiveVehicle == v)
                                           || (v._type.Type == VehInfo.Types.Dependent && v._inhabitant != null)) //this can probably be taken out
                                       select new RelativeObj(v._state.positionX, v._state.positionY, v._team._id));
                possibilities.AddRange(from f in _flags.Values
                                       where f.bActive && f.flag.FlagData.FlagRelativeId == relID
                                       select new RelativeObj(f.posX, f.posY, f.team._id));
                possibilities.AddRange(from it in _items.Values
                                       where it.relativeID == relID
                                       select new RelativeObj(it.positionX, it.positionY, it.freq));
            }
            catch (NullReferenceException)
            {
                Log.write(TLog.Warning, "Invalid Lio relative ID (" + relID + ")");
            }
            if (huntFreq >= -1)
            {   //limit to this frequency
                return possibilities.Where(p => p.freq == huntFreq).ToList();
            }
            else if (huntFreq == -2)
            {   //any freq, return all matches
                return possibilities;
            }
            else if (huntFreq == -3)
            {   //requesting player's frequency first then any-unowned
                if (requestingPlayer != null)
                {   //This is a warp request
                    var subp = possibilities.Where(p => p.freq == requestingPlayer._team._id).ToList();
                    if (subp.Count > 0)
                        return subp;
                }
                return possibilities.Where(p => p.freq == -1 || p.freq == 9999).ToList();

            }
            else if (huntFreq == -4 && requestingPlayer != null)
            {   //requesting player's frequency only
                var subp = possibilities.Where(p => p.freq == requestingPlayer._team._id).ToList();
                if (subp.Count > 0)
                    return subp;
            }
            return null;
        }

        /*
         * Changes made to hideSpawn() (notes)
         * moved the actual spawning of items/vehs into separate functions
         * try to build the lists we need only once when hidespawn() is called and pass them around
         * check objlevel before the new function is called and try to optimize the check for objs in area (maybe i can cache it somehow)
         * 
         */
        private bool hideItem(HideState hs, List<RelativeObj> spawnPoints, ItemInfo item, List<ItemDrop> sameItems)
        {
            foreach (RelativeObj sp in spawnPoints)
            {
                if (hs.Hide.HideData.MinPlayerDistance != 0 &&
                    getPlayersInRange(sp.posX, sp.posY, hs.Hide.HideData.MinPlayerDistance).Count > 0)
				    continue;
                if (hs.Hide.HideData.MaxPlayerDistance < Int32.MaxValue &&
				    getPlayersInRange(sp.posX, sp.posY, hs.Hide.HideData.MaxPlayerDistance).Count == 0)
				    continue;

                if (hs.Hide.HideData.MaxTypeInArea != -1)
                {   //Check for the amount of similiar objects
                    int objArea = 0;
                    int w2 = hs.Hide.GeneralData.Width / 2;
                    int h2 = hs.Hide.GeneralData.Height / 2;

                    foreach (ItemDrop drop in sameItems)
                    {	//In the given area?
                        if (sp.posX + w2 < drop.positionX)
                            continue;
                        if (sp.posX - w2 > drop.positionX)
                            continue;
                        if (sp.posY + h2 < drop.positionY)
                            continue;
                        if (sp.posY - h2 > drop.positionY)
                            continue;

                        objArea += drop.quantity;
                    }

                    //Can we spawn another?
                    if (objArea >= hs.Hide.HideData.MaxTypeInArea)
                        continue;
                }

                //Generate some random coordinates
                short pX = 0;
                short pY = 0;
                int attempts = 0;
                int clumpQuantity = (hs.Hide.HideData.ClumpQuantity == 0 ? 1 : hs.Hide.HideData.ClumpQuantity);
                for(; attempts < 10; attempts++)
                {
                    pX = sp.posX;
                    pY = sp.posY;
                    if (hs.Hide.HideData.ClumpRadius > 0)
                        //This is not true clustering, it should be a circle around the point but I don't feel like making a new method for it
                        Helpers.randomPositionInArea(this, ref pX, ref pY,
                            (short)hs.Hide.HideData.ClumpRadius, (short)hs.Hide.HideData.ClumpRadius);
                    else
                        Helpers.randomPositionInArea(this, ref pX, ref pY,
                            (short)hs.Hide.GeneralData.Width, (short)hs.Hide.GeneralData.Height);

                    //Is it blocked?
                    if (getTile(pX, pY).Blocked)
                    	//Try again
                        continue;

                    itemSpawn(item, (ushort)hs.Hide.HideData.HideQuantity, pX, pY, hs.Hide.HideData.RelativeId, hs.Hide.HideData.AssignFrequency);

                    if (clumpQuantity > 1)
                    {   //Have more to spawn, so make sure it runs again
                        clumpQuantity--;
                        attempts -= 5;
                        continue;
                    }
                    

                    break;
                }
                if (attempts == 10)
                    continue;

                return true;
            }
            return false;
        }

        private bool hideVehicle(HideState hs, List<RelativeObj> spawnPoints, VehInfo vehicle, List<Vehicle> sameVehicles, Team team)
        {
            foreach (RelativeObj sp in spawnPoints)
            {   
                if (hs.Hide.HideData.MinPlayerDistance != 0 &&
                    getPlayersInRange(sp.posX, sp.posY, hs.Hide.HideData.MinPlayerDistance).Count > 0)
				    continue;
                if (hs.Hide.HideData.MaxPlayerDistance < Int32.MaxValue &&
				    getPlayersInRange(sp.posX, sp.posY, hs.Hide.HideData.MaxPlayerDistance).Count == 0)
				    continue;

                if (hs.Hide.HideData.MaxTypeInArea != -1)
                {   //Check for the amount of similiar vehicles
                    int objArea = 0;
                    int w2 = hs.Hide.GeneralData.Width / 2;
                    int h2 = hs.Hide.GeneralData.Height / 2;

                    foreach (Vehicle veh in sameVehicles)
                    {	//In the given area?
                        if (sp.posX + w2 < veh._state.positionX)
                            continue;
                        if (sp.posX - w2 > veh._state.positionX)
                            continue;
                        if (sp.posY + h2 < veh._state.positionY)
                            continue;
                        if (sp.posY - h2 > veh._state.positionY)
                            continue;

                        objArea++;
                    }

                    //Can we spawn another?
                    if (objArea >= hs.Hide.HideData.MaxTypeInArea)
                        continue;
                }

                short pX = 0;
                short pY = 0;
                int attempts = 0;
                for (; attempts < 10; attempts++)
                {
                    pX = sp.posX;
                    pY = sp.posY;
                    Helpers.randomPositionInArea(this, ref pX, ref pY,
                        (short)hs.Hide.GeneralData.Width, (short)hs.Hide.GeneralData.Height);

                    //Is it blocked?
                    if (getTile(pX, pY).Blocked)
                        continue;

                    break;
                }
                if (attempts == 10) //spawn was blocked
                    continue;

                //Create a suitable state
                Helpers.ObjectState state = new Helpers.ObjectState();

                state.positionX = pX;
                state.positionY = pY;

                Vehicle spawn = newVehicle(vehicle, team, null, state);

                //Set relative ID
                if (hs.Hide.HideData.RelativeId != 0)
                    spawn.relativeID = hs.Hide.HideData.RelativeId;

                //Add to turret group
                if (hs.Hide.HideData.HideTurretGroup != 0 && vehicle.Type == VehInfo.Types.Computer)
                    _turretGroups.Add(hs.Hide, spawn as Computer);

                return true;
            }
            return false;
        }

		/// <summary>
		/// Attempts to trigger a hide spawn
		/// </summary>
		private bool hideSpawn(HideState hs, int spawns, bool initialHide)
		{   //Mark out last attempt
			hs._tickLastAttempt = Environment.TickCount;
			hs._tickLastSuccessAttempt = 0;

            //Probability = 0 means only run on the initial hide
            if (!initialHide && hs.Hide.HideData.Probability == 0)
                return false;

			//Do we have enough players in the game?
			int players = PlayerCount;
			if (players > hs.Hide.HideData.MaxPlayers)
				return false;
			if (players < hs.Hide.HideData.MinPlayers)
				return false;

            List<RelativeObj> spawnPoints;
            if (hs.Hide.GeneralData.RelativeId == 0)
            {   //Fake an object for the spawn point
                spawnPoints = new List<RelativeObj> { new RelativeObj(
                    (short)(hs.Hide.GeneralData.OffsetX - (_server._assets.Level.OffsetX * 16)),
                    (short)(hs.Hide.GeneralData.OffsetY - (_server._assets.Level.OffsetY * 16)),
                    0) };
            }
            else
            { 
                spawnPoints = findRelativeID(hs.Hide.GeneralData.HuntFrequency, hs.Hide.GeneralData.RelativeId, null);
                if (spawnPoints == null || spawnPoints.Count == 0)
                    return false;
            }

            int spawned = 0;
            if (hs.Hide.HideData.HideId > 0)
            {   //Inventory item
                ItemInfo item = _server._assets.getItemByID(hs.Hide.HideData.HideId);
                if (item == null)
                {   //No such item? Add a day delay each time this hits
                    Log.write(TLog.Error, "Hide {0} referenced invalid object id #{1}", hs.Hide, hs.Hide.HideData.HideId);
                    hs.Hide.HideData.AttemptDelay += (24 * 60 * 60) * 1000;
                    return false;
                }

                //Filter list before sending to hideItem()
                List<ItemDrop> sameItems = new List<ItemDrop>{};
                foreach (ItemDrop drop in _items.Values)
                {
                    if (drop.item == item)
                        sameItems.Add(drop);
                }

                //Too many in the level?
                if (hs.Hide.HideData.MaxTypeInlevel != -1 && sameItems.Sum(it => it.quantity) >= hs.Hide.HideData.MaxTypeInlevel)
                    return false;

                //Try to spawn it
                for (; spawns > 0; spawns--)
                    if (hideItem(hs, spawnPoints, item, sameItems))
                        spawned++;
            }
            else
            {   //Vehicle
                VehInfo vehicle = _server._assets.getVehicleByID(-hs.Hide.HideData.HideId);
                if (vehicle == null)
                {   //No such vehicle
                    Log.write(TLog.Error, "Hide {0} referenced invalid vehicle id #{1}", hs.Hide.GeneralData.Name, -hs.Hide.HideData.HideId);
                    hs.Hide.HideData.AttemptDelay += (24 * 60 * 60) * 1000;
                    return false;
                }

                //Find same vehicles
                List<Vehicle> sameVehicles = new List<Vehicle>{};
                foreach (Vehicle veh in _vehicles.ToList())
                {
                    if (veh._type == vehicle)
                        sameVehicles.Add(veh);
                }

                //Too many in level?
                if (hs.Hide.HideData.MaxTypeInlevel != -1 && sameVehicles.Count >= hs.Hide.HideData.MaxTypeInlevel)
                    return false;

                //Find the associated team
                Team team = null;
                _freqTeams.TryGetValue(hs.Hide.HideData.AssignFrequency, out team);

                //Try to spawn it
                for (; spawns > 0; spawns--)
                    if (hideVehicle(hs, spawnPoints, vehicle, sameVehicles, team))
                        spawned++;
            }

            if (spawned == 0)
                return false;

            hs._tickLastSuccessAttempt = Environment.TickCount;

            //Announce it
            //TODO: change to trigger message
            if (hs.Hide.HideData.HideAnnounce.Length > 0)
                sendArenaMessage(hs.Hide.HideData.HideAnnounce);

            return true;
		}

		/// <summary>
		/// Attempts to allow a player to activate a switch
		/// </summary>
		public bool switchRequest(bool bForce, bool bOpen, Player player, LioInfo.Switch swi)
		{	//Are we close enough?
			if (!bForce && !Helpers.isInRange(100,
				player._state.positionX, player._state.positionY,
				swi.GeneralData.OffsetX, swi.GeneralData.OffsetY))
				return false;

			//Obtain our state
			SwitchState ss;

			if (!_switches.TryGetValue(swi.GeneralData.Id, out ss))
			{
				Log.write(TLog.Error, "Unable to find switch state for switch {0}.", swi);
				return false;
			}

			//Is it an opposite state request?
			if ((bOpen && ss.bOpen) || (!bOpen && !ss.bOpen))
				return false;

			//Has there been enough time since the last operation?
			if (!bForce && Environment.TickCount - ss.lastOperation < (swi.SwitchData.SwitchDelay * 10))
				return false;

			//Gather relevant information
			ItemInfo.Ammo ammo = _server._assets.getItemByID(swi.SwitchData.AmmoId) as ItemInfo.Ammo;
			Player.InventoryItem ii = (ammo == null) ? null : player.getInventory(ammo);

			//Do we satisfy any conditions?
			bool bAmmoPass = (ammo == null) || (swi.SwitchData.UseAmmoAmount > ii.quantity);
			bool bTeamPass = (swi.SwitchData.Frequency == -1 || swi.SwitchData.Frequency == player._team._id);
			bool bLogicPass = Logic.Logic_Assets.SkillCheck(player, swi.SwitchData.SkillLogic);
			bool bValid = false;

			if (bForce)
				bValid = true;
			else if (!bTeamPass)
			{	//Test for ignore conditions
				if (bAmmoPass && swi.SwitchData.AmmoOverridesFrequency)
				{
					if (!bLogicPass && swi.SwitchData.AmmoOverridesLogic)
						bValid = true;
					else if (bLogicPass)
						bValid = true;
				}
				else if (bLogicPass && swi.SwitchData.LogicOverridesFrequency && (bAmmoPass || swi.SwitchData.LogicOverridesAmmo))
					bValid = true;
			}
			else
			{	//Test for ignore conditions
				if (!bAmmoPass)
				{
					if (bLogicPass && (swi.SwitchData.FrequencyOverridesAmmo || swi.SwitchData.LogicOverridesAmmo))
						bValid = true;
					else if (!bLogicPass && (swi.SwitchData.FrequencyOverridesLogic && swi.SwitchData.FrequencyOverridesAmmo))
						bValid = true;
				}
				else if (bLogicPass)
					bValid = true;
				else if (swi.SwitchData.AmmoOverridesLogic || swi.SwitchData.FrequencyOverridesLogic)
					bValid = true;
			}

			//Are we qualified?
			if (!bValid)
				return false;

            //We've done it! Update everything
            ss.bOpen = bOpen;
            ss.lastOperation = Environment.TickCount;
            if (swi.SwitchData.Switch == 0)
                updateDoors();
            else
                //This is a turret switch
                _turretGroups.Switch(swi.SwitchData.SwitchLioId, player, bOpen);

			Helpers.Object_LIOs(Players, ss);
			return true;
		}

		/// <summary>
		/// Attempts to allow a player to activate a flag
		/// </summary>
		public bool flagRequest(bool bForce, bool bPickup, bool bSuccess, Player player, LioInfo.Flag flag)
		{	//Obtain our state
			FlagState fs;

			if (!_flags.TryGetValue(flag.GeneralData.Id, out fs))
			{
				Log.write(TLog.Error, "Unable to find flag state for flag {0}.", flag);
				return false;
			}

			//If it isn't active, ignore it
			if (!bForce && !fs.bActive)
				return false;

			//Are we close enough?
			if (!bForce && bPickup && !Helpers.isInRange(100,
				player._state.positionX, player._state.positionY,
				fs.posX, fs.posY))
				return false;

			//If the player is dead..
			if (player.IsDead)
			{	//If he wants to drop, then fail the attempt (resetted flags)
				if (!bPickup)
					bSuccess = false;
				else
				{	//Otherwise, he's a dirty cheater
					Log.write(TLog.Warning, "Player {0} attempted to activate a flag while dead.", player);
					return false;
				}
			}

			//Is the player already carrying?
			if (fs.carrier == player && bPickup)
				return false;

			//Has there been enough time since the last operation?
			if (!bForce && Environment.TickCount - fs.lastOperation < (flag.FlagData.PickupDelay * 10))
				return false;

			//Do we have the skill to use this flag?
			if (!Logic_Assets.SkillCheck(player, flag.FlagData.SkillLogic))
				return false;

            bool turfFlag = fs.flag.FlagData.FlagCarriable == 0;
			//We've done it! Update everything
			if (bPickup && !turfFlag)
				fs.carrier = player;
			else
				fs.carrier = null;

			if (bSuccess)
			{
				fs.oldTeam = fs.team;

                if (bPickup)
                {
                    fs.team = (flag.FlagData.IsFlagOwnedWhenCarried ? player._team : null);
                    if (fs.flag.FlagData.TurretrGroupId != 0)
                        //Switch these turrets
                        _turretGroups.Switch(fs.flag.FlagData.TurretrGroupId, player, true);
                }
                else
                    fs.team = (flag.FlagData.IsFlagOwnedWhenDropped ? player._team : null);

                if (!turfFlag)
                {
                    fs.posX = player._state.positionX;
                    fs.posY = player._state.positionY;
                }


			}
			else
				fs.team = fs.oldTeam;

			fs.lastOperation = Environment.TickCount;

			//If we're dropping, randomize accordingly
			if (!bPickup && fs.flag.FlagData.DropRadius != 0)
				Helpers.randomPositionInArea(this, fs.flag.FlagData.DropRadius, ref fs.posX, ref fs.posY);

			Helpers.Object_Flags(Players, fs);
			return true;
		}

		/// <summary>
		/// Resets all the flags the player is carrying to their original state
		/// </summary>
		public void flagHandleDeath(Player player, Player killer)
		{	//Get the list of relevant flag states
			List<FlagState> carried = _flags.Values.Where(flag => flag.carrier == player).ToList();
			if (carried.Count == 0)
				return;

			//Reset each of them
			foreach (FlagState fs in carried)
			{	//What do we do with them?
				switch (fs.flag.FlagData.TransferMode)
				{
					//Kill transfer no friendly
					case 0:
						if (killer._team != player._team)
							fs.carrier = killer;
						else
							fs.carrier = null;
						break;

					//Kill transfer friendly
					case 1:
						fs.carrier = killer;
						break;

					//No kill transfers
					case 2:
						fs.carrier = null;
						break;
				}

				//Update the positions and teams
				if (fs.carrier != null)
				{
					fs.oldTeam = fs.team;
					fs.team = fs.carrier._team;
				}

				fs.posX = player._state.positionX;
				fs.posY = player._state.positionY;
				Helpers.randomPositionInArea(this, fs.flag.FlagData.DropRadius, ref fs.posX, ref fs.posY);

				fs.lastOperation = Environment.TickCount;				
			}

            //Do we notify players?
            if (_server._zoneConfig.flag.announceTransfers)
            {	//Yep
                if (carried.Count == 1)
                    triggerMessage(1, 500, player._alias + " lost a flag to " + killer._alias);
                else
                    triggerMessage(1, 500, player._alias + " lost " + carried.Count + " flags to " + killer._alias);
            }

			//Update
			Helpers.Object_Flags(Players, carried);
		}

		/// <summary>
		/// Resets all the flags the player is carrying to their original state
		/// </summary>
		public void flagResetPlayer(Player player)
		{	//Get the list of relevant flag states
			List<FlagState> carried = _flags.Values.Where(flag => flag.carrier == player).ToList();
			if (carried.Count == 0)
				return;

			//Reset each of them
			foreach (FlagState fs in carried)
			{
				fs.lastOperation = Environment.TickCount;
				fs.team = fs.oldTeam;
				fs.carrier = null;

				//Position will have been set when the player picked the flag up
			}

			//Update
			Helpers.Object_Flags(Players, carried);
		}

		/// <summary>
		/// Removes all flags from the arena
		/// </summary>
		public void flagReset()
		{	//For each flag
			foreach (FlagState fs in _flags.Values)
			{	//Gone!
				fs.oldTeam = null;
				fs.team = null;

				fs.bActive = false;
				fs.carrier = null;
				fs.lastOperation = 0;
			}

			Helpers.Object_FlagsReset(Players);
		}

		/// <summary>
		/// Spawns all the qualified flags in the arena
		/// </summary>
		public void flagSpawn()
		{	//For each flag type..
			foreach (FlagState fs in _flags.Values)
			{	//Should we spawn it?
				bool bActive = true;

				if (PlayerCount < fs.flag.FlagData.MinPlayerCount)
					continue;
				if (PlayerCount > fs.flag.FlagData.MaxPlayerCount)
					continue;

				//Check probability
				if (fs.flag.FlagData.OddsOfAppearance == 0)
					continue;
				else if (fs.flag.FlagData.OddsOfAppearance != 1000)
				{	//Test it
					if (_rand.Next(0, 1000) >= fs.flag.FlagData.OddsOfAppearance)
						continue;
				}

				//Give it some valid coordinates
				int attempts = 0;

				do
				{	//Make sure we're not doing this infinitely
					if (attempts++ > 200)
					{
						Log.write(TLog.Error, "Unable to satisfy flag spawn for '{0}'.", fs.flag);
						bActive = false;
						break;
					}

					fs.posX = (short)(fs.flag.GeneralData.OffsetX);
					fs.posY = (short)(fs.flag.GeneralData.OffsetY);

					Helpers.randomPositionInArea(this, ref fs.posX, ref fs.posY,
						(short)fs.flag.GeneralData.Width, (short)fs.flag.GeneralData.Height);

					//Check the terrain settings
					if (getTerrain(fs.posX, fs.posY).flagTimerSpeed == 0)
						continue;
				}
				while (getTile(fs.posX, fs.posY).Blocked);

				fs.team = null;
				fs.carrier = null;
				fs.bActive = bActive;
			}

			Helpers.Object_Flags(Players, _flags.Values);
		}
	}
}
