using System;

namespace Parsers
{
    public abstract partial class Item : ICsvFormat
    {
        public sealed class Warp : Use
        {
            public short Unknown26C { get; set; }
            public short Unknown26E { get; set; }
            public short Unknown270 { get; set; }
            public short Unknown272 { get; set; }
            public short Unknown274 { get; set; }
            public int Unknown260 { get; set; }
            public int Unknown268 { get; set; }
            public int Unknown264 { get; set; }
            public BlobSprite Unknown278 { get; set; }
            public BlobSound Unknown2B4 { get; set; }

            public Warp() : base()
            {
                this.Unknown278 = new BlobSprite();
                this.Unknown2B4 = new BlobSound();
            }

            public override void Read(ICsvReader reader)
            {
                base.Read(reader);

                this.Unknown26C = reader.GetShort();
                if (this.Version < 22 && this.Unknown26C > 6)
                {
                    this.Unknown26C = 6;
                }

                this.Unknown26E = (this.Version >= 22) ? reader.GetShort() : (short)0;
                this.Unknown270 = (this.Version >= 46) ? reader.GetShort() : (short)0;
                this.Unknown272 = reader.GetShort();
                this.Unknown274 = reader.GetShort();
                this.Unknown260 = reader.GetInt();
                this.Unknown268 = (this.Version >= 57) ? reader.GetInt() : 0;
                this.Unknown264 = (this.Version >= 57) ? reader.GetInt() : 0;

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
            }
        }
    }
}
