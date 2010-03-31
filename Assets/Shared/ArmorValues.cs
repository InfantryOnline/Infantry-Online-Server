namespace Assets
{
    public class ArmorValues : ICsvParseable // 00420480
    {
        public int SelfIgnore; // 0
        public int PassIgnore; // 4
        public short SelfReduction; // 8
        public short Pass; // A

        public void Parse(ICsvParser stream)
        {
            this.SelfIgnore = stream.GetInt();
            this.PassIgnore = stream.GetInt();
            this.SelfReduction = stream.GetShort();
            this.Pass = stream.GetShort();
        }
    }
}
