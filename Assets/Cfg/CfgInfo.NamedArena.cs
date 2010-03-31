using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class NamedArena
        {
            public string name;
            public string displayLogic;
            public string playLogic;
            public string startLogic;

            public NamedArena(ref Dictionary<string, Dictionary<string, string>> stringTree, int i)
            {
                Parser.values = stringTree["NamedArena" + i];

                name = Parser.GetString("Name");
                displayLogic = Parser.GetString("DisplayLogic");
                playLogic = Parser.GetString("PlayLogic");
                startLogic = Parser.GetString("StartLogic");
            }
        }
    }
}
