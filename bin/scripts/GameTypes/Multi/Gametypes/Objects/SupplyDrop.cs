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

namespace InfServer.Script.GameType_Multi
{
    public class SupplyDrop
    {
        public Team _team;      //The team we belong to\
        public Arena _arena;
        public short posX;
        public short posY;
        public string _name;
        public Computer _computer;
        private Random _rand;
        private int tickLastPing;
        private bool bOpened;

        public SupplyDrop(Team team, Computer computer, short pX, short pY)
        {
            _arena = team._arena;
            _team = team;
            posX = pX;
            posY = pY;
            _computer = computer;
            _name = Helpers.posToLetterCoord(posX, posY);
            _rand = new Random();
        }


        public void poll(int now)
        {
            if (now - tickLastPing >= 2000 && !bOpened)
            {
                Helpers.Player_RouteExplosion(_team.ActivePlayers, 3060, posX, posY, 0, 0, 0);
                tickLastPing = now;
            }
        }


        public void open(Player from)
        {
            _computer.destroy(true);
            bOpened = true;

            int medics = 0;
            int marines = 0;
            int rippers = 0;
            int grenadiers = 0;
            int snipers = 0;
            int lmgs = 0;
            int ats = 0;
            int sappers = 0;

            medics = _team.ActivePlayers.Where(p => p._baseVehicle._type.Name.Contains("Medic")).Count();
            marines = _team.ActivePlayers.Where(p => p._baseVehicle._type.Name.Contains("Marine")).Count();
            rippers = _team.ActivePlayers.Where(p => p._baseVehicle._type.Name.Contains("Ripper")).Count();
            grenadiers = _team.ActivePlayers.Where(p => p._baseVehicle._type.Name.Contains("Grenadier")).Count();
            snipers = _team.ActivePlayers.Where(p => p._baseVehicle._type.Name.Contains("Sniper")).Count();
            lmgs = _team.ActivePlayers.Where(p => p._baseVehicle._type.Name.Contains("Machinegunner")).Count();
            ats = _team.ActivePlayers.Where(p => p._baseVehicle._type.Name.Contains("Assault")).Count();
            sappers = _team.ActivePlayers.Where(p => p._baseVehicle._type.Name.Contains("Sapper")).Count();



            //Spawn some ammo around the site

            //Medics
            for (int i = 0; i < medics; ++i)
                _arena.spawnItemInArea(AssetManager.Manager.getItemByName("10mm Pistol Cartridge"), (ushort)_rand.Next(75, 180), posX, posY, Settings.c_supply_openRadius);
            //Marines
            for (int i = 0; i < marines; ++i)
                _arena.spawnItemInArea(AssetManager.Manager.getItemByName("4.55mm Rifle FMJ"), (ushort)_rand.Next(75, 180), posX, posY, Settings.c_supply_openRadius);
            //Rippers
            for (int i = 0; i < rippers; ++i)
                _arena.spawnItemInArea(AssetManager.Manager.getItemByName("20mm HEAT Cartridge"), (ushort)_rand.Next(125, 250), posX, posY, Settings.c_supply_openRadius);
            //grenadiers
            for (int i = 0; i < grenadiers; ++i)
                _arena.spawnItemInArea(AssetManager.Manager.getItemByName("40mm HE Cartridge"), (ushort)_rand.Next(20, 40), posX, posY, Settings.c_supply_openRadius);
            //snipers
            for (int i = 0; i < snipers; ++i)
                _arena.spawnItemInArea(AssetManager.Manager.getItemByName("12.7mm Rifle FMJ"), (ushort)_rand.Next(75, 180), posX, posY, Settings.c_supply_openRadius);
            //lmgs
            for (int i = 0; i < lmgs; ++i)
                _arena.spawnItemInArea(AssetManager.Manager.getItemByName("4.55mm Rifle FMJ"), (ushort)_rand.Next(75, 180), posX, posY, Settings.c_supply_openRadius);
            //ats
            for (int i = 0; i < ats; ++i)
                _arena.spawnItemInArea(AssetManager.Manager.getItemByName("10mm Pistol Cartridge"), (ushort)_rand.Next(75, 180), posX, posY, Settings.c_supply_openRadius);
            //sappers
            for (int i = 0; i < sappers; ++i)
                _arena.spawnItemInArea(AssetManager.Manager.getItemByName("Demo Charge"), (ushort)_rand.Next(2, 4), posX, posY, Settings.c_supply_openRadius);


            //Generic stuff..
            for (int i = 0; i < _team.ActivePlayerCount; ++i)
            {
                _arena.spawnItemInArea(AssetManager.Manager.getItemByName("Shotgun Shells"), (ushort)_rand.Next(20, 40), posX, posY, Settings.c_supply_openRadius);
                _arena.spawnItemInArea(AssetManager.Manager.getItemByName("Hand Grenade"), (ushort)_rand.Next(2, 4), posX, posY, Settings.c_supply_openRadius);
                _arena.spawnItemInArea(AssetManager.Manager.getItemByName("LAW"), (ushort)_rand.Next(0, 3), posX, posY, Settings.c_supply_openRadius);
                _arena.spawnItemInArea(AssetManager.Manager.getItemByName("Incendiary Grenade"), (ushort)_rand.Next(2, 4), posX, posY, Settings.c_supply_openRadius);
                _arena.spawnItemInArea(AssetManager.Manager.getItemByName("Incinerator"), (ushort)_rand.Next(35, 55), posX, posY, Settings.c_supply_openRadius);
            }

            //Alert our lucky players
            _team.sendArenaMessage(String.Format("&The supply crate at {0} has been opened", _name), 4);
        }
    }

    public static class ArenaExtensions
    {
        /// <summary>
        /// Spawns the given item randomly in the specified area
        /// </summary>
        public static void spawnItemInArea(this Arena arena, ItemInfo item, ushort quantity, short x, short y, short radius)
        {       //Sanity
            if (quantity <= 0)
                return;

            int blockedAttempts = 30;

                short pX;
                short pY;
                while (true)
                {
                    pX = x;
                    pY = y;
                    Helpers.randomPositionInArea(arena, radius, ref pX, ref pY);
                    if (arena.getTile(pX, pY).Blocked)
                    {
                        blockedAttempts--;
                        if (blockedAttempts <= 0)
                            //Consider the spawn to be blocked
                            return;
                        continue;
                    }
                         arena.itemSpawn(item, quantity, pX, pY, null);
                    break;
                }
            }
        }
    }
