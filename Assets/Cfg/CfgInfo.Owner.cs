using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class Owner
        {
            public bool kill;
            public bool Lock;
            public bool spec;
            public bool info;
            public bool arena;
            public bool warp;
            public bool summon;
            public bool prize;
            public bool cash;
            public bool experience;
            public bool grant;
            public bool shutup;
            public bool timer;
            public bool team;
            public bool restart;
            public bool scramble;
            public bool wipe;
            public bool profile;
            public bool enable;
            public bool getBall;
            public bool addBall;
            public bool energy;
            public bool flags;
            public bool points;

            public Owner(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["Owner"];

                kill = Parser.GetBool("Kill");
                Lock = Parser.GetBool("Lock");
                spec = Parser.GetBool("Spec");
                info = Parser.GetBool("Info");
                arena = Parser.GetBool("Arena");
                warp = Parser.GetBool("Warp");
                summon = Parser.GetBool("Summon");
                prize = Parser.GetBool("Prize");
                cash = Parser.GetBool("Cash");
                experience = Parser.GetBool("Experience");
                grant = Parser.GetBool("Grant");
                shutup = Parser.GetBool("Shutup");
                timer = Parser.GetBool("Timer");
                team = Parser.GetBool("Team");
                restart = Parser.GetBool("Restart");
                scramble = Parser.GetBool("Scramble");
                wipe = Parser.GetBool("Wipe");
                profile = Parser.GetBool("Profile");
                enable = Parser.GetBool("Enable");
                getBall = Parser.GetBool("GetBall");
                addBall = Parser.GetBool("AddBall");
                energy = Parser.GetBool("Energy");
                flags = Parser.GetBool("Flags");
                points = Parser.GetBool("Points");
            }
        }
    }
}
