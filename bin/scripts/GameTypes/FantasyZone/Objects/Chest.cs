using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Assets;

namespace InfServer.Script.GameType_Fantasy
{
    public partial class ChestInfo
    {
        public class Chest
        {
            //Member Properties
            public bool bOpen;
            public List<Item> inventory;
            public int inventoryCount;
            public string owner;

            /// <summary>
            /// Child class
            /// </summary>
            public class Item
            {
                public int id;
                public int itemID;
                public int quantity;
            }

            public static List<ChestInfo> Load(string filename)
            {
                List<ChestInfo> chestInfo = new List<ChestInfo>();
                TextReader reader = new StreamReader(filename);
                string line = "";

                while ((line = reader.ReadLine()) != null)
                {
                    List<string> values = CSVReader.Parse(line);

                    //Chest chest = Chest.Load(values);
                    //chestInfo.Add(chest);
                }

                return chestInfo;
            }
        }
    }
}
