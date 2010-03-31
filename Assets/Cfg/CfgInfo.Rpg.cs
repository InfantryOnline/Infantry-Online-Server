using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class Rpg
        {
            public int skillCostMethod;
            public int attributeCostMethod;
            public int skillBaseCost;
            public string skillCountPower;
            public int attributeBaseCost;
            public string attributeCountPower;

            public Rpg(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["Rpg"];

                skillCostMethod = Parser.GetInt("SkillCostMethod");
                attributeCostMethod = Parser.GetInt("AttributeCostMethod");
                skillBaseCost = Parser.GetInt("SkillBaseCost");
                skillCountPower = Parser.GetString("SkillCountPower");
                attributeBaseCost = Parser.GetInt("AttributeBaseCost");
                attributeCountPower = Parser.GetString("AttributeCountPower");
            }
        }
    }
}
