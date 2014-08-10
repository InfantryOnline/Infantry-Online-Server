using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace InfServer.Logic
{
    public partial class Logic_Admins
    {
        /// <summary>
        /// Checks to see if this player logging in is an admin
        /// </summary>
        static public bool checkAdmin(string alias)
        {
            if (!File.Exists("admins.xml"))
            {
                Log.write(TLog.Warning, "Cannot locate the admins.xml file. Skipping.");
                return false;
            }

            bool result = false;
            XmlDocument doc = new XmlDocument();
            doc.Load("admins.xml");

            XmlNodeList list = doc.SelectNodes(String.Format("/admins/player[@alias='{0}']", alias));

            if (list.Count > 0)
                result = true;

            return result;
        }

        /// <summary>
        /// Returns a list of current admins
        /// </summary>
        static public string listAdmins()
        {
            string str = "Empty.";
            if (!File.Exists("admins.xml"))
            {
                Log.write(TLog.Warning, "Cannot locate the admins.xml file, skipping.");
                return "Cannot find the admins file.";
            }

            XmlDocument doc = new XmlDocument();
            doc.Load("admins.xml");

            XmlNodeList list = doc.SelectNodes("/admins/player");
            SortedList<string, string> admins = new SortedList<string, string>();

            foreach(XmlNode node in list)
                admins.Add(node.Attributes[0].Value, ", ");

            if (admins.Count > 0)
                str = string.Join(", ", admins.Keys);
            return str;
        }
    }
}
