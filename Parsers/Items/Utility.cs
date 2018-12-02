using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Parsers
{
    public abstract partial class Item : ICsvFormat
    {
        public sealed class Utility : Item
        {
            public short Unknown162 { get; set; }
            public short Unknown160 { get; set; }
            public int Unknown134 { get; set; }
            public int Unknown138 { get; set; }
            public short Unknown13C { get; set; }
            public short Unknown13E { get; set; }
            public short Unknown140 { get; set; }
            public short Unknown142 { get; set; }
            public short Unknown144 { get; set; }
            public short Unknown146 { get; set; }
            public short Unknown148 { get; set; }
            public short Unknown152 { get; set; }
            public short Unknown154 { get; set; }
            public short Unknown14E { get; set; }
            public short Unknown150 { get; set; }
            public short Unknown14A { get; set; }
            public int Unknown168 { get; set; }
            public BlobSound Unknown214 { get; set; }
            public Unknown00406678[] Unknown16C { get; set; }
            public Unknown004066B0[] Unknown19C { get; set; }
            public short Unknown156 { get; set; }
            public short Unknown15A { get; set; }
            public short Unknown158 { get; set; }
            public short Unknown15C { get; set; }
            public short Unknown15E { get; set; }
            public short Unknown164 { get; set; }
            public short Unknown166 { get; set; }
            public int Unknown12C { get; set; }
            public int Unknown130 { get; set; }
            public int Unknown120 { get; set; }
            public int Unknown124 { get; set; }
            public int Unknown128 { get; set; }
            public short Unknown14C { get; set; }

            public Utility()
                : base()
            {
                this.Unknown214 = new BlobSound();
                this.Unknown16C = CreateInstances<Unknown00406678>(6);
                this.Unknown19C = CreateInstances<Unknown004066B0>(30);
            }

            public override void Read(ICsvReader reader)
            {
                base.Read(reader);
                this.Unknown162 = reader.GetShort();
                this.Unknown160 = reader.GetShort();
                this.Unknown134 = reader.GetInt();
                this.Unknown138 = reader.GetInt();
                this.Unknown13C = reader.GetShort();
                this.Unknown13E = reader.GetShort();
                this.Unknown140 = reader.GetShort();
                this.Unknown142 = reader.GetShort();
                this.Unknown144 = (this.Version >= 9) ? reader.GetShort() : (short)0;
                this.Unknown146 = (this.Version >= 10) ? reader.GetShort() : (short)0;
                this.Unknown148 = (this.Version >= 10) ? reader.GetShort() : (short)0;
                this.Unknown152 = (this.Version >= 28) ? reader.GetShort() : (short)0;
                this.Unknown154 = (this.Version >= 28) ? reader.GetShort() : (short)0;
                this.Unknown14E = (this.Version >= 28) ? reader.GetShort() : (short)0;
                this.Unknown150 = (this.Version >= 28) ? reader.GetShort() : (short)0;
                this.Unknown14A = (this.Version >= 28) ? reader.GetShort() : (short)0;
                this.Unknown168 = (this.Version >= 28) ? reader.GetInt() : 0;

                if (this.Version >= 11)
                {
                    this.Unknown214 = reader.GetInstance<BlobSound>();
                }

                for (int i = 0; i < 6; i++)
                {
                    this.Unknown16C[i].Read(reader);
                }

                if (this.Version >= 56)
                {
                    for (int i = 0; i < 30; i++)
                    {
                        this.Unknown19C[i].Read(reader);
                    }
                }

                this.Unknown156 = (this.Version >= 56) ? reader.GetShort() : (short)0;
                this.Unknown15A = (this.Version >= 56) ? reader.GetShort() : (short)0;
                this.Unknown158 = (this.Version >= 56) ? reader.GetShort() : (short)0;
                this.Unknown15C = (this.Version >= 56) ? reader.GetShort() : (short)0;
                this.Unknown15E = (this.Version >= 56) ? reader.GetShort() : (short)0;
                this.Unknown164 = (this.Version >= 56) ? reader.GetShort() : (short)0;
                this.Unknown166 = (this.Version >= 56) ? reader.GetShort() : (short)0;
                this.Unknown12C = (this.Version >= 56) ? reader.GetInt() : 0;
                this.Unknown130 = (this.Version >= 56) ? reader.GetInt() : 0;
                this.Unknown120 = (this.Version >= 56) ? reader.GetInt() : 0;
                this.Unknown124 = (this.Version >= 56) ? reader.GetInt() : 0;
                this.Unknown128 = (this.Version >= 56) ? reader.GetInt() : 0;
                this.Unknown14C = (this.Version >= 56) ? reader.GetShort() : (short)0;
            }
        }
    }
}
