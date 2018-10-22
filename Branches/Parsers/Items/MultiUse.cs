using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Parsers
{
    public abstract partial class Item : ICsvFormat
    {
        public sealed class MultiUse : Use
        {
            public BlobSound Unknown260 { get; set; }
            public Unknown00404B70[] Unknown294 { get; set; }

            public MultiUse()
                : base()
            {
                this.Unknown260 = new BlobSound();
                this.Unknown294 = CreateInstances<Unknown00404B70>(32);
            }

            public override void Read(ICsvReader reader)
            {
                base.Read(reader);
                this.Unknown260 = reader.GetInstance<BlobSound>();

                for (int i = 0; i < 32 && reader.AtEnd == false; i++)
                {
                    this.Unknown294[i] = reader.GetInstance<Unknown00404B70>();
                }
            }
        }
    }
}
