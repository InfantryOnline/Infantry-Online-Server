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
    public partial class Dropship : Bot
    {
        protected bool bBoolFreezeActions;

        public void pollForActions(int now)
        {
            if (bBoolFreezeActions)
                return;

            Action.Priority nextPriority;

            nextPriority = moveToDropLocation();
            if (nextPriority > Action.Priority.None)
                newAction(nextPriority, Action.Type.moveToDrop);

            nextPriority = leaveMap();
            if (nextPriority > Action.Priority.None)
                newAction(nextPriority, Action.Type.leaveMap);


        }

        public void newAction(Action.Priority priority, Action.Type type)
        {
            _actionQueue.Add(new Action(priority, type));
        }

        public Action.Priority moveToDropLocation()
        {
            Action.Priority result = Action.Priority.None;

            if (_bDropped == false)
                return Action.Priority.High;

            return result;
        }

        public Action.Priority leaveMap()
        {
            Action.Priority result = Action.Priority.None;


           if (_bDropped)
               return Action.Priority.High;


            return result;
        }
    }
}
