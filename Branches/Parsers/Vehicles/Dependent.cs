namespace Parsers
{
    public abstract partial class Vehicle : ICsvFormat
    {
        public sealed class Dependent : Vehicle
        {
            public int ChildRotateLeft { get; set; } // 5E0
            public int ChildRotateRight { get; set; } // 5E4
            public short ChildParentRelativeRotation { get; set; } // 5CC
            public short ChildAngleStart { get; set; } // 5CE
            public short ChildAngleLength { get; set; } // 5D0
            public short ChildCenterDeltaX { get; set; } // 5D2
            public short ChildCenterDeltaY { get; set; } // 5D4
            public short ChildCenterDeltaZ { get; set; } // 5D6
            public short ChildSortAdjust { get; set; } // 5D8
            public short ChildDisableWhenParentDead { get; set; } // 5C8
            public short ChildDisableWhenParentEmpty { get; set; } // 5CA
            public short ChildElevationLowAngle { get; set; } // 5DA
            public short ChildEelevationHighAngle { get; set; } // 5DC
            public short ChildElevationSpeed { get; set; } // 5DE
            public string Unknown6D0 { get; set; } // 6D0
            public string Unknown6E8 { get; set; } // 6E8
            public short Unknown700 { get; set; } // 700
            public short Unknown702 { get; set; } // 702
            public BlobSprite SpriteOccupied { get; set; } // 5E8
            public BlobSprite SpriteEmpty { get; set; } // 624
            public BlobSprite SpriteBroken { get; set; } // 660
            public BlobSound SoundRotate { get; set; } // 69C

            public Dependent()
                : base()
            {
                this.SpriteOccupied = new BlobSprite();
                this.SpriteEmpty = new BlobSprite();
                this.SpriteBroken = new BlobSprite();
                this.SoundRotate = new BlobSound();
            }

            public override void Read(ICsvReader reader)
            {
                base.Read(reader);
                this.ChildRotateLeft = reader.GetInt();
                this.ChildRotateRight = reader.GetInt();
                this.ChildParentRelativeRotation = reader.GetShort();
                this.ChildAngleStart = reader.GetShort();
                this.ChildAngleLength = reader.GetShort();
                this.ChildCenterDeltaX = reader.GetShort();
                this.ChildCenterDeltaY = reader.GetShort();
                this.ChildCenterDeltaZ = (this.Version >= 27) ? reader.GetShort() : (short)0;
                this.ChildSortAdjust = reader.GetShort();
                this.ChildDisableWhenParentDead = (this.Version >= 14) ? reader.GetShort() : (short)0;
                this.ChildDisableWhenParentEmpty = (this.Version >= 38) ? reader.GetShort() : (short)0;
                this.ChildElevationLowAngle = (this.Version >= 44) ? reader.GetShort() : (short)0;
                this.ChildEelevationHighAngle = (this.Version >= 44) ? reader.GetShort() : (short)0;
                this.ChildElevationSpeed = (this.Version >= 44) ? reader.GetShort() : (short)0;
                this.Unknown6D0 = (this.Version >= 44) ? reader.GetString() : "";
                this.Unknown6E8 = (this.Version >= 44) ? reader.GetString() : "";
                this.Unknown700 = (this.Version >= 44) ? reader.GetShort() : (short)0;
                this.Unknown702 = (this.Version >= 44) ? reader.GetShort() : (short)0;

                if (this.Version >= 30)
                {
                    this.SpriteOccupied.ReadV3(reader);
                    this.SpriteEmpty.ReadV3(reader);
                    this.SpriteBroken.ReadV3(reader);
                }
                else if (this.Version >= 12)
                {
                    this.SpriteOccupied.ReadV2(reader);
                    this.SpriteEmpty.ReadV2(reader);
                    this.SpriteBroken.ReadV2(reader);
                }
                else
                {
                    this.SpriteOccupied.ReadV1(reader);
                    this.SpriteEmpty.ReadV1(reader);
                    this.SpriteBroken.ReadV1(reader);
                }

                this.SoundRotate = reader.GetInstance<BlobSound>();

                if (this.Version < 3)
                {
                    this.SpriteOccupied.FixBlobId();
                    this.SpriteEmpty.FixBlobId();
                    this.SpriteBroken.FixBlobId();
                    this.SoundRotate.FixBlobId();
                }
            }
        }
    }
}
