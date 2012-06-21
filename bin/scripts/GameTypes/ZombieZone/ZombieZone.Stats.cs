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
        private Dictionary<int, int> zombieSkillRatings;

        public static int c_fallbackVehicle = 600;

        //player zombies
        private static int[,] playerZombStats = new int[,]{
                {600,100,3},    //Sentient zombie
                {605,100,3},    //Ambusher Zombie
                {610,80,4},      //Ammo-Eater zombie
                {615,120,4},    //Fire zombie
                {620,150,5},    //ForceField zombie
                {625,150,5},    //Hopper zombie
                {630,120,4},    //Spawner zombie
                {635,150,5},    //Teleporter zombie
                {640,115,4},    //Wraith zombie
                {645,120,3}     //Infected PlayerZombie
        };

        /// <summary>
        /// Constructor
        /// </summary>
        public ZombieZoneStats()
        {
            zombieExpLookup = new Dictionary<int, int>();
            zombieSkillRatings = new Dictionary<int, int>();

            for (int i = 0; i < 10; i++)
                addZombieStats(200 + i, 150 + 45 * i, 6 + 2 * i); //King zombies


            addZombieStats(211, 35, 1);		//Alien zombie
            addZombieStats(109, 40, 2);		//Suicide zombie
            addZombieStats(108, 45, 2);		//Ranged zombie	
            addZombieStats(105, 25, 0);		//Hive zombie
            addZombieStats(123, 50, 1);		//Lair zombie
            addZombieStats(103, 55, 2);		//Predator zombie
            addZombieStats(104, 55, 2);		//Deranged zombie
            addZombieStats(106, 60, 3);		//Repulsor zombie
            addZombieStats(111, 60, 2);		//Kamikaze zombie
            addZombieStats(122, 90, 4);		//Zombie of Death
            addZombieStats(124, 90, 4);		//Zombie of Doom
            addZombieStats(125, 90, 4);		//Zombie of Rage
            addZombieStats(100, 55, 2);		//Acid zombie
            addZombieStats(101, 55, 2);		//Disruptor zombie

            addZombieStats(256, 50, 2);		//Infected zombies
            addZombieStats(257, 70, 3);
            addZombieStats(258, 90, 4);

            addZombieStats(250, 45, 1);		//Human zombie
            addZombieStats(251, 60, 2);		//Asgardian zombie
            addZombieStats(252, 90, 4);		//Kryptonian zombie

            addZombieStats(150, 250, 8);  //Nightmare zombie

            //adds all 5 levels for each player zombie
            for (int i = 0; i < playerZombStats.GetLength(0); i++)
                for (int j = 0; j < 5; j++)
                    addZombieStats(playerZombStats[i, 0] + j, playerZombStats[i, 1] + 15 * j, playerZombStats[i, 2] + j);

        }

        private void addZombieStats(int id, int exp = 0, int skill = 0)
        {
            zombieExpLookup.Add(id, exp);
            zombieSkillRatings.Add(id, skill);
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
        /// Returns a random zombie id playable by the player
        /// </summary>
        public static int getPlayableZombieID(Player player)
        {
            List<int> zombies = new List<int>();

            //adds players zombie skills to the possibilities list
            for (int i = 0; i < playerZombStats.GetLength(0); i++)
            {
                int zombid = playerZombStats[i, 0];

                if (zombid == 600 || zombid == 605)   //the ones that don't require skill
                    zombies.Add(zombid);
                else if (player.findSkill(zombid) != null)  //if he has the skill
                    zombies.Add(zombid);
            }

            if (player != null && player._arena != null && player._arena._rand != null)
                return zombies[player._arena._rand.Next(zombies.Count)];
            else
                return zombies[0];
        }

        /// <summary>
        /// Returns a random zombie type playable by the player
        /// </summary>
        static public VehInfo getPlayableZombie(Player player)
        {
            int vehid = getPlayableZombieID(player);
            VehInfo veh = AssetManager.Manager.getVehicleByID(vehid);

            if (veh == null)
            {
                veh = AssetManager.Manager.getVehicleByID(c_fallbackVehicle);
                Log.write(TLog.Error, "getPlayableZombie: Attempted to choose non-existent vehicle " + vehid);
            }

            return veh;
        }


        /// <summary>
        /// Gets the skill rating for killing the given zombie
        /// </summary>
        public int getKillSkillRating(int zombieid)
        {
            int skill;

            if (!zombieSkillRatings.TryGetValue(zombieid, out skill))
                return 0;
            return skill;
        }

        /// <summary>
        /// Gets the skill rating for a kill with the given weapon
        /// </summary>
        static public int getWeaponSkillRating(int weaponid)
        {	//Marine innate weapons
            if (weaponid == 1062 || weaponid == 1014 || weaponid == 1094)		//Demopack, AP Mine, Kuchler ASC MG
                return 1;
            if (weaponid == 1126)		//Gas projector lvl1
                return 5;
            if (weaponid == 1081)		//Gas projector lvl2
                return 4;
            if (weaponid == 1082)		//Gas projector lvl3
                return 3;
            if (weaponid == 306)		//Incinerator

                return 3;
            if (weaponid == 1134)		//Machine pistol
                return 4;
            if (weaponid == 1021 || weaponid == 1170 || weaponid == 1171)		//Recoilless rifle
                return 1;
            if (weaponid == 1083 || weaponid == 1079 || weaponid == 1080)		//RPG
                return 4;
            if (weaponid == 1001)		//Assault rifle
                return 5;
            if (weaponid == 1107)		//Assault rifle+
                return 4;
            if (weaponid == 1002 || weaponid == 1050 || weaponid == 1051 || weaponid == 1052)		//Auto cannon, MML
                return 2;
            if (weaponid == 1106 || weaponid == 1129 || weaponid == 1125)	//Rifle grenade, Energy pistol
                return 4;
            if (weaponid == 1102 || weaponid == 1012)	//Shotgun
                return 3;
            if (weaponid == 1019)		//Battle rifle
                return 5;
            if (weaponid == 1049)		//Mini nuke
                return -1;
            if (weaponid == 1173 || weaponid == 1174)		//Powerfist, Chainfist
                return 6;
            if (weaponid == 1300 || weaponid == 1167)       //Maser, Hand Maser
                return 5;
            if (weaponid == 1150 || weaponid == 1153 || weaponid == 1154)   //Phantom MK47,+,++
                return 7;

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
