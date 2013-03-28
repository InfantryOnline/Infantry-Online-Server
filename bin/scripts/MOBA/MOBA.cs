using System;
using System.Linq;
using System.Collections.Generic;

using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;

using Assets;
using InfServer.Logic;

namespace InfServer.Script.GameType_MOBA
{	// Script Class
    /// Provides the interface between the script and arena
    ///////////////////////////////////////////////////////
    class Script_CTFHQ : Scripts.IScript
    {	///////////////////////////////////////////////////
        // Member Variables
        ///////////////////////////////////////////////////
        private Arena _arena;					//Pointer to our arena class
        private CfgInfo _config;				//The zone config

        private Team _victoryTeam;				//The team currently winning!

        private int _lastGameCheck;				//The tick at which we last checked for game viability
        private int _tickGameStarting;			//The tick at which the game began starting (0 == not initiated)
        private int _tickGameStart;				//The tick at which the game started (0 == stopped)

        private int _lastMinionSpawn;           //The tick at which we last spawned minions
        private int _minionSpawn;               //The amount of time that has to lapse before we spawn a minion

        //Settings
        private int _minPlayers;				//The minimum amount of players

        private int _hqVehId;                   //ID of our HQ
        private int _towerVehId;                //ID of our towers
        private int _inhibVehId;                //ID of our inhibitors

        private string _team1;                  //Names of teams  
        private string _team2;

        private int _team1MinionX;              //Locations for minion spawns
        private int _team1MinionY;
        private int _team2MinionX;
        private int _team2MinionY;

        private bool _team1Upgrade;             //True if team 1 requires stronger bots
        private bool _team2Upgrade;             //True if team 2 requires stronger bots


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

            _minPlayers = 2; //We want at least two players to start a game
            _hqVehId = 1;    //Our HQ ID
            _towerVehId = 1; //Our tower ID
            _inhibVehId = 1; //Our inhibitor ID

            _team1Upgrade = false; //Default to no upgraded bots
            _team2Upgrade = false;

            _team1MinionX = 1;  //Set where bots will spawn (pixels)
            _team1MinionY = 1;
            _team2MinionX = 1;
            _team2MinionY = 1;            

            _team1 = "Titan";
            _team2 = "Collective";
            
            _victoryTeam = null;

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

            //Find out if anyone won yet
            if (_victoryTeam != null)
            {//Someone won
                _arena.gameEnd();
            }

            //Check to see if we need to spawn bots            
            if (now - _lastMinionSpawn > _minionSpawn)
            {//Spawn the minions
                foreach (Team t in _arena.Teams)
                {
                    if (t._name.Equals(_team1) && _team1Upgrade)
                    {//Spawn better bots

                    }
                    else if(t._name.Equals(_team1) && !_team1Upgrade)
                    {//Spawn normal bots

                    }
                    if (t._name.Equals(_team2) && _team2Upgrade)
                    {//Spawn better bots

                    }
                    else if (t._name.Equals(_team2) && !_team2Upgrade)
                    {//Spawn normal bots

                    }
                    

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
            //Did they kill a tower?
            if (dead._type.Id == _towerVehId)
            {//Yes, let's inform everyone
                _arena.sendArenaMessage(killer._alias + " has destroyed " + dead._team._name + "'s tower!"); 
            }

            //Did they kill an inhibitor?
            if (dead._type.Id == _inhibVehId)
            {//Yes, so lets inform everyone
                _arena.sendArenaMessage(killer._alias + " has destroyed " + dead._team._name + "'s inhibitor!"); 
                //Now give them better bots on next spawn
                //Check their team to determine who gets the better bot spawn
                if (killer._team._name.Equals(_team1))
                {
                    _team1Upgrade = true;
                    foreach (Player p in killer._team.ActivePlayers)                    
                        p.sendMessage(1, "Your HQ will now produce upgraded minions");                    
                }
                else
                {
                    _team2Upgrade = true;
                    foreach (Player p in killer._team.ActivePlayers)
                        p.sendMessage(1, "Your HQ will now produce upgraded minions"); 
                }
            }

            //Did they kill an HQ?
            if (dead._type.Id == _hqVehId)
            {//Yes, signal that a team has won
                _arena.sendArenaMessage(killer._alias + " has destroyed " + dead._team._name + "'s HQ!");
                _victoryTeam = killer._team;
            }

            return true;
           
        }
        /// <summary>
        /// Called when the game begins
        /// </summary>
        [Scripts.Event("Game.Start")]
        public bool gameStart()
        {	//We've started!
            _tickGameStart = Environment.TickCount;
            _tickGameStarting = 0;

            //Spawn all the towers

            //Spawn all the inhibitors

            //Spawn all the HQs

            //Let everyone know
            _arena.sendArenaMessage("Game has started!", _config.flag.resetBong);

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

            //Destroy all the towers, inhibitors, and HQs

            //Reset our variables
            _victoryTeam = null;
            _team1Upgrade = false;
            _team2Upgrade = false;

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

            //Destroy all the towers, inhibitors, and HQs

            //Reset our variables
            _victoryTeam = null;
            _team1Upgrade = false;
            _team2Upgrade = false;

            return true;
        }

    }
}