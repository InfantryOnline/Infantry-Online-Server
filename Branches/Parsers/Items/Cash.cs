using System;

namespace Parsers
{
    public abstract partial class Item : ICsvFormat
    {
        public sealed class Cash : Item
        {
            public int Unknown124 { get; set; }
            public short Unknown128 { get; set; }
            public short Unknown14C { get; set; }
            public short Unknown12A { get; set; }
            public short[] Unknown12C { get; set; }
            public short Unknown14E { get; set; }

            public Cash()
                : base()
            {
                this.Unknown12C = new short[16];
            }

            public override void Read(ICsvReader reader)
            {
                base.Read(reader);
                this.Unknown124 = (this.Version >= 18) ? reader.GetInt() : 1;
                this.Unknown128 = (this.Version >= 18) ? reader.GetShort() : (short)0;
                this.Unknown14C = (this.Version >= 18) ? reader.GetShort() : (short)0;
                this.Unknown12A = (this.Version >= 18) ? reader.GetShort() : (short)0;
                this.Unknown124 = (this.Version >= 31) ? reader.GetInt() : 0;
                this.Unknown12C = (this.Version >= 32) ? reader.GetShorts(16) : new short[16];
                this.Unknown14E = (this.Version >= 54) ? reader.GetShort() : (short)0;
            }
        }
    }
}
