using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Logic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;

using Assets;

namespace InfServer.Script.GameType_ZombieZone
{	/// <summary>
    /// Represents a type of zombie, including the amount it should spawn
    /// </summary>
    public class ZombieType
    {
        public VehInfo vehicleType;
        public Type classType;
        public int spawnWeight;

        public Action<ZombieBot> zombieSetup;

        public ZombieType(Type _classType, VehInfo _vehicleType)
        {
            vehicleType = _vehicleType;
            classType = _classType;
        }

        public ZombieType(Type _classType, VehInfo _vehicleType, Action<ZombieBot> _zombieSetup)
        {
            vehicleType = _vehicleType;
            classType = _classType;
            zombieSetup = _zombieSetup;
        }
    }

    public static class ZombieExtensions
    {
        public static List<ZombieType> addType(this List<ZombieType> zombieTypes, ZombieType type, int spawnWeight)
        {
            if (spawnWeight <= 0)
                return zombieTypes;

            ZombieType newType = new ZombieType(type.classType, type.vehicleType);

            newType.zombieSetup = type.zombieSetup;
            newType.spawnWeight = spawnWeight;

            zombieTypes.Add(newType);

            return zombieTypes;
        }
    }

    public class ZombieParameters
    {
        public int playing;
        public int camp;
        public int separation;
        public int pauses;
        public int kingKills;
        public float kingLevel;

        public int vehicleClass;  //how much we increase multiplier due to vehicle

        public ZombieParameters() { }
    }
    
    public class TickerInfo
    {
        public Player player;
        public Script_ZombieZone.TeamState state;
        
        //mnemonic for when this is about zombies; absolutely no check for this
        public Script_ZombieZone.TeamState targetState
        {
            get
            {
                return state;
            }
        }
        
        public int time; //amount of time remaining
        
        public string timeString
        {
            get
            {
                return String.Format("{0}:{1}",time/60, (time < 10 ? "0" : "") + (time % 60) );
            }
        }
        
        public TickerInfo(Player p, Script_ZombieZone.TeamState s, int t) 
        {
            player = p;
            state = s;
            time = t;
        }
    }

    /// <summary>
    /// Allows the zone to make smooth transitions between zombie types
    /// </summary>
    public class ZombieTransitions
    {	///////////////////////////////////////////////////
        // Member Variables
        ///////////////////////////////////////////////////
        private List<ZombieTransition> transitions;

        private int transIdx;
        private int tickStartPause;
        private int tickNextTransition;

        public int currentLevel;

        public const int defaultSpawnRate = 800;
        public const int defaultMinSpawn = 1000;
        public const int defaultMaxSpawn = 1500;
        public const int numFinalWaves = 2;     //the number of final waves we're inserting here; affects where subsequent transitions are inserted into list.  (Note: includes grace period.)

        public delegate List<ZombieType> ZombieComposition(ZombieParameters parameters);
        public delegate int ZombieInt(ZombieParameters parameters);
        public delegate void Spawner(ZombieType ztype, Helpers.ObjectState state = null, int minDist = 0, int maxDist = 0);
        public delegate string TickerMessage(TickerInfo info);

        public Spawner defaultSpawner;

        #region Zombie Types
        static public ZombieType AlienZombie = new ZombieType(typeof(ZombieBot), AssetManager.Manager.getVehicleByID(211));
        static public ZombieType HumanZombie = new ZombieType(typeof(ZombieBot), AssetManager.Manager.getVehicleByID(250));
        static public ZombieType SuicideZombie = new ZombieType(typeof(SuicideZombieBot), AssetManager.Manager.getVehicleByID(109));
        static public ZombieType RangedZombie = new ZombieType(typeof(RangedZombieBot), AssetManager.Manager.getVehicleByID(108),
            delegate(ZombieBot zombie)
            {
                RangedZombieBot z = zombie as RangedZombieBot;
                z.farDist = 3.4f; z.shortDist = 2.6f; z.runDist = 2.0f;
            }
        );
        static public ZombieType HiveZombie = new ZombieType(typeof(ZombieBot), AssetManager.Manager.getVehicleByID(105));
        static public ZombieType LairZombie = new ZombieType(typeof(ZombieBot), AssetManager.Manager.getVehicleByID(123));
        //static public ZombieType HiveZombie = new ZombieType(typeof(DuelBot), AssetManager.Manager.getVehicleByID(105));
        //static public ZombieType LairZombie = new ZombieType(typeof(DuelBot), AssetManager.Manager.getVehicleByID(123));

        static public ZombieType InfectedZombie = new ZombieType(typeof(ZombieBot), AssetManager.Manager.getVehicleByID(256));
        static public ZombieType InfestedZombie = new ZombieType(typeof(ZombieBot), AssetManager.Manager.getVehicleByID(257));
        static public ZombieType InfatuatedZombie = new ZombieType(typeof(ZombieBot), AssetManager.Manager.getVehicleByID(258));
        static public ZombieType PredatorZombie = new ZombieType(typeof(ZombieBot), AssetManager.Manager.getVehicleByID(103));
        static public ZombieType DerangedZombie = new ZombieType(typeof(RangedZombieBot), AssetManager.Manager.getVehicleByID(104),
            delegate(ZombieBot zombie)
            {
                RangedZombieBot z = zombie as RangedZombieBot;
                z.farDist = 3.4f; z.shortDist = 2.6f; z.runDist = 2.0f;
            }
        );

        static public ZombieType RageZombie = new ZombieType(typeof(RangedZombieBot), AssetManager.Manager.getVehicleByID(125),
            delegate(ZombieBot zombie)
            {
                RangedZombieBot z = zombie as RangedZombieBot;
                z.farDist = 3.4f; z.shortDist = 2.6f; z.runDist = 2.0f;
            }
        );
        static public ZombieType RepulsorZombie = new ZombieType(typeof(DualZombieBot), AssetManager.Manager.getVehicleByID(106),
            delegate(ZombieBot zombie)
            {
                DualZombieBot z = zombie as DualZombieBot;
                z.wepSwitchDist = 2.0f;
                z.bNoAimFar = true;
            }
        );
        static public ZombieType KamikazeZombie = new ZombieType(typeof(SuicideZombieBot), AssetManager.Manager.getVehicleByID(111));
        static public ZombieType DestroyerZombie = new ZombieType(typeof(SuicideZombieBot), AssetManager.Manager.getVehicleByID(131));
        static public ZombieType DeathZombie = new ZombieType(typeof(SuicideZombieBot), AssetManager.Manager.getVehicleByID(122));
        static public ZombieType AcidZombie = new ZombieType(typeof(DualZombieBot), AssetManager.Manager.getVehicleByID(100),
            delegate(ZombieBot zombie)
            {
                DualZombieBot z = zombie as DualZombieBot;
                z.wepSwitchDist = 1.5f;
            }
        );
        static public ZombieType AsgardianZombie = new ZombieType(typeof(ZombieBot), AssetManager.Manager.getVehicleByID(251));
        static public ZombieType KryptonianZombie = new ZombieType(typeof(ZombieBot), AssetManager.Manager.getVehicleByID(252));
        static public ZombieType DisruptorZombie = new ZombieType(typeof(RangedZombieBot), AssetManager.Manager.getVehicleByID(101),
            delegate(ZombieBot zombie)
            {
                RangedZombieBot z = zombie as RangedZombieBot;
                z.farDist = 3.4f; z.shortDist = 2.6f; z.runDist = 2.0f;
            }
        );

        static public ZombieType DoomZombie = new ZombieType(typeof(DualZombieBot), AssetManager.Manager.getVehicleByID(124),
            delegate(ZombieBot zombie)
            {
                DualZombieBot z = zombie as DualZombieBot;
                z.wepSwitchDist = 1.5f;
            }
        );
        #endregion Zombie Types

        ///////////////////////////////////////////////////
        // Member Classes
        ///////////////////////////////////////////////////
        /// <summary>
        /// Represents the zombie composition of a snapshot in game time
        /// </summary>
        public class ZombieTransition
        {
            public Action<Script_ZombieZone.TeamState> started;	//optionally set, called when the transition is started
            public Action<Script_ZombieZone.TeamState> final;	//optionally set, called when the transition is in final stage (should use tryFinalAction)
            public Action<Script_ZombieZone.TeamState> ended;	//optionally set, called when the transition is ending

            public ZombieComposition initialWave = emptyComposition(), spawnComposition = emptyComposition();

            public ZombieInt initialZombieCount
            {
                get
                {
                    return InitialZombieCount;
                }

                set
                {
                    InitialZombieCount = parameters => Math.Max(0, value(parameters));
                }
            }

            public ZombieInt finalZombieCount
            {
                get
                {
                    return FinalZombieCount;
                }

                set
                {
                    FinalZombieCount = parameters => Math.Max(0, value(parameters));
                }
            }

            public ZombieInt spawnRate = constInt(defaultSpawnRate), minSpawnDistance = constInt(defaultMinSpawn), maxSpawnDistance = constInt(defaultMaxSpawn), InitialZombieCount = constInt(0), FinalZombieCount = constInt(0);

            private int time_initialToFinal;                      //The amount of time between initial and final spawn
            private int time_spawnAfterFinal;                         //The amount of time we continue to spawn after final

            private int time_initialStarted = -1;  //when did we actually start the transition (or pretend to, if we paused)
            private int time_resume = -1;          //if -1, n/a.  otherwise we resume at this time.

            private bool performed_finalAction = false;

            public Spawner spawner;

            public int level;     //how hard is this transition (currently used for cloak/pheremone)

            public TickerMessage[] humanTickers, zombieTickers;

            public int initialTime
            {
                get
                {
                    return time_initialToFinal;
                }

                set
                {
                    time_initialToFinal = value * 1000;
                }
            }

            public int finalTime
            {
                get
                {
                    return time_spawnAfterFinal;
                }

                set
                {
                    time_spawnAfterFinal = value * 1000;
                }
            }

            public ZombieTransition(Spawner s, int lev = 0)
            {
                spawner = s;
                level = lev;
                initialWave = emptyComposition();
                humanTickers = new TickerMessage[Script_ZombieZone.numTickers];
                zombieTickers = new TickerMessage[Script_ZombieZone.numTickers];
            }


            public void tryFinalAction(Script_ZombieZone.TeamState state)
            {
                if (!performed_finalAction)
                {
                    if (final != null)
                        final(state);

                    performed_finalAction = true;
                }
            }

            public int zombieCount(int now, ZombieParameters parameters)
            {
                if (time_initialStarted < 0 || time_resume >= 0 && now < time_resume)  //if we haven't started yet or are waiitng to resume
                    return 0;
                else if (isDone(now))
                    return 0;
                else if (now > time_initialStarted + time_initialToFinal) //we've past the final time
                    return finalZombieCount(parameters);
                else if (time_initialToFinal == 0)  //avoid division by 0
                    return Math.Max(initialZombieCount(parameters), finalZombieCount(parameters));
                else  //returns the linear interpolation between initial and final
                {
                    return initialZombieCount(parameters) * (time_initialStarted + time_initialToFinal - now) / time_initialToFinal + finalZombieCount(parameters) * (now - time_initialStarted) / time_initialToFinal;
                }
            }

            public void start(int now, ZombieParameters parameters)
            {
                if (time_initialStarted >= 0)
                {
                    Log.write(TLog.Error, "Tried to start transition that has already started.");
                    return;
                }

                //spawns initial wave
                List<ZombieType> wave = initialWave(parameters);
                int minD = minSpawnDistance(parameters);
                int maxD = maxSpawnDistance(parameters);

                foreach (ZombieType zt in wave)
                    for (int i = 0; i < zt.spawnWeight; ++i)
                        spawner(zt, minDist: minD, maxDist: maxD);

                time_initialStarted = now;

            }

            public bool notStartedYet()
            {
                return time_initialStarted < 0;
            }

            public int endTick()
            {
                if (notStartedYet())
                    return -1;

                return time_initialStarted + time_initialToFinal + time_spawnAfterFinal;
            }

            public int secondsUntilEnd(int now)
            {
                if (notStartedYet())
                    return -1;

                return (endTick() - now) / 1000;
            }

            public bool isDone(int now)
            {
                return time_initialStarted > 0 && now > endTick();
            }

            public bool finaleStarted(int now)
            {
                return time_initialStarted > 0 && now > time_initialStarted + time_initialToFinal;
            }

            public void pauseFor(int pause, int now)  //pause and now are in ticks
            {
                if (time_initialStarted < 0)
                    Log.write(TLog.Error, "Tried to set resumption for transition that hasn't started yet.");
                else if (pause > 0)
                {
                    time_initialStarted += pause;   //pretend that we started later in order to resume in the right place
                    time_resume = now + pause;
                }
            }

            //the number of seconds until we resume (or -1 if we're not waiting)
            public int secondsUntilResume(int now)
            {
                if (notStartedYet() || isDone(now) || time_resume < now)
                    return -1;

                return (time_resume - now) / 1000;
            }

            public void spawnRandomType(Random rand, ZombieParameters parameters)
            {
                spawner(getRandomType(rand, parameters), minDist: minSpawnDistance(parameters), maxDist: maxSpawnDistance(parameters));
            }

            public ZombieType getRandomType(Random rand, ZombieParameters parameters)
            {
                return getRandomType(rand, spawnComposition(parameters));
            }

            public ZombieType getRandomType(Random rand, List<ZombieType> types)
            {
                int maxWeight = 0;

                foreach (ZombieType zt in types)
                    maxWeight += zt.spawnWeight;

                int chosen = rand.Next(maxWeight + 1);

                foreach (ZombieType zt in types)
                {
                    chosen -= zt.spawnWeight;
                    if (chosen <= 0)
                        return zt;
                }

                Log.write(TLog.Error, "ZombieTransition getType fell through; spawnComposition was probably not defined.");
                return AlienZombie;
            }
        }

        ///////////////////////////////////////////////////
        // Member Functions
        ///////////////////////////////////////////////////
        /// <summary>
        /// Constructor
        /// </summary>
        public ZombieTransitions(Spawner s)
        {	//Start with no transition!
            transIdx = -1;
            transitions = new List<ZombieTransition>();

            defaultSpawner = s;

            addMastarPhase();
        }

        #region Transition setup
        public void addMastarPhase()
        {
            ZombieTransition trans = new ZombieTransition(defaultSpawner, 3);

            trans.spawnComposition = constComposition((new List<ZombieType>()).addType(DeathZombie, 5).addType(DerangedZombie, 1));
            trans.initialZombieCount = constInt(5);
            trans.initialTime = 20;
            trans.finalZombieCount = constInt(15);
            trans.finalTime = 30;
            trans.spawnRate = constInt(100);
            trans.minSpawnDistance = constInt(0);
            trans.maxSpawnDistance = constInt(30);

            trans.started = delegate(Script_ZombieZone.TeamState state)
            {
                state.wipeOut();
                state.team.sendArenaMessage("mastar hasn't coded this far.  Have a good next game.");
                state.zombieMessage("It is Time for those who DIE and DIE AGAIN - DIE ONCE MORE.");
            };

            trans.final = delegate(Script_ZombieZone.TeamState state)
            {
                state.wipeOut();
                state.team.sendArenaMessage("You've beaten the game somehow.  Time to die/spec.");
                state.zombieMessage("The infection contained within all of us now emerges.  They were doomed to failure from the start.");
            };

            trans.ended = delegate(Script_ZombieZone.TeamState state)
            {
                foreach (Player player in state.team.ActivePlayers.ToList())
                    player.inventoryModify(AssetManager.Manager.getItemByName("MinusHealth"), 500);
            };

            transitions.Add(trans);

            //specs them if the previous kill failed.  20 seconds after to clean up.
            trans = gracePeriod(2, false);
            trans.final = delegate(Script_ZombieZone.TeamState state)
            {
                foreach (Player player in state.team.ActivePlayers.ToList())
                    if (!player.IsDead && !player.isZombie())
                        player.spec("spec");
            };
            trans.finalTime = 20;
            transitions.Add(trans);
        }

        //adds a grace period to the list
        public ZombieTransition gracePeriod(int seconds, bool addToList = true)
        {
            ZombieTransition grace = new ZombieTransition(emptySpawner(), currentLevel);
            grace.initialTime = seconds;

            if (addToList)
                addTransition(grace);

            return grace; //returns it in case we want to add any messages to it
        }

        public static Spawner emptySpawner()
        {
            return delegate(ZombieType ztype, Helpers.ObjectState state, int minDist, int maxDist) { };
        }

        public static ZombieComposition emptyComposition()
        {
            return constComposition(new List<ZombieType>());
        }

        public static ZombieComposition constComposition(List<ZombieType> types)
        {
            return (parameters) => types;
        }


        public static ZombieInt constInt(int i)
        {
            return (parameters) => i;
        }


        #endregion

        public void pop()
        {
            if (transitions.Count > 0)
                transitions.RemoveAt(0);
            else
                Log.write(TLog.Error, "Tried to pop from empty transition list.");
        }

        public ZombieTransition currentTransition()
        {
            if (transitions.Count > 0)
                return transitions[0];
            else
            {
                Log.write(TLog.Error, "Tried to get transition from empty list.");
                return gracePeriod(10, false);
            }
        }

        public ZombieTransition newTransition(bool addToList = true)
        {
            return newTransition(defaultSpawner, addToList);
        }

        public ZombieTransition newTransition(Spawner s, bool addToList = true)
        {
            ZombieTransition trans = new ZombieTransition(s, currentLevel);

            if (addToList)
                addTransition(trans);

            return trans;
        }

        //adds transition to the back, prior to any class-defined final waves.
        public void addTransition(ZombieTransition newTransition)
        {
            transitions.Insert(transitions.Count - numFinalWaves, newTransition);
        }

    }
}
