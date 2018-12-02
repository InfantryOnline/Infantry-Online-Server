namespace Parsers
{
    public abstract partial class Vehicle : ICsvFormat
    {
        public class ArmorValues : ICsvFormat // 00420480
        {
            public int SelfIgnore; // 0
            public int PassIgnore; // 4
            public short SelfReduction; // 8
            public short Pass; // A

            public void Read(ICsvReader stream)
            {
                this.SelfIgnore = stream.GetInt();
                this.PassIgnore = stream.GetInt();
                this.SelfReduction = stream.GetShort();
                this.Pass = stream.GetShort();
            }
        }
    }
}
