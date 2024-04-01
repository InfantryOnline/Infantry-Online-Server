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

using Assets;

namespace InfServer.Script.GameType_Multi
{ 	// Script Class
    /// Provides the interface between the script and arena
    ///////////////////////////////////////////////////////
    public partial class RTS
    {
        public int _botMax = 20;
        public List<Bot> _bots;
        public List<Bot> _condemnedBots;

        public void pollBots(int now)
        {
            if (_bots == null)
                _bots = new List<Bot>();

            
        }


        private Bot newBot(Team team, BotType type, Vehicle target, Player owner, BotLevel level, Helpers.ObjectState state = null)
        {
            if (_bots == null)
                _bots = new List<Bot>();

            //Max bots?
            if (_bots.Count >= _botMax)
            {
                //Unless we're a special bot type, disregard
                if (type == BotType.Marine || type == BotType.Ripper)
                {
                    Log.write(TLog.Warning, "Excessive bot spawning");
                    return null;
                }
            }

            //What kind is it?
            switch (type)
            {
                #region Gunship
                case BotType.Gunship:
                    {
                        //Collective vehicle
                        ushort vehid = 134;

                        //Titan vehicle?
                        if (team._name == "Titan Militia")
                            vehid = 147;

                        Gunship gunship = _arena.newBot(typeof(Gunship), vehid, team, owner, state, null) as Gunship;
                        if (gunship == null)
                            return null;

                        gunship._team = team;
                        gunship.type = BotType.Dropship;
                        gunship.init(state, _baseScript, target, owner, Settings.GameTypes.RTS);

                        gunship.Destroyed += delegate (Vehicle bot)
                        {
                            _bots.Remove((Bot)bot);
                        };

                        _bots.Add(gunship);

                        return gunship as Bot;
                    }
                   
                #endregion
                #region Elite Heavy
                case BotType.EliteHeavy:
                    {
                        //Collective vehicle
                        ushort vehid = 148;

                        EliteHeavy heavy = _arena.newBot(typeof(EliteHeavy), vehid, team, null, state, null) as EliteHeavy;

                        if (heavy == null)
                            return null;

                        heavy._team = team;
                        heavy.type = BotType.EliteHeavy;
                        heavy.init(Settings.GameTypes.RTS, _collective);

                        heavy.Destroyed += delegate (Vehicle bot)
                        {
                            _bots.Remove((Bot)bot);

                        };

                        _bots.Add(heavy);
                        return heavy as Bot;
                    }
                #endregion
                #region Elite Marine
                case BotType.EliteMarine:
                    {
                        //Collective vehicle
                        ushort vehid = 146;

                        EliteMarine elitemarine = _arena.newBot(typeof(EliteMarine), vehid, team, null, state, null) as EliteMarine;

                        if (elitemarine == null)
                            return null;

                        elitemarine._team = team;
                        elitemarine.type = BotType.EliteHeavy;
                        elitemarine.init(Settings.GameTypes.RTS, _collective);

                        elitemarine.Destroyed += delegate (Vehicle bot)
                        {
                            _bots.Remove((Bot)bot);

                        };

                        _bots.Add(elitemarine);
                        return elitemarine as Bot;
                    }
                #endregion
                #region Marine
                case BotType.Marine:
                    {
                        //Collective vehicle
                        ushort vehid = 131;

                        switch (level)
                        {
                            case BotLevel.Adept:
                                    vehid = 151;
                                break;
                        }

                        Marine marine = _arena.newBot(typeof(Marine), vehid, team, null, state, null) as Marine;

                        if (marine == null)
                            return null;

                        marine._team = team;
                        marine.type = BotType.Marine;
                        marine.init(Settings.GameTypes.RTS, _collective);
                        marine.Destroyed += delegate (Vehicle bot)
                        {
                            _bots.Remove((Bot)bot);

                        };

                        _bots.Add(marine);
                        return marine as Bot;
                    }
                #endregion
                #region Ripper
                case BotType.Ripper:
                    {
                        //Collective vehicle normal Marine
                        ushort vehid = 145;

                        switch (level)
                        {
                            case BotLevel.Adept:
                                vehid = 152;
                                break;
                        }

                        
                        Ripper ripper = _arena.newBot(typeof(Ripper), vehid, team, null, state, null) as Ripper;

                        if (ripper == null)
                            return null;

                        ripper._team = team;
                        ripper.type = BotType.Ripper;
                        ripper.init(Settings.GameTypes.RTS, _collective);
                        ripper.Destroyed += delegate (Vehicle bot)
                        {
                            _bots.Remove((Bot)bot);

                        };

                        _bots.Add(ripper);
                        return ripper as Bot;
                    }
                #endregion
                #region ExoLight
                case BotType.ExoLight:
                    {
                        //Collective vehicle
                        ushort vehid = 149;

                        //Titan vehicle?
                        if (team._name == "Titan Militia")
                            vehid = 133;

                        ExoLight exo = _arena.newBot(typeof(ExoLight), vehid, team, null, state, null) as ExoLight;

                        if (exo == null)
                            return null;

                        exo._team = team;
                        exo.type = BotType.ExoLight;
                        exo.init(Settings.GameTypes.RTS);

                        exo.Destroyed += delegate (Vehicle bot)
                        {
                            _bots.Remove((Bot)bot);

                        };

                        _bots.Add(exo);
                        return exo as Bot;
                    }
                #endregion
                #region ExoHeavy
                case BotType.ExoHeavy:
                    {
                        //Collective vehicle
                        ushort vehid = 430;

                        //Titan vehicle?
                        if (team._name == "Titan Militia")
                            vehid = 133;

                        ExoHeavy exo = _arena.newBot(typeof(ExoHeavy), vehid, team, null, state, null) as ExoHeavy;

                        if (exo == null)
                            return null;

                        exo._team = team;
                        exo.type = BotType.ExoHeavy;
                        exo.init(Settings.GameTypes.RTS);

                        exo.Destroyed += delegate (Vehicle bot)
                        {
                            _bots.Remove((Bot)bot);

                        };

                        _bots.Add(exo);
                        return exo as Bot;
                    }
                    #endregion
            }
            return null;
        }
    }
}
