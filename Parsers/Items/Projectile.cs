namespace Parsers
{
    public abstract partial class Item : ICsvFormat
    {
        public sealed class Projectile : Use
        {
            public short Unknown318 { get; set; }
            public short Unknown31A { get; set; }
            public short Unknown31C { get; set; }
            public short Unknown31E { get; set; }
            public short Unknown320 { get; set; }
            public short Unknown322 { get; set; }
            public short Unknown32A { get; set; }
            public short Unknown32C { get; set; }
            public short Unknown324 { get; set; }
            public short Unknown326 { get; set; }
            public short Unknown328 { get; set; }
            public short Unknown32E { get; set; }
            public short Unknown33A { get; set; }
            public short Unknown33C { get; set; }
            public short Unknown33E { get; set; }
            public short Unknown330 { get; set; }
            public int Unknown340 { get; set; }
            public int Unknown344 { get; set; }
            public int Unknown354 { get; set; }
            public int Unknown358 { get; set; }
            public int Unknown35C { get; set; }
            public int Unknown360 { get; set; }
            public int Unknown348 { get; set; }
            public short Unknown2E0 { get; set; }
            public short Unknown2E4 { get; set; }
            public short Unknown2E2 { get; set; }
            public short Unknown2E6 { get; set; }
            public short Unknown2E8 { get; set; }
            public short Unknown2EA { get; set; }
            public short Unknown2EC { get; set; }
            public short Unknown2EE { get; set; }
            public short Unknown2F4 { get; set; }
            public short Unknown2F6 { get; set; }
            public short Unknown2F2 { get; set; }
            public short Unknown2F8 { get; set; }
            public short Unknown2F0 { get; set; }
            public short Unknown2FA { get; set; }
            public short Unknown2FC { get; set; }
            public short Unknown2FE { get; set; }
            public short Unknown300 { get; set; }
            public short Unknown302 { get; set; }
            public short Unknown304 { get; set; }
            public short Unknown306 { get; set; }
            public short Unknown308 { get; set; }
            public short Unknown30A { get; set; }
            public short Unknown30C { get; set; }
            public short Unknown30E { get; set; }
            public short Unknown310 { get; set; }
            public short Unknown312 { get; set; }
            public short Unknown314 { get; set; }
            public short Unknown316 { get; set; }
            public int Unknown34C { get; set; }
            public short Unknown332 { get; set; }
            public short Unknown336 { get; set; }
            public string Unknown260 { get; set; }
            public short Unknown334 { get; set; }
            public short Unknown338 { get; set; }
            public int Unknown350 { get; set; }
            public Unknown00405A12[] Unknown52C { get; set; }
            public BlobSprite Unknown364 { get; set; }
            public BlobSprite Unknown3A0 { get; set; }
            public BlobSprite Unknown3DC { get; set; }
            public BlobSprite Unknown418 { get; set; }
            public BlobSprite Unknown454 { get; set; }
            public BlobSound Unknown490 { get; set; }
            public BlobSound Unknown4C4 { get; set; }
            public BlobSound Unknown4F8 { get; set; }

            public Projectile()
                : base()
            {
                Unknown318 = 10000;
                Unknown328 = 1000;
                Unknown2F6 = 50;
                Unknown52C = CreateInstances<Unknown00405A12>(6);
                Unknown364 = new BlobSprite();
                Unknown3A0 = new BlobSprite();
                Unknown3DC = new BlobSprite();
                Unknown418 = new BlobSprite();
                Unknown454 = new BlobSprite();
                Unknown490 = new BlobSound();
                Unknown4C4 = new BlobSound();
                Unknown4F8 = new BlobSound();
            }

            public override void Read(ICsvReader reader)
            {
                base.Read(reader);
                this.Unknown318 = reader.GetShort();
                this.Unknown31A = reader.GetShort();
                
                if (this.Version >= 25)
                {
                    this.Unknown31C = reader.GetShort();
                    this.Unknown31E = reader.GetShort();
                }
                else
                {
                    reader.Skip();
                }

                this.Unknown320 = reader.GetShort();
                this.Unknown322 = reader.GetShort();
                this.Unknown32A = reader.GetShort();
                this.Unknown32C = reader.GetShort();
                this.Unknown324 = reader.GetShort();
                this.Unknown326 = reader.GetShort();
                this.Unknown328 = reader.GetShort();
                this.Unknown32E = reader.GetShort();
                this.Unknown33A = reader.GetShort();
                this.Unknown33C = reader.GetShort();
                this.Unknown33E = reader.GetShort();
                this.Unknown330 = reader.GetShort();
                this.Unknown340 = reader.GetInt();
                this.Unknown344 = reader.GetInt();
                this.Unknown354 = reader.GetInt();
                this.Unknown358 = reader.GetInt();
                this.Unknown35C = reader.GetInt();
                this.Unknown360 = reader.GetInt();
                this.Unknown348 = reader.GetInt();
                this.Unknown2E0 = (this.Version >= 1) ? reader.GetShort() : (short)0;
                this.Unknown2E4 = (this.Version >= 48) ? reader.GetShort() : (short)0;
                this.Unknown2E2 = (this.Version >= 37) ? reader.GetShort() : (short)0;
                this.Unknown2E6 = (this.Version >= 1) ? reader.GetShort() : (short)10;
                this.Unknown2E8 = (this.Version >= 1) ? reader.GetShort() : (short)0;
                this.Unknown2EA = (this.Version >= 1) ? reader.GetShort() : (short)2000;
                this.Unknown2EC = (this.Version >= 26) ? reader.GetShort() : (short)0;
                this.Unknown2EE = (this.Version >= 1) ? reader.GetShort() : (short)30;
                this.Unknown2F4 = (this.Version >= 1) ? reader.GetShort() : (short)0;
                this.Unknown2F6 = (this.Version >= 42) ? reader.GetShort() : (short)50;
                this.Unknown2F2 = (this.Version >= 43) ? reader.GetShort() : (short)0;
                this.Unknown2F8 = (this.Version >= 43) ? reader.GetShort() : (short)0;
                this.Unknown2F0 = (this.Version >= 2) ? reader.GetShort() : (short)0;
                this.Unknown2FA = (this.Version >= 40) ? reader.GetShort() : (short)0;

                if (this.Version < 40)
                {
                    reader.Skip(2);
                }

                this.Unknown2FC = (this.Version >= 14) ? reader.GetShort() : (short)0;
                this.Unknown2FE = (this.Version >= 14) ? reader.GetShort() : (short)0;
                this.Unknown300 = (this.Version >= 14) ? reader.GetShort() : (short)0;
                this.Unknown302 = (this.Version >= 14) ? reader.GetShort() : (short)0;
                this.Unknown304 = (this.Version >= 16) ? reader.GetShort() : (short)0;
                this.Unknown306 = (this.Version >= 16) ? reader.GetShort() : (short)0;
                this.Unknown308 = (this.Version >= 16) ? reader.GetShort() : (short)0;
                this.Unknown30A = (this.Version >= 16) ? reader.GetShort() : (short)0;
                this.Unknown30C = (this.Version >= 16) ? reader.GetShort() : (short)0;
                this.Unknown30E = (this.Version >= 27) ? reader.GetShort() : (short)0;
                this.Unknown310 = (this.Version >= 27) ? reader.GetShort() : (short)0;

                if (this.Version >= 53)
                {
                    this.Unknown312 = reader.GetShort();
                }
                else if (this.Version >= 20)
                {
                    this.Unknown312 = -1;
                    reader.Skip();
                }

                this.Unknown314 = (this.Version >= 23) ? reader.GetShort() : (short)0;
                this.Unknown316 = (this.Version >= 36) ? reader.GetShort() : (short)0;
                this.Unknown34C = (this.Version >= 38) ? reader.GetInt() : 0;
                this.Unknown332 = (this.Version >= 36) ? reader.GetShort() : (short)0;
                this.Unknown336 = (this.Version >= 36) ? reader.GetShort() : (short)0;
                this.Unknown260 = (this.Version >= 36) ? reader.GetString() : "";
                this.Unknown334 = (this.Version >= 39) ? reader.GetShort() : (short)0;
                this.Unknown338 = (this.Version >= 41) ? reader.GetShort() : (short)0;
                this.Unknown350 = (this.Version >= 44) ? reader.GetInt() : 0;

                for (int i = 0; i < 6; i++)
                {
                    if (this.Version >= 50)
                    {
                        this.Unknown52C[i].ReadV2(reader);
                    }
                    else
                    {
                        this.Unknown52C[i].ReadV1(reader);
                    }
                }

                if (this.Version >= 33)
                {
                    this.Unknown364.ReadV3(reader);
                    this.Unknown3A0.ReadV3(reader);
                    this.Unknown3DC.ReadV3(reader);
                    this.Unknown418.ReadV3(reader);
                    this.Unknown454.ReadV3(reader);
                }
                else if (this.Version >= 17)
                {
                    this.Unknown364.ReadV2(reader);
                    this.Unknown3A0.ReadV2(reader);
                    this.Unknown3DC.ReadV2(reader);
                    this.Unknown418.ReadV2(reader);
                    this.Unknown454.ReadV2(reader);
                }
                else
                {
                    this.Unknown364.ReadV1(reader);
                    this.Unknown3A0.ReadV1(reader);
                    this.Unknown3DC.ReadV1(reader);
                    this.Unknown418.ReadV1(reader);
                    this.Unknown454.ReadV1(reader);
                }

                this.Unknown490 = reader.GetInstance<BlobSound>();
                this.Unknown4C4 = reader.GetInstance<BlobSound>();
                this.Unknown4F8 = reader.GetInstance<BlobSound>();

                if (this.Version < 3)
                {
                    this.Unknown364.FixBlobId();
                    this.Unknown3A0.FixBlobId();
                    this.Unknown3DC.FixBlobId();
                    this.Unknown418.FixBlobId();
                    this.Unknown454.FixBlobId();
                    this.Unknown490.FixBlobId();
                    this.Unknown4C4.FixBlobId();
                    this.Unknown4F8.FixBlobId();
                }
            }
        }
    }
}
