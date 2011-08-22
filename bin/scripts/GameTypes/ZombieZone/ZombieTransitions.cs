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

namespace InfServer.Script.GameType_ZombieZone
{	/// <summary>
	/// Represents a type of zombie, including the amount it should spawn
	/// </summary>
	public class ZombieType
	{
		public VehInfo vehicleType;
		public Type classType;
		public int spawnWeight;

		public Action<ZombieBot> zombieSetup;

		public ZombieType(Type _classType, VehInfo _vehicleType)
		{
			vehicleType = _vehicleType;
			classType = _classType;
		}

		public ZombieType(Type _classType, VehInfo _vehicleType, Action<ZombieBot> _zombieSetup)
		{
			vehicleType = _vehicleType;
			classType = _classType;
			zombieSetup = _zombieSetup;
		}
	}

	/// <summary>
	/// Allows the zone to make smooth transitions between zombie types
	/// </summary>
	public class ZombieTransitions 
	{	///////////////////////////////////////////////////
		// Member Variables
		///////////////////////////////////////////////////
		private List<ZombieTransition> transitions;

		private int transIdx;
		private int tickStartPause;
		private int tickNextTransition;

		#region Zombie Types
		static public ZombieType AlienZombie = new ZombieType(typeof(ZombieBot), AssetManager.Manager.getVehicleByID(211));
		static public ZombieType SuicideZombie = new ZombieType(typeof(SuicideZombieBot), AssetManager.Manager.getVehicleByID(109));
		static public ZombieType RangedZombie = new ZombieType(typeof(RangedZombieBot), AssetManager.Manager.getVehicleByID(108),
			delegate(ZombieBot zombie)
			{
				RangedZombieBot z = zombie as RangedZombieBot;
				z.farDist = 3.4f; z.shortDist = 2.6f; z.runDist = 2.0f;
			}
		);
		static public ZombieType HiveZombie = new ZombieType(typeof(ZombieBot), AssetManager.Manager.getVehicleByID(105));
		static public ZombieType PredatorZombie = new ZombieType(typeof(ZombieBot), AssetManager.Manager.getVehicleByID(103));
		static public ZombieType DerangedZombie = new ZombieType(typeof(RangedZombieBot), AssetManager.Manager.getVehicleByID(104),
			delegate(ZombieBot zombie)
			{
				RangedZombieBot z = zombie as RangedZombieBot;
				z.farDist = 3.4f; z.shortDist = 2.6f; z.runDist = 2.0f;
			}
		);
		static public ZombieType RepulsorZombie = new ZombieType(typeof(DualZombieBot), AssetManager.Manager.getVehicleByID(106),
			delegate(ZombieBot zombie)
			{
				DualZombieBot z = zombie as DualZombieBot;
				z.wepSwitchDist = 2.0f;
				z.bNoAimFar = true;
			}
		);
		static public ZombieType KamikazeZombie = new ZombieType(typeof(SuicideZombieBot), AssetManager.Manager.getVehicleByID(111));
		static public ZombieType AcidZombie = new ZombieType(typeof(DualZombieBot), AssetManager.Manager.getVehicleByID(100),
			delegate(ZombieBot zombie)
			{
				DualZombieBot z = zombie as DualZombieBot;
				z.wepSwitchDist = 1.5f;
			}
		);
		static public ZombieType DisruptorZombie = new ZombieType(typeof(RangedZombieBot), AssetManager.Manager.getVehicleByID(101),
			delegate(ZombieBot zombie)
			{
				RangedZombieBot z = zombie as RangedZombieBot;
				z.farDist = 3.4f; z.shortDist = 2.6f; z.runDist = 2.0f;
			}
		);
		#endregion Zombie Types

		///////////////////////////////////////////////////
		// Member Classes
		///////////////////////////////////////////////////
		/// <summary>
		/// Represents the zombie composition of a snapshot in game time
		/// </summary>
		public class ZombieTransition
		{
			public int resetZombieCountTo;						//Resets the zombie count to a specified number
			public Action<Script_ZombieZone.TeamState> started;	//Called when the transition is started

			public bool bWave;									//Is this transition just a wave of zombies? If so, we just spawn the given zombies

			public bool bStarted;								//Has this transition started, or are we waiating on the threshold?
			public int zombiePopThreshold;						//The zombie population to wait for before we start the wave
			public Action<ZombieTransition> thresholdReached;	//Triggered when the population threshold has been reached

			public List<ZombieType> types;						//The types of zombies present in the transition with their corresponding spawn weights
			public int gameTime;								//The amount of time this transition lasts for
			public int pauseTime;								//The time between transitions where no zombies are spawned at all

			public float zombieCountMod;						//The maximum zombie count modifier while this transition is in effect
			public bool bPauseZombieAdd;						//Should we not increase the zombie count while this transition is in place?

			public ZombieTransition(Action<ZombieTransition> thresholdCallback)
			{
				thresholdReached = thresholdCallback;
				types = new List<ZombieType>();

				zombieCountMod = 1.0f;
				pauseTime = 15000;
			}

			public void addType(ZombieType type, int spawnWeight)
			{
				ZombieType newType = new ZombieType(type.classType, type.vehicleType);

				newType.zombieSetup = type.zombieSetup;
				newType.spawnWeight = spawnWeight;

				types.Add(newType);
			}

			public ZombieType getRandomType(Random rand)
			{
				int maxWeight = 0;

				foreach (ZombieType zt in types)
					maxWeight += zt.spawnWeight;

				int chosen = rand.Next(maxWeight + 1);

				foreach (ZombieType zt in types)
				{
					chosen -= zt.spawnWeight;
					if (chosen <= 0)
						return zt;
				}

				Log.write(TLog.Error, "ZombieTransition getType fell through.");
				return types[0];
			}

			/*public ZombieType getRandomPlayerType(Random rand)
			{	//Dont allow the player to spawn as a suicide zombie
				int maxWeight = 0;

				foreach (ZombieType zt in types)
				{
					if (ZombieZoneStats.isPlayableZombie(zt.vehicleType.Id))
						maxWeight += zt.spawnWeight;
				}

				int chosen = rand.Next(maxWeight + 1);

				foreach (ZombieType zt in types)
				{
					if (!ZombieZoneStats.isPlayableZombie(zt.vehicleType.Id))
						continue;

					chosen -= zt.spawnWeight;
					if (chosen <= 0)
						return zt;
				}

				//If no zombies are suitable, use the alien zombie
				if (!ZombieZoneStats.isPlayableZombie(types[0].vehicleType.Id))
					return AlienZombie;

				return types[0];
			}*/
		}

		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Constructor
		/// </summary>
		public ZombieTransitions()
		{	//Start with no transition!
			transIdx = -1;
			transitions = new List<ZombieTransition>();

			addPhase1(transitions);
			addPhase2(transitions);
			addPhase3(transitions);
		}

		#region Transition setup
		/// <summary>
		/// Returns a new transition to make, if any
		/// </summary>
		public void addPhase1(List<ZombieTransition> transitions)
		{	//Start! just pure melee zombie cannon fodder
			ZombieTransition trans = new ZombieTransition(transitionStarted);
			trans.gameTime = 70;
			trans.addType(AlienZombie, 1);
			transitions.Add(trans);

			//.. adding a little suicidal fun
			trans = new ZombieTransition(transitionStarted);
			trans.gameTime = 70;
			trans.addType(AlienZombie, 4);
			trans.addType(SuicideZombie, 1);
			transitions.Add(trans);

			//INSTANT MELEE ATTACK
			trans = new ZombieTransition(transitionStarted);
			trans.gameTime = 15;
			trans.bWave = true;
			trans.bPauseZombieAdd = true;
			trans.zombiePopThreshold = 1;
			trans.addType(AlienZombie, 2);
			transitions.Add(trans);

			//.. a little ranged in there
			trans = new ZombieTransition(transitionStarted);
			trans.gameTime = 80;
			trans.addType(AlienZombie, 4);
			trans.addType(SuicideZombie, 1);
			trans.addType(RangedZombie, 2);
			transitions.Add(trans);

			//INSTANT SUICIDE ATTACK
			trans = new ZombieTransition(transitionStarted);
			trans.gameTime = 15;
			trans.bWave = true;
			trans.bPauseZombieAdd = true;
			trans.zombiePopThreshold = 2;
			trans.addType(SuicideZombie, 2);
			transitions.Add(trans);

			//.. predators and suicides!
			trans = new ZombieTransition(transitionStarted);
			trans.gameTime = 80;
			trans.addType(PredatorZombie, 1);
			trans.addType(AlienZombie, 3);
			trans.addType(SuicideZombie, 1);
			transitions.Add(trans);

			//INSTANT MELEE INVASION
			trans = new ZombieTransition(transitionStarted);
			trans.gameTime = 15;
			trans.bWave = true;
			trans.bPauseZombieAdd = true;
			trans.zombiePopThreshold = 5;
			trans.addType(AlienZombie, 3);
			transitions.Add(trans);

			//.. hive swarm
			trans = new ZombieTransition(transitionStarted);
			trans.gameTime = 50;
			trans.zombieCountMod = 1.25f;
			trans.addType(HiveZombie, 1);
			transitions.Add(trans);

			//.. slow ranged and suicide combo!
			trans = new ZombieTransition(transitionStarted);
			trans.gameTime = 50;
			trans.addType(RepulsorZombie, 1);
			trans.addType(PredatorZombie, 1);
			trans.addType(DerangedZombie, 1);
			trans.addType(SuicideZombie, 2);
			transitions.Add(trans);
		}

		/// <summary>
		/// Returns a new transition to make, if any
		/// </summary>
		public void addPhase2(List<ZombieTransition> transitions)
		{	//Start with a few nasty kamekazi zombies
			ZombieTransition trans = new ZombieTransition(transitionStarted);
			trans.gameTime = 50;
			trans.zombiePopThreshold = 3;
			trans.resetZombieCountTo = 3;
			trans.started = delegate(Script_ZombieZone.TeamState state)
			{
				state.team.sendArenaMessage("The zombies are regrouping, prepare yourself!");
			};
			trans.addType(KamikazeZombie, 1);
			trans.addType(DerangedZombie, 1);
			transitions.Add(trans);

			//.. adding a little melee to the kamekazi action
			trans = new ZombieTransition(transitionStarted);
			trans.gameTime = 90;
			trans.addType(KamikazeZombie, 1);
			trans.addType(AlienZombie, 1);
			trans.addType(AcidZombie, 1);
			transitions.Add(trans);

			//.. switch the suicide with some range!
			trans = new ZombieTransition(transitionStarted);
			trans.gameTime = 80;
			trans.addType(DerangedZombie, 1);
			trans.addType(AlienZombie, 2);
			trans.addType(AcidZombie, 2);
			transitions.Add(trans);

			//.. a wave!
			trans = new ZombieTransition(transitionStarted);
			trans.gameTime = 30;
			trans.bWave = true;
			trans.bPauseZombieAdd = true;
			trans.zombiePopThreshold = 3;
			trans.addType(KamikazeZombie, 1);
			trans.addType(AcidZombie, 1);
			trans.addType(AlienZombie, 1);
			trans.addType(DisruptorZombie, 1);
			transitions.Add(trans);

			//.. throw a nasty hive/acid zombie swarm in
			trans = new ZombieTransition(transitionStarted);
			trans.gameTime = 60;
			trans.zombieCountMod = 1.3f;
			trans.addType(HiveZombie, 3);
			trans.addType(AcidZombie, 1);
			transitions.Add(trans);

			//.. add a little ranged to it
			trans = new ZombieTransition(transitionStarted);
			trans.gameTime = 60;
			trans.zombieCountMod = 1.3f;
			trans.addType(HiveZombie, 4);
			trans.addType(AcidZombie, 1);
			trans.addType(DerangedZombie, 1);
			trans.addType(DisruptorZombie, 1);
			transitions.Add(trans);

			//.. nasty repulsor wave!
			trans = new ZombieTransition(transitionStarted);
			trans.gameTime = 20;
			trans.bWave = true;
			trans.bPauseZombieAdd = true;
			trans.zombiePopThreshold = 4;
			trans.addType(RepulsorZombie, 1);
			trans.addType(AcidZombie, 1);
			trans.addType(DerangedZombie, 1);
			transitions.Add(trans);

			//.. a large, balanced army
			trans = new ZombieTransition(transitionStarted);
			trans.gameTime = 70;
			trans.addType(AcidZombie, 2);
			trans.addType(RepulsorZombie, 1);
			trans.addType(DisruptorZombie, 1);
			trans.addType(RepulsorZombie, 1);
			trans.addType(DerangedZombie, 1);
			trans.addType(KamikazeZombie, 1);
			transitions.Add(trans);

			//.. we've made it.. for now!
			trans = new ZombieTransition(transitionStarted);
			trans.gameTime = 30;
			trans.resetZombieCountTo = 3;
			trans.zombiePopThreshold = 4;
			trans.started = delegate(Script_ZombieZone.TeamState state)
			{
				state.team.sendArenaMessage("The zombies are regrouping, prepare yourself!");
			};

			trans.addType(AcidZombie, 1);
			transitions.Add(trans);
		}

		/// <summary>
		/// Returns a new transition to make, if any
		/// </summary>
		public void addPhase3(List<ZombieTransition> transitions)
		{	//Start with a few nasty kamekazi zombies
			ZombieTransition trans = new ZombieTransition(transitionStarted);
			trans.gameTime = 60;
			trans.bWave = true;
			trans.bPauseZombieAdd = true;
			trans.addType(DerangedZombie, 20);
			trans.addType(KamikazeZombie, 20);
			trans.started = delegate(Script_ZombieZone.TeamState state)
			{
				state.team.sendArenaMessage("aaerox hasn't coded this far. Die now!");
			};
			transitions.Add(trans);

			trans = new ZombieTransition(transitionStarted);
			trans.gameTime = 120;
			trans.resetZombieCountTo = 50;
			trans.addType(DerangedZombie, 1);
			trans.addType(KamikazeZombie, 1);
			transitions.Add(trans);
		}
		#endregion

		/// <summary>
		/// Returns a new transition to make, if any
		/// </summary>
		public ZombieTransition getNewTransition(int tickCount, out bool bPause)
		{	//Start pausing?
			bPause = (tickCount > tickStartPause);

			//Is it time to goto the next transition?
			if (tickNextTransition > tickCount)
				return null;

			//Are we on the last?
			if (transIdx + 1 >= transitions.Count)
				return null;

			ZombieTransition trans = transitions[++transIdx];

			//Yes! Do we wait for a wave threshold first?
			if (trans.zombiePopThreshold > 0)
			{
				tickNextTransition = int.MaxValue;
				tickStartPause = int.MaxValue;
			}
			else
			{
				tickNextTransition = tickCount + (trans.gameTime * 1000) + trans.pauseTime;
				tickStartPause = tickNextTransition - trans.pauseTime;
			}

			//Return whatever we found
			return trans;
		}

		/// <summary>
		/// Delays the next transition by the specified amount
		/// </summary>
		public void delayTransition(int ticks)
		{
			tickNextTransition += ticks;
			tickStartPause += ticks;
		}

		/// <summary>
		/// Starts the lastgame timer
		/// </summary>
		public void transitionStarted(ZombieTransition t)
		{
			tickNextTransition = Environment.TickCount + (t.gameTime * 1000) + t.pauseTime;
			tickStartPause = tickNextTransition - t.pauseTime;
		}
	}
}
