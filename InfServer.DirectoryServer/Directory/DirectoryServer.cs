using System.Collections.Generic;
using System.Net;
using DirectoryServer.Directory.Protocol;
using InfServer.DirectoryServer.Directory.Protocol.Helpers;
using InfServer.Network;

namespace InfServer.DirectoryServer.Directory
{
    public class DirectoryServer : Server
    {
        /// <summary>
        /// Initial client connections are listened on this port.
        /// </summary>
        public const int Port = 4850;

        public ConfigSetting Config;
        public new LogClient _logger;
        public ZoneStream ZoneStream;

        public DirectoryServer() : base(new Factory(), new DirectoryClient())
        {
            Config = ConfigSetting.Blank;
        }

        public bool Init()
        {
            // Load the zone list from the config here.

            // Sanity Test zones, lifted from SOE zone list.
            var zones = new List<Zone>();
            /*
            var z9 = new Zone(new byte[4] { 64, 37, 134, 136 }, 9669, "[I:Arcade] Frontlines", false, "Frontlines");
            var z6 = new Zone(new byte[4] { 64, 37, 134, 136 }, 9556, "[I:CQ] Faydon Lake", false, "Titan and Collective forces wage war against eachother for control of the Faydon Lake territory. A hybrid zone with CTF/SK/EOL inspiration.");
            var z3 = new Zone(new byte[4] { 64, 37, 134, 136 }, 9957, "[I:CTFX] CTF Extreme", false, "CTFX2 part of the PCT! Submit your PCT entry at: stationgamesfeedback@soe.sony.com");
            var z4 = new Zone(new byte[4] { 64, 37, 134, 136 }, 9318, "[I:RTS] Fleet!", false, "Fleet! 2 Space Navies attempt to destroy the other sides Command Post Satellite!");
            var z5 = new Zone(new byte[4] { 64, 37, 134, 136 }, 9466, "[I:SK] Mechanized Skirmish", false, "Mechanized SKIRMISH! Intermediate level zone. Two opposing forces fight over an area of Titan called \"Kliest's Ridge\". The first team to capture and hold the objectives wins.");
            var z1 = new Zone(new byte[4] { 64, 37, 134, 136 }, 9218, "[I:Sports] GravBall DvS", false, "GravBall is a soccer like sport for the weary soldier. This zone features the Devils vs. Suns.");
            var z7 = new Zone(new byte[4] { 64, 37, 134, 136 }, 9128, "[I:Sports] Soccer Brawl", false, "Soccer Brawl! Football Infantry style!");
            var z8 = new Zone(new byte[4] { 64, 37, 134, 136 }, 9578, "[League] CTFPL", false, "CTF Players League. Please visit http://www.ctfpl.org for details.");
            var z2 = new Zone(new byte[4] { 64, 37, 134, 136 }, 9124, "[League] GravBall League", false, "This zone is for members of the GravBall League to play matches, if you are not a member you can only spectate.");
            var z10 = new Zone(new byte[4] { 64, 37, 134, 136 }, 9126, "[League] SBL", false, "Soccer Brawl League! Point your www browser to www.sbleague.net for information!");
            var z11 = new Zone(new byte[4] { 64, 37, 134, 136 }, 9516, "[League] Skirmish League", false, "Skirmish League. Goto http://www.skirmishleague.com/ for details.");
            var z12 = new Zone(new byte[4] { 64, 37, 134, 136 }, 9224, "[League] Test Zone", true, "League testing zone for new maps");
            var z13 = new Zone(new byte[4] { 64, 37, 134, 136 }, 9857, "[League] Test Zone 2", true, "A cool zone");
            var z14 = new Zone(new byte[4] { 64, 37, 134, 136 }, 9182, "[League] USL", false, "Unified Skirmish League Deathmatch gameplay using unified skirmish settings and based on Mechanized Skirmish and Elsdragon Compound. http://www.uslzone.com");
            var z15 = new Zone(new byte[4] { 64, 37, 134, 136 }, 9628, "Test Zone 1", true, "Test zone 2 part of the PCT! Submit your PCT entry at: stationgamesfeedback@soe.sony.com");
            var z16 = new Zone(new byte[4] { 64, 37, 134, 136 }, 9222, "Test Zone 2", false, "Testing zone for new maps");
            var z17 = new Zone(new byte[4] { 64, 37, 134, 136 }, 9468, "Test Zone 3", true, "Test zone 3 part of the PCT! Submit your PCT entry at: stationgamesfeedback@soe.sony.com");
            var z18 = new Zone(new byte[4] { 64, 37, 134, 136 }, 9290, "Test Zone 4", true, "Test zone 4 part of the PCT! Submit your PCT entry at: stationgamesfeedback@soe.sony.com");
            var z19 = new Zone(new byte[4] { 64, 37, 134, 136 }, 9790, "Test Zone 5", true, "Test zone 5 part of the PCT! Submit your PCT entry at: stationgamesfeedback@soe.sony.com");
            var z20 = new Zone(new byte[4] { 64, 37, 134, 136 }, 9378, "Test Zone 6", true, "Test zone 6 part of the PCT! Submit your PCT entry at: stationgamesfeedback@soe.sony.com");
            var z21 = new Zone(new byte[4] { 64, 37, 134, 136 }, 9334, "Test Zone 7", true, "Test zone 7 part of the PCT! Submit your PCT entry at: stationgamesfeedback@soe.sony.com");
            var z22 = new Zone(new byte[4] { 64, 37, 134, 136 }, 9094, "Test Zone 9", true, "Test zone 9 part of the PCT! Submit your PCT entry at: stationgamesfeedback@soe.sony.com");
            zones.Add(z1);
            zones.Add(z2);
            zones.Add(z3);
            zones.Add(z4);
            zones.Add(z5);
            zones.Add(z6);
            zones.Add(z7);
            zones.Add(z8);
            zones.Add(z9);
            zones.Add(z10);
            zones.Add(z11);
            zones.Add(z12);
            zones.Add(z13);
            zones.Add(z14);
            zones.Add(z15);
            zones.Add(z16);
            zones.Add(z17);
            zones.Add(z18);
            zones.Add(z19);
            zones.Add(z20);
            zones.Add(z21);
            zones.Add(z22);
             */

            var z1 = new Zone(new byte[4] { 207, 191, 144, 208 }, 1337, "[Temp]HellSpawn CTF", false, "HellSpawn's Twin Peaks");
            var z2 = new Zone(new byte[4] { 99, 231, 167, 97 }, 1337, "[Temp]Jovan CTF", false, "Jovan's Twin Peaks");
            var z3 = new Zone(new byte[4] { 97, 81, 195, 81 }, 1337, "[Temp]Mongoose CTF", false, "Geese Twin Peaks");
            var z4 = new Zone(new byte[4] { 97, 81, 195, 81 }, 2001, "Combined Arms", false, "This is it.");

            zones.Add(z1);
            zones.Add(z2);
            zones.Add(z3);
            zones.Add(z4);
            ZoneStream = new ZoneStream(zones);

            return true;
        }

        public void Begin()
        {
            _logger = Log.createClient("Zone");
            base._logger = Log.createClient("Network");

            IPEndPoint listenPoint = new IPEndPoint(IPAddress.Any, 4850);
            begin(listenPoint);

            while(true)
            {
                // No need to do anything at the moment
                // NOTE: Implement autoreloading zone listing? Hmm..
            }
        }
    }
}
