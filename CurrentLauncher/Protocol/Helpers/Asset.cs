using System;

namespace InfantryLauncher.Protocol
{
    public class Asset
    {
        public AssetDownloader.AssetDescriptor Descriptor { get; private set; }

        public string FileName { get; private set; }

        public byte[] Data { get; private set; }

        public Asset(string fileName, byte[] data, AssetDownloader.AssetDescriptor descriptor)
        {
            if (fileName == null)
                throw new ArgumentNullException("fileName");
            if (descriptor == null)
                throw new ArgumentNullException("descriptor");
            if (data == null)
                throw new ArgumentNullException("data");
            this.FileName = fileName;
            this.Data = data;
            this.Descriptor = descriptor;
        }
    }
}
