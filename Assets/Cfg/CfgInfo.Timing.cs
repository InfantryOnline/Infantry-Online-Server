using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class Timing
        {
            public int sendPositionDelay;
            public int enterDelay;
            public int parentSendPositionDelay;
            public int soonToFireTickTolerance;
            public int vectorTolerance;

            public Timing(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["Timing"];

                sendPositionDelay = Parser.GetInt("SendPositionDelay");
                enterDelay = Parser.GetInt("EnterDelay");
                parentSendPositionDelay = Parser.GetInt("ParentSendPositionDelay");
                soonToFireTickTolerance = Parser.GetInt("SoonToFireTickTolerance");
                vectorTolerance = Parser.GetInt("VectorTolerance");
            }
        }
    }
}
