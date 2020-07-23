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
    public class Upgrade
    {
        #region Chances
        public struct Failurechance
        {
            public const double Level1 = 0.1;
            public const double Level2 = 0.2;
            public const double Level3 = 0.3;
            public const double Level4 = 0.4;
            public const double Level5 = 0.5;
            public const double Level6 = 0.6;
            public const double Level7 = 0.7;
            public const double Level8 = 0.8;
            public const double Level9 = 0.8;
            public const double Level10 = 0.8;
            public const double Level11 = 0.81;
            public const double Level12 = 0.82;
            public const double Level13 = 0.83;
            public const double Level14 = 0.84;
            public const double Level15 = 0.85;
            public const double Level16 = 0.86;
            public const double Level17 = 0.87;
            public const double Level18 = 0.88;
            public const double Level19 = 0.89;
            public const double Level20 = 0.9;
        }

        public struct DowngradeChance
        {
            public const double Level1 = 0;
            public const double Level2 = 0;
            public const double Level3 = 0;
            public const double Level4 = 0;
            public const double Level5 = 0;
            public const double Level6 = 0;
            public const double Level7 = 0;
            public const double Level8 = 0;
            public const double Level9 = 0;
            public const double Level10 = 0;
            public const double Level11 = 0;
            public const double Level12 = 0;
            public const double Level13 = 0;
            public const double Level14 = 0;
            public const double Level15 = 0;
            public const double Level16 = 0;
            public const double Level17 = 0;
            public const double Level18 = 0;
            public const double Level19 = 0;
            public const double Level20 = 0;
        }
        #endregion

        private Dictionary<int, double> _failureChances;
        private Dictionary<int, double> _downgradeChances;

        public Upgrade()
        {
            #region Chance Dictionary
            _failureChances = new Dictionary<int, double>();
            _downgradeChances = new Dictionary<int, double>();
            _failureChances.Add(1, Failurechance.Level1);
            _failureChances.Add(2, Failurechance.Level2);
            _failureChances.Add(3, Failurechance.Level3);
            _failureChances.Add(4, Failurechance.Level4);
            _failureChances.Add(5, Failurechance.Level5);
            _failureChances.Add(6, Failurechance.Level6);
            _failureChances.Add(7, Failurechance.Level7);
            _failureChances.Add(8, Failurechance.Level8);
            _failureChances.Add(9, Failurechance.Level9);
            _failureChances.Add(10, Failurechance.Level10);
            _failureChances.Add(11, Failurechance.Level11);
            _failureChances.Add(12, Failurechance.Level12);
            _failureChances.Add(13, Failurechance.Level13);
            _failureChances.Add(14, Failurechance.Level14);
            _failureChances.Add(15, Failurechance.Level15);
            _failureChances.Add(16, Failurechance.Level16);
            _failureChances.Add(17, Failurechance.Level17);
            _failureChances.Add(18, Failurechance.Level18);
            _failureChances.Add(19, Failurechance.Level19);
            _failureChances.Add(20, Failurechance.Level20);

            _downgradeChances.Add(1, DowngradeChance.Level1);
            _downgradeChances.Add(2, DowngradeChance.Level2);
            _downgradeChances.Add(3, DowngradeChance.Level3);
            _downgradeChances.Add(4, DowngradeChance.Level4);
            _downgradeChances.Add(5, DowngradeChance.Level5);
            _downgradeChances.Add(6, DowngradeChance.Level6);
            _downgradeChances.Add(7, DowngradeChance.Level7);
            _downgradeChances.Add(8, DowngradeChance.Level8);
            _downgradeChances.Add(9, DowngradeChance.Level9);
            _downgradeChances.Add(10, DowngradeChance.Level10);
            _downgradeChances.Add(11, DowngradeChance.Level11);
            _downgradeChances.Add(12, DowngradeChance.Level12);
            _downgradeChances.Add(13, DowngradeChance.Level13);
            _downgradeChances.Add(14, DowngradeChance.Level14);
            _downgradeChances.Add(15, DowngradeChance.Level15);
            _downgradeChances.Add(16, DowngradeChance.Level16);
            _downgradeChances.Add(17, DowngradeChance.Level17);
            _downgradeChances.Add(18, DowngradeChance.Level18);
            _downgradeChances.Add(19, DowngradeChance.Level19);
            _downgradeChances.Add(20, DowngradeChance.Level20);
            #endregion
        }

        public ItemInfo tryItemUpgrade(Player player, string currentItem)
        {
            ItemInfo newItem = null;
            string newItemName = Regex.Replace(currentItem, @"\d", "");
            int level = getLevelFromString(currentItem);

            if (level == 20)
            {
                player.sendMessage(-1, String.Format("{0} is at it's maximum level", currentItem));
                return null;
            }

            double modifier = 0;
            int luckydice = player.getInventoryAmount(2018);

            //Any lucky dice?
            if (luckydice > 0)
            {
                if (luckydice >= 100)
                {
                    modifier = Convert.ToDouble((double)100 / 100);
                    luckydice = 100;
                }
                else
                    modifier = Convert.ToDouble((double)luckydice / 100);
                player.sendMessage(0, String.Format("Upgrade chances increased by {0}% thanks to your Lucky Dice", luckydice));
                player.inventoryModify(2018, -luckydice);
            }

            if (tryFailure(currentItem, modifier))
            {
                player.sendMessage(-1, String.Format("Your upgrade attempt of {0} has failed.", currentItem));
                Log.write(TLog.Warning, String.Format("Upgrade of {0} has failed with current level at {1}", currentItem, level));
                return null;
            }

            if (tryDowngrade(currentItem, modifier))
            {
                level = level - 1;
                newItemName += String.Format("{0}", level);
                newItem = AssetManager.Manager.getItemByName(newItemName);

                if (newItem == null)
                    Log.write(TLog.Warning, "Could not find specified upgrade item. ({0})", newItemName);

                player.inventoryModify(AssetManager.Manager.getItemByName(currentItem), -1);
                player.inventoryModify(newItem, 1);

                player.sendMessage(-1, String.Format("Your upgrade attempt of {0} has failed and downgraded your item to {1}", currentItem, newItem.name));
                return null;
            }

            level = level + 1;

            string format;
            if (level == 1)
                format = String.Format(" +{0}", level);
            else
                format = String.Format("{0}", level);

            newItemName += format;
            newItem = AssetManager.Manager.getItemByName(newItemName);
            if (newItem == null)
            {
                Log.write(TLog.Warning, "Could not find specified upgrade item. ({0})", newItemName);
                return null;
            }

            player.sendMessage(4, String.Format("Your upgrade attempt of {0} was succesful and upgraded your item to {1}", currentItem, newItem.name));

            return newItem;
        }


        #region Private Calls
        private bool tryDowngrade(string itemname, double modifier)
        {
            Random rand = new Random();

            int current = getLevelFromString(itemname);

            if (current == 0)
                return false;

            //Sanity check
            if (!_downgradeChances.ContainsKey(current))
                return false;

            double chance = (_downgradeChances[current] - modifier);
            double random = rand.NextDouble();

            //Sanity
            if (chance < 0)
                chance = 0;

            Log.write(String.Format("({0}) Downgrade check: {1} < {2}", itemname, random, chance));

            //MONEYMONEY
            if (random < chance)
                return true;

            return false;
        }

        private bool tryFailure(string itemname, double modifier)
        {
            Random rand = new Random();

            int current = getLevelFromString(itemname);

            if (current == 0)
                return false;

            //Sanity check
            if (!_failureChances.ContainsKey(current))
                return false;

            double chance = (_failureChances[current] - modifier);
            double random = rand.NextDouble();

            //Sanitiy
            if (chance < 0)
                chance = 0;

            Log.write(String.Format("({0}) Failure check: {1} < {2}", itemname, random, chance));

            //MONEYMONEY
            if (random < chance)
                return true;

            return false;
        }

        private int getLevelFromString(string itemname)
        {
            ///Level 0 Items can't downgrade...
            if (!itemname.Contains("+"))
                return 0;

            //Sanity check
            if (itemname.Split('+').Count() < 2)
                return 0;

            int current = Convert.ToInt32(itemname.Split('+')[1]);

            return current;
        }
        #endregion
    }
}
