using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


using InfServer.Logic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;

namespace InfServer.Script.GameType_HQ
{
    public partial class Events
    {


        public static void newHQ(HQ hq)
        {
            hq.team._arena.sendArenaMessage
                (String.Format("*[HQ] - {0} has established a headquarters in the {1} sector.", 
                hq.team._name, Helpers.posToLetterCoord(hq.vehicle._state.positionX, hq.vehicle._state.positionY)));   
        }

        public static void periodicReward(HQ hq)
        {
            int cash = (Script_HQ.baseCash * hq.level);
            int points = (Script_HQ.basePoints * hq.level);
            int experience = (Script_HQ.baseExp * hq.level);

            hq.team.sendArenaMessage
                ((String.Format("~[HQ] - Reward=(Cash={0}) (Experience={1}) (Points={2})", cash, experience, points)));

            foreach (Player player in hq.team.ActivePlayers)
            {
                player.Cash += cash;
                player.Experience += experience;
            }
        }

        public static void onPlayerKill(HQ hq, Player killer, Player victim)
        {
            int bountyReward = (killer._bounty + victim._bounty) + Script_HQ.baseBountyPerKill;

            //Always give them some kind of reward..
            if (bountyReward < Script_HQ.baseBountyPerKill)
                bountyReward = Script_HQ.baseBountyPerKill;

            hq.bounty += bountyReward;

            killer.sendMessage(0, String.Format("~[HQ] - Kill! - Your HQ has been rewarded {0} bounty points for your kill", bountyReward));
        }

        public static void onComputerKill(HQ hq, Computer killer, Player victim)
        {
            int bountyReward = victim._bounty;

            //Always give them some kind of reward..
            if (bountyReward < Script_HQ.baseBountyPerKill)
                bountyReward = Script_HQ.baseBountyPerKill / 2;

            hq.bounty += bountyReward;
        }

        public static void onVehicleKill(HQ hq, Player killer, Vehicle victim)
        {
            //Vehicles use max hitpoints for determining size of bounty reward..
            int bountyReward = (int)(victim._type.Hitpoints * Script_HQ.vehicleKillMultiply);

            //Always give them some kind of reward..
            if (bountyReward < Script_HQ.baseBountyPerKill)
                bountyReward = Script_HQ.baseBountyPerKill;

            hq.bounty += bountyReward;

            killer.sendMessage(0, String.Format("~[HQ] - Kill! - Your HQ has been rewarded {0} bounty points for your vehicle kill", bountyReward));
        }

        public static void onHQDeath(HQ hq, Player killer)
        {
            string format = "*[HQ] - Destroyed! - {0}'s headquarters worth {1} bounty was destroyed by {2}";
            killer._arena.sendArenaMessage(String.Format(format, hq.team._name, hq.bounty, killer._team._name));

            //Do we reward them with cash or bounty?
            if (Script_HQ._hqs.ContainsKey(killer._team))
            {
                HQ headQ = Script_HQ._hqs[killer._team];

                //Alert them
                killer._team.sendArenaMessage(
                    String.Format(
                    "[HQ] - Reward! - Your Headquarters has been awarded {0} bounty for the destruction of {1}'s Headquarters",
                    hq.bounty, hq.team._name));


                //Reward their HQ instead!
                headQ.bounty += hq.bounty;

                return;
            }

            //Calculate rewards
            int cashReward = (int)(hq.bounty * Script_HQ.cashMultiplier) * Script_HQ.doubleXP;
            int expReward = (int)(hq.bounty * Script_HQ.expMultipler) * Script_HQ.doubleXP;
            int pointReward = (int)(hq.bounty * Script_HQ.pointMultiplier) * Script_HQ.doubleXP;

            //Loop through a reward!
            foreach (Player player in killer._team.ActivePlayers)
            {
                string rewardFormat = "~[HQ] - Reward! - Your personal reward for the destruction of {0}'s HQ:";
                player.sendMessage(0, String.Format(rewardFormat, hq.team._name));
                player.sendMessage(0, String.Format("(Cash={0} Experience={1} Points={2})", cashReward, expReward, pointReward));

                player.Cash += cashReward;
                player.Experience += expReward;
            }
        }

        public static void onHQLevelUp(HQ hq)
        {
            string format = "*[HQ] - Level Up! Your Headquarters is now level {0} and is worth {1} bounty. (Next level requires {2} Bounty)";
            hq.level++;

            //Multiples of 10? (For humps)
            bool isMultiple = hq.level % 10 == 0;
            if (isMultiple)
                hq.nextLvl = (int)(hq.nextLvl + (Script_HQ.baseBounty * (hq.level / Script_HQ.baseMultiplier)) + Script_HQ.levelHump);
            else
            hq.nextLvl = (int)(hq.nextLvl + (Script_HQ.baseBounty * (hq.level / Script_HQ.baseMultiplier)));

            hq.team.sendArenaMessage(String.Format(format, hq.level, hq.bounty, hq.nextLvl));
            hq.maxHealth += 10;
            hq.vehicle._state.health = (short)hq.maxHealth;
            hq.vehicle.update(false);
        }
    }
}
