using System;

namespace Parsers
{
    public abstract partial class Item : ICsvFormat
    {
        public sealed class Upgrade : Item
        {
            public short[,] Unknown120 { get; set; }

            public Upgrade()
                : base()
            {
                this.Unknown120 = new short[16, 2];
            }

            public override void Read(ICsvReader reader)
            {
                base.Read(reader);
                for (int i = 0; i < 16; i++)
                {
                    this.Unknown120[i, 0] = reader.GetShort();
                    this.Unknown120[i, 1] = reader.GetShort();
                }
            }
        }
    }
}
