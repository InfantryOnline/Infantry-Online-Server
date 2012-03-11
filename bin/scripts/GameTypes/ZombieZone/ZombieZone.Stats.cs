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
			zombieExpLookup.Add(105, 25);		//Hive zombie	
			zombieExpLookup.Add(103, 55);		//Predator zombie
			zombieExpLookup.Add(104, 55);		//Deranged zombie
			zombieExpLookup.Add(106, 60);		//Repulsor zombie
			zombieExpLookup.Add(111, 60);		//Kamikaze zombie
			zombieExpLookup.Add(100, 55);		//Acid zombie
			zombieExpLookup.Add(101, 55);		//Disruptor zombie
			zombieExpLookup.Add(119, 100);		//Sentient zombie
			zombieExpLookup.Add(116, 115);		//Wraith zombie
			zombieExpLookup.Add(120, 120);		//Teleporter zombie
			zombieExpLookup.Add(118, 120);		//Spawner zombie
		}

		/// <summary>
		/// Returns the amount of experience that a zombie is worth
		/// </summary>
		public int getZombieExp(int zombieid)
		{	//Attempt to get the value
			int exp;

			if (!zombieExpLookup.TryGetValue(zombieid, out exp))
				return 0;
			return exp;
		}

		/// <summary>
		/// Returns a random zombie type playable by the player
		/// </summary>
		static public VehInfo getPlayableZombie(Player player)
		{
			List<int> zombies = new List<int>();

			zombies.Add(119);						//Sentient zombie
			if (player.findSkill(116) != null)		//Wraith zombie
				zombies.Add(116);
			if (player.findSkill(118) != null)		//Spawner zombie
				zombies.Add(118);
			if (player.findSkill(120) != null)		//Teleporter zombie
				zombies.Add(120);

			return AssetManager.Manager.getVehicleByID(zombies[player._arena._rand.Next(zombies.Count)]);
		}


		/// <summary>
		/// Gets the skill rating for killing the given zombie
		/// </summary>
		static public int getKillSkillRating(int zombieid)
		{
			if (zombieid == 205)		//King zombie
				return 10;
			if (zombieid == 211)		//Alien zombie
				return 1;
			if (zombieid == 109)		//Suicide zombie
				return 2;
			if (zombieid == 108)		//Ranged zombie
				return 2;
			if (zombieid == 105)		//Hive zombie
				return 0;
			if (zombieid == 103)		//Predator zombie
				return 2;
			if (zombieid == 104)		//Deranged zombie
				return 2;
			if (zombieid == 106)		//Repulsor zombie
				return 3;
			if (zombieid == 111)		//Kamikaze zombie
				return 2;
			if (zombieid == 100)		//Acid zombie
				return 2;
			if (zombieid == 101)		//Disruptor zombie
				return 2;
			if (zombieid == 119)		//Sentient zombie
				return 3;
			if (zombieid == 116)		//Wraith zombie
				return 4;
			if (zombieid == 120)		//Teleporter zombie
				return 4;
			if (zombieid == 118)		//Spawner zombie
				return 4;

			return 0;
		}

		/// <summary>
		/// Gets the skill rating for a kill with the given weapon
		/// </summary>
		static public int getWeaponSkillRating(int weaponid)
		{	//Marine innate weapons
			if (weaponid == 1062)		//Demopack
				return 1;
			if (weaponid == 1126)		//Gas projector lvl1
				return 5;
			if (weaponid == 1081)		//Gas projector lvl2
				return 4;
			if (weaponid == 1082)		//Gas projector lvl3
				return 3;
			if (weaponid == 306)		//Incinerator
				return 4;
			if (weaponid == 1134)		//Machine pistol
				return 5;
			if (weaponid == 1021)		//Recoilless rifle
				return 1;
			if (weaponid == 1083 || weaponid == 1079 || weaponid == 1080)		//RPG
				return 4;
			if (weaponid == 1001)		//Assault rifle
				return 5;
			if (weaponid == 1107)		//Assault rifle+
				return 4;
			if (weaponid == 1002)		//Auto cannon
				return 2;
			if (weaponid == 1014)		//AP mine
				return 1;
			if (weaponid == 1106)		//Rifle grenade
				return 4;
			if (weaponid == 1102 || weaponid == 1012)	//Shotgun
				return 3;
			if (weaponid == 1174)		//Chainfist
				return 6;
			if (weaponid == 1019)		//Battle rifle
				return 5;
			if (weaponid == 1049)		//Mini nuke
				return -1;
			if (weaponid == 1173)		//Powerfist
				return 6;

			//Item drops
			if (weaponid == 1011)		//Frag grenade
				return 1;
			if (weaponid == 1321)		//Molotov cocktail
				return 1;
			if (weaponid == 1015)		//Railgun
				return 0;
			if (weaponid == 1008)		//Thermal lance
				return 0;
			if (weaponid == 1099)		//Machine gun
				return 0;

			//Turret weapons
			if (weaponid == 1259)		//PDB AC
				return 2;
			if (weaponid == 1094)		//PDB MG
				return 2;
			if (weaponid == 1095)		//PDB RPG
				return 2;
			if (weaponid == 1361)		//PDB Mortar
				return 1;
			if (weaponid == 3053)		//PDF Mortar incin
				return 2;

			return 0;
		}
	}
}
