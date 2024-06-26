using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;


using InfServer.Game;
using InfServer.Protocol;
using InfServer.Scripting;
using InfServer.Bots;

using Assets;
using Axiom.Math;
using Bnoerj.AI.Steering;

namespace InfServer.Script.GameType_Multi
{   // Script Class
    /// Provides the interface between the script and bot
    ///////////////////////////////////////////////////////
    public partial class EliteHeavy : Bot
    {
        protected bool bBoolFreezeActions;

        public void pollForActions(int now)
        {
            if (bBoolFreezeActions)
                return;

            Action.Priority nextPriority;

            nextPriority = shouldfireAtEnemy();
            if (nextPriority > Action.Priority.None)
                newAction(nextPriority, Action.Type.fireAtEnemy);

            nextPriority = shouldRetreat();
            if (nextPriority > Action.Priority.None)
                newAction(nextPriority, Action.Type.retreat);


        }

        public void newAction(Action.Priority priority, Action.Type type)
        {
            _actionQueue.Add(new Action(priority, type));
        }




        public Action.Priority shouldfireAtEnemy()
        {
            IEnumerable<Player> enemies = enemiesInRange();
            IEnumerable<Player> friendlies = friendliesInRange();

            int enemycount = 0;
            int friendlycount = 0;
            foreach (Player enemy in enemies)
            {
                if (Helpers.isInRange(c_playerRangeFire, _state, enemy._state))
                    enemycount++;
            }

            foreach (Player friendly in friendlies)
            {
                if (Helpers.isInRange(1000, _state, friendly._state))
                    friendlycount++;
            }

            //Meh
            if (enemycount == 1)
                return Action.Priority.Low;

            //Now we're talking
            if (enemycount > 1)
                return Action.Priority.Medium;

            //Smoke em if ya got em
            if (enemycount > 2)
                return Action.Priority.High;

            return Action.Priority.None;
        }

        public Action.Priority shouldRetreat()
        {
            int enemycount = 0;
            int friendlycount = 0;

            friendlycount = friendliesInRange().Count();
            enemycount = enemiesInRange().Count();
            friendlycount += getFriendlyBotsInRange().Count();
            enemycount += getEnemyBotsInRange().Count();


            return Action.Priority.None;
        }
    }
}
