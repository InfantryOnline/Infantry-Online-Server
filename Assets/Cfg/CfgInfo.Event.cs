using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class Event
        {
            public string hold1;
            public string hold2;
            public string hold3;
            public string hold4;
            public string startGame;
            public string sysopWipe;
            public string selfWipe;
            public string firstTimeInvSetup;
            public string firstTimeSkillSetup;
            public string killedTeam;
            public string killedEnemy;
            public string killedByTeam;
            public string killedByEnemy;
            public string enterSpawnNoScore;
            public string changeDefaultVehicle;
            public string joinTeam;
            public string exitSpectatorMode;
            public string endGame;
            public string soonGame;
            public string manualJoinTeam;

            public Event(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["Event"];

                hold1 = Parser.GetString("Hold1");
                hold2 = Parser.GetString("Hold2");
                hold3 = Parser.GetString("Hold3");
                hold4 = Parser.GetString("Hold4");
                startGame = Parser.GetString("StartGame");
                sysopWipe = Parser.GetString("SysopWipe");
                selfWipe = Parser.GetString("SelfWipe");
                firstTimeInvSetup = Parser.GetString("FirstTimeInvSetup");
                firstTimeSkillSetup = Parser.GetString("FirstTimeSkillSetup");
                killedTeam = Parser.GetString("KilledTeam");
                killedEnemy = Parser.GetString("KilledEnemy");
                killedByTeam = Parser.GetString("KilledByTeam");
                killedByEnemy = Parser.GetString("KilledByEnemy");
                enterSpawnNoScore = Parser.GetString("EnterSpawnNoScore");
                changeDefaultVehicle = Parser.GetString("ChangeDefaultVehicle");
                joinTeam = Parser.GetString("JoinTeam");
                exitSpectatorMode = Parser.GetString("ExitSpectatorMode");
                endGame = Parser.GetString("EndGame");
                soonGame = Parser.GetString("SoonGame");
                manualJoinTeam = Parser.GetString("ManualJoinTeam");
            }
        }
    }
}
