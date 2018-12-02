namespace Parsers
{
    public class BlobSprite // 004022D0
    {
        public string BlobName; // 00
        public string BlobId; // 18
        public short LightPermutation; // 30
        public short PaletteOffset; // 32
        public HSV Hsv = new HSV(); // 34
        public short AnimationTime; // 38

        // >= 0
        public void ReadV1(ICsvReader reader)
        {
            this.BlobName = reader.GetString();
            this.BlobId = reader.GetString();
            this.LightPermutation = reader.GetShort();
            this.PaletteOffset = reader.GetShort();
            this.Hsv = new HSV();
            this.AnimationTime = 0;
        }

        // >= 12
        public void ReadV2(ICsvReader reader)
        {
            this.BlobName = reader.GetString();
            this.BlobId = reader.GetString();
            this.LightPermutation = reader.GetShort();
            this.PaletteOffset = reader.GetShort();
            this.Hsv = reader.GetInstance<HSV>();
            this.AnimationTime = 0;
        }

        // >= 30
        public void ReadV3(ICsvReader reader)
        {
            this.BlobName = reader.GetString();
            this.BlobId = reader.GetString();
            this.LightPermutation = reader.GetShort();
            this.PaletteOffset = reader.GetShort();
            this.Hsv = reader.GetInstance<HSV>();
            this.AnimationTime = reader.GetShort();
        }

        public void FixBlobId()
        {
            this.BlobId = string.Format("gfx{0:5D}", int.Parse(this.BlobId));
        }
    }
}
