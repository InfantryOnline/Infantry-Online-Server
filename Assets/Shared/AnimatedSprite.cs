namespace Assets
{
    /// <summary>
    /// Class inside base Vehicle type.
    /// </summary>
    public class AnimatedSprite
    {
        public short TickDistance; // 00
        public short TickFrequency; // 04
        public short Speed; // 08
        public short Color; // 0C
        public SpriteBlob Blob = new SpriteBlob(); // 10

        public void Parse(int version, ICsvParser parser)
        {
            this.TickDistance = parser.GetShort();
            this.TickFrequency = parser.GetShort();
            this.Speed = parser.GetShort();
            this.Color = parser.GetShort();

            if (version >= 30)
            {
                this.Blob.ParseV3(parser);
            }
            else if (version >= 12)
            {
                this.Blob.ParseV2(parser);
            }
            else
            {
                this.Blob.ParseV1(parser);
            }
        }
    }
}
