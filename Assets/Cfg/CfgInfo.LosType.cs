using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class LosType
        {
            public bool seeType0;
            public bool seeType1;
            public bool seeType2;
            public bool seeType3;
            public bool seeType4;
            public bool seeType5;
            public bool seeType6;
            public bool seeType7;
            public int visibleDistance;
            public bool solid;

            public LosType(ref Dictionary<string, Dictionary<string, string>> stringTree, int i)
            {
                Parser.values = stringTree["LosType" + i];

                seeType0 = Parser.GetBool("SeeType0");
                seeType1 = Parser.GetBool("SeeType1");
                seeType2 = Parser.GetBool("SeeType2");
                seeType3 = Parser.GetBool("SeeType3");
                seeType4 = Parser.GetBool("SeeType4");
                seeType5 = Parser.GetBool("SeeType5");
                seeType6 = Parser.GetBool("SeeType6");
                seeType7 = Parser.GetBool("SeeType7");
                visibleDistance = Parser.GetInt("VisibleDistance");
                solid = Parser.GetBool("Solid");
            }
        }
    }
}
