using System;

namespace Parsers
{
    public abstract partial class Item : ICsvFormat
    {
        public sealed class Repair : Use
        {
            public short Unknown274 { get; set; }
            public int Unknown268 { get; set; }
            public int Unknown26C { get; set; }
            public short Unknown270 { get; set; }
            public int Unknown260 { get; set; }
            public int Unknown264 { get; set; }
            public short Unknown272 { get; set; }
            public BlobSprite Unknown278 { get; set; }
            public BlobSound Unknown2B4 { get; set; }

            public Repair() : base()
            {
                this.Unknown278 = new BlobSprite();
                this.Unknown2B4 = new BlobSound();
            }

            public override void Read(ICsvReader reader)
            {
                base.Read(reader);
                this.Unknown274 = reader.GetShort();

                if (this.Version >= 36)
                {
                    this.Unknown268 = reader.GetInt();
                    this.Unknown26C = reader.GetInt();
                }
                else
                {
                    int dummy = reader.GetInt();
                    if (dummy >= 0)
                    {
                        this.Unknown268 = 0;
                        this.Unknown26C = dummy;
                    }
                    else
                    {
                        this.Unknown268 = -dummy;
                        this.Unknown26C = 0;
                    }
                }

                this.Unknown270 = (this.Version >= 36) ? reader.GetShort() : (short)0;
                this.Unknown260 = reader.GetInt();
                this.Unknown264 = reader.GetInt();
                this.Unknown272 = (this.Version >= 30) ? reader.GetShort() : (short)0;

                if (this.Version >= 33)
                {
                    this.Unknown278.ReadV3(reader);
                }
                else if (this.Version >= 17)
                {
                    this.Unknown278.ReadV2(reader);
                }
                else
                {
                    this.Unknown278.ReadV1(reader);
                }

                this.Unknown2B4 = reader.GetInstance<BlobSound>();

                if (this.Version < 3)
                {
                    this.Unknown278.FixBlobId();
                    this.Unknown2B4.FixBlobId();
                }
            }
        }
    }
}
