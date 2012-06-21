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
	public class EnergizerBotTurret : Computer
	{	// Member variables
		///////////////////////////////////////////////////
    int energyPerCharge = 200;
    
    public const bool shootAtVehicles = false;      //do we fire at players in vehicles?
    public const bool chargeInVehicles = false;        //do we charge players in vehicles?
    
    public const bool shootOccludedPlayers = false; //do we fire at occluded players?
    public const bool chargeOccludedPlayers = false;   //do we charge occluded players?

		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Generic constructor
		/// </summary>
		public EnergizerBotTurret(VehInfo.Computer type, Arena arena)
			: base(type, arena)
		{
		}
		
		public override bool poll()
		{
			  bool returnPacket = base.poll();  //base returns whether we should return packet, also modifies _shouldFire
		    
		    if(_shouldFire)
		    {
		        foreach (Player p in _team.ActivePlayers.ToList())
		            if (InRange(p) && (chargeInVehicles || p._occupiedVehicle == null) && (chargeOccludedPlayers || !IsPlayerOccluded(p)) )
		                p.inventoryModify(AssetManager.Manager.getItemByName("Energy"), energyPerCharge);    

		    }
		    
		    return returnPacket;
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
			if (!shootAtVehicles && p._occupiedVehicle != null) return false;

			// Don't fire at people with full health
			if (p._baseVehicle._type.EnergyMax == p._state.energy) return false;

			// Don't fire at people outside of turret's firing range
			if (!InRange(p)) return false;

			//Don't fire at people outside of our weight limits.
			int pWeight = p.ActiveVehicle._type.Weight;
			bool trackPlayer = (pWeight >= _type.TrackingWeightLow && pWeight <= _type.TrackingWeightHigh);
			if (!trackPlayer)
				return false;

			// TODO: do not fire at people out of turret angle limits

			// do not fire at people out of LOS (complicated calculation)
			if (!shootOccludedPlayers && IsPlayerOccluded(p)) return false;

			return true;
		}
	}
}
