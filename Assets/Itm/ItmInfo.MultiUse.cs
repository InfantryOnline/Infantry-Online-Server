using System.Collections.Generic;

namespace Assets
{
    public partial class ItemInfo
    {
        public class MultiUse : ItemInfo
        {
            public Graphics iconGraphic, prefireGraphic;
            public Sound prefireSound, firingSound;
            public int useAmmoID;
            public int ammoUsedPerShot;
            public int ammoCapacity;
            public int requiredItem;
            public int requiredAmmoAmount;
            public int energyCostTerrain0;
            public int energyCostTerrain1;
            public int energyCostTerrain2;
            public int energyCostTerrain3;
            public int energyCostTerrain4;
            public int energyCostTerrain5;
            public int energyCostTerrainTerrain6;
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
            public bool routeFriendly;
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
            public int soccerThrow;
            public int soccerBallFriction;
            public int soccerBallSpeed;
            public int soccerLowFireAngle;
            public int soccerHighFireAngle;

            public struct ChildItem
            {
                public enum MultiLinkModes
                {
                    None = 0,
                    Disappear,
                    Explode,
                }
                public int id;
                public int deltaX;
                public int deltaY;
                public int deltaZ;
                public int deltaAngle;
                public MultiLinkModes theMultiLinkMode;
            }

            public List<ChildItem> childItems = new List<ChildItem>();

            public static MultiUse Load(List<string> values)
            {
                MultiUse multiUse = new MultiUse();
                multiUse.prefireGraphic = new Graphics(ref values, 71);
                multiUse.prefireSound = new Sound(ref values, 79);
                multiUse.firingSound = new Sound(ref values, 93);
                multiUse.iconGraphic = new Graphics(ref values, 23);
                ItemInfo.LoadGeneralSettings1((ItemInfo)multiUse, values);

                multiUse.useAmmoID = CSVReader.GetInt(values[31]);
                multiUse.ammoUsedPerShot = CSVReader.GetInt(values[32]);
                multiUse.ammoCapacity = CSVReader.GetInt(values[33]);
                multiUse.requiredItem = CSVReader.GetInt(values[34]);
                multiUse.requiredAmmoAmount = CSVReader.GetInt(values[35]);
                multiUse.energyCostTerrain0 = CSVReader.GetInt(values[36]);
                multiUse.energyCostTerrain1 = CSVReader.GetInt(values[37]);
                multiUse.energyCostTerrain2 = CSVReader.GetInt(values[38]);
                multiUse.energyCostTerrain3 = CSVReader.GetInt(values[39]);
                multiUse.energyCostTerrain4 = CSVReader.GetInt(values[40]);
                multiUse.energyCostTerrain5 = CSVReader.GetInt(values[41]);
                multiUse.energyCostTerrainTerrain6 = CSVReader.GetInt(values[42]);
                multiUse.energyCostTerrain7 = CSVReader.GetInt(values[43]);
                multiUse.energyCostTerrain8 = CSVReader.GetInt(values[44]);
                multiUse.energyCostTerrain9 = CSVReader.GetInt(values[45]);
                multiUse.energyCostTerrain10 = CSVReader.GetInt(values[46]);
                multiUse.energyCostTerrain11 = CSVReader.GetInt(values[47]);
                multiUse.energyCostTerrain12 = CSVReader.GetInt(values[48]);
                multiUse.energyCostTerrain13 = CSVReader.GetInt(values[49]);
                multiUse.energyCostTerrain14 = CSVReader.GetInt(values[50]);
                multiUse.energyCostTerrain15 = CSVReader.GetInt(values[51]);
                multiUse.secondShotEnergy = CSVReader.GetInt(values[52]);
                multiUse.secondShotTimeout = CSVReader.GetInt(values[53]);
                multiUse.fireDelay = CSVReader.GetInt(values[54]);
                multiUse.fireDelayOther = CSVReader.GetInt(values[55]);
                multiUse.maxFireDelay = CSVReader.GetInt(values[56]);
                multiUse.entryFireDelay = CSVReader.GetInt(values[57]);
                multiUse.reloadDelayNormal = CSVReader.GetInt(values[58]);
                multiUse.reloadDelayPartial = CSVReader.GetInt(values[59]);
                multiUse.reloadDelayAsynchronous = CSVReader.GetInt(values[60]);
                multiUse.reloadDelayAsynchronousPartial = CSVReader.GetInt(values[61]);
                multiUse.routeRange = CSVReader.GetInt(values[62]);
                multiUse.routeRotationalRange = CSVReader.GetInt(values[63]);
                multiUse.routeFriendly = CSVReader.GetBool(values[65]);
                multiUse.recoil = CSVReader.GetInt(values[66]);
                multiUse.verticalRecoil = CSVReader.GetInt(values[67]);
                multiUse.prefireDelay = CSVReader.GetInt(values[68]);
                multiUse.reliability = CSVReader.GetInt(values[69]);
                multiUse.reliabilityFireDelay = CSVReader.GetInt(values[70]);
                multiUse.movementCancelsPrefire = CSVReader.GetBool(values[83]);
                multiUse.notifyOthersOfPrefire = CSVReader.GetBool(values[84]);
                multiUse.cashCost = CSVReader.GetInt(values[85]);
                multiUse.useWhileCarryingBall = CSVReader.GetBool(values[86]);
                multiUse.useWhileCarryingFlag = CSVReader.GetBool(values[87]);
                multiUse.soccerThrow = CSVReader.GetInt(values[88]);
                multiUse.soccerBallFriction = CSVReader.GetInt(values[89]);
                multiUse.soccerBallSpeed = CSVReader.GetInt(values[90]);
                multiUse.soccerLowFireAngle = CSVReader.GetInt(values[91]);
                multiUse.soccerHighFireAngle = CSVReader.GetInt(values[92]);

                int currentPlace = 97;
				multiUse.childItems = new List<ChildItem>();

                for (int i = 0; i < 32; i++)
                {
                    if (currentPlace < values.Count)
                    {
                        ChildItem child = new ChildItem();
                        child.id = CSVReader.GetInt(values[currentPlace + 0]);
                        child.deltaX = CSVReader.GetInt(values[currentPlace + 1]);
                        child.deltaY = CSVReader.GetInt(values[currentPlace + 2]);
                        child.deltaZ = CSVReader.GetInt(values[currentPlace + 3]);
                        child.deltaAngle = CSVReader.GetInt(values[currentPlace + 4]);
                        child.theMultiLinkMode = (ChildItem.MultiLinkModes)CSVReader.GetInt(values[currentPlace + 5]);
						multiUse.childItems.Add(child);
                        currentPlace += 6;
                    }
                    else
                        break;
                }
                return multiUse;
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