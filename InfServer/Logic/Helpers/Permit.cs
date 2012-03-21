using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml;
using System.IO;

namespace InfServer.Logic
{
    public partial class Logic_Permit
    {
        static public bool checkPermit(string alias)
        {
            if (!File.Exists("permit.xml"))
            {
                Log.write(TLog.Warning, "Permission-only enabled and no permit.xml could be located.");
                return false;
            }

            //Innocent until proven guilty..
            bool result = false;
            XmlDocument doc = new XmlDocument();
            doc.Load("permit.xml");

            XmlNodeList xnList = doc.SelectNodes(String.Format("/players/player[@alias='{0}']", alias));


            if (xnList.Count > 0)
            {
                result = true;
            }

            return result;
        }

        

        static public string listPermit()
        {
            string s;
            if (!File.Exists("permit.xml"))
            {
                Log.write(TLog.Warning, "Permission-only enabled and no permit.xml could be located.");
                return "No permit file.";
            }

            XmlDocument doc = new XmlDocument();
            doc.Load("permit.xml");

            XmlNodeList xnList = doc.SelectNodes("/players/player");
            List<string> list = new List<string>();

            foreach (XmlNode node in xnList)
                list.Add(node.Attributes[0].Value);

            s = string.Join(",", list.ToArray());

            return s;
        }

        static public void addPermit(string alias)
        {
            //Does the player already exist?
            if (Logic_Permit.checkPermit(alias))
            {
                return;
            }

            //Does our permit file exist?
            if (!File.Exists("permit.xml"))
            {
                Log.write(TLog.Warning, "Permission-only enabled and no permit.xml could be located.");
                return;
            }

            //Load our doc
            XmlDocument doc = new XmlDocument();
            doc.Load("permit.xml");

            XmlNode node = doc.CreateNode(XmlNodeType.Element, "player", null);
            XmlAttribute att = doc.CreateAttribute("alias");
            att.Value = alias;
            node.Attributes.Append(att);

            //add to elements collection
            doc.DocumentElement.AppendChild(node);

            //save back
            doc.Save("permit.xml");
        }


        static public void removePermit(string alias)
        {
            //Does the player already exist?
            if (!Logic_Permit.checkPermit(alias))
            {
                return;
            }

            //Does our permit file exist?
            if (!File.Exists("permit.xml"))
            {
                Log.write(TLog.Warning, "Permission-only enabled and no permit.xml could be located.");
                return;
            }

            //Load our doc
            XmlDocument doc = new XmlDocument();
            doc.Load("permit.xml");

            XmlNodeList xnList = doc.SelectNodes(String.Format("/players/player[@alias='{0}']", alias));
            
            foreach (XmlNode node in xnList)
            doc.DocumentElement.RemoveChild(node);

            //save back
            doc.Save("permit.xml");
        }
    }
}
