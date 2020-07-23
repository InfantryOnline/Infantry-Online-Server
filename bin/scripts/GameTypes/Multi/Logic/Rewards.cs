using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text.RegularExpressions;

using InfServer.Logic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;

using Assets;

namespace InfServer.Script.GameType_Multi
{
    class Rewards
    {
        public class Jackpot
        {
            public List<Reward> _playerRewards;

            public int totalMVP;
            public int totalJackPot;

            public Jackpot()
            {
                _playerRewards = new List<Reward>();
            }

            public class Reward
            {
                public Player player;
                public double MVP = 0;
                public int Score;
                public int experience;
                public int cash;
                public int points;
            }
        }

        static public Jackpot calculateJackpot(IEnumerable<Player> players, Team winner, Settings.GameTypes gameType, Script_Multi script, bool bTicker)
        {
            Jackpot result = new Jackpot();
            int fixedjackpot = 0;
            Jackpot.Reward rewards;
            int highest = 0;

            int pointsPerKill = 0;
            int pointsPerDeath = 0;
            int pointsPerFlag = 0;
            double pointsPerSecondWinner = 0;
            double pointsPerSecondLoser = 0;
            double pointsPerHP = 0;

            int totalkills = 0;
            int totalDeaths = 0;
            int gameLength = 0;
            if (winner != null)
            {
                totalkills = winner._currentGameKills;
                totalDeaths = winner._currentGameDeaths;
                gameLength = (winner._arena._tickGameEnded - winner._arena._tickGameStarted) / 1000;
            }
            int totalHPHealed = 0;
            int totalFlagCaps = 0;

            switch (gameType)
            {
                case Settings.GameTypes.Conquest:
                    fixedjackpot = Settings.c_jackpot_CQ_Fixed;
                    pointsPerKill = Settings.c_jackpot_CQ_PointsPerKill;
                    pointsPerDeath = Settings.c_jackpot_CQ_PointsPerDeath;
                    pointsPerFlag = Settings.c_jackpot_CQ_PointsPerFlag;
                    pointsPerSecondWinner = Settings.c_jackpot_CQ_WinnerPointsPerSecond;
                    pointsPerSecondLoser = Settings.c_jackpot_CQ_LoserPointsPerSecond;
                    pointsPerHP = Settings.c_jackpot_CQ_PointsPerHP;
                    break;

                case Settings.GameTypes.Coop:
                    fixedjackpot = Settings.c_jackpot_Co_Fixed;
                    pointsPerKill = Settings.c_jackpot_Co_PointsPerKill;
                    pointsPerDeath = Settings.c_jackpot_Co_PointsPerDeath;
                    pointsPerFlag = Settings.c_jackpot_Co_PointsPerFlag;
                    pointsPerSecondWinner = Settings.c_jackpot_Co_WinnerPointsPerSecond;
                    pointsPerSecondLoser = Settings.c_jackpot_Co_LoserPointsPerSecond;
                    pointsPerHP = Settings.c_jackpot_Co_PointsPerHP;
                    break;

                case Settings.GameTypes.Royale:
                    fixedjackpot = Settings.c_jackpot_CQ_Fixed;
                    pointsPerKill = Settings.c_jackpot_CQ_PointsPerKill;
                    pointsPerDeath = Settings.c_jackpot_CQ_PointsPerDeath;
                    pointsPerFlag = Settings.c_jackpot_CQ_PointsPerFlag;
                    pointsPerSecondWinner = Settings.c_jackpot_CQ_WinnerPointsPerSecond;
                    pointsPerSecondLoser = Settings.c_jackpot_CQ_LoserPointsPerSecond;
                    pointsPerHP = Settings.c_jackpot_CQ_PointsPerHP;
                    break;
            }

            //Set our fixed
            result.totalJackPot += fixedjackpot;

            //Calculate MVP
            foreach (Player player in players)
            {
                rewards = new Jackpot.Reward();
                int score = 0;
                rewards.player = player;

                score += Convert.ToInt32((script.StatsCurrent(player).kills * pointsPerKill));
                score += Convert.ToInt32((script.StatsCurrent(player).deaths * pointsPerDeath));
                score += Convert.ToInt32((script.StatsCurrent(player).flagCaptures * pointsPerFlag));
                score += Convert.ToInt32((script.StatsCurrent(player).potentialHealthHealed * pointsPerHP));

                //Increment Jackpot totals
                totalHPHealed += script.StatsCurrent(player).potentialHealthHealed;
                totalFlagCaps += script.StatsCurrent(player).flagCaptures;

                //Calculate win/loss play seconds
                if (winner != null)
                {
                    if (winner._name == "Titan Militia")
                    {
                        score += Convert.ToInt32((script.StatsCurrent(player).titanPlaySeconds * pointsPerSecondWinner));
                        score += Convert.ToInt32((script.StatsCurrent(player).collectivePlaySeconds * pointsPerSecondLoser));
                    }
                    else
                    {
                        score += Convert.ToInt32((script.StatsCurrent(player).collectivePlaySeconds * pointsPerSecondWinner));
                        score += Convert.ToInt32((script.StatsCurrent(player).titanPlaySeconds * pointsPerSecondLoser));
                    }
                }

                //Tally up his overall score and check if it's our new highest
                rewards.Score = score;
                result.totalMVP += score;
                result._playerRewards.Add(rewards);

                if (score >= highest)
                    highest = score;
            }

            //Calculate Total Jackpot
            result.totalJackPot += (totalkills * pointsPerKill);
            result.totalJackPot += (totalDeaths * pointsPerDeath);
            result.totalJackPot += (gameLength * 4);
            result.totalJackPot += Convert.ToInt32(totalHPHealed * pointsPerHP);
            result.totalJackPot += (totalFlagCaps * pointsPerFlag);


            //Calculate each players reward based on their MVP score and the overall jackpot
            foreach (Jackpot.Reward reward in result._playerRewards)
            {

                reward.MVP = (double)reward.Score / (double)highest;
                if (!bTicker)
                {
                    reward.points = Convert.ToInt32(result.totalJackPot * reward.MVP);
                    reward.cash = Convert.ToInt32(((result.totalJackPot * reward.MVP) * Settings.c_jackpot_CashMultiplier));
                    reward.experience = Convert.ToInt32((result.totalJackPot * reward.MVP) * Settings.c_jackpot_ExpMultiplier);
                }
            }
            return result;
        }

        /// <summary>
        /// Calculates and rewards a players for a bot kill
        /// </summary>
        static public void calculateBotKillRewards(Bots.Bot victim, Player killer, Settings.GameTypes gameType)
        {
            CfgInfo cfg = killer._server._zoneConfig;
            int killerCash = 0;
            int killerExp = 0;
            int killerPoints = 0;
            int killerBounty = 0;
            int victimBounty = 0;
            int killerBountyIncrease = 0;

            BotSettings settings = victim.Settings();
            if (settings != null)
            {
                /*
                killerCash = settings.Cash;
                killerBounty = settings.Bounty;
                killerExp = settings.Experience;
                killerPoints = settings.Points;
                */
                killerBounty = Convert.ToInt32(((double)killer.Bounty / 100) * Settings.c_percentOfOwn); //We are now using base reward of 25 + 3% of current bounty
                killerPoints = Convert.ToInt32((settings.Points) + (killerBounty * Settings.c_pointMultiplier));
                killerCash = Convert.ToInt32((settings.Cash) + (killerBounty * Settings.c_cashMultiplier));
                killerExp = Convert.ToInt32((settings.Experience) + (killerBounty * Settings.c_expMultiplier));
                victimBounty = settings.Bounty;
                killerBountyIncrease = 0;

            }
            else
            {
                /* Old fixed Bot Rewards from CFG
                killerCash = (int)cfg.bot.cashKillReward;
                killerExp = (int)cfg.bot.expKillReward;
                killerPoints = (int)cfg.bot.pointsKillReward;
                killerBounty = (int)cfg.bot.fixedBountyToKiller;
                */
                killerBounty = Convert.ToInt32(((double)killer.Bounty / 100) * Settings.c_percentOfOwn); //We are now using base reward of 25 + 3% of current bounty
                killerPoints = Convert.ToInt32(((int)cfg.bot.pointsKillReward) + (killerBounty * Settings.c_pointMultiplier));
                killerCash = Convert.ToInt32(((int)cfg.bot.cashKillReward) + (killerBounty * Settings.c_cashMultiplier));
                killerExp = Convert.ToInt32(((int)cfg.bot.expKillReward) + (killerBounty * Settings.c_expMultiplier));
                victimBounty = (int)cfg.bot.fixedBountyToKiller;
                killerBountyIncrease = 0;
            }

            //Update his stats
            killerCash = addCash(killer, killerCash, gameType);
            killer.Experience += killerExp;
            killer.KillPoints += killerPoints;
            //killer.Bounty += killerBounty;
            killer.Bounty += (killerBountyIncrease + victimBounty);

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

            //Send reward notices to our lucky witnesses
            List<int> sentTo = new List<int>();
            foreach (Player p in sharedRewards)
            {
                if (p == killer || p._team != killer._team)
                    continue;

                cashRewards[p._id] = addCash(p, cashRewards[p._id], gameType);
                p.Experience += expRewards[p._id];
                p.AssistPoints += pointRewards[p._id];

                //Let em know
                p.triggerMessage(4, 500,
                    String.Format("{0} killed by {1} (Cash={2} Exp={3} Points={4})",
                    victim._type.Name, killer._alias, cashRewards[p._id],
                    expRewards[p._id], pointRewards[p._id]));


                //Sync their state
                p.syncState();

                sentTo.Add(p._id);
            }

            //Route the kill to the rest of the arena
            foreach (Player p in victim._arena.Players)
            {	//As long as we haven't already declared it, send
                if (p == null)
                    continue;

                if (p == killer)
                    continue;

                if (sentTo.Contains(p._id))
                    continue;

                //Adjust our color accordingly..
                byte color = 0;
                if (victim._team == p._team)
                    color = 4;

                //Let them know
                p.triggerMessage(color, 500,
                    String.Format("{0} killed by {1}",
                    victim._type.Name, killer._alias));
            }
        }

        static public void calculatePlayerKillRewards(Player victim, Player killer, Settings.GameTypes gameType)
        {
            CfgInfo cfg = victim._server._zoneConfig;

            int killerBounty = 0;
            int killerBountyIncrease = 0;
            int victimBounty = 0;
            int killerCash = 0;
            int killerExp = 0;
            int killerPoints = 0;

            //Fake it to make it
            CS_VehicleDeath update = new CS_VehicleDeath(0, new byte[0], 0, 0);
            update.killedID = victim._id;
            update.killerPlayerID = killer._id;
            update.positionX = victim._state.positionX;
            update.positionY = victim._state.positionY;
            update.type = Helpers.KillType.Player;

            if (killer._team != victim._team)
            {
                killerBounty = Convert.ToInt32(((double)killer.Bounty / 100) * Settings.c_percentOfOwn);
                killerBountyIncrease = Convert.ToInt32(((double)killer.Bounty / 100) * Settings.c_percentOfOwnIncrease);
                victimBounty = Convert.ToInt32(((double)victim.Bounty / 100) * Settings.c_percentOfVictim);

                killerPoints = Convert.ToInt32((Settings.c_baseReward + killerBounty + victimBounty) * Settings.c_pointMultiplier);
                killerCash = Convert.ToInt32((Settings.c_baseReward + killerBounty + victimBounty) * Settings.c_cashMultiplier);
                killerExp = Convert.ToInt32((Settings.c_baseReward + killerBounty + victimBounty) * Settings.c_expMultiplier);

            }
            else
            {
                foreach (Player p in victim._arena.Players)
                    Helpers.Player_RouteKill(p, update, victim, 0, 0, 0, 0);
                return;
            }


            //Inform the killer
            Helpers.Player_RouteKill(killer, update, victim, killerCash, killerPoints, killerPoints, killerExp);

            //Update some statistics
            killerCash = addCash(killer, killerCash, gameType);
            killer.Experience += killerExp;
            killer.KillPoints += killerPoints;
            victim.DeathPoints += killerPoints;

            //Update his bounty
            killer.Bounty += (killerBountyIncrease + victimBounty);

            //Check for players in the share radius
            List<Player> sharedCash = victim._arena.getPlayersInRange(update.positionX, update.positionY, cfg.cash.shareRadius).Where(p => p._baseVehicle._type.Name.Contains("Medic")).ToList();
            List<Player> sharedExp = victim._arena.getPlayersInRange(update.positionX, update.positionY, cfg.experience.shareRadius).Where(p => p._baseVehicle._type.Name.Contains("Medic")).ToList();
            List<Player> sharedPoints = victim._arena.getPlayersInRange(update.positionX, update.positionY, cfg.point.shareRadius).Where(p => p._baseVehicle._type.Name.Contains("Medic")).ToList();
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

                cashRewards[p._id] = addCash(p, cashRewards[p._id], gameType);
                p.Experience += expRewards[p._id];
                p.AssistPoints += pointRewards[p._id];
                Helpers.Player_RouteKill(p, update, victim, cashRewards[p._id], killerPoints, pointRewards[p._id], expRewards[p._id]);
                sentTo.Add(p._id);
            }

            foreach (Player p in sharedExp)
            {
                if (p == killer || p._team != killer._team)
                    continue;

                if (!sentTo.Contains(p._id))
                {
                    cashRewards[p._id] = addCash(p, cashRewards[p._id], gameType);
                    p.Experience += expRewards[p._id];
                    p.AssistPoints += pointRewards[p._id];

                    Helpers.Player_RouteKill(p, update, victim, cashRewards[p._id], killerPoints, pointRewards[p._id], expRewards[p._id]);

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

                    cashRewards[p._id] = addCash(p, cashRewards[p._id], gameType);
                    p.Experience += expRewards[p._id];
                    p.AssistPoints += pointRewards[p._id];

                    Helpers.Player_RouteKill(p, update, victim, cashRewards[p._id], killerPoints, pointRewards[p._id], expRewards[p._id]);

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

        static public int addCash(Player player, int quantity, Settings.GameTypes gameType)
        {
            //Any rare cash item?
            double multiplier = 1.0;
            int cash = 0;

            switch (gameType)
            {
                case Settings.GameTypes.Conquest:
                    {
                        if (Script_Multi._bPvpHappyHour)
                            multiplier = 2.0;
                        break;
                    }
                case Settings.GameTypes.Coop:
                    {
                        if (Script_Multi._bCoopHappyHour)
                            multiplier = 2.0;
                        break;
                    }
            }

            if (player.getInventoryAmount(2019) > 0)
                multiplier += 0.10;

            cash = Convert.ToInt32(quantity * multiplier);

            player.Cash += cash;
            player.syncState();

            return cash;
        }

        public static double calculateDiffMod(long points, int difficulty)
        {
            double result = 1;
            int playerLevel = 1;

            if (points >= 100000)
                playerLevel = 2;
            if (points >= 500000)
                playerLevel = 3;
            if (points >= 1000000)
                playerLevel = 4;
            if (points >= 2000000)
                playerLevel = 5;
            if (points >= 3000000)
                playerLevel = 6;
            if (points >= 4000000)
                playerLevel = 7;
            if (points >= 5000000)
                playerLevel = 8;
            if (points >= 6000000)
                playerLevel = 9;
            if (points >= 7000000)
                playerLevel = 10;


            result = Convert.ToDouble(((double)difficulty / (playerLevel + 1)) / 3);
            return result;
        }

    }
}
