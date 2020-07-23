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
    public class LootTable
    {
        public int botID;
        public string name;

        public List<LootInfo> normalLoot;
        public List<LootInfo> commonLoot;
        public List<LootInfo> uncommonLoot;
        public List<LootInfo> setLoot;
        public List<LootInfo> rareLoot;

        public int commonChance;
        public int uncommonChance;
        public int setChance;
        public int rareChance;

        public int commonCount;
        public int uncommonCount;
        public int setCount;
        public int rareCount;

        /// <summary>
        /// Constructor
        /// </summary>
        public LootTable(string filename)
        {
            commonLoot = new List<LootInfo>();
            uncommonLoot = new List<LootInfo>();
            setLoot = new List<LootInfo>();
            rareLoot = new List<LootInfo>();
            normalLoot = new List<LootInfo>();

            XmlDocument Doc = new XmlDocument();
            Doc.Load(filename);

            XmlNode header = Doc.SelectSingleNode("lootTable");

            botID = Convert.ToInt32(header.Attributes["botID"].Value);
            name = header.Attributes["name"].Value;

            foreach (XmlNode Node in Doc.SelectNodes("lootTable/normal"))
            {
                foreach (XmlNode child in Node.ChildNodes)
                {

                    name = child.Attributes["name"].Value;
                    int itemid = Convert.ToInt32(child.Attributes["itemID"].Value);
                    int chance = Convert.ToInt32(child.Attributes["chance"].Value);
                    int quantity = Convert.ToInt32(child.Attributes["quantity"].Value);

                    LootInfo lootInfo = new LootInfo(name, itemid, chance, quantity, LootType.Normal);
                    normalLoot.Add(lootInfo);
                }
            }

            foreach (XmlNode Node in Doc.SelectNodes("lootTable/common"))
            {
                commonChance = Convert.ToInt32(Node.Attributes["chance"].Value);
                commonCount = Convert.ToInt32(Node.Attributes["count"].Value);

                foreach (XmlNode child in Node.ChildNodes)
                {

                    name = child.Attributes["name"].Value;
                    int itemid = Convert.ToInt32(child.Attributes["itemID"].Value);
                    int chance = Convert.ToInt32(child.Attributes["chance"].Value);
                    int quantity = Convert.ToInt32(child.Attributes["quantity"].Value);

                    LootInfo lootInfo = new LootInfo(name, itemid, chance, quantity, LootType.Common);
                    commonLoot.Add(lootInfo);
                }
            }
            foreach (XmlNode Node in Doc.SelectNodes("lootTable/uncommon"))
            {
                uncommonChance = Convert.ToInt32(Node.Attributes["chance"].Value);
                uncommonCount = Convert.ToInt32(Node.Attributes["count"].Value);
                foreach (XmlNode child in Node.ChildNodes)
                {
                    name = child.Attributes["name"].Value;
                    int itemid = Convert.ToInt32(child.Attributes["itemID"].Value);
                    int chance = Convert.ToInt32(child.Attributes["chance"].Value);
                    int quantity = Convert.ToInt32(child.Attributes["quantity"].Value);
                    LootInfo lootInfo = new LootInfo(name, itemid, chance, quantity, LootType.Uncommon);
                    uncommonLoot.Add(lootInfo);
                }
            }
            foreach (XmlNode Node in Doc.SelectNodes("lootTable/set"))
            {
                setChance = Convert.ToInt32(Node.Attributes["chance"].Value);
                setCount = Convert.ToInt32(Node.Attributes["count"].Value);
                foreach (XmlNode child in Node.ChildNodes)
                {
                    name = child.Attributes["name"].Value;
                    int itemid = Convert.ToInt32(child.Attributes["itemID"].Value);
                    int chance = Convert.ToInt32(child.Attributes["chance"].Value);
                    int quantity = Convert.ToInt32(child.Attributes["quantity"].Value);
                    LootInfo lootInfo = new LootInfo(name, itemid, chance, quantity, LootType.Set);
                    setLoot.Add(lootInfo);  
                }
            }
            foreach (XmlNode Node in Doc.SelectNodes("lootTable/rare"))
            {
                rareChance = Convert.ToInt32(Node.Attributes["chance"].Value);
                rareCount = Convert.ToInt32(Node.Attributes["count"].Value);
                foreach (XmlNode child in Node.ChildNodes)
                {
                    name = child.Attributes["name"].Value;
                    int itemid = Convert.ToInt32(child.Attributes["itemID"].Value);
                    int chance = Convert.ToInt32(child.Attributes["chance"].Value);
                    int quantity = Convert.ToInt32(child.Attributes["quantity"].Value);
                    LootInfo lootInfo = new LootInfo(name, itemid, chance, quantity, LootType.Rare);
                    rareLoot.Add(lootInfo);
                }
            }
        }
    }
}
