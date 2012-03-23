namespace Assets
{
    public abstract partial class VehInfo : ICsvParseable
    {
        public sealed class Dependent : VehInfo
        {
            public int ChildRotateLeft; // 5E0
            public int ChildRotateRight; // 5E4
            public short ChildParentRelativeRotation; // 5CC
            public short ChildAngleStart; // 5CE
            public short ChildAngleLength; // 5D0
            public short ChildCenterDeltaX; // 5D2
            public short ChildCenterDeltaY; // 5D4
            public short ChildCenterDeltaZ; // 5D6
            public short ChildSortAdjust; // 5D8
            public short ChildDisableWhenParentDead; // 5C8
            public short ChildDisableWhenParentEmpty; // 5CA
            public short ChildElevationLowAngle; // 5DA
            public short ChildEelevationHighAngle; // 5DC
            public short ChildElevationSpeed; // 5DE
            public string Unknown6D0; // 6D0
            public string Unknown6E8; // 6E8
            public short Unknown700; // 700
            public short Unknown702; // 702
            public SpriteBlob SpriteOccupied = new SpriteBlob(); // 5E8
            public SpriteBlob SpriteEmpty = new SpriteBlob(); // 624
            public SpriteBlob SpriteBroken = new SpriteBlob(); // 660
            public SoundBlob SoundRotate; // 69C
            
            public Dependent()
            {
                Type = Types.Dependent;
            }

            public override void Parse(ICsvParser parser)
            {
                base.Parse(parser);
                this.ChildRotateLeft = parser.GetInt();
                this.ChildRotateRight = parser.GetInt();
                this.ChildParentRelativeRotation = parser.GetShort();
                this.ChildAngleStart = parser.GetShort();
                this.ChildAngleLength = parser.GetShort();
                this.ChildCenterDeltaX = parser.GetShort();
                this.ChildCenterDeltaY = parser.GetShort();
                this.ChildCenterDeltaZ = (this.Version >= 27) ? parser.GetShort() : (short)0;
                this.ChildSortAdjust = parser.GetShort();
                this.ChildDisableWhenParentDead = (this.Version >= 14) ? parser.GetShort() : (short)0;
                this.ChildDisableWhenParentEmpty = (this.Version >= 38) ? parser.GetShort() : (short)0;
                this.ChildElevationLowAngle = (this.Version >= 44) ? parser.GetShort() : (short)0;
                this.ChildEelevationHighAngle = (this.Version >= 44) ? parser.GetShort() : (short)0;
                this.ChildElevationSpeed = (this.Version >= 44) ? parser.GetShort() : (short)0;
                this.Unknown6D0 = (this.Version >= 44) ? parser.GetQuotedString() : "";
                this.Unknown6E8 = (this.Version >= 44) ? parser.GetQuotedString() : "";
                this.Unknown700 = (this.Version >= 44) ? parser.GetShort() : (short)0;
                this.Unknown702 = (this.Version >= 44) ? parser.GetShort() : (short)0;

                if (this.Version >= 30)
                {
                    this.SpriteOccupied.ParseV3(parser);
                    this.SpriteEmpty.ParseV3(parser);
                    this.SpriteBroken.ParseV3(parser);
                }
                else if (this.Version >= 12)
                {
                    this.SpriteOccupied.ParseV2(parser);
                    this.SpriteEmpty.ParseV2(parser);
                    this.SpriteBroken.ParseV2(parser);
                }
                else
                {
                    this.SpriteOccupied.ParseV1(parser);
                    this.SpriteEmpty.ParseV1(parser);
                    this.SpriteBroken.ParseV1(parser);
                }

                this.SoundRotate = parser.GetInstance<SoundBlob>();

                if (this.Version < 3)
                {
                    this.SpriteOccupied.FixUnknown18();
                    this.SpriteEmpty.FixUnknown18();
                    this.SpriteBroken.FixUnknown18();
                    this.SoundRotate.FixBlobId();
                }


                base.blofiles.Add(SpriteOccupied.BlobName);
                base.blofiles.Add(SpriteEmpty.BlobName);
                base.blofiles.Add(SpriteBroken.BlobName);
                base.blofiles.Add(SoundRotate.BlobName);

            }
        }
    }
}
