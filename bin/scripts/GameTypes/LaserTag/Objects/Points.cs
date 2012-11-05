using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


using InfServer.Logic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;

namespace InfServer.Script.GameType_LaserTag
{
    public class Points
    {
        //Our ticket dictionary <int teamid, int points>
        private Dictionary<int, int> _points;

        private int _startCount;
        private int _maxCount;
        public int StartingPoints { get { return _startCount; } set { _startCount = value; } }
        public int MaxPoints { get { return _maxCount; } set { _maxCount = value; } }

        //Our event triggered by modifications to point count
        public event Action<Team, int> PointModify;

        /// <summary>
        /// Creates a new collection of points indexed by team ids
        /// </summary>
        /// <param name="teams">List of teams</param>
        /// <param name="numPoints">Starting number of points</param>
        public Points(IEnumerable<Team> teams, int start, int max)
        {
            _points = new Dictionary<int, int>();
            _startCount = start;
            _maxCount = max;
            foreach (Team t in teams)
                _points.Add(t._id, _startCount);
        }

        /// <summary>
        /// Gets or sets number of points for a specific team
        /// </summary>
        public int this[Team team]
        {
            get
            {
                return (_points.Keys.Contains(team._id)) ? _points[team._id] : 0;
            }
            set
            {
                //Does it exist?
                if (!_points.Keys.Contains(team._id))
                    //Add it
                    _points.Add(team._id, _startCount);

                //Update the value
                _points[team._id] = value;

                //Now run some events
                if (PointModify != null)
                    PointModify(team, _points[team._id]);
            }
        }
    }
}