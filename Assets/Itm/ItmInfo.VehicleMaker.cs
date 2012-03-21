using System;
using System.Collections.Generic;

namespace Assets
{
    public partial class ItemInfo
    {
        public class VehicleMaker : ItemInfo
        {
            public Graphics prefireGraphic, iconGraphic;
            public Sound prefireSound;
            
            public int ammoID;
            public int ammoUsedPerShot;
            public int ammoCapacity;
            public int requiredItemID;
            public int requiredItemAmount;
            public int energyUseTerrain1;
            public int energyUseTerrain2;
            public int energyUseTerrain3;
            public int energyUseTerrain4;
            public int energyUseTerrain5;
            public int energyUseTerrain6;
            public int energyUseTerrain7;
            public int energyUseTerrain8;
            public int energyUseTerrain9;
            public int energyUseTerrain10;
            public int energyUseTerrain11;
            public int energyUseTerrain12;
            public int energyUseTerrain13;
            public int energyUseTerrain14;
            public int energyUseTerrain15;
            public int energyUseTerrain16;
            public int secondShotEnergy;
            public int secondShotTimeout;
            public int fireDelay;
            public int fireDelayOther;
            public int maxFireDelay;
            public int entryFireDelay;
            public int normalReloadDelay;
            public int partialReloadDelay;
            public int asynchronousReloadDelay;
            public int asynchronousPartialReloadDelay;
            public int routeRange;
            public int routeRotationalRange;
            public int recoil;
            public int verticalRecoil;
            public int prefireDelay;
            public int reliabilityMisfire;
            public int reliabilityMisfireFireDelay;
            public int movementCancelsPrefire;
            public int prefireNotify;
            public int cashCost;
            public int useWhileCarryingBall;
            public int useWhileCarryingFlag;
            public int soccerThrow;
            public int soccerBallFriction;
            public int soccerBallSpeed;
            public int soccerLowFireAngle;
            public int soccerHighFireAngle;
            public int vehicleID;

            public static VehicleMaker Load(List<string> values){

                VehicleMaker vehicleMaker = new VehicleMaker();
                vehicleMaker.iconGraphic = new Graphics(ref values, 23);
                vehicleMaker.prefireGraphic = new Graphics(ref values, 71);
                vehicleMaker.prefireSound = new Sound(ref values, 79);

                vehicleMaker.itemType = (ItemType)CSVReader.GetInt(values[0]);
                vehicleMaker.version = CSVReader.GetInt(values[1].Trim('v'));
                vehicleMaker.id = CSVReader.GetInt(values[2]);
                vehicleMaker.name = CSVReader.GetQuotedString(values[3]);
                vehicleMaker.category = CSVReader.GetString(values[4]);
				vehicleMaker.skillLogic = CSVReader.GetQuotedString(values[5]);
                vehicleMaker.description = CSVReader.GetString(values[6]);
                vehicleMaker.weight = CSVReader.GetInt(values[7]);
                vehicleMaker.buyPrice = CSVReader.GetInt(values[8]);
                vehicleMaker.probability = CSVReader.GetInt(values[9]);
                vehicleMaker.droppable = CSVReader.GetBool(values[10]);
                vehicleMaker.keyPreference = CSVReader.GetInt(values[11]);
                vehicleMaker.recommended = CSVReader.GetInt(values[12]);
                vehicleMaker.maxAllowed = CSVReader.GetInt(values[13]);
                vehicleMaker.pickupMode = (PickupMode)CSVReader.GetInt(values[14]);
                vehicleMaker.sellPrice = CSVReader.GetInt(values[15]);
                vehicleMaker.radarColor = CSVReader.GetInt(values[17]);
                vehicleMaker.ammoID = CSVReader.GetInt(values[31]);
                vehicleMaker.ammoUsedPerShot = CSVReader.GetInt(values[32]);
                vehicleMaker.ammoCapacity = CSVReader.GetInt(values[33]);
                vehicleMaker.requiredItemID = CSVReader.GetInt(values[34]);
                vehicleMaker.requiredItemAmount = CSVReader.GetInt(values[35]);
                vehicleMaker.energyUseTerrain1 = CSVReader.GetInt(values[36]);
                vehicleMaker.energyUseTerrain2 = CSVReader.GetInt(values[37]);
                vehicleMaker.energyUseTerrain3 = CSVReader.GetInt(values[38]);
                vehicleMaker.energyUseTerrain4 = CSVReader.GetInt(values[39]);
                vehicleMaker.energyUseTerrain5 = CSVReader.GetInt(values[40]);
                vehicleMaker.energyUseTerrain6 = CSVReader.GetInt(values[41]);
                vehicleMaker.energyUseTerrain7 = CSVReader.GetInt(values[42]);
                vehicleMaker.energyUseTerrain8 = CSVReader.GetInt(values[43]);
                vehicleMaker.energyUseTerrain9 = CSVReader.GetInt(values[44]);
                vehicleMaker.energyUseTerrain10 = CSVReader.GetInt(values[45]);
                vehicleMaker.energyUseTerrain11 = CSVReader.GetInt(values[46]);
                vehicleMaker.energyUseTerrain12 = CSVReader.GetInt(values[47]);
                vehicleMaker.energyUseTerrain13 = CSVReader.GetInt(values[48]);
                vehicleMaker.energyUseTerrain14 = CSVReader.GetInt(values[49]);
                vehicleMaker.energyUseTerrain15 = CSVReader.GetInt(values[50]);
                vehicleMaker.energyUseTerrain16 = CSVReader.GetInt(values[51]);
                vehicleMaker.secondShotEnergy = CSVReader.GetInt(values[52]);
                vehicleMaker.secondShotTimeout = CSVReader.GetInt(values[53]);
                vehicleMaker.fireDelay = CSVReader.GetInt(values[54]);
                vehicleMaker.fireDelayOther = CSVReader.GetInt(values[55]);
                vehicleMaker.maxFireDelay = CSVReader.GetInt(values[56]);
                vehicleMaker.entryFireDelay = CSVReader.GetInt(values[57]);
                vehicleMaker.normalReloadDelay = CSVReader.GetInt(values[58]);
                vehicleMaker.partialReloadDelay = CSVReader.GetInt(values[59]);
                vehicleMaker.asynchronousReloadDelay = CSVReader.GetInt(values[60]);
                vehicleMaker.asynchronousPartialReloadDelay = CSVReader.GetInt(values[61]);
                vehicleMaker.routeRange = CSVReader.GetInt(values[62]);
                vehicleMaker.routeRotationalRange = CSVReader.GetInt(values[63]);
                vehicleMaker.routeFriendly = CSVReader.GetBool(values[65]);
                vehicleMaker.recoil = CSVReader.GetInt(values[66]);
                vehicleMaker.verticalRecoil = CSVReader.GetInt(values[67]);
                vehicleMaker.prefireDelay = CSVReader.GetInt(values[68]);
                vehicleMaker.reliabilityMisfire = CSVReader.GetInt(values[69]);
                vehicleMaker.reliabilityMisfireFireDelay = CSVReader.GetInt(values[70]);
                vehicleMaker.movementCancelsPrefire = CSVReader.GetInt(values[83]);
                vehicleMaker.prefireNotify = CSVReader.GetInt(values[84]);
                vehicleMaker.cashCost = CSVReader.GetInt(values[85]);
                vehicleMaker.useWhileCarryingBall = CSVReader.GetInt(values[86]);
                vehicleMaker.useWhileCarryingFlag = CSVReader.GetInt(values[87]);
                vehicleMaker.soccerThrow = CSVReader.GetInt(values[88]);
                vehicleMaker.soccerBallFriction = CSVReader.GetInt(values[89]);
                vehicleMaker.soccerBallSpeed = CSVReader.GetInt(values[90]);
                vehicleMaker.soccerLowFireAngle = CSVReader.GetInt(values[91]);
                vehicleMaker.soccerHighFireAngle = CSVReader.GetInt(values[92]);
                vehicleMaker.vehicleID = CSVReader.GetInt(values[93]);

                return vehicleMaker;
            }

			public override bool getAmmoType(out int _ammoID, out int _ammoCount)
			{
				_ammoID = ammoID;
				_ammoCount = ammoUsedPerShot;
				return true;
			}

			public override bool getAmmoType(out int _ammoID, out int _ammoCount, out int _ammoCapacity)
			{
				_ammoID = ammoID;
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