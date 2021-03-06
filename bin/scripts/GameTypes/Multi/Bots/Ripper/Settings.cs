﻿using System;
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
    public partial class Ripper : Bot
    {
        public float farDist = 4.6f;                        //The distance from the player where we actively pursue them
        public float runDist = 2.6f;                        //The distance from the player where we run away!
        public float shortDist = 3.0f;                      //The distance from the player where we keep our distance
        public float patrolDist = 5.1f;                     //The distance from our patrol points where we turn around and pursue the other point
        public float retreatDist = 6.1f;                     //The distance from our patrol points where we turn around and pursue the other point
        public float fireDist = 10.3f;                      //The distance from the player where we keep our distance
        private const int c_MaxPath = 350;
        public const int c_DistanceLeeway = 500;

        public const int c_pathUpdateInterval = 15000;      //The amount of ticks before a bot will renew it's path
        public const int c_MaxRespawnDist = 1500;           //The maximum distance bots can be spawned from the players
        public const int c_playerMaxRangeEnemies = 2100;    //The max range the bot will consider enemies a threat
        public const int c_playerRangeFire = 1000;          //The range the bot will consider firing at enemies
    }
}
