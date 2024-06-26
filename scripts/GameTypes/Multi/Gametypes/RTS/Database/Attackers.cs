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
        public void removeAttacker(string alias, string key)
        {
            if (!tableExists(key))
                return;

            XmlNode t = _data[key].SelectSingleNode("/playerTable/attackers/attacker[@alias='" + alias + "']");

            if (t != null)
                t.ParentNode.RemoveChild(t);
            else
                return;

            saveData(key);
        }

        public bool canAttack(string alias, string key)
        {
            if (!_data.ContainsKey(key))
                return false;

            XmlNode attackerNode = _data[key].SelectSingleNode("/playerTable/attackers/attacker[@alias='" + alias + "']");

            if (attackerNode != null)
                return false;

            return true;
        }

        public void addAttacker(DateTime time, string alias, string key)
        {
            if (!_data.ContainsKey(key))
            {
                Log.write("Unable to add attacker");
                return;
            }

            XmlElement newNode = _data[key].CreateElement("attacker");
            newNode.SetAttribute("alias", alias);
            newNode.SetAttribute("date", Convert.ToString(time));
          

            XmlElement itemsNode = _data[key]["playerTable"]["attackers"];
            itemsNode.AppendChild(newNode);

            saveData(key);
        }

        public Dictionary<string, Attacker> loadAttackers(string player)
        {
            Dictionary<string, Attacker> attackers = new Dictionary<string, Attacker>();

            if (!_data.ContainsKey(player))
                return null;

            foreach (XmlNode Node in _data[player].SelectNodes("playerTable/attackers"))
            {
                foreach (XmlNode child in Node.ChildNodes)
                {
                    Attacker newAttacker = new Attacker(_game);
                    newAttacker._alias = child.Attributes["alias"].Value;
                    newAttacker._attackExpire = DateTime.Parse(child.Attributes["date"].Value);
                    //Add it
                    attackers.Add(newAttacker._alias, newAttacker);
                }
            }
            return attackers;
        }
    }
}
