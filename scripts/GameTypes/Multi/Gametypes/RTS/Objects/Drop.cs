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
    public class StoredItem
    {
        private RTS _game;
        public string _key;
        public ushort _id;
        public Arena.ItemDrop _drop;
        public int _itemID;
        public short _quantity;
        public short _posX;
        public short _posY;

        public StoredItem(RTS game)
        {
            _game = game;
        }


        public void remove()
        {
            //Update our database
            _game._database.removeItem(_id, _game._fileName);

            //Remove it
            _game._items.Remove(_id);
        }
    }
}
