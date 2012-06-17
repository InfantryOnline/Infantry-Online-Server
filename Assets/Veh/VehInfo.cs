using System;
using System.Collections.Generic;

namespace Assets
{
    public abstract partial class VehInfo : ICsvParseable
    {
        protected static TType[] CreateInstances<TType>(int count)
            where TType : class, new()
        {
            TType[] array = new TType[count];
            for (int i = 0; i < count; i++)
            {
                array[i] = new TType();
            }
            return array;
        }

        public enum Types
        {
            Car = 2,
            Dependent,
            Spectator,
            Computer,
            Nested
        }

        private static VehInfo Instantiate(int id)
        {
            switch (id)
            {
                case 2: return new Car();
                case 3: return new Dependent();
                case 4: return new Spectator();
                case 5: return new Computer();
                case 6: return new Nested();
            }

            return null;
        }

        public static List<VehInfo> Load(string path)
        {
            return CsvFile<VehInfo>.Load(path, Instantiate, false);
        }

        public Types Type;
        public int Version;
        public int Id; // 434
        public string Name; // 3D4
        public string SkillLogic; // 394
        public string Description; // 004
        public int Hitpoints; // 438
        public int Weight; // 420
        public int ClassId; // 424
        public ArmorValues[] Armors; // 444
        public short BarrelLength; // 404
        public short FireHeight; // 406
        public short PhysicalRadius; // 408
        public short TriggerRadius; // 40A
        public short VehiclePhysicsOwned; // 3FC
        public short VehiclePhysicsUnowned; // 3FE
        public short WeaponPhysicsOwned; // 400
        public short WeaponPhysicsUnowned; // 402
        public short LowZ; // 416
        public short HighZ; // 418
        public short PickupItemId; // 41A
        public bool IsControllable; // 41C  // MARKED AS 'BOOLEAN' in Mongoose' txt
		public bool IsWarpable; // 41E   // MARKED AS 'BOOLEAN' in Mongoose' txt
        public int EnergyCostPerTick; // 428
        public int NormalWeight; // 42C
        public int StopWeight; // 430
        public short DisplayHealth; // 40C
        public int[] InventoryItems; // 48C
        public int[] ChildVehicles; // 4A4
        public short ExplodeItemId; // 40E
        public int RadarPrize; // 43C
        public int VehicleGetInMode; // 440
        public short VisualHeight; // 414
        public short DropItemId; // 410
        public short DropItemQuantity; // 412
        public AnimatedSprite[] Sprites = CreateInstances<AnimatedSprite>(2); // 4C4
        public int EnergyMax; // 564
        public int EnergyRate; // 568
        public int EnergyMin; // 56C
        public short ThrowTime; // 570
        public short AllowWeapons; // 572
        public short Style1BallSpeed; // 576
        public short Style1LowFireAngle; // 57A
        public short Style1HighFireAngle; // 57E
        public short Style1KeyAssignment; // 582
        public short Style1BallFriction; // 574
        public short Style2BallSpeed; // 578
        public short Style2LowFireAngle; // 57C
        public short Style2HighFireAngle; // 580
        public short Style2KeyAssignment; // 584
        public short Style2BallFriction; // 586
        public short[] TerrainModifiers; // 588
        public short BallCarryRollVehicleItem; // 5A8
        public short BallCarryArmorVehicleItem; // 5AA
        public short FlagCarryRollVehicleItem; // 5AC
        public short FlagCarryArmorVehicleItem; // 5AE
        public string EventString1; // 094
        public string EventString2; // 194
        public string EventString3; // 294
        public int LosDistance; // 550
        public int LosAngle; // 554
        public int LosXRay; // 558
        public int CombatAwarenessTime; // 55C
        public short SiblingKillsShared; // 560
        public short RelativeId; // 5B0
        public int DisplayOnFriendlyRadar; // 00C
        public int FriendlyRadarColor; // 014
        public int DisplayOnEnemyRadar; // 010
        public int EnemyRadarColor; // 018
        public short[] HoldItemLimits = new short[30]; // 01C
        public short[] HoldItemExtendedLimits = new short[30]; // 058
        public List<string> blofiles = new List<string>();

        public override string ToString()
        {
            return String.Format("{0}({1}) : {2}", Name, Id, Type);
        }

        public virtual void Parse(ICsvParser parser)
        {
            this.Version = parser.GetInt('v');
            this.Id = parser.GetInt();
            this.Name = parser.GetString();
            this.SkillLogic = parser.GetQuotedString();
            this.Description = parser.GetString();
            this.Hitpoints = parser.GetInt();
            this.Weight = parser.GetInt();
            this.ClassId = this.Version < 31 ? 0 : parser.GetInt();
            this.Armors = parser.GetInstances<ArmorValues>(6);
            this.BarrelLength = parser.GetShort();
            this.FireHeight = parser.GetShort();

            if (this.Version >= 34)
            {
                this.PhysicalRadius = parser.GetShort();
                this.TriggerRadius = parser.GetShort();
            }
            else
            {
                short dummy = parser.GetShort();
                
                this.PhysicalRadius = dummy;
                this.TriggerRadius = dummy;

                if (dummy != -1)
                {
                    dummy -= 8;
                    this.PhysicalRadius = (dummy >= 1) ? dummy : (short)1;
                }
            }

            if (this.Version >= 11)
            {
                this.VehiclePhysicsOwned = parser.GetShort();
                this.VehiclePhysicsUnowned = parser.GetShort();
                this.WeaponPhysicsOwned = parser.GetShort();
                this.WeaponPhysicsUnowned = parser.GetShort();
            }
            else
            {
                parser.Skip();
                parser.Skip();
            }

            this.LowZ = parser.GetShort();
            this.HighZ = parser.GetShort();
            this.PickupItemId = parser.GetShort();
            this.IsControllable = parser.GetShort() == 1;
			this.IsWarpable = ((this.Version >= 8) ? parser.GetShort() : (short)1) == 1;
            this.EnergyCostPerTick = parser.GetInt();
            this.NormalWeight = parser.GetInt();
            this.StopWeight = parser.GetInt();
            this.DisplayHealth = (this.Version >= 1) ? parser.GetShort() : (short)0;
            this.InventoryItems = parser.GetInts(6);
            this.ChildVehicles = parser.GetInts(8);

            if (this.Version >= 6 && this.Version < 31)
            {
                parser.Skip(31);
            }

            this.ExplodeItemId = (this.Version >= 17) ? parser.GetShort() : (short)0;
            this.RadarPrize = (this.Version >= 35) ? parser.GetInt() : -1;
            this.VehicleGetInMode = (this.Version >= 36) ? parser.GetInt() : 0;
            this.VisualHeight = (this.Version >= 37) ? parser.GetShort() : (short)0;
            this.DropItemId = (this.Version >= 46) ? parser.GetShort() : (short)0;
            this.DropItemQuantity = (this.Version >= 46) ? parser.GetShort() : (short)0;

            if (this.Version >= 5)
            {
                this.Sprites[0].Parse(this.Version, parser);
                this.Sprites[1].Parse(this.Version, parser);
                blofiles.Add(Sprites[0].Blob.BlobName);
                blofiles.Add(Sprites[1].Blob.BlobName);
            }

            this.EnergyMax = (this.Version >= 5) ? parser.GetInt() : -1;
            this.EnergyRate = (this.Version >= 5) ? parser.GetInt() : -1;
            this.EnergyMin = (this.Version >= 21) ? parser.GetInt() : -1;
            this.ThrowTime = (this.Version >= 9) ? parser.GetShort() : (short)-1;

            if (this.Version >= 9 && this.Version < 24)
            {
                parser.Skip();
            }

            this.AllowWeapons = (this.Version >= 9) ? parser.GetShort() : (short)-1;
            this.Style1BallSpeed = (this.Version >= 9) ? parser.GetShort() : (short)0;
            this.Style1LowFireAngle = (this.Version >= 9) ? parser.GetShort() : (short)45;
            this.Style1HighFireAngle = (this.Version >= 9) ? parser.GetShort() : (short)45;
            this.Style1KeyAssignment = (this.Version >= 15) ? parser.GetShort() : (short)1;
            this.Style1BallFriction = (this.Version >= 15) ? parser.GetShort() : (short)-1;
            this.Style2BallSpeed = (this.Version >= 15) ? parser.GetShort() : (short)0;
            this.Style2LowFireAngle = (this.Version >= 15) ? parser.GetShort() : (short)0;
            this.Style2HighFireAngle = (this.Version >= 15) ? parser.GetShort() : (short)0;
            this.Style2KeyAssignment = (this.Version >= 15) ? parser.GetShort() : (short)0;
            this.Style2BallFriction = (this.Version >= 9) ? parser.GetShort() : (short)1;

            if (this.Version >= 24)
            {
                this.TerrainModifiers = parser.GetShorts(16);
            }
            else
            {
                this.TerrainModifiers = new short[16];
                for (int i = 0; i < this.TerrainModifiers.Length; i++)
                {
                    this.TerrainModifiers[i] = -1;
                }
            }

            if (this.Version >= 9 && this.Version < 23)
            {
                parser.Skip();
            }

            this.BallCarryRollVehicleItem = (this.Version >= 29) ? parser.GetShort() : (short)0;
            this.BallCarryArmorVehicleItem = (this.Version >= 29) ? parser.GetShort() : (short)0;
            this.FlagCarryRollVehicleItem = (this.Version >= 29) ? parser.GetShort() : (short)0;
            this.FlagCarryArmorVehicleItem = (this.Version >= 29) ? parser.GetShort() : (short)0;
            this.EventString1 = (this.Version >= 33) ? parser.GetString() : "";
            this.EventString2 = (this.Version >= 38) ? parser.GetString() : "";
            this.EventString3 = (this.Version >= 38) ? parser.GetString() : "";
            this.LosDistance = (this.Version >= 42) ? parser.GetInt() : 0;
            this.LosAngle = (this.Version >= 42) ? parser.GetInt() : 0;
            this.LosXRay = (this.Version >= 42) ? parser.GetInt() : 0;
            this.CombatAwarenessTime = (this.Version >= 42) ? parser.GetInt() : 0;
            this.SiblingKillsShared = (this.Version >= 42) ? parser.GetShort() : (short)0;
            this.RelativeId = (this.Version >= 47) ? parser.GetShort() : (short)0;
            this.DisplayOnFriendlyRadar = (this.Version >= 52) ? parser.GetInt() : 1;
            this.FriendlyRadarColor = (this.Version >= 50) ? parser.GetInt() : -1;
            this.DisplayOnEnemyRadar = (this.Version >= 52) ? parser.GetInt() : 1;
            this.EnemyRadarColor = (this.Version >= 50) ? parser.GetInt() : -1;

            if (this.Version >= 48)
            {
                for (int i = 0; i < 30; i++)
                {
                    this.HoldItemLimits[i] = parser.GetShort();
                    this.HoldItemExtendedLimits[i] = parser.GetShort();
                }
            }
        }
    }
}
