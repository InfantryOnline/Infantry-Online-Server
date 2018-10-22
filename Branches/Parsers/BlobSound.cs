namespace Parsers
{
    public class BlobSound : ICsvFormat
    {
        public string BlobName; // 00
        public string BlobId; // 18
        public short Simultaneous; // 30
        public short Unknown32; // 32

        public void Read(ICsvReader reader)
        {
            this.BlobName = reader.GetString();
            this.BlobId = reader.GetString();
            this.Simultaneous = reader.GetShort();
            this.Unknown32 = reader.GetShort();
        }

        public void FixBlobId()
        {
            this.BlobId = string.Format("wav{0:5D}", int.Parse(this.BlobId));
        }
    }
}
