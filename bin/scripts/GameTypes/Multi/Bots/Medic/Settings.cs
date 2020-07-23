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
    public partial class Medic : Bot
    {
        public float farDist = 3.4f;                        //The distance from the player where we actively pursue them
        public float shortDist = 2.8f;                      //The distance from the player where we keep our distance
        public float runDist = 2.0f;                        //The distance from the player where we run away!
        public float healfarDist = 2.1f;                    //The distance from the player where we actively pursue them
        public float patrolDist = 5.1f;                     //The distance from our patrol points where we turn around and pursue the other point

        private const int c_MaxPath = 350;

        public const int c_pathUpdateInterval = 15000;      //The amount of ticks before a bot will renew it's path
        public const int c_MaxRespawnDist = 1500;           //The maximum distance bots can be spawned from the players
        public const int c_DistanceLeeway = 500;
        public const int c_lowHealth = 68;                  //The percentage we consider a player to be low health
        public const int c_playerMaxRangeHeals = 2200;      //The max range the bot will look for players to heal
        public const int c_playerMaxRangeEnemies = 2100;    //The max range the bot will consider enemies a threat
        public const int c_playerRangeFire = 800;          //The range the bot will consider firing at enemies
        public const int c_playerRangeSafety = 400;         //The range the bot will look for teammates to consider itself safe (not firing)
        public const int c_maxEnergy = 250;                 //Our max/starting Energy
        public const int c_energyRechargeRate = 100;        //Our recharge rate 
    }
}
