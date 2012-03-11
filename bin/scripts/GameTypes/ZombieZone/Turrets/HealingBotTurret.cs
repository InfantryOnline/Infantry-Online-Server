using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Game;
using InfServer.Protocol;
using InfServer.Scripting;
using InfServer.Bots;

using Assets;

namespace InfServer.Script.GameType_ZombieZone
{
	// HealingBotTurret Class
	/// A healing turret which shoots at teammates
	///////////////////////////////////////////////////////
	public class HealingBotTurret : Computer
	{	// Member variables
		///////////////////////////////////////////////////


		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Generic constructor
		/// </summary>
		public HealingBotTurret(VehInfo.Computer type, Arena arena)
			: base(type, arena)
		{
		}

		/// <summary>
		/// Let's find a good teammate to attack!
		/// </summary>		
		protected override bool isValidTarget(Player p)
		{
			// Don't fire at spectators
			if (p.IsSpectator) return false;

			// Fire at teammates
			if (p._team != _team) return false;

			// Don't fire at dead people
			if (p.IsDead) return false;

			// Don't fire at people in a vehicle
			if (p._occupiedVehicle != null) return false;

			// Don't fire at people with full health
			if (p._baseVehicle._type.Hitpoints == p._state.health) return false;

			// Don't fire at people outside of turret's firing range
			if (!InRange(p)) return false;

			//Don't fire at people outside of our weight limits.
			int pWeight = p.ActiveVehicle._type.Weight;
			bool trackPlayer = (pWeight >= _type.TrackingWeightLow && pWeight <= _type.TrackingWeightHigh);
			if (!trackPlayer)
				return false;

			// TODO: do not fire at people out of turret angle limits

			// TODO: do not fire at people out of LOS (complicated calculation)
			if (IsPlayerOccluded(p)) return false;

			return true;
		}
	}
}
