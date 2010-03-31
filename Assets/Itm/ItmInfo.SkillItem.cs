using System.Collections.Generic;

namespace Assets
{
    public partial class ItemInfo
    {
        public class SkillItem : ItemInfo
        {
            public Graphics iconGraphic;
            public struct Skill
            {
                public string logic;
                public int ID;
            }

            public List<Skill> skills;

            public static SkillItem Load(List<string> values)
            {
                SkillItem item = new SkillItem();
                item.iconGraphic = new Graphics(ref values, 23);
                ItemInfo.LoadGeneralSettings1((ItemInfo)item, values);
                int currentPlace = 31;

                for (int i = 0; i < 15; i++)
                {
                    Skill child = new Skill();
					child.logic = CSVReader.GetQuotedString(values[currentPlace + 0]);
                    child.ID = CSVReader.GetInt(values[currentPlace + 1]);
                    currentPlace += 2;
                }
                return item;
            }
        }
    }
}