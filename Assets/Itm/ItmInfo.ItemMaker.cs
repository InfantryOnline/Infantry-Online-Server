using System.Collections.Generic;

namespace Assets
{
    public partial class ItemInfo
    {
        public class ItemMaker : ItemInfo
        {
            public Graphics iconGraphic, prefireGraphic;
            public Sound prefireSound;
            public int useAmmoID;
            public int ammoUsedPerShot;
            public int ammoCapacity;
            public int requiredItem;
            public int requiredItemAmount;
            public int energyCostTerrain0;
            public int energyCostTerrain1;
            public int energyCostTerrain2;
            public int energyCostTerrain3;
            public int energyCostTerrain4;
            public int energyCostTerrain5;
            public int energyCostTerrain6;
            public int energyCostTerrain7;
            public int energyCostTerrain8;
            public int energyCostTerrain9;
            public int energyCostTerrain10;
            public int energyCostTerrain11;
            public int energyCostTerrain12;
            public int energyCostTerrain13;
            public int energyCostTerrain14;
            public int energyCostTerrain15;
            public int secondShotEnergy;
            public int secondShotTimeout;
            public int fireDelay;
            public int fireDelayOther;
            public int maxFireDelay;
            public int entryFireDelay;
            public int reloadDelayNormal;
            public int reloadDelayPartial;
            public int reloadDelayAsynchronous;
            public int reloadDelayAsynchronousPartial;
            public int routeRange;
            public int routeRotationalRange;
            public int recoil;
            public int verticalRecoil;
            public int prefireDelay;
            public int reliability;
            public int reliabilityFireDelay;
            public bool movementCancelsPrefire;
            public bool notifyOthersOfPrefire;
            public int cashCost;
            public bool useWhileCarryingBall;
            public bool useWhileCarryingFlag;
            public int itemMakerItemID;
            public int itemMakerQuantity;

            public static ItemMaker Load(List<string> values)
            {
                ItemMaker item = new ItemMaker();
                item.iconGraphic = new Graphics(ref values, 23);
                item.prefireGraphic = new Graphics(ref values, 71);
                item.prefireSound = new Sound(ref values, 79);
                ItemInfo.LoadGeneralSettings1((ItemInfo)item, values);

                item.useAmmoID = CSVReader.GetInt(values[31]);
                item.ammoUsedPerShot = CSVReader.GetInt(values[32]);
                item.ammoCapacity = CSVReader.GetInt(values[33]);
                item.requiredItem = CSVReader.GetInt(values[34]);
                item.requiredItemAmount = CSVReader.GetInt(values[35]);
                item.energyCostTerrain0 = CSVReader.GetInt(values[36]);
                item.energyCostTerrain1 = CSVReader.GetInt(values[37]);
                item.energyCostTerrain2 = CSVReader.GetInt(values[38]);
                item.energyCostTerrain3 = CSVReader.GetInt(values[39]);
                item.energyCostTerrain4 = CSVReader.GetInt(values[40]);
                item.energyCostTerrain5 = CSVReader.GetInt(values[41]);
                item.energyCostTerrain6 = CSVReader.GetInt(values[42]);
                item.energyCostTerrain7 = CSVReader.GetInt(values[43]);
                item.energyCostTerrain8 = CSVReader.GetInt(values[44]);
                item.energyCostTerrain9 = CSVReader.GetInt(values[45]);
                item.energyCostTerrain10 = CSVReader.GetInt(values[46]);
                item.energyCostTerrain11 = CSVReader.GetInt(values[47]);
                item.energyCostTerrain12 = CSVReader.GetInt(values[48]);
                item.energyCostTerrain13 = CSVReader.GetInt(values[49]);
                item.energyCostTerrain14 = CSVReader.GetInt(values[50]);
                item.energyCostTerrain15 = CSVReader.GetInt(values[51]);
                item.secondShotEnergy = CSVReader.GetInt(values[52]);
                item.secondShotTimeout = CSVReader.GetInt(values[53]);
                item.fireDelay = CSVReader.GetInt(values[54]);
                item.fireDelayOther = CSVReader.GetInt(values[55]);
                item.maxFireDelay = CSVReader.GetInt(values[56]);
                item.entryFireDelay = CSVReader.GetInt(values[57]);
                item.reloadDelayNormal = CSVReader.GetInt(values[58]);
                item.reloadDelayPartial = CSVReader.GetInt(values[59]);
                item.reloadDelayAsynchronous = CSVReader.GetInt(values[60]);
                item.reloadDelayAsynchronousPartial = CSVReader.GetInt(values[61]);
                item.routeRange = CSVReader.GetInt(values[62]);
                item.routeRotationalRange = CSVReader.GetInt(values[63]);
                item.routeFriendly = CSVReader.GetBool(values[65]);
                item.recoil = CSVReader.GetInt(values[66]);
                item.verticalRecoil = CSVReader.GetInt(values[67]);
                item.prefireDelay = CSVReader.GetInt(values[68]);
                item.reliability = CSVReader.GetInt(values[69]);
                item.reliabilityFireDelay = CSVReader.GetInt(values[70]);
                item.movementCancelsPrefire = CSVReader.GetBool(values[83]);
                item.notifyOthersOfPrefire = CSVReader.GetBool(values[84]);
                item.cashCost = CSVReader.GetInt(values[85]);
                item.useWhileCarryingBall = CSVReader.GetBool(values[86]);
                item.useWhileCarryingFlag = CSVReader.GetBool(values[87]);
                item.itemMakerItemID = CSVReader.GetInt(values[93]);
                item.itemMakerQuantity = CSVReader.GetInt(values[94]);

                return item;

            }

			public override bool getAmmoType(out int _ammoID, out int _ammoCount)
			{
				_ammoID = useAmmoID;
				_ammoCount = ammoUsedPerShot;
				return true;
			}

			public override bool getAmmoType(out int _ammoID, out int _ammoCount, out int _ammoCapacity)
			{
				_ammoID = useAmmoID;
				_ammoCount = ammoUsedPerShot;
				_ammoCapacity = ammoCapacity;
				return true;
			}

			public override bool getRouteRange(out int range)
			{
				range = routeRange;
				return true;
			}
        }
    }
}