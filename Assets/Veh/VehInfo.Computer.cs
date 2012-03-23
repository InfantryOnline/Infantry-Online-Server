namespace Assets
{
    public abstract partial class VehInfo : ICsvParseable
    {
        public sealed class Computer : VehInfo
        {
            public int RotateSpeed; // 5D0
            public short AngleStart; // 5D4
            public short AngleLength; // 5D6
            public short TrackingTime; // 5D8
            public short TrackingRadius; // 5DA
            public int TrackingWeightLow; // 5C8
            public int TrackingWeightHigh; // 5CC
            public short FireRadius; // 5DC
            public short RepairRate; // 5DE
            public short HitpointsRequiredToOperate; // 5E0
            public short RemoveGlobalTimer; // 5E4
            public short RandomizeAim; // 5E6
            public short ObeyLos; // 5E8
            public short ComputerEnergyMax; // 5EA
            public short ComputerEnergyRate; // 5EC
            public short Destroyable; // 5E2
            public short DensityRadius; // 602
            public short DensityMaxType; // 5EE
            public short DensityMaxActive; // 5F0
            public short DensityMaxInactive; // 5F2
            public short MaxTypeByPlayerRegardlessOfTeam; // 5F4
            public short FrequencyMaxType; // 5F6
            public short FrequencyMaxActive; // 5F8
            public short FrequencyMaxInactive; // 5FA
            public short FrequencyDensityMaxType; // 5FC
            public short FrequencyDensityMaxActive; // 5FE
            public short FrequencyDensityMaxInactive; // 600
            public int ChainedTurretRequiredForBuilding; // 688
            public int ChainedTurretRequiredForOperation; // 68C
            public short ChainedTurretId; // 690
            public short ChainedTurretRadius; // 692
            public short NumChainedTurretsRequired; // 694
            public string LogicTakeOwnership; // 606
            public string LogicStealOwnership; // 646
            public short SpriteTurretZAdjust; // 604
            public SpriteBlob SpriteBase = new SpriteBlob(); // 1358
            public SpriteBlob SpriteTurret = new SpriteBlob(); // 1394
            public ComputerProduct[] Products = CreateInstances<ComputerProduct>(16); // 698

            public Computer()
            {
                Type = Types.Computer;
            }

            public override void Parse(ICsvParser parser)
            {
                base.Parse(parser);

                this.RotateSpeed = parser.GetInt();
                this.AngleStart = parser.GetShort();
                this.AngleLength = parser.GetShort();
                this.TrackingTime = parser.GetShort();
                this.TrackingRadius = parser.GetShort();
                this.TrackingWeightLow = (this.Version >= 30) ? parser.GetInt() : 0;
                this.TrackingWeightHigh = (this.Version >= 30) ? parser.GetInt() : 100000;
                this.FireRadius = parser.GetShort();
                this.RepairRate = parser.GetShort();
                this.HitpointsRequiredToOperate = parser.GetShort();
                this.RemoveGlobalTimer = (this.Version >= 7) ? parser.GetShort() : (short)0;
                this.RandomizeAim = (this.Version >= 7) ? parser.GetShort() : (short)0;
                this.ObeyLos = (this.Version >= 7) ? parser.GetShort() : (short)0;
                this.ComputerEnergyMax = (this.Version >= 7) ? parser.GetShort() : (short)0;
                this.ComputerEnergyRate = (this.Version >= 7) ? parser.GetShort() : (short)0;
                this.Destroyable = (this.Version >= 10) ? parser.GetShort() : (short)0;
                this.DensityRadius = (this.Version >= 13) ? parser.GetShort() : (short)800;
                this.DensityMaxType = (this.Version >= 13) ? parser.GetShort() : (short)2;
                this.DensityMaxActive = (this.Version >= 13) ? parser.GetShort() : (short)2;
                this.DensityMaxInactive = (this.Version >= 13) ? parser.GetShort() : (short)-1;
                this.MaxTypeByPlayerRegardlessOfTeam = (this.Version >= 53) ? parser.GetShort() : (short)-1;
                this.FrequencyMaxType = (this.Version >= 13) ? parser.GetShort() : (short)10;
                this.FrequencyMaxActive = (this.Version >= 13) ? parser.GetShort() : (short)10;
                this.FrequencyMaxInactive = (this.Version >= 13) ? parser.GetShort() : (short)-1;
                this.FrequencyDensityMaxType = (this.Version >= 39) ? parser.GetShort() : (short)-1;
                this.FrequencyDensityMaxActive = (this.Version >= 39) ? parser.GetShort() : (short)-1;
                this.FrequencyDensityMaxInactive = (this.Version >= 39) ? parser.GetShort() : (short)-1;
                this.ChainedTurretRequiredForBuilding = (this.Version >= 53) ? parser.GetInt() : (short)0;
                this.ChainedTurretRequiredForOperation = (this.Version >= 53) ? parser.GetInt() : (short)0;
                this.ChainedTurretId = (this.Version >= 53) ? parser.GetShort() : (short)-1;
                this.ChainedTurretRadius = (this.Version >= 53) ? parser.GetShort() : (short)0;
                this.NumChainedTurretsRequired = (this.Version >= 53) ? parser.GetShort() : (short)0;
                this.LogicTakeOwnership = (this.Version >= 49) ? parser.GetQuotedString() : "1&!1";
                this.LogicStealOwnership = (this.Version >= 49) ? parser.GetQuotedString() : "1&!1";
                this.SpriteTurretZAdjust = (this.Version >= 27) ? parser.GetShort() : (short)0;

                if (this.Version >= 30)
                {
                    this.SpriteBase.ParseV3(parser);
                    this.SpriteTurret.ParseV3(parser);
                }
                else if (this.Version >= 12)
                {
                    this.SpriteBase.ParseV2(parser);
                    this.SpriteTurret.ParseV2(parser);
                }
                else
                {
                    this.SpriteBase.ParseV1(parser);
                    this.SpriteTurret.ParseV1(parser);
                }

                if (this.Version < 3)
                {
                    this.SpriteBase.FixUnknown18();
                    this.SpriteTurret.FixUnknown18();
                }

                base.blofiles.Add(SpriteBase.BlobName);
                base.blofiles.Add(SpriteTurret.BlobName);

                if (this.Version >= 22)
                {
                    for (int i = 0; i < 16; i++)
                    {
                        this.Products[i].Parse(this.Version, parser);
                    }
                }
            }
        }
    }
}
