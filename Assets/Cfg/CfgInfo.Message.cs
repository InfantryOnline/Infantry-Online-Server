using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class Message
        {
            public bool reliable;

            public Message(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["Message"];

                reliable = Parser.GetBool("Reliable");
            }
        }
    }
}
