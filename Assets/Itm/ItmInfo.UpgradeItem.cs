using System.Collections.Generic;

namespace Assets
{
    public partial class ItemInfo
    {
        public class UpgradeItem : ItemInfo
        {
            public Graphics iconGraphic;
            public struct Upgrade
            {
                public int inputID;
                public int outputID;
            }

            public List<Upgrade> upgrades = new List<Upgrade>();

            public static UpgradeItem Load(List<string> values)
            {
                UpgradeItem item = new UpgradeItem();
                item.iconGraphic = new Graphics(ref values, 23);
                ItemInfo.LoadGeneralSettings1((ItemInfo)item, values);

                int currentPlace = 31;

                for (int i = 0; i < 15; i++)
                {
                    Upgrade child = new Upgrade();
                    child.inputID = CSVReader.GetInt(values[currentPlace + 0]);
                    child.outputID = CSVReader.GetInt(values[currentPlace + 1]);
                    currentPlace += 2;
                    item.upgrades.Add(child);
                }

                return item;
            }
        }
    }
}