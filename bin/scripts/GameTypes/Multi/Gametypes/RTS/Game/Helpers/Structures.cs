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
        public bool IsProductionReady(Structure structure)
        {
            DateTime currentTime = DateTime.Now;

            //Is it time to produce?
            return (structure._nextProduction < currentTime);
        }

        public Structure getStructureByVehicleID(ushort id)
        {
            Structure result = _structures.Values.FirstOrDefault(stru => stru._vehicle._id == id);

            return result;
        }
    }
}
