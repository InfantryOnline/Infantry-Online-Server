using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;


using InfServer.Game;
using InfServer.Protocol;
using InfServer.Scripting;
using InfServer.Bots;

using Assets;

namespace InfServer.Script.GameType_Multi
{
    public class LootDrop
    {
        public Arena.ItemDrop _item;
        public Player _owner;                   //The player that owns this loot (Only visible to him)
        public int _tickCreation;
        public ushort _id;

        /// <summary>
        /// Constructor
        /// </summary>
        public LootDrop(Arena.ItemDrop item, Player owner, int tickCreation, ushort id)
        {
            _item = item;
            _owner = owner;
            _tickCreation = tickCreation;
            _id = id;
        }
    }
}
