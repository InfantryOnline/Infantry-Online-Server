using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text.RegularExpressions;


using InfServer.Game;
using InfServer.Protocol;
using InfServer.Scripting;
using InfServer.Bots;

using Assets;

namespace InfServer.Script.GameType_Multi
{
    public class LootInfo
    {
        public string _name;
        public ItemInfo _item;
        public LootType _type;
        public int _weight;
        public int _quantity;

        /// <summary>
        /// Constructor
        /// </summary>
        public LootInfo(string name, int itemid, int chance, int quantity, LootType type)
        {
            _name = name;
            _item = AssetManager.Manager.getItemByID(itemid);
            _weight = chance;
            _quantity = quantity;
            _type = type;   
        }
    }

    public enum LootType
    {
        Normal,
        Common,
        Uncommon,
        Set,
        Rare
    }
}
