namespace Assets
{
    /// <summary>
    /// Hue-Saturation-Value
    /// </summary>
    public class HSV : ICsvParseable
    {
        public sbyte Hue;
        public sbyte Saturation;
        public sbyte Value;

        public void Parse(ICsvParser parser)
        {
            this.Hue = parser.GetSByte();
            this.Saturation = parser.GetSByte();
            this.Value = parser.GetSByte();
        }
    }
}
