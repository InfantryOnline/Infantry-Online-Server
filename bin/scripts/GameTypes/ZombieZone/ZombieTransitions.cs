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

		public ZombieType(Type _classType, VehInfo _vehicleType)
		{
			vehicleType = _vehicleType;
			classType = _classType;
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
		private int tickNextTransition;	


		///////////////////////////////////////////////////
		// Member Classes
		///////////////////////////////////////////////////
		/// <summary>
		/// Represents the zombie composition of a snapshot in game time
		/// </summary>
		public class ZombieTransition
		{
			public bool bWave;									//Is this transition just a wave of zombies? If so, we just spawn the given zombies
			public int zombieWaveThreshold;						//The zombie population to wait for before we start the wave
			public Action<ZombieTransition> thresholdReached;	//Triggered when the population threshold has been reached

			public List<ZombieType> types;						//The types of zombies present in the transition with their corresponding spawn weights
			public int gameTime;								//The amount of time this transition lasts for

			public float zombieCountMod;						//The maximum zombie count modifier while this transition is in effect
			public bool bPauseZombieAdd;						//Should we not increase the zombie count while this transition is in place?

			public ZombieTransition(Action<ZombieTransition> thresholdCallback)
			{
				thresholdReached = thresholdCallback;
				types = new List<ZombieType>();
				zombieCountMod = 1.0f;
			}

			public void addType(ZombieType type, int spawnWeight)
			{
				ZombieType newType = new ZombieType(type.classType, type.vehicleType);
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

			//Obtain our zombie types
			ZombieType AlienZombie = new ZombieType(typeof(ZombieBot), AssetManager.Manager.getVehicleByID(211));
			ZombieType SuicideZombie = new ZombieType(typeof(SuicideZombieBot), AssetManager.Manager.getVehicleByID(109));
			ZombieType RangedZombie = new ZombieType(typeof(RangedZombieBot), AssetManager.Manager.getVehicleByID(108));
			ZombieType PredatorZombie = new ZombieType(typeof(ZombieBot), AssetManager.Manager.getVehicleByID(103));

			//Create our list of transitions
			ZombieTransition trans = new ZombieTransition(transitionStarted);

			//Start! just pure melee zombie cannon fodder
			trans = new ZombieTransition(transitionStarted);
			trans.gameTime = 90;
			trans.addType(AlienZombie, 1);
			transitions.Add(trans);

			//.. adding a little suicidal fun
			trans = new ZombieTransition(transitionStarted);
			trans.gameTime = 90;
			trans.addType(AlienZombie, 4);
			trans.addType(SuicideZombie, 1);
			transitions.Add(trans);

			//INSTANT MELEE ATTACK
			trans = new ZombieTransition(transitionStarted);
			trans.gameTime = 30;
			trans.bWave = true;
			trans.bPauseZombieAdd = true;
			trans.zombieWaveThreshold = 1;
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
			trans.gameTime = 25;
			trans.bWave = true;
			trans.bPauseZombieAdd = true;
			trans.zombieWaveThreshold = 2;
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
			trans.gameTime = 40;
			trans.bWave = true;
			trans.bPauseZombieAdd = true;
			trans.zombieWaveThreshold = 5;
			trans.addType(AlienZombie, 3);
			transitions.Add(trans);

			//.. mass suicide/ranged invasion!
			trans = new ZombieTransition(transitionStarted);
			trans.gameTime = 80;
			trans.zombieCountMod = 1.4f;
			trans.addType(RangedZombie, 1);
			trans.addType(SuicideZombie, 1);
			transitions.Add(trans);

			//.. predators with range!
			trans = new ZombieTransition(transitionStarted);
			trans.gameTime = 80;
			trans.zombieCountMod = 0.7f;
			trans.addType(PredatorZombie, 2);
			trans.addType(RangedZombie, 2);
			transitions.Add(trans);

			//INSTANT PREDATOR ATTACK!
			trans = new ZombieTransition(transitionStarted);
			trans.gameTime = 45;
			trans.bWave = true;
			trans.bPauseZombieAdd = true;
			trans.zombieWaveThreshold = 4;
			trans.addType(PredatorZombie, 1);
			trans.addType(RangedZombie, 1);
			transitions.Add(trans);

			//.. balanced mix!
			trans = new ZombieTransition(transitionStarted);
			trans.gameTime = 80;
			trans.addType(PredatorZombie, 1);
			trans.addType(AlienZombie, 2);
			trans.addType(RangedZombie, 1);
			trans.addType(SuicideZombie, 1);
			transitions.Add(trans);

			//Do a bit of ordering
			transitions.OrderBy(t => t.gameTime);
		}

		/// <summary>
		/// Returns a new transition to make, if any
		/// </summary>
		public ZombieTransition getNewTransition(int tickCount)
		{	//Is it time to goto the next transition?
			if (tickNextTransition > tickCount)
				return null;

			//Are we on the last?
			if (transIdx + 1 >= transitions.Count)
				return null;

			ZombieTransition trans = transitions[++transIdx];

			//Yes! Do we wait for a wave threshold first?
			if (trans.zombieWaveThreshold > 0)
				tickNextTransition = int.MaxValue;
			else
				tickNextTransition = tickCount + (trans.gameTime * 1000);

			//Return whatever we found
			return trans;
		}

		/// <summary>
		/// Starts the lastgame timer
		/// </summary>
		public void transitionStarted(ZombieTransition t)
		{
			tickNextTransition = Environment.TickCount + (t.gameTime * 1000);
		}
	}
}
