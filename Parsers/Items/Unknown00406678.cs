namespace Parsers
{
    public abstract partial class Item : ICsvFormat
    {
        public class Unknown00406678 : ICsvFormat
        {
            public int Unknown4 { get; set; }
            public short Unknown0 { get; set; }

            public void Read(ICsvReader reader)
            {
                this.Unknown4 = reader.GetInt();
                this.Unknown0 = reader.GetShort();
            }
        }
    }
}
