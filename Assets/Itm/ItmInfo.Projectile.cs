using System.Collections.Generic;

namespace Assets
{
    public partial class ItemInfo
    {
        public class Projectile : ItemInfo
        {
            public Graphics iconGraphic, fireGraphic, projectileGraphic, shadowGraphic, trailGraphic, explosionGraphic, prefireGraphic;
            public Sound firingSound, explosionSound, bounceSound, prefireSound;
            public int useAmmoID;
            public int ammoUsedPerShot;
            public int ammoCapacity;
            public int requiredItem;
            public int requiredItemAmount;
            public int terrain0EnergyCost;
            public int terrain1EnergyCost;
            public int terrain2EnergyCost;
            public int terrain3EnergyCost;
            public int terrain4EnergyCost;
            public int terrain5EnergyCost;
            public int terrain6EnergyCost;
            public int terrain7EnergyCost;
            public int terrain8EnergyCost;
            public int terrain9EnergyCost;
            public int terrain10EnergyCost;
            public int terrain11EnergyCost;
            public int terrain12EnergyCost;
            public int terrain13EnergyCost;
            public int terrain14EnergyCost;
            public int terrain15EnergyCost;
            public int secondShotEnergy;
            public int secondShotTimeout;
            public int fireDelay;
            public int fireDelayOther;
            public int maxFireDelay;
            public int entryFireDelay;
            public int reloadDelayNormal;
            public int reloadDelayPartial;
            public int reloadDelayAsyncronous;
            public int reloadDelayAsynchronousPartial;
            public int routeRange;
            public int routeRotationalRange;
            public int routeFriendly;
            public int recoil;
            public int verticle;
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
            public int horizontalFriction;
            public int inheritedSpeed;
            public int inheritZSpeed;
            public int startHeightAdjust;
            public int lowRotationAngle;
            public int highRotationAngle;
            public int lowFireAngle;
            public int highFireAngle;
            public int muzzleVelocity;
            public int gravityAcceleration;
            public int horizontalBounceSpeed;
            public int bounceCount;
            public int floorBounceVerticalSpeed;
            public int floorBounceHorizontalSpeed;
            public int floorBounceCount;
            public int proximityRadius;
            public int triggerWeight;
            public int aliveTime;
            public int rotationalStartTime;
            public int rotationalEndTime;
            public int rotationalSpeed;
            public int rotationalAcceleration;
            public int inactiveTime;
            public int damageMode;
            public bool damageAccessible;
            public int trailDelay;
            public int explosionRecoilRadius;
            public int explosionRecoilIgnoreTime;
            public int explosionRecoilVehicleVertical;
            public int explosionRecoilWeapon;
            public int explosionRecoilWeaponDuration;
            public bool explosionRecoilVehicleAbsolute;
            public bool explosionRecoilWeaponAbsolute;
            public int explosionRecoilDirectionPercent;
            public int explosionScreenShakeAmount;
            public int maxLiveCategoryCode;
            public int maxLivePerPlayer;
            public int maxLivePerTeam;
            public int maxLivePerLevel;
            public int antiEffectsRadius;
            public int antiEffectsRecharge;
            public int antiEffectsFire;
            public int antiEffectsBallPickupDuration;
            public int antiEffectsRotate;
            public int antiEffectsThrust;
            public int antiEffectsBallThrowDuration;
            public bool preventPointBlank;
            public int explodeItem;
            public int projectileRadarMode;
            public int damageEventRadius;
            public string damageEventString;
            public bool portalGravity;
            public int vehicleGravity;
            public int kineticDamageRadius;
            public int kineticDamageInner;
            public int kineticDamageOuter;
            public int kineticDamageMode;
            public int explosiveDamageRadius;
            public int explosiveDamageInner;
            public int explosiveDamageOuter;
            public int explosiveDamageMode;
            public int electronicDamageRadius;
            public int electronicDamageInner;
            public int electronicDamageOuter;
            public int electronicDamageMode;
            public int psionicDamageRadius;
            public int psionicDamageInner;
            public int psionicDamageOuter;
            public int psionicDamageMode;
            public int bypassDamageRadius;
            public int bypassDamageInner;
            public int bypassDamageOuter;
            public int bypassDamageMode;
            public int energyDamageRadius;
            public int energyDamageInner;
            public int energyDamageOuter;
            public int energyDamageMode;



            public static Projectile Load(List<string> values)
            {
                Projectile projectile = new Projectile();
                projectile.iconGraphic = new Graphics(ref values, 23);
                projectile.fireGraphic = new Graphics(ref values, 175);
                projectile.projectileGraphic = new Graphics(ref values, 183);
                projectile.shadowGraphic = new Graphics(ref values, 191);
                projectile.trailGraphic = new Graphics(ref values, 199);
                projectile.explosionGraphic = new Graphics(ref values, 207);
                projectile.prefireGraphic = new Graphics(ref values, 71);
                projectile.firingSound = new Sound(ref values, 215);
                projectile.explosionSound = new Sound(ref values, 219);
                projectile.bounceSound = new Sound(ref values, 223);
                projectile.prefireSound = new Sound(ref values, 79);
                ItemInfo.LoadGeneralSettings1((ItemInfo)projectile, values);
                projectile.useAmmoID = CSVReader.GetInt(values[31]);
                projectile.ammoUsedPerShot = CSVReader.GetInt(values[32]);
                projectile.ammoCapacity = CSVReader.GetInt(values[33]);
                projectile.requiredItem = CSVReader.GetInt(values[34]);
                projectile.requiredItemAmount = CSVReader.GetInt(values[35]);
                projectile.terrain0EnergyCost = CSVReader.GetInt(values[36]);
                projectile.terrain1EnergyCost = CSVReader.GetInt(values[37]);
                projectile.terrain2EnergyCost = CSVReader.GetInt(values[38]);
                projectile.terrain3EnergyCost = CSVReader.GetInt(values[39]);
                projectile.terrain4EnergyCost = CSVReader.GetInt(values[40]);
                projectile.terrain5EnergyCost = CSVReader.GetInt(values[41]);
                projectile.terrain6EnergyCost = CSVReader.GetInt(values[42]);
                projectile.terrain7EnergyCost = CSVReader.GetInt(values[43]);
                projectile.terrain8EnergyCost = CSVReader.GetInt(values[44]);
                projectile.terrain9EnergyCost = CSVReader.GetInt(values[45]);
                projectile.terrain10EnergyCost = CSVReader.GetInt(values[46]);
                projectile.terrain11EnergyCost = CSVReader.GetInt(values[47]);
                projectile.terrain12EnergyCost = CSVReader.GetInt(values[48]);
                projectile.terrain13EnergyCost = CSVReader.GetInt(values[49]);
                projectile.terrain14EnergyCost = CSVReader.GetInt(values[50]);
                projectile.terrain15EnergyCost = CSVReader.GetInt(values[51]);
                projectile.secondShotEnergy = CSVReader.GetInt(values[52]);
                projectile.secondShotTimeout = CSVReader.GetInt(values[53]);
                projectile.fireDelay = CSVReader.GetInt(values[54]);
                projectile.fireDelayOther = CSVReader.GetInt(values[55]);
                projectile.maxFireDelay = CSVReader.GetInt(values[56]);
                projectile.entryFireDelay = CSVReader.GetInt(values[57]);
                projectile.reloadDelayNormal = CSVReader.GetInt(values[58]);
                projectile.reloadDelayPartial = CSVReader.GetInt(values[59]);
                projectile.reloadDelayAsyncronous = CSVReader.GetInt(values[60]);
                projectile.reloadDelayAsynchronousPartial = CSVReader.GetInt(values[61]);
                projectile.routeRange = CSVReader.GetInt(values[62]);
                projectile.routeRotationalRange = CSVReader.GetInt(values[63]);
                projectile.routeFriendly = CSVReader.GetInt(values[65]);
                projectile.recoil = CSVReader.GetInt(values[66]);
                projectile.verticle = CSVReader.GetInt(values[67]);
                projectile.prefireDelay = CSVReader.GetInt(values[68]);
                projectile.reliability = CSVReader.GetInt(values[69]);
                projectile.reliabilityFireDelay = CSVReader.GetInt(values[70]);
                projectile.movementCancelsPrefire = CSVReader.GetBool(values[83]);
                projectile.notifyOthersOfPrefire = CSVReader.GetBool(values[84]);
                projectile.cashCost = CSVReader.GetInt(values[85]);
                projectile.useWhileCarryingBall = CSVReader.GetBool(values[86]);
                projectile.useWhileCarryingFlag = CSVReader.GetBool(values[87]);
                projectile.soccerThrow = CSVReader.GetInt(values[88]);
                projectile.soccerBallFriction = CSVReader.GetInt(values[89]);
                projectile.soccerBallSpeed = CSVReader.GetInt(values[90]);
                projectile.soccerLowFireAngle = CSVReader.GetInt(values[91]);
                projectile.soccerHighFireAngle = CSVReader.GetInt(values[92]);
                projectile.horizontalFriction = CSVReader.GetInt(values[93]);
                projectile.inheritedSpeed = CSVReader.GetInt(values[94]);
                projectile.inheritZSpeed = CSVReader.GetInt(values[95]);
                projectile.startHeightAdjust = CSVReader.GetInt(values[96]);
                projectile.lowRotationAngle = CSVReader.GetInt(values[97]);
                projectile.highRotationAngle = CSVReader.GetInt(values[98]);
                projectile.lowFireAngle = CSVReader.GetInt(values[99]);
                projectile.highFireAngle = CSVReader.GetInt(values[100]);
                projectile.muzzleVelocity = CSVReader.GetInt(values[101]);
                projectile.gravityAcceleration = CSVReader.GetInt(values[102]);
                projectile.horizontalBounceSpeed = CSVReader.GetInt(values[103]);
                projectile.bounceCount = CSVReader.GetInt(values[104]);
                projectile.floorBounceVerticalSpeed = CSVReader.GetInt(values[105]);
                projectile.floorBounceHorizontalSpeed = CSVReader.GetInt(values[106]);
                projectile.floorBounceCount = CSVReader.GetInt(values[107]);
                projectile.proximityRadius = CSVReader.GetInt(values[108]);
                projectile.triggerWeight = CSVReader.GetInt(values[109]);
                projectile.aliveTime = CSVReader.GetInt(values[110]);
                projectile.rotationalStartTime = CSVReader.GetInt(values[111]);
                projectile.rotationalEndTime = CSVReader.GetInt(values[112]);
                projectile.rotationalSpeed = CSVReader.GetInt(values[113]);
                projectile.rotationalAcceleration = CSVReader.GetInt(values[114]);
                projectile.inactiveTime = CSVReader.GetInt(values[115]);
                projectile.damageMode = CSVReader.GetInt(values[116]);
                projectile.damageAccessible = CSVReader.GetBool(values[117]);
                projectile.trailDelay = CSVReader.GetInt(values[119]);
                projectile.explosionRecoilRadius = CSVReader.GetInt(values[120]);
                projectile.explosionRecoilVehicleVertical = CSVReader.GetInt(values[122]);
                projectile.explosionRecoilIgnoreTime = CSVReader.GetInt(values[123]);
                projectile.explosionRecoilWeapon = CSVReader.GetInt(values[124]);
                projectile.explosionRecoilWeaponDuration = CSVReader.GetInt(values[125]);
                projectile.explosionRecoilVehicleAbsolute = CSVReader.GetBool(values[126]);
                projectile.explosionRecoilWeaponAbsolute = CSVReader.GetBool(values[127]);
                projectile.explosionRecoilDirectionPercent = CSVReader.GetInt(values[128]);
                projectile.explosionScreenShakeAmount = CSVReader.GetInt(values[129]);
                projectile.maxLiveCategoryCode = CSVReader.GetInt(values[130]);
                projectile.maxLivePerPlayer = CSVReader.GetInt(values[131]);
                projectile.maxLivePerTeam = CSVReader.GetInt(values[132]);
                projectile.maxLivePerLevel = CSVReader.GetInt(values[133]);
                projectile.antiEffectsRadius = CSVReader.GetInt(values[134]);
                projectile.antiEffectsRecharge = CSVReader.GetInt(values[135]);
                projectile.antiEffectsFire = CSVReader.GetInt(values[136]);
                projectile.antiEffectsBallPickupDuration = CSVReader.GetInt(values[137]);
                projectile.antiEffectsRotate = CSVReader.GetInt(values[138]);
                projectile.antiEffectsThrust = CSVReader.GetInt(values[139]);
                projectile.antiEffectsBallThrowDuration = CSVReader.GetInt(values[140]);
                projectile.preventPointBlank = CSVReader.GetBool(values[141]);
                projectile.explodeItem = CSVReader.GetInt(values[142]);
                projectile.projectileRadarMode = CSVReader.GetInt(values[143]);
                projectile.damageEventRadius = CSVReader.GetInt(values[146]);
                projectile.damageEventString = CSVReader.GetString(values[147]);
                projectile.portalGravity = CSVReader.GetBool(values[148]);
                projectile.vehicleGravity = CSVReader.GetInt(values[150]);
                projectile.kineticDamageRadius = CSVReader.GetInt(values[151]);
                projectile.kineticDamageInner = CSVReader.GetInt(values[152]);
                projectile.kineticDamageOuter = CSVReader.GetInt(values[153]);
                projectile.kineticDamageMode = CSVReader.GetInt(values[154]);
                projectile.explosiveDamageRadius = CSVReader.GetInt(values[155]);
                projectile.explosiveDamageInner = CSVReader.GetInt(values[156]);
                projectile.explosiveDamageOuter = CSVReader.GetInt(values[157]);
                projectile.explosiveDamageMode = CSVReader.GetInt(values[158]);
                projectile.electronicDamageRadius = CSVReader.GetInt(values[159]);
                projectile.electronicDamageInner = CSVReader.GetInt(values[160]);
                projectile.electronicDamageOuter = CSVReader.GetInt(values[161]);
                projectile.electronicDamageMode = CSVReader.GetInt(values[162]);
                projectile.psionicDamageRadius = CSVReader.GetInt(values[163]);
                projectile.psionicDamageInner = CSVReader.GetInt(values[164]);
                projectile.psionicDamageOuter = CSVReader.GetInt(values[165]);
                projectile.psionicDamageMode = CSVReader.GetInt(values[166]);
                projectile.bypassDamageRadius = CSVReader.GetInt(values[167]);
                projectile.bypassDamageInner = CSVReader.GetInt(values[168]);
                projectile.bypassDamageOuter = CSVReader.GetInt(values[169]);
                projectile.bypassDamageMode = CSVReader.GetInt(values[170]);
                projectile.energyDamageRadius = CSVReader.GetInt(values[171]);
                projectile.energyDamageInner = CSVReader.GetInt(values[172]);
                projectile.energyDamageOuter = CSVReader.GetInt(values[173]);
                projectile.energyDamageMode = CSVReader.GetInt(values[174]);






                return projectile;
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
