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
		static public void calculateTurretKillRewards(Player victim, Computer comp, CS_PlayerDeath update)
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

			owner.Cash += (int)(killerCash * (((float)cfg.arena.turretCashSharePercent) / 1000));
			owner.Points += (int)(killerPoints * (((float)cfg.arena.turretPointsSharePercent) / 1000));
			owner.Experience += (int)(killerExp * (((float)cfg.arena.turretExperienceSharePercent) / 1000));
		}
		
		/// <summary>
		/// Calculates and distributes rewards for a player kill
		/// </summary>		
		static public void calculatePlayerKillRewards(Player victim, Player killer, CS_PlayerDeath update)
		{	//Calculate kill reward for killer
			CfgInfo cfg = victim._server._zoneConfig;
			int killerCash = (int)(cfg.cash.killReward +
				(victim.Bounty * (((float)cfg.cash.percentOfTarget) / 1000)) +
				(killer.Bounty * (((float)cfg.cash.percentOfKiller) / 1000)));
			int killerExp = (int)(cfg.experience.killReward +
				(victim.Bounty * (((float)cfg.experience.percentOfTarget) / 1000)) +
				(killer.Bounty * (((float)cfg.experience.percentOfKiller) / 1000)));
			int killerPoints = (int)(cfg.point.killReward +
				(victim.Bounty * (((float)cfg.point.percentOfTarget) / 1000)) +
				(killer.Bounty * (((float)cfg.point.percentOfKiller) / 1000)));

			//Inform the killer
			Helpers.Player_RouteKill(killer, update, victim, killerCash, killerPoints, killerPoints, killerExp);

			//Update some statistics
			killer.Cash += killerCash;
			killer.Experience += killerExp;
			killer.Points += killerPoints;
			killer.KillPoints += killerPoints;
			victim.DeathPoints += killerPoints;

			//Update his bounty
			killer.Bounty += (int)(cfg.bounty.fixedToKillerBounty +
				(killerPoints * (((float)cfg.bounty.percentToKillerBounty) / 1000)));

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

				cashRewards[p._id] = (int)((((float)killerCash) / 1000) * cfg.cash.sharePercent);
				expRewards[p._id] = 0;
				pointRewards[p._id] = 0;
			}

			foreach (Player p in sharedExp)
			{
				if (p == killer || p._team != killer._team)
					continue;

				expRewards[p._id] = (int)((((float)killerExp) / 1000) * cfg.experience.sharePercent);
				if (!cashRewards.ContainsKey(p._id))
					cashRewards[p._id] = 0;
				if (!pointRewards.ContainsKey(p._id))
					pointRewards[p._id] = 0;
			}

			foreach (Player p in sharedPoints)
			{
				if (p == killer || p._team != killer._team)
					continue;

				pointRewards[p._id] = (int)((((float)killerPoints) / 1000) * cfg.point.sharePercent);
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
				p.Points += pointRewards[p._id];
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
					p.Points += pointRewards[p._id];
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
					p.Points += pointRewards[p._id];
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