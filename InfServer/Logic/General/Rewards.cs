using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using InfServer.Protocol;
using InfServer.Game;

using Assets;

namespace InfServer.Logic
{	// Logic_Rewards Class
	/// Handles various rewards-related functions
	///////////////////////////////////////////////////////
	public partial class Logic_Rewards
	{
		/// <summary>
		/// Calculates and distributes rewards for a turret kill
		/// </summary>		
        static public void calculateTurretKillRewards(Player victim, Computer comp, CS_VehicleDeath update)
        {	//Does it have a valid owner?
            Player owner = comp._creator;
            if (owner == null)
                return;

            //x2Rewards
            bool x2 = victim._server._config["zone/DoubleReward"].boolValue;

            //Calculate kill reward for the turret owner
            CfgInfo cfg = victim._server._zoneConfig;
            int killerCash = (int)(cfg.cash.killReward +
                (victim.Bounty * (((float)cfg.cash.percentOfTarget) / 1000)));
            int killerExp = (int)(cfg.experience.killReward +
                (victim.Bounty * (((float)cfg.experience.percentOfTarget) / 1000)));
            int killerPoints = (int)(cfg.point.killReward +
                (victim.Bounty * (((float)cfg.point.percentOfTarget) / 1000)));

            int rewardCash = (int)(killerCash * (((float)cfg.arena.turretCashSharePercent) / 1000));
            int rewardExp = (int)(killerExp * (((float)cfg.arena.turretExperienceSharePercent) / 1000));
            int rewardPoints = (int)(killerPoints * (((float)cfg.arena.turretPointsSharePercent) / 1000));

            //x2 rewards?
            if (x2)
            {
                rewardCash = rewardCash * 2;
                rewardExp = rewardExp * 2;
                rewardPoints = rewardPoints * 2;
            }

            //Update his stats
            owner.Cash += rewardCash;
            owner.KillPoints += rewardExp;
            owner.Experience += rewardPoints;
        }

        /// <summary>
        /// Calculates and rewards a players for a bot kill
        /// </summary>
        static public void calculateBotKillRewards(Bots.Bot victim, Player killer)
        {

            //x2Rewards
            bool x2 = killer._server._config["zone/DoubleReward"].boolValue;
           
                CfgInfo cfg = killer._server._zoneConfig;
                int killerCash = 0;
                int killerExp = 0;
                int killerPoints = 0;
                int killerBounty = 0;

                killerCash = (int)cfg.bot.cashKillReward;
                killerExp = (int)cfg.bot.expKillReward;
                killerPoints = (int)cfg.bot.pointsKillReward;
                killerBounty = (int)cfg.bot.fixedBountyToKiller;

                //x2 rewards?
                if (x2)
                {
                    killerCash = killerCash * 2;
                    killerExp = killerExp * 2;
                    killerPoints = killerPoints * 2;
                    killerBounty = killerBounty * 2;
                }

                //Update his stats
                killer.Cash += killerCash;
                killer.Experience += killerExp;
                killer.KillPoints += killerPoints;
                killer.Bounty += killerBounty;


                //Inform the killer..
                killer.triggerMessage(1, 500,
                    String.Format("{0} killed by {1} (Cash={2} Exp={3} Points={4})",
                    victim._type.Name, killer._alias,
                    killerCash, killerExp, killerPoints));

                //Sync his state
                killer.syncState();

                //Check for players in the share radius
                List<Player> sharedRewards = victim._arena.getPlayersInRange(victim._state.positionX, victim._state.positionY, cfg.bot.shareRadius);
                Dictionary<int, int> cashRewards = new Dictionary<int, int>();
                Dictionary<int, int> expRewards = new Dictionary<int, int>();
                Dictionary<int, int> pointRewards = new Dictionary<int, int>();

                foreach (Player p in sharedRewards)
                {
                    if (p == killer || p._team != killer._team)
                        continue;

                    //DoubleRewards?
                    if (x2)
                    {
                        cashRewards[p._id] = (int)((((float)killerCash) / 1000) * cfg.bot.sharePercent) * 2;
                        expRewards[p._id] = (int)((((float)killerExp) / 1000) * cfg.bot.sharePercent) * 2;
                        pointRewards[p._id] = (int)((((float)killerPoints) / 1000) * cfg.bot.sharePercent) * 2;
                    }
                    //Reward normally..
                    else
                    {
                        cashRewards[p._id] = (int)((((float)killerCash) / 1000) * cfg.bot.sharePercent);
                        expRewards[p._id] = (int)((((float)killerExp) / 1000) * cfg.bot.sharePercent);
                        pointRewards[p._id] = (int)((((float)killerPoints) / 1000) * cfg.bot.sharePercent);
                    }
                }



                //Sent reward notices to our lucky witnesses
                List<int> sentTo = new List<int>();
                foreach (Player p in sharedRewards)
                {
                    if (p == killer || p._team != killer._team)
                        continue;

                    //Let em know
                    p.triggerMessage(5, 500,
                        String.Format("{0} killed by {1} (Cash={2} Exp={3} Points={4})",
                        victim._type.Name, killer._alias, cashRewards[p._id],
                        expRewards[p._id], pointRewards[p._id]));

                    p.Cash += cashRewards[p._id];
                    p.Experience += expRewards[p._id];
                    p.AssistPoints += pointRewards[p._id];

                    //Sync their state
                    p.syncState();

                    sentTo.Add(p._id);
                }

                //Route the kill to the rest of the arena
                foreach (Player p in victim._arena.Players)
                {	//As long as we haven't already declared it, send
                    if (p == killer)
                        continue;

                    if (sentTo.Contains(p._id))
                        continue;

                    p.triggerMessage(5, 500, String.Format("{0} killed by {1}", victim._type.Name, killer._alias));
                }
        }
		
		/// <summary>
		/// Calculates and distributes rewards for a player kill
		/// </summary>		
        static public void calculatePlayerKillRewards(Player victim, Player killer, CS_VehicleDeath update)
        {
            CfgInfo cfg = victim._server._zoneConfig;
            
            int killerCash = 0;
            int killerExp = 0;
            int killerPoints = 0;

            bool x2 = killer._server._config["zone/DoubleReward"].boolValue;
            int multi = 1;
            if (x2)
                multi = 2;


            if (killer._team != victim._team)
            {

                //Calculate kill reward for killer
               // List<Player> carriers = new List<Player>();
               // carriers = killer._arena._flags.Values.Where(flag => flag.carrier == killer).Select(flag => flag.carrier).ToList();
              
                killerCash = (int)(cfg.cash.killReward +
                    (victim.Bounty * (((float)cfg.cash.percentOfTarget) / 1000)) +
                    (killer.Bounty * (((float)cfg.cash.percentOfKiller) / 1000))) * multi;
                killerExp = (int)(cfg.experience.killReward +
                   (victim.Bounty * (((float)cfg.experience.percentOfTarget) / 1000)) +
                   (killer.Bounty * (((float)cfg.experience.percentOfKiller) / 1000))) * multi;
                killerPoints = (int)(cfg.point.killReward +
                   (victim.Bounty * (((float)cfg.point.percentOfTarget) / 1000)) +
                   (killer.Bounty * (((float)cfg.point.percentOfKiller) / 1000))) * multi;

         /*       if (carriers.Contains(killer))
                {
                    killerCash *= cfg.flag.cashMultiplier / 1000;
                    killerExp *= cfg.flag.experienceMultiplier / 1000;
                    killerPoints *= cfg.flag.pointMultiplier / 1000;

                }       */
            }
            else
            {
                foreach (Player p in victim._arena.Players)
                {
                    Helpers.Player_RouteKill(p, update, victim, 0, 0, 0, 0);
                }
                return;
            }


            //Inform the killer
            Helpers.Player_RouteKill(killer, update, victim, killerCash, killerPoints, killerPoints, killerExp);

            //Update some statistics
            killer.Cash += killerCash;
            killer.Experience += killerExp;
            killer.KillPoints += killerPoints;
            victim.DeathPoints += killerPoints;

            //Update his bounty
            killer.Bounty += (int)((cfg.bounty.fixedToKillerBounty / 1000) +
                (killerPoints * (((float)cfg.bounty.percentToKillerBounty) / 1000))) * multi;

            //Check for players in the share radius
            List<Player> sharedCash = victim._arena.getPlayersInRange(update.positionX, update.positionY, cfg.cash.shareRadius);
            List<Player> sharedExp = victim._arena.getPlayersInRange(update.positionX, update.positionY, cfg.experience.shareRadius);
            List<Player> sharedPoints = victim._arena.getPlayersInRange(update.positionX, update.positionY, cfg.point.shareRadius);
            Dictionary<int, int> cashRewards = new Dictionary<int, int>();
            Dictionary<int, int> expRewards = new Dictionary<int, int>();
            Dictionary<int, int> pointRewards = new Dictionary<int, int>();

            foreach (Player p in sharedCash)
            {
                if (p == killer || p._team != killer._team)
                    continue;

                cashRewards[p._id] = (int)((((float)killerCash) / 1000) * cfg.cash.sharePercent) * multi;
                expRewards[p._id] = 0;
                pointRewards[p._id] = 0;
            }

            foreach (Player p in sharedExp)
            {
                if (p == killer || p._team != killer._team)
                    continue;

                expRewards[p._id] = (int)((((float)killerExp) / 1000) * cfg.experience.sharePercent) * multi;
                if (!cashRewards.ContainsKey(p._id))
                    cashRewards[p._id] = 0;
                if (!pointRewards.ContainsKey(p._id))
                    pointRewards[p._id] = 0;
               
                //Share bounty within the experience radius, Dunno if there is a sharebounty radius?
                p.Bounty += (int)((killerPoints * (((float)cfg.bounty.percentToAssistBounty) / 1000)) * multi);
            }

            foreach (Player p in sharedPoints)
            {
                if (p == killer || p._team != killer._team)
                    continue;

                pointRewards[p._id] = (int)((((float)killerPoints) / 1000) * cfg.point.sharePercent) * multi;
                if (!cashRewards.ContainsKey(p._id))
                    cashRewards[p._id] = 0;
                if (!expRewards.ContainsKey(p._id))
                    expRewards[p._id] = 0;
            }

            //Sent reward notices to our lucky witnesses
            List<int> sentTo = new List<int>();
            foreach (Player p in sharedCash)
            {
                if (p == killer || p._team != killer._team)
                    continue;

                Helpers.Player_RouteKill(p, update, victim, cashRewards[p._id], killerPoints, pointRewards[p._id], expRewards[p._id]);
                p.Cash += cashRewards[p._id];
                p.Experience += expRewards[p._id];
                p.AssistPoints += pointRewards[p._id];

                sentTo.Add(p._id);
            }

            foreach (Player p in sharedExp)
            {
                if (p == killer || p._team != killer._team)
                    continue;

                if (!sentTo.Contains(p._id))
                {
                    Helpers.Player_RouteKill(p, update, victim, cashRewards[p._id], killerPoints, pointRewards[p._id], expRewards[p._id]);
                    p.Cash += cashRewards[p._id];
                    p.Experience += expRewards[p._id];
                    p.AssistPoints += pointRewards[p._id];

                    sentTo.Add(p._id);
                }
            }

            foreach (Player p in sharedPoints)
            {
                if (p == killer || p._team != killer._team)
                    continue;

                if (!sentTo.Contains(p._id))
                {	//Update the assist bounty
                    p.Bounty += (int)(killerPoints * (((float)cfg.bounty.percentToKillerBounty) / 1000));

                    Helpers.Player_RouteKill(p, update, victim, cashRewards[p._id], killerPoints, pointRewards[p._id], expRewards[p._id]);
                    p.Cash += cashRewards[p._id];
                    p.Experience += expRewards[p._id];
                    p.AssistPoints += pointRewards[p._id];

                    sentTo.Add(p._id);
                }
            }

            //Route the kill to the rest of the arena
            foreach (Player p in victim._arena.Players)
            {	//As long as we haven't already declared it, send
                if (p == killer)
                    continue;

                if (sentTo.Contains(p._id))
                    continue;

                Helpers.Player_RouteKill(p, update, victim, 0, killerPoints, 0, 0);
            }
        }
	}
}