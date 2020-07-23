using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;


using InfServer.Game;
using InfServer.Protocol;
using InfServer.Scripting;
using InfServer.Bots;

using Assets;

namespace InfServer.Script.GameType_Multi
{
    public class CraftItem
    {
        public int itemID;
        public string name;
        public Dictionary<ItemInfo, int> _parts;


        /// <summary>
        /// Constructor
        /// </summary>
        public CraftItem(string filename)
        {

            _parts = new Dictionary<ItemInfo, int>();

            XmlDocument Doc = new XmlDocument();
            Doc.Load(filename);
            XmlNode header = Doc.SelectSingleNode("craftItem");

            itemID = Convert.ToInt32(header.Attributes["itemID"].Value);
            name = header.Attributes["name"].Value;

            foreach (XmlNode Node in Doc.SelectNodes("craftItem/requiredItem"))
            {
                int partID = Convert.ToInt32(Node.Attributes["itemID"].Value);
                int quantity = Convert.ToInt32(Node.Attributes["count"].Value);

                _parts.Add(AssetManager.Manager.getItemByID(partID), quantity);
            }
        }
    }
}
