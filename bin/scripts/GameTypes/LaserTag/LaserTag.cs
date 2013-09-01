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

namespace InfServer.Script.GameType_LaserTag
{
    // Script Class
    /// Provides the interface between the script and arena
    ///////////////////////////////////////////////////////
    public class Script_LaserTag : Scripts.IScript
    {
        ///////////////////////////////////////////////////
        // Member Variables
        ///////////////////////////////////////////////////
        public Arena _arena;					//Pointer to our arena class
        private CfgInfo _config;				//The zone config

        public static Dictionary<Team, HQ> _hqs = new Dictionary<Team, HQ>();       //Our list of HQs

        private int tickLastUpdate;                 //The game's jackpot so far

        private int _jackpot;
        private int _tickVictoryStart;			    //The tick at which the victory countdown began
        private int _tickNextVictoryNotice;		    //The tick at which we will next indicate imminent victory
        private int _victoryNotice;				    //The number of victory notices we've done

        private int _lastGameCheck;				    //The tick at which we last checked for game viability
        private int _tickGameStarting;			    //The tick at which the game began starting (0 == not initiated)
        private int _tickGameStart;
        private int _lastFlagCheck;                 //The tick at which a resource prize has been given
        private int _tickLastRepair;                //The tick at which the pilot has auto repaired a vechicle
        private int _lastTickerUpdate;              //The tick at which we last updated the tickers
        private const int _tickerUpdateRate = 800;  //Rate at which we update ticker in milliseconds

        //The teams
        private arenaTeam teamOne;
        private arenaTeam teamTwo;

        //Resources
        private const int TITOX_ID = 2009;
        private const int HYDROCARB_ID = 2002;
        private const int ORE_ID = 2004;
        private const int RESOURCE_CHANGE = 1;

        //Settings
        private int _minPlayers;				//The minimum amount of players

        ///////////////////////////////////////////////////
        // Headquarter Configuration
        ///////////////////////////////////////////////////

        //Timing
        private int rewardDelay = 90;                  //Periodic reward delay (in seconds)

        //Rewards
        public static int baseCash = 150;                   //Base cash reward
        public static int baseExp = 80;                     //Base experience reward
        public static int basePoints = 175;                 //Base point reward
        public static int baseBountyPerKill = 25;           //Bounty rewarded per kill for HQ
        public static double vehicleKillMultiply = 1.25;    //Bounty multiplier for vehicle kills

        public static double cashMultiplier = 2;            //Multipliers for rewarding those who destroy a HQ
        public static double expMultipler = 1.50;
        public static double pointMultiplier = 2.25;

        //Leveling
        public static double baseMultiplier = 1.75;         //Base Bounty multipler for scaling Bounty required to level an HQ.
        public static int baseBounty = 500;                 //Base Bounty required to level an HQ.
        public static int levelHump = 10000;                //Base amount of bounty added to certain level humps

        //Other
        public static int killRadius = 3000;                //Radius in which kills count for HQ bounty.
        public static int killRadiusPylon = 1500;           //Radius in which kills count for HQ bounty (Pylon Extension)
        public static int doubleXP = 1;                     //Double xp Modifier, 1 = false 2 = true.

        //////////////////////////////
        //Member Classes
        //////////////////////////////
        #region Member Classes
        /// <summary>
        /// ...
        /// </summary>
        public class arenaTeam
        {
            public Team team;
            public int hqVehId;

            public arenaTeam(Team t, int id)
            {
                team = t;
                hqVehId = id;
            }

            //Returns the total amount of resources a team owns
            public int getTotalResources()
            {
                //Accumulator for resources
                int totalResources = 0;

                //Get the current quantity of resources
                int totalTitox = team.getInventoryAmount(TITOX_ID);
                int totalHydrocarb = team.getInventoryAmount(HYDROCARB_ID);
                int totalOre = team.getInventoryAmount(ORE_ID);

                totalResources = totalTitox + totalHydrocarb + totalOre;

                return totalResources;
            }

            //A quick method to remove all types of resources on the team
            public void removeAllResources()
            {
                //Remove all titanium oxide
                team.removeAllItemFromTeamInventory(TITOX_ID);
                //Remove all hydrocarbon
                team.removeAllItemFromTeamInventory(HYDROCARB_ID);
                //Remove all ore
                team.removeAllItemFromTeamInventory(ORE_ID);
            }
        }
        #endregion

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

            //Teams
            teamOne = new arenaTeam(_arena.getTeamByID(0), 403);
            teamTwo = new arenaTeam(_arena.getTeamByID(1), 422);

            foreach (Arena.FlagState fs in _arena._flags.Values)
            {
                if (fs.flag.FlagData.MinPlayerCount < _minPlayers)
                {
                    _minPlayers = fs.flag.FlagData.MinPlayerCount;
                }

                //Register our flag change events
                fs.TeamChange += onFlagChange;

                if (_minPlayers == Int32.MaxValue)
                {
                    //No flags? Run blank games
                    _minPlayers = 1;
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

            //Update our tickers
            if (_tickGameStart > 0 && now - _arena._tickGameStarted > 2000)
            {
                if (now - _lastTickerUpdate > _tickerUpdateRate)
                {
                    updateTickers();
                    _lastTickerUpdate = now;
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
                    {
                        //Make sure any HQ's spawned are cleaned up before real game start
                        _hqs.Clear();
                        //Trigger the game start
                        _arena.gameStart();
                    }
                );
            }
            //Game is in progress
            else
            {
                /////////////////////////////////////
                //      Flag Resource Capture      //
                /////////////////////////////////////
                int flagDelay = 500; //In miliseconds

                //If enough time has passed...
                if (now - _lastFlagCheck >= flagDelay)
                {
                    //Loop through every flag in the arena...
                    foreach (Arena.FlagState fs in _arena._flags.Values)
                    {
                        //Ore Generator is captured...
                        if (fs.flag == AssetManager.Manager.getLioByID(101) &&
                           fs.team != null)
                            //Add resources to the team that captured it
                            fs.team.inventoryModify(ORE_ID, RESOURCE_CHANGE);

                        //Hydrocarbon Generator is captured...
                        else if (fs.flag == AssetManager.Manager.getLioByID(103) &&
                                 fs.team != null)
                            //Add resources to the team that captured it
                            fs.team.inventoryModify(HYDROCARB_ID, RESOURCE_CHANGE);
                    }
                    _lastFlagCheck = now;
                }
            }

            //TODO: Refactor this... wrong way of maintainings reward and levels
            foreach (HQ hq in _hqs.Values)
            {
                //Reward time?
                if ((now - tickLastUpdate) > (rewardDelay * 1000))
                {
                    Events.periodicReward(hq);
                    //Last HQ in line
                    if (hq == _hqs.Last().Value)
                        tickLastUpdate = now;
                }
                //Level up time?
                if ((now - tickLastUpdate) > 1000)
                    if (hq.bounty >= hq.nextLvl)
                        Events.onHQLevelUp(hq);
            }

            ////////////////////////////////////////
            //         Pilot Auto Repair          //
            ////////////////////////////////////////
            int repairDelay = 1000; //in miliseconds

            //If enough time has passed...
            if (now - _tickLastRepair > repairDelay)
            {
                //Loop through every player in the arena
                foreach (Player player in _arena.Players)
                {
                    if (player._occupiedVehicle != null && //Inside a vehicle and...
                        player._baseVehicle._type.Id == 116 || player._baseVehicle._type.Id == 216) //of the pilot class (id = 116/216)
                    {
                        //Amount to repair
                        int repairAmount = 1;

                        //Apply repair
                        player.inventoryModify(AssetManager.Manager.getItemByName("PlusHealth"), repairAmount);
                    }
                }
                _tickLastRepair = now;
            }

            return true;
        }


        #region Events
        /// <summary>
        /// Called when a flag changes team
        /// </summary>
        public void onFlagChange(Arena.FlagState flag)
        {
            //Let the arena know
            //_arena.sendArenaMessage(flag.team._name + " has captured " + flag.flag.GeneralData.Name + "!", 0);
        }

        //*** Vehicle Manipulation ***//
        //Orientates a vehicle's current state to that of a player
        public void orientateTo(Vehicle vehicle, Player player)
        {
            vehicle._state.positionX = player._state.positionX;
            vehicle._state.positionY = player._state.positionY;
            vehicle._state.yaw = player._state.yaw;
        }

        /// <summary>
        /// Called when the specified team has won
        /// </summary>
        /// <param name="victors"></param>
        public void gameVictory(Team victors)
        {
            //Send Victory message
            _arena.sendArenaMessage("Game Over!", 0);

            //Clear all tickers (1,2)
            for (int i = 1; i <= 2; i++)
            {
                _arena.setTicker(1, i, 0, "");
            }

            //Let everyone know
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

        public void updateTickers()
        {
            string format;
            if (_arena.ActiveTeams.Count() > 1)
            {
                format = String.Format("{0}={1} - {2}={3}",
                    _arena.ActiveTeams.ElementAt(0)._name,
                    _arena.ActiveTeams.ElementAt(0)._currentGameKills,
                    _arena.ActiveTeams.ElementAt(1)._name,
                    _arena.ActiveTeams.ElementAt(1)._currentGameKills);
                _arena.setTicker(1, 0, 0, format);
            }

            string updateOne = String.Format("Resources: {0} : {1} - {2} : {3}",
                teamOne.team._name, teamOne.getTotalResources(), teamTwo.team._name, teamTwo.getTotalResources());

            _arena.setTicker(1, 1, 0, updateOne);
        }

        /// <summary>
        /// Called when a player sends a chat command
        /// </summary>
        [Scripts.Event("Player.ChatCommand")]
        public bool playerChatCommand(Player player, Player recipient, string command, string payload)
        {

            if (command.ToLower() == "hqlist")
            {
                foreach (HQ hq in _hqs.Values)
                {
                    player.sendMessage(0,
                        String.Format("[HQ] (Team={0} Bounty={1} Location={2})",
                        hq.team._name,
                        hq.bounty,
                        Helpers.posToLetterCoord(hq.vehicle._state.positionX, hq.vehicle._state.positionY)));
                }
            }

            else if (command.ToLower() == "hq")
            {
                if (!_hqs.Keys.Contains(player._team))
                {
                    player.sendMessage(-1, "No Headquarters");
                    return false;
                }

                HQ hq = _hqs[player._team];

                player.sendMessage(0, "~[HQ] - Information!");
                player.sendMessage(0, String.Format("[HQ] - Level: {0}", hq.level));
                player.sendMessage(0, String.Format("[HQ] - Next Level: {0}", hq.nextLvl));
                player.sendMessage(0, String.Format("[HQ] - Total Bounty: {0}", hq.bounty));
                player.sendMessage(0, String.Format("[HQ] - Location: {0}",
                    Helpers.posToLetterCoord(hq.vehicle._state.positionX, hq.vehicle._state.positionY)));


            }

            else if (command.ToLower() == "bounty")
            {
                if (player.PermissionLevel > 0)
                {
                    if (_hqs.ContainsKey(player._team))
                    {
                        _hqs[player._team].bounty += Int32.Parse(payload);
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Called when a player enters the game
        /// </summary>
        [Scripts.Event("Player.Enter")]
        public bool playerEnter(Player player)
        {
            return true;
        }

        /// <summary>
        /// Called when a player leaves the game
        /// </summary>
        [Scripts.Event("Player.Leave")]
        public bool playerLeave(Player player)
        {
            return true;
        }

        /// <summary>
        /// Called when the game begins
        /// </summary>
        [Scripts.Event("Game.Start")]
        public bool gameStart()
        {
            //We've started!
            _tickGameStart = Environment.TickCount;
            _tickGameStarting = 0;

            //Spawn our flags!
            _arena.flagSpawn();

            //Let everyone know
            _arena.sendArenaMessage("Game has started!", _config.flag.resetBong);
            _arena.sendArenaMessage("Destroy the Enemy HeadQuarters at all costs!", _config.flag.resetBong);

            updateTickers();

            return true;
        }

        /// <summary>
        /// Called when the game ends
        /// </summary>
        [Scripts.Event("Game.End")]
        public bool gameEnd()
        {
            //TODO: 
            //1. Fix the game start timer shit
            //2. Make sure you add cases for deleting hqs at game end and when game resets and when game stops
            _tickGameStart = 0;
            _tickGameStarting = 0;
            _tickVictoryStart = 0;
            _tickNextVictoryNotice = 0;

            _lastFlagCheck = 0;
            _tickLastRepair = 0;
            _lastTickerUpdate = 0;

            //Clean up all HQ's
            _hqs.Clear();

            //Reset team inventories
            teamOne.removeAllResources();
            teamTwo.removeAllResources();

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
        {
            //Game reset, perhaps start a new one
            _tickGameStart = 0;
            _tickGameStarting = 0;
            _tickVictoryStart = 0;
            _tickNextVictoryNotice = 0;

            _lastFlagCheck = 0;
            _tickLastRepair = 0;
            _lastTickerUpdate = 0;

            //Reset team inventories
            teamOne.removeAllResources();
            teamTwo.removeAllResources();

            //_hqs.Clear();

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
        /* 
         /// <summary>
         /// Triggered when a player requests to drop an item
         /// </summary>
         [Scripts.Event("Player.ItemDrop")]
         public bool playerItemDrop(Player player, ItemInfo item, ushort quantity)
         {
             return true;
         }
         */
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
            //When Titan Generator is used...
            if (computer._type.Name == "Titan Headquarter" &&
                player._team == computer._team)
            {
                //Refine Titanium Oxide selected...
                if (product.Title.StartsWith("Refine Titanium Oxide"))
                {
                    //Amount of resource player is carrying
                    int playerItemAmount = player.getInventoryAmount(product.PlayerItemNeededId);

                    //Add resource to team's resource pool (Titanium Oxide id = 2009)
                    player._team.inventoryModify(TITOX_ID, playerItemAmount);
                }

                //Refine Hydrocarbon selected...
                if (product.Title.StartsWith("Refine Hydrocarbon"))
                {
                    //Amount of resource player is carrying
                    int playerItemAmount = player.getInventoryAmount(product.PlayerItemNeededId);

                    //Add resource to team's resource pool (Hydrocarbon id == 2002)
                    player._team.inventoryModify(HYDROCARB_ID, playerItemAmount);
                }

                //Refine Ore selected...
                if (product.Title.StartsWith("Refine Ore"))
                {
                    //Amount of resource player is carrying
                    int playerItemAmount = player.getInventoryAmount(product.PlayerItemNeededId);

                    //Add resource to team's resource pool (Ore id == 2004)
                    player._team.inventoryModify(ORE_ID, playerItemAmount);
                }
            }

            //When Collective generator is used...
            if (computer._type.Name == "Collective Headquarter" &&
                player._team == computer._team)
            {
                //Refine Titanium Oxide selected...
                if (product.Title.StartsWith("Refine Titanium Oxide"))
                {
                    //Amount of resource player is carrying
                    int playerItemAmount = player.getInventoryAmount(product.PlayerItemNeededId);

                    //Add resource to team's resource pool (Titanium Oxide id == 2002)
                    player._team.inventoryModify(TITOX_ID, playerItemAmount);
                }

                //Refine Hydrocarbon selected...
                if (product.Title.StartsWith("Refine Hydrocarbon"))
                {
                    //Amount of resource player is carrying
                    int playerItemAmount = player.getInventoryAmount(product.PlayerItemNeededId);

                    //Add resource to team's resource pool (Hydrocarbon id == 2002)
                    player._team.inventoryModify(HYDROCARB_ID, playerItemAmount);
                }

                //Refine Ore selected...
                if (product.Title.StartsWith("Refine Ore"))
                {
                    //Amount of resource player is carrying
                    int playerItemAmount = player.getInventoryAmount(product.PlayerItemNeededId);

                    //Add resource to team's resource pool (Ore id == 2002)
                    player._team.inventoryModify(ORE_ID, playerItemAmount);
                }
            }

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
            /*
            if(player._baseVehicle._type.Id == 125)
            {
                while (vehicle._state.health < vehicle._type.Hitpoints)
                {
                    vehicle._state.health++;
                    vehicle.update(true);
                }
            }
            */
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
            //1224 = flip proj. 307 = titan dart. 308 = collective dart
            if (weapon.id == 1224 && player._occupiedVehicle != null)
            {
                //Stores the id of the vehicle the player is in
                ushort occupiedVehicleId = (ushort)player._occupiedVehicle._type.Id;

                //Make a new vehicle of the same type as the one the player is in
                Vehicle skin = _arena.newVehicle(occupiedVehicleId);

                //Orientate the new vehicle to the player's positional state
                orientateTo(skin, player);

                //Flip the vehicle
                skin._state.yaw += 45;

                //Make a note of old vehicle's health
                player.setVar("healthDecay", player._occupiedVehicle._type.Hitpoints - player._occupiedVehicle._state.health);

                //Destroy current occupied vehicle
                player._occupiedVehicle.destroy(true);

                //Enter the new vehicle
                skin.playerEnter(player);

                //Reapply old health
                player.inventoryModify(AssetManager.Manager.getItemByName("MinusHealth"), player.getVarInt("healthDecay"));
            }

            //If the player is in a titan droid (id = 324) and morph weapon used
            if (weapon.id == 1179 && player._occupiedVehicle._type.Id == 324)
            {
                //Make new vehicle (titan droid shielded) id=315
                Vehicle skin = _arena.newVehicle(315);

                //Orientate the vehicle to the player's positional state
                orientateTo(skin, player);

                //Make a note of his health (because when you change him to another vehicle, he inherits the max hp of the new vehicle)
                player.setVar("healthDecay", player._occupiedVehicle._type.Hitpoints - player._occupiedVehicle._state.health);

                //Destroy current occupied vehicle
                player._occupiedVehicle.destroy(true);

                //Enter new vehicle (titan shielded droid)
                skin.playerEnter(player);

                //Reapply health loss
                player.inventoryModify(AssetManager.Manager.getItemByName("MinusHealth"), player.getVarInt("healthDecay"));
            }
            //If the player is in a morphed droid (id = 315) and unmorph weapon used
            else if (weapon.id == 1181 && player._occupiedVehicle._type.Id == 315)
            {
                //Return him to this world!

                //Make new vehicle (titan droid)
                Vehicle skin = _arena.newVehicle(324);

                //Orientate the vehicle to the player's positional state
                orientateTo(skin, player);

                //Make note of vehicle's health lost
                player.setVar("healthDecay", player._occupiedVehicle._type.Hitpoints - player._occupiedVehicle._state.health);

                //Destroy current occupied vehicle
                player._occupiedVehicle.destroy(true);

                //Enter new vehicle (original titan droid)
                skin.playerEnter(player);

                //Reapply health loss
                player.inventoryModify(AssetManager.Manager.getItemByName("MinusHealth"), player.getVarInt("healthDecay"));
            }

            //If the vehicle is a collective droid (id = 311) and morph weapon used
            else if (weapon.id == 1179 && player._occupiedVehicle._type.Id == 311)
            {
                //Make new vehicle (collective droid shielded) with player's position id=302
                Vehicle skin = _arena.newVehicle(302);

                //Orientate the vehicle to the player's positional state
                orientateTo(skin, player);

                //Make note of vehicle's health lost
                player.setVar("healthDecay", player._occupiedVehicle._type.Hitpoints - player._occupiedVehicle._state.health);

                //Destroy current occupied vehicle
                player._occupiedVehicle.destroy(true);

                //Enter new vehicle (collective droid shielded)
                skin.playerEnter(player);

                //Reapply health loss
                player.inventoryModify(AssetManager.Manager.getItemByName("MinusHealth"), player.getVarInt("healthDecay"));
            }
            //If the vehicle is a morphed droid (id = 302) and unmorphed weapon used
            else if (weapon.id == 1179 && player._occupiedVehicle._type.Id == 302)
            {
                //Make new vehicle (collective droid) with player's position
                Vehicle skin = _arena.newVehicle(311);

                //Orientate the vehicle to the player's positional state
                orientateTo(skin, player);

                //Make note of vehicle's health lost
                player.setVar("healthDecay", player._occupiedVehicle._type.Hitpoints - player._occupiedVehicle._state.health);

                //Destroy current occupied vehicle
                player._occupiedVehicle.destroy(true);

                //Enter new vehicle (collective droid)
                skin.playerEnter(player);

                //Reapply health loss
                player.inventoryModify(AssetManager.Manager.getItemByName("MinusHealth"), player.getVarInt("healthDecay"));
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
            //Calculate rewards AGAIN if double exp
            if (doubleXP == 2 && killer != null)
            {
                Logic_Rewards.calculatePlayerKillRewards(victim, killer, update);
            }
            return true;
        }

        /// <summary>
        /// Triggered when one player has killed another
        /// </summary>
        [Scripts.Event("Player.PlayerKill")]
        public bool playerPlayerKill(Player victim, Player killer)
        {
            //Flag Kill double bty test...

            foreach (Arena.FlagState fs in _arena._flags.Values)
            {
                if (fs.carrier == killer)
                {
                    //Double killer's bty
                    killer._bounty *= 2;
                }
            }

            if (_hqs.Keys.Contains(killer._team))
            {
                HQ headq = _hqs[killer._team];

                List<Vehicle> inRange = _arena.getVehiclesInRange(
                    killer._state.positionX, killer._state.positionY,
                    killRadius);
                //Blasphemy!
                if (killer._team == victim._team)
                    return false;

                //Is it in range?
                if (inRange.Contains(headq.vehicle))
                {
                    Events.onPlayerKill(headq, killer, victim);
                    return true;
                }
                //Pylon?
                if (headq.pylon != null && inRange.Contains(headq.pylon))
                {
                    Events.onPlayerKill(headq, killer, victim);
                    return true;
                }
            }

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
            if (_hqs.Keys.Contains(computer._team))
            {
                //Smaller range for Computers
                List<Vehicle> hqsInRange = _arena.getVehiclesInRange(computer._state.positionX, computer._state.positionY, 1000);

                //Is it in range?
                if (hqsInRange.Contains(_hqs[computer._team].vehicle))
                {
                    HQ headq = _hqs[computer._team];
                    Events.onComputerKill(headq, computer, victim);
                }
            }
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
            //If either team's headquarter is created...
            if ((created._type.Id == teamOne.hqVehId) || (created._type.Id == teamTwo.hqVehId))
            {
                //Already have one?
                if (_hqs.Keys.Contains(team))
                {
                    //Destroy it
                    created.destroy(false, true);
                    return false;
                }
                //No
                else
                {
                    //Create it
                    HQ newHQ = new HQ(created);
                    _hqs.Add(team, newHQ);
                }
            }
            return true;
        }

        /// <summary>
        /// Triggered when a vehicle dies
        /// </summary>
        [Scripts.Event("Vehicle.Death")]
        public bool vehicleDeath(Vehicle dead, Player killer)
        {
            //If either team's headquarter dies...
            if (dead._type.Id == teamOne.hqVehId || dead._type.Id == teamTwo.hqVehId)
            {
                //Which team's hq?
                if (_hqs.Keys.Contains(dead._team))
                {
                    HQ headQ = _hqs[dead._team];

                    //If the killer isn't on the same team as the destroyed HQ
                    if (headQ.team != killer._team)
                    {
                        //Carry on with HQ death event
                        Events.onHQDeath(headQ, killer);
                        //Winner!
                        gameVictory(killer._team);
                    }
                    //Otherwise it's team kill
                    else
                    {
                        _arena.sendArenaMessage(
                            String.Format("~[HQ] - Oops! - Team {0} has destroyed their own headquarters!", headQ.team._name));

                        //Opposite team wins!
                        if (headQ.team == teamOne.team)
                            gameVictory(teamTwo.team);
                        else
                            gameVictory(teamOne.team);
                    }

                    _hqs.Remove(dead._team);
                }
            }
            //Pylon?
            else if (dead._type.Id == 480)
            {
                if (_hqs.Keys.Contains(dead._team))
                {
                    HQ headQ = _hqs[dead._team];
                    headQ.pylon = null;
                }
            }

            //Car?
            if (dead._type.Type == VehInfo.Types.Car)
            {
                /*[3:05:51 PM]* Exception whilst polling arena Public1:
System.NullReferenceException: Object reference not set to an instance of an object.
   at InfServer.Script.GameType_HQ.Script_HQ.vehicleDeath(Vehicle dead, Player killer) in c:\Infantry\Zones\Combined Arms\scripts\GameTypes\HQ\Headquarters.cs:line 518
                added if(killer != null)
                 */
                try
                {
                    if (killer != null && _hqs != null)
                    {
                        if (_hqs.Keys.Contains(killer._team))
                        {
                            HQ headq = _hqs[killer._team];

                            List<Vehicle> inRange = _arena.getVehiclesInRange(
                                killer._state.positionX, killer._state.positionY,
                                killRadius);
                            //Blasphemy!
                            if (killer._team == dead._team)
                                return false;

                            //Is it in range?
                            if (inRange.Contains(headq.vehicle))
                            {
                                Events.onVehicleKill(headq, killer, dead);
                                return true;
                            }
                            //Pylon?
                            if (headq.pylon != null && inRange.Contains(headq.pylon))
                            {
                                Events.onVehicleKill(headq, killer, dead);
                                return true;
                            }

                        }
                    }
                }
                catch (Exception e)
                {
                    Log.write(TLog.Warning, "hq or killer do not exist" + e);
                }
            }
            return true;
        }
    }
}
        #endregion
