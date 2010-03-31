namespace Assets
{
    public class SoundBlob : ICsvParseable
    {
        public string BlobName; // 00
        public string BlobId; // 18
        public short Simultaneous; // 30
        public short Unknown32; // 32

        public void Parse(ICsvParser parser)
        {
            this.BlobName = parser.GetString();
            this.BlobId = parser.GetString();
            this.Simultaneous = parser.GetShort();
            this.Unknown32 = parser.GetShort();
        }

        public void FixBlobId()
        {
            this.BlobId = string.Format("wav{0:5D}", int.Parse(this.BlobId));
        }
    }
}
