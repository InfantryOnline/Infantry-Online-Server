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

namespace InfServer.Script.GameType_Name
{	// Script Class
    /// Provides the interface between the script and arena
    ///////////////////////////////////////////////////////
    class Script_Name : Scripts.IScript
    {
        public struct Teams
        {
            public Team arenaTeam;
            public Arena.FlagState flag;
            public int points;
            public int flagId;
            public int portalId;
            public int resources;
        }

        ///////////////////////////////////////////////////
        // Member Variables
        ///////////////////////////////////////////////////
        private Arena _arena;					//Pointer to our arena class
        private CfgInfo _config;				//The zone config

        private int _jackpot;					//The game's jackpot so far

        private Team _victoryTeam;				//The team currently winning!
        private int _tickVictoryStart;			//The tick at which the victory countdown began
        private int _tickNextVictoryNotice;		//The tick at which we will next indicate imminent victory
        private int _victoryNotice;				//The number of victory notices we've done

        private int _lastGameCheck;				//The tick at which we last checked for game viability
        private int _tickGameStarting;			//The tick at which the game began starting (0 == not initiated)
        private int _tickGameStart;				//The tick at which the game started (0 == stopped)

        //Settings
        private int _minPlayers;				//The minimum amount of players

        //Teams
        Teams teamOne;
        Teams teamTwo;

        //Points needed to win
        public const int MAX_POINTS = 3;

        //Resource multiplier
        public const int RESOURCE_MULT = 150;

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

            //The flag's ID that corresponds to whichever team.
            teamOne.flagId = 102;
            teamOne.flagId = 101;

            //Assign each team's portal (goal box)
            teamOne.portalId = 133;
            teamTwo.portalId = 134;

            //Assign the teams
            teamOne.arenaTeam = _arena.getTeamByID(0);
            teamTwo.arenaTeam = _arena.getTeamByID(1);

            //Assign each team names
            teamOne.arenaTeam._name = _config.teams[0].name;
            teamTwo.arenaTeam._name = _config.teams[1].name;

            //Assign each team's flag
            teamOne.flag = _arena._flags.Values.Where(f => f.flag.GeneralData.Id == 102).First();
            teamTwo.flag = _arena._flags.Values.Where(f => f.flag.GeneralData.Id == 101).First();

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

            //Has any team scored MAX_POINTS more than the other team?
            if (teamOne.points - teamTwo.points >= MAX_POINTS)
            {
                _arena.sendArenaMessage(teamOne.arenaTeam._name + " has won!", 0);
                gameVictory(teamOne.arenaTeam);
            }
            if (teamTwo.points - teamOne.points >= MAX_POINTS)
            {
                _arena.sendArenaMessage(teamTwo.arenaTeam._name + " has won!", 0);
                gameVictory(teamTwo.arenaTeam);
            }

            return true;
        }

        #region Events
        /// <summary>
        /// Called when a flag changes team
        /// </summary>
        public void onFlagChange(Arena.FlagState flag)
        {
        }

        //Orientates a vehicle's current state to that of a player
        public void orientateTo(Vehicle vehicle, Player player)
        {
            vehicle._state.positionX = player._state.positionX;
            vehicle._state.positionY = player._state.positionY;
            vehicle._state.yaw = player._state.yaw;
        }

        public void updateTickers()
        {
            string updateOne = String.Format("{0}'s points: {1} - {2}'s points: {3}",
                teamOne.arenaTeam._name, teamOne.points, teamTwo.arenaTeam._name, teamTwo.points);
            _arena.setTicker(1, 1, 0, updateOne);

            string updateTwo = String.Format("{0}'s resources: {1} - {2}'s resources: {3}",
                teamOne.arenaTeam._name, teamOne.resources, teamTwo.arenaTeam._name, teamTwo.resources);
            _arena.setTicker(1, 2, 0, updateTwo);
        }

        /// <summary>
        /// Called when the specified team have won
        /// </summary>
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

        /// <summary>
        /// Called when a player sends a chat command
        /// </summary>
        [Scripts.Event("Player.ChatCommand")]
        public bool playerChatCommand(Player player, Player recipient, string command, string payload)
        {
            if (command.ToLower() == "test")
            {
                player.sendMessage(0, "Test");
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
            //ScriptHelpers.scrambleTeams(_arena, 2, true);

            //Resources each team begins with
            teamOne.resources = (teamOne.arenaTeam.ActivePlayerCount) * RESOURCE_MULT;
            teamTwo.resources = (teamTwo.arenaTeam.ActivePlayerCount) * RESOURCE_MULT;

            //Points each team begins with
            teamOne.points = 0;
            teamTwo.points = 0;

            //Spawn our flags!
            _arena.flagSpawn();

            //Let everyone know
            _arena.sendArenaMessage("Game has started!", _config.flag.resetBong);
            _arena.sendArenaMessage("Score " + MAX_POINTS + " more points than your opponents to win!", _config.flag.resetBong);
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
            _tickVictoryStart = 0;
            _tickNextVictoryNotice = 0;
            _victoryTeam = null;

            //Reset points to avoid game end spam
            teamOne.points = 0;
            teamTwo.points = 0;

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

            _victoryTeam = null;

            //Reset points
            teamOne.points = 0;
            teamTwo.points = 0;

            //Reset resources
            teamOne.resources = teamOne.arenaTeam.ActivePlayerCount;
            teamTwo.resources = teamTwo.arenaTeam.ActivePlayerCount;

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
            List<Arena.FlagState> carried = _arena._flags.Values.Where(flag => flag.carrier == player).ToList();

            foreach (Arena.FlagState carry in carried)
            {   //If the terrain number is 0-15

                int terrainNum = player._arena.getTerrainID(player._state.positionX, player._state.positionY);
                if (terrainNum >= 0 && terrainNum <= 15)
                {
                    //Check the FlagDroppableTerrains for that specific terrain id
                    //Carried flag resets to its original position if on default bad terrain (0) 
                    if ((carry.flag.FlagData.FlagDroppableTerrains[terrainNum] == 0))
                    {
                        _arena.flagResetPlayer(player);
                    }
                }
            }

            //*************************************************//
            //              SCORING MECHANISM                  //
            //*************************************************//

            //TODO: Add mvp checking
            if (player._team == teamOne.arenaTeam && //player is on team one
                portal == player._server._assets.getLioByID(teamOne.portalId) && //player is on team one's portal (his own)
                player == teamTwo.flag.carrier) //player is carrying enemy's flag
            {
                //Let the arena know
                _arena.sendArenaMessage(teamOne.arenaTeam._name + " has captured a flag!", 0);

                //Add one point to the team
                teamOne.points++;

                //Update tickers
                updateTickers();

                //Reset the flag to the original position
                _arena.flagResetPlayer(player);
                return false;
            }

            if (player._team == teamOne.arenaTeam && //player is on team one
                     portal == player._server._assets.getLioByID(teamOne.portalId) && //player is on team one's portal (his own)
                     player == teamOne.flag.carrier) //player is carrying his own TEAM'S flag
            {
                //Let arena know
                _arena.sendArenaMessage(player + " has returned a flag!", 0);

                //Reset the flag
                _arena.flagResetPlayer(player);
                return false;
            }

            if (player._team == teamTwo.arenaTeam && //player is on team two
                portal == player._server._assets.getLioByID(teamTwo.portalId) && //player is on team two's portal (his own)
                player == teamOne.flag.carrier) //player is carrying enemy's flag
            {
                //Let arena know
                _arena.sendArenaMessage(teamTwo.arenaTeam._name + " has captured a flag!", 0);

                //Add one point to the team
                teamTwo.points++;

                //Update tickers
                updateTickers();

                //Reset the flag to the original position
                _arena.flagResetPlayer(player);
                return false;
            }

            if (player._team == teamTwo.arenaTeam && //player is on team two
                     portal == player._server._assets.getLioByID(teamTwo.portalId) && //player is on team two's portal (his own)
                     player == teamTwo.flag.carrier) //player is carrying his own TEAM'S flag
            {
                //Let arena know
                _arena.sendArenaMessage(player + " has returned a flag!", 0);

                //Reset the flag
                _arena.flagResetPlayer(player);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Handles a player's produce request
        /// </summary>
        [Scripts.Event("Player.Produce")]
        public bool playerProduce(Player player, Computer computer, VehInfo.Computer.ComputerProduct product)
        {
            //Makes sure player is using the right generator and resources are subtracted from the right team

            if (computer._type.Name == "Titan Generator" && //using titan generator
               player._team == computer._team) //player and generator are on the same team 
            //(might have to do == teamOne.arenaTeam instead. Since generators don't have a frquency)
            {
                //If not enough resources...
                if (teamOne.resources < product.Cost)
                {
                    player.sendMessage(0, "Your team does not have enough resources to produce this!");
                    //Don't produce it
                    return false;
                }
                //But if we do have enough resources...
                else
                {
                    //Make it and subtract from team's resource total
                    teamOne.resources -= product.Cost;
                }

                //Accepts titanium oxide to add to team resources
                if (product.Title.StartsWith("Refine Titanium Oxide"))
                {
                    //How much titanium oxide does the player have?
                    int playerItemAmount = player.getInventoryAmount(product.PlayerItemNeededId);

                    //Remove all of the player's titanium oxide from his inventory (auto does that from editor)
                    //player.inventoryModify(product.PlayerItemNeededId, -playerItemAmount);

                    //Add the amount taken from player's inventory into team's resource pool
                    teamOne.resources += playerItemAmount;
                }
            }

            if (computer._type.Name == "Collective Generator" && //using collective generator
                player._team == computer._team) //player and computer are on the same team
            {
                //If not enough resources...
                if (teamTwo.resources < product.Cost)
                {
                    player.sendMessage(0, "Your team does not have enough resources to produce this!");
                    //Don't produce it
                    return false;
                }
                //But if we do have enough...
                else
                {
                    //Make it and subtract from total team's resource total
                    teamTwo.resources -= product.Cost;
                }

                //Accepts titanium oxide to add to team resources
                if (product.Title.StartsWith("Refine Titanium Oxide"))
                {
                    //How much titanium oxide does the player have?
                    int playerItemAmount = player.getInventoryAmount(product.PlayerItemNeededId);

                    //Add the amount taken from player's inventory into team's resource pool
                    teamTwo.resources += playerItemAmount;
                }
            }


            updateTickers();

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
            //Only add/destroy resources if more than one player present (since we need more than one team)
            if (_arena.PlayerCount > 1)
            {
                //Add resources to the team player will join (ie. the one with the less players)
                if (teamOne.arenaTeam.ActivePlayerCount < teamTwo.arenaTeam.ActivePlayerCount)
                {
                    teamOne.resources += RESOURCE_MULT;
                }
                //If he did not join the first team, he must have joined the other team
                else
                {
                    teamTwo.resources += RESOURCE_MULT;
                }
            }

            updateTickers();
            return true;
        }

        /// <summary>
        /// Triggered when a player wants to spec and leave the game
        /// </summary>
        [Scripts.Event("Player.LeaveGame")]
        public bool playerLeaveGame(Player player)
        {
            //Remove resources from the player's team
            if (player._team == teamOne.arenaTeam)
            {
                teamOne.resources -= RESOURCE_MULT;
            }
            //If he did not leave the first team, he must have left the other team
            else
            {
                teamTwo.resources -= RESOURCE_MULT;
            }

            updateTickers();
            return true;
        }

        /// <summary>
        /// Triggered when a player wants to enter a vehicle
        /// </summary>
        [Scripts.Event("Player.EnterVehicle")]
        public bool playerEnterVehicle(Player player, Vehicle vehicle)
        {
            /*
              _arena.getPlayersInRange(
			_state.positionX, _state.positionY, _type.TrackingRadius)
              */
            //Warp in place
            //player.warp(player._state.positionX, player._state.positionY);

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
            //If the player is in a titan droid (id = 111) and morph weapon used
            if (weapon.id == 1179 && player._occupiedVehicle._type.Id == 111)
            {
                //TODO: skin.orientateTo(player); function...

                //Make new vehicle (titan droid shielded)
                Vehicle skin = _arena.newVehicle(113);

                //Orientate the vehicle to the player's positional state
                orientateTo(skin, player);
                //skin._state.positionX = player._state.positionX;
                //skin._state.positionY = player._state.positionY;
                //skin._state.yaw = player._state.yaw;

                //Make a note of his health (because when you change him to another vehicle, he inherits the max hp of the new vehicle)
                player.setVar("healthDecay", player._occupiedVehicle._type.Hitpoints - player._occupiedVehicle._state.health);

                //Destroy current occupied vehicle
                player._occupiedVehicle.destroy(true);

                //Enter new vehicle (titan shielded droid)
                skin.playerEnter(player);

                //Reapply health loss
                player.inventoryModify(AssetManager.Manager.getItemByName("MinusHealth"), player.getVarInt("healthDecay"));
            }
            //If the player is in a morphed droid (id = 113) and unmorph weapon used
            else if (weapon.id == 1181 && player._occupiedVehicle._type.Id == 113)
            {
                //Return him to this world!

                //Make new vehicle (titan droid)
                Vehicle skin = _arena.newVehicle(111);

                //Orientate the vehicle to the player's positional state
                orientateTo(skin, player);
                //skin._state.positionX = player._state.positionX;
                //skin._state.positionY = player._state.positionY;
                //skin._state.yaw = player._state.yaw;

                //Make note of vehicle's health lost
                player.setVar("healthDecay", player._occupiedVehicle._type.Hitpoints - player._occupiedVehicle._state.health);

                //Destroy current occupied vehicle
                player._occupiedVehicle.destroy(true);

                //Enter new vehicle (original titan droid)
                skin.playerEnter(player);

                //Reapply health loss
                player.inventoryModify(AssetManager.Manager.getItemByName("MinusHealth"), player.getVarInt("healthDecay"));
            }

            //If the vehicle is a collective droid (id = 211) and morph weapon used
            else if (weapon.id == 1179 && player._occupiedVehicle._type.Id == 211)
            {
                //Make new vehicle (collective droid shielded) with player's position
                Vehicle skin = _arena.newVehicle(213);

                //Orientate the vehicle to the player's positional state
                orientateTo(skin, player);
                //skin._state.positionX = player._state.positionX;
                //skin._state.positionY = player._state.positionY;
                //skin._state.yaw = player._state.yaw;

                //Make note of vehicle's health lost
                player.setVar("healthDecay", player._occupiedVehicle._type.Hitpoints - player._occupiedVehicle._state.health);

                //Destroy current occupied vehicle
                player._occupiedVehicle.destroy(true);

                //Enter new vehicle (collective droid shielded)
                skin.playerEnter(player);

                //Reapply health loss
                player.inventoryModify(AssetManager.Manager.getItemByName("MinusHealth"), player.getVarInt("healthDecay"));
            }
            //If the vehicle is a morphed droid (id = 213) and unmorphed weapon used
            else if (weapon.id == 1179 && player._occupiedVehicle._type.Id == 213)
            {
                //Make new vehicle (collective droid) with player's position
                Vehicle skin = _arena.newVehicle(211);

                //Orientate the vehicle to the player's positional state
                orientateTo(skin, player);
                //skin._state.positionX = player._state.positionX;
                //skin._state.positionY = player._state.positionY;
                //skin._state.yaw = player._state.yaw;

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
            //Resets the flag to where the player picked up the flag
            //_arena.flagResetPlayer(victim);
            return true;
        }

        /// <summary>
        /// Triggered when one player has killed another
        /// </summary>
        [Scripts.Event("Player.PlayerKill")]
        public bool playerPlayerKill(Player victim, Player killer)
        {
            //Amount of resources to lose/gain from death/kill...
            int resourceLostOnDeath = victim.Bounty * 5;

            //Makes sure teamkilling does not result in a gain of resources of any form
            if (killer._team != victim._team)
            {
                //If killer is on team one...
                if (killer._team == teamOne.arenaTeam &&
                    teamTwo.resources > 0) //and team two still has resources to lose
                {
                    //team one gains resources
                    teamOne.resources += resourceLostOnDeath;
                    //team two loses resources (if more than two teams, this will have to consider victim._team)
                    teamTwo.resources -= resourceLostOnDeath;
                }

                //If killer is not on team one, but on team two...
                if (killer._team == teamTwo.arenaTeam &&
                   teamOne.resources > 0) //and team one still has resources to lose
                {
                    //team two gains resources
                    teamTwo.resources += resourceLostOnDeath;
                    //team one loses resources
                    teamOne.resources -= resourceLostOnDeath;
                }
            }
            //Team killing or suicides will only cause team resource lost, no gains
            else
            {
                //Case where killer is on team one
                if (killer._team == teamOne.arenaTeam)
                {
                    //team one loses resources
                    teamOne.resources -= resourceLostOnDeath;
                }
                //Only other team left is two...
                else
                {
                    //team two loses resources
                    teamTwo.resources -= resourceLostOnDeath;
                }
            }

            //If either team tries to lose more resources than they have to lose...
            if (teamOne.resources - resourceLostOnDeath < 0)
            {
                //Deplete all remaining resources
                teamOne.resources = 0;
            }
            if (teamTwo.resources - resourceLostOnDeath < 0)
            {
                teamTwo.resources = 0;
            }

            updateTickers();

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
            /* if(dead==killer._server._assets.getVehicleByID(403))
             {
                 teamOne.resources=0;
                 _arena.sendArenaMessage("Titan Generator has been destroyed! All resources have been destroyed!",0);
             }*/
            return true;
        }
        #endregion
    }
}