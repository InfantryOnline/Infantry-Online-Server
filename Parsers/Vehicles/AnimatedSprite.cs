namespace Parsers
{
    public abstract partial class Vehicle : ICsvFormat
    {
        /// <summary>
        /// Class inside base Vehicle type.
        /// </summary>
        public class AnimatedSprite
        {
            public short TickDistance { get; set; } // 00
            public short TickFrequency { get; set; } // 04
            public short Speed { get; set; } // 08
            public short Color { get; set; } // 0C
            public BlobSprite Blob { get; set; } // 10

            public AnimatedSprite()
            {
                this.Blob = new BlobSprite();
            }

            public void Read(int version, ICsvReader reader)
            {
                this.TickDistance = reader.GetShort();
                this.TickFrequency = reader.GetShort();
                this.Speed = reader.GetShort();
                this.Color = reader.GetShort();

                if (version >= 30)
                {
                    this.Blob.ReadV3(reader);
                }
                else if (version >= 12)
                {
                    this.Blob.ReadV2(reader);
                }
                else
                {
                    this.Blob.ReadV1(reader);
                }
            }
        }
    }
}
