using System;

namespace Parsers
{
    public abstract partial class Item : ICsvFormat
    {
        public sealed class Nested : Item
        {
            public override void Read(ICsvReader reader)
            {
                base.Read(reader);
                throw new NotImplementedException();
            }
        }
    }
}
