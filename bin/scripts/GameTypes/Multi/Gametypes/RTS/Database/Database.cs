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
        public Dictionary<string, Structure> _buildings;
        private Arena _arena;
        private RTS _game;

        public Dictionary<string, XmlDocument> _data;
        private XmlDocument _buildingData;

        public Database(Arena arena, RTS game)
        {
            _arena = arena;
            _data = new Dictionary<string, XmlDocument>();
            _game = game;
        }

        public void loadXML()
        {
            Log.write("Loading Data from XML Database...");

            string[] playerTables = Directory.GetFiles(System.Environment.CurrentDirectory + "/Data/RTS/PlayerData/", "*.xml");

            foreach (string table in playerTables)
            {
                XmlDocument newTable = new XmlDocument();

                //Load the data table
                newTable.Load(table);

                //Grab the header
                XmlNode header = newTable.SelectSingleNode("playerTable");

                //Add it!
                _data.Add(header.Attributes["name"].Value.ToLower(), newTable);
            }
        }

        public bool playerOwnsCity(Player player)
        {
            foreach (XmlDocument table in _data.Values)
                if (table.SelectSingleNode("playerTable").Attributes["owner"].Value == player._alias)
                    return true;

            return false;
        }

        public bool isSquadCity(string key)
        {
            if (!_data.ContainsKey(key))
                return false;
            else
            {
                //Grab the header
                XmlNode header = _data[key].SelectSingleNode("playerTable");
                return Convert.ToInt32(header.Attributes["allowSquad"].Value) == 1;
            }
        }

        public string getSquad(string key)
        {
            if (!_data.ContainsKey(key))
                return "";
            else
            {
                //Grab the header
                XmlNode header = _data[key].SelectSingleNode("playerTable");

                return header.Attributes["squad"].Value;
            }
        }

        public string getOwner(string key)
        {
            if (!_data.ContainsKey(key))
                return "";
            else
            {
                //Grab the header
                XmlNode header = _data[key].SelectSingleNode("playerTable");

                return header.Attributes["owner"].Value.ToLower();
            }
        }


        public bool tableExists(string key)
        {
            if (!_data.ContainsKey(key))
                return false;
            else
                return true;
        }

        public void createTable(string key, string owner, string squadname, bool bAllowSquad)
        {
            string defaultFile = System.Environment.CurrentDirectory + "/Data/RTS/default.xml";
            string newFile = System.Environment.CurrentDirectory + "/Data/RTS/PlayerData/" + key + ".xml";

            System.IO.File.Copy(defaultFile, newFile);

            XmlDocument newTable = new XmlDocument();

            //Load the data table
            newTable.Load(newFile);

            //Grab the header
            XmlNode header = newTable.SelectSingleNode("playerTable");
            header.Attributes["owner"].Value = owner;
            header.Attributes["name"].Value = key;

            if (!String.IsNullOrEmpty(squadname))
            header.Attributes["squad"].Value = squadname;
            else

            if (bAllowSquad)
                header.Attributes["allowSquad"].Value = "1";
            else
                header.Attributes["allowSquad"].Value = "0";

            //Save it
            newTable.Save(newFile);
            _data[key] = newTable;
        }

        public void saveData(string key)
        {
            _data[key].Save(System.Environment.CurrentDirectory + "/Data/RTS/PlayerData/" + key + ".xml");

            reloadData(key);
        }

        public void reloadData(string key)
        {
            _data[key].Load(System.Environment.CurrentDirectory + "/Data/RTS/PlayerData/" + key + ".xml");
        }
    }
}
