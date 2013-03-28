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
	// Player Class
	/// Represents a single player in the server
	///////////////////////////////////////////////////////
	public partial class Player : CustomObject, IClient, ILocatable
	{	// Member variables
		///////////////////////////////////////////////////
		public Client _client;					//Our network client
		public Arena _arena;					//The arena we're currently in
		public Team _team;						//The team we belong to

		public ZoneServer _server;				//The server we work for!

		public volatile bool bDestroyed;		//Have we already been destroyed?
		public bool _bIngame;					//Are we in the game, or in an arena transition?
		public bool _bLoggedIn;					//Have we made it past the login process, and are able to enter arenas?
        public List<DateTime> _msgTimeStamps;   //For spam checking

		#region Credentials
		public ushort _id;						//Unique zone id for a player
		public int _magic;						//Magic id used for distinguishing players with similiar id

		public string _alias;					//Our current name
		public string _squad;					//The squad he belongs to
        public long _squadID;

		public Data.PlayerPermission _permissionStatic;	//The player's global permission in this zone
		public Data.PlayerPermission _permissionTemp;	//The player's permission in his current arena

        public IPAddress _ipAddress;
        public uint _UID1;
        public uint _UID2;
        public uint _UID3;
		#endregion

		#region Game state
		public bool _bIgnoreUpdates;			//Are we temporarily ignoring player updates? (Usually due to vehicle change)
		public bool _bSpectator;				//Is the player in spectator mode?
        public bool _bIsStealth;                //Is the mod hidden to player lists?
        public int _level;                      //The players level

        //Player shutup stuff..
        public bool _bSilenced;                 //Is the player currently silenced?
        public DateTime _timeOfSilence;         //When he was silenced
        public int _lengthOfSilence;            //For how long...

        public bool _bLocked;                   //Is the player locked in spec?
        public bool _bAllowSpectator;           //Is the player allowing spectators?
        public bool _specQuiet;                 //Is the player allowed to type outside of spec?

		public Helpers.ObjectState _state;		//The player's positional state

		public Vehicle _baseVehicle;			//Our innate vehicle
		public Vehicle _occupiedVehicle;		//The vehicle we're currently residing in

		public bool _bEnemyDeath;				//Was the player killed by an enemy, or teammate?
		public int _deathTime;					//The tickcount at which we were killed

		public int _lastItemUseID;				//The id and ticktime at which the last item
		public int _lastItemUse;				//was fired.

        public int _lastVehicleEntry;           //The tick at which the player last entered or exited a vehicle

        public int _lastMovement;               //The tickcount at which the player last made a movement
        public uint _assetCS;

        public bool suspendCalled = false;      //Has suspended stats already been called?

        //Player arena ban stuff
        public DateTime _timeOfBlock;           //When he/she was banned from arena
        public int _lengthOfBlock;              //How long the block is
		#endregion

		#region Player state
		public Player _spectating;				//The player we're currently spectating
		public List<Player> _spectators;		//The players that are currently spectating us
        public List<string> _summonIgnore;      //The players that are currently summon-ignored.

        public int _gotBallID = 999;			//The Id of the ball

		public byte[] _bannerData;				//The data for our current banner
		public int _bounty;						//Our current bounty

		public Dictionary<int, InventoryItem> _inventory;	//Our current inventory
		public Dictionary<int, SkillItem> _skills;	//Our current skill inventory
        public List<ItemInfo.UtilityItem> activeUtilities;	//Active Utilities
        public Dictionary<SkillItem, int> _skillCounts;

        public bool firstTimePlayer;
		public bool _bDBLoaded;						//Has the player's statistics been loaded from the database?

		//Suspended player state
		private Data.PlayerStats _suspStats;
		private Dictionary<int, InventoryItem> _suspInventory;
		private Dictionary<int, SkillItem> _suspSkills;
		#endregion
        
		#region Events
		public event Action<Player> LeaveArena;	//Called when the player leaves the arena
		#endregion

		///////////////////////////////////////////////////
		// Accessors
		///////////////////////////////////////////////////
		/// <summary>
		/// The player's bounty amount
		/// </summary>
		public int Bounty
		{
			get
			{
				return _bounty;
			}

			set
			{	//Check for stuff
				if (value > 30000)
					_bounty = 30000;
				else
					_bounty = value;

				Helpers.Player_SetBounty(this, (short)_bounty);
			}
		}

		/// <summary>
		/// Is this player in spectator mode?
		/// </summary>
		public bool IsSpectator
		{
			get
			{
				return _bSpectator;
			}
		}

        /// <summary>
        /// Is the player/mod invisible to arena lists?
        /// </summary>
        public bool IsStealth
        {
            get
            {
                return _bIsStealth;
            }
        }

		/// <summary>
		/// Is this player currently dead?
		/// </summary>
		public bool IsDead
		{
			get
			{	
				return _state.health == 0 && !IsSpectator;
			}
		}

		/// <summary>
		/// Gets the vehicle the player is currently in
		/// </summary>
		public Vehicle ActiveVehicle
		{
			get
			{
				return (_occupiedVehicle == null ? _baseVehicle : _occupiedVehicle);
			}
		}

		/// <summary>
		/// Gets the player's absolute permission level across the zone
		/// </summary>
		public Data.PlayerPermission PermissionLevel
		{
			get
			{
				return (Data.PlayerPermission)_permissionStatic;
			}
		}

		/// <summary>
		/// Gets the player's permission level in his current state
		/// </summary>
		public Data.PlayerPermission PermissionLevelLocal
		{
			get
			{
				return (Data.PlayerPermission)Math.Max((sbyte)_permissionStatic, (sbyte)_permissionTemp);
			}
		}

		/// <summary>
		/// Gives a short summary of this player
		/// </summary>
		public override string ToString()
		{	//Return the player credentials
			return String.Format("{0} ({1})", _alias, _id);
		}

		#region ILocatable Functions
		public ushort getID() { return _id; }
		public Helpers.ObjectState getState() { return _state; }
		#endregion

		///////////////////////////////////////////////////
		// Member Classes
		///////////////////////////////////////////////////
		#region Member Classes
		/// <summary>
		/// Represents a single element in the skill inventory
		/// </summary>
		public class SkillItem
		{
			public SkillInfo skill;		//The type of skill
			public short quantity;		//The amount we have of said skill

			static public int MaxSkills = 100;
		}

		/// <summary>
		/// Represents a single element in the inventory
		/// </summary>
		public class InventoryItem
		{
			public ItemInfo item;		//The type of item
			public ushort quantity;		//The amount which we have

			static public int MaxItems = 100;
		}
		#endregion

		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Generic constructor
		/// </summary>
		public Player()
		{
			_alias = "";

			_state = new Helpers.ObjectState();
			_bounty = 1;

			_spectators = new List<Player>();
            _summonIgnore = new List<string>();

            activeUtilities = new List<ItemInfo.UtilityItem>();

            _msgTimeStamps = new List<DateTime>();
		}

		#region State
		/// <summary>
		/// The player is being destroyed, clean up assets
		/// </summary>
		public void destroy()
		{	//Make sure we don't perform this twice
			if (bDestroyed)
				return;
			bDestroyed = true;

			using (LogAssume.Assume(_server._logger))
			{	//Take him out of his arena
				leftArena();

				//and next, the zone server
				_server.lostPlayer(this);
			}
		}

		/// <summary>
		/// Disconnects the player and removes from everything
		/// </summary>
		public void disconnect()
		{
			Helpers.Player_Disconnect(this);
			destroy();
		}

        /// <summary>
        /// Updates active equipment
        /// </summary>
        /// <param name="equip"></param>
        public void updateActiveEquip(List<ushort> equip)
        {
            activeUtilities = new List<ItemInfo.UtilityItem>();

            foreach (ushort utility in equip)
            {
                activeUtilities.Add(_server._assets.getItemByID(utility) as ItemInfo.UtilityItem);
            }
        }

        /// <summary>
        /// Checks for anti warps.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public Player checkAntiWarp()
        {	//Get the list of bots in the area
            IEnumerable<Player> candidates = _arena.getPlayersInRange(_state.positionX, _state.positionY, 5000);

            foreach (Player candidate in candidates)
            {   //Ignore teammates..
                if (candidate == null || candidate._team == null || candidate._team == _team || candidate.IsDead || candidate.activeUtilities == null)
                    continue;
                
                //Any anti-warp utils?
                if (!candidate.activeUtilities.Any(util => util != null && util.antiWarpDistance != -1))
                    continue;
                //Is it within the distance?
                int dist = (int)(_state.position().Distance(candidate._state.position()) * 100);

                if (candidate.activeUtilities.Any(util => util != null && util.antiWarpDistance >= dist))
                    return candidate;
            }

            return null;
        }

		/// <summary>
		/// The player has left the arena, reset assets
		/// </summary>
		public void leftArena()
		{	//Stop spectating
			if (_spectating != null)
			{
				_spectating._spectators.Remove(this);
				_spectating = null;
			}

            //If we're currently in a vehicle, we want to desert it
            if (_occupiedVehicle != null)
                _occupiedVehicle.playerLeave(true);
            _occupiedVehicle = null;

			//Notify our team
			if (_team != null)
				_team.lostPlayer(this);
			_team = null;

			//Notify our current arena
            if (_arena != null)
            {
                //Lets take them away from the owner list
                if (_arena.IsPrivate && _arena._owner != null && _arena._owner.Count > 0)
                {
                    foreach (var p in _arena._owner)
                        if (this._alias.Equals(p))
                        {
                            _arena._owner.Remove(p);
                            if (this.PermissionLevel < Data.PlayerPermission.ArenaMod)
                                this._permissionTemp = Data.PlayerPermission.Normal;
                            break;
                        }
                }
                _arena.lostPlayer(this);
            }
			_arena = null;

			//We are no longer ingame
			_bIngame = false;
		}

		/// <summary>
		/// Allows a client downloading the gamestate to enter an arena
		/// </summary>
		public void setIngame()
		{	//Send the enter packet
			_bIngame = true;
			_client.sendReliable(new SC_EnterArena());
		}
		#endregion

		#region Game State
		/// <summary>
		/// Retreives the player's intended default vehicle based on settings
		/// </summary>
		public VehInfo getDefaultVehicle()
		{	//Find a skill with a default vehicle
			Player.SkillItem baseSkill = _skills.Values.LastOrDefault(skill => skill.skill.DefaultVehicleId != -1);

			//Use said skill, or the cfg default
			int baseVehicleID = (baseSkill == null) ? _server._zoneConfig.publicProfile.defaultVItemId : baseSkill.skill.DefaultVehicleId;
			VehInfo defaultVehicle = _server._assets.getVehicleByID(baseVehicleID);

			return defaultVehicle;
		}

		/// <summary>
		/// Changes the player's default vehicle type
		/// </summary>
		public void setDefaultVehicle(VehInfo defaultVehicle)
		{	//If the vehicle info is null, set it as our skill-based vehicle
			if (defaultVehicle == null)
				defaultVehicle = getDefaultVehicle();

			//Create our new base vehicle..
			int oldDVID = _baseVehicle._type.Id;
			Vehicle baseVehicle = new Vehicle(defaultVehicle, _arena);

			baseVehicle._bBaseVehicle = true;
			baseVehicle._arena = _arena;
			baseVehicle._id = _id;

			//Dispose of the old basevehicle
			if (_baseVehicle._inhabitant != null)
			{
				baseVehicle._inhabitant = _baseVehicle._inhabitant;
				_baseVehicle._inhabitant = null;
				_baseVehicle.bCondemned = true;
			}

			//Player and basevehicle share same state
			baseVehicle._state = _state;
			baseVehicle.assignDefaultState();

			_baseVehicle = baseVehicle;

			//Sync with the arena
			Helpers.Object_Vehicles(_arena.Players, baseVehicle);

			//If we actually changed vehicle, trigger the event
			if (oldDVID != defaultVehicle.Id)
				Logic_Assets.RunEvent(this, _server._zoneConfig.EventInfo.changeDefaultVehicle);
		}

		/// <summary>
		/// Attempts to have the player enter the specified vehicle
		/// </summary>
		public bool enterVehicle(Vehicle toEnter)
		{
            //wtfack
            if (toEnter == null)
                return false;

            //Are we able to enter the vehicle?
			if (!toEnter.playerEnter(this))
				return false;                      

			//We're in!
			return true;
		}

		/// <summary>
		/// Finds the specified skill in the skill inventory, if it exists
		/// </summary>
		public SkillItem findSkill(SkillInfo skill)
		{	//Attempt to get it
			SkillItem item;

			if (!_skills.TryGetValue(skill.SkillId, out item))
				return null;
			return item;
		}

		/// <summary>
		/// Finds the specified skill in the skill inventory, if it exists
		/// </summary>
		public SkillItem findSkill(int skillid)
		{	//Attempt to get it
			SkillItem item;

			if (!_skills.TryGetValue(skillid, out item))
				return null;
			return item;
		}

		/// <summary>
		/// Modifies and updates the player's skill inventory
		/// </summary>
		public bool skillModify(SkillInfo skill, int adjust)
		{	//Redirect
			return skillModify(true, skill, adjust);
		}

		/// <summary>
		/// Modifies and updates the player's skill inventory
		/// </summary>
		public bool skillModify(bool bSyncState, SkillInfo skill, int adjust)
		{	            
            //Do we already have such a skill?
			SkillItem sk;
			_skills.TryGetValue(skill.SkillId, out sk);

			if (sk != null)
			{	//If it's a skill and not an attribute, we can only have one..
				if (skill.SkillId > 0)
				{
					Log.write(TLog.Warning, "Attempted to add duplicate skill {0} for player {1}.", skill.Name, this);
					return false;
				}
				
				//Do we have enough attributes?
				if (sk.quantity + adjust < 0)
				{
					Log.write(TLog.Warning, "Attempted to remove too many attributes from player {0}.", this);
					return false;
				}
			}
			else if (adjust < 0)
			{
				Log.write(TLog.Warning, "Attempted to remove attributes which didn't exist from player {0}.", this);
				return false;
			}

			//Attribute or skill?
			if (skill.SkillId >= 0)
			{   //Do we have enough experience for this skill?
				if (skill.Price > Experience)
					return false;

				Experience -= skill.Price;
				//Success, let's also change the cash..
				Cash = Math.Max(Cash + skill.CashAdjustment, 0);

				//Clear inventory?
				if (skill.ResetInventory)
					_inventory.Clear();
				//Process inventory adjustments
				foreach (SkillInfo.InventoryMutator ia in skill.InventoryMutators)
				{	//If it's valid..
					if (ia.ItemId == 0)
						continue;

					//Add our item!
					ItemInfo item = _server._assets.getItemByID(ia.ItemId);
					if (item == null)
					{
						Log.write(TLog.Error, "Invalid itemID #{0} for inventory adjustment.", ia.ItemId);
						continue;
					}

					inventoryModify(false, item.id, ia.Quantity);
				}

				//Finally, do we use a new defaultvehicle?
				if (skill.DefaultVehicleId != -1)
				{	//Yes, create and apply it
					VehInfo baseType = _server._assets.getVehicleByID(skill.DefaultVehicleId);

					if (baseType == null)
						Log.write(TLog.Error, "Invalid vehicleID #{0} for default skill vehicle.", skill.DefaultVehicleId);
                    else if (_arena != null)
                    {
                        setDefaultVehicle(_server._assets.getVehicleByID(skill.DefaultVehicleId));
                        
                        //Set relative vehicle
                        if (!IsSpectator)
                        {
                            try
                            {
                                VehInfo vehicle; //= _server._assets.getVehicleByID(skill.DefaultVehicleId + _server._zoneConfig.teams[_team._id].relativeVehicle);
                                if (_team.IsPublic)
                                    vehicle = _server._assets.getVehicleByID(skill.DefaultVehicleId + _server._zoneConfig.teams[_team._id].relativeVehicle);
                                else
                                    vehicle = _server._assets.getVehicleByID(skill.DefaultVehicleId + _team._relativeVehicle);
                                //Make sure we're not switching twice..
                                if (getDefaultVehicle() != vehicle)
                                    setDefaultVehicle(vehicle);
                            }
                            catch (Exception e)
                            {
                                //this happens when player is on private team
                                Log.write(TLog.Warning, "default vech change " + e);
                            }
                        }
                    }
				}
			}
			else
			{   //Attributes
                int cost = skill.Price + _server._zoneConfig.rpg.attributeBaseCost;
                double attributeCountPower;
                Double.TryParse(_server._zoneConfig.rpg.attributeCountPower, out attributeCountPower);
                if (_skills.Keys.Contains(skill.SkillId))
                    cost = (int)(Math.Pow(_skills[skill.SkillId].quantity + 1, attributeCountPower) * skill.Price + _server._zoneConfig.rpg.attributeBaseCost);
                
				//Do we have enough experience for this skill?
                if (cost > Experience)
                    return false;

                Experience -= cost;
			}
			//Add the skill to our skill list
			if (sk != null)
			{	//Will there be any attributes left?
                if (adjust < 0 && (sk.quantity + adjust == 0))
                {
                    _skills.Remove(skill.SkillId);
                    Log.write(TLog.Warning, "Removing skill {0} {1} {2} {3}", skill.Name, skill.SkillId,adjust,sk.quantity);   
                }
                else
                {
                    sk.quantity = (short)(sk.quantity + adjust);
                    syncState();
                }

			}
			else
			{	//We need to add a new skill item, should we reset other skills?
				switch (skill.ResetSkills)
				{	//All skills
					case 1:
						_skills.Clear();
						break;
					//Only skills
					case 2:
						{
							List<SkillItem> removes = _skills.Values.Where(skl => skl.skill.SkillId >= 0).ToList();
							foreach (SkillItem skl in removes)
								_skills.Remove(skl.skill.SkillId);
						}
						break;
					//Only attributes
					case 3:
						{
							List<SkillItem> removes = _skills.Values.Where(skl => skl.skill.SkillId < 0).ToList();
							foreach (SkillItem skl in removes)
								_skills.Remove(skl.skill.SkillId);
						}
						break;
				}
                
				//Add our new skill
				sk = new SkillItem();
				sk.skill = skill;
				sk.quantity = (short)adjust;
				_skills.Add(sk.skill.SkillId, sk);
                syncState();             
			}

			//Update the player's state
			if (bSyncState)
				syncState();
			return true;
		}
		
		/// <summary>
		/// Sets an absolute amount for a specific item
		/// </summary>
		public void inventorySet(ItemInfo item, int amount)
		{
			inventorySet(true, item, amount);
		}

		/// <summary>
		/// Sets an absolute amount for a specific item
		/// </summary>
		public void inventorySet(bool bSync, ItemInfo item, int amount)
		{	//Do we already have such an item?
			InventoryItem ii;
			_inventory.TryGetValue(item.id, out ii);

			if (ii == null)
			{	//We need to add a new inventory item
				ii = new InventoryItem();

				ii.item = _server._assets.getItemByID(item.id);
				ii.quantity = (ushort)amount;

				_inventory.Add(item.id, ii);
			}
			else
				ii.quantity = (ushort)amount;

			if (bSync)
				syncInventory();
		}

		/// <summary>
		/// Modifies and updates the player's inventory
		/// </summary>
		public bool inventoryModify(ItemInfo item, int adjust)
		{	//Redirect
			return inventoryModify(true, item, adjust);
		}	

		/// <summary>
		/// Modifies and updates the player's inventory
		/// </summary>
		public bool inventoryModify(bool bSync, ItemInfo item, int adjust)
		{
            //Do we already have such an item?
            InventoryItem ii;
            _inventory.TryGetValue(item.id, out ii);
            if (ii != null && adjust < 0)
            {
                //Trying to take away too many?? I dont understand why wouldn't just wrap to -ii.quantity 
                if (ii.quantity + adjust < 0) // but I'll keep this how it was
                    return false;

                //Will there be any items left?
                if (ii.quantity + adjust == 0)
                    _inventory.Remove(item.id);
                else
                    ii.quantity = (ushort)(ii.quantity + adjust);

                if (bSync)
                    syncInventory();
                return true;
            }

            //Is this item an upgrade item?
			if (item.itemType == ItemInfo.ItemType.Upgrade)
			{	//Apply it!
				applyUpgradeItem(bSync, (ItemInfo.UpgradeItem)item, adjust);
				return true;
			}
			//A skill item?
			else if (item.itemType == ItemInfo.ItemType.Skill)
			{	//Add each of the applicable skills
				ItemInfo.SkillItem skill = (ItemInfo.SkillItem)item;

				foreach (ItemInfo.SkillItem.Skill entry in skill.skills)
				{	//No skill?
					if (entry.ID == 0)
						continue;
					
					//Do we satisfy the logic?
					if (!Logic_Assets.SkillCheck(this, entry.logic))
						continue;

					//Obtain the skill..
					SkillInfo skillInfo = _server._assets.getSkillByID(entry.ID);
					if (skillInfo == null)
					{
						Log.write(TLog.Warning, "Attempted to add non-existent skill '{0}' for skill item '{1}'", entry.ID, item.name);
						continue;
					}
					
					//Add the skill!
					skillModify(skillInfo, 1);
				}

				//At the moment we always sync - otherwise this function may only sync inventory
				syncState();
				return true;
			}
			//A multi item?
			else if (item.itemType == ItemInfo.ItemType.Multi)
			{	//Apply it!
				useMultiItem(item, (short)adjust);
				applyMultiItem(bSync, (ItemInfo.MultiItem)item, adjust);
				return true;
			}

            //Held category checks
            if (adjust > 0 && ii == null && item.heldCategoryType > 0)
            {
                int alreadyHolding = _inventory
                    .Where(it => it.Value.item.heldCategoryType == item.heldCategoryType)
                    .Sum(it => 1);
                //Veh editor says a held category is "maximum number of unique types of items of this category type"
                //Vehicle hold categories take precedence over the cfg values
                if (ActiveVehicle == null)
                    //Maybe they haven't instanced an active vehicle yet?
                    return false;
                if (ActiveVehicle._type.HoldItemLimits[item.heldCategoryType - 1] != -1)
                {
                    if (1 + alreadyHolding > ActiveVehicle._type.HoldItemLimits[item.heldCategoryType - 1])
                        return false;
                }
                else if (ActiveVehicle != _baseVehicle &&
                    _baseVehicle._type.HoldItemLimits[item.heldCategoryType - 1] != -1)
                {
                    if (1 + alreadyHolding > _baseVehicle._type.HoldItemLimits[item.heldCategoryType - 1])
                        return false;
                }
                else if (_server._zoneConfig.heldCategory.limit[item.heldCategoryType - 1] != -1)
                {
                    if (1 + alreadyHolding > _server._zoneConfig.heldCategory.limit[item.heldCategoryType - 1])
                        return false;
                }
            }

			if (ii != null)
			{	//Is there enough space?
				if (item.maxAllowed < 0 && adjust > 0 && ii.quantity + adjust > (-item.maxAllowed))
					//Add only the amount we're able to
					adjust = -item.maxAllowed - ii.quantity; 

				//Do we have enough items?
				if (ii.quantity + adjust < 0)
					return false;

				//Will there be any items left?
				if (adjust < 0 && (ii.quantity + adjust == 0))
					_inventory.Remove(item.id);
				else
					ii.quantity = (ushort)(ii.quantity + adjust);
			}
			else if (adjust < 0)
			{
				return false;
			}
			else
			{	//Is there enough space?
				if (item.maxAllowed < 0)
					adjust = Math.Min(-item.maxAllowed, adjust);

				//We need to add a new inventory item
				ii = new InventoryItem();

				ii.item = _server._assets.getItemByID(item.id);
				ii.quantity = (ushort)adjust;
				_inventory.Add(item.id, ii);
			}

			//Update the player's inventory
			if (bSync)
				syncInventory();
			return true;
		}

		/// <summary>
		/// Modifies and updates the player's inventory
		/// </summary>
		public bool inventoryModify(int itemid, int adjust)
		{	//Redirect
			return inventoryModify(true, _server._assets.getItemByID(itemid), adjust);
		}

		/// <summary>
		/// Modifies and updates the player's inventory
		/// </summary>
		public bool inventoryModify(bool bSyncInv, int itemid, int adjust)
		{	//Get the item info
			return inventoryModify(bSyncInv, _server._assets.getItemByID(itemid), adjust);
		}

		/// <summary>
		/// Removes all items of a specific type from the player's inventory
		/// </summary>
		public void removeAllItemFromInventory(int itemID)
		{	//Attempt to remove it!
			removeAllItemFromInventory(true, itemID);
		}

		/// <summary>
		/// Removes all items of a specific type from the player's inventory
		/// </summary>
		public void removeAllItemFromInventory(bool bSyncInv, int itemID)
		{	//Attempt to remove it!
			if (_inventory.Remove(itemID))
			{	//Should we sync?
				if (bSyncInv)
					syncInventory();
			}
		}

		/// <summary>
		/// Applys the changes a multi item makes from the inventory
		/// </summary>
		private void applyMultiItem(bool bSync, ItemInfo.MultiItem multiItem, int repeat)
		{	//Adjust stats as necessary
			if (multiItem.Cash != 0)
				this.Cash += multiItem.Cash * repeat;

			if (multiItem.Experience != 0)
				this.Experience += multiItem.Experience * repeat;

            if (multiItem.Energy != 0)
                setEnergy((short)(_state.energy + (repeat * multiItem.Energy)));

			//Give the player his items
			foreach (ItemInfo.MultiItem.Slot slot in multiItem.slots)
			{	//Valid item?
				if (slot.value == 0)
					continue;

				//Attempt to find the item in question
				ItemInfo item = _server._assets.getItemByID(slot.value);
				if (item == null)
				{
					Log.write(TLog.Warning, "MultiItem {0} attempted to spawn invalid item #{1}", multiItem.name, slot.value);
					continue;
                }

                if (!Logic_Assets.SkillCheck(this, item.skillLogic))
                    continue;

                //Add an item!
				inventoryModify(false, item, repeat);
			}

			if (bSync)
				syncState();
		}

		/// <summary>
		/// Applys the changes an update item makes from the inventory
		/// </summary>
		private void applyUpgradeItem(bool bSyncInv, ItemInfo.UpgradeItem upgradeItem, int repeat)
		{	//Find the first input item which matches
			bool bNoAdd = false;

			while (repeat-- > 0 && !bNoAdd)
			{
				bNoAdd = true;

				foreach (ItemInfo.UpgradeItem.Upgrade upgrade in upgradeItem.upgrades)
				{	//Valid entry?
					if (upgrade.inputID == 0 && upgrade.outputID == 0)
						continue;

					//If there is no input item..
					if (upgrade.inputID == 0)
					{	//Just gift the output item!
						inventoryModify(false, upgrade.outputID, 1);
						break;
					}

					//Do we have such an item?
					InventoryItem ii;
					_inventory.TryGetValue(upgrade.inputID, out ii);

					if (ii == null || ii.quantity <= 0)
						continue;

					//Yes! Remove the item
					inventoryModify(false, upgrade.inputID, -1);

					//Do we replace with an output item?
					if (upgrade.outputID != 0)
						inventoryModify(false, upgrade.outputID, 1);

					bNoAdd = false;
					break;
				}
			}

			if (bSyncInv)
				syncInventory();
		}

		/// <summary>
		/// Clears all items from the player's inventory
		/// </summary>
		public void resetInventory(bool bSync)
		{	//Clear 'em all!
			_inventory.Clear();
			if (bSync)
				syncInventory();
		}

		/// <summary>
		/// Removes all skills from the player
		/// </summary>
		public void resetSkills(bool bSync)
		{	//Clear 'em all!
			_skills.Clear();
			if (bSync)
				syncState();
		}

		/// <summary>
		/// Clears all projectiles for the client
		/// </summary>
		public void clearProjectiles()
		{	//Do eet
			Helpers.Player_ClearProjectiles(this);
		}

		/// <summary>
		/// Notifies the player that he has been healed
		/// </summary>
		/// <param name="item">The item used to heal the player</param>
		/// <param name="healer">The player who initiated the healing</param>
		public void heal(ItemInfo.RepairItem item, Player healer)
		{	//Redirect
			heal(item, healer, healer._state.positionX, healer._state.positionY);
		}

		/// <summary>
		/// Notifies the player that he has been healed
		/// </summary>
		/// <param name="item">The item used to heal the player</param>
		/// <param name="healer">The player who initiated the healing</param>
		public void heal(ItemInfo.RepairItem item, Player healer, short posX, short posY)
		{	//Send him the notification
			Helpers.Player_RouteItemUsed(this, healer, this._id, (Int16)item.id, posX, posY, 0); 
		}

		/// <summary>
		/// Sets the energy for the player
		/// </summary>
		public void setEnergy(short energy)
		{
			Helpers.Vehicle_SetEnergy(this, energy);
		}

		/// <summary>
		/// Resets the player vehicle's state
		/// </summary>
		public void resetState(bool resetEnergy, bool resetHealth, bool resetVelocity)
		{
			Helpers.Vehicle_ResetState(this, resetEnergy, resetHealth, resetVelocity);
		}

		/// <summary>
		/// Applies a multi item's effects to the player's client
		/// </summary>
		public void useMultiItem(ItemInfo item, short count)
		{
			Helpers.Player_UseMultiItems(this, (short)item.id, count);
		}
		#endregion

		#region Helpers
		/// <summary>
		/// Synchronizes the player's inventory with the player's client
		/// </summary>
		public void syncInventory()
		{
			Helpers.Player_InventoryUpdate(this);
		}

		/// <summary>
		/// Synchronizes the player's entire state with the player's client
		/// </summary>
		public void syncState()
		{
			Helpers.Player_StateInit(this, null);
		}

		/// <summary>
		/// Retrives the inventory item count for the specified item type
		/// </summary>
		public int getInventoryAmount(int itemid)
		{	//Do we have such an item?
			InventoryItem ii;

			if (!_inventory.TryGetValue(itemid, out ii))
				return 0;
			return ii.quantity;
		}

		/// <summary>
		/// Retrives the inventory entry for the specified item type
		/// </summary>
		public InventoryItem getInventory(int itemid)
		{	//Do we have such an item?
			InventoryItem ii;

			if (!_inventory.TryGetValue(itemid, out ii))
				return null;
			return ii;
		}

		/// <summary>
		/// Retrives the inventory entry for the specified item type
		/// </summary>
		public InventoryItem getInventory(ItemInfo item)
		{	//Do we have such an item?
			InventoryItem ii;

			if (!_inventory.TryGetValue(item.id, out ii))
				return null;
			return ii;
		}

		/// <summary>
		/// Forces the player to spectate a certain player
		/// </summary>
		public bool spectate(Player toSpectate)
		{	//Check whether the players are appropriate
			if (toSpectate.IsSpectator)
				return false;
			if (!IsSpectator)
				return false;

			//Stop spectating
			if (_spectating != null)
			{
				_spectating._spectators.Remove(this);
				_spectating = null;
			}
			
			toSpectate._spectators.Add(this);
			_spectating = toSpectate;

			Helpers.Player_SpectatePlayer(this, toSpectate);
			return true;
		}

        /// <summary>
        /// Sends the player to spectator mode
        /// </summary>
        public bool spec()
        {	//Redirect and return
            return spec("spec");
        }


		/// <summary>
		/// Sends the player to spectator mode
		/// </summary>
		public bool spec(string team)
		{	//Find our team and redirect
			Team specTeam = _arena.getTeamByName(team);
			if (specTeam == null)
			{
				Log.write(TLog.Warning, "Invalid spectator team {0}!", team);
				return false;
			}

			return spec(specTeam);
		}

		/// <summary>
		/// Sends the player to spectator mode
		/// </summary>
		public bool spec(Team team)
		{	//Let's create a new spectator vehicle
			Vehicle specVeh = _arena.newVehicle(_server._zoneConfig.arena.spectatorVehicleId);

            //If we're currently in a vehicle, we want to desert it MIZZ123
            if (_occupiedVehicle != null)
                _occupiedVehicle.playerLeave(false);

			//Have the player enter it
			if (!enterVehicle(specVeh))
			{	//This shouldn't happen!
				Log.write(TLog.Error, "Unable to bind player to spectator vehicle. {0}", this);
				return false;
			}

			_bSpectator = true;

			//Throw ourselves onto team spec!
			team.addPlayer(this);

			//Make sure the arena knows we've left
			_arena.playerLeave(this);

			//We should now be a spectator in spec!
			return true;
		}

		/// <summary>
		/// Unspecs the player to join another team
		/// </summary>
		public bool unspec(string team)
		{	//Find our team and redirect
			Team unspecTeam = _arena.getTeamByName(team);
			if (unspecTeam == null)
			{
				Log.write(TLog.Warning, "Invalid unspec team '{0}'!", team);
				return false;
			}

			return unspec(unspecTeam);
		}

		/// <summary>
		/// Unspecs the player to join another team
		/// </summary>
		public bool unspec(Team team)
		{	//Sanity checks
			if (_occupiedVehicle == null)
			{
				Log.write(TLog.Warning, "Attempted to unspectate with no spectator vehicle. {0}", this);
				return false;
			}
			
			//Make sure our vehicle is a spectator mode vehicle
			if (_occupiedVehicle._type.Type != VehInfo.Types.Spectator)
			{
				Log.write(TLog.Warning, "Attempted to unspectate with non-spectator vehicle. {0}", this);
				return false;
			}

			//Throw ourselves onto our new team!
            team.addPlayer(this);

			//Destroy our spectator vehicle
			_occupiedVehicle.destroy(true);
			_bSpectator = false;

            //Set relative vehicle if required, no need for any if statement here :]
            VehInfo vehicle = _server._assets.getVehicleByID(getDefaultVehicle().Id + _server._zoneConfig.teams[team._id].relativeVehicle);
            setDefaultVehicle(vehicle);

			//Run the exit spec event
			Logic_Assets.RunEvent(this, _server._zoneConfig.EventInfo.exitSpectatorMode);

			//Make sure the arena knows we've entered
			_arena.playerEnter(this);

            Bounty = _server._zoneConfig.bounty.start;

			return true;
		}

		/// <summary>
		/// Sends the player a warp request
		/// </summary>
		public void warp(Player warpTo)
		{	//Warp away!
			warp(Helpers.ResetFlags.ResetNone, -1, warpTo._state.positionX, warpTo._state.positionY);
		}

		/// <summary>
		/// Sends the player a warp request
		/// </summary>
		public void warp(int posX, int posY)
		{	//Warp away!
			warp(Helpers.ResetFlags.ResetNone, -1, (short)posX, (short)posY);
		}

		/// <summary>
		/// Sends the player a warp request
		/// </summary>
		public void warp(Helpers.ResetFlags flags, Helpers.ObjectState state, short radius, short energy, short invulnTime)
		{	//Do we need to apply a radius?
			if (radius == 0)
				warp(flags, energy, state.positionX, state.positionY, state.positionX, state.positionY, invulnTime);
			else
			{	//Calculate coordinates
				radius /= 2;
				warp(flags, energy,
					(short)(state.positionX - radius), (short)(state.positionY - radius),
					(short)(state.positionX + radius), (short)(state.positionY + radius), invulnTime);
			}
		}

		/// <summary>
		/// Sends the player a warp request
		/// </summary>
		public void warp(Helpers.ResetFlags flags, short energy, short posX, short posY)
		{	//Relay
			warp(flags, energy, posX, posY, posX, posY, (short)_server._zoneConfig.vehicle.warpDamageIgnoreTime);
		}

		/// <summary>
		/// Sends the player a warp request
		/// </summary>
		public void warp(Helpers.ResetFlags flags, short energy, short topX, short topY, short bottomX, short bottomY)
		{	//Relay
			warp(flags, energy, topX, topY, bottomX, bottomY, (short)_server._zoneConfig.vehicle.warpDamageIgnoreTime);
		}

		/// <summary>
		/// Sends the player a warp request
		/// </summary>
		public void warp(Helpers.ResetFlags flags, short energy, short topX, short topY, short bottomX, short bottomY, short invulnTime)
		{	//Approximate the player's new position
			_state.positionX = (short)(((topX - bottomX) / 2) + bottomX);
			_state.positionY = (short)(((topY - bottomY) / 2) + bottomY);

			//Prepare our packet
			SC_PlayerWarp warp = new SC_PlayerWarp();

			warp.warpFlags = flags;
			warp.energy = energy;
			warp.invulnTime = invulnTime;
			warp.topX = topX; warp.topY = topY;
			warp.bottomX = bottomX; warp.bottomY = bottomY;
			_client.sendReliable(warp);
		}

		/// <summary>
		/// Warps the player in place with a respawn warpmode
		/// </summary>
		public void resetWarp()
		{	//Make sure we're no longer waiting on death
			_deathTime = 0;

			//Prepare our packet
			SC_PlayerWarp warp = new SC_PlayerWarp();

			warp.warpFlags = Helpers.ResetFlags.ResetAll;
			warp.energy = -1;
			warp.invulnTime = 0;
			warp.topX = -1; warp.topY = -1;
			warp.bottomX = -1; warp.bottomY = -1;

			_client.sendReliable(warp);
		}
		#endregion

		#region Social
		/// <summary>
		/// Sends an arena message and logs it to disk
		/// </summary>
		public void sendNoticeLog(TLog priority, string message, params object[] formats)
		{	//Redirect
			sendNoticeLog(priority, String.Format(message, formats));
		}

		/// <summary>
		/// Sends an arena message and logs it to disk
		/// </summary>
		public void sendNoticeLog(TLog priority, string message)
		{	//Send the message to the player..
			Helpers.Social_ArenaChat(this, message, (byte)(priority > TLog.Normal ? 0xFF : 0));

			//Log it..
			Log.write(priority, "{0}: {1}", this, message);
		}

		/// <summary>
		/// Sends an arena message
		/// </summary>
		public void sendMessage(int bong, string message)
		{	//Senddit
			Helpers.Social_ArenaChat(this, message, bong);
		}

        /// <summary>
        /// Sends an arena message to the team
        /// </summary>
        public void sendTeamMessage(int bong, string message)
        {
            //Send it
            Helpers.Social_ArenaChat(this, message, bong); //To player
            Helpers.Social_ArenaChat(this._team.ActivePlayers, message, bong); //To team
        }

		/// <summary>
		/// Sends a new infoarea message
		/// </summary>
		public void triggerMessage(byte colour, int timer, string message)
		{	//Senddit
			Helpers.Social_TickerMessage(this, colour, timer, message);
		}

		/// <summary>
		/// Routes a private chat message to the player
		/// </summary>
		public void sendPlayerChat(Player from, CS_Chat chat)
		{
			Helpers.Player_RouteChat(this, from, chat);
		}

        /// <summary>
        /// Routes a private chat message to the player
        /// </summary>
        public void sendPlayerChat(Player from, SC_Whisper<Data.Database> chat)
        {
            Helpers.Player_RouteChat(this, from, chat);
        }
		#endregion

		#region Event Implementations
		/// <summary>
		/// Triggers and resets the leave arena event appropriately
		/// </summary>
		public void onLeaveArena()
		{	//Trigger the event
			if (LeaveArena != null)
			{
				LeaveArena(this);

				//TODO: Is this at all optimal?
				//Don't allow delegates to be called twice for the same arena
				foreach (Delegate d in LeaveArena.GetInvocationList())
					LeaveArena -= (Action<Player>)d;
			}
		}
		#endregion

		#region Database Related
		/// <summary>
		/// Creates a player instance object associated with the player
		/// </summary>
		public Data.PlayerInstance toInstance()
		{
			Data.PlayerInstance inst = new Data.PlayerInstance();

			inst.id = _id;
			inst.magic = _magic;

			return inst;
		}

		/// <summary>
		/// Transfers all stored information into a playerstats object 
		/// </summary>
		public Data.PlayerStats getStats()
		{	//Create a new object..
			Data.PlayerStats stats = new InfServer.Data.PlayerStats();

			//Which stats object should we be using?
			Data.PlayerStats sourceStats = StatsTotal;
			IEnumerable<InventoryItem> inv = _inventory.Values;
			IEnumerable<SkillItem> skills = _skills.Values;

			if (_suspStats != null)
			{
				sourceStats = _suspStats;
				inv = _suspInventory.Values;
				skills = _suspSkills.Values;
			}

			//Copy basic stats
			stats.zonestat1 = sourceStats.zonestat1;
			stats.zonestat2 = sourceStats.zonestat2;
			stats.zonestat3 = sourceStats.zonestat3;
			stats.zonestat4 = sourceStats.zonestat4;
			stats.zonestat5 = sourceStats.zonestat5;
			stats.zonestat6 = sourceStats.zonestat6;
			stats.zonestat7 = sourceStats.zonestat7;
			stats.zonestat8 = sourceStats.zonestat8;
			stats.zonestat9 = sourceStats.zonestat9;
			stats.zonestat10 = sourceStats.zonestat10;
			stats.zonestat11 = sourceStats.zonestat11;
			stats.zonestat12 = sourceStats.zonestat12;

			stats.kills = sourceStats.kills;
			stats.deaths = sourceStats.deaths;
			stats.killPoints = sourceStats.killPoints;
			stats.deathPoints = sourceStats.deathPoints;
			stats.assistPoints = sourceStats.assistPoints;
			stats.bonusPoints = sourceStats.bonusPoints;
			stats.vehicleKills = sourceStats.vehicleKills;
			stats.vehicleDeaths = sourceStats.vehicleDeaths;
			stats.playSeconds = sourceStats.playSeconds;
			
			//Convert our inventory
			stats.cash = sourceStats.cash;
			stats.inventory = new List<InfServer.Data.PlayerStats.InventoryStat>();

			foreach (InventoryItem ii in inv)
			{
				Data.PlayerStats.InventoryStat stat = new Data.PlayerStats.InventoryStat();

				stat.itemid = ii.item.id;
				stat.quantity = ii.quantity;

				stats.inventory.Add(stat);
			}

			//Convert our skills
			stats.experience = sourceStats.experience;
			stats.experienceTotal = sourceStats.experienceTotal;
			stats.skills = new List<InfServer.Data.PlayerStats.SkillStat>();

			foreach (SkillItem si in skills)
			{
				Data.PlayerStats.SkillStat stat = new Data.PlayerStats.SkillStat();

				stat.skillid = si.skill.SkillId;
				stat.quantity = si.quantity;

				stats.skills.Add(stat);
			}

			//All done!
			return stats;
		}

		/// <summary>
		/// Gives the player items and skill appropriate for a first time player
		/// </summary>
		public void assignFirstTimeStats(bool runEvents)
		{	//Create some new lists
			_inventory = new Dictionary<int, InventoryItem>();
			_skills = new Dictionary<int, SkillItem>();

			//No basic stats
			_stats = new InfServer.Data.PlayerStats();
			_statsSession = new Data.PlayerStats();
			_statsGame = null;
			_statsLastGame = null;

            //Sets checker for a first time setup
            //event. Changed to be called after the arena
            //is set up first.
            if (runEvents)
                firstTimePlayer = true;

                //Execute the first time setup events
//                firstTimeEvents();

			//Consider him loaded
			_bDBLoaded = true;
		}

        public void firstTimeEvents()
        {
            Logic_Assets.RunEvent(this, _server._zoneConfig.EventInfo.firstTimeSkillSetup);
            Logic_Assets.RunEvent(this, _server._zoneConfig.EventInfo.firstTimeInvSetup);
        }

		/// <summary>
		/// Uses the data stats structure to populate the player's statistics
		/// </summary>
		public void assignStats(Data.PlayerStats stats)
		{	//Create some new lists
			_inventory = new Dictionary<int,InventoryItem>();
			_skills = new Dictionary<int,SkillItem>();

			//Copy basic stats
			_stats = stats;
			_statsSession = new Data.PlayerStats();
			_statsGame = null;
			_statsLastGame = null;

			//Convert our inventory
			foreach (Data.PlayerStats.InventoryStat stat in stats.inventory)
			{	//Attempt to find the item
				ItemInfo item = _server._assets.getItemByID(stat.itemid);
				if (item == null)
				{	//Strange
					Log.write(TLog.Warning, "Encountered unknown item entry in player database stats (#{0})", stat.itemid);
					continue;
				}

				InventoryItem ii = new InventoryItem();

				ii.item = item;
				ii.quantity = (ushort)stat.quantity;

				_inventory.Add(stat.itemid, ii);
			}

			//Convert our skills
			foreach (Data.PlayerStats.SkillStat stat in stats.skills)
			{	//Attempt to find the item
				SkillInfo skill = _server._assets.getSkillByID(stat.skillid);
				if (skill == null)
				{	//Strange
					Log.write(TLog.Warning, "Encountered unknown skill entry in player database stats (#{0})", stat.skillid);
					continue;
				}

				SkillItem si = new SkillItem();

				si.skill = skill;
				si.quantity = (short)stat.quantity;

				_skills.Add(stat.skillid, si);
			}

			//Consider him loaded
			_bDBLoaded = true;
		}
		#endregion
	}
}
