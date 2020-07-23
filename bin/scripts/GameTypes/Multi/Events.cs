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
{   // Script Class
    /// Provides the interface between the script and arena
    ///////////////////////////////////////////////////////
    partial class Script_Multi : Scripts.IScript
    {

        static public bool _bCoopHappyHour;
        static public bool _bPvpHappyHour;

        private int _lastCoopHappyHourAlert;
        private int _tickCoopHappyHourStart;
        private int _lastPvpHappyHourAlert;
        private int _tickPvpHappyHourStart;

        public void pollEvents(int now)
        {
            bool bCoopHappyHour = isHappyHour(Settings._coopHappyHourStart, Settings._coopHappyHourEnd);
            bool bPvpHappyHour = isHappyHour(Settings._pvpHappyHourStart, Settings._pvpHappyHourEnd);

            if (!_bCoopHappyHour && now - _lastCoopHappyHourAlert >= 900000 && timeTo(Settings._coopHappyHourStart).Hours < 1)
            {
                TimeSpan remaining = timeTo(Settings._coopHappyHourStart);

                    _arena.sendArenaMessage(String.Format("&Happy hour in all Public Co-Op arenas starts in {0} hour(s) & {1} minute(s)", 
                        remaining.Hours, remaining.Minutes), 4);

                _lastCoopHappyHourAlert = now;
            }

            if (!_bPvpHappyHour && now - _lastPvpHappyHourAlert >= 900000 && timeTo(Settings._pvpHappyHourStart).Hours < 1)
            {
                TimeSpan remaining = timeTo(Settings._pvpHappyHourStart);
                _arena.sendArenaMessage(String.Format("&Happy hour in all Public PvP arenas starts in {0} hour(s) & {1} minute(s)",
                        remaining.Hours, remaining.Minutes), 4);

                _lastPvpHappyHourAlert = now;
            }

            if (bCoopHappyHour)
            {
                _bCoopHappyHour = true;

                if (_tickCoopHappyHourStart == 0)
                {
                    
                     _arena.sendArenaMessage(String.Format("&Happy hour in all Public Co-Op arenas is now active until 9pm EST"), 4);

                    _tickCoopHappyHourStart = now;
                }
            }
            else
            {
                _bCoopHappyHour = false;
                _tickCoopHappyHourStart = 0;
            }

            if (bPvpHappyHour)
            {
                _bPvpHappyHour = true;

                if (_tickPvpHappyHourStart == 0)
                {
                    _arena.sendArenaMessage(String.Format("&Happy hour in all Public PvP arenas is now active until 10pm EST"), 4);

                    _tickPvpHappyHourStart = now;
                }
            }
            else
            {
                _bPvpHappyHour = false;
                _tickPvpHappyHourStart = 0;
            }
        }

        public bool isHappyHour(TimeSpan start, TimeSpan end)
        {
            // convert datetime to a TimeSpan
            TimeSpan now = DateTime.Now.TimeOfDay;
            // see if start comes before end
            if (start < end)
                return start <= now && now <= end;
            // start is after end, so do the inverse comparison
            return !(end < now && now < start);
        }

        public TimeSpan timeTo(TimeSpan start)
        {
            TimeSpan remaining;

            DateTime nowTime = DateTime.Now;
            remaining = start - nowTime.TimeOfDay;

            if (remaining.TotalHours < 0)
            {
                remaining += TimeSpan.FromHours(24);
                remaining += TimeSpan.FromMinutes(1440);
            }

            return remaining;
        }
    }
}

