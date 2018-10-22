using System;

namespace Infantry_Launcher.Protocol.Helpers
{
    /// <summary>
    /// Our asset object
    /// </summary>
    public class Asset
    {
        public AssetDescriptor Descriptor { get; private set; }
        public string FileName { get; private set; }
        public byte[] Data { get; private set; }

        public Asset(string fileName, byte[] data, AssetDescriptor descriptor)
        {
            if (fileName == null)
                throw new ArgumentNullException("fileName");
            if (descriptor == null)
                throw new ArgumentNullException("descriptor");
            if (data == null)
                throw new ArgumentNullException("data");
            FileName = fileName;
            Data = data;
            Descriptor = descriptor;
        }
    }

    /// <summary>
    /// Our asset description object
    /// </summary>
    public class AssetDescriptor
    {
        public string Name { get; private set; }
        public int CrcValue { get; private set; }
        public string Compression { get; private set; }
        public long DownloadSize { get; private set; }
        public string Md5Hash { get; private set; }
        public long FileSize { get; private set; }

        internal AssetDescriptor(string name, int crcValue, string compression, long downloadSize, string md5Hash, long fileSize)
        {
            Name = name;
            CrcValue = crcValue;
            Compression = compression;
            DownloadSize = downloadSize;
            Md5Hash = md5Hash;
            FileSize = fileSize;
        }
    }
}