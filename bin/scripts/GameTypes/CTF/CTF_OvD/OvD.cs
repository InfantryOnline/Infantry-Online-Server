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

namespace InfServer.Script.GameType_CTF_OvD
{
	/// <summary>
	/// Captain type enumeration
	/// </summary>
	public enum eCaptainType : byte
	{
		None = 1,
		Defense = 2,
		Offense = 3
	}

	class OvD
	{
		private Arena _arena;
		private List<Player> _CaptainQueue = new List<Player>();
		private String _OffenseTeamName = "Titan Militia";
		private String _DefenseTeamName = "Collective Military";
		private OvdTeam _Offense;
		private OvdTeam _Defense;

		public class OvdTeam
		{
			private Arena _arena;                                   // Arena object
			private Team _Team;                                     // Raw team object
			private bool _IsLocked = false;                         // Is this team locked
			private Player _Captain;                                // The captain of the team
			private List<Player> _PlayerList = new List<Player>();  // Players actively on the team or should be
			private List<Player> _InviteList = new List<Player>();  // Anyone who has been ?invited

			/// <summary>
			/// Constructor for an OvD team, either offense or defense.
			/// </summary>
			/// <param name="TheArena">The arena object</param>
			/// <param name="TheTeam">The team to use </param>
			public OvdTeam(Arena TheArena, Team TheTeam)
			{
				_arena = TheArena;
				_Team = TheTeam;
			}

			public Player Captain
			{
				get
				{
					return _Captain;
				}
				set
				{
					if (_Captain == null)
					{
						_Captain = value;
					}
				}
			}

			/// <summary>
			/// Test if the captain's spot has been taken
			/// </summary>
			public bool CaptainTaken
			{
				get
				{
					if (_Captain == null)
					{
						return false;
					}
					else
					{
						return true;
					}
				}
			}

			// Not implemented
			public void TransferCaptain(Player FromPlayer, Player ToPlayer)
			{
				//_arena.triggerMessage(0, 10, "Blah");
			}

			/// <summary>
			/// Lock the team
			/// </summary>
			public void Lock()
			{
				this.IsLocked = true;
			}

			/// <summary>
			/// Unlock the team
			/// </summary>
			public void UnLock()
			{
				this.IsLocked = false;
			}

			/// <summary>
			/// Is the team locked and invite only
			/// </summary>
			public bool IsLocked
			{
				get { return _IsLocked; }
				set
				{
					switch (value)
					{
						case true:
							_Captain.sendMessage(0, "The team is locked.");
							break;
						case false:
							_Captain.sendMessage(0, "The team is unlocked.");
							break;
					}
					_IsLocked = value;
				}
			}

			/// <summary>
			/// The raw Team object
			/// </summary>
			public Team Team { get { return _Team; } }

			/// <summary>
			/// Cleans the invite list of anyone who is not logged in
			/// </summary>
			private void CleanInviteList()
			{
				List<Player> _ToRemove = new List<Player>();
				foreach (Player xPlayer in _InviteList)
				{
					if (xPlayer._bLoggedIn == false)
					{
						_ToRemove.Add(xPlayer);
					}
				}

				foreach (Player xPlayer in _ToRemove)
				{
					_InviteList.Remove(xPlayer);
				}
			}

			/// <summary>
			/// Invite a person to a team
			/// </summary>
			/// <param name="ThePlayer">The player to invite to this OvD team</param>
			public void Invite(Player ThePlayer)
			{
				CleanInviteList();

				// Uninvite him to clear his name from the queue
				UnInvite(ThePlayer);

				// Now add him.  He should now have only one entry.
				_InviteList.Add(ThePlayer);
			}

			/// <summary>
			/// Remove a player from the invite list for this team
			/// </summary>
			/// <param name="ThePlayer">The player to uninvite from this OvD team</param>
			public void UnInvite(Player ThePlayer)
			{
				CleanInviteList();
				// Remove the player from the invite list in case he is already on it.
				List<Player> _ToRemove = new List<Player>();
				foreach (Player xPlayer in _InviteList)
				{
					if (ThePlayer.Equals(xPlayer))
					{
						_ToRemove.Add(xPlayer);
					}
				}

				foreach (Player xPlayer in _ToRemove)
				{
					_InviteList.Remove(xPlayer);
				}
			}

			/// <summary>
			/// Returns true if the ThePlayer is invited to this team
			/// </summary>
			/// <param name="ThePlayer">The player to test for an invite</param>
			public bool IsInvited(Player ThePlayer)
			{
				CleanInviteList();
				foreach (Player xPlayer in _InviteList)
				{
					if (ThePlayer.Equals(xPlayer))
					{
						return true;
					}
				}
				return false;
			}

			/// <summary>
			/// For sorting spec and non spec people
			/// </summary>
			/// <param name="ThePlayer">The player to add</param>
			private void _AddPlayer(Player ThePlayer)
			{
				if (ThePlayer.IsSpectator) { ThePlayer.unspec(this.Team); }
				else { this.Team.addPlayer(ThePlayer); }
				_PlayerList.Add(ThePlayer);
				UnInvite(ThePlayer);
			}

			/// <summary>
			/// Add a player to this team.  Unspec him if he's in spec.
			/// </summary>
			/// <param name="ThePlayer">The player to add</param>
			public void AddPlayer(Player ThePlayer)
			{
				// If not locked, put them on the team
				if (this.IsLocked == false)
				{
					// Check the max team size
					if (this.Team._info.maxPlayers > this.Team.ActivePlayerCount || this.Team.ActivePlayerCount == 0)
					{
						_AddPlayer(ThePlayer);
					}
					else
					{
						ThePlayer.sendMessage(0, this.Team._name + " is full.");
					}
				}
				else // Check for an invite
				{
					if (IsInvited(ThePlayer))
					{
						_AddPlayer(ThePlayer);
					}
					else
					{
						ThePlayer.sendMessage(0, this.Team._name + " is locked.  The captain of the team must ?invite you.");
					}
				}

			}

			/// <summary>
			/// Remove a player from all queues
			/// </summary>
			/// <param name="ThePlayer">The player to remove</param>
			public void ResetPlayer(Player ThePlayer)
			{
				_InviteList.Remove(ThePlayer);
				_PlayerList.Remove(ThePlayer);
				if (ThePlayer.Equals(_Captain))
				{
					_Captain = null;
				}
			}

			/// <summary>
			/// Kick a player off the team and into spec
			/// </summary>
			/// <param name="ThePlayer">The player to kick</param>
			public void KickPlayer(Player ThePlayer)
			{
				ThePlayer.spec();
				_PlayerList.Remove(ThePlayer);
				UnInvite(ThePlayer);
			}

			/// <summary>
			/// Returns true if the ThePlayer is allowed to play on this team
			/// </summary>
			/// <param name="ThePlayer">The player to test</param>
			public bool IsPlayer(Player ThePlayer)
			{
				foreach (Player xPlayer in _PlayerList)
				{
					if (ThePlayer.Equals(xPlayer))
					{
						return true;
					}
				}
				return false;
			}

		}

		/// <summary>
		/// Constructor for an OvD object.
		/// </summary>
		/// <param name="TheArena">The arena object</param>
		public OvD(Arena TheArena)
		{
			_arena = TheArena;
			_Offense = new OvdTeam(_arena, _arena.getTeamByName(_OffenseTeamName));
			_Defense = new OvdTeam(_arena, _arena.getTeamByName(_DefenseTeamName));
		}

		/// <summary>
		/// Constructor for an OvD object.
		/// </summary>
		/// <param name="TheArena">The arena object</param>
		/// <param name="OffenseTeamName">The string name of the offensive team</param>
		/// <param name="DefenseTeamName">The string name of the defensive team</param>
		public OvD(Arena TheArena, String OffenseTeamName, String DefenseTeamName)
		{
			_arena = TheArena;
			_OffenseTeamName = OffenseTeamName;
			_DefenseTeamName = DefenseTeamName;
			_Offense = new OvdTeam(_arena, _arena.getTeamByName(_OffenseTeamName));
			_Defense = new OvdTeam(_arena, _arena.getTeamByName(_DefenseTeamName));
		}

		/// <summary>
		/// Returns the Offensive OvD team
		/// </summary>
		public OvdTeam Offense
		{
			get { return _Offense; }
		}

		/// <summary>
		/// Returns the Defensive OvD team
		/// </summary>
		public OvdTeam Defense
		{
			get { return _Defense; }
		}

		/// <summary>
		/// Make this player the offensive captain
		/// </summary>
		/// <param name="ThePlayer">The player to make captain</param>
		public void MakeOCaptain(Player ThePlayer)
		{
			if (CaptainType(ThePlayer) > eCaptainType.None) { return; }
			this._Offense.Captain = ThePlayer;
			ThePlayer.sendMessage(0, "You are now the captain for offense.");
			_Offense.AddPlayer(ThePlayer);
		}

		/// <summary>
		/// Make this player the defensive captain
		/// </summary>
		/// <param name="ThePlayer">The player to make captain</param>
		public void MakeDCaptain(Player ThePlayer)
		{
			if (CaptainType(ThePlayer) > eCaptainType.None) { return; }
			this._Defense.Captain = ThePlayer;
			ThePlayer.sendMessage(0, "You are now the captain for defense.");
			_Defense.AddPlayer(ThePlayer);
		}

		/// <summary>
		/// Is this person a captain and if so, which type?
		/// </summary>
		/// <param name="ThePlayer">The player to test</param>
		public eCaptainType CaptainType(Player ThePlayer)
		{
			if (ThePlayer.Equals(this._Defense.Captain))
			{
				return eCaptainType.Defense;
			}
			else if (ThePlayer.Equals(this._Offense.Captain))
			{
				return eCaptainType.Offense;
			}
			return eCaptainType.None;
		}

		/// <summary>
		/// Request to be the defensive captain
		/// </summary>
		/// <param name="ThePlayer">The player making the request</param>
		public void RequestDCaptain(Player ThePlayer)
		{
			switch (CaptainType(ThePlayer))
			{
				case eCaptainType.Defense:
					ThePlayer.sendMessage(1, "You are already the captain for the defense.");
					return;
				case eCaptainType.Offense:
					ThePlayer.sendMessage(1, "You are already the captain for the offense.");
					return;
				case eCaptainType.None:
					if (this._Defense.CaptainTaken == false)
					{
						this.MakeDCaptain(ThePlayer);
					}
					else
					{
						ThePlayer.sendMessage(0, "The captain spot for defense is taken.");
						ThePlayer.sendMessage(0, "There will be a queue for defensive captian Soon(tm).");
					}
					break;
			}
		}

		/// <summary>
		/// Request to be the offensive captain
		/// </summary>
		/// <param name="ThePlayer">The player making the request</param>
		public void RequestOCaptain(Player ThePlayer)
		{
			switch (CaptainType(ThePlayer))
			{
				case eCaptainType.Defense:
					ThePlayer.sendMessage(1, "You are already the captain for the defense.");
					return;
				case eCaptainType.Offense:
					ThePlayer.sendMessage(1, "You are already the captain for the offense.");
					return;
				case eCaptainType.None:
					if (this._Offense.CaptainTaken == false)
					{
						this.MakeOCaptain(ThePlayer);
					}
					else
					{
						ThePlayer.sendMessage(0, "The captain spot for offense is taken.");
						ThePlayer.sendMessage(0, "There will be a queue for offensive captian Soon(tm).");
					}
					break;
			}
		}

		/// <summary>
		/// Request to be the captain of any team
		/// </summary>
		/// <param name="ThePlayer">The player making the request</param>
		public void RequestCaptain(Player ThePlayer)
		{
			switch (CaptainType(ThePlayer))
			{
				case eCaptainType.Defense:
					ThePlayer.sendMessage(1, "You are already the captain for the defense.");
					return;
				case eCaptainType.Offense:
					ThePlayer.sendMessage(1, "You are already the captain for the offense.");
					return;
				case eCaptainType.None:
					if (this._Defense.CaptainTaken == false && this._Offense.CaptainTaken == false)
					{
						this.MakeDCaptain(ThePlayer);

						//Add logic to start
					}
					else if (this._Offense.CaptainTaken == false)
					{
						this.MakeOCaptain(ThePlayer);
					}
					else
					{
						ThePlayer.sendMessage(0, "Both captain spots are taken.");
						ThePlayer.sendMessage(0, "There will be a queue for defensive captian Soon(tm).");
					}
					break;
			}

		}

		/// <summary>
		/// Request to join the offense
		/// </summary>
		/// <param name="ThePlayer">The player making the request</param>
		public void JoinO(Player ThePlayer)
		{
			if (_Offense.CaptainTaken == false)
			{
				RequestOCaptain(ThePlayer);
			}
			else if (_Defense.IsPlayer(ThePlayer))
			{
				ThePlayer.sendMessage(0, "You can not join the offense if you are on the defense.");
			}
			else
			{
				_Offense.AddPlayer(ThePlayer);
			}
		}

		/// <summary>
		/// Request to join the defense
		/// </summary>
		/// <param name="ThePlayer">The player making the request</param>
		public void JoinD(Player ThePlayer)
		{
			if (_Defense.CaptainTaken == false)
			{
				RequestDCaptain(ThePlayer);
			}
			else if (_Offense.IsPlayer(ThePlayer))
			{
				ThePlayer.sendMessage(0, "You can not join the offense if you are on the defense.");
			}
			else
			{
				_Defense.AddPlayer(ThePlayer);
			}
		}

		/// <summary>
		/// Runs when a player presses f12
		/// </summary>
		/// <param name="ThePlayer">The player attempting to join the game</param>
		/// <returns>Boolean</returns>
		public bool JoinGame(Player ThePlayer)
		{
			// Which team to put him on?
			if (this.Defense.IsPlayer(ThePlayer))
			{
				this.Defense.Team.addPlayer(ThePlayer);
			}
			else if (this.Offense.IsPlayer(ThePlayer))
			{
				this.Offense.Team.addPlayer(ThePlayer);
			}
			else
			{
				ThePlayer.sendMessage(1, "This area is running OvD.  Type ?helpOvD for more info.");
			}
			return false;
		}

		/// <summary>
		/// Simple function to return a player object.  It uses either the recipient object from chat or the payload string to find the player
		/// </summary>
		/// <param name="Recipient">Player object</param>
		/// <param name="Payload">String name of a player</param>
		/// <returns>A player or null</returns>
		private Player GetPlayerFromChat(Player Recipient, string Payload)
		{
			Player ToReturn = Recipient;
			if (ToReturn == null)
			{
				ToReturn = _arena.getPlayerByName(Payload);
			}
			return ToReturn;
		}

		/// <summary>
		/// Runs when a player uses a chat command
		/// </summary>
		/// <param name="ThePlayer">The player sending the chat</param>
		/// <param name="Recipient">The recipient if any</param>
		/// <param name="Command">The command sent</param>
		/// <param name="Payload">The parameters of the command</param>
		public void ChatCommand(Player ThePlayer, Player Recipient, string Command, string Payload)
		{
			string pCommand = Command.ToLower();
			Player Target = GetPlayerFromChat(Recipient, Payload);
			switch (pCommand)
			{
				case "cap":
					this.RequestCaptain(ThePlayer);
					break;
				case "capo":
					this.RequestOCaptain(ThePlayer);
					break;
				case "capd":
					this.RequestDCaptain(ThePlayer);
					break;
				case "joino":
					this.JoinO(ThePlayer);
					break;
				case "joind":
					this.JoinD(ThePlayer);
					break;
				case "subo":
					ThePlayer.sendMessage(0, "Not implemented yet.");
					break;
				case "subd":
					ThePlayer.sendMessage(0, "Not implemented yet.");
					break;
				case "lock":
					switch (this.CaptainType(ThePlayer))
					{
						case eCaptainType.Defense:
							this.Defense.Lock();
							break;
						case eCaptainType.Offense:
							this.Offense.Lock();
							break;
						case eCaptainType.None:
							ThePlayer.sendMessage(1, "You can not lock a team unless you are a captain");
							break;
					}
					break;
				case "unlock":
					switch (this.CaptainType(ThePlayer))
					{
						case eCaptainType.Defense:
							this.Defense.UnLock();
							break;
						case eCaptainType.Offense:
							this.Offense.UnLock();
							break;
						case eCaptainType.None:
							ThePlayer.sendMessage(1, "You can not unlock a team unless you are a captain");
							break;
					}
					break;
				case "invite":
					switch (this.CaptainType(ThePlayer))
					{
						case eCaptainType.Defense:
							if (Target == null) { ThePlayer.sendMessage(1, "Can not find the player to invite."); break; }
							this.Defense.Invite(Target);
							break;
						case eCaptainType.Offense:
							if (Target == null) { ThePlayer.sendMessage(1, "Can not find the player to invite."); break; }
							this.Defense.Invite(Target);
							break;
						case eCaptainType.None:
							ThePlayer.sendMessage(1, "You can not invite a player to a team unless you are a captain");
							break;
					}
					break;
				case "kick":
					switch (this.CaptainType(ThePlayer))
					{
						case eCaptainType.Defense:
							if (Target == null) { ThePlayer.sendMessage(1, "Can not find the player to kick."); break; }
							this.Defense.KickPlayer(Target);
							break;
						case eCaptainType.Offense:
							if (Target == null) { ThePlayer.sendMessage(1, "Can not find the player to kick."); break; }
							this.Defense.KickPlayer(Target);
							break;
						case eCaptainType.None:
							ThePlayer.sendMessage(1, "You can not invite a player to a team unless you are a captain");
							break;
					}
					break;
				case "helpovd":
					ThePlayer.sendMessage(0, "?helpOvD - This message.");
					ThePlayer.sendMessage(0, "?cap - Claims a captain's spot.");
					ThePlayer.sendMessage(0, "?capo - Claims the offense's captain spot.");
					ThePlayer.sendMessage(0, "?capd - Claims the defense's captain spot.");
					ThePlayer.sendMessage(0, "?joino - Joins the offense before OvD starts.");
					ThePlayer.sendMessage(0, "?joind - Joins the defense before OvD starts.");
					ThePlayer.sendMessage(0, "?subo - Joins the offensive team after the game has started.");
					ThePlayer.sendMessage(0, "?subd - Joins the defensive team after the game has started.");
					ThePlayer.sendMessage(0, "?lock - Lock your team so people can only join via ?invite.");
					ThePlayer.sendMessage(0, "?unlock - Unlocks your team so anyone can join.");
					ThePlayer.sendMessage(0, "?invite <player> - Invite a player to join your team.  May be sent as a private message.");
					ThePlayer.sendMessage(0, "?kick <player> - Captains can kick a player on their team to spec.  May be sent as a private message.");
					break;
			}
		}

		/// <summary>
		/// Runs when a player leaves
		/// </summary>
		/// <param name="ThePlayer">The player to remove from all lists</param>
		public void PlayerLeave(Player ThePlayer)
		{
			// Remove him from all queues and lists
			_Offense.ResetPlayer(ThePlayer);
			_Defense.ResetPlayer(ThePlayer);
			if (ThePlayer.Equals(_Offense.Captain))
			{
				_arena.sendArenaMessage("The captain spot for offense is open");
			}
			else if (ThePlayer.Equals(_Defense.Captain))
			{
				_arena.sendArenaMessage("The captain spot for defense is open");
			}
		}

		/// <summary>
		/// Runs when the game ends or the game is reset.  Resets all variables
		/// </summary>
		public void Reset()
		{
			_Offense = new OvdTeam(_arena, _arena.getTeamByName(_OffenseTeamName));
			_Defense = new OvdTeam(_arena, _arena.getTeamByName(_DefenseTeamName));
		}
	}

	// Script Class
	/// Provides the interface between the script and arena
	///////////////////////////////////////////////////////
	class Script_OvD : Scripts.IScript
	{	///////////////////////////////////////////////////
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

		private bool _IsOvD = false;            //Is this arena designated as an OvD arena
		private OvD _ovd;

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

			foreach (Arena.FlagState fs in _arena._flags.Values)
			{	//Determine the minimum number of players
				if (fs.flag.FlagData.MinPlayerCount < _minPlayers)
					_minPlayers = fs.flag.FlagData.MinPlayerCount;

				//Register our flag change events
				fs.TeamChange += onFlagChange;
			}

			if (_arena._name.ToLower().Substring(0, 3) == "ovd") { _IsOvD = true; _ovd = new OvD(_arena); }

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

				//There will be a game soon, trigger the event
				/*string soonGame = _server._zoneConfig.EventInfo.soonGame;
				foreach (Player player in _players)
					if (!player.IsSpectator)
						Logic_Assets.RunEvent(player, soonGame);*/
			}

			//Is anybody experiencing a victory?
			if (_tickVictoryStart != 0)
			{	//Have they won yet?
				if (now - _tickVictoryStart > (_config.flag.victoryHoldTime * 10))
					//Yes! Trigger game victory
					gameVictory(_victoryTeam);
				else
				{	//Do we have a victory notice to give?
					if (_tickNextVictoryNotice != 0 && now > _tickNextVictoryNotice)
					{	//Yes! Let's give it
						int countdown = (_config.flag.victoryHoldTime / 100) - ((now - _tickVictoryStart) / 1000);
						_arena.sendArenaMessage(String.Format("Victory for {0} in {1} seconds!",
							_victoryTeam._name, countdown), _config.flag.victoryWarningBong);

						//Plan the next notice
						_tickNextVictoryNotice = _tickVictoryStart;
						_victoryNotice++;

						if (_victoryNotice == 1 && countdown >= 30)
							//Default 2/3 time
							_tickNextVictoryNotice += (_config.flag.victoryHoldTime / 3) * 10;
						else if (_victoryNotice == 2 || (_victoryNotice == 1 && countdown >= 20))
							//10 second marker
							_tickNextVictoryNotice += (_config.flag.victoryHoldTime * 10) - 10000;
						else
							_tickNextVictoryNotice = 0;
					}
				}
			}

			return true;
		}

		#region Events

		/// <summary>
		/// Called when the specified team have won
		/// </summary>
		public void gameVictory(Team victors)
		{	//Let everyone know
			if (_config.flag.useJackpot)
				_jackpot = (int)Math.Pow(_arena.PlayerCount, 2);

			//Stop the game
			_arena.gameEnd();

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

				//Call teh Breakdownz
				_arena.individualBreakdown(p, false);
			}
		}

		/// <summary>
		/// Called when a player sends a chat command
		/// </summary>
		[Scripts.Event("Player.ChatCommand")]
		public bool playerChatCommand(Player player, Player recipient, string command, string payload)
		{
			if (_IsOvD)
			{
				_ovd.ChatCommand(player, recipient, command, payload);
			}
			return true;
		}


		/// <summary>
		/// Called when a player enters the game
		/// </summary>
		[Scripts.Event("Player.Enter")]
		public bool playerEnter(Player player)
		{
			if (_IsOvD)
			{
				player.sendMessage(0, "This arena is setup for OvD.  Type ?helpovd for more info.");
			}
			return true;
		}

		/// <summary>
		/// Called when a player leaves the game
		/// </summary>
		[Scripts.Event("Player.Leave")]
		public bool playerLeave(Player player)
		{
			if (_IsOvD)
			{
				_ovd.PlayerLeave(player);
			}
			return true;
		}

		/// <summary>
		/// Triggered when a player wants to unspec and join the game
		/// </summary>
		[Scripts.Event("Player.JoinGame")]
		public bool playerJoinGame(Player player)
		{
			if (_IsOvD)
			{
				return _ovd.JoinGame(player);
			}
			return true;
		}

		/// <summary>
		/// Triggered when a player wants to spec and leave the game
		/// </summary>
		[Scripts.Event("Player.LeaveGame")]
		public bool playerLeaveGame(Player player)
		{
			// Not sure what to do with OvD here yet
			return true;
		}


		/// <summary>
		/// Called when the game begins
		/// </summary>
		[Scripts.Event("Game.Start")]
		public bool gameStart()
		{
			//Reset Flags
			_arena.flagReset();
			_arena.flagSpawn();
			
			//We've started!
			_tickGameStart = Environment.TickCount;
			_tickGameStarting = 0;

			//Spawn our flags!
			_arena.flagSpawn();

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
			_tickVictoryStart = 0;
			_tickNextVictoryNotice = 0;
			_victoryTeam = null;

			if (_IsOvD) { _ovd.Reset(); }
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

			if (_IsOvD) { _ovd.Reset(); }

			return true;
		}

        /// <summary>
        /// Called when a player sends a mod command
        /// </summary>
        [Scripts.Event("Player.ModCommand")]
        public bool playerModCommand(Player player, Player recipient, string command, string payload)
        {
            command = (command.ToLower());
            if (command.Equals("poweradd"))
            {
                if (player.PermissionLevelLocal < Data.PlayerPermission.SMod)
                {
                    player.sendMessage(-1, "Nice try.");
                    return false;
                }

                int level = (int)Data.PlayerPermission.ArenaMod;
                //Pm'd?
                if (recipient != null)
                {
                    //Check for a possible level
                    if (!String.IsNullOrWhiteSpace(payload))
                    {
                        try
                        {
                            level = Convert.ToInt16(payload);
                        }
                        catch
                        {
                            player.sendMessage(-1, "Invalid level. Level must be either 1 or 2.");
                            return false;
                        }

                        if (level < 1 || level > (int)player.PermissionLevelLocal
                            || level == (int)Data.PlayerPermission.SMod)
                        {
                            player.sendMessage(-1, ":alias:*poweradd level(optional), :alias:*poweradd level (Defaults to 1)");
                            player.sendMessage(0, "Note: there can only be 1 admin level.");
                            return false;
                        }

                        switch (level)
                        {
                            case 1:
                                recipient._permissionStatic = Data.PlayerPermission.ArenaMod;
                                break;
                            case 2:
                                recipient._permissionStatic = Data.PlayerPermission.Mod;
                                break;
                        }
                        recipient._developer = true;
                        recipient.sendMessage(0, String.Format("You have been powered to level {0}. Use *help to familiarize with the commands and please read all rules.", level));
                        player.sendMessage(0, String.Format("You have promoted {0} to level {1}.", recipient._alias, level));
                    }
                    else
                    {
                        recipient._developer = true;
                        recipient._permissionStatic = Data.PlayerPermission.ArenaMod;
                        recipient.sendMessage(0, String.Format("You have been powered to level {0}. Use *help to familiarize with the commands and please read all rules.", level));
                        player.sendMessage(0, String.Format("You have promoted {0} to level {1}.", recipient._alias, level));
                    }

                    //Lets send it to the database
                    //Send it to the db
                    CS_ModQuery<Data.Database> query = new CS_ModQuery<Data.Database>();
                    query.queryType = CS_ModQuery<Data.Database>.QueryType.dev;
                    query.sender = player._alias;
                    query.query = recipient._alias;
                    query.level = level;
                    //Send it!
                    player._server._db.send(query);
                    return true;
                }
                else
                {
                    //We arent
                    //Get name and possible level
                    Int16 number;
                    if (String.IsNullOrEmpty(payload))
                    {
                        player.sendMessage(-1, "*poweradd alias:level(optional) Note: if using a level, put : before it otherwise defaults to arena mod");
                        player.sendMessage(0, "Note: there can only be 1 admin.");
                        return false;
                    }
                    if (payload.Contains(':'))
                    {
                        string[] param = payload.Split(':');
                        try
                        {
                            number = Convert.ToInt16(param[1]);
                            if (number >= 0)
                                level = number;
                        }
                        catch
                        {
                            player.sendMessage(-1, "That is not a valid level. Possible powering levels are 1 or 2.");
                            return false;
                        }
                        if (level < 1 || level > (int)player.PermissionLevelLocal
                            || level == (int)Data.PlayerPermission.SMod)
                        {
                            player.sendMessage(-1, String.Format("*poweradd alias:level(optional) OR :alias:*poweradd level(optional) possible levels are 1-{0}", ((int)player.PermissionLevelLocal).ToString()));
                            player.sendMessage(0, "Note: there can be only 1 admin level.");
                            return false;
                        }
                        payload = param[0];
                    }
                    player.sendMessage(0, String.Format("You have promoted {0} to level {1}.", payload, level));
                    if ((recipient = player._server.getPlayer(payload)) != null)
                    { //They are playing, lets update them
                        switch (level)
                        {
                            case 1:
                                recipient._permissionStatic = Data.PlayerPermission.ArenaMod;
                                break;
                            case 2:
                                recipient._permissionStatic = Data.PlayerPermission.Mod;
                                break;
                        }
                        recipient._developer = true;
                        recipient.sendMessage(0, String.Format("You have been powered to level {0}. Use *help to familiarize with the commands and please read all rules.", level));
                    }

                    //Lets send it off
                    CS_ModQuery<Data.Database> query = new CS_ModQuery<Data.Database>();
                    query.queryType = CS_ModQuery<Data.Database>.QueryType.dev;
                    query.sender = player._alias;
                    query.query = payload;
                    query.level = level;
                    //Send it!
                    player._server._db.send(query);
                    return true;
                }
            }

            if (command.Equals("powerremove"))
            {
                if (player.PermissionLevelLocal < Data.PlayerPermission.SMod)
                {
                    player.sendMessage(-1, "Nice try.");
                    return false;
                }

                int level = (int)Data.PlayerPermission.Normal;
                //Pm'd?
                if (recipient != null)
                {
                    //Check for a possible level
                    if (!String.IsNullOrWhiteSpace(payload))
                    {
                        try
                        {
                            level = Convert.ToInt16(payload);
                        }
                        catch
                        {
                            player.sendMessage(-1, "Invalid level. Levels must be between 0 and 2.");
                            return false;
                        }

                        if (level < 0 || level > (int)player.PermissionLevelLocal
                            || level == (int)Data.PlayerPermission.SMod)
                        {
                            player.sendMessage(-1, ":alias:*powerremove level(optional), :alias:*powerremove level (Defaults to 0)");
                            return false;
                        }

                        switch (level)
                        {
                            case 0:
                                recipient._permissionStatic = Data.PlayerPermission.Normal;
                                recipient._developer = false;
                                break;
                            case 1:
                                recipient._permissionStatic = Data.PlayerPermission.ArenaMod;
                                break;
                            case 2:
                                recipient._permissionStatic = Data.PlayerPermission.Mod;
                                break;
                        }
                        recipient.sendMessage(0, String.Format("You have been demoted to level {0}.", level));
                        player.sendMessage(0, String.Format("You have demoted {0} to level {1}.", recipient._alias, level));
                    }
                    else
                    {
                        recipient._developer = false;
                        recipient._permissionStatic = Data.PlayerPermission.Normal;
                        recipient.sendMessage(0, String.Format("You have been demoted to level {0}.", level));
                        player.sendMessage(0, String.Format("You have demoted {0} to level {1}.", recipient._alias, level));
                    }

                    //Lets send it to the database
                    //Send it to the db
                    CS_ModQuery<Data.Database> query = new CS_ModQuery<Data.Database>();
                    query.queryType = CS_ModQuery<Data.Database>.QueryType.dev;
                    query.sender = player._alias;
                    query.query = recipient._alias;
                    query.level = level;
                    //Send it!
                    player._server._db.send(query);
                    return true;
                }
                else
                {
                    //We arent
                    //Get name and possible level
                    Int16 number;
                    if (String.IsNullOrEmpty(payload))
                    {
                        player.sendMessage(-1, "*powerremove alias:level(optional) Note: if using a level, put : before it otherwise defaults to arena mod");
                        return false;
                    }
                    if (payload.Contains(':'))
                    {
                        string[] param = payload.Split(':');
                        try
                        {
                            number = Convert.ToInt16(param[1]);
                            if (number >= 0)
                                level = number;
                        }
                        catch
                        {
                            player.sendMessage(-1, "That is not a valid level. Possible depowering levels are between 0 and 2.");
                            return false;
                        }
                        if (level < 0 || level > (int)player.PermissionLevelLocal
                            || level == (int)Data.PlayerPermission.SMod)
                        {
                            player.sendMessage(-1, String.Format("*powerremove alias:level(optional) OR :alias:*powerremove level(optional) possible levels are 0-{0}", ((int)player.PermissionLevelLocal).ToString()));
                            return false;
                        }
                        payload = param[0];
                    }
                    player.sendMessage(0, String.Format("You have demoted {0} to level {1}.", payload, level));
                    if ((recipient = player._server.getPlayer(payload)) != null)
                    { //They are playing, lets update them
                        switch (level)
                        {
                            case 0:
                                recipient._permissionStatic = Data.PlayerPermission.Normal;
                                recipient._developer = false;
                                break;
                            case 1:
                                recipient._permissionStatic = Data.PlayerPermission.ArenaMod;
                                break;
                            case 2:
                                recipient._permissionStatic = Data.PlayerPermission.Mod;
                                break;
                        }
                        recipient.sendMessage(0, String.Format("You have been depowered to level {0}.", level));
                    }

                    //Lets send it off
                    CS_ModQuery<Data.Database> query = new CS_ModQuery<Data.Database>();
                    query.queryType = CS_ModQuery<Data.Database>.QueryType.dev;
                    query.sender = player._alias;
                    query.query = payload;
                    query.level = level;
                    //Send it!
                    player._server._db.send(query);
                    return true;
                }
            }
            return false;
        }

		#region Unused

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
		/// Called when a flag changes team
		/// </summary>
		public void onFlagChange(Arena.FlagState flag)
		{	//Does this team now have all the flags?
			Team victoryTeam = flag.team;

			foreach (Arena.FlagState fs in _arena._flags.Values)
				if (fs.bActive && fs.team != victoryTeam)
					victoryTeam = null;

			if (victoryTeam != null)
			{	//Yes! Victory for them!
				_arena.setTicker(1, 1, _config.flag.victoryHoldTime, "Victory in ");
				_tickNextVictoryNotice = _tickVictoryStart = Environment.TickCount;
				_victoryTeam = victoryTeam;
			}
			else
			{	//Aborted?
				if (_victoryTeam != null)
				{
					_tickVictoryStart = 0;
					_victoryTeam = null;

					_arena.sendArenaMessage("Victory has been aborted.", _config.flag.victoryAbortedBong);
					_arena.setTicker(1, 1, 0, "");
				}
			}
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
			return true;
		}

		/// <summary>
		/// Handles a player's produce request
		/// </summary>
		[Scripts.Event("Player.Produce")]
		public bool playerProduce(Player player, Computer computer, VehInfo.Computer.ComputerProduct product)
		{
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
		/// Triggered when a player wants to enter a vehicle
		/// </summary>
		[Scripts.Event("Player.EnterVehicle")]
		public bool playerEnterVehicle(Player player, Vehicle vehicle)
		{
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
			return true;
		}

		/// <summary>
		/// Triggered when a player has died, by any means
		/// </summary>
		/// <remarks>killer may be null if it wasn't a player kill</remarks>
		[Scripts.Event("Player.Death")]
		public bool playerDeath(Player victim, Player killer, Helpers.KillType killType, CS_VehicleDeath update)
		{
			return true;
		}

		/// <summary>
		/// Triggered when one player has killed another
		/// </summary>
		[Scripts.Event("Player.PlayerKill")]
		public bool playerPlayerKill(Player victim, Player killer)
		{
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
			return true;
		}
		#endregion
		#endregion
	}
}