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
{	// ZombieZoneStats Class
	/// Contains lots of settings and variables relevant to zombiezone
	///////////////////////////////////////////////////////
	public class ZombieZoneStats
	{
		private Dictionary<int, int> zombieExpLookup;

		/// <summary>
		/// Constructor
		/// </summary>
		public ZombieZoneStats()
		{
			zombieExpLookup = new Dictionary<int, int>();

			zombieExpLookup.Add(205, 250);		//King zombie

			zombieExpLookup.Add(211, 35);		//Alien zombie
			zombieExpLookup.Add(109, 40);		//Suicide zombie
			zombieExpLookup.Add(108, 45);		//Ranged zombie	
			zombieExpLookup.Add(103, 55);		//Predator zombie
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public int getZombieExp(int zombieid)
		{	//Attempt to get the value
			int exp;

			if (!zombieExpLookup.TryGetValue(zombieid, out exp))
				return 0;
			return exp;
		}
	}
}
