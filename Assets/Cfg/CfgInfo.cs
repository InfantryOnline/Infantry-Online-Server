using System;
using System.Collections.Generic;
using System.IO;

namespace Assets
{
    public partial class CfgInfo
    {
        public Message message;
        public Level level;
        public Arena arena;
        public Addon addon;
        public PublicProfile publicProfile;
        public Timing timing;
        public Soul soul;
        public PublicColors publicColors;
        public Los lOS;
        public Vehicle vehicle;
        public View view;
        public WebMenu webMenu;
        public Bounty bounty;
        public HelpMenu helpMenu;
        public Cash cash;
        public Experience experience;
        public Rpg rpg;
        public Door door;
        public Attribute attribute;
        public Point point;
        public List<Terrain> terrains = new List<Terrain>();
        public Bong bong;
        public Flag flag;
        public Sound sound;
        public List<TeamInfo> teams = new List<TeamInfo>();
        public Soccer soccer;
        public StartGame startGame;
        public King king;
        public ZoneStat zoneStat;
        public Render render;
        public Owner owner;
        public WarpGroup warpGroup;
        public Expire expire;
        public Radar radar;
        public Rank rank;
        public Rts rts;
        public TeamDefault teamDefault;
        public Event EventInfo;
        public DeathMatch deathMatch;
        public SoccerMvp soccerMvp;
        public Stat stat;
        public Bubble bubble;
        public List<NamedArena> arenas = new List<NamedArena>();
        public Jackpot jackpot;
        public QuickSkill quickSkill;
        public Uiart uiart;
        public Uiwav uiwav;
        public UiartMetrics uiartMetrics;
        public List<LosType> losTypes = new List<LosType>();
        public FixedStat fixedStat;
        public UiartFont uiartFont;
        public DamageType damageType;
        public HeldCategory heldCategory;
        public FlagMvp flagMvp;
        public RtsStateDefault rtsStateDefault;
        public static List<string> BlobsToLoad = new List<string>();

        public static CfgInfo Load(string filename)
        {
            TextReader reader = new StreamReader(filename);
            string line = "";
            CfgInfo cfgInfo = new CfgInfo();
            Dictionary<string, Dictionary<string, string>> stringTree =
                new Dictionary<string, Dictionary<string, string>>();
            Dictionary<string, string> values = new Dictionary<string, string>();
            string currentHeader = "";
            string removeFromParsing = "[]";

                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length > 0 && line[0] == '[')
                    {
                        if (values.Count > 0)
                        {
                            if (!stringTree.ContainsKey(currentHeader))
                                stringTree.Add(currentHeader, values);
                        }
                        values = new Dictionary<string, string>();
                        currentHeader = line.Trim(removeFromParsing.ToCharArray());
                    }
                    else if (line.Length > 0)
                    {
                        int eqIdx = line.IndexOf('=');
                        if (!values.ContainsKey(line.Substring(0, eqIdx)))
                        {
                            if (eqIdx == -1)
                                values.Add(line, "");
                            else
                                values.Add(line.Substring(0, eqIdx), line.Substring(eqIdx + 1));
                        }
                    }
                }
                if (!stringTree.ContainsKey(currentHeader))
                    stringTree.Add(currentHeader, values);

                cfgInfo.message = new Message(ref stringTree);
                cfgInfo.addon = new Addon(ref stringTree);
                cfgInfo.level = new Level(ref stringTree);
                cfgInfo.arena = new Arena(ref stringTree);
                cfgInfo.publicProfile = new PublicProfile(ref stringTree);
                cfgInfo.timing = new Timing(ref stringTree);
                cfgInfo.soul = new Soul(ref stringTree);
                cfgInfo.publicColors = new PublicColors(ref stringTree);
                cfgInfo.lOS = new Los(ref stringTree);
                cfgInfo.vehicle = new Vehicle(ref stringTree);
                cfgInfo.view = new View(ref stringTree);
                cfgInfo.webMenu = new WebMenu(ref stringTree);
                cfgInfo.bounty = new Bounty(ref stringTree);
                cfgInfo.helpMenu = new HelpMenu(ref stringTree);
                cfgInfo.cash = new Cash(ref stringTree);
                cfgInfo.experience = new Experience(ref stringTree);
                cfgInfo.rpg = new Rpg(ref stringTree);
                cfgInfo.door = new Door(ref stringTree);
                cfgInfo.attribute = new Attribute(ref stringTree);
                cfgInfo.point = new Point(ref stringTree);
                for (int i = 0; i <= 15; i++)
                {
                    cfgInfo.terrains.Add(new Terrain(ref stringTree, i));
                }
                cfgInfo.bong = new Bong(ref stringTree);
                cfgInfo.flag = new Flag(ref stringTree);
                cfgInfo.sound = new Sound(ref stringTree);
                for (int i = 0; i <= 49; i++)
                {
                    if (!stringTree.ContainsKey("TeamInfo" + i))
                        continue;

                    cfgInfo.teams.Add(new TeamInfo(ref stringTree, i));
                }
                cfgInfo.soccer = new Soccer(ref stringTree);
                cfgInfo.startGame = new StartGame(ref stringTree);
                cfgInfo.king = new King(ref stringTree);
                cfgInfo.zoneStat = new ZoneStat(ref stringTree);
                cfgInfo.render = new Render(ref stringTree);
                cfgInfo.owner = new Owner(ref stringTree);
                cfgInfo.warpGroup = new WarpGroup(ref stringTree);
                cfgInfo.expire = new Expire(ref stringTree);
                cfgInfo.radar = new Radar(ref stringTree);
                cfgInfo.rank = new Rank(ref stringTree);
                cfgInfo.rts = new Rts(ref stringTree);
                cfgInfo.teamDefault = new TeamDefault(ref stringTree);
                cfgInfo.EventInfo = new Event(ref stringTree);
                cfgInfo.deathMatch = new DeathMatch(ref stringTree);
                cfgInfo.soccerMvp = new SoccerMvp(ref stringTree);
                cfgInfo.stat = new Stat(ref stringTree);
                cfgInfo.bubble = new Bubble(ref stringTree);
                for (int i = 0; i <= 19; i++)
                {
                    if (!stringTree.ContainsKey("NamedArena" + i))
                        continue;

                    cfgInfo.arenas.Add(new NamedArena(ref stringTree, i));
                }
                cfgInfo.jackpot = new Jackpot(ref stringTree);
                cfgInfo.quickSkill = new QuickSkill(ref stringTree);
                cfgInfo.uiart = new Uiart(ref stringTree);
                cfgInfo.uiwav = new Uiwav(ref stringTree);
                cfgInfo.uiartMetrics = new UiartMetrics(ref stringTree);
                for (int i = 0; i <= 7; i++)
                {
                    cfgInfo.losTypes.Add(new LosType(ref stringTree, i));
                }
                cfgInfo.fixedStat = new FixedStat(ref stringTree);
                cfgInfo.uiartFont = new UiartFont(ref stringTree);
                cfgInfo.damageType = new DamageType(ref stringTree);
                cfgInfo.heldCategory = new HeldCategory(ref stringTree);
                cfgInfo.flagMvp = new FlagMvp(ref stringTree);
                cfgInfo.rtsStateDefault = new RtsStateDefault(ref stringTree);

            return cfgInfo;
        }

        private class Parser
        {
            public static Dictionary<string, string> values = new Dictionary<string, string>();

            public static int GetInt(string value)
            {
                value = value.Trim();

				if (values.ContainsKey(value))
				{	
					string result = values[value];

					//Empty?
					if (result == "")
						//Assume zero
						return 0;

					try
					{	//Hexadecimal integer?
						if (result.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
							return Int32.Parse(result.Substring(2), System.Globalization.NumberStyles.AllowHexSpecifier);
						else
							return Int32.Parse(values[value]);
					}
					catch (OverflowException)
					{	//Just keep it capped
						return Int32.MaxValue;
					}
				}
				else
					return 0;
            }

            public static string GetString(string value)
            {
                value = value.Trim();

                if (values.ContainsKey(value))
                    return values[value];
                else
                    return "";
            }

            public static bool GetBool(string value)
            {
                value = value.Trim();

                if (values.ContainsKey(value))
                    return Convert.ToBoolean(Int16.Parse(values[value]));
                else
                    return false;
            }

            public static string GetBlob(string value)
            {
                value = value.Trim();

                if (value.Contains(","))
                {
                    string[] tmp = new string[2];
                    tmp = value.Split(',');
                    return tmp[0];
                }
                else
                {
                    return value;
                }
            }
        }
    }
}
