using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class Sound
        {
            public int attemptTime;

            public Sound(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["Sound"];

                attemptTime = Parser.GetInt("AttemptTime");
            }
        }
    }
}
