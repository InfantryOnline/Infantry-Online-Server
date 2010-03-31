using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class TeamDefault
        {
            public int inventory0;
            public int quantity0;
            public int inventory1;
            public int quantity1;
            public int inventory2;
            public int quantity2;
            public int inventory3;
            public int quantity3;
            public int inventory4;
            public int quantity4;
            public int inventory5;
            public int quantity5;
            public int inventory6;
            public int quantity6;
            public int inventory7;
            public int quantity7;
            public int inventory8;
            public int quantity8;
            public int inventory9;
            public int quantity9;
            public int inventory10;
            public int quantity10;
            public int inventory11;
            public int quantity11;
            public int inventory12;
            public int quantity12;
            public int inventory13;
            public int quantity13;
            public int inventory14;
            public int quantity14;
            public int inventory15;
            public int quantity15;

            public TeamDefault(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["TeamDefault"];

                inventory0 = Parser.GetInt("Inventory0");
                quantity0 = Parser.GetInt("Quantity0");
                inventory1 = Parser.GetInt("Inventory1");
                quantity1 = Parser.GetInt("Quantity1");
                inventory2 = Parser.GetInt("Inventory2");
                quantity2 = Parser.GetInt("Quantity2");
                inventory3 = Parser.GetInt("Inventory3");
                quantity3 = Parser.GetInt("Quantity3");
                inventory4 = Parser.GetInt("Inventory4");
                quantity4 = Parser.GetInt("Quantity4");
                inventory5 = Parser.GetInt("Inventory5");
                quantity5 = Parser.GetInt("Quantity5");
                inventory6 = Parser.GetInt("Inventory6");
                quantity6 = Parser.GetInt("Quantity6");
                inventory7 = Parser.GetInt("Inventory7");
                quantity7 = Parser.GetInt("Quantity7");
                inventory8 = Parser.GetInt("Inventory8");
                quantity8 = Parser.GetInt("Quantity8");
                inventory9 = Parser.GetInt("Inventory9");
                quantity9 = Parser.GetInt("Quantity9");
                inventory10 = Parser.GetInt("Inventory10");
                quantity10 = Parser.GetInt("Quantity10");
                inventory11 = Parser.GetInt("Inventory11");
                quantity11 = Parser.GetInt("Quantity11");
                inventory12 = Parser.GetInt("Inventory12");
                quantity12 = Parser.GetInt("Quantity12");
                inventory13 = Parser.GetInt("Inventory13");
                quantity13 = Parser.GetInt("Quantity13");
                inventory14 = Parser.GetInt("Inventory14");
                quantity14 = Parser.GetInt("Quantity14");
                inventory15 = Parser.GetInt("Inventory15");
                quantity15 = Parser.GetInt("Quantity15");
            }
        }
    }
}
