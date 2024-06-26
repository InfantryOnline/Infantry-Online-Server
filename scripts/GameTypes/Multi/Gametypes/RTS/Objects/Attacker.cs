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
    public class Attacker
    {
        private RTS _game;
        public string _key;
        public string _alias;
        public ushort _id;
        public DateTime _attackExpire;
        public int _tickExpire;


        public Attacker(RTS game)
        {
            _game = game;
        }
    }
}
