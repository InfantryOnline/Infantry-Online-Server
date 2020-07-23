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
        #region Bots
        public ushort addBot(Bot bot, string key)
        {
            if (!_data.ContainsKey(key))
                return 0;

            XmlNode header = _data[key].SelectSingleNode("playerTable/bots");

            ushort idx = (ushort)(getLastBotID(key) + 1);

            //Update our last ID
            header.Attributes["lastID"].Value = Convert.ToString(idx);


            XmlElement newNode = _data[key].CreateElement("bot");
            newNode.SetAttribute("id", Convert.ToString(idx));
            newNode.SetAttribute("vehicleID", Convert.ToString(bot._type.Id));
            newNode.SetAttribute("positionX", Convert.ToString(bot._state.positionX));
            newNode.SetAttribute("positionY", Convert.ToString(bot._state.positionY));
           

            XmlElement botsNode = _data[key]["playerTable"]["bots"];
            botsNode.AppendChild(newNode);

            saveData(key);

            return idx;
        }

        public ushort getLastBotID(string key)
        {
            XmlNode header = _data[key].SelectSingleNode("playerTable/bots");

            return Convert.ToUInt16(header.Attributes["lastID"].Value);
        }

        public Dictionary<ushort, Unit> loadBots(string player)
        {
            Dictionary<ushort, Unit> units = new Dictionary<ushort, Unit>();

            if (!_data.ContainsKey(player))
                return null;

            foreach (XmlNode Node in _data[player].SelectNodes("playerTable/bots"))
            {
                foreach (XmlNode child in Node.ChildNodes)
                {
                    Unit unit = new Unit(_game);
                    unit._id = Convert.ToUInt16(child.Attributes["id"].Value);
                    unit._vehicleID = Convert.ToUInt16(child.Attributes["vehicleID"].Value);
                    unit._state = new Helpers.ObjectState();
                    unit._state.positionX = Convert.ToInt16(child.Attributes["positionX"].Value);
                    unit._state.positionY = Convert.ToInt16(child.Attributes["positionY"].Value);
                    unit._key = player;

                    //Add it
                    units.Add(unit._id, unit);
                }
            }
            return units;
        }

        public void updateBot(Unit unit, string key)
        {
            if (!_data.ContainsKey(key))
                return;

            XmlNode botNode = _data[key].SelectSingleNode("playerTable/bots/bot[@id='" + unit._id + "']");


            botNode.Attributes["positionX"].Value = Convert.ToString(unit._bot._state.positionX);
            botNode.Attributes["positionY"].Value = Convert.ToString(unit._bot._state.positionY);

            
            saveData(key);
        }

        public void removeBot(ushort id, string key)
        {
            if (!tableExists(key))
                return;

            XmlNode t = _data[key].SelectSingleNode("/playerTable/bots/bot[@id='" + id + "']");

            if (t != null)
                t.ParentNode.RemoveChild(t);
            else
                return;

            saveData(key);
        }
        #endregion
    }
}
