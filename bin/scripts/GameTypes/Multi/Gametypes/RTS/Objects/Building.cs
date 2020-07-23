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
    public class Building
    {
        public string _name;
        public ushort _id;
        public VehInfo _type;
        public Helpers.ObjectState _state;
        public TimeSpan _collectionTime;
        public Team _team;
    }
}
