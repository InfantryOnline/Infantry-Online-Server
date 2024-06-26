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
{ 	// Script Class
    /// Provides the interface between the script and arena
    ///////////////////////////////////////////////////////
    public partial class RTS
    {
        #region Command Handlers
        public bool playerModcommand(Player player, Player recipient, string command, string payload)
        {
            return true;
        }

        public bool playerChatCommand(Player player, Player recipient, string command, string payload)
        {
            string cmd = command.ToLower();

            switch (cmd)
            {
                case "rtshelp":
                    {
                        player.sendMessage(0, "$-Useable Commands:");
                        player.sendMessage(0, "& --- ?productions (Displays upcoming productions within your city)");
                        player.sendMessage(0, "& --- ?attacklog (Displays a log of recent attacks on your city)");
                        player.sendMessage(0, "& --- ?citylist (Displays a list of all cities ordered by highest to lowest value)");

                    }
                    break;

                case "productions":
                    {
                        player.sendMessage(0, "$-Upcoming Productions:");
                        List<Structure> producers = _structures.Values.Where(str => str._productionQuantity > 0).ToList();

                        if (producers.Count == 0)
                        {
                            player.sendMessage(0, "No entries");
                            break;
                        }

                        foreach (Structure producer in producers)
                        {
                            TimeSpan remaining = _baseScript.timeTo(producer._nextProduction.TimeOfDay);

                            player.sendMessage(0, String.Format("&--- {0} (Quantity={1} Time Remaining={2}h {3}m {4}s)",
                                producer._type.Name,
                                producer._productionQuantity,
                                remaining.Hours,
                                remaining.Minutes,
                                remaining.Seconds));
                        }

                    }
                    break;
                case "attacklog":
                    {
                        player.sendMessage(0, "$-Recent Attackers:");

                        if (_attackers.Count == 0)
                        {
                            player.sendMessage(0, "No entries");
                            break;
                        }

                        foreach (Attacker attacker in _attackers.Values.OrderByDescending(atk => atk._attackExpire))
                            player.sendMessage(0, String.Format("& --- {0} @ {1} EST", attacker._alias, attacker._attackExpire.AddHours(-24)));

                        player.sendMessage(0, "Attack log entries expire after 24 hours");

                    }
                    break;

                case "citylist":
                    {
                        List<City> cities = new List<City>();
                        Dictionary<ushort, Structure> structures;
                        foreach (KeyValuePair<string, XmlDocument> table in _database._data)
                        {
                            structures = _database.loadStructures(table.Key);
                            int cityValue = calculateCityValue(structures.Values);

                            City city = new City();
                            city._key = table.Key;
                            city._owner = _database.getOwner(table.Key);
                            city._squad = _database.getSquad(table.Key);
                            city._bSquad = _database.isSquadCity(table.Key);
                            city._value = cityValue;

                            cities.Add(city);
                        }

                        foreach (City city in cities.OrderByDescending(cty => cty._value))
                        {
                            player.sendMessage(0, String.Format("$-[RTS] {0} (Value={1})", city._key, city._value));
                            if (city._bSquad)
                                player.sendMessage(0, String.Format("& --- Squad Owner = {0}", city._squad));
                            else
                                player.sendMessage(0, String.Format("& --- Owner = {0}", city._owner));
                        }

                        
                    }
                    break;
            }
            return true;
        }
        #endregion
    }
}
