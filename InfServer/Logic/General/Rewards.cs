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

            //Update his stats
            owner.Cash += rewardCash;
            owner.KillPoints += rewardExp;
            owner.Experience += rewardPoints;

            //Is our creator still on the same team?
            if (owner._team == comp._team)
            {   
                //Are we sharing kills to our owner?
                if (cfg.arena.turretKillShareOwner)
                    owner.Kills++;

                //Show the message
                owner.triggerMessage(2, 500,
                    String.Format("Turret Kill: Kills=1 (Points={0} Exp={1} Cash={2})",
                    rewardCash, rewardExp, rewardPoints));
            }
        }

        /// <summary>
        /// Calculates and rewards a players for a bot kill
        /// </summary>
        static public void calculateBotKillRewards(Bots.Bot victim, Player killer)
        {
            CfgInfo cfg = killer._server._zoneConfig;
            int killerCash = 0;
            int killerExp = 0;
            int killerPoints = 0;
            int killerBounty = 0;

            killerCash = (int)cfg.bot.cashKillReward;
            killerExp = (int)cfg.bot.expKillReward;
            killerPoints = (int)cfg.bot.pointsKillReward;
            killerBounty = (int)cfg.bot.fixedBountyToKiller;

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

                cashRewards[p._id] = (int)((((float)killerCash) / 1000) * cfg.bot.sharePercent);
                expRewards[p._id] = (int)((((float)killerExp) / 1000) * cfg.bot.sharePercent);
                pointRewards[p._id] = (int)((((float)killerPoints) / 1000) * cfg.bot.sharePercent);
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

                //p.triggerMessage(5, 500, String.Format("{0} killed by {1}", victim._type.Name, killer._alias));
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

            if (killer._team != victim._team)
            {

                //Calculate kill reward for killer
               // List<Player> carriers = new List<Player>();
               // carriers = killer._arena._flags.Values.Where(flag => flag.carrier == killer).Select(flag => flag.carrier).ToList();
              
                killerCash = (int)(cfg.cash.killReward +
                    (victim.Bounty * (((float)cfg.cash.percentOfTarget) / 1000)) +
                    (killer.Bounty * (((float)cfg.cash.percentOfKiller) / 1000)));
                killerExp = (int)(cfg.experience.killReward +
                   (victim.Bounty * (((float)cfg.experience.percentOfTarget) / 1000)) +
                   (killer.Bounty * (((float)cfg.experience.percentOfKiller) / 1000)));
                killerPoints = (int)(cfg.point.killReward +
                   (victim.Bounty * (((float)cfg.point.percentOfTarget) / 1000)) +
                   (killer.Bounty * (((float)cfg.point.percentOfKiller) / 1000)));

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
                (killerPoints * (((float)cfg.bounty.percentToKillerBounty) / 1000)));

            //Check for players in the share radius
            List<Player> sharedCash = victim._arena.getPlayersInRange(update.positionX, update.positionY, cfg.cash.shareRadius);
            List<Player> sharedExp = victim._arena.getPlayersInRange(update.positionX, update.positionY, cfg.experience.shareRadius);
            List<Player> sharedPoints = victim._arena.getPlayersInRange(update.positionX, update.positionY, cfg.point.shareRadius);
            Dictionary<int, int> cashRewards = new Dictionary<int, int>();
            Dictionary<int, int> expRewards = new Dictionary<int, int>();
            Dictionary<int, int> pointRewards = new Dictionary<int, int>();
            //Set up our shared math
            int CashShare = (int)((((float)killerCash) / 1000) * cfg.cash.sharePercent);
            int ExpShare = (int)((((float)killerExp) / 1000) * cfg.experience.sharePercent);
            int PointsShare = (int)((((float)killerPoints) / 1000) * cfg.point.sharePercent);
            int BtyShare = (int)((killerPoints * (((float)cfg.bounty.percentToAssistBounty) / 1000)));

            foreach (Player p in sharedCash)
            {
                if (p == killer || p._team != killer._team)
                    continue;

                cashRewards[p._id] = CashShare;
                expRewards[p._id] = 0;
                pointRewards[p._id] = 0;
            }

            foreach (Player p in sharedExp)
            {
                if (p == killer || p._team != killer._team)
                    continue;

                expRewards[p._id] = ExpShare;
                if (!cashRewards.ContainsKey(p._id))
                    cashRewards[p._id] = 0;
                if (!pointRewards.ContainsKey(p._id))
                    pointRewards[p._id] = 0;
            }

            foreach (Player p in sharedPoints)
            {
                if (p == killer || p._team != killer._team)
                    continue;

                pointRewards[p._id] = PointsShare;
                if (!cashRewards.ContainsKey(p._id))
                    cashRewards[p._id] = 0;
                if (!expRewards.ContainsKey(p._id))
                    expRewards[p._id] = 0;

                //Share bounty within the experience radius, Dunno if there is a sharebounty radius?
                p.Bounty += BtyShare;
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
                    p.Bounty += BtyShare;

                    Helpers.Player_RouteKill(p, update, victim, cashRewards[p._id], killerPoints, pointRewards[p._id], expRewards[p._id]);
                    p.Cash += cashRewards[p._id];
                    p.Experience += expRewards[p._id];
                    p.AssistPoints += pointRewards[p._id];

                    sentTo.Add(p._id);
                }
            }

            //Shared kills anyone?
            Vehicle sharedveh = killer._occupiedVehicle;
            //are we in a vehicle?
            if (sharedveh != null)
            {
                //Was this a child vehicle? If so, re-route us to the parent
                if (sharedveh._parent != null)
                    sharedveh = sharedveh._parent;

                //Can we even share kills?
                if (sharedveh._type.SiblingKillsShared > 0)
                {   //Yep!
                    //Does this vehicle have any childs?
                    if (sharedveh._childs.Count > 0)
                    {
                        //Cycle through each child and reward them
                        foreach (Vehicle child in sharedveh._childs)
                        {
                            //Anyone home?
                            if (child._inhabitant == null)
                                continue;

                            //Can we share?
                            if (child._type.SiblingKillsShared == 0)
                                continue;

                            //Skip our killer
                            if (child._inhabitant == killer)
                                continue;

                            //Give them a kill!
                            child._inhabitant.Kills++;

                            //Show the message
                            child._inhabitant.triggerMessage(2, 500,
                                String.Format("Sibling Assist: Kills=1 (Points={0} Exp={1} Cash={2})",
                                CashShare, ExpShare, PointsShare));
                        }
                    }
                }
            }

            //Route the kill to the rest of the arena
            foreach (Player p in victim._arena.Players.ToList())
            {	//As long as we haven't already declared it, send

                if (p == null)
                    continue;

                if (p == killer)
                    continue;

                if (sentTo.Contains(p._id))
                    continue;

                Helpers.Player_RouteKill(p, update, victim, 0, killerPoints, 0, 0);
            }
        }
	}
}