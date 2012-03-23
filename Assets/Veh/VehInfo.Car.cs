namespace Assets
{
    public abstract partial class VehInfo : ICsvParseable
    {
        public sealed class Car : VehInfo
        {
            public SpeedValues[] TerrainSpeeds = CreateInstances<SpeedValues>(16); // 004216A0
            public short BouncePercent; // 9F0
            public short Mode; // 9F2
            public short RemoveDeadTimer; // 9FC
            public short RemoveUnoccupiedTimer; // 9FE
            public short RemoveGlobalTimer; // A00
            public short ThrustOffsetX; // 9F4
            public short ThrustOffsetY; // 9F6
            public short ThrustHeight; // 9F8
            public short ThrustEffectSpeed; // 9FA
            public short SmokeCount; // A02
            public short SmokeDelta; // A04
            public int HyperEnergyCost; // 9EC
            public short GravityAcceleration; // A06
            public short FloorBouncePercent; // A08
            public short ThrustWhileFlying; // A0A
            public short RotateWhileFlying; // A0C
            public short CatchLipPercent; // A0E
            public short PortalGravity; // A10
            public short ThrustDelay; // A12
            public short ThrustOffsetDelay; // A14
            //
            // NOTE: "SpriteRoll" instead of "RollSprite" used so all Sprites/Sounds
            // are clearly visible in intellisense, y/n? - Jovan
            // (Probably should be aggregated under Sprites and Sounds structs maybe?)
            //
            public SpriteBlob SpriteRoll = new SpriteBlob(); // 5C8
            public SpriteBlob SpriteEmpty = new SpriteBlob(); // 604
            public SpriteBlob SpriteBroken = new SpriteBlob(); // 640
            public SpriteBlob SpriteThrust = new SpriteBlob(); // 67C
            public SpriteBlob SpriteSmoke = new SpriteBlob(); // 6BB
            public SpriteBlob SpriteStopped = new SpriteBlob(); // 6F4
            public SpriteBlob SpriteShadow = new SpriteBlob(); // 730
            public SoundBlob SoundIdle = new SoundBlob(); // A16
            public SoundBlob SoundThrust = new SoundBlob(); // A4A
            public SoundBlob SoundRotate = new SoundBlob(); // A7E
            public SoundBlob SoundDeath = new SoundBlob(); // AB2

            public Car()
            {
                Type = Types.Car;
            }

            public override void Parse(ICsvParser parser)
            {
                base.Parse(parser);

                for (int i = 0; i < 16; i++)
                {
                    this.TerrainSpeeds[i].Parse(this.Version, parser);
                }

                this.BouncePercent = parser.GetShort();
                this.Mode = parser.GetShort();
                this.RemoveDeadTimer = (this.Version >= 2) ? parser.GetShort() : (short)0;
                this.RemoveUnoccupiedTimer = (this.Version >= 2) ? parser.GetShort() : (short)0;
                this.RemoveGlobalTimer = (this.Version >= 2) ? parser.GetShort() : (short)0;
                this.ThrustOffsetX = (this.Version >= 5) ? parser.GetShort() : (short)0;
                this.ThrustOffsetY = (this.Version >= 5) ? parser.GetShort() : (short)0;
                this.ThrustHeight = (this.Version >= 5) ? parser.GetShort() : (short)0;
                this.ThrustEffectSpeed = (this.Version >= 43) ? parser.GetShort() : (short)4000;
                this.SmokeCount = (this.Version >= 5) ? parser.GetShort() : (short)0;
                this.SmokeDelta = (this.Version >= 5) ? parser.GetShort() : (short)0;
                this.HyperEnergyCost = (this.Version >= 16) ? parser.GetInt() : 0;
                this.GravityAcceleration = (this.Version >= 18) ? parser.GetShort() : (short)0;
                this.FloorBouncePercent = (this.Version >= 18) ? parser.GetShort() : (short)0;
                this.ThrustWhileFlying = (this.Version >= 20) ? parser.GetShort() : (short)1;
                this.RotateWhileFlying = (this.Version >= 20) ? parser.GetShort() : (short)1;
                this.CatchLipPercent = (this.Version >= 28) ? parser.GetShort() : (short)1000;
                this.PortalGravity = (this.Version >= 40) ? parser.GetShort() : (short)0;
                this.ThrustDelay = (this.Version >= 41) ? parser.GetShort() : (short)10;
                this.ThrustOffsetDelay = (this.Version >= 41) ? parser.GetShort() : (short)24;

                if (this.Version >= 30)
                {
                    this.SpriteRoll.ParseV3(parser);
                    this.SpriteEmpty.ParseV3(parser);
                    this.SpriteBroken.ParseV3(parser);
                    this.SpriteThrust.ParseV3(parser);
                    this.SpriteSmoke.ParseV3(parser);
                    this.SpriteStopped.ParseV3(parser);
                }
                else if (this.Version >= 12)
                {
                    this.SpriteRoll.ParseV2(parser);
                    this.SpriteEmpty.ParseV2(parser);
                    this.SpriteBroken.ParseV2(parser);
                    this.SpriteThrust.ParseV2(parser);
                    this.SpriteSmoke.ParseV2(parser);
                    this.SpriteStopped.ParseV2(parser);
                }
                else
                {
                    this.SpriteRoll.ParseV1(parser);
                    this.SpriteEmpty.ParseV1(parser);
                    this.SpriteBroken.ParseV1(parser);
                    this.SpriteThrust.ParseV1(parser);
                    this.SpriteSmoke.ParseV1(parser);
                    this.SpriteStopped.ParseV1(parser);
                }

                if (this.Version >= 30)
                {
                    this.SpriteShadow.ParseV3(parser);
                }
                else if (this.Version >= 19)
                {
                    this.SpriteShadow.ParseV2(parser);
                }

                if (this.Version >= 4)
                {
                    this.SoundDeath = parser.GetInstance<SoundBlob>();
                    base.blofiles.Add(SoundDeath.BlobName);
                }
                else
                {
                    this.SoundDeath = new SoundBlob()
                    {
                        BlobName = "None",
                    };
                }

                if (this.Version < 3)
                {
                    this.SpriteRoll.FixUnknown18();
                    this.SpriteEmpty.FixUnknown18();
                    this.SpriteBroken.FixUnknown18();
                    this.SpriteThrust.FixUnknown18();
                    this.SpriteSmoke.FixUnknown18();
                    this.SpriteStopped.FixUnknown18();
                    this.SoundIdle.FixBlobId();
                    this.SoundThrust.FixBlobId();
                    this.SoundRotate.FixBlobId();

                }

                base.blofiles.Add(SpriteRoll.BlobName);
                base.blofiles.Add(SpriteEmpty.BlobName);
                base.blofiles.Add(SpriteBroken.BlobName);
                base.blofiles.Add(SpriteThrust.BlobName);
                base.blofiles.Add(SpriteSmoke.BlobName);
                base.blofiles.Add(SpriteStopped.BlobName);
                this.SoundIdle = parser.GetInstance<SoundBlob>();
                this.SoundThrust = parser.GetInstance<SoundBlob>();
                this.SoundRotate = parser.GetInstance<SoundBlob>();
                base.blofiles.Add(SoundIdle.BlobName);
                base.blofiles.Add(SoundThrust.BlobName);
                base.blofiles.Add(SoundRotate.BlobName);

            }
        }
    }
}
