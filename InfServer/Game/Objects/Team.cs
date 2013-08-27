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
    // Team Class
    /// Represents a single team in an arena
    ///////////////////////////////////////////////////////
    public class Team : CustomObject, IChatTarget
    {	// Member variables
        ///////////////////////////////////////////////////
        public Arena _arena;					//The arena we belong to
        public ZoneServer _server;				//The server we belong to

        private List<Player> _players;			//The list of players in this team

        public string _name;					//The name of the team
        public string _password;				//The password necessary to join the team
        public short _id;						//The ID of the team

        public Player _owner;                   //The owner of this team  (if private)

        public bool _isPrivate;					//Is this a private team?

        public int _currentGameKills;			//The amount of kills for the team this game
        public int _currentGameDeaths;			//The amoutn of deaths for the team this game

        public CfgInfo.TeamInfo _info;		    //The team-specific info
        public Dictionary<int, TeamInventoryItem> _tInventory;	//Our current inventory

        //Events
        public event Action<Team> Empty;	//Called when an team runs out of players and is empty

        public int _relativeVehicle;


        ///////////////////////////////////////////////////
        // Accessors
        ///////////////////////////////////////////////////
        /// <summary>
        /// Returns the number of unspecced players in the team
        /// </summary>
        public int ActivePlayerCount
        {
            get
            {
                return _players.Count(player => (!player.IsSpectator));
            }
        }

        /// <summary>
        /// Returns a list of the unspecced players in the team
        /// </summary>
        public IEnumerable<Player> ActivePlayers
        {
            get
            {
                return _players.Where(player => (!player.IsSpectator));
            }
        }

        /// <summary>
        /// Returns a list of specced and unspecced players on a team
        /// </summary>
        public IEnumerable<Player> AllPlayers
        {
            get
            {
                return _players;
            }
        }

        /// <summary>
        /// Returns whether the team is a public team
        /// </summary>
        public bool IsPublic
        {
            get
            {
                return !_isPrivate;
            }
        }

        /// <summary>
        /// Returns whether the team is a spectator team
        /// </summary>
        public bool IsSpec
        {
            get
            {
                return _name.Equals("spec", StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Returns whether the team is full
        /// </summary>
        public bool IsFull
        {
            get
            {
                return _players.Count(player => (!player.IsSpectator)) >= MaxPlayers;
            }
        }

        /// <summary>
        /// Returns the number of maximum players allowed on this team
        /// </summary>
        public int MaxPlayers
        {
            get
            {
                if (_isPrivate || (_id < _server._zoneConfig.arena.desiredFrequencies && _info.maxPlayers == 0))
                    return _server._zoneConfig.arena.maxPerFrequency;
                else
                    return _info.maxPlayers;
            }
        }

        /// <summary>
        /// Gives a short summary of this team
        /// </summary>
        public override string ToString()
        {	//Return the player credentials
            return String.Format("{0} ({1})", _name, _id);
        }

        ///////////////////////////////////////////////////
        // Member Classes
        ///////////////////////////////////////////////////
        #region Member Classes
        /// <summary>
        /// Represents a single element in the inventory
        /// </summary>
        public class TeamInventoryItem
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
        public Team(Arena owner, ZoneServer server)
        {
            _arena = owner;
            _server = server;

            _players = new List<Player>();
            _tInventory = new Dictionary<int, TeamInventoryItem>();
        }

        #region State
        /// <summary>
        /// Called when the team is empty of players 
        /// </summary>
        private void empty()
        {	//Let everyone know
            if (Empty != null)
                Empty(this);
        }

        /// <summary>
        /// Adds the given player to the team and notifies others
        /// </summary>
        public void addPlayer(Player player, bool manualJoin)
        {	//Sanity checks
            if (player._bIngame && player._team == this)
            {
                Log.write(TLog.Warning, "Attempted to add player to his own team. {0}", player);
                return;
            }

            //Have him lose his current team
            if (player._team != null)
                player._team.lostPlayer(player);

            //Set his new team!
            player._team = this;

            //Welcome him in
            _players.Add(player);

            //Do we need to notify, or is this an initial team?
            if (player._bIngame)
                //Let everyone know
                Helpers.Player_SetTeam(_arena.Players, player, this);

            player.sendMessage(0, "Team joined: " + _name);

            //Set relative vehicle if required, no need for any if statement here :]
            VehInfo vehicle = _server._assets.getVehicleByID(player.getDefaultVehicle().Id + _relativeVehicle);
            player.setDefaultVehicle(vehicle);

            //If he isn't a spectator, trigger the event too
            if (!player.IsSpectator)
            {
                if (manualJoin)
                    Logic_Assets.RunEvent(player, _server._zoneConfig.EventInfo.manualJoinTeam);
                else
                    Logic_Assets.RunEvent(player, _server._zoneConfig.EventInfo.joinTeam);
            }
        }

        public void addPlayer(Player player)
        {
            addPlayer(player, false);
        }

        /// <summary>
        /// Handles the loss of a player
        /// </summary>
        public void lostPlayer(Player player)
        {	//Sob, let him go
            _players.Remove(player);

            //Do we have any players left?
            if (_players.Count == 0)
                //Nope. It's closing time.
                empty();

            /*Disabling for now..
            if (_isPrivate && _owner != null)
            {
                //Owner is leaving, transfer to someone else randomly..
                if (player._alias == _owner._alias && _players.Count > 0)
                {
                    _owner = _players.FirstOrDefault();
                    _owner.sendMessage(0, String.Format("Ownership of {0} has been transfered to you", _name));
                }
            }*/

            //Reset any team-related state the player might have had
            _arena.flagResetPlayer(player);

            //We don't need to inform others of the loss of team,
            //as the player will either be leaving or joining another
            //team - both of which infer leaving the old team.
        }

        /// <summary>
        /// Modifies and updates the team's inventory
        /// </summary>
        public bool inventoryModify(ItemInfo item, int adjust)
        {
            //Do we already have such an item?
            TeamInventoryItem ii;
            _tInventory.TryGetValue(item.id, out ii);
            if (ii != null && adjust < 0)
            {
                //Trying to take away too many?? I dont understand why wouldn't just wrap to -ii.quantity 
                if (ii.quantity + adjust < 0) // but I'll keep this how it was
                    return false;

                //Will there be any items left?
                if (ii.quantity + adjust == 0)
                    _tInventory.Remove(item.id);
                else
                    ii.quantity = (ushort)(ii.quantity + adjust);

                return true;
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
                    _tInventory.Remove(item.id);
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
                ii = new TeamInventoryItem();

                ii.item = _server._assets.getItemByID(item.id);
                ii.quantity = (ushort)adjust;
                _tInventory.Add(item.id, ii);
            }

            return true;
        }

        /// <summary>
        /// Modifies and updates the team's inventory
        /// </summary>
        public bool inventoryModify(int itemid, int adjust)
        {	//Redirect
            if (itemid == 0)
            {
                Log.write(TLog.Warning, "Team.inventoryModify(): itemid is 0");
                return false;
            }
            else
            {
                return inventoryModify(_server._assets.getItemByID(itemid), adjust);
            }
        }

        /// <summary>
        /// Removes all items of a specific type from the player's inventory
        /// </summary>
        public void removeAllItemFromInventory(int itemID)
        {	//Attempt to remove it!
            removeAllItemFromTeamInventory(itemID);
        }

        /// <summary>
        /// Removes all items of a specific type from the player's inventory
        /// </summary>
        public void removeAllItemFromTeamInventory(int itemID)
        {	//Attempt to remove it!
            if (_tInventory.Remove(itemID))
            {
            }
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Retrives the inventory item count for the specified item type
        /// </summary>
        public int getInventoryAmount(int itemid)
        {	//Do we have such an item?
            TeamInventoryItem ii;

            if (!_tInventory.TryGetValue(itemid, out ii))
                return 0;
            return ii.quantity;
        }

        /// <summary>
        /// Retrives the inventory entry for the specified item type
        /// </summary>
        public TeamInventoryItem getInventory(int itemid)
        {	//Do we have such an item?
            TeamInventoryItem ii;

            if (!_tInventory.TryGetValue(itemid, out ii))
                return null;
            return ii;
        }

        /// <summary>
        /// Retrives the inventory entry for the specified item type
        /// </summary>
        public TeamInventoryItem getInventory(ItemInfo item)
        {	//Do we have such an item?
            TeamInventoryItem ii;

            if (!_tInventory.TryGetValue(item.id, out ii))
                return null;
            return ii;
        }
        #endregion

        #region Social
        /// <summary>
        /// Returns the list of players to for team chat (' or ")
        /// </summary>
        public IEnumerable<Player> getChatTargets()
        {
            return _players;
        }

        /// <summary>
        /// Triggered when a player has sent chat to his current team
        /// </summary>
        public void playerTeamChat(Player from, CS_Chat chat)
        {	//Route it to our entire player list!
            Helpers.Player_RouteChat(this, from, chat);
        }

        /// <summary>
        /// Triggered when a player has sent chat to an enemy team
        /// </summary>
        public void playerEnemyTeamChat(Player from, CS_Chat chat)
        {	//Route it to both the enemy team and our team
        }

        /// <summary>
        /// Sends an arena message to the entire arena
        /// </summary>
        public void sendArenaMessage(string message)
        {	//Relay the message
            Helpers.Social_ArenaChat(AllPlayers, message, 0);
        }

        /// <summary>
        /// Sends an arena message to the entire arena
        /// </summary>
        public void sendArenaMessage(string message, int bong)
        {	//Relay the message
            Helpers.Social_ArenaChat(AllPlayers, message, bong);
        }

        /// <summary>
        /// Sends a new infoarea message
        /// </summary>
        public void triggerMessage(byte colour, int timer, string message)
        {	//Relay the message
            Helpers.Arena_Message(AllPlayers, colour, timer, message, (Player)null);
        }

        /// <summary>
        /// Sends a new infoarea message
        /// </summary>
        public void triggerMessage(byte colour, int timer, string message, Player except)
        {	//Relay the message
            Helpers.Arena_Message(AllPlayers, colour, timer, message, except);
        }

        #endregion
    }
}