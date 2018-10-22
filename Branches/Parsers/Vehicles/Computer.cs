namespace Parsers
{
    public abstract partial class Vehicle : ICsvFormat
    {
        public sealed class Computer : Vehicle
        {
            public int RotateSpeed { get; set; } // 5D0
            public short AngleStart { get; set; } // 5D4
            public short AngleLength { get; set; } // 5D6
            public short TrackingTime { get; set; } // 5D8
            public short TrackingRadius { get; set; } // 5DA
            public int TrackingWeightLow { get; set; } // 5C8
            public int TrackingWeightHigh { get; set; } // 5CC
            public short FireRadius { get; set; } // 5DC
            public short RepairRate { get; set; } // 5DE
            public short HitpointsRequiredToOperate { get; set; } // 5E0
            public short RemoveGlobalTimer { get; set; } // 5E4
            public short RandomizeAim { get; set; } // 5E6
            public short ObeyLos { get; set; } // 5E8
            public short ComputerEnergyMax { get; set; } // 5EA
            public short ComputerEnergyRate { get; set; } // 5EC
            public short Destroyable { get; set; } // 5E2
            public short DensityRadius { get; set; } // 602
            public short DensityMaxType { get; set; } // 5EE
            public short DensityMaxActive { get; set; } // 5F0
            public short DensityMaxInactive { get; set; } // 5F2
            public short MaxTypeByPlayerRegardlessOfTeam { get; set; } // 5F4
            public short FrequencyMaxType { get; set; } // 5F6
            public short FrequencyMaxActive { get; set; } // 5F8
            public short FrequencyMaxInactive { get; set; } // 5FA
            public short FrequencyDensityMaxType { get; set; } // 5FC
            public short FrequencyDensityMaxActive { get; set; } // 5FE
            public short FrequencyDensityMaxInactive { get; set; } // 600
            public int ChainedTurretRequiredForBuilding { get; set; } // 688
            public int ChainedTurretRequiredForOperation { get; set; } // 68C
            public short ChainedTurretId { get; set; } // 690
            public short ChainedTurretRadius { get; set; } // 692
            public short NumChainedTurretsRequired { get; set; } // 694
            public string LogicTakeOwnership { get; set; } // 606
            public string LogicStealOwnership { get; set; } // 646
            public short SpriteTurretZAdjust { get; set; } // 604
            public BlobSprite SpriteBase { get; set; } // 1358
            public BlobSprite SpriteTurret { get; set; } // 1394
            public ComputerProduct[] Products { get; set; } // 698

            public Computer()
                : base()
            {
                SpriteBase = new BlobSprite();
                SpriteTurret = new BlobSprite();
                Products = CreateInstances<ComputerProduct>(16);
            }

            public override void Read(ICsvReader reader)
            {
                base.Read(reader);
                this.RotateSpeed = reader.GetInt();
                this.AngleStart = reader.GetShort();
                this.AngleLength = reader.GetShort();
                this.TrackingTime = reader.GetShort();
                this.TrackingRadius = reader.GetShort();
                this.TrackingWeightLow = (this.Version >= 30) ? reader.GetInt() : 0;
                this.TrackingWeightHigh = (this.Version >= 30) ? reader.GetInt() : 100000;
                this.FireRadius = reader.GetShort();
                this.RepairRate = reader.GetShort();
                this.HitpointsRequiredToOperate = reader.GetShort();
                this.RemoveGlobalTimer = (this.Version >= 7) ? reader.GetShort() : (short)0;
                this.RandomizeAim = (this.Version >= 7) ? reader.GetShort() : (short)0;
                this.ObeyLos = (this.Version >= 7) ? reader.GetShort() : (short)0;
                this.ComputerEnergyMax = (this.Version >= 7) ? reader.GetShort() : (short)0;
                this.ComputerEnergyRate = (this.Version >= 7) ? reader.GetShort() : (short)0;
                this.Destroyable = (this.Version >= 10) ? reader.GetShort() : (short)0;
                this.DensityRadius = (this.Version >= 13) ? reader.GetShort() : (short)800;
                this.DensityMaxType = (this.Version >= 13) ? reader.GetShort() : (short)2;
                this.DensityMaxActive = (this.Version >= 13) ? reader.GetShort() : (short)2;
                this.DensityMaxInactive = (this.Version >= 13) ? reader.GetShort() : (short)-1;
                this.MaxTypeByPlayerRegardlessOfTeam = (this.Version >= 53) ? reader.GetShort() : (short)-1;
                this.FrequencyMaxType = (this.Version >= 13) ? reader.GetShort() : (short)10;
                this.FrequencyMaxActive = (this.Version >= 13) ? reader.GetShort() : (short)10;
                this.FrequencyMaxInactive = (this.Version >= 13) ? reader.GetShort() : (short)-1;
                this.FrequencyDensityMaxType = (this.Version >= 39) ? reader.GetShort() : (short)-1;
                this.FrequencyDensityMaxActive = (this.Version >= 39) ? reader.GetShort() : (short)-1;
                this.FrequencyDensityMaxInactive = (this.Version >= 39) ? reader.GetShort() : (short)-1;
                this.ChainedTurretRequiredForBuilding = (this.Version >= 53) ? reader.GetInt() : (short)0;
                this.ChainedTurretRequiredForOperation = (this.Version >= 53) ? reader.GetInt() : (short)0;
                this.ChainedTurretId = (this.Version >= 53) ? reader.GetShort() : (short)-1;
                this.ChainedTurretRadius = (this.Version >= 53) ? reader.GetShort() : (short)0;
                this.NumChainedTurretsRequired = (this.Version >= 53) ? reader.GetShort() : (short)0;
                this.LogicTakeOwnership = (this.Version >= 49) ? reader.GetString() : "1&!1";
                this.LogicStealOwnership = (this.Version >= 49) ? reader.GetString() : "1&!1";
                this.SpriteTurretZAdjust = (this.Version >= 27) ? reader.GetShort() : (short)0;

                if (this.Version >= 30)
                {
                    this.SpriteBase.ReadV3(reader);
                    this.SpriteTurret.ReadV3(reader);
                }
                else if (this.Version >= 12)
                {
                    this.SpriteBase.ReadV2(reader);
                    this.SpriteTurret.ReadV2(reader);
                }
                else
                {
                    this.SpriteBase.ReadV1(reader);
                    this.SpriteTurret.ReadV1(reader);
                }

                if (this.Version < 3)
                {
                    this.SpriteBase.FixBlobId();
                    this.SpriteTurret.FixBlobId();
                }

                if (this.Version >= 22)
                {
                    for (int i = 0; i < 16; i++)
                    {
                        this.Products[i].Read(this.Version, reader);
                    }
                }
            }
        }
    }
}
