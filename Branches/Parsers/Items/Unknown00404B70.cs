namespace Parsers
{
    public abstract partial class Item : ICsvFormat
    {
        public class Unknown00404B70 : ICsvFormat
        {
            public short Unknown8 { get; set; }
            public short Unknown2 { get; set; }
            public short Unknown4 { get; set; }
            public short Unknown6 { get; set; }
            public short Unknown0 { get; set; }
            public int UnknownC { get; set; }

            public void Read(ICsvReader reader)
            {
                this.Unknown8 = reader.GetShort();
                this.Unknown2 = reader.GetShort();
                this.Unknown4 = reader.GetShort();
                this.Unknown6 = reader.GetShort();
                this.Unknown0 = reader.GetShort();
                this.UnknownC = reader.GetInt();
            }
        }
    }
}
