using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Logic;
using InfServer.Game;

namespace InfServer.Script.GameType_Burnt
{
    public class VoteSystem
    {
        private List<Player> Voters;
        private List<Vote> Votes;

        private class Vote
        {
            public GameTypes GameType;
            public int Count;

            public Vote(GameTypes type)
            {
                GameType = type;
                Count = 0;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public VoteSystem()
        {
            Voters = new List<Player>();
            Votes = new List<Vote>();

            foreach (GameTypes type in Settings.AllowedGameTypes)
            {
                Votes.Add(new Vote(type));
            }
        }

        /// <summary>
        /// Returns the winning vote
        /// </summary>
        public GameTypes GetWinningVote()
        {
            int highCount = 0;
            GameTypes Winner = GameTypes.NULL;

            foreach (Vote vote in Votes)
            {
                if (vote.Count > highCount)
                {
                    Winner = vote.GameType;
                    highCount = vote.Count;
                }
            }
            return Winner;
        }

        /// <summary>
        /// Adds a vote to the vote list
        /// </summary>
        public bool AddVote(GameTypes gameType, Player player)
        {
            if (gameType == GameTypes.NULL)
            {
                return false;
            }

            if (!CanVote(player))
            {
                Voters.Add(player);
                GetVote(gameType).Count++;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Did this person already vote?
        /// </summary>
        private bool CanVote(Player player)
        {
            return Voters.Contains(player);
        }

        private Vote GetVote(GameTypes gameType)
        {
            try
            {
                return Votes.Single(type => type.GameType == gameType);
            }
            catch (Exception e)
            {
                Log.write(TLog.Error, e.ToString());
                foreach (Vote v in Votes)
                {
                    Log.write(TLog.Error, v.GameType.ToString());
                    Console.WriteLine("vote: " + v.GameType);
                }

                //Use first as default
                return Votes.First(type => type.GameType == gameType);
            }
        }
    }
}
