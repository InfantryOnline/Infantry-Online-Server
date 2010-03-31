using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class RtsStateDefault
        {
            public int initialState0;
            public int initialState1;
            public int initialState2;
            public int initialState3;
            public int initialState4;
            public int initialState5;
            public int initialState6;
            public int initialState7;

            public RtsStateDefault(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["RtsStateDefault"];

                initialState0 = Parser.GetInt("InitialState0");
                initialState1 = Parser.GetInt("InitialState1");
                initialState2 = Parser.GetInt("InitialState2");
                initialState3 = Parser.GetInt("InitialState3");
                initialState4 = Parser.GetInt("InitialState4");
                initialState5 = Parser.GetInt("InitialState5");
                initialState6 = Parser.GetInt("InitialState6");
                initialState7 = Parser.GetInt("InitialState7");
            }
        }
    }
}
