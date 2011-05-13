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
		private Data.PlayerStats _statsGame;		//The player's statistics for the current game
		private Data.PlayerStats _statsLastGame;	//The player's statistics for the last game


		///////////////////////////////////////////////////
		// Accessors
		///////////////////////////////////////////////////
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

				if (_statsGame != null)
					_statsGame.cash += diff;
			}
		}

		/// <summary>
		/// The player's point amount
		/// </summary>
		public int Points
		{
			get
			{
				return _stats.points;
			}

			set
			{	//Establish the difference
				int diff = value - _stats.points;

				_stats.points = value;

				if (_statsGame != null)
					_statsGame.points += diff;
			}
		}

		/// <summary>
		/// The player's amount of experience remaining to be sent
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
				_stats.experienceTotal += diff;

				if (_statsGame != null)
				{
					_statsGame.experience += diff;
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

				if (_statsGame != null)
					_statsGame.playSeconds += diff;
			}
		}

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
		/// Clears the player's stats for the current game
		/// </summary>
		public void clearCurrentStats()
		{
			_statsGame = new InfServer.Data.PlayerStats();
		}
	}
}
