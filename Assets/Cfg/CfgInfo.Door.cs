using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class Door
        {
            public int attemptTime;
            public int randomControlDelay;

            public Door(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["Door"];

                attemptTime = Parser.GetInt("AttemptTime");
                randomControlDelay = Parser.GetInt("RandomControlDelay");
            }
        }
    }
}
