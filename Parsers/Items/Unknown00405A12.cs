namespace Parsers
{
    public abstract partial class Item : ICsvFormat
    {
        public class Unknown00405A12
        {
            public short Unknown0;
            public int Unknown4;
            public int Unknown8;
            public int UnknownC;

            public void ReadV1(ICsvReader reader)
            {
                this.Unknown0 = reader.GetShort();
                this.Unknown4 = reader.GetInt();
                this.Unknown8 = reader.GetInt();
                this.UnknownC = 2;
            }

            public void ReadV2(ICsvReader reader)
            {
                this.Unknown0 = reader.GetShort();
                this.Unknown4 = reader.GetInt();
                this.Unknown8 = reader.GetInt();
                this.UnknownC = reader.GetInt();
            }
        }
    }
}
