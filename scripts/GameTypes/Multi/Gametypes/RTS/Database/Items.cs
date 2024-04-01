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
    public partial class Database
    {
        public void removeItem(ushort id, string key)
        {
            if (!tableExists(key))
                return;

            XmlNode t = _data[key].SelectSingleNode("/playerTable/items/item[@id='" + id + "']");

            if (t != null)
                t.ParentNode.RemoveChild(t);
            else
                return;

            saveData(key);
        }

        public ushort addItem(int itemID, short posX, short posY, int quantity, string key)
        {
            if (!_data.ContainsKey(key))
            {
                Log.write("Unable to add item");
                return 0;
            }


            XmlNode header = _data[key].SelectSingleNode("playerTable/items");

            ushort idx = (ushort)(getLastItemID(key) + 1);

            //Update our last ID
            header.Attributes["lastID"].Value = Convert.ToString(idx);


            XmlElement newNode = _data[key].CreateElement("item");
            newNode.SetAttribute("id", Convert.ToString(idx));
            newNode.SetAttribute("itemID", Convert.ToString(itemID));
            newNode.SetAttribute("positionX", Convert.ToString(posX));
            newNode.SetAttribute("positionY", Convert.ToString(posY));
            newNode.SetAttribute("quantity", Convert.ToString(quantity));
          

            XmlElement itemsNode = _data[key]["playerTable"]["items"];
            itemsNode.AppendChild(newNode);

            saveData(key);

            return idx;
        }

        public void updateItem(StoredItem item, string key)
        {
            if (!_data.ContainsKey(key))
                return;

            XmlNode structureNode = _data[key].SelectSingleNode("/playerTable/items/item[@id='" + item._id + "']");

            structureNode.Attributes["quantity"].Value = Convert.ToString(item._quantity);

            saveData(key);
        }

        public ushort getLastItemID(string key)
        {
            XmlNode header = _data[key].SelectSingleNode("playerTable/items");

            return Convert.ToUInt16(header.Attributes["lastID"].Value);
        }

        public Dictionary<ushort, StoredItem> loadItems(string player)
        {
            Dictionary<ushort, StoredItem> items = new Dictionary<ushort, StoredItem>();

            if (!_data.ContainsKey(player))
                return null;

            foreach (XmlNode Node in _data[player].SelectNodes("playerTable/items"))
            {
                foreach (XmlNode child in Node.ChildNodes)
                {
                    StoredItem newItem = new StoredItem(_game);
                    newItem._id = Convert.ToUInt16(child.Attributes["id"].Value);
                    newItem._quantity = Convert.ToInt16(child.Attributes["quantity"].Value);
                    newItem._itemID = Convert.ToInt32(child.Attributes["itemID"].Value);
                    newItem._posX = Convert.ToInt16(child.Attributes["positionX"].Value);
                    newItem._posY = Convert.ToInt16(child.Attributes["positionY"].Value);

                    //Add it
                    items.Add(newItem._id, newItem);
                }
            }
            return items;
        }
    }
}
