using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using InfServer.Logic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;
using Axiom.Math;
using Assets;

namespace InfServer.Script.GameType_Multi
{

    public partial class Script_Multi : Scripts.IScript
    {
        public int _playerWarpRadius = 760;
        private int _engagedRadius = 1000;
        private int _maxEnemyRange = 2200;
        private int _flankerLogic = 3;
        private short _enemyBuffer = 1800;
        public Dictionary<string, Helpers.ObjectState> _lastSpawn;

        public Helpers.ObjectState findFlagWarp(Player player, bool bCoop)
        {
            Helpers.ObjectState warpPoint = null;
            List<Arena.FlagState> sortedFlags = new List<Arena.FlagState>();

            if (player._team._name == "Titan Militia")
                sortedFlags = _arena._flags.Values.OrderByDescending(f => f.posX).Where(f => f.team == player._team).ToList();
            if (player._team._name == "Collective Military")
                sortedFlags = _arena._flags.Values.OrderBy(f => f.posX).Where(f => f.team == player._team).ToList();

            int count = sortedFlags.Count;

            //No flags for some reason? defer to a player warp
            if (count == 0)
                return null;

            int index = sortedFlags.IndexOf(sortedFlags.Last());

            if (index != 0)
                index--;

            warpPoint = new Helpers.ObjectState();
            warpPoint.positionX = sortedFlags[index].posX;
            warpPoint.positionY = sortedFlags[index].posY;

            List<Player> enemies = new List<Player>();
            enemies = _arena.getPlayersInRange(sortedFlags.Last().posX, sortedFlags.Last().posY, _engagedRadius).Where(p => p._team != player._team).ToList();
            int botCount = _arena.getVehiclesInRange(sortedFlags.Last().posX, sortedFlags.Last().posY, _engagedRadius).Where(v => v._team != player._team).Count();

            if (enemies.Count() > 0 && !bCoop)
            {
                if (player._team._name == "Titan Militia")
                    warpPoint.positionX = (short)(sortedFlags.First().posX - ScaleOffset());
                if (player._team._name == "Collective Military")
                    warpPoint.positionX = (short)(sortedFlags.First().posX + ScaleOffset());
            }

            if (bCoop && sortedFlags.Last().flag.GeneralData.Name != "Titan Home")
            {
                if (player._team._name == "Titan Militia")
                    warpPoint.positionX = (short)(sortedFlags.First().posX - 1000);
                if (player._team._name == "Collective Military")
                    warpPoint.positionX = (short)(sortedFlags.First().posX + ScaleOffset());
            }

            return warpPoint;
        }

        public Helpers.ObjectState findFlagWarp(Team team, bool bBot)
        {
            Helpers.ObjectState warpPoint = null;
            List<Arena.FlagState> sortedFlags = new List<Arena.FlagState>();

            if (team._name == "Titan Militia")
                sortedFlags = _arena._flags.Values.OrderByDescending(f => f.posX).Where(f => f.team == team).ToList();
            if (team._name == "Collective Military")
                sortedFlags = _arena._flags.Values.OrderBy(f => f.posX).Where(f => f.team == team).ToList();

            int count = sortedFlags.Count;

            //No flags for some reason? defer to a player warp
            if (count == 0)
                return null;

            int index = 0;

            //Randomly set bots back a flag
            if (bBot)
            {
                Random random = new Random();

                if (random.Next(0, 100) >= 35)
                {
                    if (count >= 2)
                        index++;
                }
            }


            warpPoint = new Helpers.ObjectState();
            warpPoint.positionX = sortedFlags[index].posX;
            warpPoint.positionY = sortedFlags[index].posY;

            List<Player> enemies = new List<Player>();
            enemies = _arena.getPlayersInRange(sortedFlags.Last().posX, sortedFlags.Last().posY, _engagedRadius).Where(p => p._team != team).ToList();

            if (enemies.Count() > 0)
            {
                if (team._name == "Titan Militia")
                    warpPoint.positionX = (short)(sortedFlags.Last().posX - ScaleOffset());
                if (team._name == "Collective Military")
                    warpPoint.positionX = (short)(sortedFlags.Last().posX + ScaleOffset());
            }

            return warpPoint;
        }

        public Helpers.ObjectState findPlayerWarp(Player player)
        {
            Helpers.ObjectState warpPoint = null;
            List<Player> teammates;
            List<Player> enemies;

            if (player._team == _cq.cqTeam1)
            {
                //if we're team1 (left side of the map, we sort teammates by descending (biggest to smallest)
                teammates = player._team.ActivePlayers.OrderByDescending(p => p._state.positionX).ToList();
                //Sort enemies by ascending (smallest to biggest)
                enemies = _cq.cqTeam2.ActivePlayers.OrderBy(p => p._state.positionX).ToList();
            }
            else
            {
                //if we're team2 (right side of the map, we sort teammates by ascending (smallest to biggest)
                teammates = player._team.ActivePlayers.OrderByDescending(p => p._state.positionX).ToList();
                //Sort enemies by descending (biggest to smallest)
                enemies = _cq.cqTeam1.ActivePlayers.OrderBy(p => p._state.positionX).ToList();
            }

            int attempts = 5;
            while (true)
            {
                attempts--;
                if (attempts <= 0)
                    break;

                foreach (Player teammate in teammates)
                {
                    //Are they dead?
                    if (teammate.IsDead)
                    {
                        continue;
                    }

                    //Are they in a dropship?
                    if (player._arena.getTerrain(teammate._state.positionX, teammate._state.positionY).safety)
                    {
                        continue;
                    }

                    //Are they engaged?
                    if (_arena.getPlayersInRange(teammate._state.positionX, teammate._state.positionY, _engagedRadius).Count > 0)
                    {
                        continue;
                    }

                    //Are they too far from the enemy?
                    if (_arena.getPlayersInRange(teammate._state.positionX, teammate._state.positionY, _maxEnemyRange).Count == 0)
                    {
                        continue;
                    }

                    if (player._team == _cq.cqTeam1)
                    {
                        int count = 0;
                        foreach (Player enemy in enemies)
                        {
                            //No warping to flankers
                            if (teammate._state.positionX > enemy._state.positionX)
                                count++;
                        }
                        //No warping to flankers
                        if (count > _flankerLogic)
                        {
                            continue;
                        }
                    }

                    if (player._team == _cq.cqTeam2)
                    {

                        int count = 0;
                        foreach (Player enemy in enemies)
                        {
                            //No warping to flankers
                            if (teammate._state.positionX < enemy._state.positionX)
                                count++;
                        }
                        //No warping to flankers
                        if (count > _flankerLogic)
                        {
                            continue;
                        }
                    }

                    //If we've reached this code, we must have a match!
                    warpPoint = new Helpers.ObjectState();
                    warpPoint.positionX = teammate._state.positionX;
                    warpPoint.positionY = teammate._state.positionY;

                    //Alerts
                    teammate.sendMessage(0, String.Format("{0} has joined you in battle.", player._alias));
                    break;
                }
                //We've iterated through all of our teammates, now let's find a suitable warp using the enemy
                if (warpPoint == null)
                {
                    Log.write(TLog.Normal, String.Format("Trying Enemies..."));
                    foreach (Player enemy in enemies)
                    {

                        //Are they dead?
                        if (enemy.IsDead)
                        {
                            continue;
                        }

                        //Are they in a dropship?
                        if (player._arena.getTerrain(enemy._state.positionX, enemy._state.positionY).safety)
                        {
                            continue;
                        }


                        //If we've reached this code, we must have a match!
                        warpPoint = new Helpers.ObjectState();
                        warpPoint.positionY = enemy._state.positionY;
                        if (player._team == _cq.cqTeam1)
                            warpPoint.positionX = (short)(enemy._state.positionX - ScaleOffset());
                        if (player._team == _cq.cqTeam2)
                            warpPoint.positionX = (short)(enemy._state.positionX + ScaleOffset());

                        //Alerts
                        enemy.sendMessage(0, "!Enemy troops have been detected near your coordinates.");
                        break;
                    }

                }
                break;
            }
            return warpPoint;
        }

        /// <summary>
        /// Finds a specific point within a radius with no physics for a player to warp to
        /// </summary>
        /// <param name="arena"></param>
        /// <param name="posX"></param>
        /// <param name="posY"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public Helpers.ObjectState findOpenWarp(Player player, Arena arena, short posX, short posY, int radius)
        {
            Helpers.ObjectState warpPoint = null;


            try
            {
                int blockedAttempts = 10;

                int enemycount = _arena.getPlayersInRange(posX, posY, _engagedRadius).Where(p => p._team != player._team).Count();
                if (player._team == _cq.cqTeam1 && enemycount > 0)
                    posX = (short)(posX - ScaleOffset());
                if (player._team == _cq.cqTeam2 && enemycount > 0)
                    posX = (short)(posX + ScaleOffset());


                short pX;
                short pY;

                while (true)
                {
                    pX = posX;
                    pY = posY;
                    Helpers.randomPositionInArea(arena, radius, ref pX, ref pY);
                    if (arena.getTile(pX, pY).Blocked)
                    {
                        blockedAttempts--;
                        if (blockedAttempts <= 0)
                            //Consider the area to be blocked
                            return findPlayerWarp(player);
                        else
                            continue;
                    }

                    warpPoint = new Helpers.ObjectState();
                    warpPoint.positionX = pX;
                    warpPoint.positionY = pY;


                    break;

                }
            }
            catch (Exception ex)
            {
                Log.write(TLog.Exception, ex.Message);
            }
            return warpPoint;
        }


        /// <summary>
        /// Finds a specific point within a radius with no physics for a player to warp to
        /// </summary>
        /// <param name="arena"></param>
        /// <param name="posX"></param>
        /// <param name="posY"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public Helpers.ObjectState findOpenWarp(Team team, Arena arena, short posX, short posY, int radius)
        {
            Helpers.ObjectState warpPoint = null;


            try
            {
                int blockedAttempts = 10;

                int enemycount = _arena.getPlayersInRange(posX, posY, _engagedRadius).Where(p => p._team != team).Count();
                if (team._name == "Titan Militia" && enemycount > 0)
                    posX = (short)(posX - ScaleOffset());
                if (team._name == "Collective Military" && enemycount > 0)
                    posX = (short)(posX + ScaleOffset());


                short pX;
                short pY;

                while (true)
                {
                    pX = posX;
                    pY = posY;
                    Helpers.randomPositionInArea(arena, radius, ref pX, ref pY);
                    if (arena.getTile(pX, pY).Blocked)
                    {
                        blockedAttempts--;
                        if (blockedAttempts <= 0)
                            //Consider the area to be blocked
                            return null;
                        else
                            continue;
                    }

                    warpPoint = new Helpers.ObjectState();
                    warpPoint.positionX = pX;
                    warpPoint.positionY = pY;


                    break;

                }
            }
            catch (Exception ex)
            {
                Log.write(TLog.Exception, ex.Message);
            }
            return warpPoint;
        }




        /// <summary>
        /// Warps a player to specific objectstate
        /// </summary>
        /// <param name="player"></param>
        /// <param name="warpTo"></param>
        public void warp(Player player, Helpers.ObjectState warpTo)
        {
            player.warp(warpTo.positionX, warpTo.positionY);
        }

        public int ScaleOffset()
        {
            int offset = _enemyBuffer;

            if (_arena.PlayerCount < 10)
                offset -= 400;
            if (_arena.PlayerCount < 5)
                offset -= 500;
            if (_arena.PlayerCount < 4)
                offset -= 600;


            return offset;
        }
    }
}