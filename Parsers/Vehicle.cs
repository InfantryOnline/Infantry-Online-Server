namespace Parsers
{
    /// <summary>
    /// cocks cocks cocks cocks
    /// </summary>
    public abstract partial class Vehicle : ICsvFormat
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

        public int Version;
        public int Id { get; set; } // 434
        public string Name { get; set; } // 3D4
        public string SkillLogic { get; set; } // 394
        public string Description { get; set; } // 004
        public int Hitpoints { get; set; } // 438
        public int Weight { get; set; } // 420
        public int ClassId { get; set; } // 424
        public ArmorValues[] Armors  { get; set; } // 444
        public short BarrelLength { get; set; } // 404
        public short FireHeight { get; set; } // 406
        public short PhysicalRadius { get; set; } // 408
        public short TriggerRadius { get; set; } // 40A
        public short VehiclePhysicsOwned { get; set; } // 3FC
        public short VehiclePhysicsUnowned { get; set; } // 3FE
        public short WeaponPhysicsOwned { get; set; } // 400
        public short WeaponPhysicsUnowned { get; set; } // 402
        public short LowZ { get; set; } // 416
        public short HighZ { get; set; } // 418
        public short PickupItemId { get; set; } // 41A
        public short IsControllable { get; set; } // 41C  // MARKED AS 'BOOLEAN' in Mongoose' txt
        public short IsWarpable { get; set; } // 41E   // MARKED AS 'BOOLEAN' in Mongoose' txt
        public int EnergyCostPerTick { get; set; } // 428
        public int NormalWeight { get; set; } // 42C
        public int StopWeight { get; set; } // 430
        public short DisplayHealth { get; set; } // 40C
        public int[] InventoryItems { get; set; } // 48C
        public int[] ChildVehicles { get; set; } // 4A4
        public short ExplodeItemId { get; set; } // 40E
        public int RadarPrize { get; set; } // 43C
        public int VehicleGetInMode { get; set; } // 440
        public short VisualHeight { get; set; } // 414
        public short DropItemId { get; set; } // 410
        public short DropItemQuantity { get; set; } // 412
        public AnimatedSprite[] Sprites { get; set; } // 4C4
        public int EnergyMax { get; set; } // 564
        public int EnergyRate { get; set; } // 568
        public int EnergyMin { get; set; } // 56C
        public short ThrowTime { get; set; } // 570
        public short AllowWeapons { get; set; } // 572
        public short Style1BallSpeed { get; set; } // 576
        public short Style1LowFireAngle { get; set; } // 57A
        public short Style1HighFireAngle { get; set; } // 57E
        public short Style1KeyAssignment { get; set; } // 582
        public short Style1BallFriction { get; set; } // 574
        public short Style2BallSpeed { get; set; } // 578
        public short Style2LowFireAngle { get; set; } // 57C
        public short Style2HighFireAngle { get; set; } // 580
        public short Style2KeyAssignment { get; set; } // 584
        public short Style2BallFriction { get; set; } // 586
        public short[] TerrainModifiers { get; set; } // 588
        public short BallCarryRollVehicleItem { get; set; } // 5A8
        public short BallCarryArmorVehicleItem { get; set; } // 5AA
        public short FlagCarryRollVehicleItem { get; set; } // 5AC
        public short FlagCarryArmorVehicleItem { get; set; } // 5AE
        public string EventString1 { get; set; } // 094
        public string EventString2 { get; set; } // 194
        public string EventString3 { get; set; } // 294
        public int LosDistance { get; set; } // 550
        public int LosAngle { get; set; } // 554
        public int LosXRay { get; set; } // 558
        public int CombatAwarenessTime { get; set; } // 55C
        public short SiblingKillsShared { get; set; } // 560
        public short RelativeId { get; set; } // 5B0
        public int DisplayOnFriendlyRadar { get; set; } // 00C
        public int FriendlyRadarColor { get; set; } // 014
        public int DisplayOnEnemyRadar { get; set; } // 010
        public int EnemyRadarColor { get; set; } // 018
        public short[] HoldItemLimits { get; set; } // 01C
        public short[] HoldItemExtendedLimits { get; set; } // 058

        public Vehicle()
        {
            this.Armors = CreateInstances<ArmorValues>(6);
            this.InventoryItems = new int[6];
            this.ChildVehicles = new int[8];
            this.Sprites = CreateInstances<AnimatedSprite>(2);
            this.TerrainModifiers = new short[16];
            this.HoldItemLimits = new short[30];
            this.HoldItemExtendedLimits = new short[30];
        }

        public virtual void Read(ICsvReader reader)
        {
            this.Version = reader.GetInt('v');
            this.Id = reader.GetInt();
            this.Name = reader.GetString();
            this.SkillLogic = reader.GetString();
            this.Description = reader.GetString();
            this.Hitpoints = reader.GetInt();
            this.Weight = reader.GetInt();
            this.ClassId = this.Version < 31 ? 0 : reader.GetInt();
            this.Armors = reader.GetInstances<ArmorValues>(6);
            this.BarrelLength = reader.GetShort();
            this.FireHeight = reader.GetShort();

            if (this.Version >= 34)
            {
                this.PhysicalRadius = reader.GetShort();
                this.TriggerRadius = reader.GetShort();
            }
            else
            {
                short dummy = reader.GetShort();
                
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
                this.VehiclePhysicsOwned = reader.GetShort();
                this.VehiclePhysicsUnowned = reader.GetShort();
                this.WeaponPhysicsOwned = reader.GetShort();
                this.WeaponPhysicsUnowned = reader.GetShort();
            }
            else
            {
                reader.Skip();
                reader.Skip();
            }

            this.LowZ = reader.GetShort();
            this.HighZ = reader.GetShort();
            this.PickupItemId = reader.GetShort();
            this.IsControllable = reader.GetShort();
            this.IsWarpable = (this.Version >= 8) ? reader.GetShort() : (short)1;
            this.EnergyCostPerTick = reader.GetInt();
            this.NormalWeight = reader.GetInt();
            this.StopWeight = reader.GetInt();
            this.DisplayHealth = (this.Version >= 1) ? reader.GetShort() : (short)0;
            this.InventoryItems = reader.GetInts(6);
            this.ChildVehicles = reader.GetInts(8);

            if (this.Version >= 6 && this.Version < 31)
            {
                reader.Skip(31);
            }

            this.ExplodeItemId = (this.Version >= 17) ? reader.GetShort() : (short)0;
            this.RadarPrize = (this.Version >= 35) ? reader.GetInt() : -1;
            this.VehicleGetInMode = (this.Version >= 36) ? reader.GetInt() : 0;
            this.VisualHeight = (this.Version >= 37) ? reader.GetShort() : (short)0;
            this.DropItemId = (this.Version >= 46) ? reader.GetShort() : (short)0;
            this.DropItemQuantity = (this.Version >= 46) ? reader.GetShort() : (short)0;

            if (this.Version >= 5)
            {
                this.Sprites[0].Read(this.Version, reader);
                this.Sprites[1].Read(this.Version, reader);
            }

            this.EnergyMax = (this.Version >= 5) ? reader.GetInt() : -1;
            this.EnergyRate = (this.Version >= 5) ? reader.GetInt() : -1;
            this.EnergyMin = (this.Version >= 21) ? reader.GetInt() : -1;
            this.ThrowTime = (this.Version >= 9) ? reader.GetShort() : (short)-1;

            if (this.Version >= 9 && this.Version < 24)
            {
                reader.Skip();
            }

            this.AllowWeapons = (this.Version >= 9) ? reader.GetShort() : (short)-1;
            this.Style1BallSpeed = (this.Version >= 9) ? reader.GetShort() : (short)0;
            this.Style1LowFireAngle = (this.Version >= 9) ? reader.GetShort() : (short)45;
            this.Style1HighFireAngle = (this.Version >= 9) ? reader.GetShort() : (short)45;
            this.Style1KeyAssignment = (this.Version >= 15) ? reader.GetShort() : (short)1;
            this.Style1BallFriction = (this.Version >= 15) ? reader.GetShort() : (short)-1;
            this.Style2BallSpeed = (this.Version >= 15) ? reader.GetShort() : (short)0;
            this.Style2LowFireAngle = (this.Version >= 15) ? reader.GetShort() : (short)0;
            this.Style2HighFireAngle = (this.Version >= 15) ? reader.GetShort() : (short)0;
            this.Style2KeyAssignment = (this.Version >= 15) ? reader.GetShort() : (short)0;
            this.Style2BallFriction = (this.Version >= 9) ? reader.GetShort() : (short)1;

            if (this.Version >= 24)
            {
                this.TerrainModifiers = reader.GetShorts(16);
            }
            else
            {
                for (int i = 0; i < this.TerrainModifiers.Length; i++)
                {
                    this.TerrainModifiers[i] = -1;
                }
            }

            if (this.Version >= 9 && this.Version < 23)
            {
                reader.Skip();
            }

            this.BallCarryRollVehicleItem = (this.Version >= 29) ? reader.GetShort() : (short)0;
            this.BallCarryArmorVehicleItem = (this.Version >= 29) ? reader.GetShort() : (short)0;
            this.FlagCarryRollVehicleItem = (this.Version >= 29) ? reader.GetShort() : (short)0;
            this.FlagCarryArmorVehicleItem = (this.Version >= 29) ? reader.GetShort() : (short)0;
            this.EventString1 = (this.Version >= 33) ? reader.GetString() : "";
            this.EventString2 = (this.Version >= 38) ? reader.GetString() : "";
            this.EventString3 = (this.Version >= 38) ? reader.GetString() : "";
            this.LosDistance = (this.Version >= 42) ? reader.GetInt() : 0;
            this.LosAngle = (this.Version >= 42) ? reader.GetInt() : 0;
            this.LosXRay = (this.Version >= 42) ? reader.GetInt() : 0;
            this.CombatAwarenessTime = (this.Version >= 42) ? reader.GetInt() : 0;
            this.SiblingKillsShared = (this.Version >= 42) ? reader.GetShort() : (short)0;
            this.RelativeId = (this.Version >= 47) ? reader.GetShort() : (short)0;
            this.DisplayOnFriendlyRadar = (this.Version >= 52) ? reader.GetInt() : 1;
            this.FriendlyRadarColor = (this.Version >= 50) ? reader.GetInt() : -1;
            this.DisplayOnEnemyRadar = (this.Version >= 52) ? reader.GetInt() : 1;
            this.EnemyRadarColor = (this.Version >= 50) ? reader.GetInt() : -1;

            if (this.Version >= 48)
            {
                for (int i = 0; i < 30; i++)
                {
                    this.HoldItemLimits[i] = reader.GetShort();
                    this.HoldItemExtendedLimits[i] = reader.GetShort();
                }
            }
        }
    }
}
