namespace Assets
{
    public class SpriteBlob // 004022D0
    {
        public string BlobName; // 00
        public string BlobId; // 18
        public short LightPermutation; // 30
        public short PaletteOffset; // 32
        public HSV Hsv = new HSV(); // 34
        public short AnimationTime; // 38

        // >= 0
        public void ParseV1(ICsvParser parser)
        {
            this.BlobName = parser.GetString();
            this.BlobId = parser.GetString();
            this.LightPermutation = parser.GetShort();
            this.PaletteOffset = parser.GetShort();
            this.Hsv = new HSV();
            this.AnimationTime = 0;
        }

        // >= 12
        public void ParseV2(ICsvParser parser)
        {
            this.BlobName = parser.GetString();
            this.BlobId = parser.GetString();
            this.LightPermutation = parser.GetShort();
            this.PaletteOffset = parser.GetShort();
            this.Hsv = parser.GetInstance<HSV>();
            this.AnimationTime = 0;
        }

        // >= 30
        public void ParseV3(ICsvParser parser)
        {
            this.BlobName = parser.GetString();
            this.BlobId = parser.GetString();
            this.LightPermutation = parser.GetShort();
            this.PaletteOffset = parser.GetShort();
            this.Hsv = parser.GetInstance<HSV>();
            this.AnimationTime = parser.GetShort();
        }

        public void FixUnknown18()
        {
            this.BlobId = string.Format("gfx{0:5D}", int.Parse(this.BlobId));
        }
    }
}
