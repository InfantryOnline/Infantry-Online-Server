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
    public partial class Conquest
    {
        private int _lastMedicSpawnTeam1;
        private int _lastMedicSpawnTeam2;
        private int _lastMarineSpawnTeam1;
        private int _lastMarineSpawnTeam2;
        private int _marineSpawnDelay = 120000;
        private int _medicSpawnDelay = 30000;
        public List<Bot> _bots;
        public bool spawnBots;

        public void pollBots(int now)
        {
            if (spawnBots)
            {
                //Does team1 need a medic?
                if (now - _lastMedicSpawnTeam1 > _medicSpawnDelay)
                {
                    int playercount = cqTeam1.ActivePlayers.Where(p => p.ActiveVehicle._type.Name.Contains("Medic")).Count();
                    int botcount = _bots.Where(b => b._type.Id == 128 && b._team == cqTeam1).Count();

                    if (botcount < 2)
                    {
                        _lastMedicSpawnTeam1 = now;
                        spawnMedic(cqTeam1);
                    }
                }
                //Does team2 need a medic?
                if (now - _lastMedicSpawnTeam2 > _medicSpawnDelay)
                {
                    int playercount = cqTeam2.ActivePlayers.Where(p => p.ActiveVehicle._type.Name.Contains("Medic")).Count();
                    int botcount = _bots.Where(b => b._type.Id == 301 && b._team == cqTeam2).Count();

                    if (botcount < 2)
                    {
                        _lastMedicSpawnTeam2 = now;
                        spawnMedic(cqTeam2);
                    }
                }

                //Does team1 need a marine?
                if (now - _lastMarineSpawnTeam1 > _marineSpawnDelay)
                {
                    int playercount = cqTeam1.ActivePlayers.Where(p => p.ActiveVehicle._type.Name.Contains("Marine")).Count();
                    int botcount = _bots.Where(b => b._type.Id == 133 && b._team == cqTeam1).Count();

                    if (botcount < 1)
                    {
                        _lastMarineSpawnTeam1 = now;
                        spawnMarine(cqTeam1);
                    }
                }
                //Does team2 need a marine?
                if (now - _lastMarineSpawnTeam2 > _marineSpawnDelay)
                {
                    int playercount = cqTeam2.ActivePlayers.Where(p => p.ActiveVehicle._type.Name.Contains("Marine")).Count();
                    int botcount = _bots.Where(b => b._type.Id == 131 && b._team == cqTeam2).Count();

                    if (botcount < 1)
                    {
                        _lastMarineSpawnTeam2 = now;
                        spawnMarine(cqTeam2);
                    }
                }
            }

            foreach (Bot bot in _bots)
            {
                switch (bot._type.Name)
                {
                    case ("Titan Medic (B)"):
                        {
                            IEnumerable<Player> enemies = _arena.Players.Where(p => p._team != bot._team);
                            if (!bot.IsDead)
                            {
                                Helpers.Player_RouteExplosion(bot._team.ActivePlayers, 1131, bot._state.positionX, bot._state.positionY, 0, 0, 0);
                                Helpers.Player_RouteExplosion(enemies, 1130, bot._state.positionX, bot._state.positionY, 0, 0, 0);
                            }
                        }
                        break;

                    case ("Collective Medic (B)"):
                        {
                            IEnumerable<Player> enemies = _arena.Players.Where(p => p._team != bot._team);
                       
                            if (!bot.IsDead)
                            {
                                Helpers.Player_RouteExplosion(bot._team.ActivePlayers, 1131, bot._state.positionX, bot._state.positionY, 0, 0, 0);
                                Helpers.Player_RouteExplosion(enemies, 1130, bot._state.positionX, bot._state.positionY, 0, 0, 0);
                            }
                        }
                        break;

                    case ("Collective Marine (B)"):
                        {

                            IEnumerable<Player> enemies = _arena.Players.Where(p => p._team != bot._team);

                            if (!bot.IsDead)
                            {
                                Helpers.Player_RouteExplosion(bot._team.ActivePlayers, 1131, bot._state.positionX, bot._state.positionY, 0, 0, 0);
                                Helpers.Player_RouteExplosion(enemies, 1130, bot._state.positionX, bot._state.positionY, 0, 0, 0);
                            }
                        }
                        break;

                    case ("Titan Marine (B)"):
                        { 

                            IEnumerable<Player> enemies = _arena.Players.Where(p => p._team != bot._team);

                            if (!bot.IsDead)
                            {
                                Helpers.Player_RouteExplosion(bot._team.ActivePlayers, 1131, bot._state.positionX, bot._state.positionY, 0, 0, 0);
                                Helpers.Player_RouteExplosion(enemies, 1130, bot._state.positionX, bot._state.positionY, 0, 0, 0);
                            }
                        }
                        break;
                }

            }
        }

        public void spawnMedic(Team team)
        {
            Helpers.ObjectState warpPoint = _baseScript.findFlagWarp(team, true);

            if (!newBot(team, BotType.Medic, warpPoint))
                Log.write(TLog.Warning, "Unable to spawn medic bot");

        }

        public void spawnMarine(Team team)
        {
            Helpers.ObjectState warpPoint = _baseScript.findFlagWarp(team, true);

            if (!newBot(team, BotType.Marine, warpPoint))
                Log.write(TLog.Warning, "Unable to spawn marine bot");

        }

        private bool newBot(Team team, BotType type, Helpers.ObjectState state = null)
        {
            if (_bots == null)
                _bots = new List<Bot>();

            switch (type)
            {
                case BotType.Medic:
                    {
                        //Collective vehicle
                        ushort vehid = 301;

                        //Titan vehicle?
                        if (team == cqTeam1)
                            vehid = 128;

                        Medic medic = _arena.newBot(typeof(Medic), vehid, team, null, state, null) as Medic;

                        if (medic == null)
                            return false;

                        medic._team = team;
                        medic.type = BotType.Medic;
                        medic.init(null);

                        medic.Destroyed += delegate (Vehicle bot)
                        {
                            _bots.Remove((Bot)bot);

                        };

                        _bots.Add(medic);
                    }
                    break;

                case BotType.Marine:
                    {
                        //Collective vehicle
                        ushort vehid = 131;

                        //Titan vehicle?
                        if (team == cqTeam1)
                            vehid = 133;

                        Marine marine = _arena.newBot(typeof(Marine), vehid, team, null, state, null) as Marine;

                        if (marine == null)
                            return false;

                        marine._team = team;
                        marine.type = BotType.Marine;
                        marine._cq = this;
                        marine.init(Settings.GameTypes.Conquest, null);

                        marine.Destroyed += delegate (Vehicle bot)
                        {
                            _bots.Remove((Bot)bot);

                        };

                        _bots.Add(marine);
                    }
                    break;


            }
            return true;
        }
    }
}
