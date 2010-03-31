using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class View
        {
            public int adjustDistance;
            public int adjustSpeed;
            public int adjustRotateSpeed;

            public View(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["View"];

                adjustDistance = Parser.GetInt("AdjustDistance");
                adjustSpeed = Parser.GetInt("AdjustSpeed");
                adjustRotateSpeed = Parser.GetInt("AdjustRotateSpeed");
            }
        }
    }
}
