using System;

namespace Parsers
{
    public abstract partial class Item : ICsvFormat
    {
        public sealed class Skill : Item
        {
            public string[] Unknown120 { get; set; }
            public int[] Unknown520 { get; set; }

            public Skill()
            {
                this.Unknown120 = new string[16];
                this.Unknown520 = new int[16];
            }

            public override void Read(ICsvReader reader)
            {
                base.Read(reader);

                if (this.Version >= 21)
                {
                    for (int i = 0; i < 16; i++)
                    {
                        this.Unknown120[i] = reader.GetString();
                        this.Unknown520[i] = reader.GetInt();
                    }
                }
                else
                {
                    this.Unknown520[0] = reader.GetInt();
                }
            }
        }
    }
}
