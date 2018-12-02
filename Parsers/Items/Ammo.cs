namespace Parsers
{
    public abstract partial class Item : ICsvFormat
    {
        public sealed class Ammo : Item
        {
            public override void Read(ICsvReader reader)
            {
                base.Read(reader);
            }
        }
    }
}
