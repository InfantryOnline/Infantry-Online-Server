using System;
using System.Collections.Generic;
using System.Linq;

using InfServer.Game;

namespace InfServer.Script.GameType_CTFHQ
{
    public class Headquarters
    {
        #region HQ Object
        public class Headquarter
        {
            //Private members
            private Team _team;                                 //The team we belong to
            private int _bounty;                                //Our current bounty
            private int[] _bountyLevels;                        //The bounty required to level up
            
            //Events
            public event Action<Team> LevelModify;

            //Properties
            /// <summary>
            /// Gets the headquarters team
            /// </summary>
            public Team Team
            {
                get
                {
                    return _team;
                }
            }

            /// <summary>
            /// Gets or sets the headquarters bounty
            /// </summary>
            public int Bounty
            {
                get
                {
                    return _bounty;
                }
                set
                {
                    int oldlvl = Level;
                    //Everytime bounty is recalculated, check to see for a level change
                    _bounty = value;
                    if (Level != oldlvl && LevelModify != null)
                        //Let our subscribers know!
                        LevelModify(Team);
                }
            }

            /// <summary>
            /// Gets the headquarters level
            /// </summary>
            public int Level
            {
                get
                {
                    //Calculate our current level
                    for (int i = _bountyLevels.Count() - 1; i >= 0; i--)
                        if (_bounty >= _bountyLevels[i])
                            return i + 1;
                    return 0;
                }
            }

            //Public accessors
            public Headquarter(Team team, int bounty, int[] levels)
            {
                _team = team;
                _bounty = bounty;
                _bountyLevels = levels;
            }
        }
        #endregion

        //Private members
        private List<Headquarter> _headquarters;        //Our collection of headquarters
        private int[] _bountyLevels;                    //The bounty required to level up

        public event Action<Team> LevelModify;

        /// <summary>
        /// Creates a Headquarters tracking object
        /// </summary>
        public Headquarters(int[] levels)
        {
            _headquarters = new List<Headquarter>();
            _bountyLevels = levels;
        }

        /// <summary>
        /// Gets a teams headquarters object
        /// </summary>
        public Headquarter this[Team t]
        {
            get
            {
                return _headquarters.FirstOrDefault(hq => hq.Team == t);
            }
        }

        /// <summary>
        /// Creates a headquarter to track
        /// </summary>
        public void Create(Team team)
        {
            Headquarter newhq = new Headquarter(team, 0, _bountyLevels);
            newhq.LevelModify += onLevelModify;
            _headquarters.Add(newhq);
        }

        /// <summary>
        /// Destroys and stops tracking a headquarter
        public void Destroy(Team team)
        {
            _headquarters.RemoveAll(hq => hq.Team == team);
        }

        private void onLevelModify(Team team)
        {
            if (LevelModify != null)
                LevelModify(team);
        }
    }
}
