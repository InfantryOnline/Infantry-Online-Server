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
	public partial class Player : IClient, ILocatable
	{	// Member variables
		///////////////////////////////////////////////////
		private Data.PlayerStats _stats;			//The player's total statistics
		private Data.PlayerStats _statsSession;		//The player's total statistics
		private Data.PlayerStats _statsGame;		//The player's statistics for the current game
		private Data.PlayerStats _statsLastGame;	//The player's statistics for the last game


		///////////////////////////////////////////////////
		// Accessors
		///////////////////////////////////////////////////
		#region Stat Accessors
		/// <summary>
		/// Returns the player's statistics
		/// </summary>
		public Data.PlayerStats StatsTotal
		{
			get
			{
				return _stats;
			}
		}

		/// <summary>
		/// Returns the player's statistics for the current session
		/// </summary>
		public Data.PlayerStats StatsCurrentSession
		{
			get
			{
				return _statsSession;
			}
		}

		/// <summary>
		/// Returns the player's statistics for the current game
		/// </summary>
		public Data.PlayerStats StatsCurrentGame
		{
			get
			{
				return _statsGame;
			}
		}

		/// <summary>
		/// Returns the player's statistics for the last game
		/// </summary>
		public Data.PlayerStats StatsLastGame
		{
			get
			{
				return _statsLastGame;
			}
		}

		/// <summary>
		/// The player's cash amount
		/// </summary>
		public int Cash
		{
			get
			{
				return _stats.cash;
			}

			set
			{	//Establish the difference
				int diff = value - _stats.cash;

				_stats.cash = Math.Max(value, 0);

				if (_statsSession != null)
					_statsSession.cash += diff;

				if (_statsGame != null)
					_statsGame.cash += diff;
			}
		}

		/// <summary>
		/// The player's point amount
		/// </summary>
		public long Points
		{
			get
			{
				return _stats.Points;
			}
		}

		/// <summary>
		/// The player's amount of experience remaining to be spent
		/// </summary>
		public int Experience
		{
			get
			{
				return _stats.experience;
			}

			set
			{	//Establish the difference
				int diff = value - _stats.experience;

				_stats.experience = Math.Max(value, 0);
				if (diff > 0)
					_stats.experienceTotal += diff;

				if (_statsSession != null)
				{
					_statsSession.experience += diff;
					if (diff > 0)
						_statsSession.experienceTotal += diff;
				}

				if (_statsGame != null)
				{
					_statsGame.experience += diff;
					if (diff > 0)
						_statsGame.experienceTotal += diff;
				}
			}
		}

		/// <summary>
		/// The player's experience amount
		/// </summary>
		public int ExperienceTotal
		{
			get
			{
				return _stats.experienceTotal;
			}

			set
			{	//Establish the difference
				int diff = value - _stats.experienceTotal;

				_stats.experienceTotal = Math.Max(value, 0);

				if (_statsSession != null)
					_statsSession.experienceTotal += diff;

				if (_statsGame != null)
					_statsGame.experienceTotal += diff;
			}
		}

		/// <summary>
		/// The amount of kills the player has made
		/// </summary>
		public int Kills
		{
			get
			{
				return _stats.kills;
			}

			set
			{	//Establish the difference
				int diff = value - _stats.kills;

				_stats.kills = Math.Max(value, 0);

				if (_statsSession != null)
					_statsSession.kills += diff;

				if (_statsGame != null)
					_statsGame.kills += diff;
			}	
		}

		/// <summary>
		/// The amount of deaths the player has suffered
		/// </summary>
		public int Deaths
		{
			get
			{
				return _stats.deaths;
			}

			set
			{	//Establish the difference
				int diff = value - _stats.deaths;

				_stats.deaths = Math.Max(value, 0);

				if (_statsSession != null)
					_statsSession.deaths += diff;

				if (_statsGame != null)
					_statsGame.deaths += diff;
			}
		}

		/// <summary>
		/// The player's point amount
		/// </summary>
		public int KillPoints
		{
			get
			{
				return _stats.killPoints;
			}

			set
			{	//Establish the difference
				int diff = value - _stats.killPoints;

				_stats.killPoints = value;

				if (_statsSession != null)
					_statsSession.killPoints += diff;

				if (_statsGame != null)
					_statsGame.killPoints += diff;
			}
		}

		/// <summary>
		/// The player's point amount
		/// </summary>
		public int DeathPoints
		{
			get
			{
				return _stats.deathPoints;
			}

			set
			{	//Establish the difference
				int diff = value - _stats.deathPoints;

				_stats.deathPoints = value;

				if (_statsSession != null)
					_statsSession.deathPoints += diff;

				if (_statsGame != null)
					_statsGame.deathPoints += diff;
			}
		}

		/// <summary>
		/// The player's point amount
		/// </summary>
		public int BonusPoints
		{
			get
			{
				return _stats.bonusPoints;
			}

			set
			{	//Establish the difference
				int diff = value - _stats.bonusPoints;

				_stats.bonusPoints = value;

				if (_statsSession != null)
					_statsSession.bonusPoints += diff;

				if (_statsGame != null)
					_statsGame.bonusPoints += diff;
			}
		}

		/// <summary>
		/// The player's point amount
		/// </summary>
		public int AssistPoints
		{
			get
			{
				return _stats.assistPoints;
			}

			set
			{	//Establish the difference
				int diff = value - _stats.assistPoints;

				_stats.assistPoints = value;

				if (_statsSession != null)
					_statsSession.assistPoints += diff;

				if (_statsGame != null)
					_statsGame.assistPoints += diff;
			}
		}

		/// <summary>
		/// The player's point amount
		/// </summary>
		public int PlaySeconds
		{
			get
			{
				return _stats.playSeconds;
			}

			set
			{	//Establish the difference
				int diff = value - _stats.playSeconds;

				_stats.playSeconds = value;

				if (_statsSession != null)
					_statsSession.playSeconds += diff;

				if (_statsGame != null)
					_statsGame.playSeconds += diff;
			}
		}

		/// <summary>
		/// The player's point amount
		/// </summary>
		public int ZoneStat1
		{
			get
			{
				return _stats.zonestat1;
			}

			set
			{	//Establish the difference
				int diff = value - _stats.zonestat1;

				_stats.zonestat1 = value;

				if (_statsSession != null)
					_statsSession.zonestat1 += diff;

				if (_statsGame != null)
					_statsGame.zonestat1 += diff;
			}
		}

		/// <summary>
		/// The player's point amount
		/// </summary>
		public int ZoneStat2
		{
			get
			{
				return _stats.zonestat2;
			}

			set
			{	//Establish the difference
				int diff = value - _stats.zonestat2;

				_stats.zonestat2 = value;

				if (_statsSession != null)
					_statsSession.zonestat2 += diff;

				if (_statsGame != null)
					_statsGame.zonestat2 += diff;
			}
		}

		/// <summary>
		/// The player's point amount
		/// </summary>
		public int ZoneStat3
		{
			get
			{
				return _stats.zonestat3;
			}

			set
			{	//Establish the difference
				int diff = value - _stats.zonestat3;

				_stats.zonestat3 = value;

				if (_statsSession != null)
					_statsSession.zonestat3 += diff;

				if (_statsGame != null)
					_statsGame.zonestat3 += diff;
			}
		}

		/// <summary>
		/// The player's point amount
		/// </summary>
		public int ZoneStat4
		{
			get
			{
				return _stats.zonestat4;
			}

			set
			{	//Establish the difference
				int diff = value - _stats.zonestat4;

				_stats.zonestat4 = value;

				if (_statsSession != null)
					_statsSession.zonestat4 += diff;

				if (_statsGame != null)
					_statsGame.zonestat4 += diff;
			}
		}

		/// <summary>
		/// The player's point amount
		/// </summary>
		public int ZoneStat5
		{
			get
			{
				return _stats.zonestat5;
			}

			set
			{	//Establish the difference
				int diff = value - _stats.zonestat5;

				_stats.zonestat5 = value;

				if (_statsSession != null)
					_statsSession.zonestat5 += diff;

				if (_statsGame != null)
					_statsGame.zonestat5 += diff;
			}
		}

		/// <summary>
		/// The player's point amount
		/// </summary>
		public int ZoneStat6
		{
			get
			{
				return _stats.zonestat6;
			}

			set
			{	//Establish the difference
				int diff = value - _stats.zonestat6;

				_stats.zonestat6 = value;

				if (_statsSession != null)
					_statsSession.zonestat6 += diff;

				if (_statsGame != null)
					_statsGame.zonestat6 += diff;
			}
		}

		/// <summary>
		/// The player's point amount
		/// </summary>
		public int ZoneStat7
		{
			get
			{
				return _stats.zonestat7;
			}

			set
			{	//Establish the difference
				int diff = value - _stats.zonestat7;

				_stats.zonestat7 = value;

				if (_statsSession != null)
					_statsSession.zonestat7 += diff;

				if (_statsGame != null)
					_statsGame.zonestat7 += diff;
			}
		}

		/// <summary>
		/// The player's point amount
		/// </summary>
		public int ZoneStat8
		{
			get
			{
				return _stats.zonestat8;
			}

			set
			{	//Establish the difference
				int diff = value - _stats.zonestat8;

				_stats.zonestat8 = value;

				if (_statsSession != null)
					_statsSession.zonestat8 += diff;

				if (_statsGame != null)
					_statsGame.zonestat8 += diff;
			}
		}

		/// <summary>
		/// The player's point amount
		/// </summary>
		public int ZoneStat9
		{
			get
			{
				return _stats.zonestat9;
			}

			set
			{	//Establish the difference
				int diff = value - _stats.zonestat9;

				_stats.zonestat9 = value;

				if (_statsSession != null)
					_statsSession.zonestat9 += diff;

				if (_statsGame != null)
					_statsGame.zonestat9 += diff;
			}
		}

		/// <summary>
		/// The player's point amount
		/// </summary>
		public int ZoneStat10
		{
			get
			{
				return _stats.zonestat10;
			}

			set
			{	//Establish the difference
				int diff = value - _stats.zonestat10;

				_stats.zonestat10 = value;

				if (_statsSession != null)
					_statsSession.zonestat10 += diff;

				if (_statsGame != null)
					_statsGame.zonestat10 += diff;
			}
		}

		/// <summary>
		/// The player's point amount
		/// </summary>
		public int ZoneStat11
		{
			get
			{
				return _stats.zonestat11;
			}

			set
			{	//Establish the difference
				int diff = value - _stats.zonestat11;

				_stats.zonestat11 = value;

				if (_statsSession != null)
					_statsSession.zonestat11 += diff;

				if (_statsGame != null)
					_statsGame.zonestat11 += diff;
			}
		}

		/// <summary>
		/// The player's point amount
		/// </summary>
		public int ZoneStat12
		{
			get
			{
				return _stats.zonestat12;
			}

			set
			{	//Establish the difference
				int diff = value - _stats.zonestat12;

				_stats.zonestat12 = value;

				if (_statsSession != null)
					_statsSession.zonestat12 += diff;

				if (_statsGame != null)
					_statsGame.zonestat12 += diff;
			}
		}
		#endregion

		///////////////////////////////////////////////////
		// Member functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Causes the player's current game stats to be considered last game, and last game deprecated
		/// </summary>
		public void migrateStats()
		{
			_statsLastGame = (_statsGame == null) ? new InfServer.Data.PlayerStats() : _statsGame;
			_statsGame = new InfServer.Data.PlayerStats();
		}

		/// <summary>
		/// Stops all stats accumulated from this point on from counting
		/// </summary>
		public void suspendStats()
		{
			_suspStats = new Data.PlayerStats(_stats);

			_suspInventory = _inventory;
			_inventory = new Dictionary<int, InventoryItem>();

			foreach (InventoryItem ii in _suspInventory.Values)
			{
				InventoryItem nii = new InventoryItem();

				nii.item = ii.item;
				nii.quantity = ii.quantity;

				_inventory.Add(nii.item.id, nii);
			}

			_suspSkills = _skills;
			_skills = new Dictionary<int, SkillItem>();

			foreach (SkillItem si in _suspSkills.Values)
			{
				SkillItem nsi = new SkillItem();

				nsi.skill = si.skill;
				nsi.quantity = si.quantity;

				_skills.Add(nsi.skill.SkillId, nsi);
			}
		}

		/// <summary>
		/// Restores the suspended stats
		/// </summary>
		public void restoreStats()
		{	//Restore it all!
            //Sanity checks
            if (_suspStats == null)
                return;

            //Retrieve his stats
			_stats = _suspStats;
			_statsSession = new Data.PlayerStats();
            _statsGame = _statsLastGame;
			_statsLastGame = null;

			_inventory = _suspInventory;
			_skills = _suspSkills;

            //Destroy suspended stats
            _suspStats = null;
            _suspInventory = null;
            _suspSkills = null;
		}

		/// <summary>
		/// Clears the player's stats for the current game
		/// </summary>
		public void clearCurrentStats()
		{
			_statsGame = new InfServer.Data.PlayerStats();
		}
	}
}
