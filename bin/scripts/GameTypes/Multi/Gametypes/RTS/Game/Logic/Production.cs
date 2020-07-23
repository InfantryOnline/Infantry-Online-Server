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
        private int lastProductionCheck;

        public void checkForProductions(int now)
        {
            //Don't check too often
            if (now - lastProductionCheck < 1000)
                return;

            DateTime currentTime = DateTime.Now;

            foreach (Structure structure in _structures.Values)
            {
                //Does this building even produce anything?
                if (structure._productionQuantity == 0)
                    continue;

                //Is it time to produce?
                if (structure._nextProduction < currentTime)
                    Helpers.Player_RouteExplosion(_arena.Players, 1465, structure._state.positionX, structure._state.positionY, 185, 0, 0);
                
                else
                    continue;

            }
            lastProductionCheck = now;
        }
    }
}
