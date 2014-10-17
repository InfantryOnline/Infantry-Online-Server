using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Logic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;

using Assets;

namespace InfServer.Script.GameType_Burnt
{
    public class Script_Burnt : Scripts.IScript
    {
        #region Vars
        //Pointers
        private Arena _Arena;
        private CfgInfo _CFG;

        //Game Types
        private KOTH _KOTH;
        private CTF _CTF;
        private TDM _TDM;
        private Gladiator _Gladiator;

        //Settings
        private GameTypes _GameType;
        private Settings _Settings;
        private List<string> gametypes = new List<string> {"KOTH", "TDM"};

        //Poll Variables
        private int _lastGameCheck;
        private VoteSystem _VoteSystem;

        //Misc 
        private int _gameCount;
        #endregion

        #region Functions

        public bool init(IEventObject invoker)
        {	//Populate our variables
            _Arena = invoker as Arena;
            _CFG = _Arena._server._zoneConfig;

            _Settings = new Settings();

            _KOTH = new KOTH(_Arena, _Settings);
            _CTF = new CTF(_Arena, _Settings);
            _TDM = new TDM(_Arena, _Settings);
            _Gladiator = new Gladiator(_Arena, _Settings);            

            _GameType = GameTypes.TDM;

            _VoteSystem = new VoteSystem();

            foreach (string type in gametypes)
                Settings.AllowedGameTypes.Add((GameTypes)_Settings.GetType(type));
           
            _gameCount = 0;
            return true;
        }

        public bool poll()
        {	//Should we check game state yet?
            int now = Environment.TickCount;

            if (now - _lastGameCheck <= Arena.gameCheckInterval)
                return true;
            _lastGameCheck = now;

            if (_Settings.GameState == GameStates.Vote)
            {
                //_Arena.sendArenaMessage("vote -- main");
                if (_Arena.PlayersIngame.Count() >= Settings.MinPlayers)
                {
                    vote();
                }
                else
                    _Arena.setTicker(1, 1, 0, "Not Enough Players");
            }

            switch (_GameType)
            {
                case GameTypes.TDM:
                    _TDM.Poll(now);
                    break;
                case GameTypes.KOTH:
                    _KOTH.Poll(now);
                    break;
                case GameTypes.GLAD:
                    _Gladiator.Poll(now);
                    break;
                case GameTypes.CTF:
                    _CTF.Poll(now);
                    break;
            }
            return true;
        }

        private void vote()
        {
            if (_Settings.EventsEnabled)
            {
                _Arena.sendArenaMessage("[Event] Games until event starts: " + (Settings.GamesBeforeEvent - _gameCount));
                _gameCount++;
            }

            if (_Settings.EventsEnabled && (_gameCount > Settings.GamesBeforeEvent))
            {
                _Settings.GameState = GameStates.Init;
                _GameType = GameTypes.GLAD;
                _gameCount = 0;

                return;
            }

            if (!Settings.VotingEnabled)
            {
                _Arena.sendArenaMessage("[Game] Voting has been disabled");
            }
            else
            {
                _VoteSystem = new VoteSystem();

                string getTypes = String.Join(" & ", gametypes);
                _Arena.sendArenaMessage("[Round Vote] Gametype Vote starting - Vote with ?Game <type>", 1);
                _Arena.sendArenaMessage(String.Format("[Round Vote] Choices are: {0}", getTypes));            
            }
          
            _Settings.GameState = GameStates.PreGame;
            preGame();
        }

        private void preGame()
        {
            //Sit here until timer runs out
            //People can vote at this time
            _Arena.setTicker(1, 1, Settings.VotingPeriod * 100, "Next game: ",
                    delegate()
                    {	//Trigger the game start
                        _Settings.GameState = GameStates.ActiveGame;

                        if (!Settings.VotingEnabled)
                        {
                            _Arena.gameStart();
                            return;
                        }

                        if (_VoteSystem.GetWinningVote() != GameTypes.NULL)
                        {
                            _GameType = _VoteSystem.GetWinningVote();
                        }
                        
                        _Arena.gameStart();
                    }
                );
        }

        #endregion

        #region Script Events
        /// <summary>
        /// Called when the game begins
        /// </summary>
        [Scripts.Event("Game.Start")]
        public bool gameStart()
        {
            switch (_GameType)
            {
                case GameTypes.CTF:
                    _CTF.StartGame();
                    break;
                case GameTypes.GLAD:
                    _Gladiator.StartGame();
                    break;
                case GameTypes.KOTH:
                    _KOTH.StartGame();
                    break;
                case GameTypes.TDM:
                    _TDM.StartGame();
                    break;
            }

            return true;
        }

        /// <summary>
        /// Called when the game ends
        /// </summary>
        [Scripts.Event("Game.End")]
        public bool gameEnd()
        {	//Game finished
            _Settings.GameState = GameStates.PostGame;
            _Arena.sendArenaMessage("Game Over");

            switch (_GameType)
            {
                case GameTypes.CTF:
                    _CTF.EndGame();
                    break;
                case GameTypes.GLAD:
                    _Gladiator.EndGame();
                    break;
                case GameTypes.KOTH:
                    _KOTH.EndGame();
                    break;
                case GameTypes.TDM:
                    _TDM.EndGame();
                    break;
            }

            return true;
        }

        /// <summary>
        /// Triggered when one player has killed another
        /// </summary>
        [Scripts.Event("Player.PlayerKill")]
        public bool playerPlayerKill(Player victim, Player killer)
        {
            switch (_GameType)
            {
                case GameTypes.TDM:
                    _TDM.PlayerKill(killer, victim);
                    break;
                case GameTypes.KOTH:
                    _KOTH.PlayerKill(killer, victim);
                    break;
                case GameTypes.CTF:
                    _CTF.PlayerKill(killer, victim);
                    break;
                case GameTypes.GLAD:
                    _Gladiator.PlayerKill(killer, victim);
                    break;
            }
            return true;
        }

        /// <summary>
        /// Called when a player enters the arena
        /// </summary>
        [Scripts.Event("Player.EnterArena")]
        public void playerEnterArena(Player player)
        {
            switch (_GameType)
            {
                case GameTypes.TDM:
                    _TDM.PlayerEnterArena(player);
                    break;
                case GameTypes.KOTH:
                    _KOTH.PlayerEnterArena(player);
                    break;
                case GameTypes.CTF:
                    _CTF.PlayerEnterArena();
                    break;
                case GameTypes.GLAD:
                    _Gladiator.PlayerEnterArena(player);
                    break;
            }
        }

        /// <summary>
        /// Called when a player enters the game
        /// </summary>
        [Scripts.Event("Player.Enter")]
        public void playerEnter(Player player)
        {
            switch (_GameType)
            {
                case GameTypes.TDM:
                    _TDM.PlayerEnter(player);
                    break;
                case GameTypes.KOTH:
                    _KOTH.PlayerEnter(player);
                    break;
                case GameTypes.CTF:
                    _CTF.PlayerEnter();
                    break;
                case GameTypes.GLAD:
                    _Gladiator.PlayerEnter(player);
                    break;
            }
        }

        /// <summary>
        /// Triggered when a player wants to spec and leave the game
        /// </summary>
        [Scripts.Event("Player.Leave")]
        public void playerLeave(Player player)
        {
            switch (_GameType)
            {
                case GameTypes.TDM:
                    _TDM.PlayerLeaveGame(player);
                    break;
                case GameTypes.KOTH:
                    _KOTH.PlayerLeaveGame(player);
                    break;
                case GameTypes.CTF:
                    _CTF.PlayerLeaveGame();
                    break;
                case GameTypes.GLAD:
                    _Gladiator.PlayerLeaveGame(player);
                    break;
            }
        }

        /// <summary>
        /// Called when a player sends a chat command
        /// </summary>
        [Scripts.Event("Player.ChatCommand")]
        public bool playerChatCommand(Player player, Player recipient, string command, string payload)
        {
            if (command.ToLower().Equals("killp"))
            {
                if (player.PermissionLevel == Data.PlayerPermission.Sysop)
                {
                    if (recipient == null)
                    {
                        return true;
                    }
                    else
                    {
                        recipient._state.health = 0;
                    }
                    return true;
                }
            }

            if (command.ToLower().Equals("game"))
            {
                if (_Settings.GameState == GameStates.PreGame)
                {
                    GameTypes vote = GameTypes.NULL;

                    switch (payload.ToLower())
                    {
                        case "tdm":
                            vote = GameTypes.TDM;
                            break;
                        case "koth":
                            vote = GameTypes.KOTH;
                            break;
                        case "ctf":
                            vote = GameTypes.CTF;
                            break;
                    }

                    if (_VoteSystem.AddVote(vote, player))
                    {
                        player.sendMessage(0, "You have voted for " + vote.ToString());
                    }
                }
                else
                {
                    player.sendMessage(0, "You can't vote right now jerk");
                }
                return true;
            }

            if (command.ToLower().Equals("settings"))
            {
                if (player.PermissionLevel >= Data.PlayerPermission.Sysop)
                {
                    if (String.IsNullOrEmpty(payload))
                    {
                        player.sendMessage(0, "?settings <setting> <new value>");
                        player.sendMessage(0, "Events " + _Settings.EventsEnabled);
                        player.sendMessage(0, "Voting " + Settings.VotingEnabled);
                        player.sendMessage(0, "StartEvent <gladiator>");
                        player.sendMessage(0, "MinPlayers " + Settings.MinPlayers);
                        player.sendMessage(0, "GamesBeforeEvent " + Settings.GamesBeforeEvent);
                        player.sendMessage(0, "VoteTimer " + Settings.VotingPeriod);
                        player.sendMessage(0, "GamePlay " + String.Join(", ", gametypes));
                        player.sendMessage(0, "Note: for game play, type ?settings gameplay add/delete <new value>");
                        return true;
                    }

                    string[] cmd = payload.Split(' ');
                    switch (cmd[0].ToLower())
                    {
                        case "events":
                            Boolean.TryParse(cmd[1], out _Settings.EventsEnabled);
                            player.sendMessage(0, String.Format("Events have been {0}.", _Settings.EventsEnabled ? "enabled" : "disabled"));
                            break;
                        case "voting":
                            Boolean.TryParse(cmd[1], out Settings.VotingEnabled);
                            player.sendMessage(0, String.Format("Voting has been {0}.", Settings.VotingEnabled ? "enabled" : "disabled"));
                            break;
                        case "startevent":
                            player.sendMessage(0, "TODO");
                            //TODO
                            break;
                        case "minplayers":
                            Int32.TryParse(cmd[1], out Settings.MinPlayers);
                            player.sendMessage(0, String.Format("Minimum players are now {0}.", Settings.MinPlayers.ToString()));
                            break;
                        case "gamesbeforeevent":
                            Int32.TryParse(cmd[1], out Settings.GamesBeforeEvent);
                            player.sendMessage(0, String.Format("How many games before an event has been changed to {0}.", Settings.GamesBeforeEvent.ToString()));
                            break;
                        case "votetimer":
                            Int32.TryParse(cmd[1], out Settings.VotingPeriod);
                            player.sendMessage(0, String.Format("Voting period has been changed to {0}.", Settings.VotingPeriod.ToString()));
                            break;
                        case "gameplay":
                            {
                                string lower = cmd[1].ToLower();
                                if (lower.Contains("add") || lower.Contains("delete"))
                                {
                                    if (String.IsNullOrWhiteSpace(cmd[2]))
                                    {
                                        player.sendMessage(0, "Invalid payload: gameplay add/delete <new value>");
                                        break;
                                    }
                                    string value = cmd[2].ToLower();
                                    switch (lower)
                                    {
                                        case "add":
                                            Settings.AllowedGameTypes.Add((GameTypes)_Settings.GetType(value));
                                            gametypes.Add(value);
                                            break;
                                        case "delete":
                                            Settings.AllowedGameTypes.Remove((GameTypes)_Settings.GetType(value));
                                            gametypes.Remove(value);
                                            break;
                                        default:
                                            player.sendMessage(0, "Invalid payload: gameplay add/delete <new value>");
                                            break;
                                    }
                                }
                                else
                                    player.sendMessage(0, "Invalid payload: gameplay add/delete <new value>");
                            }
                            break;
                         default:
                            player.sendMessage(0, "Invalid payload");
                            break;
                    }
                }
            }
            return true;
        }

        #endregion

    }
}
