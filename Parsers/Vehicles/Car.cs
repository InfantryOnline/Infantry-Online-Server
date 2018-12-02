namespace Parsers
{
    public abstract partial class Vehicle : ICsvFormat
    {
        public sealed class Car : Vehicle
        {
            public SpeedValues[] TerrainSpeeds { get; set; } // 004216A0
            public short BouncePercent { get; set; } // 9F0
            public short Mode { get; set; } // 9F2
            public short RemoveDeadTimer { get; set; } // 9FC
            public short RemoveUnoccupiedTimer { get; set; } // 9FE
            public short RemoveGlobalTimer { get; set; } // A00
            public short ThrustOffsetX { get; set; } // 9F4
            public short ThrustOffsetY { get; set; } // 9F6
            public short ThrustHeight { get; set; } // 9F8
            public short ThrustEffectSpeed { get; set; } // 9FA
            public short SmokeCount { get; set; } // A02
            public short SmokeDelta { get; set; } // A04
            public int HyperEnergyCost { get; set; } // 9EC
            public short GravityAcceleration { get; set; } // A06
            public short FloorBouncePercent { get; set; } // A08
            public short ThrustWhileFlying { get; set; } // A0A
            public short RotateWhileFlying { get; set; } // A0C
            public short CatchLipPercent { get; set; } // A0E
            public short PortalGravity { get; set; } // A10
            public short ThrustDelay { get; set; } // A12
            public short ThrustOffsetDelay { get; set; } // A14
            //
            // NOTE: "SpriteRoll" instead of "RollSprite" used so all Sprites/Sounds
            // are clearly visible in intellisense, y/n? - Jovan
            // (Probably should be aggregated under Sprites and Sounds structs maybe?)
            //
            public BlobSprite SpriteRoll { get; set; } // 5C8
            public BlobSprite SpriteEmpty { get; set; } // 604
            public BlobSprite SpriteBroken { get; set; } // 640
            public BlobSprite SpriteThrust { get; set; } // 67C
            public BlobSprite SpriteSmoke { get; set; } // 6BB
            public BlobSprite SpriteStopped { get; set; } // 6F4
            public BlobSprite SpriteShadow { get; set; } // 730
            public BlobSound SoundIdle { get; set; } // A16
            public BlobSound SoundThrust { get; set; } // A4A
            public BlobSound SoundRotate { get; set; } // A7E
            public BlobSound SoundDeath { get; set; } // AB2

            public Car()
                : base()
            {
                this.TerrainSpeeds = CreateInstances<SpeedValues>(16);
                this.SpriteRoll = new BlobSprite();
                this.SpriteEmpty = new BlobSprite();
                this.SpriteBroken = new BlobSprite();
                this.SpriteThrust = new BlobSprite();
                this.SpriteSmoke = new BlobSprite();
                this.SpriteStopped = new BlobSprite();
                this.SpriteShadow = new BlobSprite();
                this.SoundIdle = new BlobSound();
                this.SoundThrust = new BlobSound();
                this.SoundRotate = new BlobSound();
                this.SoundDeath = new BlobSound();
            }

            public override void Read(ICsvReader reader)
            {
                base.Read(reader);

                for (int i = 0; i < 16; i++)
                {
                    this.TerrainSpeeds[i].Parse(this.Version, reader);
                }

                this.BouncePercent = reader.GetShort();
                this.Mode = reader.GetShort();
                this.RemoveDeadTimer = (this.Version >= 2) ? reader.GetShort() : (short)0;
                this.RemoveUnoccupiedTimer = (this.Version >= 2) ? reader.GetShort() : (short)0;
                this.RemoveGlobalTimer = (this.Version >= 2) ? reader.GetShort() : (short)0;
                this.ThrustOffsetX = (this.Version >= 5) ? reader.GetShort() : (short)0;
                this.ThrustOffsetY = (this.Version >= 5) ? reader.GetShort() : (short)0;
                this.ThrustHeight = (this.Version >= 5) ? reader.GetShort() : (short)0;
                this.ThrustEffectSpeed = (this.Version >= 43) ? reader.GetShort() : (short)4000;
                this.SmokeCount = (this.Version >= 5) ? reader.GetShort() : (short)0;
                this.SmokeDelta = (this.Version >= 5) ? reader.GetShort() : (short)0;
                this.HyperEnergyCost = (this.Version >= 16) ? reader.GetInt() : 0;
                this.GravityAcceleration = (this.Version >= 18) ? reader.GetShort() : (short)0;
                this.FloorBouncePercent = (this.Version >= 18) ? reader.GetShort() : (short)0;
                this.ThrustWhileFlying = (this.Version >= 20) ? reader.GetShort() : (short)1;
                this.RotateWhileFlying = (this.Version >= 20) ? reader.GetShort() : (short)1;
                this.CatchLipPercent = (this.Version >= 28) ? reader.GetShort() : (short)1000;
                this.PortalGravity = (this.Version >= 40) ? reader.GetShort() : (short)0;
                this.ThrustDelay = (this.Version >= 41) ? reader.GetShort() : (short)10;
                this.ThrustOffsetDelay = (this.Version >= 41) ? reader.GetShort() : (short)24;

                if (this.Version >= 30)
                {
                    this.SpriteRoll.ReadV3(reader);
                    this.SpriteEmpty.ReadV3(reader);
                    this.SpriteBroken.ReadV3(reader);
                    this.SpriteThrust.ReadV3(reader);
                    this.SpriteSmoke.ReadV3(reader);
                    this.SpriteStopped.ReadV3(reader);
                }
                else if (this.Version >= 12)
                {
                    this.SpriteRoll.ReadV2(reader);
                    this.SpriteEmpty.ReadV2(reader);
                    this.SpriteBroken.ReadV2(reader);
                    this.SpriteThrust.ReadV2(reader);
                    this.SpriteSmoke.ReadV2(reader);
                    this.SpriteStopped.ReadV2(reader);
                }
                else
                {
                    this.SpriteRoll.ReadV1(reader);
                    this.SpriteEmpty.ReadV1(reader);
                    this.SpriteBroken.ReadV1(reader);
                    this.SpriteThrust.ReadV1(reader);
                    this.SpriteSmoke.ReadV1(reader);
                    this.SpriteStopped.ReadV1(reader);
                }

                if (this.Version >= 30)
                {
                    this.SpriteShadow.ReadV3(reader);
                }
                else if (this.Version >= 19)
                {
                    this.SpriteShadow.ReadV2(reader);
                }

                this.SoundIdle = reader.GetInstance<BlobSound>();
                this.SoundThrust = reader.GetInstance<BlobSound>();
                this.SoundRotate = reader.GetInstance<BlobSound>();

                if (this.Version >= 4)
                {
                    this.SoundDeath = reader.GetInstance<BlobSound>();
                }
                else
                {
                    this.SoundDeath = new BlobSound()
                    {
                        BlobName = "None",
                    };
                }

                if (this.Version < 3)
                {
                    this.SpriteRoll.FixBlobId();
                    this.SpriteEmpty.FixBlobId();
                    this.SpriteBroken.FixBlobId();
                    this.SpriteThrust.FixBlobId();
                    this.SpriteSmoke.FixBlobId();
                    this.SpriteStopped.FixBlobId();
                    this.SoundIdle.FixBlobId();
                    this.SoundThrust.FixBlobId();
                    this.SoundRotate.FixBlobId();
                }
            }
        }
    }
}
