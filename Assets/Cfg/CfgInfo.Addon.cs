using System.Collections.Generic;
using System;
namespace Assets
{
    public partial class CfgInfo
    {
        public class Addon
        {
            public int aidLogic;
            public bool aidEnabled;
            public bool teamOwnership;

            public Addon(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["Addon"];

                aidLogic = Parser.GetInt("AidLogic");
                aidEnabled = Parser.GetBool("AidEnabled");
                teamOwnership = Parser.GetBool("AllowTeamOwnership");
            }
        }
    }
}
