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
        public void removeStructure(ushort id, string key)
        {
            if (!tableExists(key))
                return;

            XmlNode t = _data[key].SelectSingleNode("/playerTable/structures/structure[@id='"+ id +"']");

            if (t != null)
                t.ParentNode.RemoveChild(t);
            else
                return;

            saveData(key);
        }

        public ushort addStructure(Structure structure, string key)
        {
            if (!_data.ContainsKey(key))
            {
                Log.write("Unable to add structure");
                return 0;
            }
                

            XmlNode header = _data[key].SelectSingleNode("playerTable/structures");

            ushort idx = (ushort)(getLastStructureID(key) + 1);

            //Update our last ID
            header.Attributes["lastID"].Value = Convert.ToString(idx);


            XmlElement newNode = _data[key].CreateElement("structure");
            newNode.SetAttribute("id", Convert.ToString(idx));
            newNode.SetAttribute("vehicleID", Convert.ToString(structure._vehicle._type.Id));
            newNode.SetAttribute("positionX", Convert.ToString(structure._vehicle._state.positionX));
            newNode.SetAttribute("positionY", Convert.ToString(structure._vehicle._state.positionY));
            newNode.SetAttribute("health", Convert.ToString(structure._vehicle._state.health));
            if (structure._productionItem == null)
                newNode.SetAttribute("productionID", Convert.ToString(0));
            else
                newNode.SetAttribute("productionID", Convert.ToString(structure._productionItem.id));

            newNode.SetAttribute("productionAmount", Convert.ToString(structure._productionQuantity));
            newNode.SetAttribute("productionTime", Convert.ToString(structure._nextProduction));
            newNode.SetAttribute("productionLevel", Convert.ToString(structure._productionLevel));
            newNode.SetAttribute("upgradeCost", Convert.ToString(structure._upgradeCost));

            XmlElement structuresNode = _data[key]["playerTable"]["structures"];
            structuresNode.AppendChild(newNode);

            saveData(key);

            return idx;
        }

        public void updateStructure(Structure structure, string key)
        {
            if (!_data.ContainsKey(key))
                return;

            XmlNode structureNode = _data[key].SelectSingleNode("/playerTable/structures/structure[@id='" + structure._id + "']");


            structureNode.Attributes["positionX"].Value = Convert.ToString(structure._vehicle._state.positionX);
            structureNode.Attributes["positionY"].Value = Convert.ToString(structure._vehicle._state.positionY);
            structureNode.Attributes["health"].Value = Convert.ToString(structure._vehicle._state.health);

            if (structure._productionItem == null)
                structureNode.Attributes["productionID"].Value = Convert.ToString(0);
            else
                structureNode.Attributes["productionID"].Value = Convert.ToString(structure._productionItem.id);

            structureNode.Attributes["productionAmount"].Value = Convert.ToString(structure._productionQuantity);

            if (structure._nextProduction == null)
                structureNode.Attributes["productionTime"].Value = Convert.ToString(0);
            else
                structureNode.Attributes["productionTime"].Value = Convert.ToString(structure._nextProduction);

            structureNode.Attributes["productionLevel"].Value = Convert.ToString(structure._productionLevel);
            structureNode.Attributes["upgradeCost"].Value = Convert.ToString(structure._upgradeCost);

            saveData(key);
        }

        public ushort getLastStructureID(string key)
        {
            XmlNode header = _data[key].SelectSingleNode("playerTable/structures");

            return Convert.ToUInt16(header.Attributes["lastID"].Value);
        }

        public Dictionary<ushort, Structure> loadStructures(string key)
        {
            Dictionary<ushort, Structure> structures = new Dictionary<ushort, Structure>();

            if (!_data.ContainsKey(key))
                return null;

            foreach (XmlNode Node in _data[key].SelectNodes("playerTable/structures"))
            {
                foreach (XmlNode child in Node.ChildNodes)
                {
                    Structure building = new Structure(_game);
                    building._id = Convert.ToUInt16(child.Attributes["id"].Value);
                    building._type = AssetManager.Manager.getVehicleByID(Convert.ToInt32(child.Attributes["vehicleID"].Value));
                    building._key = key;

                    building._state = new Helpers.ObjectState();
                    building._state.positionX = Convert.ToInt16(child.Attributes["positionX"].Value);
                    building._state.positionY = Convert.ToInt16(child.Attributes["positionY"].Value);
                    building._state.health = Convert.ToInt16(child.Attributes["health"].Value);
                    building._productionItem = AssetManager.Manager.getItemByID(Convert.ToInt32(child.Attributes["productionID"].Value));
                    building._productionQuantity = Convert.ToInt32(child.Attributes["productionAmount"].Value);
                    building._nextProduction = DateTime.Parse(child.Attributes["productionTime"].Value);
                    building._productionLevel = Convert.ToInt32(child.Attributes["productionLevel"].Value);
                    building._upgradeCost = Convert.ToInt32(child.Attributes["upgradeCost"].Value);

                    //Add it
                    structures.Add(building._id, building);
                }
            }
            return structures;
        }
    }
}
