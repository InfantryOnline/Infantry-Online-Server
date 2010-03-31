using System.Collections.Generic;

namespace Assets
{
    public partial class ItemInfo
    {
        public class MultiItem : ItemInfo
        {
            public Graphics graphic;

            public int Cash;
            public int Energy;
            public int Health;
            public int Repair;
            public int Experience;
            public struct Slot
            {
                public int number;
                public int value;
                public Slot(int _number, int _value)
                {
                    number = _number;
                    value = _value;
                }
            }
            public List<Slot> slots = new List<Slot>();




            public static MultiItem Load(List<string> values)
            {
                MultiItem multiItem = new MultiItem();
                multiItem.graphic = new Graphics(ref values, 23);
                ItemInfo.LoadGeneralSettings1((ItemInfo)multiItem, values);
                multiItem.Cash = CSVReader.GetInt(values[31]);
                multiItem.Energy = CSVReader.GetInt(values[32]);
                multiItem.Health = CSVReader.GetInt(values[33]);
                multiItem.Repair = CSVReader.GetInt(values[34]);
                multiItem.Experience = CSVReader.GetInt(values[35]);
                for (int i = 0; i <= 16; i++)
                {
                    int value = CSVReader.GetInt(values[36 + i]);
                    multiItem.slots.Add(new Slot(i, value));
                }





                return multiItem;
            }
        }
    }
}
