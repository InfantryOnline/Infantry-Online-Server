using System.Collections.Generic;

namespace Assets
{
    public partial class ItemInfo
    {
        public class NestedItem : ItemInfo
        {
            public int itemID;
            public string location;

            public static NestedItem Load(List<string> values)
            {
                NestedItem item = new NestedItem();
                item.id = CSVReader.GetInt(values[0]);
                item.version = CSVReader.GetInt(values[1].Trim('v'));
                item.itemID = CSVReader.GetInt(values[2]);
                item.name = CSVReader.GetString(values[3]);
                item.location = CSVReader.GetString(values[4]);
                return item;
            }
        }
    }
}