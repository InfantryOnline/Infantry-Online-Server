using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Database;
using InfServer.Data;
using Microsoft.EntityFrameworkCore;

namespace InfServer.Logic
{
    class Logic_Admins
    {
        /// <summary>
        /// Our list of the current server admins
        /// </summary>
        public static List<string> ServerAdmins = new List<string>();

        public static List<long> ServerAdminAccountIds = new List<long>();

        /// <summary>
        /// Populates the admin list
        /// </summary>
        static public void PopulateAdmins()
        {
            ServerAdmins = new List<string>();

            if (!File.Exists("admins.xml"))
            {
                Log.write(TLog.Warning, "Cannot locate the admins.xml file. Skipping.");
                return;
            }

            XmlDocument doc = new XmlDocument();
            doc.Load("admins.xml");

            if (doc.HasChildNodes)
            {
                int i = 0;
                foreach (XmlNode n in doc.ChildNodes.Item(i))
                {
                    ServerAdmins.Add(n.InnerText);
                    i++;
                }
            }

            return;
        }

        static public void PopulateAdminAccountIds(DBServer server)
        {
            using (var ctx = server.getContext())
            {
                ServerAdminAccountIds = ctx.Aliases
                    .Include(a => a.AccountNavigation)
                    .Where(a => ServerAdmins.Contains(a.Name))
                    .Select(a => a.AccountNavigation.Id)
                    .ToList();
            }
        }

        /// <summary>
        /// Returns a list of current admins
        /// </summary>
        static public string listAdmins()
        {
            string str = "Empty.";
            if (ServerAdmins == null)
            {
                return "None.";
            }

            ServerAdmins.Sort();
            str = string.Join(", ", ServerAdmins);
            return str;
        }
    }
}