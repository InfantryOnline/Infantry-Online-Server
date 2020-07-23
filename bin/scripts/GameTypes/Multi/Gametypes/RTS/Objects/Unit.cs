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
{
    public class Unit
    {
        private RTS _game;
        public string _key;
        public ushort _id;
        public Helpers.ObjectState _state;
        public ushort _vehicleID;
        public Bot _bot;


        public Unit(RTS game)
        {
            _game = game;
        }


        public void destroyed(Bot dead, Player killer)
        {
            //Update our database
            _game._database.removeBot(_id, _game._fileName);

            //Remove it
            _game._units.Remove(_id);
        }
    }
}
