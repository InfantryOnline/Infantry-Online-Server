namespace Parsers
{
    public abstract partial class Item : ICsvFormat
    {
        public class Unknown004066B0 : ICsvFormat
        {
            public short Unknown2 { get; set; }
            public short Unknown0 { get; set; }

            public void Read(ICsvReader reader)
            {
                this.Unknown2 = reader.GetShort();
                this.Unknown0 = reader.GetShort();
            }
        }
    }
}
