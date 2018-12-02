namespace Parsers
{
    public abstract partial class Item : ICsvFormat
    {
        public abstract class Use : Item
        {
            public short Unknown19C { get; set; }
            public short Unknown194 { get; set; }
            public short Unknown196 { get; set; }
            public short Unknown198 { get; set; }
            public short Unknown19A { get; set; }
            public int[] Unknown154 { get; set; }
            public int Unknown14C { get; set; }
            public int Unknown150 { get; set; }
            public int Unknown120 { get; set; }
            public int Unknown124 { get; set; }
            public int Unknown128 { get; set; }
            public int Unknown12C { get; set; }
            public int Unknown140 { get; set; }
            public int Unknown134 { get; set; }
            public int Unknown138 { get; set; }
            public int Unknown13C { get; set; }
            public int Unknown144 { get; set; }
            public int Unknown148 { get; set; }
            public int Unknown1C0 { get; set; }
            public int Unknown1BC { get; set; }
            public short Unknown1A0 { get; set; }
            public short Unknown1A2 { get; set; }
            public short Unknown19E { get; set; }
            public short Unknown1A4 { get; set; }
            public short Unknown1A6 { get; set; }
            public BlobSprite Unknown1C4 { get; set; }
            public BlobSound Unknown200 { get; set; }
            public short Unknown1A8 { get; set; }
            public short Unknown1AA { get; set; }
            public int Unknown130 { get; set; }
            public short Unknown1B4 { get; set; }
            public short Unknown1B6 { get; set; }
            public int Unknown1AC { get; set; }
            public short Unknown1B2 { get; set; }
            public short Unknown1B0 { get; set; }
            public short Unknown1B8 { get; set; }
            public short Unknown1BA { get; set; }

            public Use() : base()
            {
                this.Unknown154 = new int[16];
                this.Unknown120 = 50;
                this.Unknown124 = 30;
                this.Unknown128 = -1;
                this.Unknown1C4 = new BlobSprite();
                this.Unknown200 = new BlobSound();
                this.Unknown1B4 = 1;
                this.Unknown1B6 = 1;
            }

            public override void Read(ICsvReader reader)
            {
                base.Read(reader);

                if (this.Version < 8)
                {
                    reader.Skip(3);
                }

                this.Unknown19C = reader.GetShort();
                this.Unknown194 = reader.GetShort();
                this.Unknown196 = reader.GetShort();
                this.Unknown198 = (this.Version >= 16) ? reader.GetShort() : (short)0;
                this.Unknown19A = (this.Version >= 16) ? reader.GetShort() : (short)0;
                this.Unknown154 = reader.GetInts(16);
                this.Unknown14C = (this.Version >= 53) ? reader.GetInt() : 0;
                this.Unknown150 = (this.Version >= 53) ? reader.GetInt() : 0;
                this.Unknown120 = reader.GetInt();
                this.Unknown124 = reader.GetInt();
                this.Unknown128 = (this.Version >= 43) ? reader.GetInt() : -1;
                this.Unknown12C = (this.Version >= 45) ? reader.GetInt() : 0;
                this.Unknown140 = reader.GetInt();
                this.Unknown134 = (this.Version >= 40) ? reader.GetInt() : this.Unknown140;
                this.Unknown138 = (this.Version >= 40) ? reader.GetInt() : 0;
                this.Unknown13C = (this.Version >= 40) ? reader.GetInt() : 0;
                this.Unknown144 = reader.GetInt();
                this.Unknown148 = reader.GetInt();
                this.Unknown1C0 = reader.GetInt();
                this.Unknown1BC = (this.Version >= 38) ? reader.GetInt() : 0;
                this.Unknown1A0 = reader.GetShort();
                this.Unknown1A2 = (this.Version >= 25) ? reader.GetShort() : (short)0;
                this.Unknown19E = reader.GetShort();
                this.Unknown1A4 = (this.Version >= 36) ? reader.GetShort() : (short)0;
                this.Unknown1A6 = (this.Version >= 36) ? reader.GetShort() : (short)0;

                if (this.Version >= 33)
                {
                    this.Unknown1C4.ReadV3(reader);
                }
                else if (this.Version >= 17)
                {
                    this.Unknown1C4.ReadV2(reader);
                }
                else
                {
                    this.Unknown1C4.ReadV1(reader);
                }

                this.Unknown200 = reader.GetInstance<BlobSound>();
                this.Unknown1A8 = reader.GetShort();
                this.Unknown1AA = reader.GetShort();
                this.Unknown130 = (this.Version >= 7) ? reader.GetInt() : 0;
                this.Unknown1B4 = (this.Version >= 19) ? reader.GetShort() : (short)1;
                this.Unknown1B6 = (this.Version >= 19) ? reader.GetShort() : (short)1;
                this.Unknown1AC = (this.Version >= 27) ? reader.GetInt() : 0;
                this.Unknown1B2 = (this.Version >= 27) ? reader.GetShort() : (short)1;
                this.Unknown1B0 = (this.Version >= 27) ? reader.GetShort() : (short)1;
                this.Unknown1B8 = (this.Version >= 27) ? reader.GetShort() : (short)1;
                this.Unknown1BA = (this.Version >= 27) ? reader.GetShort() : (short)1;

                if (this.Version < 4)
                {
                    reader.Skip(8 * 2);
                }

                if (this.Version < 3)
                {
                    this.Unknown1C4.FixBlobId();
                    this.Unknown200.FixBlobId();
                }
            }
        }
    }
}
