namespace Parsers
{
    /// <summary>
    /// Hue-Saturation-Value
    /// </summary>
    public class HSV : ICsvFormat
    {
        public sbyte Hue;
        public sbyte Saturation;
        public sbyte Value;

        public void Read(ICsvReader reader)
        {
            this.Hue = reader.GetSByte();
            this.Saturation = reader.GetSByte();
            this.Value = reader.GetSByte();
        }
    }
}
