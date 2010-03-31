using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class Stat
        {
            public bool displayTeamBreakdown;
            public bool displayIndividualBreakdown;
            public bool displayTeamBubble;
            public bool displayIndividualBubble;
            public bool bubbleUpdateDelay;
            public bool displayGameTimeAtEnd;
            public bool trackDaily;
            public bool trackWeekly;
            public bool trackMonthly;
            public bool trackYearly;

            public Stat(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["Stat"];

                displayTeamBreakdown = Parser.GetBool("DisplayTeamBreakdown");
                displayIndividualBreakdown = Parser.GetBool("DisplayIndividualBreakdown");
                displayTeamBubble = Parser.GetBool("DisplayTeamBubble");
                displayIndividualBubble = Parser.GetBool("DisplayIndividualBubble");
                bubbleUpdateDelay = Parser.GetBool("BubbleUpdateDelay");
                displayGameTimeAtEnd = Parser.GetBool("DisplayGameTimeAtEnd");
                trackDaily = Parser.GetBool("TrackDaily");
                trackWeekly = Parser.GetBool("TrackWeekly");
                trackMonthly = Parser.GetBool("TrackMonthly");
                trackYearly = Parser.GetBool("TrackYearly");
            }
        }
    }
}
