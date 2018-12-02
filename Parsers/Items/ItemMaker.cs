namespace Parsers
{
    public abstract partial class Item : ICsvFormat
    {
        public sealed class ItemMaker : Use
        {
            public int Unknown260;
            public int Unknown264;

            public override void Read(ICsvReader reader)
            {
                base.Read(reader);
                this.Unknown260 = reader.GetInt();
                this.Unknown264 = reader.GetInt();
            }
        }
    }
}
